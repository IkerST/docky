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

using Docky.Services;

namespace NetworkManagerDocklet
{
	public class DBusObject<T>
	{
		public string BusName { get; private set; }
		public string ObjectPath { get; private set; }
		public T BusObject { get; private set; }
		
		public DBusObject (string busName, string objectPath)
		{
			this.ObjectPath = objectPath;
			this.BusName = busName;
			
			try {
				this.BusObject = Bus.System.GetObject<T> (BusName, new ObjectPath (ObjectPath));
			} catch (Exception e) {
				Log<DBusObject<T>>.Error (e.Message);
				Log<DBusObject<T>>.Debug (e.StackTrace);
			}
		}
	}
}
