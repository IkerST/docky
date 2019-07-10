// WindowControl.cs
// 
// Copyright (C) 2008 GNOME Do
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Linq;

using Wnck;

namespace Docky.Services.Windows
{
	
	public static class WindowControl
	{
		
		const int SleepTime = 10;
		const int FocusDelay = 200;
		
		/// <summary>
		/// Handles intelligent minimize/restoring of windows.  If one or more windows is minimized, it restores
		/// all windows.  If more all are visible, it minimizes.  This operation only takes into account windows
		/// on the current workspace (by design).
		/// </summary>
		/// <param name="windows">
		/// A <see cref="IEnumerable"/>
		/// </param>
		public static void MinimizeRestoreWindows (IEnumerable<Window> windows)
		{
			bool restore = false;
			foreach (Window w in windows) {
				if (w.IsMinimized) {
					restore = true;
					break;
				}
			}
			if (restore)
				RestoreWindows (windows);
			else
				MinimizeWindows (windows);
		}
		
		/// <summary>
		/// Minimizes every window in the list if it is not minimized
		/// </summary>
		/// <param name="windows">
		/// A <see cref="IEnumerable"/>
		/// </param>
		public static void MinimizeWindows (IEnumerable<Window> windows)
		{
			foreach (Window window in windows) {
				if (window.IsInViewport (window.Screen.ActiveWorkspace) && !window.IsMinimized) {
					window.Minimize ();
					System.Threading.Thread.Sleep (SleepTime);
				}
			}
		}
		
		/// <summary>
		/// Restores every window in the list that is minimized
		/// </summary>
		/// <param name="windows">
		/// A <see cref="IEnumerable"/>
		/// </param>
		public static void RestoreWindows (IEnumerable<Window> windows)
		{
			foreach (Window window in windows.Reverse ()) {
				if (window.IsInViewport (window.Screen.ActiveWorkspace) && window.IsMinimized) {
					window.Unminimize (Gtk.Global.CurrentEventTime);
					System.Threading.Thread.Sleep (SleepTime);
				}
			}
		}
		
		public static void FocusWindows (IEnumerable<Window> windows)
		{
			if (!windows.Any ())
				return;
			
			if (windows.Any (w => w.IsInViewport (w.Screen.ActiveWorkspace))) {
				foreach (Window window in windows.Reverse ()) {
					if (window.IsInViewport (window.Screen.ActiveWorkspace) && !window.IsMinimized) {
						window.CenterAndFocusWindow ();
						System.Threading.Thread.Sleep (SleepTime);
					}
				}
			} else {
				windows.First ().CenterAndFocusWindow ();
			}
			
			if (windows.Count () <= 1)
				return;
			
			// we do this to make sure our active window is also at the front... Its a tricky thing to do.
			// sometimes compiz plays badly.  This hacks around it
			uint time = Gtk.Global.CurrentEventTime + FocusDelay;
			GLib.Timeout.Add (FocusDelay, delegate {
				try { //unimportant if this fails, its just "nice"
					windows.Where (w => w.IsInViewport (w.Screen.ActiveWorkspace) && !w.IsMinimized).First ().Activate (time);
				} catch { }
				return false;
			});
		}
		
		public static void FocusWindows (Window window)
		{
			FocusWindows (new [] {window});
		}
		
		public static void IntelligentFocusOffViewportWindow (Window targetWindow, IEnumerable<Window> additionalWindows)
		{
			foreach (Window window in additionalWindows.Reverse ()) {
				if (!window.IsMinimized && WindowsShareViewport (targetWindow, window)) {
					window.CenterAndFocusWindow ();
					System.Threading.Thread.Sleep (SleepTime);
				}
			}
			
			targetWindow.CenterAndFocusWindow ();
			
			if (additionalWindows.Count () <= 1)
				return;
			
			// we do this to make sure our active window is also at the front... Its a tricky thing to do.
			// sometimes compiz plays badly.  This hacks around it
			uint time = Gtk.Global.CurrentEventTime + FocusDelay;
			GLib.Timeout.Add (FocusDelay, delegate {
				targetWindow.Activate (time);
				return false;
			});
		}
		
		static bool WindowsShareViewport (Wnck.Window first, Wnck.Window second)
		{
			if (first == null || second == null) return false;
			
			Wnck.Workspace wksp = first.Workspace ?? second.Workspace;
			if (wksp == null) return false;
			
			Gdk.Rectangle firstGeo, secondGeo;
			
			first.GetGeometry (out firstGeo.X, out firstGeo.Y, out firstGeo.Width, out firstGeo.Height);
			second.GetGeometry (out secondGeo.X, out secondGeo.Y, out secondGeo.Width, out secondGeo.Height);
			
			firstGeo.X += wksp.ViewportX;
			firstGeo.Y += wksp.ViewportY;
			
			secondGeo.X += wksp.ViewportX;
			secondGeo.Y += wksp.ViewportY;
			
			int viewportWidth, viewportHeight;
			viewportWidth = first.Screen.Width;
			viewportHeight = first.Screen.Height;
			
			int firstViewportX = ((firstGeo.X + firstGeo.Width / 2) / viewportWidth) * viewportWidth;
			int firstViewportY = ((firstGeo.Y + firstGeo.Height / 2) / viewportHeight) * viewportHeight;
			
			Gdk.Rectangle viewpRect = new Gdk.Rectangle (firstViewportX, firstViewportY, 
			                                             viewportWidth, viewportHeight);
			
			return viewpRect.IntersectsWith (secondGeo);
		}
		
		public static void CloseWindows (IEnumerable<Window> windows)
		{
			foreach (Window window in windows.Where (w => !w.IsSkipTasklist))
				window.Close (Gtk.Global.CurrentEventTime);
		}
		
		public static void CloseWindows (Window window)
		{
			CloseWindows (new [] {window});
		}
		
		public static void MinimizeRestoreWindows (Window window)
		{
			MinimizeRestoreWindows (new [] {window});
		}
		
		public static void MaximizeWindows (IEnumerable<Window> windows)
		{
			foreach (Window window in windows)
				window.Maximize ();
		}
		
		public static void MaximizeWindows (Window window)
		{
			window.Maximize ();
		}
		
		public static void UnmaximizeWindows (IEnumerable<Window> windows)
		{
			foreach (Wnck.Window window in windows)
				UnmaximizeWindows (window);
		}
		
		public static void UnmaximizeWindows (Wnck.Window window)
		{
			if (window.IsMaximized)
				window.Unmaximize ();
		}
		
		public static void MoveToWorkspace (Window window, Workspace workspace)
		{
			MoveToWorkspace (new [] {window}, workspace);
		}
		
		public static void MoveToWorkspace (IEnumerable<Window> windows, Workspace workspace)
		{
			foreach (Window window in windows.Where (w => w.Workspace != workspace))
				window.MoveToWorkspace (workspace);
		}
		
		/// <summary>
		/// Moves the current viewport to the selected window and then raises it
		/// </summary>
		/// <param name="w">
		/// A <see cref="Window"/>
		/// </param>
		public static void CenterAndFocusWindow (this Window w) 
		{
			if (w == null)
				return;

			uint time = Gtk.Global.CurrentEventTime;
			if (w.Workspace != null && w.Workspace != w.Screen.ActiveWorkspace) 
				w.Workspace.Activate (time);
			
			
			if (w.IsMinimized) 
				w.Unminimize (time);
			
			w.ActivateTransient (time);
		}
	}
}
