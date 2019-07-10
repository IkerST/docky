//  
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
using System.Collections.Generic;
using System.Linq;

using DBus;

using Docky.Items;

namespace Docky.DBus
{
	public class DockManagerDBus : IDockManagerDBus
	{
		#region IDockyDBus implementation
		public event ItemChangedHandler ItemAdded;
		
		public event ItemChangedHandler ItemRemoved;
		
		public string[] GetCapabilities ()
		{
			return new string[] {
				"dock-item-message",
				"dock-item-tooltip",
				"dock-item-badge",
				"dock-item-progress",
				//"dock-item-visible",
				"dock-item-icon-file",
				"dock-item-attention",
				"dock-item-waiting",
				
				"menu-item-with-label",
				"menu-item-with-uri",
				"menu-item-icon-name",
				"menu-item-icon-file",
				"menu-item-container-title",
				
				"x-docky-uses-menu-confirmation",
				"x-docky-message-has-icons",
				"x-docky-message-has-slots",
				"x-docky-get-item-pids",
			};
		}
		
		public ObjectPath[] GetItems ()
		{
			return DBusManager.Default.Items
				.Select (adi => new ObjectPath(DBusManager.Default.PathForItem (adi)))
				.ToArray ();
		}

		public ObjectPath[] GetItemsByName (string name)
		{
			return DBusManager.Default.Items
				.OfType<ApplicationDockItem> ()
				.Where (adi => adi.OwnedItem.GetString ("Name") == name)
				.Select (adi => new ObjectPath(DBusManager.Default.PathForItem (adi)))
				.ToArray ();
		}

		public ObjectPath[] GetItemsByDesktopFile (string path)
		{
			return DBusManager.Default.Items
				.OfType<ApplicationDockItem> ()
				.Where (adi => adi.OwnedItem.Path == path)
				.Select (adi => new ObjectPath(DBusManager.Default.PathForItem (adi)))
				.ToArray ();
		}
		
		public ObjectPath[] GetItemsByPID (uint pid)
		{
			return DBusManager.Default.Items
				.OfType<WnckDockItem> ()
				.Where (wdi => wdi.Windows.Any (w => (uint) w.Pid == pid))
				.Select (wdi => new ObjectPath(DBusManager.Default.PathForItem (wdi)))
				.ToArray ();
		}
		
		public ObjectPath GetItemByXid (uint xid)
		{
			return DBusManager.Default.Items
				.OfType<WnckDockItem> ()
				.Where (wdi => wdi.Windows.Any (w => (uint) w.Xid == xid))
				.Select (wdi => new ObjectPath(DBusManager.Default.PathForItem (wdi)))
				.FirstOrDefault ();
		}
		
		#endregion

		public DockManagerDBus ()
		{
		}
		
		public void OnItemAdded (ObjectPath path)
		{
			if (ItemAdded != null)
				ItemAdded (path);
		}
		
		public void OnItemRemoved (ObjectPath path)
		{
			if (ItemRemoved != null)
				ItemRemoved (path);
		}
	}
}
