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

using Docky.Menus;

namespace Clippy
{
	class ClippyMenuItem : MenuItem
	{
		int i;
		ClippyItem item;

		public ClippyMenuItem (ClippyItem item, int i, string text) : base (text, Gtk.Stock.Cut)
		{
			this.item = item;
			this.i = i;
			
			Clicked += delegate {
				this.item.CopyEntry (this.i);
			};
		}
	}
}
