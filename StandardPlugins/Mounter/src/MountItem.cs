//  
//  Copyright (C) 2009 Chris Szikszoy
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
using System.Text.RegularExpressions;

using GLib;
using Mono.Unix;

using Docky.Items;
using Docky.Menus;
using Docky.Widgets;
using Docky.Services;

namespace Mounter
{
	public class MountItem : FileDockItem
	{
		#region IconDockItem implementation
		
		public override string UniqueID ()
		{
			return "MountItem#" + (Mnt.Root != null ? Mnt.Root.Path : Mnt.Handle.ToString ());
		}
		
		#endregion
		
		public Mount Mnt { get; private set; }
		
		public MountItem (Mount mount) : base (mount.Root.StringUri ())
		{
			Mnt = mount;
			
			SetIconFromGIcon (mount.Icon);
			
			HoverText = Mnt.Name;
			
			Mnt.Changed += HandleMountChanged;
		}
		
		void HandleMountChanged (object o, EventArgs args)
		{
			SetIconFromGIcon (Mnt.Icon);
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				Open ();
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		public bool UnMount ()
		{
			bool successful = false;
			if (Mnt.CanEject ())
				Mnt.EjectWithOperation (MountUnmountFlags.Force, new Gtk.MountOperation (null), null, (s, result) =>
				{
					try {
						if (!Mnt.EjectWithOperationFinish (result))
							Log<MountItem>.Error ("Failed to eject {0}", Mnt.Name);
						else
							successful = true;
					} catch (Exception e) {
						Log<MountItem>.Error ("An error when ejecting {0} was encountered: {1}", Mnt.Name, e.Message);
						Log<MountItem>.Debug (e.StackTrace);
					}
				});
			else if (Mnt.CanUnmount)
				Mnt.UnmountWithOperation (MountUnmountFlags.Force, new Gtk.MountOperation (null), null, (s, result) =>
				{
					try {
						if (!Mnt.UnmountWithOperationFinish (result))
							Log<MountItem>.Error ("Failed to unmount {0}", Mnt.Name);
						else
							successful = true;
					} catch (Exception e) {
						Log<MountItem>.Error ("An error when unmounting {0} was encountered: {1}", Mnt.Name, e.Message);
						Log<MountItem>.Debug (e.StackTrace);
					}
				});
			return successful;
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = new MenuList ();
			
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("_Open"), Icon, (o, a) => Open ()));
			
			if (Mnt.CanEject () || Mnt.CanUnmount)
				list[MenuListContainer.Actions].Add (new MenuItem (Mnt.CanEject () ? Catalog.GetString ("_Eject") : Catalog.GetString ("_Unmount"), "media-eject", (o, a) => UnMount ()));
			
			return list;
		}
		
		public override void Dispose ()
		{
			Mnt.Changed -= HandleMountChanged;
			
			base.Dispose ();
		}
	}
}
