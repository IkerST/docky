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
using System.Linq;
using System.Collections.Generic;

using Mono.Unix;

using Docky.Services;
using Docky.Widgets;

namespace WeatherDocklet
{

	[System.ComponentModel.ToolboxItem(true)]
	public partial class WeatherConfig : Gtk.Bin
	{

		public WeatherConfig ()
		{
			this.Build ();
			label2.MnemonicWidget = auto_update;
			location_code.InnerEntry.Activated += delegate {
				search.Click ();
			};
			location_code.Show ();
			
			List<AbstractWeatherSource> sources = WeatherController.Sources.Values.OrderBy (d => d.Name).ToList ();
			
			Shown += delegate {
				auto_update.Value = WeatherPreferences.Timeout;
				metric_units.Active = WeatherPreferences.Metric;
				sources.ForEach (s => provider.AppendText (s.Name));
				provider.Active = sources.IndexOf (sources.First (s => s.Name == WeatherPreferences.Source));
			};
		}
		
		public void ShowMyLocations ()
		{
			location_code.InnerEntry.Text = "";
			results_view.Clear ();
			foreach (string location in WeatherPreferences.Locations)
				results_view.AppendTile (new WeatherTile (location, location));
		}
		
		protected virtual void OnMetricToggled (object sender, System.EventArgs e)
		{
			WeatherPreferences.Metric = metric_units.Active;
		}

		protected virtual void OnUpdateValueChanged (object sender, System.EventArgs e)
		{
			WeatherPreferences.Timeout = (uint) auto_update.ValueAsInt;
		}

		protected virtual void OnProviderChanged (object sender, System.EventArgs e)
		{
			WeatherPreferences.Source = provider.ActiveText;
			provider_info.Text = WeatherController.Weather.About;
			provider_info.Wrap = true;
		}

		protected virtual void OnSearchClicked (object sender, System.EventArgs e)
		{
			DockServices.System.RunOnThread (() => {
				List<string> vals = WeatherController.Weather.SearchLocation (location_code.InnerEntry.Text).ToList ();
				
				DockServices.System.RunOnMainThread (() => {
					results_view.Clear ();
					
					if (vals.Count > 0) {
						for (int i = 0; i < vals.Count; i += 2)
							results_view.AppendTile (new WeatherTile (vals [i], vals [i + 1]));
					} else {
						results_view.AppendTile (new WeatherTile (Catalog.GetString ("No results found."), Catalog.GetString ("Please try your search again.")));
					}
				});
			});
		}

		protected virtual void OnMyLocationsClicked (object sender, System.EventArgs e)
		{
			ShowMyLocations ();
		}
	}
}
