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
using System.Linq;

using Wnck;

namespace WindowManager.Wink
{
	public static class ScreenUtils
	{
		static Dictionary<Workspace, Viewport [,]> layouts;
		
		static List<Window> window_list;
		static bool window_list_update_needed;
		
		public static Viewport ActiveViewport {
			get {
				if (Viewports.Any (vp => vp.IsActive))
					return Viewports.First (vp => vp.IsActive);
				return null;
			}
		}
		
		public static IEnumerable<Viewport> Viewports {
			get { 
				foreach (Viewport [,] layout in layouts.Values)
					foreach (Viewport viewport in layout)
						yield return viewport;
			}
		}
		
		public static void Initialize ()
		{
			Wnck.Screen.Default.ViewportsChanged += HandleViewportsChanged;
			Wnck.Screen.Default.WorkspaceCreated += HandleWorkspaceCreated;
			Wnck.Screen.Default.WorkspaceDestroyed += HandleWorkspaceDestroyed;
			
			UpdateViewports ();
			
			Wnck.Screen.Default.WindowClosed += delegate {
				window_list_update_needed = true;
			};
			
			Wnck.Screen.Default.WindowOpened += delegate {
				window_list_update_needed = true;
			};
			
			Wnck.Screen.Default.ApplicationOpened += delegate {
				window_list_update_needed = true;
			};
			
			Wnck.Screen.Default.ApplicationClosed += delegate {
				window_list_update_needed = true;
			};
		}
		
		static void HandleWorkspaceDestroyed(object o, WorkspaceDestroyedArgs args)
		{
			UpdateViewports ();
		}

		static void HandleWorkspaceCreated(object o, WorkspaceCreatedArgs args)
		{
			UpdateViewports ();
		}

		static void HandleViewportsChanged(object sender, EventArgs e)
		{
			UpdateViewports ();
		}
		
		public static bool DesktopShown (Screen screen)
		{
			return screen.ShowingDesktop;
		}
		
		public static void ShowDesktop (Screen screen)
		{
			if (!screen.ShowingDesktop)
				screen.ToggleShowingDesktop (true);
		}
		
		public static void UnshowDesktop (Screen screen)
		{
			if (screen.ShowingDesktop)
				screen.ToggleShowingDesktop (false);
		}
		
		public static Viewport [,] ViewportLayout ()
		{
			return ViewportLayout (Wnck.Screen.Default.ActiveWorkspace);
		}
		
		public static Viewport [,] ViewportLayout (Workspace workspace)
		{
			if (!layouts.ContainsKey (workspace))
				return new Viewport [0,0];
			
			return layouts [workspace];
		}
		
		static void UpdateViewports ()
		{
			layouts = new Dictionary<Workspace, Viewport [,]> ();

			int currentViewport = 1;
			foreach (Wnck.Workspace workspace in Wnck.Screen.Default.Workspaces)
				if (workspace.IsVirtual) {
					int viewportWidth = workspace.Screen.Width;
					int viewportHeight = workspace.Screen.Height;
					
					int rows = workspace.Height / viewportHeight;
					int columns = workspace.Width / viewportWidth;
					
					layouts [workspace] = new Viewport [rows, columns];
					
					for (int i = 0; i < rows; i++)
						for (int j = 0; j < columns; j++) {
							Gdk.Rectangle area = new Gdk.Rectangle (j * viewportWidth, i * viewportHeight,
							                                        viewportWidth, viewportHeight);
							layouts [workspace] [i, j] = new Viewport (area, workspace);
							currentViewport++;
						}
				} else {
					layouts [workspace] = new Viewport [1,1];
					Viewport viewport = new Viewport (new Gdk.Rectangle (0, 0, workspace.Width, workspace.Height),
					                                  workspace);
					layouts [workspace] [0,0] = viewport;
					currentViewport++;
				}
		}
		
		public static List<Window> GetWindows ()
		{
			if (window_list == null || window_list_update_needed) {
				window_list_update_needed = false;
				window_list = new List<Window> (Wnck.Screen.Default.WindowsStacked);
				
				ActiveViewport.CleanRestoreStates ();
			}
			
			return window_list;
		}
	}
}
