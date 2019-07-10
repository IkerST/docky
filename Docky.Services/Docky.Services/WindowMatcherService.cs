//  
//  Copyright (C) 2009-2010 Jason Smith, Robert Dyer, Chris Szikszoy, 
//                          Rico Tzschichholz
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
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Wnck;

using Docky.Services;
using Docky.Services.Applications;

namespace Docky.Services
{
	public class WindowMatcherService
	{
		public void Initialize ()
		{
			// Initialize window matching with currently available windows
			DesktopItemsByWindow = new Dictionary<Window, List<DesktopItem>> ();

			foreach (Window w in Wnck.Screen.Default.Windows)
				SetupWindow (w);

			Wnck.Screen.Default.WindowOpened += WnckScreenDefaultWindowOpened;
			Wnck.Screen.Default.WindowClosed += WnckScreenDefaultWindowClosed;
		}
		
		IEnumerable<Window> UnmatchedWindows {
			get {
				IEnumerable<Window> matched = DesktopItemsByWindow.Keys.Cast<Window> ();
				return Wnck.Screen.Default.Windows.Where (w => !w.IsSkipTasklist && !matched.Contains (w));
			}
		}
		
		Dictionary<Window, List<DesktopItem>> DesktopItemsByWindow;
		
		#region public API
		
		public IEnumerable<Window> WindowsForDesktopItem (DesktopItem item)
		{
			if (item == null)
				throw new ArgumentNullException ("DesktopItem item cannot be null.");
			

			foreach (KeyValuePair<Window, List<DesktopItem>> kvp in DesktopItemsByWindow)
				if (kvp.Value.Any (df => df == item))
					yield return kvp.Key;
		}
		
		public DesktopItem DesktopItemForWindow (Window window)
		{
			if (window == null)
				throw new ArgumentNullException ("Window window cannot be null.");
			
			List<DesktopItem> matches;
			if (DesktopItemsByWindow.TryGetValue (window, out matches)) {
				DesktopItem useritem = matches.Find (item => item.File.Path.StartsWith (DockServices.Paths.HomeFolder.Path));
				if (useritem != null)
					return useritem;
				return matches.FirstOrDefault ();
			}
			
			return null;
		}
		
		public List<Regex> PrefixFilters {
			get {
				return BuildPrefixFilters ();
			}
		}
		public List<Regex> SuffixFilters {
			get {
				return BuildSuffixFilters ();
			}
		}
		
		public IEnumerable<Window> SimilarWindows (Window window)
		{
			if (window == null)
				throw new ArgumentNullException ("Window cannot be null.");
			
			//TODO perhaps make it a bit smarter
			if (!DesktopItemsByWindow.ContainsKey (window))
				foreach (Window win in UnmatchedWindows) {
					if (win == window)
						continue;
					
					if (win.Pid == window.Pid)
						yield return win; else if (window.Pid <= 1) {
						if (window.ClassGroup != null && win.ClassGroup != null && !string.IsNullOrEmpty (window.ClassGroup.ResClass) && !string.IsNullOrEmpty (win.ClassGroup.ResClass) && win.ClassGroup.ResClass.Equals (window.ClassGroup.ResClass))
							yield return win; else if (!string.IsNullOrEmpty (win.Name) && win.Name.Equals (window.Name))
							yield return win;
					}
				}
			
			yield return window;
		}
		
		public bool WindowIsReadyForMatch (Window window)
		{
			if (!WindowIsOpenOffice (window) && !WindowIsLibreOffice (window))
				return true;
			
			return SetupWindow (window);
		}
		
		public bool WindowIsOpenOffice (Window window)
		{
			return window.ClassGroup != null && window.ClassGroup.Name.ToLower ().StartsWith ("openoffice");
		}

		public bool WindowIsLibreOffice (Window window)
		{
			return window.ClassGroup != null && window.ClassGroup.Name.ToLower ().StartsWith ("libreoffice");
		}

		#endregion
		
		#region Window Setup
		
		void WnckScreenDefaultWindowOpened (object o, WindowOpenedArgs args)
		{
			SetupWindow (args.Window);
		}

		void WnckScreenDefaultWindowClosed (object o, WindowClosedArgs args)
		{
			if (args.Window != null)
				DesktopItemsByWindow.Remove (args.Window);
		}
		
		bool SetupWindow (Window window)
		{
			IEnumerable<DesktopItem> items = DesktopItemsForWindow (window);
			if (items.Any ()) {
				DesktopItemsByWindow [window] = items.ToList ();
				return true;
			} else {
				return false;
			}
		}
		
		#endregion

		#region Window Matching
		
		IEnumerable<DesktopItem> DesktopItemsForWindow (Window window)
		{
			// use the StartupWMClass as the definitive match
			if (window.ClassGroup != null
					&& !string.IsNullOrEmpty (window.ClassGroup.ResClass)
					&& window.ClassGroup.ResClass != "Wine"
					&& DockServices.DesktopItems.DesktopItemFromClass (window.ClassGroup.ResClass) != null) {
				yield return DockServices.DesktopItems.DesktopItemFromClass (window.ClassGroup.ResClass);
				yield break;
			}
			
			int pid = window.Pid;
			if (pid <= 1) {
				if (window.ClassGroup != null && !string.IsNullOrEmpty (window.ClassGroup.ResClass)) {
					IEnumerable<DesktopItem> matches = DockServices.DesktopItems.DesktopItemsFromID (window.ClassGroup.ResClass);
					if (matches.Any ())
						foreach (DesktopItem s in matches)
							yield return s;
				}
				yield break;
			}
			
			bool matched = false;
			
			// get ppid and parents
			IEnumerable<int> pids = PidAndParents (pid);
			// this list holds a list of the command line parts from left (0) to right (n)
			List<string> commandLine = new List<string> ();
			
			// if we have a classname that matches a desktopid we have a winner
			if (window.ClassGroup != null) {
				if (WindowIsOpenOffice (window)) {
					string title = window.Name.Trim ();
					if (title.EndsWith ("Writer"))
						commandLine.Add ("ooffice-writer");
					else if (title.EndsWith ("Draw"))
						commandLine.Add ("ooffice-draw");
					else if (title.EndsWith ("Impress"))
						commandLine.Add ("ooffice-impress");
					else if (title.EndsWith ("Calc"))
						commandLine.Add ("ooffice-calc");
					else if (title.EndsWith ("Math"))
						commandLine.Add ("ooffice-math");
				} else if (WindowIsLibreOffice (window)) {
					string title = window.Name.Trim ();
					if (title.EndsWith ("Writer"))
						commandLine.Add ("libreoffice-writer");
					else if (title.EndsWith ("Draw"))
						commandLine.Add ("libreoffice-draw");
					else if (title.EndsWith ("Impress"))
						commandLine.Add ("libreoffice-impress");
					else if (title.EndsWith ("Calc"))
						commandLine.Add ("libreoffice-calc");
					else if (title.EndsWith ("Math"))
						commandLine.Add ("libreoffice-math");
				} else if (window.ClassGroup.ResClass == "Wine") {
					// we can match Wine apps normally so don't do anything here
				} else {
					string className = window.ClassGroup.ResClass.Replace (".", "");
					IEnumerable<DesktopItem> matches = DockServices.DesktopItems.DesktopItemsFromID (className);
					
					if (matches.Any ()) {
						foreach (DesktopItem s in matches) {
							yield return s;
							matched = true;
						}
					}
				}
			}
			
			foreach (int currentPid in pids) {
				// do a match on the process name
				string name = NameForPid (currentPid);
				foreach (DesktopItem s in DockServices.DesktopItems.DesktopItemsFromExec (name)) {
					yield return s;
					matched = true;
				}
				
				// otherwise do a match on the commandline
				commandLine.AddRange (CommandLineForPid (currentPid)
					.Select (cmd => cmd.Replace (@"\", @"\\")));
				
				if (commandLine.Count () == 0)
					continue;
				
				foreach (string cmd in commandLine) {
					foreach (DesktopItem s in DockServices.DesktopItems.DesktopItemsFromExec (cmd)) {
						yield return s;
						matched = true;
					}
					if (matched)
						break;
				}
				
				// if we found a match, bail.
				if (matched)
					yield break;
			}
			
			yield break;
		}

		IEnumerable<string> PrefixStrings {
			get {
				yield return "gksu(do)?";
				yield return "sudo";
				yield return "java";
				yield return "mono";
				yield return "ruby";
				yield return "padsp";
				yield return "perl";
				yield return "aoss";
				yield return "python(\\d+.\\d+)?";
				yield return "wish(\\d+\\.\\d+)?";
				yield return "(ba)?sh";
				yield return "-.*";
				yield return "*.\\.desktop";
			}
		}
		
		List<Regex> BuildPrefixFilters ()
		{
			return new List<Regex> (PrefixStrings.Select (s => new Regex ("^" + s + "$")));
		}
		
		IEnumerable<string> SuffixStrings {
			get {
				// some wine apps are launched via a shell script that sets the proc name to "app.exe"
				yield return "\\.exe";
				// some apps have a script 'foo' which does 'exec foo-bin' or 'exec foo.bin'
				yield return "[.-]bin";
				// some python apps have a script 'foo' for 'python foo.py'
				yield return "\\.py";
				// some apps append versions, such as '-1' or '-3.0'
				yield return "(-)?\\d+(\\.\\d+)?";
			}
		}

		List<Regex> BuildSuffixFilters ()
		{
			return new List<Regex> (SuffixStrings.Select (s => new Regex (s + "$")));
		}
		
		IEnumerable<int> PidAndParents (int pid)
		{
			string cmdline;

			do {
				yield return pid;
				
				try {
					string procPath = new [] { "/proc", pid.ToString (), "stat" }.Aggregate (Path.Combine);
					using (StreamReader reader = new StreamReader (procPath)) {
						cmdline = reader.ReadLine ();
						reader.Close ();
					}
				} catch { 
					yield break; 
				}
				
				if (cmdline == null)
					yield break;
				
				string [] result = cmdline.Split (Convert.ToChar (0x0)) [0].Split (' ');

				if (result.Count () < 4)
					yield break;
				
				// the ppid is index number 3
				if (!int.TryParse (result [3], out pid))
					yield break;
			} while (pid > 1);
		}
		
		IEnumerable<string> CommandLineForPid (int pid)
		{
			string cmdline;

			try {
				string procPath = new [] { "/proc", pid.ToString (), "cmdline" }.Aggregate (Path.Combine);
				using (StreamReader reader = new StreamReader (procPath)) {
					cmdline = reader.ReadLine ();
					reader.Close ();
				}
			} catch { yield break; }
			
			if (cmdline == null)
				yield break;
			
			cmdline = cmdline.Trim ();
						
			string [] result = cmdline.Split (Convert.ToChar (0x0));
			
			// these are sanitized results
			foreach (string sanitizedCmd in result
				.Select (s => s.Split (new []{'/', '\\'}).Last ())
			    .Distinct ()
				.Where (s => !string.IsNullOrEmpty (s) && !PrefixFilters.Any (f => f.IsMatch (s)))) {
				
				yield return sanitizedCmd;
				
				if (DockServices.DesktopItems.Remaps.ContainsKey (sanitizedCmd))
					yield return DockServices.DesktopItems.Remaps [sanitizedCmd];
				
				// if it ends with a special suffix, strip the suffix and return an additional result
				foreach (Regex f in SuffixFilters)
					if (f.IsMatch (sanitizedCmd))
						yield return f.Replace (sanitizedCmd, "");
			}
			
			// return the entire cmdline last as a last ditch effort to find a match
			yield return cmdline;
		}
		
		string NameForPid (int pid)
		{
			string name;

			try {
				string procPath = new [] { "/proc", pid.ToString (), "status" }.Aggregate (Path.Combine);
				using (StreamReader reader = new StreamReader (procPath)) {
					name = reader.ReadLine ();
					reader.Close ();
				}
			} catch { return ""; }
			
			if (string.IsNullOrEmpty (name) || !name.StartsWith ("Name:"))
				return "";
			
			return name.Substring (6);
		}
		
		#endregion
	}
}
