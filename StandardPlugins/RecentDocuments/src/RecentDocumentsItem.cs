//  
//  Copyright (C) 2009 Chris Szikszoy, Robert Dyer
//  Copyright (C) 2010 Robert Dyer
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

using GLib;
using Mono.Unix;

using Docky.Items;
using Docky.Menus;
using Docky.Services;
using Docky.Services.Prefs;

namespace RecentDocuments
{
	public class RecentDocumentsItem : ProxyDockItem
	{
		#region AbstractDockItem implementation
		
		public override string UniqueID ()
		{
			return "RecentDocuments";
		}
		
		#endregion
		
		static IPreferences prefs = DockServices.Preferences.Get <RecentDocumentsItem> ();
		
		bool AlwaysShowRecent { get; set; }
		
		FileMonitor watcher;
		
		public RecentDocumentsItem () : base (new RecentFilesProvider (prefs), prefs)
		{
			AlwaysShowRecent = prefs.Get<bool> ("AlwaysShowRecent", false);
			
			Provider.ItemsChanged += HandleItemsChanged;
			CurrentItemChanged += HandleCurrentItemChanged;
		}
		
		void HandleItemsChanged (object o, ItemsChangedArgs args)
		{
			if (AlwaysShowRecent)
				SetItem (Provider.Items.First ());
		}
		
		void HandleCurrentItemChanged (object o, EventArgs args)
		{
			StopWatcher ();
			
			if (o is FileDockItem) {
				watcher = FileMonitor.File ((o as FileDockItem).OwnedFile, FileMonitorFlags.None, null);
				watcher.Changed += WatcherChanged;
			}
		}
		
		void StopWatcher ()
		{
			if (watcher != null) {
				watcher.Cancel ();
				watcher.Changed -= WatcherChanged;
				watcher.Dispose ();
				watcher = null;
			}
		}
		
		void WatcherChanged (object o, ChangedArgs args)
		{
			((RecentFilesProvider) Provider).RefreshRecentDocs ();
		}
		
		public override void Dispose ()
		{
			Provider.ItemsChanged -= HandleItemsChanged;
			CurrentItemChanged -= HandleCurrentItemChanged;
			
			StopWatcher ();
			
			base.Dispose ();
		}
	}
}
