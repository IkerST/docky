//  
//  Copyright (C) 2011 Florian Dorn, Rico Tzschichholz, Robert Dyer
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Cairo;
using Mono.Unix;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;
using Docky.Widgets;

namespace NetworkMonitorDocklet 
{
	public class NetworkMonitorDockItem : AbstractDockItem
	{
		uint timer;
		NetworkMonitor monitor;
		DeviceInfo device;
		
		public override string UniqueID () { return "NetworkMonitor"; }
		
		public NetworkMonitorDockItem ()
		{
			monitor = new NetworkMonitor ();
			UpdateUtilization ();
			timer = GLib.Timeout.Add (3000, UpdateUtilization);
			ScalableRendering = false;
		}
		
		bool UpdateUtilization ()
		{
			monitor.UpdateDevices ();
			device = monitor.GetDevice (OutputDevice.AUTO);
			
			if (device != null)
				HoverText = device.ToString ();
			else
				HoverText = Catalog.GetString ("No network connection available");
			
			SetMessage (string.Format ("0;;{0};;down", device.FormatUpDown (false)));
			SetMessage (string.Format ("1;;{0};;up", device.FormatUpDown (true)));
			QueueRedraw ();
			return true;
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			if (device == null)
				return;
			
			Context cr = surface.Context;
			
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				layout.FontDescription = new Gtk.Style ().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.Alignment = Pango.Alignment.Center;
				
				int fontSize = surface.Height / 5;
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (fontSize);
				layout.SetText (device.name);
				
				Pango.Rectangle inkRect, logicalRect;
				layout.GetPixelExtents (out inkRect, out logicalRect);
				
				cr.MoveTo ((surface.Width - logicalRect.Width) / 2, 1);
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.LineWidth = 2;
				cr.Color = new Cairo.Color (0, 0, 0, 0.5);
				cr.StrokePreserve ();
				cr.Color = new Cairo.Color (1, 1, 1, 0.8);
				cr.Fill ();
				
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();
			}
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
