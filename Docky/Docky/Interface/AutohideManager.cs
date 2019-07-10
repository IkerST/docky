//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
//  Copyright (C) 2010-2011 Robert Dyer
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

using Gdk;
using Wnck;

using Docky.Services;
using Docky.Services.Prefs;

namespace Docky.Interface
{
	public class AutohideManager : IDisposable
	{
		public event EventHandler HiddenChanged;
		public event EventHandler DockHoveredChanged;
		
		static IPreferences prefs = DockServices.Preferences.Get <AutohideManager> ();
		static uint unhideDelay = (uint) prefs.Get<int> ("UnhideDelay", 0);
		static uint updateDelay = (uint) prefs.Get<int> ("UpdateDelay", 200);
		
		Gdk.Rectangle cursor_area, intersect_area, last_known_geo;
		Wnck.Screen screen;
		CursorTracker tracker;
		int pid;
		uint timer_activewindow;
		uint timer_geometry;
		
		bool WindowIntersectingOther { get; set; }
		
		bool dockHovered;
		public bool DockHovered {
			get { return dockHovered; }
			private set {
				if (dockHovered == value)
					return;
				
				dockHovered = value;
				
				OnDockHoveredChanged ();
			}
		}
		
		uint event_timer = 0;
		
		bool hidden;
		public bool Hidden {
			get { return hidden; } 
			private set { 
				if (value && event_timer > 0) {
					GLib.Source.Remove (event_timer);
					event_timer = 0;
				}
				
				if (hidden == value)
					return;
				
				if (!hidden || !DockHovered || unhideDelay == 0) {
					hidden = value; 
					
					OnHiddenChanged ();
				} else {
					if (event_timer > 0)
						return;
					event_timer = GLib.Timeout.Add (unhideDelay, delegate {
						hidden = value; 
						OnHiddenChanged ();
						event_timer = 0;
						return false;
					});
				}
			} 
		}
		
		AutohideType behavior;
		public AutohideType Behavior { 
			get { return behavior; } 
			set { 
				if (behavior == value)
					return;
				
				behavior = value; 
				
				SetHidden ();
			} 
		}
		
		bool config_mode;
		public bool ConfigMode { 
			get { return config_mode; } 
			set { 
				if (config_mode == value)
					return;
				
				config_mode = value; 
				
				SetHidden ();
			} 
		}
		
		bool startup_mode;
		public bool StartupMode { 
			get { return startup_mode; } 
			set { 
				if (startup_mode == value)
					return;
				
				startup_mode = value; 
				
				SetHidden ();
			} 
		}
		
		internal AutohideManager (Gdk.Screen screen)
		{
			pid = System.Diagnostics.Process.GetCurrentProcess ().Id;
			
			tracker = CursorTracker.ForDisplay (screen.Display);
			this.screen = Wnck.Screen.Get (screen.Number);
			
			tracker.CursorPositionChanged   += HandleCursorPositionChanged;
			this.screen.ActiveWindowChanged += HandleActiveWindowChanged;
			this.screen.WindowOpened        += HandleWindowOpened;
			this.screen.WindowClosed        += HandleWindowClosed;
		}
		
		public void SetCursorArea (Gdk.Rectangle area)
		{
			if (cursor_area == area)
				return;
			cursor_area = area;
			SetDockHovered ();
			SetHidden ();
		}
		
		void SetDockHovered ()
		{
			DockHovered = cursor_area.Contains (tracker.Cursor) 
				&& (screen.ActiveWindow == null || !screen.ActiveWindow.IsFullscreen || !screen.ActiveWindow.EasyGeometry().IntersectsWith (intersect_area));
		}
		
		public void SetIntersectArea (Gdk.Rectangle area)
		{
			if (intersect_area == area)
				return;
			
			intersect_area = area;
			UpdateWindowIntersect ();
		}
		
		void HandleCursorPositionChanged (object sender, CursorPostionChangedArgs args)
		{
			SetDockHovered ();
			SetHidden ();
		}

		void HandleActiveWindowChanged (object o, ActiveWindowChangedArgs args)
		{
			if (args.PreviousWindow != null)
				args.PreviousWindow.GeometryChanged -= HandleGeometryChanged;

			if (timer_activewindow > 0)
				return;

			timer_activewindow = GLib.Timeout.Add (updateDelay, delegate {
				SetupActiveWindow ();
				UpdateWindowIntersect ();
				timer_activewindow = 0;
				return false;
			});
		}
		
		void HandleWindowOpened (object sender, WindowOpenedArgs args)
		{
			UpdateWindowIntersect ();
		}
		
		void HandleWindowClosed (object sender, WindowClosedArgs args)
		{
			UpdateWindowIntersect ();
		}
		
		void SetupActiveWindow ()
		{
			Wnck.Window active = screen.ActiveWindow;
			if (active != null) {
				active.GeometryChanged += HandleGeometryChanged; 
				last_known_geo = active.EasyGeometry ();
			}
		}
		
		void HandleGeometryChanged (object sender, EventArgs e)
		{
			Wnck.Window window = sender as Wnck.Window;
			
			if (sender == null)
				return;
			
			Gdk.Rectangle geo = window.EasyGeometry ();
			
			if (geo == last_known_geo)
				return;
			
			last_known_geo = geo;
			
			if (timer_geometry > 0)
				return;
			
			timer_geometry = GLib.Timeout.Add (updateDelay, delegate {
				UpdateWindowIntersect ();
				timer_geometry = 0;
				return false;
			});
		}
		
		bool IsIntersectableWindow (Wnck.Window window)
		{
			return window != null &&
				!window.IsMinimized &&
				window.Pid != pid &&
				window.WindowType != Wnck.WindowType.Desktop &&
				window.WindowType != Wnck.WindowType.Dock &&
				window.WindowType != Wnck.WindowType.Splashscreen &&
				window.WindowType != Wnck.WindowType.Menu &&
				Wnck.Screen.Default.ActiveWorkspace != null &&
				window.IsVisibleOnWorkspace (Wnck.Screen.Default.ActiveWorkspace);
		}
		
		void UpdateWindowIntersect ()
		{
			Gdk.Rectangle adjustedDockArea = intersect_area;
			adjustedDockArea.Inflate (-2, -2);
			
			bool intersect = false;
			Wnck.Window activeWindow = screen.ActiveWindow;
			
			try {
				foreach (Wnck.Window window in screen.Windows.Where (w => IsIntersectableWindow (w))) {
					if (Behavior == AutohideType.Intellihide && activeWindow != null && activeWindow.Pid != window.Pid)
						continue;
					
					if (window.EasyGeometry ().IntersectsWith (adjustedDockArea)) {
						intersect = true;
						break;
					}
				}
			} catch (Exception e) {
				Log<AutohideManager>.Error ("Failed to update window intersect: '{0}'", e.Message);
				Log<AutohideManager>.Debug (e.StackTrace);
			}
			
			if (WindowIntersectingOther != intersect) {
				WindowIntersectingOther = intersect;
				SetHidden ();
			}
		}
		
		void SetHidden ()
		{
			if (StartupMode) {
				Hidden = true;
				return;
			}
			
			switch (Behavior) {
			default:
			case AutohideType.None:
				Hidden = false;
				break;
			case AutohideType.Autohide:
				Hidden = !ConfigMode && !DockHovered;
				break;
			case AutohideType.Intellihide:
			case AutohideType.UniversalIntellihide:
				Hidden = !ConfigMode && !DockHovered && WindowIntersectingOther;
				break;
			}
		}
		
		void OnDockHoveredChanged ()
		{
			if (DockHoveredChanged != null)
				DockHoveredChanged (this, EventArgs.Empty);
		}
		
		void OnHiddenChanged ()
		{
			if (HiddenChanged != null)
				HiddenChanged (this, EventArgs.Empty);
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			if (event_timer > 0) {
				GLib.Source.Remove (event_timer);
				event_timer = 0;
			}
			
			if (screen != null) {
				screen.ActiveWindowChanged -= HandleActiveWindowChanged;
				if (screen.ActiveWindow != null)
					screen.ActiveWindow.GeometryChanged -= HandleGeometryChanged;
				screen.WindowOpened -= HandleWindowOpened;
				screen.WindowClosed -= HandleWindowClosed;
			}
			
			if (tracker != null)
				tracker.CursorPositionChanged -= HandleCursorPositionChanged;
		}
		#endregion
	}
}
