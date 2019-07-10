//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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

using Docky.Services;
using Docky.Items;
using Docky.Menus;

namespace Bookmarks
{
	public class BookmarksItemProvider : AbstractDockItemProvider
	{
		class NonRemovableItem : FileDockItem
		{
			public NonRemovableItem (string uri, string name, string icon) : base (uri)
			{
				if (!string.IsNullOrEmpty (icon))
				    Icon = icon;
				    
				if (string.IsNullOrEmpty (name))
					HoverText = OwnedFile.Basename;
				else 
					HoverText = name;
			}
			
			protected override bool OnCanAcceptDrop (AbstractDockItem item)
			{
				return false;
			}
			
			protected override bool OnCanAcceptDrop (IEnumerable<string> uris)
			{
				return false;
			}
		}
		
		NonRemovableItem computer;
		FileDockItem home;
		File bookmarks_file = null;
		List<AbstractDockItem> items;
		
		IEnumerable<AbstractDockItem> InnerItems {
			get {
				yield return computer;
				yield return home;
				foreach (AbstractDockItem item in items)
					yield return item;
			}
		}
		
		File BookmarksFile {
			get {
				if (bookmarks_file == null)
					bookmarks_file = FileFactory.NewForPath (Environment.GetFolderPath (Environment.SpecialFolder.Personal)).GetChild (".gtk-bookmarks");
				return bookmarks_file;
			}
		}
		
		FileMonitor watcher;
		
		public BookmarksItemProvider ()
		{
			items = new List<AbstractDockItem> ();

			computer = new NonRemovableItem ("computer://", Catalog.GetString ("Computer"), "computer");
			home = FileDockItem.NewFromUri (string.Format ("file://{0}",
			    Environment.GetFolderPath (Environment.SpecialFolder.Personal)));
		
			UpdateItems ();
			
			watcher = FileMonitor.File (BookmarksFile, FileMonitorFlags.None, null);
			
			watcher.Changed += WatcherChanged;
		}

		void WatcherChanged (object o, ChangedArgs args)
		{
			if (args.EventType == FileMonitorEvent.ChangesDoneHint)
				DockServices.System.RunOnMainThread ( delegate {
					UpdateItems ();
				});
		}
		
		void UpdateItems ()
		{
			List<AbstractDockItem> old = items;
			items = new List<AbstractDockItem> ();
			
			Log<BookmarksItemProvider>.Debug ("Updating bookmarks.");
			
			if (!BookmarksFile.QueryExists (null)) {
				Log<BookmarksItemProvider>.Error ("File '{0} does not exist.", BookmarksFile);
				return;
			}
			
			using (DataInputStream stream = new DataInputStream (BookmarksFile.Read (null))) {
				ulong length;
				string line, name, uri;
				while ((line = stream.ReadLine (out length, null)) != null) {
					uri = line.Split (' ').First ();
					File bookmark = FileFactory.NewForUri (uri);
					name = line.Substring (uri.Length).Trim ();
					if (old.Cast<FileDockItem> ().Any (fdi => fdi.Uri == uri)) {
						FileDockItem item = old.Cast<FileDockItem> ().First (fdi => fdi.Uri == uri);
						old.Remove (item);
						items.Add (item);
						item.ForcedHoverText = name;
					} else if (bookmark.StringUri ().StartsWith ("file://") && !bookmark.Exists) {
						Log<BookmarksItemProvider>.Warn ("Bookmark path '{0}' does not exist, please fix the bookmarks file", bookmark.StringUri ());
						continue;
					} else {
						FileDockItem item = FileDockItem.NewFromUri (bookmark.StringUri (), name, "folder");
						if (item != null)
							items.Add (item);
					}
				}
			}
			
			Items = InnerItems;
			
			foreach (AbstractDockItem item in old)
				item.Dispose ();
		}

		#region IDockItemProvider implementation
		
		public override string Name {
			get { return "Bookmarks"; }
		}		
		
		protected override bool OnCanAcceptDrop (string uri)
		{
			return System.IO.Directory.Exists (new Uri (uri).LocalPath);
		}

		protected override AbstractDockItem OnAcceptDrop (string uri)
		{
			File tempFile = FileFactory.NewForPath (System.IO.Path.GetTempFileName ());
			FileDockItem bookmark = FileDockItem.NewFromUri (uri);
			
			// make sure the bookmarked location actually exists
			if (!bookmark.OwnedFile.Exists)
				return null;
			
			using (DataInputStream reader = new DataInputStream (BookmarksFile.Read (null))) {
				using (DataOutputStream writer = new DataOutputStream (tempFile.AppendTo (FileCreateFlags.None, null))) {
					string line;
					ulong length;
					while ((line = reader.ReadLine (out length, null)) != null)
						writer.PutString (string.Format ("{0}{1}", line, reader.NewLineString ()), null);
					
					writer.PutString (string.Format ("{0}{1}", bookmark.Uri, reader.NewLineString ()), null);
				}
			}
			
			items.Add (bookmark);
			Items = InnerItems;
			
			if (tempFile.Exists)
				tempFile.Move (BookmarksFile, FileCopyFlags.Overwrite, null, null);
			
			return bookmark;
		}
		
		public override bool ItemCanBeRemoved (AbstractDockItem item)
		{
			return !(item is NonRemovableItem);
		}
		
		public override bool RemoveItem (AbstractDockItem item)
		{
			if (!ItemCanBeRemoved (item))
				return false;
			
			FileDockItem bookmark = item as FileDockItem;
			
			if (!bookmark.OwnedFile.Exists)
				return false;
			
			File tempFile = FileFactory.NewForPath (System.IO.Path.GetTempFileName ());
			
			using (DataInputStream reader = new DataInputStream (BookmarksFile.Read (null))) {
				using (DataOutputStream writer = new DataOutputStream (tempFile.AppendTo (FileCreateFlags.None, null))) {
					string line;
					ulong length;
					while ((line = reader.ReadLine (out length, null)) != null) {
						if (line.Split (' ')[0] != bookmark.Uri) {
							writer.PutString (string.Format ("{0}{1}", line, reader.NewLineString ()), null);
						} else {
							items.Remove (bookmark);
							Items = InnerItems;
							Log<BookmarksItemProvider>.Debug ("Removing '{0}'", bookmark.HoverText);
						}
					}
				}
			}
			
			if (tempFile.Exists)
				tempFile.Move (BookmarksFile, FileCopyFlags.Overwrite, null, null);

			return true;
		}
		
		public override bool Separated {
			get { return true; }
		}
		
		#endregion
		
		public override void Dispose ()
		{
			watcher.Cancel ();
			watcher.Changed -= WatcherChanged;
			watcher.Dispose ();
			base.Dispose ();
		}
	}
}
