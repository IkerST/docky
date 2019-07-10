//  
//  Copyright (C) 2009-2010 Jason Smith, Chris Szikszoy
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
using System.Runtime.InteropServices;

using Wnck;

using Docky.Services.Windows;
using Docky.Services.Xlib;

namespace Docky.Services
{

	public static class WnckWindowExtension
	{
		public static int Area (this Window self)
		{
			Gdk.Rectangle geo = self.EasyGeometry ();
			return geo.Width * geo.Height;
		}
		
		public static Gdk.Rectangle EasyGeometry (this Window self)
		{
			Gdk.Rectangle geo;
			self.GetGeometry (out geo.X, out geo.Y, out geo.Width, out geo.Height);
			return geo;
		}
		
		public static void SetWorkaroundGeometry (this Window window, WindowGravity gravity, WindowMoveResizeMask mask, 
			int x, int y, int width, int height)
		{
			// This is very weird.  Don't know when they will fix it. You must subtract the top and left
			// frame extents from a move operation to get the window to actually show in the right spot.
			// Save for maybe kwin, I think only compiz uses Viewports anyhow, so this is ok.
			int [] extents = window.FrameExtents ();
			
			x -= extents [(int) Position.Left];
			y -= extents [(int) Position.Top];
			
			window.SetGeometry (gravity, mask, x, y, width, height);
		}
		
		public static int [] FrameExtents (this Window window)
		{
			return GetCardinalProperty (window, X11Atoms.Instance._NET_FRAME_EXTENTS);
		}
		
		public static int [] GetCardinalProperty (this Window window, IntPtr atom)
		{
			X11Atoms atoms = X11Atoms.Instance;
			IntPtr display;
			IntPtr type;
			int format;
			IntPtr prop_return;
			IntPtr nitems, bytes_after;
			int [] extents = new int[12];
			
			IntPtr window_handle = (IntPtr) window.Xid;
			
			display = X.GdkDisplayXDisplay (Gdk.Screen.Default.Display);
			type = IntPtr.Zero;
			
			X.XGetWindowProperty (display, window_handle, atom, (IntPtr) 0,
				(IntPtr) System.Int32.MaxValue, false, atoms.XA_CARDINAL, out type, out format,
				out nitems, out bytes_after, out prop_return);
			
			if (type == atoms.XA_CARDINAL && format == 32) {
				extents = new int [(int) nitems];
				for (int i = 0; i < (int) nitems; i++) {
					extents [i] = Marshal.ReadInt32 (prop_return, i * IntPtr.Size);
				}
			}
			
			return extents;
		}
	}
}
