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
	/// Provides a weather source using data from iGoogle.
	/// </summary>
	public class GoogleWeatherSource : AbstractWeatherSource
	{
		/// <value>
		/// A map that maps conditions to icon names.
		/// </value>
		static Dictionary<string, string> image_map = new Dictionary<string, string>();
		/// <value>
		/// A map that maps conditions to icon names.
		/// </value>
		protected override Dictionary<string, string> ImageMap { get { return image_map; } }
		
		public override int ForecastDays { get { return 4; } }
		
		public override string Name {
			get {
				return "iGoogle";
			}
		}
		
		public override string About {
			get {
				return Catalog.GetString ("Weather data provided by Google.  " +
					"This source requires locations to be specified as US Zip Code or 'City, Country'.");
			}
		}
		
		protected override string FeedUrl {
			get {
				return "http://www.google.com/ig/api?hl=en&weather=" + WeatherController.EncodedCurrentLocation;
			}
		}
		
		protected override string ForecastUrl {
			get {
				return "http://www.wunderground.com/cgi-bin/findweather/getForecast?query="  + WeatherController.EncodedCurrentLocation + "&hourly=1&yday=";
			}
		}
		
		protected override string SearchUrl {
			get {
				return "http://api.wunderground.com/auto/wui/geo/GeoLookupXML/index.xml?query=";
			}
		}
		
		static GoogleWeatherSource () {
			image_map.Add ("/ig/images/weather/sunny.gif", "weather-clear");
			
			image_map.Add ("/ig/images/weather/cloudy.gif", "weather-few-clouds");
			image_map.Add ("/ig/images/weather/mostly_cloudy.gif", "weather-few-clouds");
			image_map.Add ("/ig/images/weather/mostly_sunny.gif", "weather-few-clouds");
			image_map.Add ("/ig/images/weather/partly_cloudy.gif", "weather-few-clouds");
			
			image_map.Add ("/ig/images/weather/chance_of_storm.gif", "weather-storm");
			image_map.Add ("/ig/images/weather/storm.gif", "weather-storm");
			image_map.Add ("/ig/images/weather/thunderstorm.gif", "weather-storm");
			
			image_map.Add ("/ig/images/weather/rain.gif", "weather-showers");
			image_map.Add ("/ig/images/weather/chance_of_rain.gif", "weather-showers");
			
			image_map.Add ("/ig/images/weather/mist.gif", "weather-showers-scattered");
			
			image_map.Add ("/ig/images/weather/chance_of_snow.gif", "weather-snow");
			image_map.Add ("/ig/images/weather/icy.gif", "weather-snow");
			image_map.Add ("/ig/images/weather/sleet.gif", "weather-snow");
			image_map.Add ("/ig/images/weather/snow.gif", "weather-snow");
			
			image_map.Add ("/ig/images/weather/dust.gif", "weather-fog");
			image_map.Add ("/ig/images/weather/fog.gif", "weather-fog");
			image_map.Add ("/ig/images/weather/haze.gif", "weather-fog");
			image_map.Add ("/ig/images/weather/smoke.gif", "weather-fog");
			
			// currently unused icons
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
				Instance = new GoogleWeatherSource ();
			
			return Instance;
		}
		
		protected override void ParseXml (XmlDocument xml)
		{
			FetchLatLong ();
			
			XmlNodeList nodelist = xml.SelectNodes ("xml_api_reply/weather/forecast_information");
			XmlNode item = nodelist.Item (0);
			City = item.SelectSingleNode ("city").Attributes["data"].InnerText;
			if (City.IndexOf (",") != -1)
				City = City.Substring (0, City.IndexOf (","));
			
			nodelist = xml.SelectNodes ("xml_api_reply/weather/current_conditions");
			item = nodelist.Item (0);
			
			int temp;
			Int32.TryParse (item.SelectSingleNode ("temp_f").Attributes["data"].InnerText, out temp);
			Temp = temp;
			FeelsLike = temp;
			
			string [] wind = item.SelectSingleNode ("wind_condition").Attributes["data"].InnerText.Split (' ');
			Int32.TryParse (wind [3], out temp);
			Wind = temp;
			WindDirection = wind [1];
			
			string [] humidity = item.SelectSingleNode ("humidity").Attributes["data"].InnerText.Split (' ');
			Humidity = humidity [1];
			
			Condition = item.SelectSingleNode ("condition").Attributes["data"].InnerText;
			Image = GetImage (item.SelectSingleNode ("icon").Attributes["data"].InnerText, true);
			
			nodelist = xml.SelectNodes ("xml_api_reply/weather/forecast_conditions");
			for (int i = 0; i < ForecastDays; i++)
			{
				item = nodelist.Item (i);
				if (item == null)
					break;
				
				Int32.TryParse (item.SelectSingleNode ("high").Attributes["data"].InnerText, out temp);
				Forecasts [i].high = temp; 
				Int32.TryParse (item.SelectSingleNode ("low").Attributes["data"].InnerText, out temp);
				Forecasts [i].low = temp;
				Forecasts [i].condition = item.SelectSingleNode ("condition").Attributes["data"].InnerText;
				Forecasts [i].dow = item.SelectSingleNode ("day_of_week").Attributes["data"].InnerText;
				Forecasts [i].image = GetImage (item.SelectSingleNode ("icon").Attributes["data"].InnerText, false);
				Forecasts [i].chanceOf = Forecasts [i].condition.ToLower ().IndexOf ("chance") != -1;
			}
		}
		
		void FetchLatLong ()
		{
			XmlDocument xml = FetchXml ("http://api.wunderground.com/auto/wui/geo/WXCurrentObXML/index.xml?query=" + WeatherController.EncodedCurrentLocation);
			XmlNodeList nodelist = xml.SelectNodes ("current_observation/display_location");
			XmlNode item = nodelist.Item (0);
			double dbl;
			Double.TryParse(item.SelectSingleNode ("latitude").InnerText, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out dbl);
			Latitude = dbl;
			Double.TryParse (item.SelectSingleNode ("longitude").InnerText, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out dbl);
			Longitude = dbl;
			SunRise = WeatherController.Sunrise(Latitude, Longitude);
			SunSet = WeatherController.Sunset(Latitude, Longitude);
		}
		
		public override void ShowForecast (int day)
		{
			DateTime forecast = DateTime.Today.AddDays(day);
			DockServices.System.Open (ForecastUrl + (forecast.DayOfYear - 1) + "&weekday=" + forecast.DayOfWeek.ToString());
		}
		
		protected override void ShowRadar (string location)
		{
			string lat = Latitude.ToString(CultureInfo.GetCultureInfo("en-US"));
			string lon = Longitude.ToString(CultureInfo.GetCultureInfo("en-US"));
			
			DockServices.System.Open ("http://www.wunderground.com/wundermap/?lat=" + lat + "&lon=" + lon +
										 "&zoom=8&type=hyb&units=" + (WeatherPreferences.Metric ? "metric" : "english") +
										 "&rad=0&wxsn=0&svr=0&cams=0&sat=1&sat.num=1&sat.spd=25&sat.opa=85&sat.gtt1=109&sat.gtt2=108&sat.type=IR4&riv=0&mm=0&hur=0");
		}
		
		public override IEnumerable<string> SearchLocation (string origLocation)
		{
			XmlDocument xml;
			string location = HttpUtility.UrlEncode (origLocation);
			
			try
			{
				xml = FetchXml (SearchUrl + location);
			} catch (Exception) { yield break; }
			
			if (xml.SelectNodes ("wui_error").Count > 0)
				yield break;
			
			XmlNodeList nodelist = xml.SelectNodes ("locations/location");
			if (nodelist.Count > 0)
			{
				for (int i = 0; i < nodelist.Count; i++)
				{
					yield return nodelist.Item (i).SelectSingleNode ("name").InnerText;
					yield return nodelist.Item (i).SelectSingleNode ("name").InnerText;
				}
			}
			else
			{
				nodelist = xml.SelectNodes ("location");
				
				string city = nodelist.Item (0).SelectSingleNode ("city").InnerText;
				string state = nodelist.Item (0).SelectSingleNode ("state").InnerText;
				string country = nodelist.Item (0).SelectSingleNode ("country").InnerText;
				
				string loc = city;
				if (state.Length > 0)
					loc += ", " + state;
				if (country.Length > 0)
					loc += ", " + country;
				
				if (nodelist.Item (0).Attributes ["type"].InnerText.Equals ("CITY"))
				{
					yield return loc;
					yield return nodelist.Item (0).SelectSingleNode ("zip").InnerText;
				}
				else
				{
					yield return loc;
					yield return origLocation;
				}
			}
			
			yield break;
		}
	}
}
