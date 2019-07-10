//  
//  Copyright (C) 2009 Jason Smith, Chris Szikszoy, Robert Dyer
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

using System.Linq;
using System.Collections.Generic;

using Mono.Unix;

using GLib;

using Docky.Items;
using Docky.Menus;
using Docky.Services;
using Docky.Services.Prefs;

namespace Trash
{
	public class TrashDockItem : IconDockItem
	{
		
		IPreferences trashPrefs = DockServices.Preferences.Get ("/apps/nautilus/preferences");
		bool ConfirmTrashDelete {
			get {
				return trashPrefs.Get<bool> ("confirm_trash", true);
			}
		}
		
		uint ItemsInTrash {
			get {
				return OwnedFile.QueryInfo<uint> ("trash::item-count");
			}
		}
		
		bool TrashFull {
			get {
				return ItemsInTrash > 0;
			}
		}
		
		public override string DropText {
			get { return Catalog.GetString ("Drop to move to Trash"); }
		}
		
		FileMonitor TrashMonitor { get; set; }
		public File OwnedFile { get; private set; }
		
		public TrashDockItem ()
		{
			OwnedFile = FileFactory.NewForUri ("trash://");
			Update ();
		
			TrashMonitor = OwnedFile.Monitor (FileMonitorFlags.None, null);
			TrashMonitor.Changed += HandleChanged;
		}

		void HandleChanged (object o, ChangedArgs args)
		{			
			DockServices.System.RunOnMainThread (delegate {
				Update ();
			});
		}

		void Update ()
		{
			// this can be a little costly, let's just call it once and store locally
			uint itemsInTrash = ItemsInTrash;
			if (itemsInTrash == 0)
				HoverText = Catalog.GetString ("No items in Trash");
			else
				HoverText = string.Format (Catalog.GetPluralString ("{0} item in Trash", "{0} items in Trash", (int) itemsInTrash), itemsInTrash);
			
			Icon = OwnedFile.Icon ();
		}
		
		public override string UniqueID ()
		{
			return "TrashCan";
		}
		
		protected override bool OnCanAcceptDrop (IEnumerable<string> uris)
		{
			bool accepted = false;
			
			foreach (string uri in uris)
				accepted |= CanReceiveItem (uri);

			return accepted;
		}
		
		protected override bool OnCanAcceptDrop (AbstractDockItem item)
		{
			if (item == this)
				return false;

			if (item.Owner == null)
				return false;

			return item.Owner.ItemCanBeRemoved (item);
		}
		
		protected override bool OnAcceptDrop (AbstractDockItem item)
		{
			if (!CanAcceptDrop (item))
				return false;

			item.Owner.RemoveItem (item);
			return true;
		}
		
		protected override bool OnAcceptDrop (IEnumerable<string> uris)
		{
			bool accepted = false;
			
			foreach (string uri in uris)
				accepted |= ReceiveItem (uri);

			return accepted;
		}
		
		bool CanReceiveItem (string uri)
		{
			// if the file doesn't exist for whatever reason, we bail
			return FileFactory.NewForUri (uri).Exists;
		}
		
		bool ReceiveItem (string uri)
		{
			bool trashed = FileFactory.NewForUri (uri).Trash (null);
			
			if (trashed) {
				Update ();
				OnPaintNeeded ();
			}
			else
				Log<TrashDockItem>.Error ("Could not move {0} to trash.'", uri);
			
			return trashed;
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				OpenTrash ();
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			// intentionally dont inherit
			MenuList list = new MenuList ();
			
			list.SetContainerTitle (MenuListContainer.CustomOne, Catalog.GetString ("Restore Files"));
			
			FileEnumerator enumerator = OwnedFile.EnumerateChildren ("standard::type,standard::name", FileQueryInfoFlags.NofollowSymlinks, null);
			List<File> files = new List<File> ();
			
			if (enumerator != null) {
				FileInfo info;
				
				while ((info = enumerator.NextFile ()) != null) {
					files.Add (OwnedFile.GetChild (info.Name));
					info.Dispose ();
				}
				
				if (info != null)
					info.Dispose ();
				enumerator.Close (null);
				enumerator.Dispose ();
			}
			
			/* FIXME
				- this code should work, but GetFiles() currently uses .net IO instead of GIO
				  when this starts working, the enumeration block above can go away too
			foreach (File _f in OwnedFile.GetFiles ().OrderByDescending (f => f.QueryInfo<string> ("trash::deletion-date")).Take (5)) {
			*/
			foreach (File _f in files.OrderByDescending (f => f.QueryInfo<string> ("trash::deletion-date")).Take (5)) {
				File f = _f;
				MenuItem item = new IconMenuItem (f.Basename, f.Icon (), (o, a) => RestoreFile (f));
				item.Mnemonic = null;
				list[MenuListContainer.CustomOne].Add (item);
			}
			
			list[MenuListContainer.CustomTwo].Add (
				new MenuItem (Catalog.GetString ("_Open Trash"), Icon, (o, a) => OpenTrash ()));
			list[MenuListContainer.CustomTwo].Add (
				new MenuItem (Catalog.GetString ("Empty _Trash"), "gtk-clear", (o, a) => EmptyTrash (), !TrashFull));
			
			return list;
		}
		
		void RestoreFile (File f)
		{
			File destFile = FileFactory.NewForPath (f.QueryInfo<string> ("trash::orig-path"));
			f.Move (destFile, FileCopyFlags.NofollowSymlinks | FileCopyFlags.AllMetadata | FileCopyFlags.NoFallbackForMove, null, null);
		}
		
		void OpenTrash ()
		{
			DockServices.System.Open (OwnedFile);
		}
		
		void EmptyTrash ()
		{
			if (ConfirmTrashDelete) {
				Gtk.MessageDialog md = new Gtk.MessageDialog (null, 
						  0,
						  Gtk.MessageType.Warning, 
						  Gtk.ButtonsType.None,
						  "<b><big>" + Catalog.GetString ("Empty all of the items from the trash?") + "</big></b>");
				md.Icon = DockServices.Drawing.LoadIcon ("docky", 22);
				md.SecondaryText = Catalog.GetString ("If you choose to empty the trash, all items in it\n" +
					"will be permanently lost. Please note that you\n" +
					"can also delete them separately.");
				md.Modal = false;
				
				md.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
				md.AddButton (Catalog.GetString ("Empty _Trash"), Gtk.ResponseType.Ok);
				md.DefaultResponse = Gtk.ResponseType.Ok;

				md.Response += (o, args) => {
					if (args.ResponseId != Gtk.ResponseType.Cancel)
						PerformEmptyTrash ();
					md.Destroy ();
				};
				
				md.Show ();
			} else {
				PerformEmptyTrash ();
			}
		}
		
		void PerformEmptyTrash ()
		{
			// disable events for a minute
			TrashMonitor.Changed -= HandleChanged;
			
			DockServices.System.RunOnMainThread (() => {
				OwnedFile.Delete_Recurse ();
			});
			
			// eneble events again
			TrashMonitor.Changed += HandleChanged;

			DockServices.System.RunOnMainThread (delegate {
				Update ();
				OnPaintNeeded ();
			});
		}
		
		public override void Dispose ()
		{
			TrashMonitor.Cancel ();
			TrashMonitor.Changed -= HandleChanged;
			TrashMonitor.Dispose ();
			
			base.Dispose ();
		}
	}
}
