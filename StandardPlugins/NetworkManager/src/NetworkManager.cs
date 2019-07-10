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
using System.Diagnostics;
using System.Collections.Generic;

using DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	public class NetworkManager
	{
		public delegate void DeviceStateChangedHandler (object sender, DeviceStateChangedArgs args);
		
		public event DeviceStateChangedHandler DeviceStateChanged;
		
		public ConnectionManager ConManager { get; private set; }
		public DeviceManager DevManager { get; private set; }
		
		public IEnumerable<NetworkConnection> ActiveConnections {
			get {
				foreach (string active in DevManager.ActiveConnections) {
					DBusObject<IActiveConnection> ActiveConnection = new DBusObject<IActiveConnection> ("org.freedesktop.NetworkManager", active);
					if (ActiveConnection.BusObject.Get (ActiveConnection.BusName, "ServiceName").ToString ().Contains ("System"))
						yield return ConManager.SystemConnections.Where (con => con.ObjectPath == ActiveConnection.BusObject.Get (ActiveConnection.BusName, "Connection").ToString ()).First ();
					else
						yield return ConManager.UserConnections.Where (con => con.ObjectPath == ActiveConnection.BusObject.Get (ActiveConnection.BusName, "Connection").ToString ()).First ();
				}
			}
		}

		public NetworkManager ()
		{
			ConManager = new ConnectionManager ();
			DevManager = new DeviceManager ();
			
			DevManager.NetworkDevices.ForEach (dev => dev.StateChanged += OnDevStateChanged);
		}
		
		public void ConnectTo (WirelessAccessPoint ap)
		{
			NetworkConnection connection;
			
			try {
				connection = ConManager.AllConnections.OfType<WirelessConnection> ().Where (con => (con as WirelessConnection).SSID == ap.SSID).First ();
				ConnectTo (connection);
			} catch {
				// FIXME We're trying to connect to an AP but no connection entry exists.
				// If we can figure out how to manually create a connection behind the scenes, we can remove this.
				Docky.Services.DockServices.System.RunOnThread ( () => {
					Process.Start ("nm-connection-editor --type=802-11-wireless");
				});
			}
		}
		
		public void ConnectTo (NetworkConnection con)
		{
			//Console.WriteLine ("Connecting to {0}", con.ConnectionName);
			
			NetworkDevice dev; 
			string specObj;
			if (con is WirelessConnection) {
				dev = DevManager.NetworkDevices.OfType<WirelessDevice> ().First ();
				specObj = (dev as WirelessDevice).APBySSID ((con as WirelessConnection).SSID).ObjectPath;
			} else if (con is WiredConnection) {
				dev = DevManager.NetworkDevices.OfType<WiredDevice> ().First ();
				specObj = "/";
			} else {
				return;
			}
			
			string serviceName;
			if (con.Owner == ConnectionOwner.System)
				serviceName = "org.freedesktop.NetworkManagerSystemSettings";
			else
				serviceName = "org.freedesktop.NetworkManagerUserSettings";
			string conStr = con.ObjectPath;
			
			DevManager.BusObject.ActivateConnection(serviceName, new ObjectPath (conStr), new ObjectPath (dev.ObjectPath), new ObjectPath (specObj));
		}

		void OnDevStateChanged(object o, DeviceStateChangedArgs args)
		{
			if (DeviceStateChanged != null)
				DeviceStateChanged (o as NetworkDevice, args);
		}
	}
}
