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

using Gdk;
using Gtk;

using Mono.Unix;

using Docky.Items;
using Docky.Interface;
using Docky.Services;
using Docky.Services.Prefs;

namespace Docky
{
	class DockMonitor
	{
		public Rectangle Geo { get; set; }
		public int MonitorNumber { get; set; }
		public IEnumerable<DockPosition> PossiblePositions { get; set; }
	}

	internal class DockController : IDisposable
	{
		IPreferences prefs = DockServices.Preferences.Get<DockController> ();
		
		List<Dock> docks;
		public IEnumerable<Dock> Docks { 
			get { return docks.AsEnumerable (); }
		}
		
		public int NumDocks {
			get { return DockNames.Count (); }
		}
		
		List<DockMonitor> DockMonitors { get; set; }

		public IEnumerable<DockPosition> PositionsAvailableForDock (int monitorNum)
		{
			if (DockMonitors.Count () != Gdk.Screen.Default.NMonitors)
				DetectMonitors ();
			
			foreach (DockPosition position in DockMonitors.Where (d => d.MonitorNumber == monitorNum).First ().PossiblePositions) {
				if (!DocksForMonitor (monitorNum).Any (dock => dock.Preferences.Position == position))
					yield return position;
			}
		}

		public IEnumerable<Dock> DocksForMonitor (int monitorNumber)
		{
			return docks.Where (d => d.Preferences.MonitorNumber == monitorNumber);
		}
		
		IEnumerable<string> DockNames {
			get {
				return prefs.Get<string []> ("ActiveDocks", new [] {"Dock1"}).AsEnumerable ().Take (4);
			}
			set {
				prefs.Set<string []> ("ActiveDocks", value.ToArray ());
			}
		}
		
		public bool CompositeCheckEnabled {
			get { return prefs.Get<bool> ("CompositeCheckEnabled", true); }
		}
		
		public void Initialize ()
		{
			docks = new List<Dock> ();
			
			DetectMonitors ();
			CreateDocks ();
			
			EnforceWindowManager ();
			EnsurePluginState ();
			
			GLib.Timeout.Add (500, delegate {
				EnsurePluginState ();
				return false;
			});
		}
		
		void DetectMonitors ()
		{
			DockMonitors = new List<DockMonitor> ();
			
			// first add all of the screens and their geometries
			for (int i = 0; i < Screen.Default.NMonitors; i++) {
				DockMonitor mon = new DockMonitor ();
				mon.MonitorNumber = i;
				mon.Geo = Screen.Default.GetMonitorGeometry (i);
				DockMonitors.Add (mon);
			}
			
			int topDockVal = DockMonitors.OrderBy (d => d.Geo.Top).First ().Geo.Top;
			int bottomDockVal = DockMonitors.OrderByDescending (d => d.Geo.Bottom).First ().Geo.Bottom;
			int leftDockVal = DockMonitors.OrderBy (d => d.Geo.Left).First ().Geo.Left;
			int rightDockVal = DockMonitors.OrderByDescending (d => d.Geo.Right).First ().Geo.Right;
			
			// now build the list of available positions for a given screen.
			for (int i = 0; i < DockMonitors.Count (); i++) {
				List<DockPosition> positions = new List<DockPosition> ();
				DockMonitor mon = DockMonitors.Where (d => d.MonitorNumber == i).First ();
				
				if (mon.Geo.Left == leftDockVal)
					positions.Add (DockPosition.Left);
				if (mon.Geo.Right == rightDockVal)
					positions.Add (DockPosition.Right);
				if (mon.Geo.Top == topDockVal)
					positions.Add (DockPosition.Top);
				if (mon.Geo.Bottom == bottomDockVal)
					positions.Add (DockPosition.Bottom);
				
				mon.PossiblePositions = positions;
			}
		}
		
		public Dock CreateDock ()
		{
			int mon;
			for (mon = 0; mon < Screen.Default.NMonitors; mon++) {
				if (PositionsAvailableForDock (mon).Any ())
					break;
				if (mon == Screen.Default.NMonitors - 1)
					return null;
			}
			
			string name = "Dock" + 1;
			for (int i = 2; DockNames.Contains (name); i++)
				name = "Dock" + i;
			
			DockNames = DockNames.Concat (new[] { name });
			
			DockPreferences dockPrefs = new DockPreferences (name, mon);
			dockPrefs.Position = PositionsAvailableForDock (mon).First ();
			Dock dock = new Dock (dockPrefs);
			docks.Add (dock);
			
			return dock;
		}
		
		public bool DeleteDock (Dock dock)
		{
			if (!docks.Contains (dock) || docks.Count == 1)
				return false;
			
			docks.Remove (dock);
			if (dock.Preferences.DefaultProvider.IsWindowManager)
				docks.First ().Preferences.DefaultProvider.SetWindowManager ();
			dock.Preferences.FreeProviders ();
			dock.Preferences.ResetPreferences ();
			dock.Dispose ();
			DockNames = DockNames.Where (s => s != dock.Preferences.GetName ());
			
			return true;
		}
		
		void CreateDocks ()
		{
			foreach (string name in DockNames) {
				DockPreferences dockPrefs = new DockPreferences (name);
				Dock dock = new Dock (dockPrefs);
				docks.Add (dock);
			}
		}
		
		void EnforceWindowManager ()
		{
			bool hasWm = false;
			
			foreach (Dock dock in docks)
				if (dock.Preferences.DefaultProvider.IsWindowManager){
					hasWm = true;
					break;
				}
			
			if (!hasWm)
				docks.First ().Preferences.DefaultProvider.SetWindowManager ();
		}
		
		void EnsurePluginState ()
		{
			foreach (AbstractDockItemProvider provider in PluginManager.ItemProviders)
				if (!docks.Any (d => d.Preferences.ItemProviders.Contains (provider))) {
					Log<DockController>.Warn ("\"{0}\" seems to have been abandoned... disabling.", provider.Name);
					PluginManager.Disable (provider);
				}
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
			foreach (Dock d in Docks) {
				d.Dispose ();
			}
		}
		#endregion
	}
}
