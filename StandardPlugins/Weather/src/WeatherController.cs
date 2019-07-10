//  
//  Copyright (C) 2009-2010 Robert Dyer, Rico Tzschichholz
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
using System.Web;

using Docky.Services;

namespace WeatherDocklet
{
	/// <summary>
	/// A controller class to handle weather sources.
	/// </summary>
	public static class WeatherController
	{
		/// <summary>
		/// Wraps the active weather source's event.
		/// </summary>
		public static event Action WeatherReloading;
		
		/// <summary>
		/// Wraps the active weather source's event.
		/// </summary>
		public static event EventHandler<WeatherErrorArgs> WeatherError;
		
		/// <summary>
		/// Wraps the active weather source's event.
		/// </summary>
		public static event Action WeatherUpdated;
		
		/// <value>
		/// A <see cref="System.Collections.Generic.Dictionary"/> of all weather sources.
		/// </value>
		public static Dictionary<string, AbstractWeatherSource> Sources { get; private set; }
		
		/// <value>
		/// The current weather source.
		/// </value>
		public static AbstractWeatherSource Weather { get; private set; }
		
		/// <value>
		/// How frequently the weather should reload automatically.
		/// </value>
		static uint UpdateTimer { get; set; }
		
		/// <value>
		/// The current location to use.
		/// </value>
		public static string CurrentLocation {
			get {
				if (CurrentLocationIndex >= WeatherPreferences.Locations.Length || CurrentLocationIndex < 0)
					return "";
				else
					return WeatherPreferences.Locations [CurrentLocationIndex];
			}
		}
		
		/// <value>
		/// The current location to use, encoded for use in URLs.
		/// </value>
		public static string EncodedCurrentLocation { get { return HttpUtility.UrlEncode (CurrentLocation); } }
		
		/// <value>
		/// The index of the current location to use.
		/// </value>
		static int CurrentLocationIndex { get; set; }
		
		static WeatherController ()
		{
			CurrentLocationIndex = 0;
			
			Sources = new Dictionary<string, AbstractWeatherSource> ();
			
			Sources.Add (GoogleWeatherSource.GetInstance ().Name, GoogleWeatherSource.GetInstance ());
			Sources.Add (WeatherChannelWeatherSource.GetInstance ().Name, WeatherChannelWeatherSource.GetInstance ());
			Sources.Add (WunderWeatherSource.GetInstance ().Name, WunderWeatherSource.GetInstance ());
			
			ResetWeatherSource ();
			
			WeatherPreferences.LocationsChanged += HandleLocationsChanged;
			WeatherPreferences.SourceChanged += HandleSourceChanged;
			WeatherPreferences.TimeoutChanged += HandleTimeoutChanged;
			WeatherPreferences.MetricChanged += HandleMetricChanged;
			
			DockServices.System.ConnectionStatusChanged += HandleStateChanged;
		}
		
		/// <summary>
		/// Uses the next location for weather.
		/// </summary>
		public static void NextLocation ()
		{
			if (CurrentLocationIndex >= WeatherPreferences.Locations.Length - 1)
				CurrentLocationIndex = 0;
			else
				CurrentLocationIndex++;
			
			ResetTimer ();
		}
		
		/// <summary>
		/// Uses the previous location for weather.
		/// </summary>
		public static void PreviousLocation ()
		{
			if (CurrentLocationIndex == 0)
				CurrentLocationIndex = WeatherPreferences.Locations.Length - 1;
			else
				CurrentLocationIndex--;
			
			ResetTimer ();
		}
		
		/// <summary>
		/// Resets the weather timer and immediately reloads.
		/// </summary>
		public static void ResetTimer ()
		{
			ResetTimer (true);
		}
		
		/// <summary>
		/// Resets the weather timer and immediately reloads if needed.
		/// </summary>
		/// <param name="reloadImmediately">
		/// A <see cref="System.Boolean"/> indicating if it should immediately reload weather data.
		/// </param>
		public static void ResetTimer (bool reloadImmediately)
		{
			StopTimer ();
			
			if (CurrentLocation == "" || !DockServices.System.NetworkConnected)
				return;

			if (reloadImmediately && !Weather.IsBusy)
				Weather.StartReload ();
				
			UpdateTimer = GLib.Timeout.Add (WeatherPreferences.Timeout * 60 * 1000, () => {
				if (!Weather.IsBusy && DockServices.System.NetworkConnected) 
					Weather.StartReload (); 
				return true; 
			});
		}
		
		public static void StopTimer ()
		{
			if (UpdateTimer > 0) {
				GLib.Source.Remove (UpdateTimer);
				UpdateTimer = 0;
			}

			Weather.StopReload (); 
		}
		
		/// <summary>
		/// Resets the weather source, using a default if needed.
		/// </summary>
		public static void ResetWeatherSource ()
		{
			if (Weather != null)
			{
				Weather.WeatherReloading -= HandleWeatherReloading;
				Weather.WeatherError -= HandleWeatherError;
				Weather.WeatherUpdated -= HandleWeatherUpdated;
			}
			
			if (Sources.ContainsKey (WeatherPreferences.Source))
				Weather = Sources [WeatherPreferences.Source];
			else
				Weather = Sources [WunderWeatherSource.GetInstance ().Name];
			
			AbstractWeatherSource.UseMetric = WeatherPreferences.Metric;
			
			Weather.WeatherReloading += HandleWeatherReloading;
			Weather.WeatherError += HandleWeatherError;
			Weather.WeatherUpdated += HandleWeatherUpdated;
		}
		
		/// <summary>
		/// Handles when the network goes up/down to reset/remove the timer.
		/// </summary>
		/// <param name="o">
		/// Ignored
		/// </param>
		/// <param name="state">
		/// Ignored
		/// </param>
		static void HandleStateChanged (object o, ConnectionStatusChangeEventArgs state)
		{
			ResetTimer ();
		}
		
		/// <summary>
		/// Handles when the Timeout preference is changed.
		/// </summary>
		/// <param name="sender">
		/// Ignored
		/// </param>
		/// <param name="e">
		/// Ignored
		/// </param>
		static void HandleTimeoutChanged (object sender, EventArgs e)
		{
			ResetTimer (false);
		}
		
		/// <summary>
		/// Handles when the Source preference is changed.
		/// </summary>
		/// <param name="sender">
		/// Ignored
		/// </param>
		/// <param name="e">
		/// Ignored
		/// </param>
		static void HandleSourceChanged (object sender, EventArgs e)
		{
			ResetWeatherSource ();
			ResetTimer ();
		}
		
		/// <summary>
		/// Handles when the Location preference is changed.
		/// </summary>
		/// <param name="sender">
		/// Ignored
		/// </param>
		/// <param name="e">
		/// Ignored
		/// </param>
		static void HandleLocationsChanged (object sender, EventArgs e)
		{
			CurrentLocationIndex = 0;
			ResetTimer ();
		}
		
		/// <summary>
		/// Handles when the Metric preference is changed.
		/// </summary>
		/// <param name="sender">
		/// Ignored
		/// </param>
		/// <param name="e">
		/// Ignored
		/// </param>
		static void HandleMetricChanged (object sender, EventArgs e)
		{
			AbstractWeatherSource.UseMetric = WeatherPreferences.Metric;
			HandleWeatherUpdated ();
		}
		
		/// <summary>
		/// Proxies the weather source's event.
		/// </summary>
		static void HandleWeatherReloading ()
		{
			if (WeatherReloading != null)
				WeatherReloading ();
		}
		
		/// <summary>
		/// Proxies the weather source's event.
		/// </summary>
		static void HandleWeatherError (object sender, WeatherErrorArgs e)
		{
			if (WeatherError != null)
				WeatherError (sender, e);
		}
		
		/// <summary>
		/// Proxies the weather source's event.
		/// </summary>
		static void HandleWeatherUpdated ()
		{
			if (WeatherUpdated != null)
				WeatherUpdated ();
		}

		public static DateTime Sunrise (double latitude, double longitude)
		{
			DateTime riseTime = DateTime.Now, setTime = DateTime.Now;
			bool isRise = false, isSet = false;
			SunTimes.CalculateSunRiseSetTimes (latitude, longitude, DateTime.Now, ref riseTime, ref setTime, ref isRise, ref isSet);
			return riseTime;
		}
		
		public static DateTime Sunset (double latitude, double longitude)
		{
			DateTime riseTime = DateTime.Now, setTime = DateTime.Now;
			bool isRise = false, isSet = false;
			SunTimes.CalculateSunRiseSetTimes (latitude, longitude, DateTime.Now, ref riseTime, ref setTime, ref isRise, ref isSet);
			return setTime;
		}
	}

	// Based on code by Zacky Pickholz (zacky.pickholz@gmail.com)
	// http://www.codeproject.com/KB/cs/suntimes.aspx
	// which is based on code from http://home.att.net/~srschmitt/script_sun_rise_set.html
	internal sealed class SunTimes
	{
		const double mDR = Math.PI / 180;
		const double mK1 = 15 * mDR * 1.0027379;

		static int[] mRiseTimeArr = new int[2] { 0, 0 };
		static int[] mSetTimeArr = new int[2] { 0, 0 };

		static double[] mSunPositionInSkyArr = new double[2] { 0.0, 0.0 };
		static double[] mRightAscentionArr = new double[3] { 0.0, 0.0, 0.0 };
		static double[] mDecensionArr = new double[3] { 0.0, 0.0, 0.0 };
		static double[] mVHzArr = new double[3] { 0.0, 0.0, 0.0 };

		static bool mIsSunrise = false;
		static bool mIsSunset = false;

		public static bool CalculateSunRiseSetTimes(double lat, double lon, DateTime date,
												ref DateTime riseTime, ref DateTime setTime,
												ref bool isSunrise, ref bool isSunset)
		{
			double zone = -(int)Math.Round(TimeZone.CurrentTimeZone.GetUtcOffset(date).TotalSeconds / 3600);
			double jd = GetJulianDay(date) - 2451545;  // Julian day relative to Jan 1.5, 2000

			lon = lon / 360;
			double tz = zone / 24;
			double ct = jd / 36525 + 1;                                 // centuries since 1900.0
			double t0 = LocalSiderealTimeForTimeZone(lon, jd, tz);      // local sidereal time

			// get sun position at start of day
			jd += tz;
			CalculateSunPosition(jd, ct);
			double ra0 = mSunPositionInSkyArr[0];
			double dec0 = mSunPositionInSkyArr[1];

			// get sun position at end of day
			jd += 1;
			CalculateSunPosition(jd, ct);
			double ra1 = mSunPositionInSkyArr[0];
			double dec1 = mSunPositionInSkyArr[1];

			// make continuous 
			if (ra1 < ra0)
				ra1 += 2 * Math.PI;

			// initialize
			mIsSunrise = false;
			mIsSunset = false;

			mRightAscentionArr[0] = ra0;
			mDecensionArr[0] = dec0;

			// check each hour of this day
			for (int k = 0; k < 24; k++)
			{
				mRightAscentionArr[2] = ra0 + (k + 1) * (ra1 - ra0) / 24;
				mDecensionArr[2] = dec0 + (k + 1) * (dec1 - dec0) / 24;
				mVHzArr[2] = TestHour(k, zone, t0, lat);

				// advance to next hour
				mRightAscentionArr[0] = mRightAscentionArr[2];
				mDecensionArr[0] = mDecensionArr[2];
				mVHzArr[0] = mVHzArr[2];
			}

			riseTime = new DateTime(date.Year, date.Month, date.Day, mRiseTimeArr[0], mRiseTimeArr[1], 0);
			setTime = new DateTime(date.Year, date.Month, date.Day, mSetTimeArr[0], mSetTimeArr[1], 0);

			isSunset = true;
			isSunrise = true;

			// neither sunrise nor sunset
			if ((!mIsSunrise) && (!mIsSunset))
			{
				if (mVHzArr[2] < 0)
					isSunrise = false; // Sun down all day
				else
					isSunset = false; // Sun up all day
			}
			// sunrise or sunset
			else
			{
				if (!mIsSunrise)
					// No sunrise this date
					isSunrise = false;
				else if (!mIsSunset)
					// No sunset this date
					isSunset = false;
			}

			return true;
		}

		static int Sign(double value)
		{
			if (value > 0.0)
				return 1;
			if (value < 0.0)
				return -1;
			return 0;
		}

		// Local Sidereal Time for zone
		static double LocalSiderealTimeForTimeZone(double lon, double jd, double z)
		{
			double s = 24110.5 + 8640184.812999999 * jd / 36525 + 86636.6 * z + 86400 * lon;
			s = s / 86400;
			s = s - Math.Floor(s);
			return s * 360 * mDR;
		}

		// determine Julian day from calendar date
		// (Jean Meeus, "Astronomical Algorithms", Willmann-Bell, 1991)
		static double GetJulianDay(DateTime date)
		{
			int month = date.Month;
			int day = date.Day;
			int year = date.Year;

			bool gregorian = (year < 1583) ? false : true;

			if ((month == 1) || (month == 2))
			{
				year = year - 1;
				month = month + 12;
			}

			double a = Math.Floor((double)year / 100);
			double b = 0;

			if (gregorian)
				b = 2 - a + Math.Floor(a / 4);
			else
				b = 0.0;

			double jd = Math.Floor(365.25 * (year + 4716))
					   + Math.Floor(30.6001 * (month + 1))
					   + day + b - 1524.5;

			return jd;
		}

		// sun's position using fundamental arguments 
		// (Van Flandern & Pulkkinen, 1979)
		static void CalculateSunPosition(double jd, double ct)
		{
			double g, lo, s, u, v, w;

			lo = 0.779072 + 0.00273790931 * jd;
			lo = lo - Math.Floor(lo);
			lo = lo * 2 * Math.PI;

			g = 0.993126 + 0.0027377785 * jd;
			g = g - Math.Floor(g);
			g = g * 2 * Math.PI;

			v = 0.39785 * Math.Sin(lo);
			v = v - 0.01 * Math.Sin(lo - g);
			v = v + 0.00333 * Math.Sin(lo + g);
			v = v - 0.00021 * ct * Math.Sin(lo);

			u = 1 - 0.03349 * Math.Cos(g);
			u = u - 0.00014 * Math.Cos(2 * lo);
			u = u + 0.00008 * Math.Cos(lo);

			w = -0.0001 - 0.04129 * Math.Sin(2 * lo);
			w = w + 0.03211 * Math.Sin(g);
			w = w + 0.00104 * Math.Sin(2 * lo - g);
			w = w - 0.00035 * Math.Sin(2 * lo + g);
			w = w - 0.00008 * ct * Math.Sin(g);

			// compute sun's right ascension
			s = w / Math.Sqrt(u - v * v);
			mSunPositionInSkyArr[0] = lo + Math.Atan(s / Math.Sqrt(1 - s * s));

			// ...and declination 
			s = v / Math.Sqrt(u);
			mSunPositionInSkyArr[1] = Math.Atan(s / Math.Sqrt(1 - s * s));
		}

		// test an hour for an event
		static double TestHour(int k, double zone, double t0, double lat)
		{
			double[] ha = new double[3];
			double a, b, c, d, e, s, z;
			double time;
			int hr, min;
			double az, dz, hz, nz;

			ha[0] = t0 - mRightAscentionArr[0] + k * mK1;
			ha[2] = t0 - mRightAscentionArr[2] + k * mK1 + mK1;

			ha[1] = (ha[2] + ha[0]) / 2;    // hour angle at half hour
			mDecensionArr[1] = (mDecensionArr[2] + mDecensionArr[0]) / 2;  // declination at half hour

			s = Math.Sin(lat * mDR);
			c = Math.Cos(lat * mDR);
			z = Math.Cos(90.833 * mDR);    // refraction + sun semidiameter at horizon

			if (k <= 0)
				mVHzArr[0] = s * Math.Sin(mDecensionArr[0]) + c * Math.Cos(mDecensionArr[0]) * Math.Cos(ha[0]) - z;

			mVHzArr[2] = s * Math.Sin(mDecensionArr[2]) + c * Math.Cos(mDecensionArr[2]) * Math.Cos(ha[2]) - z;

			if (Sign(mVHzArr[0]) == Sign(mVHzArr[2]))
				return mVHzArr[2];  // no event this hour

			mVHzArr[1] = s * Math.Sin(mDecensionArr[1]) + c * Math.Cos(mDecensionArr[1]) * Math.Cos(ha[1]) - z;

			a = 2 * mVHzArr[0] - 4 * mVHzArr[1] + 2 * mVHzArr[2];
			b = -3 * mVHzArr[0] + 4 * mVHzArr[1] - mVHzArr[2];
			d = b * b - 4 * a * mVHzArr[0];

			if (d < 0)
				return mVHzArr[2];  // no event this hour

			d = Math.Sqrt(d);
			e = (-b + d) / (2 * a);

			if ((e > 1) || (e < 0))
				e = (-b - d) / (2 * a);

			time = (double)k + e + (double)1 / (double)120; // time of an event

			hr = (int)Math.Floor(time);
			min = (int)Math.Floor((time - hr) * 60);

			hz = ha[0] + e * (ha[2] - ha[0]);                 // azimuth of the sun at the event
			nz = -Math.Cos(mDecensionArr[1]) * Math.Sin(hz);
			dz = c * Math.Sin(mDecensionArr[1]) - s * Math.Cos(mDecensionArr[1]) * Math.Cos(hz);
			az = Math.Atan2(nz, dz) / mDR;
			if (az < 0) az = az + 360;

			if ((mVHzArr[0] < 0) && (mVHzArr[2] > 0))
			{
				mRiseTimeArr[0] = hr;
				mRiseTimeArr[1] = min;
				mIsSunrise = true;
			}

			if ((mVHzArr[0] > 0) && (mVHzArr[2] < 0))
			{
				mSetTimeArr[0] = hr;
				mSetTimeArr[1] = min;
				mIsSunset = true;
			}

			return mVHzArr[2];
		}
	}
}
