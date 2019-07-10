//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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

using Docky.Items;

namespace BatteryMonitor
{
	public class BatteryMonitorItemProvider : AbstractDockItemProvider
	{
		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "BatteryMonitor";
			}
		}
		
		public override bool AutoDisable {
			get { return false; }
		}
		
		#endregion

		BatteryMonitorAbstractItem battery;
		
		bool hidden = true;
		public bool Hidden
		{
			get {
				return hidden;
			}
			set {
				if (hidden == value)
					return;
				hidden = value;
				if (hidden)
					Items = Enumerable.Empty<AbstractDockItem> ();
				else
					Items = battery.AsSingle<AbstractDockItem> ();
			}
		}
		
		public BatteryMonitorItemProvider ()
		{
			// determine what system is available and instantiate the proper item for it
			if (BatteryMonitorUPowerItem.Available)
				battery = new BatteryMonitorUPowerItem (this);
			else if (BatteryMonitorSysItem.Available)
				battery = new BatteryMonitorSysItem (this);
			else
				battery = new BatteryMonitorProcItem (this);
			
			Hidden = false;
			battery.UpdateBattStat ();
		}
	}
}
