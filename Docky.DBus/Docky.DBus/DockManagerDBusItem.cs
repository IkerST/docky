//  
//  Copyright (C) 2010 Robert Dyer, Chris Szikszoy
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
using System.Linq;

using GLib;

using org.freedesktop.DBus;
using DBus;

using Docky.Items;
using Docky.Menus;

namespace Docky.DBus
{
	public struct ItemTuple
	{
		public string Name;
		public string Icon;
		public string Title;
		
		public ItemTuple (string name, string icon, string title)
		{
			Name = name;
			Icon = icon;
			Title = title;
		}
	}
	
	public class DockManagerDBusItem : IDockManagerDBusItem, IDisposable
	{		
		uint timer;
		Dictionary<uint, RemoteMenuEntry> items = new Dictionary<uint, RemoteMenuEntry> ();
		Dictionary<uint, DateTime> update_time = new Dictionary<uint, DateTime> ();

		AbstractDockItem owner;
		
		string DesktopFile {
			get {
				if (owner is ApplicationDockItem)
					return (owner as ApplicationDockItem).OwnedItem.Path;
				return "";
			}
		}
		
		string Uri {
			get {
				if (owner is FileDockItem)
					return (owner as FileDockItem).Uri;
				return "";
			}
		}
		
		int[] PIDs {
			get {
				if (owner is WnckDockItem) {
					return (owner as WnckDockItem).ManagedWindows.Select (w => w.Pid).DefaultIfEmpty (-1).ToArray ();
				}
				return new int[] { -1 };
			}
		}
		
		public DockManagerDBusItem (AbstractDockItem item)
		{
			owner = item;
			
			timer = GLib.Timeout.Add (4 * 60 * 1000, delegate {
				TriggerConfirmation ();
				return true;
			});
		}
		
		public void TriggerConfirmation ()
		{
			if (MenuItemConfirmationNeeded != null)
				MenuItemConfirmationNeeded ();
			
			GLib.Timeout.Add (30 * 1000, delegate {
				foreach (uint i in update_time
					.Where (kvp => (DateTime.UtcNow - kvp.Value).TotalMinutes > 1)
					.Select (kvp => kvp.Key))
					RemoveMenuItem (i);
				
				return false;
			});
		}
		
		public event Action MenuItemConfirmationNeeded;
		
		public void ConfirmMenuItem (uint item)
		{
			update_time [item] = DateTime.UtcNow;
		}
		
		#region IDockManagerDBusItem implementation
		
		public object Get (string iface, string property)
		{
			if (iface != DBusManager.DockManagerItemBusName)
				return null;
			
			switch (property) {
			case "DesktopFile":
				return DesktopFile;
			case "Uri":
				return Uri;
			case "PIDs":
				return PIDs;
			}

			return null;
		}
		
		public void Set (string iface, string property, object val)
		{
		}
		
		public IDictionary<string, object> GetAll (string iface)
		{
			if (iface != DBusManager.DockManagerItemBusName)
				return null;
			
			Dictionary<string, object> items = new Dictionary<string, object> ();
			
			items ["DesktopFile"] = DesktopFile;
			items ["Uri"] = Uri;
			items ["PIDs"] = PIDs;
			
			return items;
		}
		
		#endregion
		
		#region IDockManagerDBusItem implementation
		
		public event MenuItemActivatedHandler MenuItemActivated;
		
		public uint AddMenuItem (IDictionary<string, object> dict)
		{
			string uri = "";
			if (dict.ContainsKey ("uri"))
				uri = (string) dict ["uri"];
			
			string title = "";
			if (dict.ContainsKey ("container-title"))
				title = (string) dict ["container-title"];
			
			RemoteMenuEntry rem;
			
			if (uri.Length > 0) {
				 rem = new RemoteFileMenuEntry (FileFactory.NewForUri (uri), title);
				
				AddToList (rem);
			} else {
				string label = "";
				if (dict.ContainsKey ("label"))
					label = (string) dict ["label"];
				
				string iconName = "";
				if (dict.ContainsKey ("icon-name"))
					iconName = (string) dict ["icon-name"];
				
				string iconFile = "";
				if (dict.ContainsKey ("icon-file"))
					iconFile = (string) dict ["icon-file"];

				if (iconFile.Length > 0)
					rem = new RemoteMenuEntry (label, iconFile, title);
				else
					rem = new RemoteMenuEntry (label, iconName, title);
				rem.Clicked += HandleActivated;
				
				AddToList (rem);
			}
			
			return rem.ID;
		}
		
		public void RemoveMenuItem (uint item)
		{
			if (items.ContainsKey (item)) {
				RemoteMenuEntry entry = items [item];
				entry.Clicked -= HandleActivated;
				
				items.Remove (item);
				
				owner.RemoteMenuItems.Remove (entry);
			}
		}
		
		public void UpdateDockItem (IDictionary<string, object> dict)
		{
			foreach (string key in dict.Keys)
			{
				if (key == "tooltip") {
					owner.SetRemoteText ((string) dict [key]);
				} else if (key == "badge") {
					owner.SetRemoteBadgeText ((string) dict [key]);
				} else if (key == "progress") {
					owner.Progress = (double) dict [key];
				} else if (key == "message") {
					owner.SetMessage ((string) dict [key]);
				} else if (key == "icon-file") {
					if (owner is IconDockItem)
						(owner as IconDockItem).SetRemoteIcon ((string) dict [key]);
				} else if (key == "attention") {
					if ((bool) dict [key])
						owner.State |= ItemState.Urgent;
					else
						owner.State &= ~ItemState.Urgent;
				} else if (key == "waiting") {
					if ((bool) dict [key])
						owner.State |= ItemState.Wait;
					else
						owner.State &= ~ItemState.Wait;
				}
			}
		}
		
		#endregion
		
		private void AddToList (RemoteMenuEntry entry)
		{
			items [entry.ID] = entry;
			update_time [entry.ID] = DateTime.UtcNow;
						
			//TODO Insert items into list... this is stupid but whatever fix later
			foreach (MenuItem item in items.Values)
				owner.RemoteMenuItems.Remove (item);
			
			MenuListContainer _container = MenuListContainer.Footer + 1;
			var groupedItems = items.Values
				.GroupBy (rmi => rmi.Title)
				.OrderBy (g => g.Key);
			
			foreach (var itemGroup in groupedItems) {
				MenuListContainer container;
				
				switch (itemGroup.Key.ToLower ()) {
				case "actions":
					container = MenuListContainer.Actions;
					break;
				case "relateditems":
					container = MenuListContainer.RelatedItems;
					break;
				case "windows":
					container = MenuListContainer.Windows;
					break;
				case "header":
					container = MenuListContainer.Header;
					break;
				case "footer":
					container = MenuListContainer.Footer;
					break;
				default:
					container = _container;
					owner.RemoteMenuItems.SetContainerTitle (container, itemGroup.Key);
					break;
				}
				
				foreach (RemoteMenuEntry item in itemGroup.OrderBy (i => i.ID)) {
					owner.RemoteMenuItems [container].Add (item);
				}
				_container++;
			}
		}
		
		public ItemTuple GetItem (uint item)
		{
			if (!items.ContainsKey (item))
				return new ItemTuple ("", "", "");
			
			RemoteMenuEntry entry = items [item];
			return new ItemTuple (entry.Text, entry.Icon, entry.Title);
		}
			
		void HandleActivated (object sender, EventArgs args)
		{
			if (!(sender is RemoteMenuEntry))
				return;
			
			if (MenuItemActivated != null)
				MenuItemActivated ((sender as RemoteMenuEntry).ID);
		}
		
		#region IDisposable implementation
		
		public void Dispose ()
		{
			if (timer > 0) {
				GLib.Source.Remove (timer);
				timer = 0;
			}
			
			update_time.Clear ();
			
			foreach (RemoteMenuEntry m in items.Values) {
				m.Clicked -= HandleActivated;
				m.Dispose ();
			}
			items.Clear ();
			
			owner = null;
		}
		
		#endregion
	}
}
