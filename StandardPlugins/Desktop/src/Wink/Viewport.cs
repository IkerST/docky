//  
//  Copyright (C) 2009 GNOME Do
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
using System.Runtime.InteropServices;
using System.Linq;

using Gdk;
using Wnck;

using Docky.Services;
using Docky.Services.Xlib;
using Docky.Services.Windows;

namespace WindowManager.Wink
{
	public class Viewport
	{
		static Stack<Dictionary<Wnck.Window, WindowState>> window_states = new Stack<Dictionary<Wnck.Window, WindowState>> ();
		
		private struct WindowState
		{
			public Gdk.Rectangle Area;
			public Wnck.WindowState State;
			
			public WindowState (Gdk.Rectangle area, Wnck.WindowState state)
			{
				Area = area;
				State = state;
			}
			
			public static bool operator ==(WindowState a, WindowState b)
			{
				return a.Area.Equals (b.Area) && (a.State == b.State);
			}
			
			public static bool operator !=(WindowState a, WindowState b)
			{
				return !a.Area.Equals (b.Area) || !(a.State == b.State);
			}
			
			public override bool Equals (object obj)
			{
				WindowState ws = (WindowState) obj;
				return Area.Equals (ws.Area) && (State == ws.State);
			}
			
			public override int GetHashCode ()
			{
				return (int) (((long) Area.GetHashCode () + State.GetHashCode ()) % int.MaxValue);
			}
			
			public override string ToString ()
			{
				return string.Format ("{0} ({1})", Area, State);
			}
		}
		
		Workspace parent;
		Rectangle area;
		
		public bool IsActive {
			get {
				if (!parent.IsVirtual)
					return Wnck.Screen.Default.ActiveWorkspace == parent;
				return Wnck.Screen.Default.ActiveWorkspace.ViewportX == area.X && Wnck.Screen.Default.ActiveWorkspace.ViewportY == area.Y;
			}
		}
		
		WindowMoveResizeMask MoveResizeMask {
			get {
				return WindowMoveResizeMask.X | WindowMoveResizeMask.Y |
					   WindowMoveResizeMask.Height | WindowMoveResizeMask.Width;
			}
		}
		
		internal Viewport(Rectangle area, Workspace parent)
		{
			this.area = area;
			this.parent = parent;
		}
		
		IEnumerable<Wnck.Window> RawWindows ()
		{
			foreach (Wnck.Window window in ScreenUtils.GetWindows ())
				if (WindowCenterInViewport (window) || window.IsSticky)
					yield return window;
		}
		
		IEnumerable<Wnck.Window> Windows ()
		{
			return RawWindows ().Where (w => !w.IsSkipTasklist && w.WindowType != Wnck.WindowType.Dock);
		}
		
		bool WindowCenterInViewport (Wnck.Window window)
		{
			if (!window.IsOnWorkspace (parent))
				return false;
				
			Rectangle geo = window.EasyGeometry ();
			geo.X += parent.ViewportX;
			geo.Y += parent.ViewportY;
			
			return area.Contains (new Point (geo.X + geo.Width / 2, geo.Y + geo.Height / 2));
		}
		
		public void ShowDesktop ()
		{
			ShowDesktop (true);
		}
		
		void ShowDesktop (bool storeState)
		{
			if (storeState)
				window_states.Push (new Dictionary<Wnck.Window, WindowState> ());
			
			if (!ScreenUtils.DesktopShown (parent.Screen))
				ScreenUtils.ShowDesktop (parent.Screen);
			else
				ScreenUtils.UnshowDesktop (parent.Screen);
		}
		
		public void Cascade ()
		{
			IEnumerable<Wnck.Window> windows = Windows ().Where (w => !w.IsMinimized);
			if (windows.Count () <= 1) return;
			
			Dictionary<Wnck.Window, WindowState> state = new Dictionary<Wnck.Window, WindowState> ();
			window_states.Push (state);
			
			Gdk.Rectangle screenGeo = GetScreenGeoMinusStruts ();
			
			int titleBarSize = windows.First ().FrameExtents () [(int) Position.Top];
			int windowHeight = screenGeo.Height - ((windows.Count () - 1) * titleBarSize);
			int windowWidth = screenGeo.Width - ((windows.Count () - 1) * titleBarSize);
			
			int count = 0;
			foreach (Wnck.Window window in windows) {
				int x = screenGeo.X + titleBarSize * count - parent.ViewportX;
				int y = screenGeo.Y + titleBarSize * count - parent.ViewportY;
				
				Gdk.Rectangle windowArea = new Gdk.Rectangle (x, y, windowWidth, windowHeight);;

				SetTemporaryWindowGeometry (window, windowArea, state);
				count++;
			}
		}
		
		public void Tile ()
		{
			IEnumerable<Wnck.Window> windows = Windows ().Where (w => !w.IsMinimized);
			if (windows.Count () <= 1) return;
			
			Dictionary<Wnck.Window, WindowState> state = new Dictionary<Wnck.Window, WindowState> ();
			window_states.Push (state);
			
			Gdk.Rectangle screenGeo = GetScreenGeoMinusStruts ();
			
			// We are going to tile to a square, so what we want is to find
			// the smallest perfect square all our windows will fit into
			int width = (int) Math.Ceiling (Math.Sqrt (windows.Count ()));
			
			// Our height is at least one (e.g. a 2x1)
			int height = 1;
			while (width * height < windows.Count ())
				height++;
			
			int windowWidth = screenGeo.Width / width;
			int windowHeight = screenGeo.Height / height;
			
			int row = 0, column = 0;
			
			foreach (Wnck.Window window in windows) {
				int x = screenGeo.X + (column * windowWidth) - parent.ViewportX;
				int y = screenGeo.Y + (row * windowHeight) - parent.ViewportY;
				
				Gdk.Rectangle windowArea = new Gdk.Rectangle (x, y, windowWidth, windowHeight);;
				
				if (window == windows.Last ())
					windowArea.Width *= width - column;
				
				SetTemporaryWindowGeometry (window, windowArea, state);
				
				column++;
				if (column == width) {
					column = 0;
					row++;
				}
			}
		}
		
		public bool CanRestoreLayout ()
		{
			return window_states.Count () > 0;
		}
		
		public void RestoreLayout ()
		{
			if (!CanRestoreLayout ())
				return;
			
			Dictionary<Wnck.Window, WindowState> state = window_states.Pop ();
			
			if (state.Count () == 0)
				ShowDesktop (false);
			else
				foreach (Wnck.Window window in Windows ())
					RestoreTemporaryWindowGeometry (window, state);
		}
		
		public void CleanRestoreStates ()
		{
			IEnumerable<Wnck.Window> windows = Windows ();
			List<Dictionary<Wnck.Window, WindowState>> expireStates = new List<Dictionary<Wnck.Window, WindowState>> ();
			
			foreach (Dictionary<Wnck.Window, WindowState> dict in window_states) {
				if (dict.Count () == 0)
					continue;
				
				IEnumerable<Wnck.Window> keys = dict.Keys;
				foreach (Wnck.Window w in keys.Except (windows))
					dict.Remove (w);
				
				if (dict.Count () == 0)
					expireStates.Add (dict);
			}
			
			IEnumerable<Dictionary<Wnck.Window, WindowState>> newStack = window_states.Except (expireStates).Take (10);
			window_states = new Stack<Dictionary<Wnck.Window, WindowState>> (newStack);
		}
		
		Gdk.Rectangle GetScreenGeoMinusStruts ()
		{
			IEnumerable<int []> struts = RawWindows ()
				.Where (w => w.WindowType == Wnck.WindowType.Dock)
				.Select (w => w.GetCardinalProperty (X11Atoms.Instance._NET_WM_STRUT_PARTIAL));
			
			int [] offsets = new int [4];
			for (int i = 0; i < 4; i++)
				offsets [i] = struts.Max (a => a[i]);
			
			Gdk.Rectangle screenGeo = area;
			screenGeo.Width -= offsets [(int) Position.Left] + offsets [(int) Position.Right];
			screenGeo.Height -= offsets [(int) Position.Top] + offsets [(int) Position.Bottom];
			screenGeo.X += offsets [(int) Position.Left];
			screenGeo.Y += offsets [(int) Position.Top];
			
			return screenGeo;
		}
		
		void SetTemporaryWindowGeometry (Wnck.Window window, Gdk.Rectangle area, Dictionary<Wnck.Window, WindowState> state)
		{
			Gdk.Rectangle oldGeo = window.EasyGeometry ();
			
			oldGeo.X += parent.ViewportX;
			oldGeo.Y += parent.ViewportY;
			
			state [window] = new WindowState (oldGeo, window.State);
			
			if (window.IsMaximized)
				window.Unmaximize ();
			
			window.SetWorkaroundGeometry (WindowGravity.Current, MoveResizeMask, area.X, area.Y, area.Width, area.Height);
		}
		
		void RestoreTemporaryWindowGeometry (Wnck.Window window, Dictionary<Wnck.Window, WindowState> state)
		{
			if (!state.ContainsKey (window))
				return;
			
			WindowState currentState = state [window];
			window.SetWorkaroundGeometry (WindowGravity.Current, MoveResizeMask,
											currentState.Area.X - parent.ViewportX, currentState.Area.Y - parent.ViewportY,
											currentState.Area.Width, currentState.Area.Height);
		}
	}
}
