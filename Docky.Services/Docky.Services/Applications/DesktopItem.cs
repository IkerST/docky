//  
//  Copyright (C) 2009-2010 Jason Smith, Rico Tzschichholz
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
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using GLib;

using Docky.Services;

namespace Docky.Services.Applications
{
	public class DesktopItem : IDisposable
	{
		public static Regex keyValueRegex = new Regex (
			@"(^(\s)*(?<Key>([^\=^\n]+))[\s^\n]*\=(\s)*(?<Value>([^\n]+(\n){0,1})))",
			RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.CultureInvariant
		);
		public static Regex sectionRegex = new Regex (
			@"(^(\[)*(?<Section>([^\]^\n]+))\]$)" ,
			RegexOptions.Compiled | RegexOptions.CultureInvariant
		);
		public static Regex localizedKeyRegex = new Regex (
			@"(^(\s)*(?<PureKey>([^\[^\n]+))\[(?<Locale>([^\]^\n]+))\])" ,
			RegexOptions.Compiled | RegexOptions.CultureInvariant
		);
		public static Regex execAcceptsDropRegex = new Regex (
			@"%[fFuU]",
			RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.CultureInvariant
		);

		IEnumerable<string> Locales {
			get {
				return PostfixStringsForLocale (DockServices.System.Locale);
			}
		}
		
		#region Delayed change propagation
		
		public event EventHandler HasChanged;
		private uint trigger_changed_timer = 0;
		private object onchanged_lock = new object ();
		protected void OnChanged ()
		{
			if (HasChanged != null) {
				lock (onchanged_lock) {
					// collect changes within 2500ms then trigger events
					if (trigger_changed_timer == 0) {
						trigger_changed_timer = GLib.Timeout.Add (2500, delegate {
							trigger_changed_timer = 0;
							if (HasChanged != null)
								HasChanged (this, null);
							return false;
						});
					}
				}
			}
		}
		
		#endregion

		public Dictionary<string, string> Values { get; set; }

		GLib.FileMonitor monitor;
		GLib.File file;
		public GLib.File File {
			get {
				return file;
			}
			private set {
				file = value;
				if (monitor != null) {
					monitor.Cancel ();
					monitor.Changed -= HandleFileChanged;
					monitor.Dispose ();
				}
				monitor = file.Monitor (GLib.FileMonitorFlags.None, null);
				monitor.RateLimit = 2500;
				monitor.Changed += HandleFileChanged;
			}
		}
		
		public string Path { 
			get { return File.Path; }
		}
		
		public Uri Uri {
			get { return File.Uri; }
		}

		public string DesktopID {
			get { return System.IO.Path.GetFileNameWithoutExtension (Path); }
		}

		public bool AcceptsDrops {
			get {
				return HasAttribute ("Exec") && execAcceptsDropRegex.Match (GetString ("Exec")).Success;
			}
		}
		
		public bool Ignored {
			get {
				return ((HasAttribute ("NoDisplay") && GetBool ("NoDisplay"))
					|| (HasAttribute ("Hidden") && GetBool ("Hidden"))
					|| (HasAttribute ("X-Docky-NoMatch") && GetBool ("X-Docky-NoMatch")));
			}
		}
		
		public DesktopItem (GLib.File file, bool load_file)
		{
			if (file == null)
				throw new ArgumentNullException ("DesktopItem can't be initialized with a null file object");
			
			File = file;
			
			if (load_file)
				Values = GetValuesFromFile ();
			else
				Values = new Dictionary<string, string> ();
		}

		public DesktopItem (GLib.File file) : this (file, true)
		{
		}
		
		public DesktopItem (Uri uri) : this (GLib.FileFactory.NewForUri (uri))
		{
		}

		public DesktopItem (string path) : this (GLib.FileFactory.NewForPath (path))
		{
		}

		public bool HasAttribute (string key)
		{
			return Values.ContainsKey (key);
		}

		public string GetString (string key)
		{
			string result;
			if (Values.TryGetValue (key, out result))
				return result;
			return null;
		}
		
		public void SetString (string key, string val) 
		{
			string result;
			if (Values.TryGetValue (key, out result)) {
				if (result.Equals (val))
					return;
				Values.Remove (key);
			}
			Values.Add (key, val);
			
			OnChanged ();
		}

		public IEnumerable<string> GetStrings (string key)
		{
			string result = GetString (key);
			if (result == null)
				return Enumerable.Empty<string> ();
			
			return result.Split (';');
		}

		public bool GetBool (string key)
		{
			string result = GetStrings (key).First ();

			bool val;
			if (bool.TryParse (result, out val))
				return val;
			
			return false;
		}

		public double GetDouble (string key)
		{
			string result = GetStrings (key).First ();
			
			double val;
			if (double.TryParse (result, out val))
				return val;
			
			return 0;
		}
		
		public void Launch (IEnumerable<string> uris)
		{
			DockServices.System.Launch (File, uris.Select (uri => GLib.FileFactory.NewForUri (uri)));
		}

		Dictionary<string, string> GetValuesFromFile ()
		{
			Dictionary<string, string> result = new Dictionary<string, string> ();

			if (!File.Exists)
				return result;
			
			try {
				using (StreamReader reader = new StreamReader (Path))
				{
					bool desktop_entry_found = false;
					Match match;
					string line;

					while ((line = reader.ReadLine ()) != null) {
						
						if (line.Trim ().Length <= 0)
							continue;
						
						if (!desktop_entry_found) {
							
							match = sectionRegex.Match (line);
							if (match.Success) {
								string section = match.Groups["Section"].Value;
								desktop_entry_found = string.Equals (section, "Desktop Entry");
							}
						
						} else {
							
							//Only add unlocalized values and values matching the current locale
							match = keyValueRegex.Match (line);
							if (match.Success) {
								string key = match.Groups["Key"].Value;
								string val = match.Groups["Value"].Value;
								if (!string.IsNullOrEmpty (key) && !string.IsNullOrEmpty (val) && !result.ContainsKey (key)) {
									match = localizedKeyRegex.Match (key);
									if (match.Success) {
										if (Locales.Contains (match.Groups["Locale"].Value)) {
											//Remove existing value in favour of this localized one
											result.Remove (match.Groups["PureKey"].Value);
											result.Add (match.Groups["PureKey"].Value, val);
										}
									} else {
										if (!result.ContainsKey (key))
											result.Add (key, val);
									}
								}
							
							} else if (sectionRegex.Match (line).Success)
								break;
						}
					}
					reader.Close ();
				}

			} catch (Exception e) {
				Log<DesktopItem>.Error ("Failed getting values from desktop file '{0}' : {1}", Path, e.Message);
				Log<DesktopItem>.Error (e.StackTrace);
			}

			return result;
		}
		
		IEnumerable<string> PostfixStringsForLocale (string locale)
		{
			if (string.IsNullOrEmpty (locale) || locale.Length < 2)
				yield break;
			
			if (locale.Contains (".")) {
				locale = Regex.Replace (locale, "\\..+(?<end>@*)", "${end}");
			}
			yield return locale;
			
			if (locale.Contains ("@")) {
				string noMod = Regex.Replace (locale, "@*", "");
				yield return noMod;
			}
			
			if (locale.Contains ("_")) {
				string noCountry = Regex.Replace (locale, "_..", "");
				yield return noCountry;
			}
			
			yield return locale.Substring (0, 2);
		}
		
		void HandleFileChanged (object o, ChangedArgs args)
		{
			Log<DesktopItem>.Debug ("file {0} changed", File.Path);
			
			Values = GetValuesFromFile ();
			
			OnChanged ();
		}

		#region IDisposable implementation
		
		public void Dispose ()
		{
			Values.Clear ();

			if (monitor != null) {
				monitor.Cancel ();
				monitor.Changed -= HandleFileChanged;
				monitor.Dispose ();
			}
		}
		
		#endregion
		
	}
}
