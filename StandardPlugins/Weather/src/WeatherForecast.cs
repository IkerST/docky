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
using Mono.Unix;

namespace WeatherDocklet
{
	/// <summary>
	/// Stores information about one day's weather forecast.
	/// </summary>
	public struct WeatherForecast
	{
		/// <summary>
		/// The high value for the forecast day.
		/// </summary>
		private int thigh;
		public int high { 
			get {
				if (!AbstractWeatherSource.UseMetric)
					return thigh;
				return AbstractWeatherSource.ConvertFtoC (thigh);
			}
			set {
				thigh = value;
			}
		}	
		
		/// <summary>
		/// The low value for the forecast day.
		/// </summary>
		private int tlow;
		public int low { 
			get {
				if (!AbstractWeatherSource.UseMetric)
					return tlow;
				return AbstractWeatherSource.ConvertFtoC (tlow);
			}
			set {
				tlow = value;
			}
		}
		
		/// <summary>
		/// The day of the week (3 char string, ex: Mon, Tue, etc).
		/// </summary>
		public string dow;
		
		/// <summary>
		/// The condition for the forecast day.
		/// </summary>
		public string condition;
		
		/// <summary>
		/// If there is a chance of precipitation.
		/// </summary>
		public bool chanceOf;
		
		/// <summary>
		/// An icon name representing the condition for the forecast day.
		/// </summary>
		public string image;
		
		/// <summary>
		/// Takes a short name for a day and returns the full name.
		/// </summary>
		/// <param name="dow">
		/// A <see cref="System.string"/> indicating the short name for the day.
		/// </param>
		/// <returns>
		/// A <see cref="System.string"/> of the day's full name.
		/// </returns>
		public static string DayName (string dow)
		{
			DayOfWeek day = DayOfWeek.Sunday;

			if (dow.Equals ("Mon"))
				day = DayOfWeek.Monday;
			else if (dow.Equals ("Tue"))
				day = DayOfWeek.Tuesday;
			else if (dow.Equals ("Wed"))
				day = DayOfWeek.Wednesday;
			else if (dow.Equals ("Thu"))
				day = DayOfWeek.Thursday;
			else if (dow.Equals ("Fri"))
				day = DayOfWeek.Friday;
			else if (dow.Equals ("Sat"))
				day = DayOfWeek.Saturday;
			else if (dow.Equals ("Sun"))
				day = DayOfWeek.Sunday;

			if (DateTime.Now.DayOfWeek == day)
				return Catalog.GetString ("_Today");

			if (DateTime.Now.AddDays (1).DayOfWeek == day)
				return Catalog.GetString ("T_omorrow");

			return DateTime.Now.AddDays (day - DateTime.Now.DayOfWeek).ToString ("dddd");
		}

		/// <summary>
		/// Takes a short name for a day and returns the short name.
		/// </summary>
		/// <param name="dow">
		/// A <see cref="System.string"/> indicating the short name for the day.
		/// </param>
		/// <returns>
		/// A <see cref="System.string"/> of the day's short name.
		/// </returns>
		public static string DayShortName (string dow)
		{
			if (string.IsNullOrEmpty (dow)) return "";

			DayOfWeek day = DayOfWeek.Sunday;

			if (dow.Equals ("Mon"))
				day = DayOfWeek.Monday;
			else if (dow.Equals ("Tue"))
				day = DayOfWeek.Tuesday;
			else if (dow.Equals ("Wed"))
				day = DayOfWeek.Wednesday;
			else if (dow.Equals ("Thu"))
				day = DayOfWeek.Thursday;
			else if (dow.Equals ("Fri"))
				day = DayOfWeek.Friday;
			else if (dow.Equals ("Sat"))
				day = DayOfWeek.Saturday;
			else if (dow.Equals ("Sun"))
				day = DayOfWeek.Sunday;

			return DateTime.Now.AddDays (day - DateTime.Now.DayOfWeek).ToString ("ddd");
		}

	}
}
