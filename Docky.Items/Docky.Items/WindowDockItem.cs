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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Cairo;
using Gdk;
using Wnck;

using Docky.Menus;
using Docky.Services;
using Docky.Services.Windows;

namespace Docky.Items
{
	public class WindowDockItem : WnckDockItem
	{
		Wnck.Window base_window;
		string id;
		
		public WindowDockItem (Wnck.Window baseWindow)
		{
			base_window = baseWindow;
			
			id = baseWindow.Name + baseWindow.Pid;
			UpdateWindows (baseWindow);
			SetNameAndIcon ();
			
			Wnck.Screen.Default.WindowOpened += WnckScreenDefaultWindowOpened;
			Wnck.Screen.Default.WindowClosed += WnckScreenDefaultWindowClosed;
		}
		
		public override string UniqueID ()
		{
			return id;
		}

		void WnckScreenDefaultWindowOpened (object o, WindowOpenedArgs args)
		{
			UpdateWindows (base_window);
			SetNameAndIcon ();
		}

		void WnckScreenDefaultWindowClosed (object o, WindowClosedArgs args)
		{
			if (base_window == args.Window)
				base_window = ManagedWindows
					.Where (w => w != args.Window)
					.DefaultIfEmpty (null)
					.FirstOrDefault ();
			
			UpdateWindows (base_window);
			SetNameAndIcon ();
		}
		
		void SetNameAndIcon ()
		{
			if (!ManagedWindows.Any ())
				return;
			
			if (ShowHovers) {
				if (ManagedWindows.Count () > 1 && Windows.First ().ClassGroup != null)
					HoverText = ManagedWindows.First ().ClassGroup.Name;
				else
					HoverText = ManagedWindows.First ().Name;
			}
			
			SetIconFromPixbuf (base_window.Icon);
		}
		
		void UpdateWindows (Wnck.Window baseWindow)
		{
			if (baseWindow != null)
				Windows = DockServices.WindowMatcher.SimilarWindows (baseWindow)
					.Where (w => !FileApplicationProvider.ManagedWindows.Contains (w));
			else
				Windows = Enumerable.Empty<Wnck.Window> ();
		}
		
		public override void Dispose ()
		{
			Wnck.Screen.Default.WindowOpened -= WnckScreenDefaultWindowOpened;
			Wnck.Screen.Default.WindowClosed -= WnckScreenDefaultWindowClosed;
			
			base.Dispose ();
		}		
	}
}
