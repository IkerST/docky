//  
//  Copyright (C) 2010 Benn Snyder, Robert Dyer
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
using System.IO;
using System.Text.RegularExpressions;

using Docky.Items;

namespace BatteryMonitor
{
	public class BatteryMonitorSysItem : BatteryMonitorAbstractItem
	{
		const string BattBasePath = "/sys/class/power_supply";
		const string BattCapacityPath = "charge_full";
		const string BattChargePath = "charge_now";
		const string BattRatePath = "rate_now";
		const string BattStatusPath = "status";
		
		Regex number_regex = new Regex ("[0-9]+");
		
		public static bool Available {
			get {
				return Directory.Exists (BattBasePath);
			}
		}
		
		public BatteryMonitorSysItem (AbstractDockItemProvider owner) : base(owner)
		{
		}
		
		protected override void GetMaxBatteryCapacity ()
		{
			DirectoryInfo basePath = new DirectoryInfo (BattBasePath);                                      

			foreach (DirectoryInfo battDir in basePath.GetDirectories ())
				if (battDir.Name.StartsWith ("BAT")) {
					string path = BattBasePath + "/" + battDir.Name + "/" + BattCapacityPath;
					
					if (System.IO.File.Exists (path))
						try {
							using (StreamReader reader = new StreamReader (path))
								try {
									max_capacity += Convert.ToInt32 (number_regex.Matches (reader.ReadLine ()) [0].Value) / 1000;
								} catch { }
						} catch (IOException) { }
				}
			
			max_capacity = Math.Max (1, max_capacity);
		}
		
		protected override bool GetCurrentBatteryCapacity ()
		{
			string capacity = null;
			string rate = null;
			bool charging = false;
			
			DirectoryInfo basePath = new DirectoryInfo (BattBasePath);
			
			foreach (DirectoryInfo battDir in basePath.GetDirectories ())
				if (battDir.Name.StartsWith("BAT")) {
					string path = BattBasePath + "/" + battDir.Name + "/" + BattChargePath;
					if (System.IO.File.Exists (path)) {
						try {
							using (StreamReader reader = new StreamReader (path))
								capacity = reader.ReadLine ();
						} catch (IOException) { }
						try {
							current_capacity += Convert.ToInt32 (number_regex.Matches (capacity) [0].Value) / 1000;
						} catch { }
					}
				}
			
			foreach (DirectoryInfo battDir in basePath.GetDirectories ())
				if (battDir.Name.StartsWith("BAT")) {
					string path = BattBasePath + "/" + battDir.Name + "/" + BattRatePath;
					if (System.IO.File.Exists (path)) {
						try {                                                   
							using (StreamReader reader = new StreamReader (path))
								rate = reader.ReadLine();
						} catch (IOException) {}
						
						if (rate != "-1000")
							try {
								current_rate += Convert.ToInt32 (number_regex.Matches (rate) [0].Value) / 1000;
							} catch { }
					}
				}

			foreach (DirectoryInfo battDir in basePath.GetDirectories ())
				if (battDir.Name.StartsWith("BAT")) {
					string path = BattBasePath + "/" + battDir.Name + "/" + BattStatusPath;
					if (System.IO.File.Exists (path))
						try {
							using (StreamReader reader = new StreamReader (path))
								charging = !reader.ReadLine().StartsWith ("Discharging");
						} catch (IOException) { }
				}
			
			return charging;
		}
	}
}
