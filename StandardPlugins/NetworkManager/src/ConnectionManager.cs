//  
//  Copyright (C) 2009 Chris Szikszoy
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
using System.Linq;
using System.Collections.Generic;

using DBus;
using org.freedesktop.DBus;

using Docky.Services;

namespace NetworkManagerDocklet
{
	public class ConnectionManager
	{
		const string SettingsObjectPath = "/org/freedesktop/NetworkManagerSettings";
		const string SystemBus = "org.freedesktop.NetworkManagerSystemSettings";
		const string UserBus = "org.freedesktop.NetworkManagerUserSettings";
		
		public ConnectionManager()
		{
			SystemConnectionManager = new DBusObject<IConnectionManager> (SystemBus,  SettingsObjectPath);
			//SystemConnectionManager.BusObject.NewConnection += OnConnectionAdded;
			UserConnectionManager = new DBusObject<IConnectionManager> (UserBus,  SettingsObjectPath);
			//UserConnectionManager.BusObject.NewConnection += OnConnectionAdded;
			
			UserConnections = new List<NetworkConnection> ();
			SystemConnections = new List<NetworkConnection> ();
			
			UpdateConnections ();
			//this workaround is necessary because NM emits bad signals, multiple times with bad data.
			GLib.Timeout.Add (1000*60*5, delegate { UpdateConnections(); return true; });
		}
		
		DBusObject<IConnectionManager> SystemConnectionManager { get; set; }
		DBusObject<IConnectionManager> UserConnectionManager { get; set; }
		public List<NetworkConnection> UserConnections { get; private set; }
		public List<NetworkConnection> SystemConnections { get; private set; }
		public IEnumerable<NetworkConnection> AllConnections {
			get { return UserConnections.Union (SystemConnections); }
		}
	
		
		//Commented because of some oddity in NetworkManager that emits these signals multiple times with erroneous data.
		/*
		void OnConnectionAdded (string objectPath)
		{
			Console.WriteLine ("Connection added: {0}", objectPath);
		}

		void OnNetworkConnectionRemoved (object o, NetworkConnection.NetworkConnectionRemovedArgs args)
		{
			Console.WriteLine ("connection removed: {0}", args.ConnectionName);
		}
		*/
		
		public void UpdateConnections ()
		{
			lock (SystemConnections) {
				SystemConnections.Clear ();
				try {
					foreach (string con in SystemConnectionManager.BusObject.ListConnections ())
					{
						NetworkConnection connection = new NetworkConnection (SystemBus, con, ConnectionOwner.System);
						if (connection.Settings.ContainsKey ("802-11-wireless"))
							connection = new WirelessConnection (SystemBus, con, ConnectionOwner.System);
						else if (connection.Settings.ContainsKey ("802-3-ethernet"))
							connection = new WiredConnection (SystemBus, con, ConnectionOwner.System);
						else 
							continue;
						//connection.ConnectionRemoved += OnNetworkConnectionRemoved;
						SystemConnections.Add (connection);
					}
				} catch (Exception e) {
					Log<ConnectionManager>.Error (e.Message);
					Log<ConnectionManager>.Debug (e.StackTrace);
				}
			}
			
			lock (UserConnections) {
				UserConnections.Clear ();
				try {
					foreach (string con in UserConnectionManager.BusObject.ListConnections ())
					{
						NetworkConnection connection = new NetworkConnection (UserBus, con, ConnectionOwner.User);
						if (connection.Settings.ContainsKey ("802-11-wireless"))
							connection = new WirelessConnection (UserBus, con, ConnectionOwner.User);
						else if (connection.Settings.ContainsKey ("802-3-ethernet"))
							connection = new WiredConnection (UserBus, con, ConnectionOwner.User);
						else 
							continue;
						
						//connection.ConnectionRemoved += OnNetworkConnectionRemoved;
						UserConnections.Add (connection);
					}
				} catch (Exception e) {
					Log<ConnectionManager>.Error (e.Message);
					Log<ConnectionManager>.Debug (e.StackTrace);
				}
			}
		}
	}
}
