//  
//  Copyright (C) 2011 Florian Dorn, Rico Tzschichholz, Robert Dyer
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
using System.IO;
using System.Text.RegularExpressions;

using Gdk;
using GLib;

namespace NetworkMonitorDocklet
{
	enum OutputDevice
	{
		AUTO = 0
	}
	
	class NetworkMonitor
	{
		Dictionary<string, DeviceInfo> devices = new Dictionary<string, DeviceInfo> ();
		
		public NetworkMonitor ()
		{
		}

		public void PrintDevices ()
		{
			foreach (DeviceInfo device in devices.Values)
				Console.WriteLine (device.ToString ());
		}
		
		public void UpdateDevices ()
		{
			try  {
				using (StreamReader reader = new StreamReader ("/proc/net/dev")) {
					while (!reader.EndOfStream)
						ParseDeviceInfoString (reader.ReadLine ());
					reader.Close ();
				}
			} catch {
				// we dont care
			}
		}
		
		public void ParseDeviceInfoString (string line)
		{
			if (string.IsNullOrEmpty (line) || !line.Contains (':'))
				return;
			
			string[] parts = line.Split (new char[] {':'}, 2);
			string devicename = parts[0].Trim ();
			
			if (devicename == "lo")
				return;
			
			//             Receive                                                 Transmit
			// interface : bytes packets errs drop fifo frame compressed multicast bytes packets errs drop fifo colls carrier compressed
			// So we need fields 0 (bytes-sent) and 8 (bytes-received)
			string[] values = parts[1].Trim ().Split (new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
			if (values.Length <= 8)
				return;
			
			long downloadedBytes = Convert.ToInt64 (values [0].Trim ());
			long uploadedBytes = Convert.ToInt64 (values [8].Trim ());
			
			DeviceInfo d;
			if (devices.TryGetValue (devicename, out d))
				d.Update (downloadedBytes, uploadedBytes);
			else
				devices.Add (devicename, new DeviceInfo (devicename, downloadedBytes, uploadedBytes));
		}
		
		public DeviceInfo GetDevice (OutputDevice n)
		{
			if (n != OutputDevice.AUTO)
				return null;
			
			DeviceInfo d = devices.Values.OrderByDescending (dev => dev.TotalBytes).FirstOrDefault ();
			foreach (DeviceInfo device in devices.Values)
				if (d == null || device.TotalRate > d.TotalRate)
					d = device;
			
			return d;
		}
	}
}
