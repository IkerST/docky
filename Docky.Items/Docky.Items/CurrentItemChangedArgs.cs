//  
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

namespace Docky.Items
{
	public class CurrentItemChangedArgs : EventArgs
	{
		public AbstractDockItem NewItem { get; protected set; }
		public AbstractDockItem OldItem { get; protected set; }

		public CurrentItemChangedArgs ()
		{
			NewItem = null;
			OldItem = null;
		}
		
		public CurrentItemChangedArgs (AbstractDockItem newItem, AbstractDockItem oldItem)
		{
			NewItem = newItem;
			OldItem = oldItem;
		}
	}
}
