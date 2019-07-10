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

using Docky.Services;

namespace NetworkManagerDocklet
{
	public enum AccessPointSecurity
	{
		None               = 0,
		PairWEP40          = 1 << 0,
		PairWEP104         = 1 << 1,
		PairTKIP           = 1 << 2,
		PairCCMP           = 1 << 3,
		GroupWEP40         = 1 << 4,
		GroupWEP104        = 1 << 5,
		GroupTKIP          = 1 << 6,
		GroupCCMP          = 1 << 7,
		KeyManagementPSK   = 1 << 8,
		KeyManagement8021X = 1 << 9,
	}
	
	public enum APFlags
	{
		None    = 0,
		Privacy = 1,
	}
	
	public class WirelessAccessPoint : DBusObject<IAccessPoint>, IComparable<WirelessAccessPoint>
	{
		public WirelessAccessPoint (string objectPath) : base("org.freedesktop.NetworkManager", objectPath)
		{
		}
		
		public string SSID {
			get {
				try {
					return System.Text.ASCIIEncoding.ASCII.GetString ((byte[]) BusObject.Get (BusName, "Ssid"));
				} catch (Exception e) {
					Log<WirelessAccessPoint>.Error (ObjectPath);
					Log<WirelessAccessPoint>.Error (e.Message);
					Log<WirelessAccessPoint>.Debug (e.StackTrace);
					return "Unknown SSID";
				}
			}
		}

		public byte Strength {
			get {
				try {
					return (byte) BusObject.Get (BusName, "Strength");
				} catch (Exception e) {
					Log<WirelessAccessPoint>.Error (ObjectPath);
					Log<WirelessAccessPoint>.Error (e.Message);
					Log<WirelessAccessPoint>.Debug (e.StackTrace);
					return (byte) 0;
				}
			}
		}
		
		public APFlags Flags {
			get {
				try {
					return (APFlags) Convert.ToInt32 (BusObject.Get (BusName, "Flags"));
				} catch (Exception e) {
					Log<WirelessAccessPoint>.Error (ObjectPath);
					Log<WirelessAccessPoint>.Error (e.Message);
					Log<WirelessAccessPoint>.Debug (e.StackTrace);
					return APFlags.None;
				}
			}
		}
		
		public AccessPointSecurity RsnFlags {
			get {
				try {
					return (AccessPointSecurity) Convert.ToInt32 (BusObject.Get (BusName, "RsnFlags"));
				} catch (Exception e) {
					Log<WirelessAccessPoint>.Error (ObjectPath);
					Log<WirelessAccessPoint>.Error (e.Message);
					Log<WirelessAccessPoint>.Debug (e.StackTrace);
					return AccessPointSecurity.None;
				}
			}
		}
		
		public AccessPointSecurity WpaFlags {
			get {
				try {
					return (AccessPointSecurity) Convert.ToInt32 (BusObject.Get (BusName, "WpaFlags"));
				} catch (Exception e) {
					Log<WirelessAccessPoint>.Error (ObjectPath);
					Log<WirelessAccessPoint>.Error (e.Message);
					Log<WirelessAccessPoint>.Debug (e.StackTrace);
					return AccessPointSecurity.None;
				}
			}
		}
		
		#region IComparable<WirelessAccessPoint>
		
		public int CompareTo (WirelessAccessPoint other)
		{
			if (this.Strength >= other.Strength)
				return -1;
			return 1;
		}
		
		#endregion
	}
}
