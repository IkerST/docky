//  
//  Copyright (C) 2009 Jason Smith
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

using Gtk;

namespace Docky.Interface
{
	public class Dock : IDisposable
	{
		DockPreferences prefs;
		DockWindow window;
		
		public event EventHandler ConfigurationClick;
		
		public IDockPreferences Preferences {
			get { return prefs as IDockPreferences; }
		}
		
		public Gtk.Widget PreferencesWidget { 
			get { return Preferences as Gtk.Widget; }
		}
		
		public Dock (DockPreferences prefs)
		{
			this.prefs = prefs;
			window = new DockWindow {
				Preferences = Preferences,
			};
			window.ShowAll ();
		}
		
		public void EnterConfigurationMode ()
		{
			window.ConfigurationMode = true;
			window.ButtonPressEvent += HandleWindowButtonPressEvent;
		}

		public void LeaveConfigurationMode ()
		{
			window.ConfigurationMode = false;
			window.ButtonPressEvent -= HandleWindowButtonPressEvent;
		}
		
		public void SetActiveGlow ()
		{
			window.ActiveGlow = true;
		}
		
		public void UnsetActiveGlow ()
		{
			window.ActiveGlow = false;
		}
		
		void HandleWindowButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			if (ConfigurationClick != null)
				ConfigurationClick (this, EventArgs.Empty);
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
			prefs.Destroy ();
			prefs.Dispose ();
			
			window.Dispose ();
		}
		#endregion

	}
}
