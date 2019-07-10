//  
//  Copyright (C) 2009 Chris Szikszoy
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
using System.Linq;
using System.Collections.Generic;

using DBus;

namespace NetworkManagerDocklet
{
	public class DeviceManager : DBusObject<INetManager>
	{
		public IEnumerable<string> ActiveConnections {
			get {
				foreach (ObjectPath conPath in (ObjectPath[]) BusObject.Get (BusName, "ActiveConnections"))
					yield return conPath.ToString ();
			}
		}
		
		public DeviceManager() : base ("org.freedesktop.NetworkManager", "/org/freedesktop/NetworkManager")
		{
		}
		
		public List<NetworkDevice> NetworkDevices {
			get {
				List<NetworkDevice> list = new List<NetworkDevice> ();
				
				foreach (ObjectPath objPath in BusObject.GetDevices ()) {
					NetworkDevice device = NetworkDevice.NewForObjectPath (objPath.ToString ());
					list.Add (device);
				}
				
				return list;
			}
		}
	}
}
