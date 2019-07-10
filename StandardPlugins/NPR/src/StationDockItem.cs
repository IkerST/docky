//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

using Docky.Menus;
using Docky.Items;
using Docky.Services;
using Docky.Widgets;

using Mono.Unix;

namespace NPR
{
	public class StationDockItem : IconDockItem
	{
		public Station OwnedStation;
		static ConfigDialog config;
		
		private bool IsReady {
			get {
				return (State & ItemState.Wait) != ItemState.Wait && OwnedStation.ID > 0;
			}
		}

		public StationDockItem (Station station)
		{
			State |= ItemState.Wait;
			HoverText = Catalog.GetString ("Fetching Information...");
			
			OwnedStation = station;			
			
			if (OwnedStation.ForcePixbuf != null)
				ForcePixbuf = OwnedStation.ForcePixbuf.Copy ();
			else
				Icon = OwnedStation.Icon;
			
			OwnedStation.FinishedLoading += delegate {
				SetInfo ();
			};
			
			if (OwnedStation.IsLoaded)
				SetInfo ();
		}
		
		void SetInfo () 
		{
			if (OwnedStation.ForcePixbuf != null)
				ForcePixbuf = OwnedStation.ForcePixbuf.Copy ();
			else
				Icon = OwnedStation.Icon;
			string hover = (string.IsNullOrEmpty (OwnedStation.Description)) ? 
				OwnedStation.Name : string.Format ("{0} : {1}", OwnedStation.Name, OwnedStation.Description);
			if (OwnedStation.ID < 1)
				hover = Catalog.GetString ("Click to add NPR stations.");
			HoverText = hover;
			State ^= ItemState.Wait;
		}
		
		#region IconDockItem implementation
		
		public override string UniqueID ()
		{
			return "NPR#_" + OwnedStation.Name + OwnedStation.Description;
		}

		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{			
			if (button == 1) {
				if (IsReady)
					DockServices.System.Open (OwnedStation.StationUrls.First (u => u.UrlType == StationUrlType.OrgHomePage).Target);
				else
					ShowConfig ();
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
		}

		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();

			if (!IsReady)
				return list;
			
			List<StationUrl> urls = OwnedStation.StationUrls.ToList ();
			
			if (urls.Any (u => u.UrlType == StationUrlType.OrgHomePage)) {
				list[MenuListContainer.Header].Add (new MenuItem (Catalog.GetString ("Home Page"),
						Gtk.Stock.Home,
						delegate {
							Clicked (1, Gdk.ModifierType.None, 0, 0);
						}));
			}
			if (urls.Any (u => u.UrlType == StationUrlType.ProgramSchedule)) {
				list[MenuListContainer.Header].Add (new MenuItem (Catalog.GetString ("Program Schedule"),
						"gnome-calendar",
						delegate {
							DockServices.System.Open (urls.First (u => u.UrlType == StationUrlType.ProgramSchedule).Target);
						}));
			}
			if (urls.Any (u => u.UrlType == StationUrlType.PledgePage)) {
				list[MenuListContainer.Header].Add (new MenuItem (Catalog.GetString ("Donate"),
						"emblem-money",
						delegate {
							DockServices.System.Open (urls.First (u => u.UrlType == StationUrlType.PledgePage).Target);
						}));
			}
			
			list.SetContainerTitle (MenuListContainer.CustomOne, Catalog.GetString ("Live Streams"));
			
			urls.Where (u => u.UrlType >= StationUrlType.AudioMP3Stream).ToList ().ForEach (url => {
				string format = "", icon = "";
				string port = "";

				int start = url.Target.LastIndexOf (":") + 1;
				int end = url.Target.IndexOf ("/", start);
				port = url.Target.Substring (start, end-start);
				
				switch (url.UrlType) {
				case StationUrlType.AudioMP3Stream:
					format = "MP3";
					icon = "audio-x-mpeg:audio-x-generic";
					break;
				case StationUrlType.AudioRAMStream:
					format = "Real Audio";
					icon = "audio-x-generic";
					break;
				case StationUrlType.AudioWMAStream:
					format = "Windows Media";
					icon = "audio-x-ms-wma:audio-x-generic";
					break;
				default:
					icon = "audio-x-mpeg";
					break;
				}
				
				string formatStr = string.IsNullOrEmpty (format) ? "{0} " : "{0} ({1}) ";
				formatStr += string.IsNullOrEmpty (port) ? "" : " port {2}";
								
				list[MenuListContainer.CustomOne].Add (new MenuItem (string.Format (formatStr, url.Title, format, port),
					icon,
					delegate {
						OwnedStation.PlayStream (url.Target);
					}));
			});
						
			list[MenuListContainer.Footer].Add (new MenuItem (Catalog.GetString ("Settings"),
					Gtk.Stock.Preferences,
					delegate { ShowConfig (); }));
			return list;
		}
		#endregion
		
		void ShowConfig ()
		{
			if (config == null)
				config = new NPRConfigDialog ();
			config.Show ();	
		}
		
		public override void Dispose ()
		{
			OwnedStation.Dispose ();
			base.Dispose ();
		}
	}
}
