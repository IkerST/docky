//  
//  Copyright (C) 2009 Chris Szikszoy, Robert Dyer
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
using IO = System.IO;
using System.Linq;
using System.Collections.Generic;

using GLib;

using Docky.Services.Helpers;
using Docky.Services.Prefs;

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Tar;

namespace Docky.Services
{
	public class HelperService
	{
		public static File UserDir = DockServices.Paths.DockManagerUserDataFolder;
		static File UserScriptsDir = UserDir.GetChild ("scripts");
		public static File UserMetaDir = UserDir.GetChild ("metadata");
		
		public static File SysDir = FileFactory.NewForPath ("/usr/share/dockmanager/");
		static File SysScriptsDir = SysDir.GetChild ("scripts");
		public static File SysMetaDir = SysDir.GetChild ("metadata");
		
		public static File SysLocalDir = FileFactory.NewForPath ("/usr/local/share/dockmanager/");
		static File SysLocalScriptsDir = SysLocalDir.GetChild ("scripts");
		public static File SysLocalMetaDir = SysLocalDir.GetChild ("metadata");
		
		IEnumerable<GLib.File> HelperDirs = new [] {
			UserScriptsDir,
			SysLocalScriptsDir,
			SysScriptsDir,
		}.Where (dir => dir.Exists).Distinct (new FileEqualityComparer ());
		
		public event EventHandler<HelperStatusChangedEventArgs> HelperStatusChanged;
		public event EventHandler HelperInstalled;
		public event EventHandler HelperUninstalled;
		
		public bool ShowOutput {
			get {
				return prefs.Get<bool> ("ShowOutput", true);
			}
			set {
				if (ShowOutput == value)
					return;
				prefs.Set<bool> ("ShowOutput", value);
			}
		}

		static IPreferences prefs = DockServices.Preferences.Get<HelperService> ();
		
		List<Helper> helpers;
		public List<Helper> Helpers {
			get { return helpers; }
			private set {
				IEnumerable<Helper> added = value.Where (h => !helpers.Contains (h));
				IEnumerable<Helper> removed = helpers.Where (h => !value.Contains (h));
				
				if (!added.Any () && !removed.Any ())
					return;

				foreach (Helper h in removed) {
					Log<HelperService>.Info ("Helper removed: {0}", h.File.Path);
					h.HelperStatusChanged -= OnHelperStatusChanged;
					h.Dispose ();
				}

				foreach (Helper h in added) {
					Log<HelperService>.Info ("Helper added: {0}", h.File.Path);
					h.HelperStatusChanged += OnHelperStatusChanged;
				}
				
				helpers = value;

				OnHelpersChanged (added, removed);
			}
		}
		
		List<FileMonitor> monitors = new List<FileMonitor> ();
		
		public void Initialize ()
		{
			helpers = new List<Helper> ();
			
			// set up the file monitors to watch our script directories
			foreach (File dir in HelperDirs) {
				FileMonitor mon = dir.Monitor (0, null);
				monitors.Add (mon);
				mon.RateLimit = 5000;
				mon.Changed += HandleMonitorChanged;
			}
			
			GLib.Timeout.Add (2000, delegate {
				UpdateHelpers ();
				return false;
			});
		}
		
		void HandleMonitorChanged (object o, ChangedArgs args)
		{
			if (args.EventType == FileMonitorEvent.Created || args.EventType == FileMonitorEvent.Deleted)
				UpdateHelpers ();
		}

		void UpdateHelpers ()
		{
			List<Helper> helpers = Helpers.ToList ();
			
			//Remove deleted helpers
			helpers.RemoveAll (h => !h.File.Exists);
			
			//Find new helper files and filter them:
			// get all files in helpers directories
			// remove all files which already have a helper and don't overrule their files
			// remove all files which are overruled by other files
			List<File> new_files = HelperDirs
				.SelectMany (d => d.GetFiles (""))
				.Where (file => !file.Basename.EndsWith ("~"))
				.ToList ();
			new_files.RemoveAll (file => helpers.Exists (h => h.File.Path == file.Path || (h.File.Basename == file.Basename 
					&& h.File.Path != file.Path && h.File.Path.StartsWith (DockServices.Paths.DockManagerUserDataFolder.Path))));
			new_files.RemoveAll (file => new_files.Exists (f => f.Basename == file.Basename 
					&& f.Path != file.Path && !file.Path.StartsWith (DockServices.Paths.DockManagerUserDataFolder.Path)));

			//Remove system-helpers which are overruled by new user-helpers
			helpers.RemoveAll (helper => new_files.Exists (f => f.Basename == helper.File.Basename));

			//Create and Add new helpers
			helpers.AddRange (new_files.Select (f => new Helper (f)).ToList ());

			new_files.Clear ();
			
			Helpers = helpers;
		}
		
		void OnHelperStatusChanged (object o, HelperStatusChangedEventArgs args)
		{
			if (HelperStatusChanged != null)
				HelperStatusChanged (o, args);
		}
		
		void OnHelpersChanged (IEnumerable<Helper> added, IEnumerable<Helper> removed)
		{
			if (added.Any () &&  HelperInstalled != null)
				HelperInstalled (this, EventArgs.Empty);
			
			if (removed.Any () && HelperUninstalled != null)
				HelperUninstalled (this, EventArgs.Empty);
		}
		
		public bool InstallHelper (string path)
		{
			File file = FileFactory.NewForPath (path);
			
			if (!file.Exists)
				return false;
			if (!UserDir.Exists)
				UserDir.MakeDirectory (null);
			if (!UserScriptsDir.Exists)
				UserScriptsDir.MakeDirectory (null);
			if (!UserMetaDir.Exists)
				UserMetaDir.MakeDirectory (null);
			
			Log<HelperService>.Info ("Trying to install: {0}", file.Path);
			
			try {
				TarArchive ar = TarArchive.CreateInputTarArchive (new IO.FileStream (file.Path, IO.FileMode.Open));
				ar.ExtractContents (UserDir.Path);
			} catch (Exception e) {
				Log<HelperService>.Error ("Error trying to unpack '{0}': {1}", file.Path, e.Message);
				Log<HelperService>.Debug (e.StackTrace);
				return false;
			}
			
			try {
				UpdateHelpers ();
				return true;
			} catch (Exception e) {
				Log<HelperService>.Error ("Error trying to install helper '{0}': {1}", file.Path, e.Message);
				Log<HelperService>.Debug (e.StackTrace);
			}
			
			return false;
		}
		
		public bool UninstallHelper (Helper helper)
		{
			Log<HelperService>.Info ("Trying to unininstall: {0}", helper.File.Path);
			
			try {
				helper.File.Delete ();
				if (helper.Data != null) {
					if (helper.Data.DataFile.Exists)
						helper.Data.DataFile.Delete ();
					if (helper.Data.IconFile != null && helper.Data.IconFile.Exists)
						helper.Data.IconFile.Delete ();
				}
				UpdateHelpers ();
				return true;
			} catch (Exception e) {
				Log<HelperService>.Error ("Error trying to uninstall helper '{0}': {1}", helper.File.Path, e.Message);
				Log<HelperService>.Debug (e.StackTrace);
			}
			
			return false;
		}
		
		public void Dispose ()
		{
			foreach (FileMonitor mon in monitors) {
				mon.Cancel ();
				mon.Changed -= HandleMonitorChanged;
				mon.Dispose ();
			}
			
			foreach (Helper h in Helpers) {
				h.HelperStatusChanged -= OnHelperStatusChanged;
				h.Dispose ();
			}
		}
	}
}
