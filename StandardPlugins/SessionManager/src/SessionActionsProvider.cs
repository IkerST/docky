//  
//  Copyright (C) 2010 Claudio Melis, Rico Tzschichholz, Robert Dyer
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

using Mono.Unix;

using Docky.Items;

namespace SessionManager
{
	public class SessionActionsProvider : AbstractDockItemProvider
	{
		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "SessionManagerActions";
			}
		}
		
		#endregion

		SystemManager system_manager = SystemManager.GetInstance ();

		AbstractDockItem lockItem;
		AbstractDockItem logoutItem;
		AbstractDockItem suspendItem;
		AbstractDockItem hibernateItem;
		public AbstractDockItem restartItem;
		AbstractDockItem shutdownItem;

		void UpdateItems ()
		{
			List<AbstractDockItem> items = new List<AbstractDockItem> ();

			if (system_manager.CanLockScreen ())
				items.Add (lockItem);
			
			if (system_manager.CanLogOut ())
				items.Add (logoutItem);
			
			if (system_manager.CanSuspend ())
				items.Add (suspendItem);
			
			if (system_manager.CanHibernate ())
				items.Add (hibernateItem);
			
			if (system_manager.CanRestart ())
				items.Add (restartItem);
			
			if (system_manager.CanStop ())
				items.Add (shutdownItem);

			Items = items;
		}

		void CreateItems ()
		{
			lockItem = new SessionManagerActionItem ("system-lock-screen", Catalog.GetString ("L_ock Screen"), () => { 
				system_manager.LockScreen ();
			});
		
			logoutItem = new SessionManagerActionItem ("system-log-out", Catalog.GetString ("_Log Out..."), () => { 
				SessionManagerActionItem.ShowConfirmationDialog (Catalog.GetString ("Log Out"), 
										Catalog.GetString ("Are you sure you want to close all programs and log out of the computer?"), 
										"system-log-out", 
										system_manager.LogOut);
			});
		
			suspendItem = new SessionManagerActionItem ("system-suspend", Catalog.GetString ("_Suspend"), () => { 
				system_manager.LockScreen ();
				system_manager.Suspend (); 
			});
		
			hibernateItem = new SessionManagerActionItem ("system-hibernate", Catalog.GetString ("_Hibernate"), () => { 
				system_manager.LockScreen ();
				system_manager.Hibernate (); 
			});
		
			restartItem = new SessionManagerActionItem ("system-restart", Catalog.GetString ("_Restart..."), () => { 
				SessionManagerActionItem.ShowConfirmationDialog (Catalog.GetString ("Restart"), 
										Catalog.GetString ("Are you sure you want to close all programs and restart the computer?"), 
										"system-restart", 
										() => system_manager.Restart ());
			});
		
			shutdownItem = new SessionManagerActionItem ("system-shutdown", Catalog.GetString ("Shut _Down..."), () => { 
				SessionManagerActionItem.ShowConfirmationDialog (Catalog.GetString ("Shut Down"), 
										Catalog.GetString ("Are you sure you want to close all programs and shut down the computer?"), 
										"system-shutdown", 
										() => system_manager.Stop ());
			});
		}

		public SessionActionsProvider ()
		{
			CreateItems ();

			system_manager.CapabilitiesChanged += HandlePowermanagerCapabilitiesChanged;

			UpdateItems ();
		}
		
		void HandlePowermanagerCapabilitiesChanged (object sender, EventArgs e)
		{
			UpdateItems ();
		}

		public override void Dispose ()
		{
			system_manager.CapabilitiesChanged -= HandlePowermanagerCapabilitiesChanged;
			system_manager.Dispose ();
			
			Items = Enumerable.Empty<AbstractDockItem> ();
			lockItem.Dispose ();
			logoutItem.Dispose ();
			suspendItem.Dispose ();
			hibernateItem.Dispose ();
			restartItem.Dispose ();
			shutdownItem.Dispose ();
			
			base.Dispose ();
		}
	}
}
