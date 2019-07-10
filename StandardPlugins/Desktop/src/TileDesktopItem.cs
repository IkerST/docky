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

using Mono.Unix;

using Docky.Items;
using WindowManager.Wink;

namespace Desktop
{
	public class TileDesktopItem : IconDockItem
	{
		public override string UniqueID ()
		{
			return "TileDesktop";
		}
		
		public TileDesktopItem ()
		{
			HoverText = Catalog.GetString ("_Tile Desktop");
			Icon = "window-tile.svg@" + GetType ().Assembly.FullName;
		}

		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				ScreenUtils.ActiveViewport.Tile ();
				return ClickAnimation.Bounce;
			}
			return ClickAnimation.None;
		}
	}
}
