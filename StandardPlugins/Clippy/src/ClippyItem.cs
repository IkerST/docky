//  
//  Copyright (C) 2010-2011 Robert Dyer
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

using Gdk;
using Gtk;
using Mono.Unix;

using Docky.Items;
using Docky.Menus;
using Docky.Services;
using Docky.Services.Prefs;

namespace Clippy
{
	public class ClippyItem : IconDockItem
	{
		static IPreferences prefs = DockServices.Preferences.Get<ClippyItem> ();

		bool trackMouseSelections = prefs.Get<bool> ("TrackMouseSelections", false);
		uint timerDelay = (uint)prefs.Get<int> ("TimerDelay", 500);
		uint maxEntries = (uint)prefs.Get<int> ("MaxEntries", 15);

		List<string> clips = new List<string> ();
		int curPos = 0;

		public override string UniqueID ()
		{
			return "Clippy";
		}

		Gtk.Clipboard clipboard;

		uint timer;
		
		public ClippyItem ()
		{
			Icon = "edit-cut";

			if (trackMouseSelections)
				clipboard = Gtk.Clipboard.Get (Gdk.Selection.Primary);
			else
				clipboard = Gtk.Clipboard.Get (Gdk.Selection.Clipboard);

			timer = GLib.Timeout.Add (timerDelay, CheckClipboard);
			Updated ();
		}

		bool CheckClipboard ()
		{
			clipboard.RequestText ((cb, text) => {
				if (string.IsNullOrEmpty (text))
					return;
				clips.Remove (text);
				clips.Add (text);
				while (clips.Count > maxEntries)
					clips.RemoveAt (0);
				curPos = clips.Count;
				Updated ();
			});
			return true;
		}

		string GetClipboardAt (int pos)
		{
			return clips [pos - 1].Replace ("\n", "");
		}

		void Updated ()
		{
			if (clips.Count == 0)
				HoverText = Catalog.GetString ("Clipboard is currently empty.");
			else if (curPos == 0 || curPos > clips.Count)
				HoverText = GetClipboardAt (clips.Count);
			else
				HoverText = GetClipboardAt (curPos);
		}

		public void CopyEntry (int pos)
		{
			if (pos < 1 || pos > clips.Count)
				return;

			clipboard.Text = clips[pos - 1];

			Updated ();
		}

		void CopyEntry ()
		{
			if (curPos == 0)
				CopyEntry (clips.Count);
			else
				CopyEntry (curPos);
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (direction == Gdk.ScrollDirection.Up)
				curPos++;
			else
				curPos--;

			if (curPos < 1)
				curPos = clips.Count;
			else if (curPos > clips.Count)
				curPos = 1;

			Updated ();
		}

		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1 && clips.Count > 0) {
				CopyEntry ();
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			List<Docky.Menus.MenuItem> items = new List<Docky.Menus.MenuItem> ();
			
			for (int i = clips.Count; i > 0; i--)
				items.Add (new ClippyMenuItem (this, i, clips [i - 1]));
			
			MenuList list = base.OnGetMenuItems ();
			
			if (items.Count > 0) {
				list[MenuListContainer.Actions].InsertRange (0, items);
				list[MenuListContainer.Footer].Add (new Docky.Menus.MenuItem ("_Clear", "gtk-clear", delegate {
					clipboard.Clear ();
					clips.Clear ();
					curPos = 0;
					Updated ();
				}));
			}
			
			return list;
		}
		
		public override void Dispose ()
		{
			if (timer > 0) {
				GLib.Source.Remove (timer);
				timer = 0;
			}

			base.Dispose ();
		}
	}
}
