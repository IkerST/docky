//  
//  Copyright (C) 2010 Robert Dyer
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

using Docky.Items;
using Docky.Services.Prefs;
using Docky.Services;

namespace SessionManager
{
	public class SessionManagerItem : ProxyDockItem
	{
		static IPreferences prefs = DockServices.Preferences.Get<SessionManagerItem> ();
		
		SystemManager system_manager = SystemManager.GetInstance ();
		
		public override string UniqueID ()
		{
			return "SessionManager";
		}

		public SessionManagerItem () : base (new SessionActionsProvider (), prefs)
		{
			StripMnemonics = true;
			system_manager.RebootRequired += HandleRebootRequired;
		}
		
		void HandleRebootRequired (object sender, EventArgs e)
		{
			SessionActionsProvider provider = (SessionActionsProvider)Provider;
			
			if (system_manager.CanRestart ()) {
				SetItem (provider.restartItem, true);
				
				provider.restartItem.State &= ~ItemState.Urgent;
				provider.restartItem.State |= ItemState.Urgent;
			}
		}
		
		public override void Dispose ()
		{
			system_manager.RebootRequired -= HandleRebootRequired;
			
			base.Dispose ();
		}
	}
}
