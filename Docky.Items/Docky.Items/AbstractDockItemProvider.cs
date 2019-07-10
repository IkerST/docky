//  
//  Copyright (C) 2009 Robert Dyer
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
using System.Collections.Generic;
using System.Linq;

using Docky.Menus;
using Docky.Services;

namespace Docky.Items
{
	public abstract class AbstractDockItemProvider
	{
		public event EventHandler<ItemsChangedArgs> ItemsChanged;
		
		public abstract string Name { get; }
		
		public virtual bool Separated {
			get { return false; }
		}
		
		public virtual bool AutoDisable {
			get { return !disposing; }
		}
		
		public bool IsOnVerticalDock { get; set; }

		IEnumerable<AbstractDockItem> items;
		public IEnumerable<AbstractDockItem> Items {
			get { return items; }
			protected set {
				IEnumerable<AbstractDockItem> added = value.Where (adi => !items.Contains (adi)).ToArray ();
				IEnumerable<AbstractDockItem> removed = items.Where (adi => !value.Contains (adi)).ToArray ();
				
				if (!added.Any () && !removed.Any ())
					return;
				
				int position = items.Any () ? items.Max (adi => adi.Position) + 1 : 0;
				foreach (AbstractDockItem item in added) {
					item.AddTime = DateTime.UtcNow;
					item.Position = position;
					position++;
				}
				
				items = value.ToArray ();
				foreach (AbstractDockItem item in items)
					item.Owner = this;
				
				OnItemsChanged (added, removed);
			}
		}

		protected AbstractDockItemProvider ()
		{
			items = Enumerable.Empty<AbstractDockItem> ();
			IsOnVerticalDock = false;
		}
		
		public bool CanAcceptDrop (string uri)
		{
			try {
				return OnCanAcceptDrop (uri);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItemProvider>.Debug (e.StackTrace);
			}
			return false;
		}
		
		protected virtual bool OnCanAcceptDrop (string uri)
		{
			return false;
		}
		
		public bool AcceptDrop (string uri, int position)
		{
			AbstractDockItem newItem = null;
			try {
				newItem = OnAcceptDrop (uri);
			} catch (Exception e) {
				Log<AbstractDockItem>.Error (e.Message);
				Log<AbstractDockItemProvider>.Debug (e.StackTrace);
			}
			
			if (newItem != null) {
				newItem.Position = position;
				foreach (AbstractDockItem item in Items.Where (adi => adi != newItem && adi.Position >= newItem.Position))
					item.Position++;
			}
			
			OnItemsChanged (null, null);
			
			return newItem != null;
		}
		
		protected virtual AbstractDockItem OnAcceptDrop (string uri)
		{
			return null;
		}
		
		public virtual bool ItemCanBeRemoved (AbstractDockItem item)
		{
			return true;
		}
		
		public virtual bool RemoveItem (AbstractDockItem item)
		{
			if (!ItemCanBeRemoved (item))
				return false;
			
			IEnumerable<AbstractDockItem> saved = Items.Where (adi => adi != item).ToArray ();
			Items = saved;
			item.Dispose ();
			return true;
		}
		
		public virtual MenuList GetMenuItems (AbstractDockItem item)
		{
			return item.GetMenuItems ();
		}
		
		protected void OnItemsChanged (IEnumerable<AbstractDockItem> added, IEnumerable<AbstractDockItem> removed)
		{
			if (ItemsChanged != null)
				ItemsChanged (this, new ItemsChangedArgs (added, removed));
		}
		
		public virtual void Registered ()
		{
		}
		
		public virtual void Unregistered ()
		{
		}
		
		bool disposing = false;
		
		public virtual void Dispose ()
		{
			disposing = true;
			
			foreach (AbstractDockItem adi in Items)
				adi.Dispose ();
		}
	}
}
