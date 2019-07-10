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
using Diag = System.Diagnostics;

using Docky.Services.Prefs;

using GLib;

namespace Docky.Services.Helpers
{
	public class Helper
	{
		public File File { get; private set; }
		
		public HelperMetadata Data { get; private set; }
		
		public bool IsUser { get; private set; }
		
		public bool IsAppAvailable {
			get {
				if (Data != null && !string.IsNullOrEmpty (Data.AppUri))
					return DockServices.System.IsValidExecutable (Data.AppUri);
				return true;
			}
		}
		
		bool? is_running;
		public bool IsRunning {
			get {
				if (!is_running.HasValue)
					is_running = false;
				return is_running.Value;
			}
			set {
				if (is_running.HasValue && is_running.Value == value)
					return;
				is_running = value;
				OnHelperStatusChanged ();
			}
		}
		
		bool enabled;
		public bool Enabled {
			get {
				return enabled;
			}
			set {
				if (enabled == value)
					return;
				
				if (value)
					Start ();
				else
					Stop ();
				
				enabled = value;
				prefs.Set<bool> (prefs.SanitizeKey (File.Basename), enabled);
				OnHelperStatusChanged ();
			}
		}

		static IPreferences prefs = DockServices.Preferences.Get<HelperService> ();
		
		Diag.Process Proc { get; set; }
		
		readonly uint X_PERM = Convert.ToUInt32 ("1001001", 2);
		
		internal event EventHandler<HelperStatusChangedEventArgs> HelperStatusChanged;
		
		public Helper (File file)
		{
			File = file;
			IsUser = file.Path.StartsWith (HelperService.UserDir.Path);
			enabled = prefs.Get<bool> (prefs.SanitizeKey (File.Basename), false);
			
			GLib.File DataFile;
			if (IsUser)
				DataFile = HelperService.UserMetaDir;
			else if (file.Path.StartsWith (HelperService.SysDir.Path))
				DataFile = HelperService.SysMetaDir;
			else
				DataFile = HelperService.SysLocalMetaDir;
			
			DataFile = DataFile.GetChild (File.Basename + ".info");
			
			if (DataFile.Exists)
				Data = new HelperMetadata (DataFile);
			
			if (Enabled)
				Start ();
		}
		
		void OnHelperStatusChanged ()
		{
			if (HelperStatusChanged != null)
				HelperStatusChanged (this, new HelperStatusChangedEventArgs (File, Enabled, IsRunning));
		}
		
		void Start ()
		{	
			if (Proc != null)
				return;
			
			// if the execute bits aren't set, try to set
			if (!File.QueryInfo<bool> ("access::can-execute")) {
				Log<Helper>.Debug ("Execute permissions are not currently set for '{0}', attempting to set them.", File.Path);
				try {
					uint currentPerm = File.QueryInfo<uint> ("unix::mode");
					File.SetAttributeUint32 ("unix::mode", currentPerm | X_PERM, 0, null);
				} catch (Exception e) {
					// if we can't set execute, then log the error and disable this script
					Log<Helper>.Error ("Failed to set execute permissions for '{0}': {1}", File.Path, e.Message);
					Enabled = false;
					return;
				}
			}
			
			Log<Helper>.Info ("Starting {0}", File.Basename);
			
			Proc = new Diag.Process ();
			Proc.StartInfo.FileName = File.Path;
			Proc.StartInfo.UseShellExecute = false;
			Proc.StartInfo.RedirectStandardError = true;
			Proc.StartInfo.RedirectStandardOutput = true;
			Proc.EnableRaisingEvents = true;
			Proc.ErrorDataReceived += delegate(object sender, Diag.DataReceivedEventArgs e) {
				if (DockServices.Helpers.ShowOutput && !string.IsNullOrEmpty (e.Data))
					Log<Helper>.Error ("{0} :: {1}", File.Basename, e.Data);
			};
			Proc.OutputDataReceived += delegate(object sender, Diag.DataReceivedEventArgs e) {
				if (DockServices.Helpers.ShowOutput && !string.IsNullOrEmpty (e.Data))
					Log<Helper>.Info ("{0} :: {1}", File.Basename, e.Data);
			};
			Proc.Exited += HandleExited;
			
			Proc.Start ();
			Proc.BeginErrorReadLine ();
			Proc.BeginOutputReadLine ();
			IsRunning = true;
		}
		
		void HandleExited (object o, EventArgs args)
		{
			Log<Helper>.Info ("{0} has exited (Code {1}).", File.Basename, Proc.ExitCode);
			Proc.Exited -= HandleExited;
			Proc.Dispose ();
			Proc = null;
			IsRunning = false;
		}
		
		void Stop ()
		{
			if (Proc == null)
				return;
			
			Log<Helper>.Info ("Stopping {0}", File.Basename);
			
			DockServices.System.RunOnThread (delegate {
				// we check again because there is a bit of a race condition
				if (Proc == null)
					return;
				
				if (!Proc.HasExited) {
					Proc.CancelErrorRead ();
					Proc.CancelOutputRead ();
					
					Proc.Exited -= HandleExited;
					Proc.CloseMainWindow ();
					Proc.WaitForExit (500);
					if (!Proc.HasExited) {
						Proc.Kill ();
						Proc.WaitForExit (200);
					}
					Log<Helper>.Info ("{0} has exited (Code {1}).", File.Basename, Proc.ExitCode);
				}
				
				Proc.Dispose ();
				Proc = null;
				
				IsRunning = false;
			});
		}
		
		public void Dispose ()
		{
			Stop ();
			if (Data != null)
				Data.Dispose ();
		}
	}
}
