//  
//  Copyright (C) 2010 Rico Tzschichholz
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
using System.Diagnostics;

using DBus;
using org.freedesktop.DBus;

using Mono.Unix;
using GLib;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace SessionManager
{
	public class SystemManager
	{
		const string UPowerName = "org.freedesktop.UPower";
		const string UPowerPath = "/org/freedesktop/UPower";
		const string UPowerIface = "org.freedesktop.UPower";

		const string SystemdName = "org.freedesktop.login1";
		const string SystemdPath = "/org/freedesktop/login1";
		const string SystemdIface = "org.freedesktop.login1.Manager";

		const string ConsoleKitName = "org.freedesktop.ConsoleKit";
		const string ConsoleKitPath = "/org/freedesktop/ConsoleKit/Manager";
		const string ConsoleKitIface = "org.freedesktop.ConsoleKit.Manager";

		IBus SystemBus;

		FileMonitor reboot_required_monitor;
		
		public event EventHandler BusChanged;
		public event EventHandler CapabilitiesChanged;
		public event EventHandler RebootRequired;
		
		[Interface (UPowerIface)]
		interface IUPower : org.freedesktop.DBus.Properties
		{
			bool HibernateAllowed ();
			bool SuspendAllowed ();
			void Hibernate ();
			void Suspend ();

			//bool CanHibernate { get; }
			//bool CanSuspend { get; } 
			
			event Action Changed;
		}

		[Interface (SystemdIface)]
		interface ISystemd
		{
			string CanHibernate ();
			string CanSuspend ();
			string CanPowerOff ();
			string CanReboot ();

			void PowerOff (bool interactive);
			void Reboot (bool interactive);
			void Suspend (bool interactive);
			void Hibernate (bool interactive);
		}

		[Interface (ConsoleKitIface)]
		interface IConsoleKit
		{
			bool CanStop ();
			bool CanRestart ();

			void Stop ();
			void Restart ();
		}

		bool GetBoolean (org.freedesktop.DBus.Properties dbusobj, string path, string propname) 
		{
			try {
				return Boolean.Parse (dbusobj.Get (path, propname).ToString ());
			} catch (Exception) {
				return false;
			}
		}
		
		IUPower upower;
		ISystemd systemd;
		IConsoleKit consolekit;
		
		private static SystemManager instance;

		public static SystemManager GetInstance ()
		{
			if (instance == null)
				instance = new SystemManager ();
			return instance;
		}

		private SystemManager ()
		{
			try {
				SystemBus = Bus.System.GetObject<IBus> ("org.freedesktop.DBus", new ObjectPath ("/org/freedesktop/DBus"));
				
				SystemBus.NameOwnerChanged += delegate(string name, string old_owner, string new_owner) {
					if (name != UPowerName && name != SystemdName && name != ConsoleKitName)
						return;

					Log<SystemManager>.Debug ("DBus services changed, reconnecting now");
					
					if (upower != null)
						upower = null;
					
					if (systemd != null)
						systemd = null;

					if (consolekit != null)
						consolekit = null;
					
					Initialize ();
					HandlePowerBusChanged ();
					HandleCapabilitiesChanged ();
				};
				
				Initialize ();
				
				// Set up file monitor to watch for reboot_required file
				GLib.File reboot_required_file = FileFactory.NewForPath ("/var/run/reboot-required");
				reboot_required_monitor = reboot_required_file.Monitor (FileMonitorFlags.None, null);
				reboot_required_monitor.RateLimit = 10000;
				reboot_required_monitor.Changed += HandleRebootRequired;
			} catch (Exception e) {
				Log<SessionManagerItem>.Error (e.Message);
			}
		}
		
		void Initialize ()
		{
			try {
				if (upower == null && Bus.System.NameHasOwner (UPowerName)) {
					upower = Bus.System.GetObject<IUPower> (UPowerName, new ObjectPath (UPowerPath));
					upower.Changed += HandleCapabilitiesChanged;
					Log<SystemManager>.Debug ("Using UPower dbus service");
				}
				
				if (systemd == null && Bus.System.NameHasOwner (SystemdName)) {
					systemd = Bus.System.GetObject<ISystemd> (SystemdName, new ObjectPath (SystemdPath));
					Log<SystemManager>.Debug ("Using login1.Manager dbus service");
				} else if (consolekit == null && Bus.System.NameHasOwner (ConsoleKitName)) {
					consolekit = Bus.System.GetObject<IConsoleKit> (ConsoleKitName, new ObjectPath (ConsoleKitPath));
					Log<SystemManager>.Debug ("Using ConsoleKit.Manager dbus service");
				}
			} catch (Exception e) {
				Log<SystemService>.Error ("Could not initialize needed dbus service: '{0}'", e.Message);
				Log<SystemService>.Info (e.StackTrace);
			}
		}

		void HandleCapabilitiesChanged ()
		{
			if (CapabilitiesChanged != null)
				CapabilitiesChanged (this, EventArgs.Empty);
		}

		void HandlePowerBusChanged ()
		{
			if (BusChanged != null)
				BusChanged (this, EventArgs.Empty);
		}

		void HandleRebootRequired (object sender, EventArgs e)
		{
			if (RebootRequired != null)
				RebootRequired (this, EventArgs.Empty);
		}
		
		public bool CanHibernate ()
		{
			if (systemd != null)
				return String.Equals (systemd.CanHibernate (), "yes");
			else if (upower != null)
				return GetBoolean (upower, UPowerName, "CanHibernate") && upower.HibernateAllowed ();
			
			Log<SystemManager>.Debug ("No power bus available");
			return false;
		}

		public void Hibernate ()
		{
			if (systemd != null) {
				if (String.Equals (systemd.CanHibernate (), "yes"))
					systemd.Hibernate (true);
			} else if (upower != null) {
				if (GetBoolean (upower, UPowerName, "CanHibernate") && upower.HibernateAllowed ())
					upower.Hibernate ();
			} else {
				Log<SystemManager>.Debug ("No power bus available");
			}
		}

		public bool CanSuspend ()
		{
			if (systemd != null)
				return String.Equals (systemd.CanSuspend (), "yes");
			else if (upower != null)
				return GetBoolean (upower, UPowerName, "CanSuspend") && upower.SuspendAllowed ();
			
			Log<SystemManager>.Debug ("No power bus available");
			return false;
		}

		public void Suspend ()
		{
			if (systemd != null) {
				if (String.Equals (systemd.CanSuspend (), "yes"))
					systemd.Suspend (true);
			} else if (upower != null) {
				if (GetBoolean (upower, UPowerName, "CanSuspend") && upower.SuspendAllowed ())
					upower.Suspend ();
			} else {
				Log<SystemManager>.Debug ("No power bus available");
			}
		}

		public bool OnBattery ()
		{
			if (upower != null)
				return GetBoolean (upower, UPowerName, "OnBattery");
			
			Log<SystemManager>.Debug ("No power bus available");
			return false;
		}
		
		public bool OnLowBattery ()
		{
			if (upower != null)
				return GetBoolean (upower, UPowerName, "OnLowBattery");
			
			Log<SystemManager>.Debug ("No power bus available");
			return false;
		}
		
		public bool CanRestart ()
		{
			if (systemd != null)
				return String.Equals (systemd.CanReboot (), "yes");
			else if (consolekit != null)
				return consolekit.CanRestart ();
			
			Log<SystemManager>.Debug ("No consolekit or systemd bus available");
			return false;
		}

		public void Restart ()
		{
			if (systemd != null) {
				if (String.Equals (systemd.CanReboot (), "yes"))
					systemd.Reboot (true);
			} else if (consolekit != null) {
				if (consolekit.CanRestart ())
					consolekit.Restart ();
			} else {
				Log<SystemManager>.Debug ("No consolekit or systemd bus available");
			}
		}

		public bool CanStop ()
		{
			if (systemd != null)
				return String.Equals (systemd.CanPowerOff (), "yes");
			else if (consolekit != null)
				return consolekit.CanStop ();
			
			Log<SystemManager>.Debug ("No consolekit or systemd bus available");
			return false;
		}

		public void Stop ()
		{
			if (systemd != null) {
				if (String.Equals (systemd.CanPowerOff (), "yes"))
					systemd.PowerOff (true);
			} else if (consolekit != null) {
				if (consolekit.CanStop ())
					consolekit.Stop ();
			} else {
				Log<SystemManager>.Debug ("No consolekit or systemd bus available");
			}
		}
		
		public bool CanLockScreen ()
		{
			return DockServices.System.IsValidExecutable ("gnome-screensaver-command");
		}
		
		public void LockScreen ()
		{
			DockServices.System.Execute ("gnome-screensaver-command --lock");
		}
		
		public bool CanLogOut ()
		{
			return DockServices.System.IsValidExecutable ("gnome-session-quit");
		}
		
		public void LogOut ()
		{
			DockServices.System.Execute ("gnome-session-quit --logout --no-prompt");
		}
		
		public void Dispose ()
		{
			reboot_required_monitor.Cancel ();
			reboot_required_monitor.Changed -= HandleRebootRequired;
			reboot_required_monitor.Dispose ();
		}
	}
}
