//  
//  Copyright (C) 2009 Chris Szikszoy
//  Copyright (C) 2010 Robert Dyer
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

using Docky.Items;
using Docky.Services;

namespace NPR
{
	public class NPRItemProvider : AbstractDockItemProvider
	{
		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "NPR";
			}
		}
		
		#endregion
		
		List<AbstractDockItem> items = new List<AbstractDockItem> ();
		
		public NPRItemProvider ()
		{
			ReloadStations ();
			
			NPR.StationsUpdated += delegate (object sender, StationsUpdatedEventArgs args) {
				switch (args.UpdateAction) {
				case StationUpdateAction.Added:
					AddStation (args.Station);
					break;
				case StationUpdateAction.Removed:
					RemoveStation (args.Station);
					break;
				}
			};
		}
		
		void AddStation (Station station)
		{
			items.Add (new StationDockItem (station));
			
			items.Cast <StationDockItem> ().Where (s => s.OwnedStation.ID < 0).ToList ().ForEach (sdi => {
				items.Remove (sdi);
				sdi.Dispose ();
			});
			
			Items = items;
		}
		
		void RemoveStation (Station station)
		{
			StationDockItem sdi = items.Cast <StationDockItem> ().Where (s => s.OwnedStation == station).First ();
			
			items.Remove (sdi);
			
			MaybeAddNullStation ();
			
			Items = items;
			
			sdi.Dispose ();
		}
		
		void MaybeAddNullStation ()
		{
			if (items.Count () == 0)
				items.Add (new StationDockItem (NPR.LookupStation (-1)));
		}
		
		public void ReloadStations ()
		{
			items.Clear ();
			
			NPR.MyStations.ToList ().ForEach (s => {
				items.Add (new StationDockItem (NPR.LookupStation (s)));
			});
			
			MaybeAddNullStation ();
				
			Items = items;
		}
	}
}
