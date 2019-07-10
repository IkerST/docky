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
using System.Collections.Generic;

using DBus;
using org.freedesktop.DBus;

namespace NetworkManagerDocklet
{
	public class NetworkConnection : DBusObject<INetworkConnection>
	{
		public class NetworkConnectionRemovedArgs : EventArgs
		{
			public string ConnectionName { get; protected set; }
			
			public NetworkConnectionRemovedArgs (string connectionName)
			{
				ConnectionName = connectionName;
			}
		}
		
		public delegate void ConnectionRemovedHandler (object o, NetworkConnectionRemovedArgs args);
		
		//public event ConnectionRemovedHandler ConnectionRemoved;		
		
		public ConnectionOwner Owner { get; private set; }
		
		public IDictionary<string, IDictionary<string, object>> Settings {
			get { return BusObject.GetSettings (); }
		}
		
		private IDictionary<string, object> Connection {
			get { return this.Settings["connection"]; }
		}
		
		public string ConnectionName {
			get { return this.Connection["id"].ToString (); }
		}
		
		public NetworkConnection(string busName, string objectPath, ConnectionOwner owner) : base (busName, objectPath)
		{
			this.Owner = owner;
			//Workaround for bad signals from NM
			//BusObject.Removed += OnDeviceRemoved;
		}

		/*
		void OnDeviceRemoved()
		{
			if (ConnectionRemoved != null)
				ConnectionRemoved (this, new NetworkConnectionRemovedArgs (ConnectionName));
		}
		*/
	}
}
