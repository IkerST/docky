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
using System.Web;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using System.Text;
using System.IO;
using System.Collections.Specialized;
using System.Collections.Generic;

using Docky.Services;
using Docky.Services.Prefs;

namespace NPR
{

	public class NPR
	{
		const string apiKey = "MDA0NDA4MTcxMDEyNTkzNzkwMTc4ODYwYQ001";
		const string stationsUrl = "http://api.npr.org/stations";
		
		static IPreferences prefs;
		static List<Station> Stations;
		
		public static EventHandler<StationsUpdatedEventArgs> StationsUpdated;
		
		public static int[] MyStations {
			get {
				return prefs.Get<int []> ("MyStations", new int[] { });
			}
			set {
				int[] currentStations = MyStations;
	
				StationUpdateAction action;
				int station;
				
				if (value.Length > currentStations.Length) {
					action = StationUpdateAction.Added;
					station = value.Except (currentStations).First ();
				} else {
					action = StationUpdateAction.Removed;
					station = MyStations.Except (value).First ();
				}
				
				prefs.Set<int []> ("MyStations", value);
				if (StationsUpdated != null)
					StationsUpdated (null, new StationsUpdatedEventArgs (LookupStation (station), action));
			}
		}
		
		static NPR ()
		{
			prefs = DockServices.Preferences.Get <NPR> ();
			Stations = new List<Station> ();
			// create the null station
			LookupStation (-1);
		}
		
		public NPR ()
		{
		}
		
		static string BuildQueryString (string url, NameValueCollection query)
		{
			StringBuilder queryString = new StringBuilder ();
			queryString.AppendFormat ("{0}?",url);
			foreach (string key in query.Keys)
			{
				queryString.AppendFormat ("{0}={1}&", HttpUtility.UrlEncode(key),
				                          HttpUtility.UrlEncode(query[key]));
			}
			queryString.AppendFormat ("apiKey={0}", apiKey);
			return queryString.ToString ();
		}
		
		static XElement APIReturn (string url, NameValueCollection query)
		{
			// we only have to go through this song and dance if we're using a proxy.
			if (DockServices.System.UseProxy) {
				HttpWebRequest request = (HttpWebRequest) WebRequest.Create (BuildQueryString (url, query));
				request.Timeout = 60000;
				request.UserAgent = DockServices.System.UserAgent;
				request.Proxy = DockServices.System.Proxy;
				
				XElement xml;
				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse ()) {
					try {
						using (TextReader reader = new StreamReader (response.GetResponseStream ()))
							xml = XElement.Load (reader);
					} finally {
						response.Close ();
					}
				}
				return xml;
			} else {
				return XElement.Load (BuildQueryString (url, query));
			}
		}
		
		public static IEnumerable<Station> SearchStations (string zip) 
		{
			NameValueCollection query = new NameValueCollection ();
			query["zip"] = zip;
				
			XElement result = APIReturn (stationsUrl, query);
			
			if (result.Elements ("station").Any (e => e.HasAttributes))
				return result.Elements ("station").Select (e => LookupStation (int.Parse (e.Attribute ("id").Value)));
						
			return new [] { LookupStation (-1) };
		}
		
		public static XElement StationXElement (int id)
		{
			NameValueCollection query = new NameValueCollection ();
			query["id"] = id.ToString ();
			
			return APIReturn (stationsUrl, query).Element ("station");
		}
		
		public static Station LookupStation (int id)
		{
			lock (Stations) {
				if (Stations.Where (s => s.ID == id).Any ())
					return Stations.First (s => s.ID == id);
			
				//the station wasn't in our list, so create one and add it
				Station station = new Station (id);
				Stations.Add (station);
				return station;
			}
		}
	}
}
