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

using Mono.Unix;

using Docky.Items;
using Docky.Menus;

using WindowManager.Wink;

namespace Desktop
{
	public class DesktopActionsProvider : AbstractDockItemProvider
	{
		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "DesktopActions";
			}
		}
		
		#endregion

		public DesktopActionsProvider ()
		{
			ScreenUtils.Initialize ();
			
			List<AbstractDockItem> items = new List<AbstractDockItem> ();
			items.Add (new ShowDesktopItem ());
			items.Add (new TileDesktopItem ());
			items.Add (new CascadeDesktopItem ());
			Items = items;
		}
		
		public override MenuList GetMenuItems (AbstractDockItem item)
		{
			MenuList list = base.GetMenuItems (item);
			
			list[MenuListContainer.Footer].Add (new MenuItem (Catalog.GetString ("_Undo"), "edit-undo", (o, a) => ScreenUtils.ActiveViewport.RestoreLayout (), !ScreenUtils.ActiveViewport.CanRestoreLayout ()));
			
			return list;
		}
	}
}
