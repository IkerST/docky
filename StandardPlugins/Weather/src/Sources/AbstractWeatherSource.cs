//  
//  Copyright (C) 2009 Robert Dyer
//  Copyright (C) 2010 Robert Dyer, Rico Tzschichholz
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
using System.Net;
using System.Threading;
using System.Web;
using System.Xml;

using Mono.Unix;

using Docky.Services;

namespace WeatherDocklet
{
	public abstract class AbstractWeatherSource
	{
		public class WeatherCachedXMLData
		{
			public XmlDocument Data { get; private set; }
			
			public DateTime Time { get; private set; }

			public string Url { get; private set; }
			
			public WeatherCachedXMLData (string url, XmlDocument data)
			{
				Url = url;
				Data = data;
				Time = DateTime.Now;
			}
		}

		const int MAXXMLCACHEAGE = 60 * 60 * 1000;
		static List<WeatherCachedXMLData> xml_cache = new List<WeatherCachedXMLData> ();
		
		public abstract string Name { get; }
		public abstract string About { get; }
		
		static bool? use_metric;
		public static bool UseMetric {
			get {
				if (!use_metric.HasValue)
					use_metric = false;
				return use_metric.Value;
			}
			set {
				use_metric = value;
			}
		}

		public abstract int ForecastDays { get; }

		public string City { get; protected set; }
		
		public double Latitude { get; protected set; }
		public double Longitude { get; protected set; }
		
		public DateTime SunRise { get; protected set; }
		public DateTime SunSet { get; protected set; }

		int temp = 0;
		public int Temp { 
			get {
				if (!UseMetric)
					return temp;
				return ConvertFtoC (temp);
			}
			protected set {
				temp = value;
			}
		}

		int feelslike = 0;
		public int FeelsLike { 
			get {
				if (!UseMetric)
					return feelslike;
				return ConvertFtoC (feelslike);
			}
			protected set {
				feelslike = value;
			}
		}

		int wind = 0;
		public int Wind { 
			get {
				if (!UseMetric)
					return wind;
				return ConvertMphToKmh (wind);
			} 
			protected set {
				wind = value;
			}
		}

		public bool ShowFeelsLike {
			get { return temp != feelslike; }
		}

		public string Condition { get; protected set; }
		public string WindDirection { get; protected set; }
		public string Humidity { get; protected set; }
		public string Image { get; protected set; }
		
		public WeatherForecast[] Forecasts { get; protected set; }

		protected abstract Dictionary<string, string> ImageMap { get; }

		public event Action WeatherReloading;
		public event Action WeatherUpdated;
		public event EventHandler<WeatherErrorArgs> WeatherError;
		
		Thread checkerThread = null;
		
		public bool IsBusy {
			get { return checkerThread != null && checkerThread.IsAlive; }
		}
		
		/// <summary>
		/// Creates a new weather source object.
		/// </summary>
		protected AbstractWeatherSource ()
		{
			Image = DefaultImage;
			Forecasts = new WeatherForecast [ForecastDays];
			for (int i = 0; i < ForecastDays; i++)
				Forecasts [i].image = DefaultImage;
			
			GLib.Timeout.Add (MAXXMLCACHEAGE, () => {
				xml_cache.RemoveAll (data => ((DateTime.Now - data.Time).TotalMilliseconds > MAXXMLCACHEAGE));
				return true; 
			});
		}
		
		public void StartReload ()
		{
			// stop running thread if there is one, this shouldnt happen
			StopReload ();

			checkerThread = DockServices.System.RunOnThread (() => {
				try {
					OnWeatherReloading ();
	
					FetchData ();
					
					OnWeatherUpdated ();
				} catch (ThreadAbortException) {
					Log<AbstractWeatherSource>.Debug (Name + ": Reload aborted");
					// restore Dockitem state
					OnWeatherUpdated ();
				} catch (NullReferenceException e) {
					OnWeatherError (Catalog.GetString ("Invalid Weather Location"));
					Log<AbstractWeatherSource>.Debug (Name + ": " + e.Message + e.StackTrace);
				} catch (Exception) {
					OnWeatherError (Catalog.GetString ("Network Error"));
				}
			});
		}

		public void StopReload ()
		{
			if (checkerThread != null) {
				checkerThread.Abort ();
				checkerThread.Join ();
			}
			OnWeatherUpdated ();
		}
		
		public void ShowRadar ()
		{
			ShowRadar (WeatherController.EncodedCurrentLocation);
		}
		
		public virtual void ShowForecast (int day)
		{
			DockServices.System.Open (ForecastUrl + day);
		}
		
		public bool IsNight ()
		{
			return (DateTime.Now < SunRise || DateTime.Now > SunSet);
		}
		
		public abstract IEnumerable<string> SearchLocation (string location);
		
		/// <value>
		/// The URL to retrieve weather data from.
		/// </value>
		protected abstract string FeedUrl { get; }
		
		/// <value>
		/// The URL to display a day's forecast.
		/// </value>
		protected abstract string ForecastUrl { get; }
		
		/// <value>
		/// A URL for searching for Location's.
		/// </value>
		protected abstract string SearchUrl { get; }
		
		/// <value>
		/// The default image name.
		/// </value>
		public static string DefaultImage {
			get { return Gtk.Stock.DialogQuestion; }
		}
		
		/// <summary>
		/// Finds an icon name for the specified condition.  Attempts to guess if the map does
		/// not contain an entry for the condition.  Also attempts to use night icons when appropriate.
		/// </summary>
		/// <param name="condition">
		/// A <see cref="System.String"/> representing the condition to look up an icon for.
		/// </param>
		/// <param name="useNight">
		/// A <see cref="System.Boolean"/> indicating if night icons should be used (if it is night).
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/> representing the icon name for the condition.
		/// </returns>
		protected string GetImage (string condition, bool useNight)
		{
			condition = condition.ToLower ();
			
			if (!ImageMap.ContainsKey (condition))
			{
				Log<AbstractWeatherSource>.Info (Name + ": no image for condition '" + condition + "'");
				
				if (condition.Contains ("sun"))
				{
					if (useNight && IsNight ())
						return "weather-clear-night";
					else
						return "weather-clear";
				}
				if (condition.Contains ("storm") || condition.Contains ("thunder"))
					return "weather-storm";
				if (condition.Contains ("rain") || condition.Contains ("showers"))
					return "weather-showers";
				if (condition.Contains ("drizzle") || condition.Contains ("mist"))
					return "weather-showers-scattered";
				if (condition.Contains ("snow") || condition.Contains ("flur"))
					return "weather-snow";
				if (condition.Contains ("cloud"))
				{
					if (useNight && IsNight ())
						return "weather-few-clouds-night";
					else
						return "weather-few-clouds";
				}
				if (condition.Contains ("fog"))
					return "weather-fog";
				
				return DefaultImage;
			}
			
			if (useNight && IsNight ())
			{
				if (ImageMap [condition].Equals ("weather-clear"))
					return "weather-clear-night";
				if (ImageMap [condition].Equals ("weather-few-clouds"))
					return "weather-few-clouds-night";
			}
			
			return ImageMap [condition];
		}
		
		/// <summary>
		/// Gets the XML document and parses it.
		/// </summary>
		protected virtual void FetchData ()
		{
			FetchAndParse (FeedUrl, ParseXml);
		}
		
		protected delegate void XmlParser (XmlDocument xml);
		
		protected void FetchAndParse (string url, XmlParser parser)
		{
			XmlDocument xml = null;
			WeatherCachedXMLData cacheddata = null;
			
			try {
				xml = FetchXml (url);
			} catch (Exception e) {
				// the fetch failed, see if we have cached data to use instead
				cacheddata = xml_cache.Find (data => (data.Url == url && (DateTime.Now - data.Time).TotalMilliseconds < MAXXMLCACHEAGE));
				
				// if we cant fetch data and have nothing cached, show an error
				if (cacheddata == null)
					throw e;
				
				Log<AbstractWeatherSource>.Debug (Name + ": Using cached XML file '" + url + "'");
				xml = cacheddata.Data;
			}
			
			parser (xml);
			
			// if we didnt use the cached data, then cache the fetched XML
			if (cacheddata == null) {
				xml_cache.RemoveAll (data => data.Url == url);
				xml_cache.Add (new WeatherCachedXMLData (url, xml));
			}
		}
		
		/// <summary>
		/// Retrieves an XML document from the specified URL.
		/// </summary>
		/// <param name="url">
		/// A <see cref="System.String"/> representing the URL to retrieve.
		/// </param>
		/// <returns>
		/// A <see cref="XmlDocument"/> from the URL.
		/// </returns>
		protected XmlDocument FetchXml (string url)
		{
			Log<AbstractWeatherSource>.Debug (Name + ": Fetching XML file '" + url + "'");
			
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create (url);
			request.Timeout = 60000;
			request.UserAgent = DockServices.System.UserAgent;
			if (DockServices.System.UseProxy)
				request.Proxy = DockServices.System.Proxy;
			
			XmlDocument xml = new XmlDocument ();
			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse ())
				try {
					xml.Load (response.GetResponseStream ());
				} finally {
					response.Close ();
				}
			
			return xml;
		}
		
		/// <summary>
		/// Parses the <see cref="XmlDocument"/> to obtain weather data.
		/// </summary>
		/// <param name="xml">
		/// A <see cref="XmlDocument"/> containing the weather data.
		/// </param>
		protected abstract void ParseXml (XmlDocument xml);
		
		/// <summary>
		/// Shows the radar in the default browser.
		/// </summary>
		/// <param name="location">
		/// A <see cref="System.String"/> representing the Location to show a radar for.
		/// </param>
		protected abstract void ShowRadar (string location);
		
		/// <summary>
		/// Forwards the weather source event.
		/// </summary>
		protected void OnWeatherUpdated ()
		{
			Log<AbstractWeatherSource>.Debug (Name + ": reload success");
			if (WeatherUpdated != null)
				DockServices.System.RunOnMainThread (WeatherUpdated);
		}
		
		/// <summary>
		/// Forwards the weather source event.
		/// </summary>
		protected void OnWeatherError (string msg)
		{
			Log<AbstractWeatherSource>.Debug (Name + ": error: " + msg);
			if (WeatherError != null)
				DockServices.System.RunOnMainThread (() => WeatherError (this, new WeatherErrorArgs(msg)));
		}
		
		/// <summary>
		/// Forwards the weather source event.
		/// </summary>
		protected void OnWeatherReloading ()
		{
			Log<AbstractWeatherSource>.Info (Name + ": Reloading weather data");
			if (WeatherReloading != null)
				DockServices.System.RunOnMainThread (WeatherReloading);
		}

		
		const string TEMP_C = "\u2103";
		const string TEMP_F = "\u2109";
		public static string TempUnit { 
			get {
				if (UseMetric)
					return TEMP_C;
				return TEMP_F;
			}
		}
		
		const string WIND_KMH = "km/h";
		const string WIND_MPH = "mph";
		public static string WindUnit {
			get {
				if (UseMetric)
					return WIND_KMH;
				return WIND_MPH;
			}
		}
		
		public static int ConvertFtoC (int F)
		{
			return (int) Math.Round ((double) (F - 32) * 5 / 9);
		}
		
		public static int ConvertMphToKmh (int Mph)
		{
			return (int) Math.Round ((double) Mph * 1.609344);
		}		
	}
}
