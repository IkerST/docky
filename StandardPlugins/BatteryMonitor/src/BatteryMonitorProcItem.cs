//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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
using System.IO;
using System.Text.RegularExpressions;

using Docky.Items;

namespace BatteryMonitor
{
	public class BatteryMonitorProcItem : BatteryMonitorAbstractItem
	{
		const string BattBasePath = "/proc/acpi/battery";
		const string BattInfoPath = "info";
		const string BattStatePath = "state";
		
		Regex number_regex = new Regex ("[0-9]+");
		
		public static bool Available {
			get {
				return Directory.Exists (BattBasePath);
			}
		}
		
		public BatteryMonitorProcItem (AbstractDockItemProvider owner) : base(owner)
		{
		}
		
		protected override void GetMaxBatteryCapacity ()
		{
			DirectoryInfo basePath = new DirectoryInfo (BattBasePath);
			
			foreach (DirectoryInfo battDir in basePath.GetDirectories ()) {
				string path = BattBasePath + "/" + battDir.Name + "/" + BattInfoPath;
				if (System.IO.File.Exists (path)) {
					using (StreamReader reader = new StreamReader (path)) {
						string line;
						while (!reader.EndOfStream) {
							line = reader.ReadLine ();
							if (!line.StartsWith ("last full capacity"))
								continue;
							
							try {
								max_capacity += Convert.ToInt32 (number_regex.Matches (line) [0].Value);
							} catch { }
						}
					}
				}
			}
			
			max_capacity = Math.Max (1, max_capacity);
		}
		
		protected override bool GetCurrentBatteryCapacity ()
		{
			string capacity = null;
			string rate = null;
			bool charging = false;
			
			DirectoryInfo basePath = new DirectoryInfo (BattBasePath);
			
			foreach (DirectoryInfo battDir in basePath.GetDirectories ()) {
				string path = BattBasePath + "/" + battDir.Name + "/" + BattStatePath;
				if (System.IO.File.Exists (path)) {
					try {
						using (StreamReader reader = new StreamReader (path)) {
							while (!reader.EndOfStream) {
								string line = reader.ReadLine ();
								
								if (line.StartsWith ("remaining capacity")) {
									capacity = line;
								} else if (line.StartsWith ("present rate")) {
									rate = line;
								} else if (line.StartsWith ("charging state")) {
									if (line.EndsWith ("discharging"))
										charging = false;
									else
										charging = true;
 								}
							}
						}
					} catch (IOException) {}
					
					try {
						current_capacity += Convert.ToInt32 (number_regex.Matches (capacity) [0].Value);
						current_rate += Convert.ToInt32 (number_regex.Matches (rate) [0].Value);
					} catch { }
				}
			}
			
			return charging;
		}
	}
}
