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
using System.Collections.Generic;

using Docky.Items;
using Docky.Services;

using DBus;

namespace BatteryMonitor
{
	public class BatteryMonitorUPowerItem : BatteryMonitorAbstractItem
	{
		const string UPowerName = "org.freedesktop.UPower";
		const string UPowerPath = "/org/freedesktop/UPower";
		const string UPowerDeviceName = "org.freedesktop.UPower.Device";
		
		[Interface(UPowerDeviceName)]
		interface IUPowerDevice : org.freedesktop.DBus.Properties
		{
		}
		
		[Interface(UPowerName)]
		interface IUPower
		{
			event Action Changed;
			string[] EnumerateDevices ();
		}
		
		IUPower upower;
		List<IUPowerDevice> devices = new List<IUPowerDevice> ();
		
		public static bool Available {
			get {
				return Bus.System.NameHasOwner (UPowerName);
			}
		}
		
		public BatteryMonitorUPowerItem (AbstractDockItemProvider owner) : base(owner)
		{
			try {
				upower = Bus.System.GetObject<IUPower> (UPowerName, new ObjectPath (UPowerPath));
				upower.Changed += HandleUPowerChanged;
				EnumerateDevices ();
			} catch (Exception e) {
				Log<SystemService>.Error ("Could not initialize power manager dbus: '{0}'", e.Message);
				Log<SystemService>.Info (e.StackTrace);
			}
		}
		
		void HandleUPowerChanged ()
		{
			if (upower != null)
				EnumerateDevices ();
		}
		
		double GetDouble (org.freedesktop.DBus.Properties dbusobj, string path, string propname) 
		{
			try {
				return Double.Parse (dbusobj.Get (path, propname).ToString ());
			} catch {
				return 0;
			}
		}
		
		int GetInt (org.freedesktop.DBus.Properties dbusobj, string path, string propname) 
		{
			try {
				return Int32.Parse (dbusobj.Get (path, propname).ToString ());
			} catch {
				return 0;
			}
		}
		
		uint GetUInt (org.freedesktop.DBus.Properties dbusobj, string path, string propname) 
		{
			try {
				return UInt32.Parse (dbusobj.Get (path, propname).ToString ());
			} catch {
				return 0;
			}
		}
		
		double GetEnergy (IUPowerDevice device)
		{
			return GetDouble (device, UPowerDeviceName, "Energy");
		}
		
		double GetEnergyFull (IUPowerDevice device)
		{
			return GetDouble (device, UPowerDeviceName, "EnergyFull");
		}
		
		int GetTimeToEmpty (IUPowerDevice device)
		{
			return GetInt (device, UPowerDeviceName, "TimeToEmpty");
		}
		
		int GetTimeToFull (IUPowerDevice device)
		{
			return GetInt (device, UPowerDeviceName, "TimeToFull");
		}
		
		uint GetState (IUPowerDevice device)
		{
			return GetUInt (device, UPowerDeviceName, "State");
		}
		
		uint GetType (IUPowerDevice device)
		{
			return GetUInt (device, UPowerDeviceName, "Type");
		}
		
		void EnumerateDevices ()
		{
			devices = new List<IUPowerDevice> ();
			
			foreach (string s in upower.EnumerateDevices ()) {
				IUPowerDevice device = Bus.System.GetObject<IUPowerDevice> (UPowerName, new ObjectPath (s));
				
				// only want batteries
				if (GetType (device) != 2)
					continue;
				
				devices.Add (device);
			}
		}
		
		protected override void GetMaxBatteryCapacity ()
		{
			if (upower != null) {
				List<IUPowerDevice> batt_devices = devices;
				foreach (IUPowerDevice device in batt_devices)
					max_capacity += (int) GetEnergyFull (device);
			}
			
			max_capacity = Math.Max (1, max_capacity);
		}
		
		int time_to_empty;
		int time_to_full;
		
		protected override double GetBatteryTime (bool charging)
		{
			if (charging)
				return (double) time_to_full / 3600.0;
			
			return (double) time_to_empty / 3600.0;
		}
		
		protected override bool GetCurrentBatteryCapacity ()
		{
			bool charging = false;
			time_to_empty = 0;
			time_to_full = 0;
			
			if (upower != null) {
				List<IUPowerDevice> batt_devices = devices;
				foreach (IUPowerDevice device in batt_devices) {
					current_capacity += (int) GetEnergy (device);
					time_to_empty += GetTimeToEmpty (device);
					time_to_full += GetTimeToFull (device);
					
					// 0: Unknown, 1: Charging, 2: Discharging, 3: Empty, 
					// 4: Fully charged, 5: Pending charge, 6: Pending discharge
					uint state = GetState (device);
					if (state == 1 || state == 5) // charging
						charging = true;
				}
			}
			
			if (time_to_empty > 0 || time_to_full > 0)
				current_rate = 1;
			
			return charging;
		}
		
		public override void Dispose ()
		{
			if (upower != null)
				upower.Changed -= HandleUPowerChanged;
			base.Dispose ();
		}
	}
}
