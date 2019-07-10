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
using System.Linq;
using System.Collections.Generic;

using GLib;

namespace Docky.Services
{
	
	public class FileEqualityComparer : IEqualityComparer<GLib.File>
	{
		public bool Equals (File file, File other)
		{
			return file.Path == other.Path;
		}

		public int GetHashCode (File file)
		{
			return file.Path.GetHashCode ();
		}
	}

	public static class GLibFileExtension
	{
		static Dictionary<File, List<Action>> MountActions;

		static GLibFileExtension ()
		{
			// setup the mount actions dict
			MountActions = new Dictionary<File, List<Action>> ();
		}
		
		public static string StringUri (this GLib.File file)
		{
			return NativeInterop.StrUri (file);
		}
		
		public static T QueryInfo<T> (this GLib.File file, string attribute)
		{
			IntPtr info = NativeInterop.GFileQueryInfo (file, attribute, FileQueryInfoFlags.None, null);
						
			Type returnType = typeof(T);
			object ret;
			
			if (returnType == typeof(bool)) {
				ret = QueryBoolAttr (info, attribute);
			} else if (returnType == typeof(string)) {
				ret = QueryStringAttr (info, attribute);
			} else if (returnType == typeof(string[])) {
				ret = QueryStringVAttr (info, attribute);
			} else if (returnType == typeof(uint)) {
				ret = QueryUIntAttr (info, attribute);
			} else if (returnType == typeof(ulong)) {
				ret = QueryULongAttr (info, attribute);
			} else {
				ret = default(T);
			}
			
			NativeInterop.GObjectUnref (info);
			
			return (T) ret;
		}
		
		public static string Icon (this GLib.File file)
		{
			IntPtr info = NativeInterop.GFileQueryInfo (file, "standard::icon", FileQueryInfoFlags.None, null);
			GLib.Icon icon = NativeInterop.GFileInfoIcon (info);
			string iconName = DockServices.Drawing.IconFromGIcon (icon);
			NativeInterop.GObjectUnref (info);
			return iconName;
		}
		
		static long Size (this GLib.File file)
		{
			IntPtr info = NativeInterop.GFileQueryInfo (file, "standard::size", FileQueryInfoFlags.None, null);
			long size = NativeInterop.GFileInfoSize (info);
			NativeInterop.GObjectUnref (info);
			return size;
		}
		
		static string QueryStringAttr (IntPtr info, string attribute)
		{
			return NativeInterop.GFileInfoQueryString (info, attribute);
		}
			
		static string[] QueryStringVAttr (IntPtr info, string Attribute)
		{
			return NativeInterop.GFileInfoQueryStringV (info, Attribute);
		}
		
		static uint QueryUIntAttr (IntPtr info, string attribute)
		{
			return NativeInterop.GFileInfoQueryUInt (info, attribute);
		}
		
		static bool QueryBoolAttr (IntPtr info, string attribute)
		{
			return NativeInterop.GFileInfoQueryBool (info, attribute);
		}
		
		static ulong QueryULongAttr (IntPtr info, string attribute)
		{
			return NativeInterop.GFileInfoQueryULong (info, attribute);
		}

		public static FileType QueryFileType (this GLib.File file)
		{
			return file.QueryFileType (0, null);
		}
		
		// Recursively list all of the subdirs for a given directory
		public static IEnumerable<GLib.File> SubDirs (this GLib.File file)
		{
			return file.SubDirs (true);
		}
		
		// list all of the subdirs for a given directory
		public static IEnumerable<GLib.File> SubDirs (this GLib.File file, bool recurse)
		{
			if (!IO.Directory.Exists (file.Path))
				return Enumerable.Empty<GLib.File> ();
			
			try {
				// ignore symlinks
				if ((IO.File.GetAttributes (file.Path) & IO.FileAttributes.ReparsePoint) != 0)
					return Enumerable.Empty<GLib.File> ();
				
				// get all dirs contained in this dir
				List<GLib.File> dirs = IO.Directory.GetDirectories (file.Path)
							.Select (d => GLib.FileFactory.NewForPath (d)).ToList ();
				
				// if we are recursing, for each dir we found get its subdirs
				if (recurse)
					dirs.AddRange (dirs.SelectMany (d => d.SubDirs (true)));
				
				return dirs;
			} catch {
				return Enumerable.Empty<GLib.File> ();
			}
		}
		
		public static IEnumerable<GLib.File> GetFiles (this GLib.File file)
		{
			return file.GetFiles ("");
		}
		
		// gets all files under the given GLib.File (directory) with the extension of extension	
		public static IEnumerable<GLib.File> GetFiles (this GLib.File file, string extension)
		{
			if (!IO.Directory.Exists (file.Path))
				return Enumerable.Empty<GLib.File> ();
			
			try {
				return IO.Directory.GetFiles (file.Path, string.Format ("*{0}", extension))
					.Select (f => GLib.FileFactory.NewForPath (f));
			} catch {
				return Enumerable.Empty<GLib.File> ();
			}
		}
		
		/// <summary>
		/// Recursive equivalent to GLib.File.Delete () when called on a directory.
		/// Functionally equivalent to GLib.File.Delete when called on files.
		/// </summary>
		/// <param name="file">
		/// A <see cref="GLib.File"/>
		/// </param>
		public static void Delete_Recurse (this GLib.File file)
		{
			FileEnumerator enumerator = null;
			try {
				enumerator = file.EnumerateChildren ("standard::type,standard::name,access::can-delete", FileQueryInfoFlags.NofollowSymlinks, null);
			} catch { }
			
			if (enumerator == null)
				return;
			
			FileInfo info;
			
			while ((info = enumerator.NextFile ()) != null) {
				File child = file.GetChild (info.Name);
				
				if (info.FileType == FileType.Directory)
					Delete_Recurse (child);
				
				try {
					if (info.GetAttributeBoolean ("access::can-delete"))
						child.Delete (null);
				} catch {
					// if it fails to delete, not much we can do!
				}
				
				info.Dispose ();
			}
			
			if (info != null)
				info.Dispose ();
			enumerator.Close (null);
			enumerator.Dispose ();
		}
		
		// This is the recursive equivalent of GLib.File.Copy ()
		/// <summary>
		/// Recursive equivalent to GLib.File.Copy () when called on a directory.
		/// Functionally equivalent to GLib.File.Copy () when called on files.
		/// </summary>
		/// <param name="source">
		/// A <see cref="GLib.File"/>
		/// </param>
		/// <param name="dest">
		/// A <see cref="GLib.File"/>
		/// </param>
		/// <param name="flags">
		/// A <see cref="FileCopyFlags"/>
		/// </param>
		/// <param name="progress_cb">
		/// A <see cref="FileProgressCallback"/>
		/// </param>
		public static void Copy_Recurse (this GLib.File source, GLib.File dest, FileCopyFlags flags, FileProgressCallback progress_cb)
		{
			long totalBytes = source.GetSize ();
			long copiedBytes = 0;
			
			Recursive_Copy (source, dest, flags, ref copiedBytes, totalBytes, progress_cb);
		}
		
		/// <summary>
		/// Indicates whether or not a directory has files.
		/// </summary>
		/// <remarks>
		/// Will return false if called on a file.
		/// </remarks>
		/// <param name="file">
		/// A <see cref="GLib.File"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static bool DirectoryHasFiles (this GLib.File file)
		{
			IO.DirectoryInfo dir = new IO.DirectoryInfo (file.Path);
			
			if (dir.GetFiles ().Count () > 0 || dir.GetDirectories ().Count () > 0)
				return true;
			return false;
		}
		
		public static string NewFileName (this GLib.File fileToMove, File dest)
		{
			string name, ext;
			
			if (fileToMove.Basename.Split ('.').Count() > 1) {
				name = fileToMove.Basename.Split ('.').First ();
				ext = fileToMove.Basename.Substring (fileToMove.Basename.IndexOf ('.'));
			} else {
				name = fileToMove.Basename;
				ext = "";
			}
			if (dest.GetChild (fileToMove.Basename).Exists) {
				int i = 1;
				while (dest.GetChild (string.Format ("{0} ({1}){2}", name, i, ext)).Exists) {
					i++;
				}
				return string.Format ("{0} ({1}){2}", name, i, ext);
			} else {
				return fileToMove.Basename;
			}
		}
		
		static void Recursive_Copy (GLib.File source, GLib.File dest, FileCopyFlags flags, ref long copiedBytes, long totalBytes, FileProgressCallback progress_cb)
		{
			if (IO.File.Exists (source.Path)) {
				source.Copy (dest, flags, null, (current, total) => { 
					progress_cb.Invoke (current, totalBytes); 
				});
				return;
			}
			
			foreach (GLib.File subdir in source.SubDirs ()) {
				dest.GetChild (subdir.Basename).MakeDirectoryWithParents (null);
				// if it's a directory, continue the recursion
				Recursive_Copy (subdir, dest.GetChild (subdir.Basename), flags, ref copiedBytes, totalBytes, progress_cb);
			}
			
			foreach (File child in source.GetFiles ()) {
				long copied = copiedBytes;
				
				child.Copy (dest.GetChild (child.Basename), flags, null, (current, total) => {
					progress_cb.Invoke (copied + current, totalBytes);
				});
				copiedBytes += child.GetSize ();
			}
		}
		
		// will recurse and get the total size in bytes
		/// <summary>
		/// Returns the size in bytes of this file.  If called on a directory will return the size
		/// of all subsequent files recursively.
		/// </summary>
		/// <param name="file">
		/// A <see cref="GLib.File"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int64"/>
		/// </returns>
		public static long GetSize (this GLib.File file)
		{
			// if file is a regular file (not a directory), return the size
			if (IO.File.Exists (file.Path))
				return file.Size ();
			
			// otherwise treat it as a directory, and aggregate the size of all files in all subdirs (recursive)
			long size = 0;
			IEnumerable<GLib.File> files = IO.Directory.GetFiles (file.Path, "*", IO.SearchOption.AllDirectories)
				.Select (f => FileFactory.NewForPath (f));
			foreach (File f in files)
				size += f.GetSize ();
			return size;
		}
		
		/// <summary>
		/// The newline string for a particular input stream.
		/// </summary>
		/// <param name="stream">
		/// A <see cref="GLib.DataInputStream"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static string NewLineString (this GLib.DataInputStream stream)
		{
			switch (stream.NewlineType) {
			case DataStreamNewlineType.Cr:
				return "\r";
			case DataStreamNewlineType.Lf:
				return "\n";
			case DataStreamNewlineType.CrLf:
				return "\r\n";
			// this is a safe default because \n is the common line ending on *nix
			default:
				return "\n";
			}
		}
		
		/// <summary>
		/// Tries to mount a file with success and failure action callbacks.
		/// </summary>
		/// <param name="file">
		/// A <see cref="GLib.File"/>
		/// </param>
		/// <param name="success">
		/// A <see cref="Action"/> invoked when the mount was successful.
		/// </param>
		/// <param name="failed">
		/// A <see cref="Action"/> invoked when the mount failed.
		/// </param>
		public static void MountWithActionAndFallback (this GLib.File file, Action success, Action failed)
		{
			// In rare instances creating a Gtk.MountOperation can fail so let's try to create it first
			Gtk.MountOperation op = null;
			try {
				op = new Gtk.MountOperation (null);
			} catch (Exception e) {
				Log.Error ("Unable to create a Gtk.MountOperation. " +
					"This is most likely due to a missing gtk or glib library.  Error message: {0}", e.Message);
				Log.Debug (e.StackTrace);
			}
			file.MountEnclosingVolume (0, op == null ? null : op, null, (o, result) =>
			{
				// wait for the mount to finish
				try {
					if (file.MountEnclosingVolumeFinish (result)) {
						// invoke the supplied success action
						success.Invoke ();
						// if we have any other actions for this file on a successful mount
						// invoke them too
						if (!MountActions.ContainsKey (file))
							return;
						lock (MountActions[file]) {
							foreach (Action act in MountActions[file])
								act.Invoke ();
						}
					}
					// an exception can be thrown here if we are trying to mount an already mounted file
					// in that case, resort to the fallback
				} catch (GLib.GException) {
					try {
						failed.Invoke ();
					} catch {}
				}
			});
		}
		
		/// <summary>
		/// Add an action that gets invoked when a file gets mounted.
		/// </summary>
		/// <param name="file">
		/// A <see cref="GLib.File"/>
		/// </param>
		/// <param name="action">
		/// A <see cref="Action"/>
		/// </param>
		public static void AddMountAction (this GLib.File file, Action action)
		{
			if (!MountActions.ContainsKey (file))
				MountActions[file] = new List<Action> ();
			MountActions [file].Add (action);
		}
		
		/// <summary>
		/// Removes an action from the mount actions list for this file.
		/// </summary>
		/// <param name="file">
		/// A <see cref="GLib.File"/>
		/// </param>
		/// <param name="action">
		/// A <see cref="Action"/>
		/// </param>
		public static void RemoveAction (this GLib.File file, Action action)
		{
			if (MountActions.ContainsKey (file) && MountActions [file].Contains (action))
				MountActions [file].Remove (action);
		}
	}
}
