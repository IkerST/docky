//  
//  Copyright (C) 2009 Chris Szikszoy, Robert Dyer
//  Copyright (C) 2010 Robert Dyer
//  Copyright (C) 2011 Robert Dyer
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
using System.Linq;
using System.Collections.Generic;

using Mono.Unix;

using Docky.Items;
using Docky.Menus;
using Docky.Services;
using Docky.Services.Prefs;

namespace RecentDocuments
{
	public class RecentFilesProvider : AbstractDockItemProvider
	{
		#region AbstractDockItemProvider implementation
		
		public override string Name {
			get {
				return "RecentFiles";
			}
		}
		
		#endregion
		
		int NumRecentDocs { get; set; }
		
		bool CanClear { get; set; }
		
		AbstractDockItem emptyItem = new EmptyRecentItem ();
		
		public RecentFilesProvider (IPreferences prefs)
		{
			NumRecentDocs = prefs.Get<int> ("NumRecentDocs", 7);
			
			RefreshRecentDocs ();
			Gtk.RecentManager.Default.Changed += delegate { RefreshRecentDocs (); };
		}
		
		public void RefreshRecentDocs ()
		{
			List<AbstractDockItem> items = new List<AbstractDockItem> ();
			
			GLib.List recent_items = new GLib.List (Gtk.RecentManager.Default.Items.Handle, typeof(Gtk.RecentInfo), false, false);
			IEnumerable<Gtk.RecentInfo> infos = recent_items.OfType<Gtk.RecentInfo> ();
			CanClear = recent_items.Count > 0;
			
			items.Add (emptyItem);
			items.AddRange (infos.Where (it => it.Exists ())
								 .OrderByDescending (f => f.Modified)
								 .Take (NumRecentDocs)
								 .Select (f => (AbstractDockItem)FileDockItem.NewFromUri (f.Uri)));
			
			foreach (Gtk.RecentInfo ri in infos)
				ri.Dispose ();
			recent_items.Dispose ();
			
			Items = items;
		}
		
		public override MenuList GetMenuItems (AbstractDockItem item)
		{
			MenuList list = base.GetMenuItems (item);
			
			list[MenuListContainer.ProxiedItems].RemoveAt (0);
			list[MenuListContainer.Footer].Add (new MenuItem (Catalog.GetString ("_Clear Recent Documents..."), "edit-clear", (o, a) => ClearRecent (), !CanClear));
			
			return list;
		}
		
		void ClearRecent ()
		{
			Gtk.MessageDialog md = new Gtk.MessageDialog (null, 
					  0,
					  Gtk.MessageType.Warning, 
					  Gtk.ButtonsType.None,
					  "<b><big>" + Catalog.GetString ("Clear the Recent Documents list?") + "</big></b>");
			
			md.Title = Catalog.GetString ("Clear Recent Documents");
			md.Icon = DockServices.Drawing.LoadIcon ("docky", 22);
			md.SecondaryText = Catalog.GetString ("If you clear the Recent Documents list, you clear the following:\n" +
				"\u2022 All items from the Places \u2192 Recent Documents menu item.\n" +
				"\u2022 All items from the recent documents list in all applications.");
			md.Modal = false;
			
			md.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
			md.AddButton (Gtk.Stock.Clear, Gtk.ResponseType.Ok);
			md.DefaultResponse = Gtk.ResponseType.Ok;

			md.Response += (o, args) => {
				if (args.ResponseId != Gtk.ResponseType.Cancel)
					Gtk.RecentManager.Default.PurgeItems ();
				md.Destroy ();
			};
			
			md.Show ();
		}
	}
}
