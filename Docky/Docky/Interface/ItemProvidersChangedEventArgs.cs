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
using System.Linq;

using Docky.Items;

namespace Docky.Interface
{
	public class ItemProvidersChangedEventArgs : System.EventArgs
	{
		public IEnumerable<AbstractDockItemProvider> AddedProviders { get; set; }
		public IEnumerable<AbstractDockItemProvider> RemovedProviders { get; set; }
		
		public ItemProvidersChangedEventArgs (IEnumerable<AbstractDockItemProvider> addedProviders, IEnumerable<AbstractDockItemProvider> removedProviders)
		{
			AddedProviders = addedProviders == null ? Enumerable.Empty<AbstractDockItemProvider> () : addedProviders;
			RemovedProviders = removedProviders == null ? Enumerable.Empty<AbstractDockItemProvider> () : removedProviders;
		}
	}
}
