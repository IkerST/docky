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

using Mono.Unix;

using Docky.Items;
using Docky.Menus;
using Docky.Services;
using Docky.Services.Prefs;

namespace Timer
{
	public class TimerMainDockItem : IconDockItem
	{
		static IPreferences prefs = DockServices.Preferences.Get<TimerMainDockItem> ();
		
		public override string UniqueID ()
		{
			return "TimerMainItem";
		}
		
		static TimerMainDockItem ()
		{
			defaultTimer = (uint) prefs.Get<int> ("DefaultTimer", 60);
			autoStart = prefs.Get<bool> ("AutoStartTimers", false);
			autoDismiss = prefs.Get<bool> ("AutoDismissTimers", false);
			dismissOnClick = prefs.Get<bool> ("DismissOnClick", true);
		}
		
		public static string TimeRemaining (uint remaining)
		{
			if (remaining == 0)
				return "none";
			
			uint hours = (uint) (remaining / 3600);
			uint mins = (uint) ((remaining - 3600 * hours) / 60);
			uint secs = remaining - 3600 * hours - 60 * mins;
			String text = "";
			
			if (hours > 0)
				text += hours + " " + Catalog.GetPluralString ("hour", "hours", (int) hours);
			if (mins > 0) {
				if (text.Length > 0)
					text += " ";
				text += mins + " " + Catalog.GetPluralString ("minute", "minutes", (int) mins);
			}
			if (secs > 0) {
				if (text.Length > 0)
					text += " ";
				text += secs + " " + Catalog.GetPluralString ("second", "seconds", (int) secs);
			}
			return text;
		}
		
		static uint defaultTimer;
		public static uint DefaultTimer {
			get { return defaultTimer; }
			set {
				if (defaultTimer == value)
					return;
				defaultTimer = value;
				prefs.Set<int> ("DefaultTimer", (int) value);
			}
		}
		
		static bool autoStart;
		public static bool AutoStartTimers {
			get { return autoStart; }
			set {
				if (autoStart == value)
					return;
				autoStart = value;
				prefs.Set<bool> ("AutoStartTimers", value);
			}
		}
		
		static bool autoDismiss;
		public static bool AutoDismissTimers {
			get { return autoDismiss; }
			set {
				if (autoDismiss == value)
					return;
				autoDismiss = value;
				prefs.Set<bool> ("AutoDismissTimers", value);
			}
		}
		
		static bool dismissOnClick;
		public static bool DismissOnClick {
			get { return dismissOnClick; }
			set {
				if (dismissOnClick == value)
					return;
				dismissOnClick = value;
				prefs.Set<bool> ("DismissOnClick", value);
			}
		}
		
		TimerItemProvider provider;
		
		public TimerMainDockItem (TimerItemProvider provider)
		{
			this.provider = provider;
			Icon = "timer.svg@" + GetType ().Assembly.FullName;
			UpdateHoverText ();
		}

		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			uint amount = 1;
			
			if ((mod & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) {
				if ((mod & Gdk.ModifierType.Mod1Mask) == Gdk.ModifierType.Mod1Mask)
					amount = 3600;
				else
					amount = 60;
			}
			
			if (direction == Gdk.ScrollDirection.Up || direction == Gdk.ScrollDirection.Right)
				DefaultTimer += amount;
			else if (DefaultTimer > amount)
				DefaultTimer -= amount;
			
			UpdateHoverText ();
		}
		
		void UpdateHoverText ()
		{
			HoverText = string.Format (Catalog.GetString ("Click to create a timer for {0}."), TimeRemaining (DefaultTimer));
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				provider.NewTimer ();
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			
			list[MenuListContainer.Actions].Add (new IconMenuItem (Catalog.GetString ("Automatically _Start Timers"), AutoStartTimers ? "gtk-apply" : "gtk-remove", delegate {
				AutoStartTimers = !AutoStartTimers;
			}));
			
			list[MenuListContainer.Actions].Add (new IconMenuItem (Catalog.GetString ("Automatically _Dismiss Timers"), AutoDismissTimers ? "gtk-apply" : "gtk-remove", delegate {
				AutoDismissTimers = !AutoDismissTimers;
			}));
			
			return list;
		}
	}
}
