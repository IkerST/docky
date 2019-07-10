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
using System.Linq;
using System.Web;
using Mono.Unix;

using Docky.Items;

namespace WeatherDocklet
{
	/// <summary>
	/// A <see cref="Do.Universe.ItemSource"/> for <see cref="WeatherItem"/>s.
	/// </summary>
	public class WeatherItemProvider : AbstractDockItemProvider
	{
		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "Weather";
			}
		}
		
		#endregion
		
		AbstractDockItem item;
		
		public WeatherItemProvider ()
		{
			item = new WeatherDocklet ();
			
			Items = item.AsSingle<AbstractDockItem> ();
		}
		
		public override void Registered ()
		{
			GLib.Idle.Add (delegate {
				WeatherController.ResetTimer ();
				return false;
			});
		}
		
		public override void Unregistered ()
		{
			WeatherController.StopTimer ();
		}
	}
}
