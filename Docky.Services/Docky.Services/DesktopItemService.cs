//  
//  Copyright (C) 2010 Chris Szikszoy
//  Copyright (C) 2011 Robert Dyer
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using GLib;

using Docky.Services.Applications;

namespace Docky.Services
{
	public class DesktopItemService
	{
		public event EventHandler<DesktopFileChangedEventArgs> DesktopFileChanged;
		
		object update_lock;
		
		// shorthand for all registered AND unregistered desktop items
		public IEnumerable<DesktopItem> DesktopItems {
			get { return RegisteredItems.Union (UnregisteredItems); }
		}
		
		public Dictionary<string, string> Remaps { get; private set; }
		List<DesktopItem> RegisteredItems { get; set; }
		List<DesktopItem> UnregisteredItems { get; set; }
		Dictionary<string, List<DesktopItem>> ItemsByExec { get; set; }
		Dictionary<string, DesktopItem> ItemByClass { get; set; }

		public void Initialize ()
		{
			update_lock = new object ();

			Remaps = new Dictionary<string, string> ();
			LoadRemaps (DockServices.Paths.SystemDataFolder.GetChild ("remaps.ini"));
			LoadRemaps (DockServices.Paths.UserDataFolder.GetChild ("remaps.ini"));
			
			// Load DesktopFilesCache from docky.desktop.[LANG].cache
			RegisteredItems = LoadDesktopItemsCache (DockyDesktopFileCacheFile);
			UnregisteredItems = new List<DesktopItem> ();
			
			if (RegisteredItems == null || RegisteredItems.Count () == 0) {
				Log<DesktopItemService>.Info ("Loading *.desktop files and regenerating cache. This may take a while...");
				UpdateDesktopItemsList ();
				ProcessAndMergeAllSystemCacheFiles ();
				SaveDesktopItemsCache ();
			}
			DesktopItemsChanged ();
			
			// Update desktopItems and save cache after 2 minutes just to be sure we are up to date
			GLib.Timeout.Add (2 * 60 * 1000, delegate {
				lock (update_lock) {
					UpdateDesktopItemsList ();
					ProcessAndMergeAllSystemCacheFiles ();
					DesktopItemsChanged ();
					SaveDesktopItemsCache ();
				}
				return false;
			});
			
			// Set up monitors for cache files and desktop directories
			foreach (GLib.File dir in DesktopFileDirectories)
				MonitorDesktopFileDirs (dir);
			MonitorDesktopFileSystemCacheFiles ();
		}
		
		#region public API
		
		/// <summary>
		/// Find all DesktopItems by specifying the exec.
		/// </summary>
		/// <returns>
		/// A List of DesktopItems that use the supplied exec string.
		/// </returns>
		/// <param name='exec'>
		/// An exec string.
		/// </param>
		public IEnumerable<DesktopItem> DesktopItemsFromExec (string exec)
		{
			exec = ExecForPath (exec);
			if (ItemsByExec.ContainsKey (exec))
				return ItemsByExec [exec].AsEnumerable ();
			return Enumerable.Empty<DesktopItem> ();
		}
		
		/// <summary>
		/// Find a DesktopItem by specifying the class.
		/// </summary>
		/// <returns>
		/// The DesktopItem, if any exists.
		/// </returns>
		/// <param name='class'>
		/// The Window class from the .desktop file.
		/// </param>
		public DesktopItem DesktopItemFromClass (string @class)
		{
			if (ItemByClass.ContainsKey (@class))
				return ItemByClass [@class];
			return null;
		}
		
		public IEnumerable<DesktopItem> DesktopItemsFromID (string id)
		{
			IEnumerable<DesktopItem> result = DesktopItems
				.Where (item => item.DesktopID.Equals (id, StringComparison.CurrentCultureIgnoreCase));
			return (result.Any () ? result : Enumerable.Empty<DesktopItem> ());
		}
		
		/// <summary>
		/// Find a DesktopItem by specifying a path to a .desktop file.
		/// </summary>
		/// <returns>
		/// The DesktopItem, if any exists.
		/// </returns>
		/// <param name='file'>
		/// A path to a .desktop file.
		/// </param>
		public DesktopItem DesktopItemFromDesktopFile (string file)
		{
			return DesktopItems
				.Where (item => item.Path.Equals (file, StringComparison.CurrentCultureIgnoreCase))
				.DefaultIfEmpty (null)
				.First ();
		}
		
		public void RegisterDesktopItem (DesktopItem item)
		{
			// check if this item is in either the registered or unregistered items
			if (DesktopItems.Contains (item))
				return;
			
			// if this item isn't in our desktop item list, we need to add it as an unregistered item
			UnregisteredItems.Add (item);
			
			// and make sure we process it
			// FIXME: do we really need to reload _every_ desktop file here? Probably only need to process the new one..
			DesktopItemsChanged ();
		}
		
		#endregion

		void DesktopItemsChanged ()
		{
			BuildExecStrings ();
			BuildClassStrings ();
		}
		
		IEnumerable<GLib.File> DesktopFileSystemCacheFiles
		{
			get {
				return DesktopFileDirectories
					.Select (d => d.GetChild (string.Format ("desktop.{0}.cache", DockServices.System.Locale)))
					.Where (f => f.Exists);
			}
		}
		
		string DockyDesktopFileCacheFile
		{
			get {
				if (!string.IsNullOrEmpty (DockServices.System.Locale))
					return DockServices.Paths.UserCacheFolder.GetChild (string.Format ("docky.desktop.{0}.cache", DockServices.System.Locale)).Path;
				return DockServices.Paths.UserCacheFolder.GetChild ("docky.desktop.cache").Path;
			}
		}
			
		IEnumerable<GLib.File> DesktopFileDirectories
		{
			get {
				return DockServices.Paths.XdgDataDirFolders.Select (d => d.GetChild ("applications"))
					.Union (new [] {
						DockServices.Paths.XdgDataHomeFolder.GetChild ("applications"),
						DockServices.Paths.HomeFolder.GetChild (".cxoffice"),
					})
					.Where (d => d.Exists);
			}
		}
		
		void UpdateDesktopItemsList ()
		{
			if (RegisteredItems == null)
				RegisteredItems = new List<DesktopItem> ();
			
			List<DesktopItem> newItems = new List<DesktopItem> ();
			IEnumerable<DesktopItem> knownItems = DesktopItems;
			
			// Get desktop items for new "valid" desktop files
			newItems = DesktopFileDirectories
				.SelectMany (dir => dir.SubDirs ())
				.Union (DesktopFileDirectories)
				.SelectMany (dir => dir.GetFiles (".desktop"))
				.Where (file => !knownItems.Any (existing => existing.File.Path == file.Path))
				.Select (file => new DesktopItem (file))
				.ToList ();
			
			RegisteredItems.AddRange (newItems);

			if (newItems.Count () > 0) {
				Log<DesktopItemService>.Debug ("{0} new application(s) found.", newItems.Count ());
				foreach (DesktopItem item in newItems)
					Log<DesktopItemService>.Debug ("Adding '{0}'.", item.Path);
			}

			// Check file existence and remove unlinked items
			List<DesktopItem> removeItems = RegisteredItems.Where (item => !item.File.Exists).ToList ();
			if (removeItems.Count > 0) {
				removeItems.ForEach (item => {
					RegisteredItems.Remove (item);
					item.Dispose ();
				});
				Log<DesktopItemService>.Debug ("{0} application(s) removed.", removeItems.Count);
			}
		}
		
		void ProcessAndMergeAllSystemCacheFiles ()
		{
			foreach (GLib.File cache in DesktopFileSystemCacheFiles)
				ProcessAndMergeSystemCacheFile (cache);
		}

		void ProcessAndMergeSystemCacheFile (GLib.File cache)
		{
			if (!cache.Exists)
				return;
			
			Log<DesktopItemService>.Debug ("Processing {0}", cache.Path);
			
			try {
				using (StreamReader reader = new StreamReader (cache.Path)) {
					DesktopItem desktopItem = null;
					string line;
					
					while ((line = reader.ReadLine ()) != null) {
						if (line.Trim ().Length <= 0)
							continue;
						
						if (line.ElementAt (0) == '[') {
							Match match = DesktopItem.sectionRegex.Match (line);
							if (match.Success) {
								string section = match.Groups ["Section"].Value;
								if (section != null) {
									GLib.File file = cache.Parent.GetChild (string.Format ("{0}.desktop", section));
									desktopItem = RegisteredItems.First (item => item.File.Path == file.Path);
									if (desktopItem == null && file.Exists) {
										desktopItem = new DesktopItem (file);
										RegisteredItems.Add (desktopItem);
										Log<DesktopItemService>.Debug ("New application found: {0}", desktopItem.Path);
									}
									continue;
								}
							}
						} else if (desktopItem != null) {
							Match match = DesktopItem.keyValueRegex.Match (line);
							if (match.Success) {
								string key = match.Groups ["Key"].Value;
								string val = match.Groups ["Value"].Value;
								if (!string.IsNullOrEmpty (key) && !string.IsNullOrEmpty (val))
									desktopItem.SetString (key, val);
								continue;
							}
						}
					}
					reader.Close ();
				}
			} catch (Exception e) {
				Log<DesktopItemService>.Error ("Error processing desktop item cache: {0}", e.Message);
				Log<DesktopItemService>.Error (e.StackTrace);
			}
		}

		void LoadRemaps (GLib.File file)
		{
			if (file.Exists) {
				Log<DesktopItemService>.Debug ("Loading remap file '{0}'.", file.Path);
			} else {
				Log<DesktopItemService>.Warn ("Could not find remap file '{0}'!", file.Path);
				return;
			}
			
			Regex keyValueRegex = new Regex (
				@"(^(\s)*(?<Key>([^\=^\n]+))[\s^\n]*\=(\s)*(?<Value>([^\n]+(\n){0,1})))",
				RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.CultureInvariant
			);
			
			try {
				using (StreamReader reader = new StreamReader (file.Path)) {
					string line;
					
					while ((line = reader.ReadLine ()) != null) {
						line = line.Trim ();
						if (line.Length <= 0 || line.Substring (0, 1) == "#")
							continue;
						
						Match match = keyValueRegex.Match (line);
						if (match.Success) {
							string key = match.Groups ["Key"].Value;
							string val = match.Groups ["Value"].Value;
							if (!string.IsNullOrEmpty (key)) {
								Remaps [key] = val;
								Log<DesktopItemService>.Debug ("Remapping '{0}' to '{1}'.", key, val);
							}
						}
					}
					reader.Close ();
				}
			} catch (Exception e) {
				Log<DesktopItemService>.Error ("Error loading remap file: {0}", e.Message);
				Log<DesktopItemService>.Error (e.StackTrace);
			}
		}
		
		List<DesktopItem> LoadDesktopItemsCache (string filename)
		{
			if (!GLib.FileFactory.NewForPath (filename).Exists)
				return null;
				
			Log<DesktopItemService>.Debug ("Loading desktop item cache '{0}'.", DockyDesktopFileCacheFile);
			
			List<DesktopItem> items = new List<DesktopItem> ();
			
			try {
				using (StreamReader reader = new StreamReader (filename)) {
					DesktopItem desktopItem = null;
					string line;
					
					while ((line = reader.ReadLine ()) != null) {
						if (line.Trim ().Length <= 0)
							continue;
						
						if (line.ElementAt (0) == '[') {
							Match match = DesktopItem.sectionRegex.Match (line);
							if (match.Success) {
								string section = match.Groups ["Section"].Value;
								if (section != null) {
									GLib.File file = GLib.FileFactory.NewForPath (section);
									desktopItem = new DesktopItem (file, false);
									items.Add (desktopItem);
								}
								continue;
							}
						} else if (desktopItem != null) {
							Match match = DesktopItem.keyValueRegex.Match (line);
							if (match.Success) {
								string key = match.Groups ["Key"].Value;
								string val = match.Groups ["Value"].Value;
								if (!string.IsNullOrEmpty (key) && !string.IsNullOrEmpty (val))
									desktopItem.SetString (key, val);
								continue;
							}
						}
					}
					reader.Close ();
				}
			} catch (Exception e) {
				Log<DesktopItemService>.Error ("Error loading desktop item cache: {0}", e.Message);
				Log<DesktopItemService>.Error (e.StackTrace);
				return null;
			}

			return items;
		}

		void SaveDesktopItemsCache ()
		{
			Log<DesktopItemService>.Debug ("Saving desktop item cache '{0}'.", DockyDesktopFileCacheFile);

			try {
				using (StreamWriter writer = new StreamWriter (DockyDesktopFileCacheFile, false)) {
					foreach (DesktopItem item in RegisteredItems) {
						writer.WriteLine ("[{0}]", item.Path);
						IDictionaryEnumerator enumerator = item.Values.GetEnumerator ();
						enumerator.Reset ();
						while (enumerator.MoveNext ())
							writer.WriteLine ("{0}={1}", enumerator.Key, enumerator.Value);
						writer.WriteLine ("");
					}
					writer.Close ();
				}
			} catch (Exception e) {
				Log<DesktopItemService>.Error ("Error saving desktop item cache: {0}", e.Message);
				Log<DesktopItemService>.Error (e.StackTrace);
			}
		}

		void MonitorDesktopFileSystemCacheFiles ()
		{
			foreach (GLib.File file in DesktopFileSystemCacheFiles) {
				GLib.FileMonitor mon = file.Monitor (GLib.FileMonitorFlags.None, null);
				mon.RateLimit = 2500;
				mon.Changed += delegate(object o, GLib.ChangedArgs args) {
					if (args.EventType == GLib.FileMonitorEvent.ChangesDoneHint) {
						DockServices.System.RunOnThread (() =>
						{
							lock (update_lock) {
								ProcessAndMergeSystemCacheFile (file);
								DesktopItemsChanged ();
							}
						});
					}
				};
			}
		}
		
		void MonitorDesktopFileDirs (GLib.File dir)
		{
			// build a list of all the subdirectories
			List<GLib.File> dirs = new List<GLib.File> () {dir};
			try {
				dirs = dirs.Union (dir.SubDirs ()).ToList ();	
			} catch {}
			
			foreach (GLib.File d in dirs) {
				GLib.FileMonitor mon = d.Monitor (GLib.FileMonitorFlags.None, null);
				mon.RateLimit = 2500;
				mon.Changed += delegate(object o, GLib.ChangedArgs args) {
					// bug in GIO#, calling args.File or args.OtherFile crashes hard
					GLib.File file = GLib.FileAdapter.GetObject ((GLib.Object) args.Args [0]);
					GLib.File otherFile = GLib.FileAdapter.GetObject ((GLib.Object) args.Args [1]);

					// according to GLib documentation, the change signal runs on the same
					// thread that the monitor was created on.  Without running this part on a thread
					// docky freezes up for about 500-800 ms while the .desktop files are parsed.
					DockServices.System.RunOnThread (() => {
						// if a new directory was created, make sure we watch that dir as well
						if (file.QueryFileType (GLib.FileQueryInfoFlags.NofollowSymlinks, null) == GLib.FileType.Directory)
							MonitorDesktopFileDirs (file);
						
						// we only care about .desktop files
						if (!file.Path.EndsWith (".desktop"))
							return;

						lock (update_lock) {
							UpdateDesktopItemsList ();
							DesktopItemsChanged ();
							SaveDesktopItemsCache();
						}
						
						// Make sure to trigger event on main thread
						DockServices.System.RunOnMainThread (() => {
							if (DesktopFileChanged != null)
								DesktopFileChanged (this, new DesktopFileChangedEventArgs (args.EventType, file, otherFile));
						});
					});
				};
			}
		}

		void BuildClassStrings ()
		{
			ItemByClass = new Dictionary<string, DesktopItem> ();
			
			foreach (DesktopItem item in DesktopItems) {
				if (item == null || item.Ignored || !item.HasAttribute ("StartupWMClass"))
					continue;
				
				string cls = item.GetString ("StartupWMClass").Trim ();
				// we only want exactly 1 launcher, and so if we already have one we use that
				// otherwise it will prefer global over local launchers
				if (!ItemByClass.ContainsKey (cls))
					ItemByClass [cls] = item;
			}
		}
				
		void BuildExecStrings ()
		{
			ItemsByExec = new Dictionary<string, List<DesktopItem>> ();
			
			foreach (DesktopItem item in DesktopItems) {
				if (item == null || item.Ignored || !item.HasAttribute ("Exec"))
					continue;
				
				string exec = item.GetString ("Exec").Trim ();
				string vexec = null;
				
				// for openoffice
				if (exec.Contains (' ') &&
				   (exec.StartsWith ("ooffice") || exec.StartsWith ("openoffice") || exec.StartsWith ("soffice") ||
				    exec.Contains ("/ooffice ") || exec.Contains ("/openoffice.org ") || exec.Contains ("/soffice "))) {
					vexec = "ooffice" + exec.Split (' ')[1];
					vexec = vexec.Replace ("--", "-");
				
				// for libreoffice
				} else if (exec.Contains (' ') &&
				   (exec.StartsWith ("libreoffice"))) {
					vexec = "libreoffice" + exec.Split (' ')[1];
					vexec = vexec.Replace ("--", "-");
				
				// for wine apps
				} else if ((exec.Contains ("env WINEPREFIX=") && exec.Contains (" wine ")) ||
						exec.Contains ("wine ")) {
					int startIndex = exec.IndexOf ("wine ") + 5;
					// length of 'wine '
					// CommandLineForPid already splits based on \\ and takes the last entry, so do the same here
					vexec = exec.Substring (startIndex).Split (new[] { @"\\" }, StringSplitOptions.RemoveEmptyEntries).Last ();
					// remove the trailing " and anything after it
					if (vexec.Contains ("\""))
						vexec = vexec.Substring (0, vexec.IndexOf ("\""));

				// for crossover apps
				} else if (exec.Contains (".cxoffice") || (item.HasAttribute ("X-Created-By") && item.GetString ("X-Created-By").Contains ("cxoffice"))) {
					// The exec is actually another file that uses exec to launch the actual app.
					exec = exec.Replace ("\"", "");
					
					GLib.File launcher = GLib.FileFactory.NewForPath (exec);
					if (!launcher.Exists) {
						Log<DesktopItemService>.Warn ("Crossover launcher decoded as: {0}, but does not exist.", launcher.Path);
						continue;
					}
					
					string execLine = "";
					using (GLib.DataInputStream stream = new GLib.DataInputStream (launcher.Read (null))) {
						ulong len;
						string line;
						try {
							while ((line = stream.ReadLine (out len, null)) != null) {
								if (line.StartsWith ("exec")) {
									execLine = line;
									break;
								}
							}
						} catch (Exception e) {
							Log<DesktopItemService>.Error ("Error reading crossover launher: {0}", e.Message);
							Log<DesktopItemService>.Error (e.StackTrace);
							continue;
						}
					}
	
					// if no exec line was found, bail
					if (string.IsNullOrEmpty (execLine))
						continue;
					
					// get the relevant part from the execLine
					string[] parts = execLine.Split (new[] { '\"' });
					// find the part that contains C:/path/to/app.lnk
					if (parts.Any (part => part.StartsWith ("C:"))) {
						vexec = parts.First (part => part.StartsWith ("C:"));
						// and take only app.lnk (this is what is exposed to ps -ef)
						vexec = ExecForPath (vexec);
					} else {
						continue;
					}
					
				// other apps
				} else {
					string[] parts = exec.Split (' ');
					
					vexec = parts
						.DefaultIfEmpty (null)
						.Select (part => ExecForPath (part))
						.Where (part => !DockServices.WindowMatcher.PrefixFilters.Any (f => f.IsMatch (part)))
						.FirstOrDefault ();
					
					// for AIR apps
					if (vexec != null && vexec.Contains ('\'')) {
						vexec = vexec.Replace ("'", "");
					}
				}
				
				if (vexec == null)
					continue;
				
				if (!ItemsByExec.ContainsKey (vexec))
					ItemsByExec [vexec] = new List<DesktopItem> ();
				
				ItemsByExec [vexec].Add (item);
				foreach (Regex f in DockServices.WindowMatcher.SuffixFilters) {
					if (f.IsMatch (vexec)) {
						string vexecStripped = f.Replace (vexec, "");
						if (!ItemsByExec.ContainsKey (vexecStripped))
							ItemsByExec [vexecStripped] = new List<DesktopItem> ();
						ItemsByExec [vexecStripped].Add (item);
					}
				}
			}
		}
		
		string ExecForPath (string path)
		{
			return path.Split (new [] {'/', '\\'}).Last ();
		}
	}
}

