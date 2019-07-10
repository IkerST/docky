//  
//  Copyright (C) 2009 Robert Dyer
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
using System.IO;
using System.Linq;
using System.Text;

using Mono.Unix;
using Gtk;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace Clock
{
	public class ClockThemeSelector : Gtk.Dialog
	{
		public static ClockThemeSelector instance;
	
		TreeStore labelTreeStore = new TreeStore (typeof (string));
		TreeView labelTreeView = new TreeView ();
		
		ClockDockItem DockItem { get; set; }
		
		public ClockThemeSelector (ClockDockItem dockItem)
		{
			DockItem = dockItem;
			
			SkipTaskbarHint = true;
			TypeHint = Gdk.WindowTypeHint.Dialog;
			WindowPosition = Gtk.WindowPosition.Center;
			KeepAbove = true;
			Stick ();
			
			Title = Catalog.GetString ("Themes");
			IconName = Gtk.Stock.Preferences;
			
			AddButton (Stock.Close, ResponseType.Close);
			
			labelTreeView.Model = labelTreeStore;
			labelTreeView.HeadersVisible = false;
			labelTreeView.Selection.Changed += OnLabelSelectionChanged;
			labelTreeView.AppendColumn (Catalog.GetString ("Theme"), new CellRendererText (), "text", 0);
			
			ScrolledWindow win = new ScrolledWindow ();
			win.Add (labelTreeView);
			win.SetSizeRequest (200, 300);
			win.Show ();
			VBox.PackEnd (win);
			VBox.ShowAll ();

			UpdateThemeList ();
		}
		
		public void UpdateThemeList ()
		{
			List<string> themes = new List<string> ();
			
			if (Directory.Exists (DockItem.ThemeFolder))
				foreach (DirectoryInfo p in new DirectoryInfo (DockItem.ThemeFolder).GetDirectories ())
					themes.Add (p.Name);
			
			labelTreeStore.Clear ();
			
			themes.Sort ();
			
			int i = 0, selected = -1;
			foreach (string p in themes.Distinct ()) {
				if (p == DockItem.CurrentTheme)
					selected = i;
				labelTreeStore.AppendValues (p);
				i++;
			}
			
			labelTreeView.Selection.SelectPath (new TreePath ("" + selected));
			labelTreeView.ScrollToCell (new TreePath ("" + selected), null, false, 0, 0);
		}
		
		protected virtual void OnLabelSelectionChanged (object o, System.EventArgs args)
		{
			TreeIter iter;
			TreeModel model;
			
			if (((TreeSelection)o).GetSelected (out model, out iter))
				DockItem.SetTheme ((string) model.GetValue (iter, 0));
		}
		
		protected override void OnClose ()
		{
			Hide ();
		}
		
		protected override void OnResponse (ResponseType response_id)
		{
			Hide ();
		}
	}
}
