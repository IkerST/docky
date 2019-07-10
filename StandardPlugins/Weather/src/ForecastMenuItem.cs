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

using Docky.Menus;
using Docky.Services;

namespace WeatherDocklet
{
	/// <summary>
	/// A menu button that launches the day's forecast.
	/// </summary>
	public class ForecastMenuItem : IconMenuItem
	{
		int i;
		
		/// <summary>
		/// Creates a new ForecastMenuButtonArgs object.
		/// </summary>
		/// <param name="i">
		/// A <see cref="System.Int32"/> representing the forecast day.
		/// </param>
		/// <param name="description">
		/// A <see cref="System.String"/> representing the description.
		/// </param>
		/// <param name="icon">
		/// A <see cref="System.String"/> representing the icon to display.
		/// </param>
		public ForecastMenuItem (int i, string description, string icon) : base (description, icon)
		{
			this.i = i;
			Clicked += ShowForecast;
		}
		
		public void ShowForecast (object o, EventArgs e)
		{
			DockServices.System.RunOnThread (() => {
				WeatherController.Weather.ShowForecast (i);
			});
		}
	}
}
