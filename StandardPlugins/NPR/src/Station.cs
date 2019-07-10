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
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;

using GLib;

using Docky.Items;
using Docky.Menus;
using Docky.Services;
using Docky.Widgets;

using Mono.Unix;

namespace NPR
{

	public class Station : AbstractTileObject
	{
		public int ID { get; private set; }
		public IEnumerable<StationUrl> StationUrls { get; private set; }
		public bool IsLoaded { get; private set; }
		
		public override bool Enabled {
			get {
				return NPR.MyStations.Contains (ID)  || ID == -1;
			}
		}
		
		public event EventHandler FinishedLoading;
		
		public void OnFinishedLoading ()
		{
			if (FinishedLoading != null)
				FinishedLoading (this, EventArgs.Empty);
		}
		
		private string DefaultLogo { get; set; }
		private string LogoFile { get; set; }
		private bool IsSetUp { get; set; }
		
		public Station (int id)
		{
			IsLoaded = false;
			ForcePixbuf = DockServices.Drawing.LoadIcon ("nprlogo.gif@" + GetType ().Assembly.FullName, 128, -1);
			ID = id;
			
			if (ID > 0) {
				DockServices.System.RunOnThread (() => {
					LoadDataFromXElement (NPR.StationXElement (ID));
				});
				// this is how we create our "null" station entry
			} else {
				Name = Catalog.GetString ("No stations found.");
				Description = Catalog.GetString ("Please try your search again.");
				ShowActionButton = false;
				IsLoaded = true;
			}

			DockServices.System.ConnectionStatusChanged += HandleConnectionStatusChanged;
		}
		
		void HandleConnectionStatusChanged (object o, EventArgs args)
		{
			DockServices.System.RunOnThread (() => {
				LoadDataFromXElement (NPR.StationXElement (ID));
			});
		}

		void LoadDataFromXElement (XElement stationElement)
		{
			if (!DockServices.System.NetworkConnected)
				return;
			
			IsSetUp = false;
			
			Name = stationElement.Element ("name").Value;
			ID = int.Parse (stationElement.Attribute ("id").Value);

			SubDescriptionTitle = "City";
			SubDescriptionText = stationElement.Element ("marketCity").Value;
			
			Description = stationElement.Element ("tagline").Value;
			
			StationUrls = stationElement.Elements ("url").Select (u => new StationUrl (u));
			
			string logo = stationElement.Elements ("image").First (el => el.Attribute ("type").Value == "logo").Value;
			GLib.File l = FileFactory.NewForUri (logo);
			LogoFile = System.IO.Path.Combine (System.IO.Path.GetTempPath (), l.Basename);
			if (!System.IO.File.Exists (LogoFile)) {
				WebClient cl = new WebClient ();
				cl.Headers.Add ("user-agent", DockServices.System.UserAgent);
				if (DockServices.System.UseProxy)
					cl.Proxy = DockServices.System.Proxy;
				
				cl.DownloadFile (logo, LogoFile);
			}
			
			Finish ();
		}
		
		void Finish ()
		{
			// try loading the logo, if this fails, then we use the backup.
			try {
				Gdk.Pixbuf pbuf = new Gdk.Pixbuf (LogoFile);
				pbuf.Dispose ();
				// if we get to this point, the logofile will load just fine
				Icon = LogoFile;
			} catch {
				// delete the bad logofile, if it exists
				if (System.IO.File.Exists (LogoFile))
					System.IO.File.Delete (LogoFile);
				ForcePixbuf = DockServices.Drawing.LoadIcon ("nprlogo.gif@" + GetType ().Assembly.FullName, 128, -1);
			}
			
			IsLoaded = true;
			OnFinishedLoading ();
		}
		
		public override void OnActiveChanged ()
		{
			List<int> stations = NPR.MyStations.ToList ();
			
			if (stations.Contains (ID))
				stations.Remove (ID);
			else
				stations.Add (ID);
						
			NPR.MyStations = stations.ToArray ();
			
			base.OnActiveChanged ();
		}
		
		public void PlayStream (string url)
		{
			DockServices.System.RunOnThread (() => {
				try {
					WebClient cl = new WebClient ();
					cl.Headers.Add ("user-agent", DockServices.System.UserAgent);
					if (DockServices.System.UseProxy)
						cl.Proxy = DockServices.System.Proxy;
					string tempPath = System.IO.Path.GetTempPath ();
					string filename = url.Split (new [] {'/'}).Last ();
					filename = System.IO.Path.Combine (tempPath, filename);
					
					GLib.File file = FileFactory.NewForPath (filename);
					if (file.Exists)
						file.Delete ();
					
					cl.DownloadFile (url, file.Path);
					DockServices.System.Open (file);
				} catch (Exception e) {
					Docky.Services.Log<Station>.Error ("Failed to play streaming url ({0}) : {1}", url, e.Message);
					Docky.Services.Log<Station>.Debug (e.StackTrace);
					// also notify the user that we couldn't play this stream for some reason.
					Docky.Services.Log.Notify (Name, Icon, "The streaming link failed to play.  " +
					                           "This is most likely a problem with the NPR station.");
				}
			});
		}

		public override void Dispose ()
		{
			DockServices.System.ConnectionStatusChanged -= HandleConnectionStatusChanged;
			FinishedLoading = null;
			base.Dispose ();
		}
	}
}
