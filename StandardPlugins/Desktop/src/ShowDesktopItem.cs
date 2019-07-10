//  
//  Copyright (C) 2010 Rico Tzschichholz, Robert Dyer
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

using Mono.Unix;
using Wnck;

using Docky.Items;
using WindowManager.Wink;

namespace Desktop
{
	public class ShowDesktopItem : IconDockItem
	{
		public override string UniqueID ()
		{
			return "ShowDesktop";
		}
		
		public ShowDesktopItem ()
		{
			HoverText = Catalog.GetString ("_Show Desktop");
			Icon = "show-desktop.svg@" + GetType ().Assembly.FullName;
		}

		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				ScreenUtils.ActiveViewport.ShowDesktop ();
				return ClickAnimation.Bounce;
			}
			return ClickAnimation.None;
		}
	}
}
