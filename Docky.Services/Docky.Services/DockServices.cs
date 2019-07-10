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

namespace Docky.Services
{
	public class DockServices
	{
		public static DrawingService Drawing { get; private set; }
		
		public static PreferencesService Preferences { get; private set; }
		
		public static SystemService System { get; private set; }
		
		public static HelperService Helpers { get; private set; }
		
		public static PathsService Paths { get; private set; }
		
		public static ThemeService Theme { get; private set; }
		
		public static DesktopItemService DesktopItems { get; private set; }
		
		public static WindowMatcherService WindowMatcher { get; private set; }
		
		static DockServices ()
		{
			Paths = new PathsService ();
			Paths.Initialize ();
			Drawing = new DrawingService ();
			Preferences = new PreferencesService ();
			System = new SystemService ();
			Helpers = new HelperService ();
			Theme = new ThemeService ();
			WindowMatcher = new WindowMatcherService ();
			DesktopItems = new DesktopItemService ();
		}
		
		public static void Init (bool disableHelpers)
		{
			System.Initialize ();
			if (!disableHelpers)
				Helpers.Initialize ();
			Theme.Initialize ();
			WindowMatcher.Initialize ();
			DesktopItems.Initialize ();
			NotificationService.Initialize ();
			
			Log<DockServices>.Info ("Dock services initialized.");
		}
		
		public static void Dispose ()
		{
			Helpers.Dispose ();
		}
	}
}
