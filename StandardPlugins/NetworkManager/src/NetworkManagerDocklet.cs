//  
//  Copyright (C) 2009 Chris Szikszoy, Robert Dyer, Jason Smith
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
using System.Collections.Generic;

using Gdk;
using Cairo;
using Mono.Unix;

using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace NetworkManagerDocklet
{
	public class NetworkManagerDocklet : IconDockItem
	{
		uint timer;
		
		IEnumerable<WirelessAccessPoint> ActiveAccessPoints {
			get {
				return NM.DevManager.NetworkDevices.OfType<WirelessDevice> ()
					.Where (dev => dev.ActiveAccessPoint != null)
					.Select (dev => dev.ActiveAccessPoint);
			}
		}
		
		public override MenuButton MenuButton {
			get { return MenuButton.Left | MenuButton.Right; }
		}
		
		public NetworkManagerDocklet ()
		{
			NM = new NetworkManager ();
			NM.DeviceStateChanged += OnDeviceStateChanged;
			
			HoverText = Catalog.GetString ("Network Manager");
			Icon = SetDockletIcon ();
			
			timer = GLib.Timeout.Add (10 * 1000, delegate {
				ReDraw ();
				return true;
			});
		}
		
		public override string UniqueID ()
		{
			return GetType ().FullName;
		}
		
		void OnDeviceStateChanged (object sender, DeviceStateChangedArgs args)
		{
			//Console.WriteLine (args.NewState);
			ReDraw ();
		}
		
		public NetworkManager NM { get; private set; }
		
		void ReDraw ()
		{
			DockServices.System.RunOnMainThread (delegate {
				Icon = SetDockletIcon ();
				QueueRedraw ();
			});
		}
		
		string SetDockletIcon ()
		{
			try {
				// currently connecting (animated)
				NetworkDevice dev = NM.DevManager.NetworkDevices
					.Where (d => d.State == DeviceState.Configuring || d.State == DeviceState.IPConfiguring || d.State == DeviceState.Preparing)
					.FirstOrDefault ();
				
				if (dev != null) {
					HoverText = Catalog.GetString ("Connecting...");
					State |= ItemState.Wait;
					return "nm-no-connection";
				}
				
				State &= ~ItemState.Wait;
				
				// no connection
				if (!NM.ActiveConnections.Any ()) {
					HoverText = Catalog.GetString ("Disconnected");
					return "nm-no-connection";
				}
				
				// wireless connection
				if (NM.ActiveConnections.OfType<WirelessConnection> ().Any ()) {
					string ssid = NM.ActiveConnections.OfType<WirelessConnection> ().First ().SSID;
					byte strength = NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().First ().APBySSID (ssid).Strength;
					
					HoverText = string.Format (Catalog.GetString ("Connected: {0}"), ssid);
					
					return APIconFromStrength (strength);
				}
			} catch {
				// FIXME why do we default to this?
				return APIconFromStrength ((byte) 100);
			}
			
			// default to wired connection
			return "nm-device-wired";
		}
		
		string APIconFromStrength (byte strength)
		{
			string icon = "network-wireless-connected-{0};;gnome-netstatus-{1};;nm-signal-{0}";
			if (strength >= 75)
				icon = string.Format (icon, "75", "75-100");
			else if (strength >= 50)
				icon = string.Format (icon, "50", "50-74");
			else if (strength >= 25)
				icon = string.Format (icon, "25", "25-49");
			else
				icon = string.Format (icon, "00", "0-24");
			return icon;
		}

		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			
			List<MenuItem> wifi = list[MenuListContainer.CustomOne];
			List<MenuItem> active = list[MenuListContainer.Header];
			
			foreach (WirelessAccessPoint wap in ActiveAccessPoints) {
				active.Add (MakeConEntry (wap));
			}
			
			int count = 0;
			if (NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().Any ()) {
				foreach (WirelessDevice device in NM.DevManager.NetworkDevices.OfType<WirelessDevice> ()) {
					foreach (IEnumerable<WirelessAccessPoint> val in device.VisibleAccessPoints.Values.OrderByDescending (ap => ap.Max (wap => wap.Strength))) {
						if (count > 7)
							break;
						
						// Dont pack any of the active access points since they should already be packed
						if (val.Any (wap => ActiveAccessPoints.Contains (wap)))
							continue;
						
						wifi.Add (MakeConEntry (val.OrderByDescending (wap => wap.Strength).First ()));
						count++;
					}
				}
			}
			
			if (!wifi.Any () && !active.Any ()) {
				wifi.Add (new MenuItem (Catalog.GetString ("Disconnected"), "nm-no-connection", true));
			}
			
			return list;
		}
		
//		public IEnumerable<AbstractMenuArgs> GetMenuItems ()
//		{
//			List<AbstractMenuArgs> cons = new List<AbstractMenuArgs> ();
//
//			//show Wired networks (if carrier is true)
//			if (NM.DevManager.NetworkDevices.OfType<WiredDevice> ().Any (dev => (dev as WiredDevice).Carrier == true)) {
//				NM.ConManager.AllConnections.OfType<WiredConnection> ().ForEach<WiredConnection> ( con => {
//					cons.Add (MakeConEntry (con));
//				});
//			}
//
//			//show wireless connections if wireless is available
//			if (NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().Any ()) {
//				cons.Add (new SeparatorMenuButtonArgs ());
//				NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().ForEach<WirelessDevice> (dev => {
//					dev.VisibleAccessPoints.Values.ForEach <List<WirelessAccessPoint>> ( apList => {
//						cons.Add (MakeConEntry (apList.First ()));
//					});
//				});
//			}
//			
//			foreach (AbstractMenuArgs arg in cons)
//			{
//				yield return arg;
//			}
//			
//			//yield return new WidgetMenuArgs (box);
//			
//			//yield return new SimpleMenuButtonArgs (() => Console.WriteLine ("asdf"),"Click me!", "network-manager");
//		}
		
		MenuItem MakeConEntry (WirelessAccessPoint ap)
		{
			string apName = ap.SSID;
			string icon = APIconFromStrength (ap.Strength);
			bool bold = NM.DevManager.NetworkDevices.OfType<WirelessDevice> ().Any (dev => dev.ActiveAccessPoint == ap);
			
			bool secure = ap.Flags == APFlags.Privacy || ap.RsnFlags != AccessPointSecurity.None || ap.WpaFlags != AccessPointSecurity.None;
			
			Docky.Menus.MenuItem item = new Docky.Menus.MenuItem (apName, icon, (o, a) => NM.ConnectTo (ap));
			item.Bold = bold;
			
			if (secure)
				item.Emblem = "nm-secure-lock";
			
			return item;
		}
		
		public override void Dispose ()
		{
			if (timer > 0) {
				GLib.Source.Remove (timer);
				timer = 0;
			}

			base.Dispose ();
		}
	}
}
