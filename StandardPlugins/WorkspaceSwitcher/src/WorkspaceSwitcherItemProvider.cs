//  
//  Copyright (C) 2010 Rico Tzschichholz
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

using Docky.Items;

namespace WorkspaceSwitcher
{
	public class WorkspaceSwitcherItemProvider : AbstractDockItemProvider
	{
		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "WorkspaceSwitcher";
			}
		}
		
		#endregion

		WorkspaceSwitcherDockItem workspaceswitcher;

		public WorkspaceSwitcherItemProvider ()
		{
			workspaceswitcher = new WorkspaceSwitcherDockItem ();
			Items = workspaceswitcher.AsSingle<AbstractDockItem> ();
		}
	}
}
