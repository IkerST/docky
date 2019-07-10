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
//
// AddinView.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;
using Gtk;

using Docky.Widgets;

namespace Docky.Widgets
{
	[System.ComponentModel.ToolboxItem(true)]
	public class TileView : EventBox 
	{		
		private List<Tile> tiles = new List<Tile> ();
		private VBox box = new VBox ();
		
		const int DefaultIconSize = 64;
		
		int? icon_size;
		public int IconSize {
			get { 
				if (!icon_size.HasValue)
					icon_size = DefaultIconSize;
				return icon_size.Value;
			}
			set { 
				icon_size = value;
			}
		}
		
		protected int selected_index = -1;
		
		public TileView ()
		{
			CanFocus = true;
			VisibleWindow = false;
			
			box.Show ();
			Add (box);
		}
		
		public void Clear ()
		{
			tiles.ForEach (tile => {
				tile.Hide ();
				tile.ActiveChanged -= OnTileActiveChanged;
				tile.SizeAllocated -= OnTileSizeAllocated;
				tile.Owner = null;
				box.Remove (tile);
				tile.Dispose ();
				tile.Destroy ();
			});
			tiles.Clear ();
			
			foreach (Widget child in box.Children) {
				box.Remove (child);
				child.Dispose ();
				child.Destroy ();
			}
		}
		
		public virtual void AppendTile (AbstractTileObject tileObject)
		{			
			Tile tile = new Tile (tileObject, IconSize);
			tile.Owner = this;
			tile.ActiveChanged += OnTileActiveChanged;
			tile.SizeAllocated += OnTileSizeAllocated;
			tile.Show ();
			tiles.Add (tile);
			
			box.PackStart (tile, false, false, 0);
		}		

		public virtual void RemoveTile (AbstractTileObject tileObject)
		{
			Tile tile = tiles.First (t => t.OwnedObject == tileObject);
			
			if (tile == null)
				throw new ArgumentException ("Container does not own that AbstractTileObject.  It may have been removed already.", "tileObject");
	
			if (selected_index == tiles.IndexOf (tile))
				ClearSelection ();
	
			tile.Hide ();
			tile.ActiveChanged -= OnTileActiveChanged;
			tile.SizeAllocated -= OnTileSizeAllocated;
			tile.Owner = null;
			
			tiles.Remove (tile);
			box.Remove (tile);
			tile.Dispose ();
			tile.Destroy ();
		}
		
		public virtual void ClearSelection ()
		{
			if (selected_index >= 0 && selected_index < tiles.Count) {
				tiles[selected_index].Select (false);
			}
			
			selected_index = -1;
		}		
		
		public virtual AbstractTileObject CurrentTile ()
		{
			if (selected_index >= 0 && selected_index < tiles.Count)
				return tiles[selected_index].OwnedObject;
			return null;
		}
		
		private bool changing_styles = false;
		
		protected override void OnStyleSet (Style previous_style)
		{
			if (changing_styles) {
				return;
			}
			
			changing_styles = true;
			base.OnStyleSet (previous_style);
			Parent.ModifyBg (StateType.Normal, Style.Base (StateType.Normal));
			changing_styles = false;
		}
		
		public virtual void OnTileActiveChanged (object o, EventArgs args)
		{
			Tile tile = o as Tile;
			
			if (tile != null) {
				tile.OwnedObject.OnActiveChanged ();
			}
			
			foreach (Tile t in tiles) {
				t.UpdateState ();
			}
		}
		
		private void OnTileSizeAllocated (object o, SizeAllocatedArgs args)
		{
			ScrolledWindow scroll;
			
			if (Parent == null || (scroll = Parent.Parent as ScrolledWindow) == null) {
				return;
			}
			
			Tile tile = (Tile)o;
			
			if (tiles.IndexOf (tile) != selected_index) {
				return;
			}
			
			Gdk.Rectangle ta = ((Tile)o).Allocation;
			Gdk.Rectangle va = new Gdk.Rectangle (0, (int)scroll.Vadjustment.Value, 
			                                      Allocation.Width, Parent.Allocation.Height);
			
			if (!va.Contains (ta)) {
				double delta = 0.0;
				if (ta.Bottom > va.Bottom) {
					delta = ta.Bottom - va.Bottom;
				} else if (ta.Top < va.Top) {
					delta = ta.Top - va.Top;
				}
				scroll.Vadjustment.Value += delta;
				QueueDraw();
			}
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			HasFocus = true;
			
			ClearSelection ();
			
			for (int i = 0; i < tiles.Count; i++) {
				if (tiles[i].Allocation.Contains ((int)evnt.X, (int)evnt.Y)) {
					Select (i);
					break;
				}
			}
			
			QueueDraw ();
			
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			int index = selected_index;
			
			switch (evnt.Key) {
			case Gdk.Key.Up:
			case Gdk.Key.uparrow:
				index--;
				if (index < 0) {
					index = 0;
				}
				break;
			case Gdk.Key.Down:
			case Gdk.Key.downarrow:
				index++;
				if (index > tiles.Count - 1) {
					index = tiles.Count - 1;
				}
				break;
			}
			
			if (index != selected_index) {
				ClearSelection ();
				Select (index);
				return true;
			}
			
			return base.OnKeyPressEvent (evnt);
		}
		
		public void Select (int index)
		{
			if (index >= 0 && index < tiles.Count) {
				selected_index = index;
				tiles[index].Select (true);
			} else {
				ClearSelection ();
			}
			
			if (Parent != null && Parent.IsRealized) {
				Parent.GdkWindow.InvalidateRect (Parent.Allocation, true);
			}
			
			QueueResize ();
		}
		
		public override void Dispose ()
		{
			Clear ();
		
			if (box != null) {
				box.Dispose ();
				box.Destroy ();
			}
			
			base.Dispose ();
		}		
	}
}
