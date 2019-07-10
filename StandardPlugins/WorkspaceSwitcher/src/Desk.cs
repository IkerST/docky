//  
//  Copyright (C) 2010 Rico Tzschichholz
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Gdk;
using Wnck;

namespace WorkspaceSwitcher
{
	internal class Desk
	{
		public Wnck.Workspace Parent { get; private set; }
		public string Name { get; private set; }
		public int Number { get; private set; }
		public Gdk.Rectangle Area { get; private set; }

		Dictionary<Wnck.MotionDirection, Desk> neighbors;
		Dictionary<Wnck.MotionDirection, Desk> wrapneighbors;
		
		public static Wnck.MotionDirection AlternateMovingDirection (Wnck.MotionDirection direction)
		{
			switch (direction) {
			case MotionDirection.Down: return MotionDirection.Right;
			case MotionDirection.Up: return MotionDirection.Left;
			case MotionDirection.Left: return MotionDirection.Up;
			case MotionDirection.Right: default: return MotionDirection.Down;
			}
		}

		public static Wnck.MotionDirection OppositeDirection (Wnck.MotionDirection direction)
		{
			switch (direction) {
			case MotionDirection.Down: return MotionDirection.Up;
			case MotionDirection.Up: return MotionDirection.Down;
			case MotionDirection.Left: return MotionDirection.Right;
			case MotionDirection.Right: default: return MotionDirection.Left;
			}
		}
		
		public bool IsVirtual {
			get {
				return Parent.IsVirtual;
			}
		}
		
		public bool IsActive {
			get {
				if (!Parent.IsVirtual)
					return Wnck.Screen.Default.ActiveWorkspace == Parent;
				else
					return Wnck.Screen.Default.ActiveWorkspace.ViewportX == Area.X && Wnck.Screen.Default.ActiveWorkspace.ViewportY == Area.Y;
			}
		}
		
		Desk GetUpperLeftDesk ()
		{
			Desk upperleft, next;
			upperleft = this;
			bool found = false;
			while (!found) {
				found = true;
				if ((next = upperleft.GetNeighbor (Wnck.MotionDirection.Up)) != null) {
					upperleft = next;
					found = false;
				}
				if ((next = upperleft.GetNeighbor (Wnck.MotionDirection.Left)) != null) {
					upperleft = next;
					found = false;
				}
			}
			return upperleft;
		}
		
		Gdk.Point GetDeskGridSize ()
		{
			Desk upperleft, temp, next;
			int rows = 1, cols = 1;
			upperleft = GetUpperLeftDesk ();
			if ((temp = upperleft.GetWrapNeighbor (MotionDirection.Up)) != null) {
				next = temp;
				rows++;
				while ((temp = next.GetNeighbor (MotionDirection.Up)) != null && temp != upperleft) {
					next = temp;
					rows++;
				}
			}
			if ((temp = upperleft.GetWrapNeighbor (MotionDirection.Left)) != null) {
				next = temp;
				cols++;
				while ((temp = next.GetNeighbor (MotionDirection.Left)) != null && temp != upperleft) {
					next = temp;
					cols++;
				}
			}
			return new Gdk.Point (cols, rows);
		}
		
		public Desk [,] GetDeskGridLayout ()
		{
			Desk next, desk = GetUpperLeftDesk ();
			Gdk.Point gridsize = GetDeskGridSize ();
			Desk [,] grid = new Desk [gridsize.X, gridsize.Y];
			grid [0, 0] = desk;
			int x = 0;
			for (int y = 0; y < gridsize.Y; y++) {
				x = 0;
				while ((next = desk.GetNeighbor (Wnck.MotionDirection.Right)) != null) {
					desk = next;
					x++;
					if (gridsize.X - 1 < x)
						break;
					grid [x, y] = desk;
				}
				if (gridsize.Y - 1 > y) {
					desk = (grid [0, y] != null ? grid [0, y].GetNeighbor (Wnck.MotionDirection.Down) : null);
					grid [0, y+1] = desk;
				}
			}
			return grid;
		}
		
		public void Activate ()
		{
			if (Parent.Screen.ActiveWorkspace != Parent)
				Parent.Activate (Gtk.Global.CurrentEventTime);
			if (Parent.IsVirtual)
				Parent.Screen.MoveViewport (Area.X, Area.Y);
		}
		
		public void SetNeighbor (Wnck.MotionDirection direction, Desk newneighbor)
		{
			Desk oldneighbor = GetNeighbor (direction);
			if (oldneighbor != null && oldneighbor != newneighbor) {
				neighbors.Remove (direction);
				if (oldneighbor.GetNeighbor (OppositeDirection (direction)) == this)
					oldneighbor.SetNeighbor (OppositeDirection (direction), null);
			}
			if (oldneighbor != newneighbor && newneighbor != null) {
				neighbors.Add (direction, newneighbor);
				
				if (GetNeighbor (OppositeDirection (direction)) == null) {
					Desk oldwrapneighbor = newneighbor.GetWrapNeighbor (OppositeDirection (direction));
					if (oldwrapneighbor != null) {
						SetWrapNeighbor (OppositeDirection (direction), oldwrapneighbor);
					} else {
						SetWrapNeighbor (OppositeDirection (direction), newneighbor);
					}
				}
				
				newneighbor.SetNeighbor (OppositeDirection (direction), this);
			}
		}

		public void SetWrapNeighbor (Wnck.MotionDirection direction, Desk newwrapneighbor)
		{
			Desk oldwrapneighbor = GetWrapNeighbor (direction);
			if (oldwrapneighbor != null && oldwrapneighbor != newwrapneighbor) {
				wrapneighbors.Remove (direction);
				if (oldwrapneighbor.GetWrapNeighbor (OppositeDirection (direction)) == this)
					oldwrapneighbor.SetWrapNeighbor (OppositeDirection (direction), null);
			}
			if (oldwrapneighbor != newwrapneighbor && newwrapneighbor != null) {
				wrapneighbors.Add (direction, newwrapneighbor);
				newwrapneighbor.SetWrapNeighbor (OppositeDirection (direction), this);
			}
		}
		
		public Desk GetNeighbor (Wnck.MotionDirection direction)
		{
			Desk desk;
			neighbors.TryGetValue (direction, out desk);
			return desk;
		}

		public Desk GetWrapNeighbor (Wnck.MotionDirection direction)
		{
			Desk desk;
			wrapneighbors.TryGetValue (direction, out desk);
			return desk;
		}
		
		public Desk (string name, int number, Gdk.Rectangle area, Workspace parent)
		{
			Parent = parent;
			Area = area;
			Name = name;
			Number = number;
			neighbors = new Dictionary<MotionDirection, Desk> ();
			wrapneighbors = new Dictionary<MotionDirection, Desk> ();
		}
		
		public Desk (Workspace parent) : this (parent.Name, parent.Number, new Gdk.Rectangle (0, 0, parent.Width, parent.Height), parent)
		{
		}
		
		public void Dispose ()
		{
			neighbors.Clear ();
			wrapneighbors.Clear ();
		}
	}
}
