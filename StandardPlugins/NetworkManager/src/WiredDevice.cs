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
using System.Collections.Generic;
using System.Linq;

using DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	public class WiredDevice : NetworkDevice
	{
		internal WiredDevice (string objectPath) : base (objectPath)
		{
			this.WiredProperties = new DBusObject<IWiredDevice> ("org.freedesktop.NetworkManager", objectPath);
		}
		
		public DBusObject<IWiredDevice> WiredProperties { get; private set; }
		
		public bool Carrier {
			get {
				return Boolean.Parse (WiredProperties.BusObject.Get (WiredProperties.BusName, "Carrier").ToString ());
			}
		}
		
		public string HWAddresss {
			get {
				return WiredProperties.BusObject.Get (WiredProperties.BusName, "HwAddress").ToString ();
			}
		}
	}
}
