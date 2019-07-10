//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer, Chris Szikszoy
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

using Cairo;
using Gdk;
using Mono.Unix;

using Docky.Menus;
using Docky.Services;
using Docky.Services.Prefs;

namespace Docky.Items
{
	internal class DockyItem : ColoredIconDockItem, INonPersistedItem
	{
		static IPreferences prefs = DockServices.Preferences.Get <DockyItem> ();
		
		public bool Show {
			get {
				return prefs.Get<bool> ("ShowDockyItem", true);
			}
		}
		
		public bool ShowSettings {
			get {
				return prefs.Get<bool> ("ShowSettings", true);
			}
		}
		
		public bool ShowQuit {
			get {
				return prefs.Get<bool> ("ShowQuit", true);
			}
		}
		
		public int Hue {
			get {
				return prefs.Get<int> ("Hue", 0);
			}
		}
		
		public DockyItem ()
		{
			Indicator = ActivityIndicator.Single;
			HoverText = prefs.Get<string> ("HoverText", "Docky");
			Icon = "docky";
			HueShift = Hue;
		}
		
		protected string AboutIcon {
			get {
				return "[monochrome]about.svg@" + GetType ().Assembly.FullName;
			}
		}
		
		protected string CloseIcon {
			get {
				return "[monochrome]close.svg@" + GetType ().Assembly.FullName;
			}
		}
		
		protected string PrefsIcon {
			get {
				return "[monochrome]preferences.svg@" + GetType ().Assembly.FullName;
			}
		}
		
		protected string HelpIcon {
			get {
				return "[monochrome]help.svg@" + GetType ().Assembly.FullName;
			}
		}
		
		protected override void OnStyleSet (Gtk.Style style)
		{
			// if we set a hue manually, we don't want to reset the hue when the style changes
			if (Hue != 0)
				return;
			
			Gdk.Color gdkColor = Style.Backgrounds [(int) Gtk.StateType.Selected];
			int hue = (int) new Cairo.Color ((double) gdkColor.Red / ushort.MaxValue,
											(double) gdkColor.Green / ushort.MaxValue,
											(double) gdkColor.Blue / ushort.MaxValue,
											1.0).GetHue ();
			if (HueShift >= 0)
				HueShift = (((hue - 202) % 360) + 360) % 360;
		}
		
		public override string UniqueID ()
		{
			return "DockyItem";
		}

		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				string command = prefs.Get<string> ("DockyItemCommand", "");
				if (!string.IsNullOrEmpty (command))
					DockServices.System.Execute (command);
				else if (ShowSettings)
					ConfigurationWindow.Instance.Show ();
				else
					return ClickAnimation.None;
				
				return ClickAnimation.Bounce;
			}
			return ClickAnimation.None;
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = new MenuList ();
			if (ShowSettings)
				list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Settings"), PrefsIcon, (o, a) => ConfigurationWindow.Instance.Show ()));
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_About"), AboutIcon, (o, a) => Docky.ShowAbout ()));
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Help"), HelpIcon, (o, a) => DockServices.System.Open ("http://wiki.go-docky.com")));
			if (ShowQuit)
				list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Quit Docky"), CloseIcon, (o, a) => Docky.Quit ()));
			return list;
		}

	}
}
