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
using System.Xml.Linq;

using Gtk;

using Docky.Services;
using Docky.Widgets;

namespace NPR
{

	[System.ComponentModel.ToolboxItem(true)]
	public partial class StationSearchWidget : Gtk.Bin
	{

		public StationSearchWidget ()
		{
			this.Build ();
			
			ZipEntry.InnerEntry.KeyPressEvent += OnKeyPressed;
			ZipEntry.Show ();
		}
		
		public void ClearResults ()
		{
			tileview.Clear ();
			ZipEntry.InnerEntry.Text = "";
		}
		
		public void ShowMyStations ()
		{
			ClearResults ();
			my_stations.Sensitive = false;
			
			// no need to thread this, MyStations already exist in memory
			NPR.MyStations.ToList ().ForEach (sID => {
				tileview.AppendTile (NPR.LookupStation (sID));
			});
		}
		
		protected virtual void SearchClicked (object sender, System.EventArgs e)
		{			
			my_stations.Sensitive = true;
			
			tileview.Clear ();
			
			DockServices.System.RunOnThread (() => {
				// grab a list of nearby stations, sorted by closeness to the supplied query
				IEnumerable<Station> stations = NPR.SearchStations (ZipEntry.InnerEntry.Text);

				stations.ToList ().ForEach (s => {
					if (s.IsLoaded)
						tileview.AppendTile (s);
					s.FinishedLoading += delegate {
						DockServices.System.RunOnMainThread (() => {
							tileview.AppendTile (s);
						});
					};
				});
			});
		}

		protected virtual void MyStationsClicked (object sender, System.EventArgs e)
		{
			ShowMyStations ();
		}

		[GLib.ConnectBefore]
		protected virtual void OnKeyPressed (object o, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Return)
				Search.Click ();
		}
	}
}