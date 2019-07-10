//  
//  Copyright (C) 2009 Chris Szikszoy
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

using Gdk;

namespace Docky.Menus
{

	public class IconMenuItem : MenuItem
	{

		#region string icon constructors
		public IconMenuItem (string text, string icon, bool disabled) : base (text, icon, disabled)
		{
			// forcibly override gconf setting
			ShowIcons = true;
		}		
		
		public IconMenuItem (string text, string icon) : this (text, icon, false)
		{
		}
		
		public IconMenuItem (string text, string icon, EventHandler onClicked) : this(text, icon)
		{
			Clicked += onClicked;
		}
		
		public IconMenuItem (string text, string icon, EventHandler onClicked, bool disabled) : this(text, icon, disabled)
		{
			Clicked += onClicked;
		}
		#endregion
		
		#region pixbuf icon constructors
		public IconMenuItem (string text, Pixbuf icon, bool disabled) : this (text, "", disabled)
		{
			ForcePixbuf = icon;
		}		
		
		public IconMenuItem (string text, Pixbuf icon) : this (text, icon, false)
		{
		}
		
		public IconMenuItem (string text, Pixbuf icon, EventHandler onClicked) : this(text, icon)
		{
			Clicked += onClicked;
		}
		
		public IconMenuItem (string text, Pixbuf icon, EventHandler onClicked, bool disabled) : this(text, icon, disabled)
		{
			Clicked += onClicked;
		}
		#endregion
	}
}
