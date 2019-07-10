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
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	public class WirelessDevice : NetworkDevice
	{
		public DBusObject<IWirelessDevice> WirelessProperties { get; private set; }
		
		IEnumerable<WirelessAccessPoint> accessPoints;
		IEnumerable<WirelessAccessPoint> AccessPoints {
			get {
				IEnumerable<string> paths = AccessPointPaths.Select (ap => ap.ToString ()).ToArray ();
				
				accessPoints = accessPoints
					.Where (ap => paths.Any (p => ap.ObjectPath == p))
					.Concat (paths
							.Where (p => !accessPoints.Any (x => x.ObjectPath == p))
							.Select (p => new WirelessAccessPoint (p))
							)
					.ToArray ();
				
				return accessPoints;
			}
		}
		
		public Dictionary<string, IEnumerable<WirelessAccessPoint>> VisibleAccessPoints {
			get {
				return AccessPoints
					.GroupBy (ap => ap.SSID)
					.Select (en => en.AsEnumerable ())
					.ToDictionary (en => en.First ().SSID);
			}
		}
		
		IEnumerable<ObjectPath> AccessPointPaths {
			get { return WirelessProperties.BusObject.GetAccessPoints (); }
		}
		
		internal WirelessDevice (string objectPath) : base(objectPath)
		{
			accessPoints = Enumerable.Empty<WirelessAccessPoint> ();
			
			this.WirelessProperties = new DBusObject<IWirelessDevice> ("org.freedesktop.NetworkManager", objectPath);
		}
		
		public WirelessAccessPoint APBySSID (string ssid)
		{
			// Multiple APs per SSID are sorted by strength, this should always return the strongest AP
			if (VisibleAccessPoints.ContainsKey (ssid))
				return VisibleAccessPoints[ssid].First ();
			return null;
		}

		public WirelessAccessPoint ActiveAccessPoint {
			get {
				string access = WirelessProperties.BusObject.Get (WirelessProperties.BusName, "ActiveAccessPoint").ToString ();
				return AccessPoints
					.Where (ap => ap.ObjectPath == access)
					.DefaultIfEmpty (null)
					.FirstOrDefault ();
			}
		}
		
		public string HWAddress {
			get { return WirelessProperties.BusObject.Get (WirelessProperties.BusName, "HwAddress").ToString (); }
		}
	}
}
