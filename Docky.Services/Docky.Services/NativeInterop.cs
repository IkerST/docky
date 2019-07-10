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
using System.Linq;
using System.Runtime.InteropServices;

using GLib;

using Mono.Unix;

namespace Docky.Services
{
	
	internal class NativeInterop
	{		
		[DllImport ("libgio-2.0")]
		private static extern IntPtr g_file_get_uri (IntPtr fileHandle);
		
		[DllImport("libc")]
		private static extern int prctl (int option, byte[] arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);
		
		// these next 4 methods are not yet in GIO#.  The methods in GIO# (Unmount, Eject, UnmountFinish, EjectFinish)
		// have been marked as deprecated since 2.22.  Once GIO# gets these methods we can remove these.
		[DllImport("libgio-2.0")]
		private static extern void g_mount_unmount_with_operation (IntPtr mount, int flags, IntPtr mount_operation, 
			IntPtr cancellable, GLibSharp.AsyncReadyCallbackNative callback, IntPtr user_data);
		
		[DllImport("libgio-2.0")]
		private static extern void g_mount_eject_with_operation (IntPtr mount, int flags, IntPtr mount_operation, 
			IntPtr cancellable, GLibSharp.AsyncReadyCallbackNative callback, IntPtr user_data);
		
		[DllImport("libgio-2.0")]
		private static extern bool g_mount_unmount_with_operation_finish (IntPtr mount, IntPtr result, out IntPtr error);
		
		[DllImport("libgio-2.0")]
		private static extern bool g_mount_eject_with_operation_finish (IntPtr mount, IntPtr result, out IntPtr error);
		
		// GTK# seems not to have lookup_by_gicon... I need it...
		[DllImport("libgtk-x11-2.0", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr gtk_icon_theme_lookup_by_gicon (IntPtr icon_theme, IntPtr icon, int size, int flags);

		[DllImport("libgdk_pixbuf-2.0")]
		private static extern IntPtr gdk_pixbuf_new_from_file_at_size (string filename, int width, int height, out IntPtr error);

		#region Workaround for GLib.FileInfo leaks...
		
		// some attributes must be looked up as bytestrings, not strings
		static readonly string [] BYTE_STRING_VALS = new string [] {
			"thumbnail::path",
			"trash::orig-path",
			"standard::name",
			"standard::symlink-target",
		};
		
		[DllImport("libgio-2.0")]
		static extern int g_file_info_get_attribute_type(IntPtr raw, string attribute);
		
		[DllImport("libgobject-2.0")]
		private static extern IntPtr g_object_ref (IntPtr @object);
		
		[DllImport("libgobject-2.0")]
		private static extern void g_object_unref (IntPtr @object);
		
		[DllImport("libgio-2.0")]
		private static extern IntPtr g_file_query_info (IntPtr file, string attributes, int flags, IntPtr cancellable, out IntPtr error);

		[DllImport("libgio-2.0")]
		private static extern IntPtr g_file_query_filesystem_info(IntPtr file, string attributes, IntPtr cancellable, out IntPtr error);
		
		[DllImport("libgio-2.0")]
		private static extern IntPtr g_file_info_get_attribute_string (IntPtr info, string attribute);
		
		[DllImport("libgio-2.0")]
		private static extern IntPtr g_file_info_get_attribute_stringv (IntPtr info, string attribute);
				
		[DllImport("libgio-2.0")]
		private static extern IntPtr g_file_info_get_attribute_byte_string (IntPtr info, string attribute);
		
		[DllImport("libgio-2.0")]
		static extern uint g_file_info_get_attribute_uint32 (IntPtr info, string attribute);
		
		[DllImport("libgio-2.0")]
		static extern ulong g_file_info_get_attribute_uint64 (IntPtr info, string attribute);
		
		[DllImport("libgio-2.0")]
		static extern bool g_file_info_get_attribute_boolean (IntPtr info, string attribute);
		
		[DllImport("libgio-2.0")]
		static extern long g_file_info_get_size (IntPtr info);
		
		[DllImport("libgio-2.0")]
		static extern IntPtr g_file_info_get_icon (IntPtr info);
		
		#endregion
		
		const string GIO_NOT_FOUND = "Could not find gio-2.0, please report immediately.";
		const string GOBJECT_NOT_FOUND = "Could not find gobject-2.0, please report immediately.";
		const string GTK_NOT_FOUND = "Could not find gtk-2.0, please report immediately.";
		const string GDK_PIXBUF_NOT_FOUND = "Could not find gdk_pixbuf-2.0, please report immediately.";
		
		public static string StrUri (File file)
		{
			return NativeHelper<string> (() => {
				return GLib.Marshaller.PtrToStringGFree (g_file_get_uri (file.Handle));
			}, GIO_NOT_FOUND, 
			string.Format ("Failed to retrieve uri for file '{0}': ", file.Path) + "{0}");
		}

		public static int prctl (int option, string arg2)
		{
			return NativeHelper<int> (() => {
				return prctl (option, System.Text.Encoding.ASCII.GetBytes (arg2 + "\0"), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			}, -1, "Could not find libc, please report immediately.",
			"Failed to set process name: {0}");
		}
		
		public static void UnmountWithOperation (Mount mount, MountUnmountFlags flags, MountOperation op, 
			Cancellable cancellable, AsyncReadyCallback cb)
		{
			NativeHelper (() =>
			{
				GLibSharp.AsyncReadyCallbackWrapper cb_wrapper = new GLibSharp.AsyncReadyCallbackWrapper (cb);
				g_mount_unmount_with_operation (mount.Handle, (int) flags, op == null ? IntPtr.Zero : op.Handle, 
				cancellable == null ? IntPtr.Zero : cancellable.Handle, cb_wrapper.NativeDelegate, IntPtr.Zero);
			}, GIO_NOT_FOUND,
			"Failed to unmount with operation: {0}");
		}
		
		public static void EjectWithOperation (Mount mount, MountUnmountFlags flags, MountOperation op, 
			Cancellable cancellable, AsyncReadyCallback cb)
		{
			NativeHelper (() =>
			{
				GLibSharp.AsyncReadyCallbackWrapper cb_wrapper = new GLibSharp.AsyncReadyCallbackWrapper (cb);
				g_mount_eject_with_operation (mount.Handle, (int) flags, op == null ? IntPtr.Zero : op.Handle, 
				cancellable == null ? IntPtr.Zero : cancellable.Handle, cb_wrapper.NativeDelegate, IntPtr.Zero);
			}, GIO_NOT_FOUND,
			"Failed to eject with operation name: {0}");
		}
		
		public static bool EjectWithOperationFinish (Mount mount, AsyncResult result)
		{
			return NativeHelper<bool> (() =>
			{
				IntPtr error = IntPtr.Zero;
				bool success = g_mount_eject_with_operation_finish (mount.Handle, result == null ? IntPtr.Zero : 
				((result is GLib.Object) ? (result as GLib.Object).Handle : (result as GLib.AsyncResultAdapter).Handle), out error);
				if (error != IntPtr.Zero)
					throw new GLib.GException (error);
				return success;
			}, false, GIO_NOT_FOUND,
			"Failed to eject with operation finish name: {0}");
		}
		
		public static bool UnmountWithOperation (Mount mount, AsyncResult result)
		{
			return NativeHelper<bool> (() =>
			{
				IntPtr error = IntPtr.Zero;
				bool success = g_mount_unmount_with_operation_finish (mount.Handle, result == null ? IntPtr.Zero : ((result is GLib.Object) ? (result as GLib.Object).Handle : (result as GLib.AsyncResultAdapter).Handle), out error);
				if (error != IntPtr.Zero)
					throw new GLib.GException (error);
				return success;
			}, false, GIO_NOT_FOUND,
			"Failed to unmount with operation finish name: {0}");
		}
		
		public static Gtk.IconInfo IconThemeLookUpByGIcon (Gtk.IconTheme theme, GLib.Icon icon, int size, int flags)
		{
			return NativeHelper<Gtk.IconInfo> (() =>
			{
				IntPtr raw_ret = gtk_icon_theme_lookup_by_gicon (theme.Handle, 
				    icon == null ? IntPtr.Zero : ((icon is GLib.Object) ? (icon as GLib.Object).Handle : (icon as GLib.IconAdapter).Handle),
				    size, (int) flags);
				Gtk.IconInfo ret = raw_ret == IntPtr.Zero ? null : (Gtk.IconInfo) GLib.Opaque.GetOpaque (raw_ret, typeof(Gtk.IconInfo), true);
				return ret;
			}, null, GTK_NOT_FOUND,
			"Failed to lookup by GIcon: {0}");
		}
		
		#region Workaround for GLib.FileInfo leaks...
		
		public static IntPtr GFileQueryInfo (GLib.File file, string attributes, FileQueryInfoFlags flags, Cancellable cancellable)
		{
			return NativeHelper<IntPtr> (() =>
			{
				IntPtr error;
				IntPtr info;
				if (attributes.StartsWith ("filesystem::"))
					info = g_file_query_filesystem_info (file.Handle, attributes,
					cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
				else
					info = g_file_query_info (file.Handle, attributes, (int) flags, 
					cancellable == null ? IntPtr.Zero : cancellable.Handle, out error);
				
				if (error != IntPtr.Zero)
					throw new GException (error);
				return info;
			}, IntPtr.Zero, GIO_NOT_FOUND,
			string.Format ("Failed to query info for '{0}': ", file.Path) + "{0}");
		}
		
		public static int GFileInfoQueryAttributeType (IntPtr info, string attribute)
		{
			return NativeHelper<int> (() =>
			{
				int type = g_file_info_get_attribute_type (info, attribute);
				return type;
			}, GIO_NOT_FOUND,
			string.Format ("Failed to query attribute type '{0}': ", attribute) + "{0}");
		}
		
		public static string GFileInfoQueryString (IntPtr info, string attribute)
		{
			return NativeHelper<string> (() =>
			{
				IntPtr str;
				if (BYTE_STRING_VALS.Any (s => attribute.StartsWith (s)))
					str = g_file_info_get_attribute_byte_string (info, attribute);
				else
					str = g_file_info_get_attribute_string (info, attribute);
				return GLib.Marshaller.Utf8PtrToString (str);
			}, GIO_NOT_FOUND,
			string.Format ("Failed to query string '{0}': ", attribute) + "{0}");
		}
		
		public static string[] GFileInfoQueryStringV (IntPtr info, string attribute)
		{
			return NativeHelper<string[]> (() =>
			{
				IntPtr strv;
				strv = g_file_info_get_attribute_stringv (info, attribute);
				return GLib.Marshaller.NullTermPtrToStringArray (strv, false);
			}, GIO_NOT_FOUND,
			string.Format ("Failed to query stringv '{0}': ", attribute) + "{0}");
		}
		
		public static uint GFileInfoQueryUInt (IntPtr info, string attribute)
		{
			return NativeHelper<uint> (() =>
			{
				return g_file_info_get_attribute_uint32 (info, attribute);
			}, GIO_NOT_FOUND,
			string.Format ("Failed to query uint '{0}': ", attribute) + "{0}");
		}
		
		public static ulong GFileInfoQueryULong (IntPtr info, string attribute)
		{
			return NativeHelper<ulong> (() =>
			{
				return g_file_info_get_attribute_uint64 (info, attribute);
			}, GIO_NOT_FOUND,
			string.Format ("Failed to query uint '{0}': ", attribute) + "{0}");
		}		
		
		public static bool GFileInfoQueryBool (IntPtr info, string attribute)
		{
			return NativeHelper<bool> (() =>
			{
				return g_file_info_get_attribute_boolean (info, attribute);
			}, GIO_NOT_FOUND,
			string.Format ("Failed to query bool '{0}': ", attribute) + "{0}");
		}
		
		public static Icon GFileInfoIcon (IntPtr info)
		{
			return NativeHelper<Icon> (() =>
			{
				IntPtr icon = g_file_info_get_icon (info);
				return IconAdapter.GetObject (g_object_ref (icon), false);
			}, GIO_NOT_FOUND,
			"Failed to query icon {0}");
		}
		
		public static long GFileInfoSize (IntPtr info)
		{
			return NativeHelper<long> (() =>
			{
				return g_file_info_get_size (info);
			}, GIO_NOT_FOUND,
			"Failed to query icon {0}");
		}
		
		public static Gdk.Pixbuf GdkPixbufNewFromFileAtSize (string filename, int width, int height)
		{
			return NativeHelper<Gdk.Pixbuf> (() =>
			{
				IntPtr error = IntPtr.Zero;
				IntPtr pixbuf = gdk_pixbuf_new_from_file_at_size (filename, width, height, out error);
				if (error != IntPtr.Zero)
					throw new GLib.GException (error);
				return new Gdk.Pixbuf (pixbuf);
			}, GDK_PIXBUF_NOT_FOUND,
			"Failed to load pixbuf from file {0}");
		}

		public static void GObjectUnref (IntPtr objectHandle)
		{
			NativeHelper (() =>
			{
				if (objectHandle == IntPtr.Zero)
					return;
				g_object_unref (objectHandle);
			}, GOBJECT_NOT_FOUND,
			"Could not unref object: {0}");
		}
		
		#endregion
	
		static T NativeHelper<T> (Func<T> act, T errorReturn, string notFound, string error)
		{
			try {
				T ret = act.Invoke ();
				return ret;
			} catch (DllNotFoundException e) {
				Log<NativeInterop>.Fatal (notFound);
				Log<NativeInterop>.Info (e.StackTrace);
				return errorReturn;
			} catch (Exception e) {
				Log<NativeInterop>.Error (error, e.Message);
				Log<NativeInterop>.Info (e.StackTrace);
				return errorReturn;
			}
		}
		
		static void NativeHelper (Action act, string notFound, string error)
		{
			NativeHelper<int> (() => {
				act.Invoke ();
				return -1;
			}, notFound, error);
		}
		
		static T NativeHelper<T> (Func<T> act, string notFound, string error)
		{
			return NativeHelper<T> (act, default(T), notFound, error);
		}
	}
}
