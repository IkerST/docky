//  
//  Copyright (C) 2009 Chris Szikszoy, Robert Dyer
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
using System.Net;
using System.Linq;

using System.Collections.Generic;

using DBus;
using org.freedesktop.DBus;

using Docky.Services;

namespace NetworkManagerDocklet
{
	public class NetworkDevice : DBusObject<INetworkDevice>
	{
		static Dictionary<string, NetworkDevice> devices = new Dictionary<string, NetworkDevice> ();
		
		public static NetworkDevice NewForObjectPath (string objectPath)
		{
			if (!devices.ContainsKey (objectPath)) {
				NetworkDevice result = new NetworkDevice (objectPath);
				if (result.DType == DeviceType.Wired) {
					result = new WiredDevice (objectPath);
				} else if (result.DType == DeviceType.Wireless) {
					result = new WirelessDevice (objectPath);
				}
				devices[objectPath] = result;
			}
			return devices[objectPath];
		}
		
		const string NMBusName = "org.freedesktop.NetworkManager";
		
		public delegate void DeviceStateChangedHandler (object o, DeviceStateChangedArgs args);
		
		public event DeviceStateChangedHandler StateChanged;

		public IPAddress IP4Address { get; private set; }
		public IPAddress PrimaryDNS { get; private set; }
		public IPAddress Gateway { get; private set; }
		public IPAddress SubnetMask { get; private set; }
		public ConnectionType ConType { get; private set; }
		
		DBusObject<IIP4Config> IP4Config { get; set; }
		
		public DeviceType DType {
			get { 
				try {
					return (DeviceType) Enum.ToObject (typeof (DeviceType), BusObject.Get (BusName, "DeviceType"));
				} catch (Exception e) {
					Log<NetworkDevice>.Error (e.Message);
					Log<NetworkDevice>.Debug (e.StackTrace);
					return DeviceType.Unknown;
				}
			}
		}
		
		public DeviceState State {
			get	{ 
				try {
					return (DeviceState) Enum.ToObject (typeof (DeviceState), BusObject.Get (BusName, "State")); 
				} catch (Exception e) {
					Log<NetworkDevice>.Error (e.Message);
					Log<NetworkDevice>.Debug (e.StackTrace);
					return DeviceState.Unknown;
				}
			}
		}
		
		protected NetworkDevice (string objectPath) : base (NMBusName, objectPath)
		{
			BusObject.StateChanged += OnStateChanged;
			SetIPs ();
		}
		
		void SetIPs ()
		{
			if (State == DeviceState.Active) {
				if (BusObject.Get (BusName, "Dhcp4Config").ToString () != "/")
					ConType = ConnectionType.Manaul;
				else
					ConType = ConnectionType.DHCP;
				IP4Config = new DBusObject<IIP4Config> (NMBusName, BusObject.Get (BusName, "Ip4Config").ToString ());
				IP4Address = new IPAddress (long.Parse (BusObject.Get (BusName, "Ip4Address").ToString ()));
				uint[][] Addresses = (uint[][]) IP4Config.BusObject.Get (IP4Config.BusName, "Addresses");
				Gateway = new IPAddress (Addresses[0][2]);
				SubnetMask = ConvertPrefixToIp ((int) Addresses[0][1]);
				uint[] NameServers = (uint[]) IP4Config.BusObject.Get (IP4Config.BusName, "Nameservers");
				if (NameServers.Length > 0)
					PrimaryDNS = new IPAddress (NameServers[0]);
				else
					PrimaryDNS = null;
			} else {
				IP4Config = null;
				ConType = ConnectionType.Unknown;
				IP4Address = null;
				PrimaryDNS = null;
				Gateway = null;
				SubnetMask = null;
			}
		}
		
		IPAddress ConvertPrefixToIp (int prefix)
		{
			uint IP = 0;
			while (prefix > 0) {
				prefix--;
				IP += (uint) (1 << prefix);
			}
			return new IPAddress (IP);
		}

		void OnStateChanged (DeviceState newState, DeviceState oldState, uint reason)
		{
			if (newState == DeviceState.Active)
				SetIPs ();
			if (StateChanged != null)
				StateChanged (this, new DeviceStateChangedArgs (newState, oldState, reason));
		}
	}
}
