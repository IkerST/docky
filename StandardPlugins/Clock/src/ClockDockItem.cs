//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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
using Docky.Services.Prefs;
using Docky.Widgets;

namespace Clock
{
	public class ClockDockItem : AbstractDockItem, IConfig
	{
		uint timer;
		
		int minute;
		CalendarPainter painter;
		
		static IPreferences prefs = DockServices.Preferences.Get<ClockDockItem> ();
		
		bool show_military = prefs.Get<bool> ("ShowMilitary", CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern [0] == 'H');
		bool ShowMilitary {
			get { return show_military; }
			set {
				if (show_military == value)
					return;
				show_military = value;
				prefs.Set<bool> ("ShowMilitary", value);
			}
		}
		
		bool digital = prefs.Get<bool> ("ShowDigital", false);
		bool ShowDigital {
			get { return digital; }
			set {
				if (digital == value)
					return;
				digital = value;
				prefs.Set<bool> ("ShowDigital", value);
				CheckForThemes ();
				ScalableRendering = !digital;
			}
		}
		
		bool show_date = prefs.Get<bool> ("ShowDate", false);
		bool ShowDate {
			get { return show_date; }
			set {
				if (show_date == value)
					return;
				show_date = value;
				prefs.Set<bool> ("ShowDate", value);
			}
		}
		
		string current_theme = prefs.Get<string> ("ClockTheme", "default");
		public string CurrentTheme {
			get { return current_theme; }
			protected set {
				if (current_theme == value)
					return;
				current_theme = value;
				prefs.Set<string> ("ClockTheme", value);
				ShowMilitary = value.EndsWith ("-24");
				CheckForThemes ();
			}
		}
		
		public string ThemeFolder {
			get {
				return DockServices.Paths.SystemDataFolder.GetChild ("ClockTheme").Path;
			}
		}
		
		public string ThemePath {
			get {
				string path = System.IO.Path.Combine (ThemeFolder, CurrentTheme);
				if (Directory.Exists (path))
					return path;

				return "";
			}
		}
		
		void CheckForThemes ()
		{
			// if its digital, we dont care about the theme
			if (ShowDigital)
				return;
			
			// check if we have a 24hr theme available
			bool has24hourtheme = false;
			
			if (CurrentTheme.EndsWith ("-24") || Directory.Exists (ThemePath + "-24"))
				has24hourtheme = true;
			
			// check if we have a 12hr theme available
			bool has12hourtheme = false;
			
			if (!CurrentTheme.EndsWith ("-24") || Directory.Exists (ThemePath.Substring (0, ThemePath.Length - 3)))
				has12hourtheme = true;
			
			// make sure military and the theme match
			if (ShowMilitary) {
				if (!has24hourtheme)
					ShowMilitary = false;
				else if (!CurrentTheme.EndsWith ("-24"))
					CurrentTheme = CurrentTheme + "-24";
			} else {
				if (!has12hourtheme)
					ShowMilitary = true;
				else if (CurrentTheme.EndsWith ("-24"))
					CurrentTheme = CurrentTheme.Substring (0, CurrentTheme.Length - 3);
			}
		}
		
		public ClockDockItem ()
		{
			ScalableRendering = !ShowDigital;
			
			painter = new CalendarPainter ();
			CheckForThemes ();
			
			timer = GLib.Timeout.Add (1000, ClockUpdateTimer);
		}
		
		public override bool Square {
			get { return !IsSmall || !ShowDigital; }
		}
		
		public override string UniqueID ()
		{
			return "Clock";
		}
		
		bool ClockUpdateTimer ()
		{
			if (minute != DateTime.UtcNow.Minute) {
				QueueRedraw ();
				minute = DateTime.UtcNow.Minute;
			}
			return true;
		}
		
		void RenderFileOntoContext (Context cr, string file, int size)
		{
			if (!File.Exists (file))
				return;
			
			using (Gdk.Pixbuf pbuf = GdkPixbufExtension.FromFileAtSize (file, size, size)) {
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, 0, 0);
				cr.Paint ();
			}
		}
		
		protected override DockySurface CreateIconBuffer (DockySurface model, int size)
		{
			if (Square)
				return base.CreateIconBuffer (model, size);
			else
				return new DockySurface (2 * size, size, model);
		}

		protected override void PaintIconSurface (DockySurface surface)
		{
			if (ShowMilitary)
				HoverText = DateTime.Now.ToString ("ddd, MMM dd HH:mm");
			else
				HoverText = DateTime.Now.ToString ("ddd, MMM dd h:mm tt");
			
			if (!ShowDigital)
				MakeAnalogIcon (surface.Context, Math.Min (surface.Width, surface.Height));
			else if (Square)
				MakeSquareDigitalIcon (surface);
			else
				MakeRectangularDigitalIcon (surface);
		}
		
		void MakeSquareDigitalIcon (DockySurface surface)
		{
			Context cr = surface.Context;
			
			// useful sizes
			int timeSize = surface.Height / 4;
			int dateSize = surface.Height / 5;
			int ampmSize = surface.Height / 5;
			int spacing = timeSize / 2;
			int center = surface.Height / 2;
			
			// shared by all text
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				layout.FontDescription = new Gtk.Style().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.Width = Pango.Units.FromPixels (surface.Width);
				
				
				// draw the time, outlined
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (timeSize);
				
				if (ShowMilitary)
					layout.SetText (DateTime.Now.ToString ("HH:mm"));
				else
					layout.SetText (DateTime.Now.ToString ("h:mm"));
				
				Pango.Rectangle inkRect, logicalRect;
				layout.GetPixelExtents (out inkRect, out logicalRect);
				
				int timeYOffset = ShowMilitary ? timeSize : timeSize / 2;
				int timeXOffset = (surface.Width - inkRect.Width) / 2;
				if (ShowDate)
					cr.MoveTo (timeXOffset, timeYOffset);
				else
					cr.MoveTo (timeXOffset, timeYOffset + timeSize / 2);
				
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.LineWidth = 3;
				cr.Color = new Cairo.Color (0, 0, 0, 0.5);
				cr.StrokePreserve ();
				cr.Color = new Cairo.Color (1, 1, 1, 0.8);
				cr.Fill ();
				
				// draw the date, outlined
				if (ShowDate) {
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (dateSize);
					
					layout.SetText (DateTime.Now.ToString ("MMM dd"));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo ((surface.Width - inkRect.Width) / 2, surface.Height - spacing - dateSize);
					
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.LineWidth = 2.5;
					cr.Color = new Cairo.Color (0, 0, 0, 0.5);
					cr.StrokePreserve ();
					cr.Color = new Cairo.Color (1, 1, 1, 0.8);
					cr.Fill ();
				}
				
				if (!ShowMilitary) {
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (ampmSize);
					
					int yOffset = ShowDate ? center - spacing : surface.Height - spacing - ampmSize;
					
					// draw AM indicator
					layout.SetText ("am");
					cr.Color = new Cairo.Color (1, 1, 1, DateTime.Now.Hour < 12 ? 0.8 : 0.2);
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo ((center - inkRect.Width) / 2, yOffset);
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Fill ();
					
					// draw PM indicator
					layout.SetText ("pm");
					cr.Color = new Cairo.Color (1, 1, 1, DateTime.Now.Hour > 11 ? 0.8 : 0.2);
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (center + (center - inkRect.Width) / 2, yOffset);
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Fill ();
				}
				
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();
			}
		}
		
		void MakeRectangularDigitalIcon (DockySurface surface)
		{
			Context cr = surface.Context;
			
			// useful sizes
			int timeSize = surface.Height / 3;
			int dateSize = surface.Height / 4;
			int ampmSize = surface.Height / 4;
			int spacing = timeSize / 2;
			
			// shared by all text
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				layout.FontDescription = new Gtk.Style().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.Width = Pango.Units.FromPixels (surface.Width);
				
				
				// draw the time, outlined
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (timeSize);
				
				if (ShowMilitary)
					layout.SetText (DateTime.Now.ToString ("HH:mm"));
				else
					layout.SetText (DateTime.Now.ToString ("h:mm"));
				
				Pango.Rectangle inkRect, logicalRect;
				layout.GetPixelExtents (out inkRect, out logicalRect);
				
				int timeYOffset = timeSize / 2;
				if (!ShowDate)
					timeYOffset += timeSize / 2;
				cr.MoveTo ((surface.Width - inkRect.Width) / 2, timeYOffset);
				
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.LineWidth = 2;
				cr.Color = new Cairo.Color (0, 0, 0, 0.5);
				cr.StrokePreserve ();
				cr.Color = new Cairo.Color (1, 1, 1, 0.8);
				cr.Fill ();
				
				// draw the date, outlined
				if (ShowDate) {
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (dateSize);
					
					layout.SetText (DateTime.Now.ToString ("MMM dd"));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo ((surface.Width - inkRect.Width) / 2, surface.Height - spacing - dateSize);
					
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Color = new Cairo.Color (0, 0, 0, 0.5);
					cr.StrokePreserve ();
					cr.Color = new Cairo.Color (1, 1, 1, 0.8);
					cr.Fill ();
				}
				
				if (!ShowMilitary) {
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (ampmSize);
					
					if (DateTime.Now.Hour < 12)
						layout.SetText ("am");
					else
						layout.SetText ("pm");
					
					layout.GetPixelExtents (out inkRect, out logicalRect);
					int yOffset = timeSize;
					if (!ShowDate)
						yOffset += timeSize / 2;
					cr.MoveTo (surface.Width - logicalRect.Width, yOffset - inkRect.Height);
					
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Color = new Cairo.Color (1, 1, 1, 0.8);
					cr.Fill ();
				}
				
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();
			}
		}
		
		void MakeAnalogIcon (Context cr, int size)
		{
			int center = size / 2;
			int radius = center;
			
			RenderFileOntoContext (cr, System.IO.Path.Combine (ThemePath, "clock-drop-shadow.svg"), radius * 2);
			RenderFileOntoContext (cr, System.IO.Path.Combine (ThemePath, "clock-face-shadow.svg"), radius * 2);
			RenderFileOntoContext (cr, System.IO.Path.Combine (ThemePath, "clock-face.svg"), radius * 2);
			RenderFileOntoContext (cr, System.IO.Path.Combine (ThemePath, "clock-marks.svg"), radius * 2);
			
			cr.Translate (center, center);
			cr.Color = new Cairo.Color (.15, .15, .15);
			
			cr.LineWidth = Math.Max (1, size / 48);
			cr.LineCap = LineCap.Round;
			double minuteRotation = 2 * Math.PI * (DateTime.Now.Minute / 60.0) + Math.PI;
			cr.Rotate (minuteRotation);
			cr.MoveTo (0, radius - radius * .35);
			cr.LineTo (0, -radius * .15);
			cr.Stroke ();
			cr.Rotate (-minuteRotation);
			
			cr.Color = new Cairo.Color (0, 0, 0);
			double hourRotation = 2 * Math.PI * (DateTime.Now.Hour / (ShowMilitary ? 24.0 : 12.0)) + 
					Math.PI + (Math.PI / (ShowMilitary ? 12.0 : 6.0)) * DateTime.Now.Minute / 60.0;
			cr.Rotate (hourRotation);
			cr.MoveTo (0, radius - radius * .5);
			cr.LineTo (0, -radius * .15);
			cr.Stroke ();
			cr.Rotate (-hourRotation);
			
			cr.Translate (-center, -center);
			
			RenderFileOntoContext (cr, System.IO.Path.Combine (ThemePath, "clock-glass.svg"), radius * 2);
			RenderFileOntoContext (cr, System.IO.Path.Combine (ThemePath, "clock-frame.svg"), radius * 2);
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1)
				ShowPainter (painter);
			return ClickAnimation.None;
		}
		
		public void SetTheme (string theme)
		{
			if (string.IsNullOrEmpty (theme))
				return;
			
			DockServices.System.RunOnMainThread (delegate {
				CurrentTheme = theme;
				QueueRedraw ();
			});
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			list[MenuListContainer.Actions].Add (new IconMenuItem (Catalog.GetString ("Di_gital Clock"), ShowDigital ? "gtk-apply" : "gtk-remove", (o, a) =>
			{
				ShowDigital = !ShowDigital;
				QueueRedraw ();
			}));
			
			list[MenuListContainer.Actions].Add (new IconMenuItem (Catalog.GetString ("24-Hour _Clock"), ShowMilitary ? "gtk-apply" : "gtk-remove", (o, a) =>
			{
				ShowMilitary = !ShowMilitary;
				QueueRedraw ();
			}, !ShowDigital));
			
			list[MenuListContainer.Actions].Add (new IconMenuItem (Catalog.GetString ("Show _Date"), ShowDate ? "gtk-apply" : "gtk-remove", (o, a) =>
			{
				ShowDate = !ShowDate;
				QueueRedraw ();
			}, !ShowDigital));
			
			list[MenuListContainer.Actions].Add (new MenuItem (Catalog.GetString ("Select _Theme"), "preferences-desktop-theme", (o, a) => 
			{ 
				ShowSettings ();
			}, ShowDigital));
			return list;
		}
		
		public void ShowSettings ()
		{
			if (ClockThemeSelector.instance == null)
				ClockThemeSelector.instance = new ClockThemeSelector (this);
			ClockThemeSelector.instance.Show (); 
		}
		
		public override void Dispose ()
		{
			if (ClockThemeSelector.instance != null) {
				ClockThemeSelector.instance.Destroy ();
				ClockThemeSelector.instance = null;
			}
			painter.Dispose ();

			if (timer > 0) {
				GLib.Source.Remove (timer);
				timer = 0;
			}

			base.Dispose ();
		}
	}
}
