//  
//  Copyright (C) 2009 Jason Smith, Chris Szikszoy, Robert Dyer
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;

using GLib;

using Docky.Services.Prefs;
using Docky.Services.Applications;

using DBus;
using org.freedesktop.DBus;

namespace Docky.Services
{
	public class SystemService
	{
		public System.Threading.Thread MainThread { get; set; }
		
		public void Initialize ()
		{
			InitializeBattery ();
			InitializeNetwork ();
		}
		
		#region Locale

		public string Locale {
			get {
				string loc;
				foreach (string env in new[] { "LC_ALL", "LC_MESSAGES", "LANG", "LANGUAGE" }) {
					loc = Environment.GetEnvironmentVariable (env);
					if (!string.IsNullOrEmpty (loc) && loc.Length >= 2) {
						return loc;
					}
				}
				return "";
			}
		}
		
		#endregion
		
		#region Network
		
		IPreferences NetworkSettings = DockServices.Preferences.Get ("/system/http_proxy");
		
		const string PROXY_USE_PROXY = "use_http_proxy";
		const string PROXY_USE_AUTH = "use_authentication";
		const string PROXY_HOST = "host";
		const string PROXY_PORT = "port";
		const string PROXY_USER = "authentication_user";
		const string PROXY_PASSWORD = "authentication_password";
		const string PROXY_BYPASS_LIST = "ignore_hosts";
		
		IBus NetworkManagerBus;
		
		void InitializeNetwork ()
		{
			NetworkManagerBus = Bus.System.GetObject<IBus> ("org.freedesktop.DBus", new ObjectPath ("/org/freedesktop/DBus"));
			NetworkManagerBus.NameOwnerChanged += delegate(string name, string old_owner, string new_owner) {
				if (name != NetworkManagerName)
					return;
				Log<SystemService>.Debug ("DBus services changed, reconnecting to Network Manager");
				network = null;
			};
			InitializeNetworkManager ();
			
			// watch for changes on any of the proxy keys. If they change, reload the proxy
			NetworkSettings.Changed += delegate (object sender, PreferencesChangedEventArgs e) {
				switch (e.Key) {
				case PROXY_HOST:
				case PROXY_USER:
				case PROXY_PASSWORD:
				case PROXY_PORT:
				case PROXY_USE_AUTH:
				case PROXY_BYPASS_LIST:
				case PROXY_USE_PROXY:
					Proxy = GetWebProxy ();
					break;
				}
				Proxy = GetWebProxy ();
			};
			
			Proxy = GetWebProxy ();
		}
		
		uint nmTimer = 0;
		
		void InitializeNetworkManager ()
		{
			NetworkConnected = true;
			if (nmTimer > 0) {
				GLib.Source.Remove (nmTimer);
				nmTimer = 0;
			}
			
			if (Bus.System.NameHasOwner (NetworkManagerName)) {
				try {
					network = Bus.System.GetObject<INetworkManager> (NetworkManagerName, new ObjectPath (NetworkManagerPath));
					var state = State;
					NetworkConnected = (state == NetworkState.ConnectedGlobal || state == NetworkState.ConnectedLocal
					                    || state == NetworkState.ConnectedSite);
					network.StateChanged += OnConnectionStatusChanged;
					nmTimer = GLib.Timeout.Add (1 * 60 * 1000, () => {
						state = State;
						NetworkConnected = (state == NetworkState.ConnectedGlobal || state == NetworkState.ConnectedLocal
						                    || state == NetworkState.ConnectedSite);
						return true;
					});
				} catch (Exception e) {
					// if something bad happened, log the error and assume we are connected
					Log<SystemService>.Error ("Could not initialize Network Manager dbus: '{0}'", e.Message);
					Log<SystemService>.Info (e.StackTrace);
				}
			} else {
				Log<SystemService>.Error ("Network Manager is not available.");
			}
		}
		
		public event EventHandler<ConnectionStatusChangeEventArgs> ConnectionStatusChanged;
		
		const string NetworkManagerName = "org.freedesktop.NetworkManager";
		const string NetworkManagerPath = "/org/freedesktop/NetworkManager";
		INetworkManager network;
		
		[Interface(NetworkManagerName)]
		interface INetworkManager : org.freedesktop.DBus.Properties
		{
			event StateChangedHandler StateChanged;
		}
		
		delegate void StateChangedHandler (uint state);
		
		public bool NetworkConnected { get; private set; }
		
		void OnConnectionStatusChanged (uint state)
		{
			NetworkState newState = (NetworkState) Enum.ToObject (typeof (NetworkState), state);
			NetworkConnected = (newState == NetworkState.ConnectedGlobal || newState == NetworkState.ConnectedLocal
			                    || newState == NetworkState.ConnectedSite);
			
			if (ConnectionStatusChanged != null) {
				Delegate [] handlers = ConnectionStatusChanged.GetInvocationList ();
				foreach (Delegate d in handlers)
					try {
						d.DynamicInvoke (new object [] {this, new ConnectionStatusChangeEventArgs (newState)});
					} catch (Exception e) {
						Log.Error ("Error in ConnectionStatusChanged handler, {0}", e.Message);
						Log.Debug (e.StackTrace);
					}
			}
		}
		
		NetworkState State {
			get {
				try {
					return (NetworkState) Enum.ToObject (typeof(NetworkState), network.Get (NetworkManagerName, "State"));
				} catch (Exception) {
					return NetworkState.Unknown;
				}
			}
		}
		
		public string UserAgent {
			get {
				return @"Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.9.2) Gecko/20100308 Ubuntu/10.04 (lucid) Firefox/3.6";
			}
		}

		public bool UseProxy {
			get {
				return NetworkSettings.Get<bool> (PROXY_USE_PROXY, false);
			}
		}

		public WebProxy Proxy { get; private set; }
		
		WebProxy GetWebProxy ()
		{
			WebProxy proxy;
			
			if (!UseProxy)
				return null;
			
			try {
				string proxyUri = string.Format ("http://{0}:{1}", NetworkSettings.Get<string> (PROXY_HOST, ""),
					NetworkSettings.Get<int> (PROXY_PORT, 0).ToString ());
				
				proxy = new WebProxy (proxyUri);
				string[] bypassList = NetworkSettings.Get<string[]> (PROXY_BYPASS_LIST, new[] { "" });
				if (bypassList != null) {
					foreach (string host in bypassList) {
						if (host.Contains ("*.local")) {
							proxy.BypassProxyOnLocal = true;
							continue;
						}
						proxy.BypassArrayList.Add (string.Format ("http://{0}", host));
					}
				}
				if (NetworkSettings.Get<bool> (PROXY_USE_AUTH, false))
					proxy.Credentials = new NetworkCredential (NetworkSettings.Get<string> (PROXY_USER, ""),
						NetworkSettings.Get<string> (PROXY_PASSWORD, ""));
			} catch (Exception e) {
				Log.Error ("Error creating web proxy, {0}", e.Message);
				Log.Debug (e.StackTrace);
				return null;
			}
			
			return proxy;
		}
		
		#endregion
		
		#region Battery
		
		public event EventHandler BatteryStateChanged;

		public bool OnBattery {
			get {
				return on_battery;
			}
		}
		
		void OnBatteryStateChanged ()
		{
			if (BatteryStateChanged != null) {
				Delegate [] handlers = BatteryStateChanged.GetInvocationList ();
				foreach (Delegate d in handlers)
					try {
						d.DynamicInvoke (new object [] {this, EventArgs.Empty});
					} catch (Exception e) {
						Log.Error ("Error in BatteryStateChanged handler, {0}", e.Message);
						Log.Debug (e.StackTrace);
					}
			}
		}
		
		const string UPowerName = "org.freedesktop.UPower";
		const string UPowerPath = "/org/freedesktop/UPower";
		
		delegate void BoolDelegate (bool val);
		
		[Interface(UPowerName)]
		interface IUPower : org.freedesktop.DBus.Properties
		{
			//bool OnBattery { get; }

			event Action Changed;
		}
		
		bool GetBoolean (org.freedesktop.DBus.Properties dbusobj, string path, string propname) 
		{
			try {
				return Boolean.Parse (dbusobj.Get (path, propname).ToString ());
			} catch (Exception e) {
				Log.Error ("{0}", e.Message);
				Log.Debug (e.StackTrace);
				return false;
			}
		}

		bool on_battery;
		
		IUPower upower;
		
		void InitializeBattery ()
		{
			// Set a sane default value for on_battery.  Thus, if we don't find a working power manager
			// we assume we're not on battery.
			on_battery = false;
			try {
				if (Bus.System.NameHasOwner (UPowerName)) {
					upower = Bus.System.GetObject<IUPower> (UPowerName, new ObjectPath (UPowerPath));
					upower.Changed += HandleUPowerChanged;
					HandleUPowerChanged ();
					Log<SystemService>.Debug ("Using org.freedesktop.UPower for battery information");
				}
			} catch (Exception e) {
				Log<SystemService>.Error ("Could not initialize power manager dbus: '{0}'", e.Message);
				Log<SystemService>.Info (e.StackTrace);
			}
		}

		void HandleUPowerChanged ()
		{
			bool newState = GetBoolean (upower, UPowerName, "OnBattery");
			
			if (on_battery != newState) {
				on_battery = newState;
				OnBatteryStateChanged ();
			}
		}

		#endregion

		public void Open (string uri)
		{
			Open (GLib.FileFactory.NewForUri (uri));
		}
		
		public void Open (IEnumerable<string> uris)
		{
			Launch (null, uris.Select (uri => GLib.FileFactory.NewForUri (uri)));
		}
		
		public void Open (GLib.File file)
		{
			Open (new [] { file });
		}
		
		public void Open (IEnumerable<GLib.File> files)
		{
			// null forces the default handler
			Launch (null, files);
		}
		
		public void Launch (GLib.File app, IEnumerable<GLib.File> files)
		{
			if (app != null && !app.Exists) {
				Log<SystemService>.Warn ("Application {0} doesnt exist", app.Path);
				return;
			}
			
			List<GLib.File> noMountNeeded = new List<GLib.File> ();

			// before we try to use the files, make sure they are mounted
			foreach (GLib.File f in files) {
				// if the path isn't empty, 
				// check if it's a local file or on VolumeMonitor's mount list.
				// if it is, skip it.
				if (!string.IsNullOrEmpty (f.Path) 
				    && (f.IsNative || VolumeMonitor.Default.Mounts.Any (m => f.Path.Contains (m.Root.Path)))) {
					noMountNeeded.Add (f);
					continue;
				}
				// if the file has no path, there are 2 possibilities
				// either it's a "fake" file, like computer:// or trash://
				// or it's a mountable file that isn't mounted
				// this launches the "fake" files":
				try {
					GLib.AppInfoAdapter.LaunchDefaultForUri (f.StringUri (), null);
				// FIXME: until we use a more recent Gtk# (one that exposes the GException code
				// we'll just have to silently fail and assume it's an unmounted but mountable file
				} catch { 
					// otherwise:
					// try to mount, if successful launch, otherwise (it's possibly already mounted) try to launch anyways
					f.MountWithActionAndFallback (() => LaunchWithFiles (app, new [] {f}), () => LaunchWithFiles (app, new [] {f}));				
				}
			}

			if (noMountNeeded.Any () || !files.Any ())
				LaunchWithFiles (app, noMountNeeded);
		}
		
		void LaunchWithFiles (GLib.File app, IEnumerable<GLib.File> files)
		{
			AppInfo appinfo = null;
			if (app != null) {
				appinfo = GLib.DesktopAppInfo.NewFromFilename (app.Path);
			} else {
				if (!files.Any ())
					return;

				// if we weren't given an app info, query the file for the default handler
				try {
					appinfo = files.First ().QueryDefaultHandler (null);
				} catch {
					// file probably doesnt exist
					return;
				}
			}
			
			try {
				GLib.List launchList;
				
				if (!files.Any ()) {
					appinfo.Launch (null, null);

				// check if the app supports files or Uris
				} else if (appinfo.SupportsFiles) {
					launchList = new GLib.List (new GLib.File[] {}, typeof (GLib.File), true, false);
					foreach (GLib.File f in files)
						launchList.Append (f);

					appinfo.Launch (launchList, null);
					launchList.Dispose ();
					
				} else if (appinfo.SupportsUris) {
					launchList = new GLib.List (new string[] {}, typeof (string), true, true);
					foreach (GLib.File f in files) {
						// try to use GLib.File.Uri first, if that throws an exception,
						// catch and use P/Invoke to libdocky.  If that's still null, warn & skip the file.
						try {
							launchList.Append (f.Uri.ToString ());
						} catch (UriFormatException) { 
							string uri = f.StringUri ();
							if (string.IsNullOrEmpty (uri)) {
								Log<SystemService>.Warn ("Failed to retrieve URI for {0}.  It will be skipped.", f.Path);
								continue;
							}
							launchList.Append (uri);
						}
					}
					appinfo.LaunchUris (launchList, null);
					launchList.Dispose ();
					
				} else {
					Log<SystemService>.Error ("Error opening files. The application doesn't support files/URIs or wasn't found.");	
				}
				
			} catch (GException e) {
				Log.Notify (string.Format ("Error running: {0}", appinfo.Name), Gtk.Stock.DialogWarning, e.Message);
				Log<SystemService>.Error (e.Message);
				Log<SystemService>.Info (e.StackTrace);
			}

			(appinfo as IDisposable).Dispose ();
		}
		
		public void Execute (string executable)
		{
			try {
				System.Diagnostics.Process proc;
				if (System.IO.File.Exists (executable)) {
					proc = new System.Diagnostics.Process ();
					proc.StartInfo.FileName = executable;
					proc.StartInfo.UseShellExecute = false;
					proc.Start ();
					proc.Dispose ();
				} else {
					int pos = -1;
					executable = executable.Trim ();
					if ((pos = executable.IndexOf (' ')) >= 0) {
						string command = executable.Substring (0, pos);
						string arguments = executable.Substring (pos + 1);
						proc = System.Diagnostics.Process.Start (command, arguments);
					} else {
						proc = System.Diagnostics.Process.Start (executable);
					}
					Log<SystemService>.Debug ("Calling: '{0}'", executable);
					proc.Dispose ();
				}
			} catch {
				Log<SystemService>.Error ("Error executing '{0}'", executable);
			}
		}
		
		public bool IsValidExecutable (string Executable)
		{
			foreach (string path in Environment.GetEnvironmentVariable ("PATH").Split (':'))
				if (GLib.FileFactory.NewForPath (path + "/" + Executable).Exists)
					return true;
			return false;
		}
		
		public System.Threading.Thread RunOnThread (Action action, TimeSpan delay)
		{
			return RunOnThread (() => {
				System.Threading.Thread.Sleep (delay);
				action ();
			});
		}
		
		public System.Threading.Thread RunOnThread (Action action, int delay)
		{
			return RunOnThread (action, new TimeSpan (0, 0, 0, 0, delay));
		}
		
		public System.Threading.Thread RunOnThread (Action action)
		{
			System.Threading.Thread newThread = new System.Threading.Thread (() =>
			{
				try {
					action ();
				} catch (ThreadAbortException) {
				} catch (Exception e) {
					Log<SystemService>.Error ("Error in RunOnThread: {0}", e.Message);
					Log<SystemService>.Debug (e.StackTrace);
				}
			});
			
			newThread.IsBackground = true;
			newThread.Start ();
			return newThread;
		}
		
		public void RunOnMainThread (Action action)
		{
			if (System.Threading.Thread.CurrentThread.Equals (MainThread))
				action ();
			else
				Gtk.Application.Invoke ((sender, arg) => {
					try {
						action ();
					} catch (Exception e) {
						Log<SystemService>.Error ("Error in RunOnMainThread: {0}", e.Message);
						Log<SystemService>.Debug (e.StackTrace);
					}
				});
		}
		
		public void RunOnMainThread (Action action, int delay)
		{
			RunOnMainThread (action, new TimeSpan (0, 0, 0, 0, delay));
		}
		
		public void RunOnMainThread (Action action, TimeSpan delay)
		{
			RunOnThread (() => RunOnMainThread (action), delay);
		}
		
		public void SetProcessName (string name)
		{
			NativeInterop.prctl (15 /* PR_SET_NAME */, name);
		}
		
		public bool HasNvidia {
			get {
				string logFile = "/var/log/Xorg.0.log";
				if (!System.IO.File.Exists (logFile))
					return false;
				
				try {
					using (StreamReader reader = new StreamReader (logFile)) {
						string line;
						while ((line = reader.ReadLine ()) != null) {
							if (line.Contains ("Module nvidia: vendor=\"NVIDIA Corporation\""))
								return true;
						}
					}
				} catch (Exception e) {
					Log<SystemService>.Warn ("Error encountered while trying to detect if an Nvidia graphics card is present: {0}", e.Message);
					return false;
				}
				
				return false;
			}
		}
	}
}
