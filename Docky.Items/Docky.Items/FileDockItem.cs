//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer, Chris Szikszoy
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

using GLib;
using Mono.Unix;
using Notifications;

using Docky.Menus;
using Docky.Services;

namespace Docky.Items
{
	public class FileDockItem : ColoredIconDockItem
	{
		public static FileDockItem NewFromUri (string uri)
		{
			return NewFromUri (uri, null, null);
		}
		
		public static FileDockItem NewFromUri (string uri, string force_hover_text, string backup_icon)
		{
			// FIXME: need to do something with this... .Exists will fail for non native files
			// but they are still valid file items (like an unmounted ftp://... file)
			// even File.QueryExists () will return false for valid files (ftp://) that aren't mounted.

			// for now we just attempt to figure out if it is a local file and check for its existance
			if (uri.IndexOf ("file://") != -1 || uri.IndexOf ("://") == -1)
				if (!GLib.FileFactory.NewForUri (uri).Exists)
					return null;

			return new FileDockItem (uri, force_hover_text, backup_icon);
		}
		
		const string ThumbnailPathKey = "thumbnail::path";
		const string FilesystemIDKey = "id::filesystem";
		const string FilesystemFreeKey = "filesystem::free";
		const string CustomIconKey = "metadata::custom-icon";
		const string EmblemsKey = "metadata::emblems";
		
		string uri;
		bool is_folder;
		string forced_hover_text;
		string backup_icon;
		
		public string ForcedHoverText {
			get { return forced_hover_text; }
			set {
				if (forced_hover_text == value)
					return;
				forced_hover_text = value;
				if (!string.IsNullOrEmpty (forced_hover_text))
					HoverText = forced_hover_text;
			}
		}
		
		public string Uri {
			get { return uri; }
		}
		
		public File OwnedFile { get; private set; }
		Action OnOwnedFileMount;
		
		public override string DropText {
			get { return string.Format (Catalog.GetString ("Drop to move to {0}"), HoverText); }
		}
		
		protected FileDockItem (string uri)
		{			
			this.uri = uri;
			OwnedFile = FileFactory.NewForUri (uri);
			
			OnOwnedFileMount = new Action (() => {
				UpdateInfo ();
				OnPaintNeeded ();
			});
			
			// update this file on successful mount
			OwnedFile.AddMountAction (OnOwnedFileMount);
			
			UpdateInfo ();
		}
		
		protected FileDockItem (string uri, string forced_hover, string backupIcon) : this (uri)
		{
			forced_hover_text = forced_hover;
			backup_icon = backupIcon;
			UpdateInfo ();
		}
		
		// this should be called after a successful mount of the file
		public void UpdateInfo ()
		{
			is_folder = OwnedFile.QueryFileType (0, null) == FileType.Directory;
			
			// only check the icon if it's mounted (ie: .Path != null)
			if (!string.IsNullOrEmpty (OwnedFile.Path)) {
				string customIconPath = OwnedFile.QueryInfo<string> (CustomIconKey);
				string thumbnailPath = OwnedFile.QueryInfo<string> (ThumbnailPathKey);
				string[] emblems = OwnedFile.QueryInfo<string[]> (EmblemsKey);
				
				// if the icon lives inside the folder (or one of its subdirs) then this
				// is actually a relative path... not a file uri.
				// we need to make this a file:// uri regardless.
				if (!string.IsNullOrEmpty (customIconPath)) {
					if (!customIconPath.StartsWith ("file://"))
						customIconPath = System.IO.Path.Combine (OwnedFile.StringUri (), customIconPath);
					Icon = customIconPath;
				} else if (!string.IsNullOrEmpty (thumbnailPath)) {
					Icon = thumbnailPath;
				} else {
					Icon = OwnedFile.Icon ();
				}
				
				// process the emblems
				if (emblems.Length != 0) {
					int [] emblemPositions = { 2, 1, 0, 3};
					int i=0;
					emblems.Reverse ()
						.Where (e => !string.IsNullOrEmpty (e))
						.Take (4)
						.ToList ()
						.ForEach (e => {
								AddEmblem (new IconEmblem (emblemPositions[i], string.Format ("emblem-{0}", e), 128));
								i++;
							});
				}
			} else if (!string.IsNullOrEmpty (backup_icon)) {
				Icon = backup_icon;
			} else {
				Icon = "";
			}

			if (string.IsNullOrEmpty (ForcedHoverText))
			    HoverText = OwnedFile.Basename;
			else
				HoverText = ForcedHoverText;
			
			OnPaintNeeded ();
		}
		
		public override string UniqueID ()
		{
			return uri;
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (Icon == null)
				return;
			base.OnScrolled (direction, mod);
		}

		protected override bool OnCanAcceptDrop (IEnumerable<string> uris)
		{
			bool can_write = false;
			
			try {
				if (!string.IsNullOrEmpty (OwnedFile.Path))
					can_write = OwnedFile.QueryInfo<bool> ("access::can-write");
			} catch { }
			
			// only accept the drop if it's a folder, and we can write to it
			return is_folder && can_write;
		}

		protected override bool OnAcceptDrop (IEnumerable<string> uris)
		{
			// verify there is enough space to move/copy everything
			long fileSize = 0;
			
			foreach (File file in uris.Select (uri => FileFactory.NewForUri (uri))) {
				if (!file.Exists)
					continue;
				fileSize += file.GetSize ();
			}
			
			if ((ulong) fileSize > OwnedFile.QueryInfo<ulong> (FilesystemFreeKey)) {
				Docky.Services.Log.Notify (Catalog.GetString ("Error performing drop action"), Gtk.Stock.DialogError, Catalog.GetString ("Not enough free space on destination."));
				return true;
			}
			
			DockServices.System.RunOnThread (()=> {
				// do the move/copy
				string ownedFSID = OwnedFile.QueryInfo<string> (FilesystemIDKey);
				Notification note = null;
				bool performing = true;
				
				foreach (File file in uris.Select (uri => FileFactory.NewForUri (uri))) {
					if (!file.Exists)
						continue;
					
					long cur = 0, tot = 10;
					
					if (note == null) {
						note = Docky.Services.Log.Notify ("", file.Icon (), string.Format ("{0}% " + Catalog.GetString ("Complete") + "...", cur / tot));
						
						GLib.Timeout.Add (250, () => {
							note.Body = string.Format ("{0:00.0}% ", ((float) Math.Min (cur, tot) / tot) * 100) + Catalog.GetString ("Complete") + "...";
							return performing;
						});
					}
					
					string nameAfterMove = file.NewFileName (OwnedFile);
					
					try {
						// check the filesystem IDs, if they are the same, we move, otherwise we copy.
						if (ownedFSID == file.QueryInfo<string> (FilesystemIDKey)) {
							note.Summary = Catalog.GetString ("Moving") + string.Format (" {0}...", file.Basename);
							file.Move (OwnedFile.GetChild (nameAfterMove), FileCopyFlags.NofollowSymlinks | FileCopyFlags.AllMetadata | FileCopyFlags.NoFallbackForMove, null, (current, total) => {
								cur = current;
								tot = total;
							});
						} else {
							note.Summary = Catalog.GetString ("Copying") + string.Format (" {0}...", file.Basename);
							file.Copy_Recurse (OwnedFile.GetChild (nameAfterMove), 0, (current, total) => {
								cur = current;
								tot = total;
							});
						}
					} catch (Exception e) {
						// until we use a new version of GTK# which supports getting the GLib.Error code
						// this is about the best we can do.
						Docky.Services.Log.Notify (Catalog.GetString ("Error performing drop action"), Gtk.Stock.DialogError, e.Message);
						Log<FileDockItem>.Error ("Error performing drop action: " + e.Message);
						Log<FileDockItem>.Debug ("Error moving file '" + file.Path + "' to '" + OwnedFile.GetChild (nameAfterMove) + "'");
						Log<FileDockItem>.Debug (e.StackTrace);
					}
					
					performing = false;
					note.Body = string.Format ("100% {0}.", Catalog.GetString ("Complete"));
				}			
			});
			return true;
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1 || button == 2) {
				Open ();
				return ClickAnimation.Bounce;
			}
			return base.OnClicked (button, mod, xPercent, yPercent);
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			list[MenuListContainer.Actions].Insert (0, new MenuItem (Catalog.GetString ("_Open"), "gtk-open", (o, a) => Open ()));
			list[MenuListContainer.Actions].Insert (1, new MenuItem (Catalog.GetString ("Open Containing _Folder"), "folder", (o, a) => OpenContainingFolder (), OwnedFile.Parent == null));
			return list;
		}
		
		protected void Open ()
		{
			DockServices.System.Open (OwnedFile);
		}
		
		protected void OpenContainingFolder ()
		{
			DockServices.System.Open (OwnedFile.Parent);
		}
		
		public override void Dispose ()
		{
			OwnedFile.RemoveAction (OnOwnedFileMount);
			OwnedFile = null;
			
			base.Dispose ();
		}
	}
}
