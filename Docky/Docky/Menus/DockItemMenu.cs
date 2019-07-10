//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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

using Cairo;
using Gdk;
using Gtk;

using Docky.CairoHelper;

namespace Docky.Menus
{
	public class DockItemMenu : DockMenu
	{
		public DockItemMenu (Gtk.Window parent) : base (parent)
		{
		}
		
		int GetSelected ()
		{
			int selected = 0;
			
			foreach (Gtk.Widget widget in (Container.Child as VBox).Children)
				if (widget is MenuItemWidget) {
					if ((widget as MenuItemWidget).Selected)
						return selected;
					selected++;
				}
			
			return -1;
		}
		
		MenuItemWidget GetSelectedItem ()
		{
			int selected = 0;
			
			foreach (Gtk.Widget widget in (Container.Child as VBox).Children)
				if (widget is MenuItemWidget) {
					if ((widget as MenuItemWidget).Selected)
						return (widget as MenuItemWidget);
					selected++;
				}
			
			return null;
		}
		
		void UpdateSelected (int selected)
		{
			int count = 0;
			
			foreach (Gtk.Widget widget in (Container.Child as VBox).Children)
				if (widget is MenuItemWidget) {
					(widget as MenuItemWidget).Selected = count == selected;
					count++;
				}
			
			QueueDraw ();
		}
		
		int NumberItems ()
		{
			int count = 0;
			
			foreach (Gtk.Widget widget in (Container.Child as VBox).Children)
				if (widget is MenuItemWidget)
					count++;
			
			return count;
		}
		
		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Up) {
				do {
					int selected = GetSelected () - 1;
					
					if (selected < 0)
						selected = NumberItems () - 1;
					
					UpdateSelected (selected);
				} while (GetSelectedItem ().item.Disabled);
			} else if (evnt.Key == Gdk.Key.Down) {
				do {
					int selected = GetSelected () + 1;
					
					if (selected > NumberItems () - 1)
						selected = 0;
					
					UpdateSelected (selected);
				} while (GetSelectedItem ().item.Disabled);
			} else if (evnt.Key == Gdk.Key.Return && GetSelectedItem () != null) {
				MenuItem item = (GetSelectedItem () as MenuItemWidget).item;
				if (!item.Disabled) {
					item.SendClick ();
					Hide ();
				}
			} else if (evnt.Key == Gdk.Key.Escape) {
				Hide ();
			} else {
				foreach (Gtk.Widget widget in (Container.Child as VBox).Children)
					if (widget is MenuItemWidget) {
						MenuItem item = (widget as MenuItemWidget).item;
						if (evnt.KeyValue == item.Mnemonic)
							if (!item.Disabled) {
								item.SendClick ();
								Hide ();
							}
					}
			}
			
			return base.OnKeyPressEvent (evnt);
		}
		
		void HandleSelectedChanged (object obj, EventArgs args)
		{
			MenuItemWidget item = obj as MenuItemWidget;
			bool selected = item.Selected;
			UpdateSelected (-1);
			item.Selected = selected;
		}
		
		public void SetItems (MenuList items)
		{
			if (Container.Child != null) {
				foreach (Gtk.Widget widget in (Container.Child as VBox).Children) {
					if (widget is MenuItemWidget)
						(widget as MenuItemWidget).SelectedChanged -= HandleSelectedChanged;
					widget.Dispose ();
					widget.Destroy ();
				}
				Container.Remove (Container.Child);
			}
			
			VBox vbox = new VBox ();
			Container.Add (vbox);
			
			Cairo.Color textColor;
			if (IsLight) {
				textColor = new Cairo.Color (0.1, 0.1, 0.1);
			} else {
				textColor = new Cairo.Color (1, 1, 1);
			}
			
			bool hasIcon = false;
			foreach (MenuItem item in items.DisplayItems) {
				if (item.ShowIcons) {
					hasIcon = true;
					break;
				}
			}
			
			bool first = true;
			int width = 1;
			foreach (MenuItem item in items.DisplayItems) {
				if (item is SeparatorMenuItem) {
					SeparatorWidget widget = new SeparatorWidget ((item as SeparatorMenuItem).Title);
					widget.TextColor = textColor;
					
					if (first)
						widget.DrawLine = false;
					
					first = false;
					vbox.PackStart (widget);
				} else {
					MenuItemWidget menuItem = new MenuItemWidget (item);
					menuItem.SelectedChanged += HandleSelectedChanged;
					menuItem.TextColor = textColor;
					menuItem.MenuShowingIcons = hasIcon;
					
					first = false;
					vbox.PackStart (menuItem, false, false, 0);
					
					width = Math.Max (width, menuItem.RequestedWidth);
				}
			}
			vbox.SetSizeRequest (width, -1);
			
			Container.ShowAll ();
		}
		
		public override void Dispose ()
		{
			if (Container != null && Container.Child != null && (Container.Child as VBox).Children != null) {
				foreach (Gtk.Widget widget in (Container.Child as VBox).Children) {
					if (widget is MenuItemWidget)
						(widget as MenuItemWidget).SelectedChanged -= HandleSelectedChanged;
					widget.Dispose ();
					widget.Destroy ();
				}
			}
			
			base.Dispose ();
		}
	}
}
