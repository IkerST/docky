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
using System.Text;

using Mono.Unix;

using Cairo;
using GLib;
using Gdk;
using Gtk;
using Wnck;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;
using Docky.Services.Prefs;

namespace WorkspaceSwitcher
{
	public class WorkspaceSwitcherDockItem : IconDockItem
	{
		static IPreferences prefs = DockServices.Preferences.Get<WorkspaceSwitcherDockItem> ();
		
		bool? wrapped_scrolling;
		bool WrappedScrolling {
			get {
				if (!wrapped_scrolling.HasValue)
					wrapped_scrolling = prefs.Get<bool> ("WrappedScrolling", true);
				return wrapped_scrolling.Value;
			}
		}
				
		public event EventHandler DesksChanged;
		
		uint update_timer = 0;
		List<Desk> Desks = new List<Desk> ();
		Desk[,] DeskGrid = null;
		
		bool AreMultipleDesksAvailable {
			get {
				return Desks.Count () > 1;
			}
		}
		
		public override bool Square {
			get { return false; }
		}
		
		public WorkspaceSwitcherDockItem ()
		{
			HoverText = Catalog.GetString ("Switch Desks");
			Icon = "workspace-switcher";
			
			UpdateDesks ();
			UpdateItem ();
			
			Wnck.Screen.Default.ActiveWorkspaceChanged += HandleWnckScreenDefaultActiveWorkspaceChanged;;
			Wnck.Screen.Default.ViewportsChanged += HandleWnckScreenDefaultViewportsChanged;
			Wnck.Screen.Default.WorkspaceCreated += HandleWnckScreenDefaultWorkspaceCreated;
			Wnck.Screen.Default.WorkspaceDestroyed += HandleWnckScreenDefaultWorkspaceDestroyed;
		}

		public override string UniqueID ()
		{
			return "WorkspaceSwitcher";
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (!AreMultipleDesksAvailable || DeskGrid == null)
				return ClickAnimation.None;
			
			if (button == 1 && DeskGrid != null && DeskGrid.GetLength (0) > 0 && DeskGrid.GetLength (1) > 0) {
				int col = (int) (xPercent * DeskGrid.GetLength (0));
				int row = (int) (yPercent * DeskGrid.GetLength (1));
				if (DeskGrid [col,row] != null)
					DeskGrid [col,row].Activate ();
			}
			
			return ClickAnimation.None;
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (!AreMultipleDesksAvailable || DeskGrid == null)
				return;
			
			switch (direction) {
			case ScrollDirection.Down:
				if (((mod & ModifierType.ShiftMask) == ModifierType.ShiftMask) || DeskGrid.GetLength (1) == 1)
					SwitchDesk (Wnck.MotionDirection.Right);
				else
					SwitchDesk (Wnck.MotionDirection.Down);
				break;
			case ScrollDirection.Right:
				SwitchDesk (Wnck.MotionDirection.Right);
				break;
			case ScrollDirection.Up:
				if (((mod & ModifierType.ShiftMask) == ModifierType.ShiftMask) || DeskGrid.GetLength (1) == 1)
					SwitchDesk (Wnck.MotionDirection.Left);
				else
					SwitchDesk (Wnck.MotionDirection.Up);
				break;
			case ScrollDirection.Left:
				SwitchDesk (Wnck.MotionDirection.Left);
				break;
			}
		}

		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = new MenuList ();
			
			Desks.ForEach (d => {
				Desk desk = d;
				list[MenuListContainer.Actions].Add (new Docky.Menus.MenuItem (desk.Name, (desk.IsActive ? "desktop" : ""), (o, a) => desk.Activate ()));
			});

			return list;
		}		

		bool SwitchDesk (Wnck.MotionDirection direction)
		{
			Desk activedesk = Desks.Find (desk => desk.IsActive);
			Desk nextdesk = activedesk.GetNeighbor (direction);
			if (WrappedScrolling) {
				// Walk through the columns/rows and jump between [1,1] and [n,m]
				Desk tmp = activedesk.GetWrapNeighbor (direction);
				if (tmp != null && (nextdesk = tmp.GetNeighbor (Desk.AlternateMovingDirection (direction))) == null)
					if ((nextdesk = tmp.GetWrapNeighbor (Desk.AlternateMovingDirection (direction))) == null)
						nextdesk = tmp;
			}
			if (nextdesk != null) {
				nextdesk.Activate ();
				return true;
			}
			return false;
		}
		
		#region Update Switcher
		void UpdateDesks ()
		{
			lock (Desks)
			{
				DeskGrid = null;
				Desks.ForEach (desk => desk.Dispose());
				Desks.Clear ();
				
				string DeskNameFormatString;
				
				if (Wnck.Screen.Default.WorkspaceCount > 1)
					DeskNameFormatString = Catalog.GetString ("Desk") + " {0}";
				else
					DeskNameFormatString = Catalog.GetString ("Virtual Desk") + " {0}";
				
				foreach (Wnck.Workspace workspace in Wnck.Screen.Default.Workspaces) {
					if (workspace.IsVirtual) {
						int deskWidth = workspace.Screen.Width;
						int deskHeight = workspace.Screen.Height;
						int rows = workspace.Height / deskHeight;
						int columns = workspace.Width / deskWidth;
						
						for (int row = 0; row < rows; row++) {
							for (int col = 0; col < columns; col++) {
								int desknumber = (int) (columns * row + col + 1);
								Gdk.Rectangle area = new Gdk.Rectangle (col * deskWidth, row * deskHeight, deskWidth, deskHeight);
								
								Desk desk = new Desk (string.Format (DeskNameFormatString, desknumber), desknumber, area, workspace);
								desk.SetNeighbor (Wnck.MotionDirection.Down, Desks.Find (d => d.Area.X == area.X && (d.Area.Y - deskHeight == area.Y)));
								desk.SetNeighbor (Wnck.MotionDirection.Up, Desks.Find (d => d.Area.X == area.X && (d.Area.Y + deskHeight == area.Y)));
								desk.SetNeighbor (Wnck.MotionDirection.Right, Desks.Find (d => (d.Area.X - deskWidth == area.X) && d.Area.Y == area.Y));
								desk.SetNeighbor (Wnck.MotionDirection.Left, Desks.Find (d => (d.Area.X + deskWidth == area.X) && d.Area.Y == area.Y));
								Desks.Add (desk);
							}
						}
					} else {
						Desk desk = new Desk (workspace);
						desk.SetNeighbor (Wnck.MotionDirection.Down, Desks.Find (d => d.Parent == workspace.GetNeighbor (Wnck.MotionDirection.Down)));
						desk.SetNeighbor (Wnck.MotionDirection.Up, Desks.Find (d => d.Parent == workspace.GetNeighbor (Wnck.MotionDirection.Up)));
						desk.SetNeighbor (Wnck.MotionDirection.Right, Desks.Find (d => d.Parent == workspace.GetNeighbor (Wnck.MotionDirection.Right)));
						desk.SetNeighbor (Wnck.MotionDirection.Left, Desks.Find (d => d.Parent == workspace.GetNeighbor (Wnck.MotionDirection.Left)));
						Desks.Add (desk);
					}
				}

				Desk activedesk = Desks.Find (d => d.IsActive);
				if (activedesk != null)
					DeskGrid = activedesk.GetDeskGridLayout ();
			}
			
			if (DesksChanged != null)
				DesksChanged (new object (), EventArgs.Empty);
		}
		
		void UpdateItem ()
		{
			Desk activedesk = Desks.Find (desk => desk.IsActive);
			if (activedesk != null)
				HoverText = activedesk.Name;
			else
				HoverText = Catalog.GetString ("Switch Desks");
		}

		#endregion

		void Update ()
		{
			if (update_timer > 0)
				GLib.Source.Remove (update_timer);
			
			update_timer = GLib.Timeout.Add (250, delegate {
				update_timer = 0;

				UpdateDesks ();
				UpdateItem ();
				
				QueueRedraw ();

				return false;
			});
		}
		
		void HandleWnckScreenDefaultWorkspaceCreated (object o, WorkspaceCreatedArgs args)
		{
			Update ();
		}	
		
		void HandleWnckScreenDefaultWorkspaceDestroyed (object o, WorkspaceDestroyedArgs args)
		{
			Update ();
		}
		
		void HandleWnckScreenDefaultViewportsChanged (object sender, EventArgs e)
		{
			Update ();
		}
		
		void HandleWnckScreenDefaultActiveWorkspaceChanged (object o, ActiveWorkspaceChangedArgs args)
		{
			Desk activedesk = Desks.Find (desk => desk.IsActive);
			if (activedesk != null && activedesk.Parent != args.PreviousWorkspace)
				DeskGrid = activedesk.GetDeskGridLayout ();
			UpdateItem ();
			
			QueueRedraw ();
		}
		
		#region Drawing
		protected override DockySurface CreateIconBuffer (DockySurface model, int size)
		{
			if (DeskGrid == null) 
				return new DockySurface (size, size, model);
			
			int height = size, width = size;
			
			if (Owner != null && Owner.IsOnVerticalDock)
				height = Math.Min ((int) (size * 2.5), Math.Max (size, (int) (size * DeskGrid.GetLength (1) * 0.5)));
			else
				width = Math.Min ((int) (size * 2.5), Math.Max (size, (int) (size * DeskGrid.GetLength (0) * 0.5)));
			
			return new DockySurface (width, height, model);
		}		
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			if (DeskGrid == null)
				return;
			
			int cols = DeskGrid.GetLength (0);
			int rows = DeskGrid.GetLength (1);
			
			Gdk.Color gdkColor = this.Style.Backgrounds [(int) Gtk.StateType.Selected];
			Cairo.Color selection_color = new Cairo.Color ((double) gdkColor.Red / ushort.MaxValue,
											(double) gdkColor.Green / ushort.MaxValue,
											(double) gdkColor.Blue / ushort.MaxValue,
											0.5);
			Context cr = surface.Context;

			cr.AlphaPaint ();
			
			LinearGradient lg = new LinearGradient (0, 0, 0, surface.Height);
			lg.AddColorStop (0, new Cairo.Color (.35, .35, .35, .6));
			lg.AddColorStop (1, new Cairo.Color (.05, .05, .05, .7));
			
			for (int x = 0; x < cols; x++) {
				for (int y = 0; y < rows; y++) {
					if (DeskGrid[x,y] != null) {
						Cairo.Rectangle area = DeskAreaOnIcon (surface.Width, surface.Height, x, y);
						cr.Rectangle (area.X, area.Y, area.Width, area.Height);
						cr.Pattern = lg;
						cr.FillPreserve ();
						if (DeskGrid[x,y].IsActive) {
							cr.Color = selection_color;
							cr.Fill ();
							if (area.Width >= 16 && area.Height >= 16) {
								using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon ("desktop", (int) area.Width, (int) area.Height)) {
									Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, (int) area.X + (area.Width - pbuf.Width) / 2, (int) area.Y + (area.Height - pbuf.Height) / 2);
									cr.Paint ();
								}
							}
						}
						cr.NewPath ();
					}
				}
			}
			
			lg.Destroy ();
			
			for (int x = 0; x < cols; x++) {
				for (int y = 0; y < rows; y++) {
					if (DeskGrid[x,y] != null) {
						Cairo.Rectangle area = DeskAreaOnIcon (surface.Width, surface.Height, x, y);
						cr.Rectangle (area.X, area.Y, area.Width, area.Height);
					}
				}
			}
			
			lg = new LinearGradient (0, 0, 0, surface.Height);
			lg.AddColorStop (0, new Cairo.Color (.95, .95, .95, .7));
			lg.AddColorStop (1, new Cairo.Color (.5, .5, .5, .7));
			cr.Pattern = lg;
			cr.StrokePreserve ();
			lg.Destroy ();
		}
		
		Cairo.Rectangle DeskAreaOnIcon (int width, int height, int column, int row)
		{
			double BorderPercent = 0.05;
			double Border = height * BorderPercent;
			double boxWidth, boxHeight;
			Cairo.Rectangle area;
			
			boxWidth = (width - 2 * Border) / DeskGrid.GetLength (0);
			boxHeight = (height - 2 * Border) / DeskGrid.GetLength (1);
			area = new Cairo.Rectangle (boxWidth * column + Border, boxHeight * row + Border, boxWidth, boxHeight);
			
			return area;
		}
		#endregion
		
		#region IDisposable implementation
		public override void Dispose ()
		{
			Wnck.Screen.Default.ActiveWorkspaceChanged -= HandleWnckScreenDefaultActiveWorkspaceChanged;
			Wnck.Screen.Default.ViewportsChanged -= HandleWnckScreenDefaultViewportsChanged;
			Wnck.Screen.Default.WorkspaceCreated -= HandleWnckScreenDefaultWorkspaceCreated;
			Wnck.Screen.Default.WorkspaceDestroyed -= HandleWnckScreenDefaultWorkspaceDestroyed;
			
			DeskGrid = null;
			Desks.ForEach (desk => desk.Dispose ());
			Desks.Clear ();
			
			base.Dispose ();
		}

		#endregion
		
	}
}
