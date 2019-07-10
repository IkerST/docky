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
using IO = System.IO;
using System.Collections.Generic;

using GLib;
using Gtk;

namespace Docky.Services
{

	public static class GLibMountExtension
	{
		
		const string EmptyTrashTitle = "Do you want to empty the trash before you unmount?";
		const string EmptyTrashMessage = "In order to regain the free space on this volume " + 
			"the trash must be emptied. All trashed items on the volume will be permanently lost.";
		
		// as of GLib 2.22, Mount.UnMount and Mount.Eject are deprecated.  These should be used instead.
		public static void UnmountWithOperation (this GLib.Mount m, MountUnmountFlags flags, GLib.MountOperation operation, 
			Cancellable cancellable, AsyncReadyCallback callback)
		{
			m.MaybeEmptyTrashWithAction (() => {
				NativeInterop.UnmountWithOperation (m, flags, operation, cancellable, callback);
			});
		}
		
		public static void EjectWithOperation (this GLib.Mount m, MountUnmountFlags flags, GLib.MountOperation operation, 
			Cancellable cancellable, AsyncReadyCallback callback)
		{
			m.MaybeEmptyTrashWithAction (() => {
				NativeInterop.EjectWithOperation (m, flags, operation, cancellable, callback);
			});
		}
		
		public static bool EjectWithOperationFinish (this GLib.Mount m, AsyncResult result)
		{
			return NativeInterop.EjectWithOperationFinish (m, result);
		}
		
		public static bool UnmountWithOperationFinish (this GLib.Mount m, AsyncResult result)
		{
			return NativeInterop.UnmountWithOperation (m, result);
		}
		
		static void MaybeEmptyTrashWithAction (this Mount m, System.Action act)
		{
			bool perform = true;
			
			if (m.TrashHasFiles ()) {
				MessageDialog dialog;
				ResponseType response = m.PromptEmptyTrash (out dialog);
				if (response == ResponseType.Accept) {
					foreach (File dir in m.TrashDirs ()) {
						IO.DirectoryInfo info = new IO.DirectoryInfo (dir.Path);
						info.Delete (true);
					}
				} else if (response == ResponseType.Cancel) {
					perform = false;
				}
				dialog.Hide ();
				dialog.Destroy ();
			}
			if (perform)
				act.Invoke ();
		}
		
		public static bool TrashHasFiles (this Mount m)
		{
			foreach (File f in m.TrashDirs ()) {
				if (f.QueryExists (null) && f.DirectoryHasFiles ())
					return true;
			}
			return false;
		}
		
		static IEnumerable<File> TrashDirs (this Mount m)
		{
			File root = m.Root;
			if (root == null)
				yield break;
			
			if (root.IsNative) {
				IO.DirectoryInfo rootInfo = new IO.DirectoryInfo (root.Path);
				foreach (IO.DirectoryInfo d in rootInfo.GetDirectories (".Trash*", IO.SearchOption.TopDirectoryOnly)) {
					yield return FileFactory.NewForPath (root.GetChild (d.Name).GetChild ("files").Path);
					yield return FileFactory.NewForPath (root.GetChild (d.Name).GetChild ("info").Path);
				}
			}
		}
		
		static ResponseType PromptEmptyTrash (this Mount m, out MessageDialog dialog)
		{
			dialog = new Gtk.MessageDialog (null, DialogFlags.Modal, MessageType.Question, 
				ButtonsType.None, EmptyTrashMessage);
			dialog.Title = EmptyTrashTitle;
			dialog.AddButton ("Do _not Empty Trash", ResponseType.Reject);
			dialog.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
			dialog.AddButton ("Empty _Trash", ResponseType.Accept);
			dialog.DefaultResponse = ResponseType.Accept;
			dialog.SkipTaskbarHint = true;

			return (ResponseType) dialog.Run ();
		}
	}
}
