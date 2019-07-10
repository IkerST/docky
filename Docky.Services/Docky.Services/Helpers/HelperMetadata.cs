//  
//  Copyright (C) 2009 Chris Szikszoy, Robert Dyer
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
using System.Text.RegularExpressions;

using GLib;
using Gdk;

namespace Docky.Services.Helpers
{
	public class HelperMetadata
	{
		const string NameTag = "Name";
		const string DescTag = "Description";
		const string IconTag = "Icon";
		const string AppUriTag = "AppName";
		
		public string Name { get; private set; }
		public string Description { get; private set; }
		public Pixbuf Icon { get; private set; }
		public File IconFile { get; private set; }
		public string AppUri { get; private set; }
		public File DataFile { get; private set; }
		
		public event EventHandler DataReady;

		public HelperMetadata (File dataFile)
		{
			DataFile = dataFile;
			IconFile = null;
			dataFile.ReadAsync (0, null, DataRead);
		}
		
		void OnDataReady ()
		{
			if (DataReady != null)
				DataReady (this, null);
		}
		
		void DataRead (GLib.Object obj, GLib.AsyncResult res) 
		{
			File file = FileAdapter.GetObject (obj);
			
			Regex keyValueRegex = new Regex (
				@"(^(\s)*(?<Key>([^\=^\n]+))[\s^\n]*\=(\s)*(?<Value>([^\n]+(\n){0,1})))",
				RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | 
				RegexOptions.CultureInvariant
			);

			using (DataInputStream stream = new DataInputStream (file.ReadFinish (res))) {
				ulong len;
				string line;
				while ((line = stream.ReadLine (out len, null)) != null) {
					line = line.Trim ();
					
					Match match = keyValueRegex.Match (line);
					if (match.Success) {
						string key = match.Groups["Key"].Value;
						string val = match.Groups["Value"].Value;
						
						if (key.Equals (NameTag)) {
							Name = val;
						} else if (key.Equals (DescTag)) {
							Description = val;
						} else if (key.Equals (AppUriTag)) {
							AppUri = val;
						} else if (key.Equals (IconTag)) {
							if (val.StartsWith ("./") && val.Length > 2) {
								IconFile = file.Parent.GetChild (val.Substring (2));
								if (IconFile.Exists)
									Icon = DockServices.Drawing.LoadIcon (IconFile.Path + ";;extension");
							} else {
								Icon = DockServices.Drawing.LoadIcon (val + ";;extension", 128);
							}
						}
					}
				}
			}
			OnDataReady ();
		}
		
		public void Dispose ()
		{
			if (Icon != null)
				Icon.Dispose ();
		}
	}
}
