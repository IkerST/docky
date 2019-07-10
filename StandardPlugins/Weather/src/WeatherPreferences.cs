//  
//  Copyright (C) 2009 Robert Dyer
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
using System.Globalization;

using Docky.Services;
using Docky.Services.Prefs;

namespace WeatherDocklet
{
	/// <summary>
	/// Handles all weather preferences.
	/// </summary>
	internal class WeatherPreferences
	{
		const string LocationKey = "Location";
		const string TimeoutKey = "Timeout";
		const string SourceKey = "Source";
		const string MetricKey = "Metric";
		
		static IPreferences prefs = DockServices.Preferences.Get<WeatherPreferences> ();
		
		/// <value>
		/// Indicates the Source preference changed.
		/// </value>
		public static event EventHandler SourceChanged;
		
		/// <value>
		/// Called to indicate the Source preference changed.
		/// </value>
		public static void OnSourceChanged ()
		{
			if (SourceChanged != null)
				SourceChanged (null, EventArgs.Empty);
		}
		
		/// <value>
		/// The current weather source name.
		/// </value>
		public static string Source {
 			get { return prefs.Get<string> (SourceKey, WunderWeatherSource.GetInstance ().Name); }
 			set {
				if (value == Source) return;
				prefs.Set<string> (SourceKey, value);
				OnSourceChanged ();
			}
 		}
		
		/// <value>
		/// Indicates the Location preference changed.
		/// </value>
		public static event EventHandler LocationsChanged;
		
		/// <value>
		/// Called to indicate the Location preference changed.
		/// </value>
		public static void OnLocationsChanged ()
		{
			if (LocationsChanged != null)
				LocationsChanged (null, EventArgs.Empty);
		}
		
		/// <value>
		/// The current weather location.
		/// </value>
		public static string[] Locations {
 			get { return prefs.Get<string[]> (LocationKey, new string[] {}); }
 			set {
				if (value == Locations) return;
				prefs.Set<string[]> (LocationKey, value);
				OnLocationsChanged ();
			}
 		}
		
		/// <value>
		/// Indicates the Timeout preference changed.
		/// </value>
		public static event EventHandler TimeoutChanged;
		
		/// <value>
		/// Called to indicate the Timeout preference changed.
		/// </value>
		public static void OnTimeoutChanged ()
		{
			if (TimeoutChanged != null)
				TimeoutChanged (null, EventArgs.Empty);
		}
		
		/// <value>
		/// The current weather reload timeout.
		/// </value>
		public static uint Timeout {
 			get { return (uint) prefs.Get<int> (TimeoutKey, 30); }
 			set {
				if (value == Timeout) return;
				prefs.Set<int> (TimeoutKey, (int) value);
				OnTimeoutChanged ();
			}
 		}
		
		/// <value>
		/// Indicates the Metric preference changed.
		/// </value>
		public static event EventHandler MetricChanged;
		
		/// <summary>
		/// Called to indicate the Metric preference changed.
		/// </summary>
		public static void OnMetricChanged ()
		{
			if (MetricChanged != null)
				MetricChanged (null, EventArgs.Empty);
		}
		
		static bool? MetricDefault;
		
		/// <value>
		/// If metric units are used.
		/// </value>
		public static bool Metric {
 			get { 
				if (!MetricDefault.HasValue)
					MetricDefault = RegionInfo.CurrentRegion != null && RegionInfo.CurrentRegion.IsMetric;
				return prefs.Get<bool> (MetricKey, false); }
 			set {
				if (value == Metric) return;
				prefs.Set<bool> (MetricKey, value);
				OnMetricChanged ();
			}
 		}
	}
}
