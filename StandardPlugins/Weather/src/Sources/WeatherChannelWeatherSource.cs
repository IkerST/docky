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
using System.Collections.Generic;
using System.Net;
using System.Web;
using System.Xml;
using System.Globalization;

using Mono.Unix;

using Docky.Services;

namespace WeatherDocklet
{
	/// <summary>
	/// Provides a weather source using data from The Weather Channel.
	/// </summary>
	public class WeatherChannelWeatherSource : AbstractWeatherSource
	{
		/// <value>
		/// A map that maps conditions to icon names.
		/// </value>
		static Dictionary<string, string> image_map = new Dictionary<string, string>();
		/// <value>
		/// A map that maps conditions to icon names.
		/// </value>
		protected override Dictionary<string, string> ImageMap { get { return image_map; } }
		
		public override int ForecastDays { get { return 5 - (Offset ? 1 : 0); } }

		/// <value>
		/// If the FeedUrl returned a first forecast day with N/A values and we offset to skip it.
		/// </value>
		bool Offset { get; set; }
		
		public override string Name {
			get {
				return "Weather Channel";
			}
		}
		
		public override string About {
			get {
				return Catalog.GetString ("Weather data provided by and copyright The Weather Channel.  " +
					"This source requires locations to be specified as US Zip Code or a unique code from the website.  " +
					"To find your code, look up your forecast on their site and the code is in the URL after '/local/' and looks like AAXX0000.");
			}
		}

		protected override string FeedUrl {
			get {
				return "http://xoap.weather.com/weather/local/" +
					WeatherController.EncodedCurrentLocation +
					"?cc=*&dayf=10&prod=xoap&par=1097943453&link=xoap&key=306225326138d0bd&unit=s";
			}
		}
		
		protected override string ForecastUrl {
			get {
				return "http://www.weather.com/outlook/travel/businesstraveler/wxdetail/" +
					WeatherController.EncodedCurrentLocation + "?dayNum=";
			}
		}
		
		protected override string SearchUrl {
			get {
				return "http://xoap.weather.com/search/search?where=";
			}
		}
		
		static WeatherChannelWeatherSource () {
			image_map.Add ("am clouds / pm sun", "weather-few-clouds");
			image_map.Add ("am light rain", "weather-showers-scattered");
			image_map.Add ("am showers", "weather-showers");
			image_map.Add ("clear", "weather-clear");
			image_map.Add ("cloudy", "weather-few-clouds");
			image_map.Add ("fair", "weather-clear");
			image_map.Add ("fair  and  windy", "weather-clear");
			image_map.Add ("fog", "weather-fog");
			image_map.Add ("few showers", "weather-showers-scattered");
			image_map.Add ("haze", "weather-fog");
			image_map.Add ("heavy rain", "weather-showers");
			image_map.Add ("isolated t-storms", "weather-storm");
			image_map.Add ("light rain", "weather-showers-scattered");
			image_map.Add ("light snow  and  wind", "weather-snow");
			image_map.Add ("mostly cloudy", "weather-few-clouds");
			image_map.Add ("mostly cloudy  and  windy", "weather-few-clouds");
			image_map.Add ("mostly sunny", "weather-clear");
			image_map.Add ("partly cloudy", "weather-few-clouds");
			image_map.Add ("partly cloudy / wind", "weather-few-clouds");
			image_map.Add ("partly cloudy  and  windy", "weather-few-clouds");
			image_map.Add ("pm showers", "weather-showers");
			image_map.Add ("pm t-storms", "weather-storm");
			image_map.Add ("rain", "weather-showers");
			image_map.Add ("rain / thunder", "weather-storm");
			image_map.Add ("rain / wind", "weather-showers");
			image_map.Add ("scattered showers", "weather-showers-scattered");
			image_map.Add ("scattered strong storms", "weather-storm");
			image_map.Add ("scattered strong storms / wind", "weather-storm");
			image_map.Add ("scattered t-storms", "weather-storm");
			image_map.Add ("showers", "weather-showers");
			image_map.Add ("showers / wind", "weather-showers");
			image_map.Add ("smoke", "weather-fog");
			image_map.Add ("sunny", "weather-clear");
			image_map.Add ("t-showers", "weather-storm");
			image_map.Add ("t-storms", "weather-storm");
			image_map.Add ("thunder in the vicinity", "weather-storm");
			image_map.Add ("wintry mix", "weather-snow");
			
			// currently unused icons
			//image_map.Add ("", "weather-overcast");
			//image_map.Add ("", "weather-severe-alert");
		}
		
		/// <value>
		/// The singleton instance;
		/// </value>
		static AbstractWeatherSource Instance { get; set; }
		
		/// <summary>
		/// Returns the singleton instance for this class.
		/// </summary>
		/// <returns>
		/// A <see cref="AbstractWeatherSource"/> that is the only instance of this class.
		/// </returns>
		public static AbstractWeatherSource GetInstance ()
		{
			if (Instance == null)
				Instance = new WeatherChannelWeatherSource ();
			
			return Instance;
		}
		
		protected override void ParseXml (XmlDocument xml)
		{
			XmlNodeList nodelist = xml.SelectNodes ("weather/loc");
			XmlNode item = nodelist.Item (0);
			double dbl;
			Double.TryParse(item.SelectSingleNode ("lat").InnerText, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out dbl);
			Latitude = dbl;
			Double.TryParse (item.SelectSingleNode ("lon").InnerText, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out dbl);
			Longitude = dbl;
			SunRise = DateTime.Parse (item.SelectSingleNode ("sunr").InnerText);
			SunSet = DateTime.Parse (item.SelectSingleNode ("suns").InnerText);
			
			nodelist = xml.SelectNodes ("weather/cc");
			item = nodelist.Item (0);
			City = item.SelectSingleNode ("obst").InnerText;
			if (City.IndexOf (",") != -1)
				City = City.Substring (0, City.IndexOf (","));
			
			int temp;
			Int32.TryParse (item.SelectSingleNode ("tmp").InnerText, out temp);
			Temp = temp;
			FeelsLike = temp;
			
			if (!item.SelectSingleNode ("flik").InnerText.Equals ("N/A")) {
				Int32.TryParse (item.SelectSingleNode ("flik").InnerText, out temp);
				FeelsLike = temp;
			}
			
			Int32.TryParse(item.SelectSingleNode ("wind").SelectSingleNode ("s").InnerText, out temp);
			Wind = temp;
			WindDirection = item.SelectSingleNode ("wind").SelectSingleNode ("t").InnerText;
			
			Humidity = item.SelectSingleNode ("hmid").InnerText + "%";
			
			Condition = item.SelectSingleNode ("t").InnerText;
			Image = GetImage (Condition, true);
			
			nodelist = xml.SelectNodes ("weather/dayf/day");

			if (nodelist.Item (0).SelectSingleNode ("hi").InnerText.Equals ("N/A"))
				Offset = true;
			else
				Offset = false;

			for (int i = 0; i < ForecastDays + (Offset ? 1 : 0); i++)
			{
				item = nodelist.Item (i + (Offset ? 1 : 0));
				if (item == null)
					break;
				
				Int32.TryParse (item.SelectSingleNode ("hi").InnerText, out temp);
				Forecasts [i].high = temp; 
				Int32.TryParse (item.SelectSingleNode ("low").InnerText, out temp);
				Forecasts [i].low = temp;
				Forecasts [i].condition = item.SelectSingleNode ("part").SelectSingleNode ("t").InnerText;
				Forecasts [i].dow = item.Attributes ["t"].InnerText.Substring (0, 3);
				Forecasts [i].image = GetImage (Forecasts [i].condition, false);
				int ppcp = 0;
				Int32.TryParse (item.SelectSingleNode ("part").SelectSingleNode ("ppcp").InnerText, out ppcp);
				Forecasts [i].chanceOf = ppcp < 60 && ppcp > 30;
			}
		}
		
		protected override void ShowRadar (string location)
		{
			DockServices.System.Open ("http://www.weather.com/outlook/travel/businesstraveler/map/" + location);
		}
		
		public override void ShowForecast (int day)
		{
			DockServices.System.Open (ForecastUrl + (day + (Offset ? 1 : 0)));
		}
		
		public override IEnumerable<string> SearchLocation (string location)
		{
			XmlDocument xml;
			location = HttpUtility.UrlEncode (location);
			
			try
			{
				xml = FetchXml (SearchUrl + location);
			} catch (Exception) { yield break; }
			
			XmlNodeList nodelist = xml.SelectNodes ("search/loc");
			
			for (int i = 0; i < nodelist.Count; i++)
			{
				yield return nodelist.Item (i).InnerText;
				yield return nodelist.Item (i).Attributes ["id"].InnerText;
			}
			
			yield break;
		}
	}
}
