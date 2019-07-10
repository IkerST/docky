//  
//  Copyright (C) 2009 Jason Smith
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Docky.Menus
{
	public enum MenuListContainer {
		Header,
		Actions,
		Windows,
		RelatedItems,
		CustomOne,
		CustomTwo,
		ProxiedItems,
		Footer,
	}
	
	public class MenuList
	{
		Dictionary<MenuListContainer, List<MenuItem>> list;
		Dictionary<MenuListContainer, string> titles;
		
		public IEnumerable<MenuItem> DisplayItems {
			get {
				bool separate = false;
				foreach (MenuListContainer container in list.Keys.OrderBy (key => (int) key)) {
					if (!list[container].Any ())
						continue;
					
					if (separate || (titles.ContainsKey (container) && list.Values.Count > 1)) {
						if (titles.ContainsKey (container))
							yield return new SeparatorMenuItem (titles[container]);
						else
							yield return new SeparatorMenuItem ();
					}
					foreach (MenuItem item in list[container])
						yield return item;
					separate = true;
				}
			}
		}
		
		public MenuList ()
		{
			list = new Dictionary<MenuListContainer, List<MenuItem>> ();
			titles = new Dictionary<MenuListContainer, string> ();
		}
		
		public void SetContainerTitle (MenuListContainer container, string title)
		{
			titles[container] = title;
		}
		
		public string GetContainerTitle (MenuListContainer container)
		{
			if (titles.ContainsKey (container))
				return titles[container];
			return null;
		}
		
		public List<MenuItem> this[MenuListContainer container]
		{
			get {
				if (!list.ContainsKey (container))
					list[container] = new List<MenuItem> ();
				return list[container];
			}
		}
		
		public void Remove (MenuItem item)
		{
			foreach (List<MenuItem> lst in list.Values)
				lst.Remove (item);
		}
		
		public bool Any ()
		{
			return list.Values.Any (sl => sl.Any ());
		}
		
		public int Count ()
		{
			return list.Values.Count ();
		}
		
		public MenuList Combine (MenuList other)
		{
			MenuList result = new MenuList ();
			
			foreach (KeyValuePair<MenuListContainer, List<MenuItem>> kvp in list)
			{
				result[kvp.Key].AddRange (kvp.Value);
			}
			
			foreach (KeyValuePair<MenuListContainer, List<MenuItem>> kvp in other.list)
			{
				result[kvp.Key].AddRange (kvp.Value);
			}
			
			// copy other first so any conflicts are resolved with this copy winning
			foreach (KeyValuePair<MenuListContainer, string> kvp in other.titles) {
				result.SetContainerTitle (kvp.Key, kvp.Value);
			}
			
			foreach (KeyValuePair<MenuListContainer, string> kvp in titles) {
				result.SetContainerTitle (kvp.Key, kvp.Value);
			}
			
			return result;
		}
	}
}
