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

using org.freedesktop.DBus;
using DBus;

using Docky.Items;
using Docky.Services;

namespace Docky.DBus
{
	public class DBusManager
	{
		#region The Private/non-standard Bus
		
		const string BusName        = "org.gnome.Docky";
		const string DockyPath      = "/org/gnome/Docky";
		const string ItemsPath      = "/org/gnome/Docky/Items";

		DockyDBus docky;
		
		bool InitializePrivateBus (Bus bus)
		{
			if (bus.RequestName (BusName) != RequestNameReply.PrimaryOwner) {
				Log<DBusManager>.Error ("Bus Name '{0}' is already owned", BusName);
				return false;
			}
			
			docky = new DockyDBus ();
			docky.QuitCalled += HandleQuitCalled;
			docky.SettingsCalled += HandleSettingsCalled;
			docky.AboutCalled += HandleAboutCalled;
			
			ObjectPath dockyPath = new ObjectPath (DockyPath);
			bus.Register (dockyPath, docky);
			Log<DBusManager>.Debug ("DBus Registered: {0}", BusName);
			
			return true;
		}
		
		public void HandleAboutCalled ()
		{
			if (AboutCalled != null)
				AboutCalled ();
		}
		
		public void HandleSettingsCalled ()
		{
			if (SettingsCalled != null)
				SettingsCalled ();
		}
		
		public void HandleQuitCalled ()
		{
			if (QuitCalled != null)
				QuitCalled ();
		}
		
		#endregion
		
		#region The Shared/standard Bus
		
		const string DockManagerBusRoot              = "net.launchpad";
		const string DockManagerPathRoot             = "/net/launchpad";
		
		internal const string DockManagerBusName     = DockManagerBusRoot + ".DockManager";
		const string DockManagerPath                 = DockManagerPathRoot + "/DockManager";
		
		internal const string DockManagerItemBusName = DockManagerBusRoot + ".DockItem";
		const string DockManagerItemsPath            = DockManagerPath + "/Item";

		internal IEnumerable<AbstractDockItem> Items {
			get {
				return item_dict.Keys;
			}
		}
		
		Dictionary<AbstractDockItem, DockManagerDBusItem> item_dict;
		
		DockManagerDBus dock_manager;
		
		void InitializeSharedBus (Bus bus)
		{
			if (bus.RequestName (DockManagerBusName) != RequestNameReply.PrimaryOwner) {
				Log<DBusManager>.Warn ("Bus Name '{0}' is already owned", DockManagerBusName);
				return;
			}
			
			item_dict = new Dictionary<AbstractDockItem, DockManagerDBusItem> ();
			
			dock_manager = new DockManagerDBus ();
			
			ObjectPath dockPath = new ObjectPath (DockManagerPath);
			bus.Register (dockPath, dock_manager);
			Log<DBusManager>.Debug ("DBus Registered: {0}", DockManagerBusName);
		}
		
		internal string PathForItem (AbstractDockItem item)
		{
			return DockManagerItemsPath + Math.Abs (item.UniqueID ().GetHashCode ());
		}
		
		public void RegisterItem (AbstractDockItem item)
		{
			if (item_dict == null || item_dict.ContainsKey (item))
				return;
			
			DockManagerDBusItem dbusitem = new DockManagerDBusItem (item);
			item_dict[item] = dbusitem;
			
			ObjectPath path = new ObjectPath (PathForItem (item));
			Bus.Session.Register (path, dbusitem);
			
			dock_manager.OnItemAdded (path);
		}
		
		public void UnregisterItem (AbstractDockItem item)
		{
			if (item_dict == null || !item_dict.ContainsKey (item))
				return;
			
			item_dict[item].Dispose ();
			item_dict.Remove (item);
			
			ObjectPath path = new ObjectPath (PathForItem (item));
			
			try {
				Bus.Session.Unregister (path);
			} catch (Exception e) {
				Log<DBusManager>.Error ("Could not unregister: {0}", path);
				Log<DBusManager>.Debug (e.StackTrace);
				return;
			}
			
			dock_manager.OnItemRemoved (path);
		}
		
		#endregion
		
		public event Action QuitCalled;
		public event Action SettingsCalled;
		public event Action AboutCalled;
		
		public static DBusManager Default { get; protected set; }
		
		static DBusManager ()
		{
			Default = new DBusManager ();
		}
		
		private DBusManager () { }
		
		public bool Initialize (bool disableDockManager)
		{
			Bus bus = Bus.Session;
			
			if (!InitializePrivateBus (bus))
				return false;
			
			if (!disableDockManager) {
				InitializeSharedBus (bus);
				
				DockServices.Helpers.HelperStatusChanged += delegate(object sender, Docky.Services.Helpers.HelperStatusChangedEventArgs e) {
					// if a script has stopped running, trigger a refresh
					if (!e.IsRunning)
						ForceRefresh ();
				};
			}
			
			return true;
		}
		
		public void ForceRefresh ()
		{
			if (item_dict == null)
				return;
			
			foreach (DockManagerDBusItem item in item_dict.Values)
				item.TriggerConfirmation ();
		}
		
		public void Shutdown ()
		{
			if (docky != null) {
				docky.QuitCalled -= HandleQuitCalled;
				docky.SettingsCalled -= HandleSettingsCalled;
				docky.AboutCalled -= HandleAboutCalled;
				
				docky.Shutdown ();
			}
		}
	}
}
