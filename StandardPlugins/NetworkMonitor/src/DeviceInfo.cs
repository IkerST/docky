//  
//  Copyright (C) 2011 Florian Dorn, Rico Tzschichholz, Robert Dyer
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

namespace NetworkMonitorDocklet 
{
	class DeviceInfo 
	{
		public string name;
		public long uploadedBytes = 0;
		public long downloadedBytes = 0;
		public double uploadRate = 0.0;
		public double downloadRate = 0.0;
		public DateTime lastUpdated;
		
		public long TotalBytes {
			get { return uploadedBytes + downloadedBytes; }
		}
		
		public double TotalRate {
			get { return uploadRate + downloadRate; }
		}
		
		public DeviceInfo (string _name) : this (_name, 0, 0)
		{
		}

		public DeviceInfo (string _name, long _downloadedBytes, long _uploadedBytes)
		{
			name = _name;
			
			lastUpdated = DateTime.Now;

			downloadedBytes = _downloadedBytes;
			uploadedBytes = _uploadedBytes;
		}

		public void Update (long new_downloadedBytes, long new_uploadedBytes)
		{
			var now = DateTime.Now;
			
			uploadRate = (new_uploadedBytes - uploadedBytes) / (now - lastUpdated).TotalSeconds;
			downloadRate = (new_downloadedBytes - downloadedBytes) / (now - lastUpdated).TotalSeconds;
			
			uploadedBytes = new_uploadedBytes;
			downloadedBytes = new_downloadedBytes;

			lastUpdated = now;
		}
		
		public override string ToString ()
		{
			return string.Format ("{0}: {2,10} down {1,10} up (Total: {3}/{4})",
			                      name,
			                      BytesToFormattedString (uploadRate, true),
			                      BytesToFormattedString (downloadRate, true),
			                      BytesToFormattedString (uploadedBytes),
			                      BytesToFormattedString (downloadedBytes));
		}
		
		public string FormatUpDown (bool up)
		{
			double rate = downloadRate;
			
			if (up)
				rate = uploadRate;
			
			if (rate < 1)
				return "-";
			
			return BytesToFormattedString (rate, true);
		}
		
		static string BytesToFormattedString (double bytes)
		{
			return BytesToFormattedString (bytes, false);
		}
		
		static string BytesToFormattedString (double bytes, bool per_sec)
		{
			var suffix = new string[] { "B", "K", "M", "G", "T", "P", "E" };
			int depth = 0;
			
			while (bytes >= 1000 && depth < suffix.Length-1) {
				bytes /= 1024;
				depth++;
			}
			string unit = suffix[depth];
			if (per_sec)
				unit = string.Format ("{0}/s", unit);
			
			if (bytes > 100 || depth == 0)
				return string.Format ("{0:0} {1}", bytes, unit);
			
			return string.Format ("{0:0.0} {1}", bytes, unit);
		}
	}
}
