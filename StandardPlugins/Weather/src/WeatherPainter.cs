//  
//  Copyright (C) 2009 GNOME Do
//  Copyright (C) 2010-2011 Robert Dyer
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

using Cairo;
using Gtk;
using Mono.Unix;

using Docky.CairoHelper;
using Docky.Painters;
using Docky.Services;

namespace WeatherDocklet
{
	/// <summary>
	/// A painter that displays information about weather forecasts.
	/// </summary>
	public class WeatherPainter : PagingDockPainter
	{
		/// <value>
		/// The color to draw most text in.
		/// </value>
		protected static readonly Cairo.Color colorTitle = new Cairo.Color (0.627, 0.627, 0.627, 1);
		
		/// <value>
		/// The color to draw high temperatures in.
		/// </value>
		protected static readonly Cairo.Color colorHigh = new Cairo.Color (0.945, 0.431, 0.431, 1);
		
		/// <value>
		/// The color to draw low temperatures in.
		/// </value>
		protected static readonly Cairo.Color colorLow = new Cairo.Color (0.427, 0.714, 0.945, 1);
		
		/// <summary>
		/// Creates a new weather painter object.
		/// </summary>
		public WeatherPainter () : base (3)
		{
			Page = 1;
		}
		
		public override bool SupportsVertical {
			get { return true; }
		}
		
		public override int MinimumHeight {
			get {
				if (IsVertical)
					return BUTTON_SIZE + (int) (1.5 * WeatherController.Weather.ForecastDays * Math.Max (42, Allocation.Width));
				return 42;
			}
		}
		
		public override int MinimumWidth {
			get {
				if (IsVertical)
					return 2 * 42;
				return 2 * BUTTON_SIZE + 2 * WeatherController.Weather.ForecastDays * Math.Max (MinimumHeight, Allocation.Height);
			}
		}
		
		#region IDockPainter implementation 
		
		protected override void DrawPageOnSurface (int page, DockySurface surface)
		{
			switch (page) {
				default:
				case 0:
					if (IsVertical)
						DrawVertCurrentCondition (surface.Context);
					else
						DrawCurrentCondition (surface.Context);
					break;
				
				case 1:
					if (IsVertical)
						DrawVertForecast (surface.Context);
					else
						DrawForecast (surface.Context);
					break;
				
				case 2:
					DrawTempGraph (surface.Context);
					break;
			}
		}
		
		#endregion
		
		/// <summary>
		/// Paints an overview of the forecast including high/low temps and a condition icon.
		/// </summary>
		/// <param name="cr">
		/// A <see cref="Cairo.Context"/> to do the painting.
		/// </param>
		void DrawForecast (Cairo.Context cr)
		{
			int xOffset = BUTTON_SIZE;
			int cellWidth = (Allocation.Width - 2 * BUTTON_SIZE) / (WeatherController.Weather.ForecastDays * 2);
			
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				Pango.Rectangle inkRect, logicalRect;
				
				layout.FontDescription = new Gtk.Style().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.Width = Pango.Units.FromPixels (cellWidth);
				
				for (int day = 0; day < WeatherController.Weather.ForecastDays; day++) {
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (cellWidth / 5));
					
					cr.Color = colorTitle;
					layout.SetText (string.Format ("{0}", WeatherForecast.DayShortName (WeatherController.Weather.Forecasts [day].dow)));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (xOffset + (cellWidth - inkRect.Width) / 2, 0);
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Fill ();
					
					cr.Color = colorHigh;
					layout.SetText (string.Format ("{0}{1}", WeatherController.Weather.Forecasts [day].high, AbstractWeatherSource.TempUnit));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (xOffset + (cellWidth - inkRect.Width) / 2, Allocation.Height / 2 - logicalRect.Height / 2);
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Fill ();
					
					cr.Color = colorLow;
					layout.SetText (string.Format ("{0}{1}", WeatherController.Weather.Forecasts [day].low, AbstractWeatherSource.TempUnit));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (xOffset + (cellWidth - inkRect.Width) / 2, Allocation.Height - logicalRect.Height);
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Fill ();
					
					using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (WeatherController.Weather.Forecasts [day].image, cellWidth - 5)) {
						Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, xOffset + cellWidth + 2, 5 + (Allocation.Height - cellWidth) / 2);
						cr.PaintWithAlpha (WeatherController.Weather.Forecasts [day].chanceOf ? .6 : 1);
					}
					
					if (WeatherController.Weather.Forecasts [day].chanceOf) {
						layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (cellWidth / 2));
						
						layout.SetText ("?");
						
						layout.GetPixelExtents (out inkRect, out logicalRect);
						cr.MoveTo (xOffset + cellWidth + (cellWidth - inkRect.Width) / 2, Allocation.Height / 2 - logicalRect.Height / 2);
						
						cr.LineWidth = 4;
						cr.Color = new Cairo.Color (0, 0, 0, 0.3);
						Pango.CairoHelper.LayoutPath (cr, layout);
						cr.StrokePreserve ();
						
						cr.Color = new Cairo.Color (1, 1, 1, .6);
						cr.Fill ();
					}
					
					xOffset += 2 * cellWidth;
				}
				
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();
			}
		}
		
		/// <summary>
		/// Paints an overview of the forecast including high/low temps and a condition icon.
		/// </summary>
		/// <param name="cr">
		/// A <see cref="Cairo.Context"/> to do the painting.
		/// </param>
		void DrawVertForecast (Cairo.Context cr)
		{
			int cellHeight = (int) ((Allocation.Height - BUTTON_SIZE) / WeatherController.Weather.ForecastDays / 1.5);
			double xOffset = 0;
			double yOffset = cellHeight / 4.0;
			
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				Pango.Rectangle inkRect, logicalRect;
				
				layout.FontDescription = new Gtk.Style().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.Width = Pango.Units.FromPixels (cellHeight);
				
				for (int day = 0; day < WeatherController.Weather.ForecastDays; day++) {
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (cellHeight / 5));
					
					cr.Color = colorTitle;
					layout.SetText (string.Format ("{0}", WeatherForecast.DayShortName (WeatherController.Weather.Forecasts [day].dow)));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (xOffset + (cellHeight - inkRect.Width) / 2, yOffset);
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Fill ();
					
					cr.Color = colorHigh;
					layout.SetText (string.Format ("{0}{1}", WeatherController.Weather.Forecasts [day].high, AbstractWeatherSource.TempUnit));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (xOffset + (cellHeight - inkRect.Width) / 2, yOffset + (cellHeight - logicalRect.Height) / 2);
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Fill ();
					
					cr.Color = colorLow;
					layout.SetText (string.Format ("{0}{1}", WeatherController.Weather.Forecasts [day].low, AbstractWeatherSource.TempUnit));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (xOffset + (cellHeight - inkRect.Width) / 2, yOffset + cellHeight - logicalRect.Height);
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Fill ();
					
					using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (WeatherController.Weather.Forecasts [day].image, cellHeight - 5)) {
						Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, xOffset + 5 + cellHeight, yOffset + 2);
						cr.PaintWithAlpha (WeatherController.Weather.Forecasts [day].chanceOf ? .6 : 1);
					}
					
					if (WeatherController.Weather.Forecasts [day].chanceOf) {
						layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (cellHeight / 2));
						
						layout.SetText ("?");
						
						layout.GetPixelExtents (out inkRect, out logicalRect);
						cr.MoveTo (xOffset + cellHeight + (cellHeight - inkRect.Width) / 2, yOffset + (cellHeight - logicalRect.Height) / 2);
						
						cr.LineWidth = 4;
						cr.Color = new Cairo.Color (0, 0, 0, 0.3);
						Pango.CairoHelper.LayoutPath (cr, layout);
						cr.StrokePreserve ();
						
						cr.Color = new Cairo.Color (1, 1, 1, .6);
						cr.Fill ();
					}
					
					yOffset += (int) (1.5 * cellHeight);
				}
				
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();
			}
		}
		
		/// <summary>
		/// Paints the forecast temperatures as a chart.
		/// </summary>
		/// <param name="cr">
		/// A <see cref="Cairo.Context"/> to do the painting.
		/// </param>
		void DrawTempGraph (Cairo.Context cr)
		{
			int max = -1000, min = 1000;
			
			for (int day = 0; day < WeatherController.Weather.ForecastDays; day++) {
				if (WeatherController.Weather.Forecasts [day].high > max)
					max = WeatherController.Weather.Forecasts [day].high;
				if (WeatherController.Weather.Forecasts [day].low > max)
					max = WeatherController.Weather.Forecasts [day].low;
				if (WeatherController.Weather.Forecasts [day].high < min)
					min = WeatherController.Weather.Forecasts [day].high;
				if (WeatherController.Weather.Forecasts [day].low < min)
					min = WeatherController.Weather.Forecasts [day].low;
		    }
			
			if (max <= min)
				return;
			
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				Pango.Rectangle inkRect, logicalRect;
				
				layout.FontDescription = new Gtk.Style().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (Allocation.Height / 5));
				
				// high/low temp
				layout.Width = Pango.Units.FromPixels (Allocation.Height);
				cr.Color = colorHigh;
				layout.SetText (string.Format ("{0}{1}", max, AbstractWeatherSource.TempUnit));
				layout.GetPixelExtents (out inkRect, out logicalRect);
				cr.MoveTo (Allocation.Width - Allocation.Height + (Allocation.Height - inkRect.Width) / 2 - BUTTON_SIZE, Allocation.Height / 6 - logicalRect.Height / 2);
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Fill ();
				
				cr.Color = colorLow;
				layout.SetText (string.Format ("{0}{1}", min, AbstractWeatherSource.TempUnit));
				layout.GetPixelExtents (out inkRect, out logicalRect);
				cr.MoveTo (Allocation.Width - Allocation.Height + (Allocation.Height - inkRect.Width) / 2 - BUTTON_SIZE, Allocation.Height * 6 / 9 - logicalRect.Height / 2);
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Fill ();
				
				// day names
				layout.Width = Pango.Units.FromPixels (2 * Allocation.Height);
				
				cr.Color = colorTitle;
				for (int day = 0; day < WeatherController.Weather.ForecastDays; day++) {
					layout.SetText (WeatherForecast.DayShortName (WeatherController.Weather.Forecasts [day].dow));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (BUTTON_SIZE + day * Allocation.Height * 2 + (Allocation.Height - inkRect.Width) / 2, Allocation.Height * 8 / 9 - logicalRect.Height / 2);
					Pango.CairoHelper.LayoutPath (cr, layout);
				}
				cr.Fill ();
				cr.Save ();
				
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();
			}
			
			// draw tick lines
			cr.Color = new Cairo.Color (0.627, 0.627, 0.627, .8);
			cr.LineWidth = 1;
			cr.LineCap = LineCap.Round;
			
			int lines = 5;
			for (int line = 0; line < lines - 1; line++) {
				cr.MoveTo (BUTTON_SIZE + Allocation.Height / 4, 4.5 + Allocation.Height * line / lines);
				cr.LineTo (BUTTON_SIZE + (2 * WeatherController.Weather.ForecastDays - 1) * Allocation.Height - Allocation.Height / 4, 4.5 + Allocation.Height * line / lines);
				cr.Stroke ();
			}
			for (int line = 0; ; line++) {
				double x = BUTTON_SIZE + Allocation.Height / 2 + line * 2 * Allocation.Height - 0.5;
				if (x >= BUTTON_SIZE + (2 * WeatherController.Weather.ForecastDays - 1) * Allocation.Height - Allocation.Height / 4)
					break;
				cr.MoveTo (x, 4.5);
				cr.LineTo (x, 4.5 + Allocation.Height * (lines - 2) / lines);
				cr.Stroke ();
			}
			
			cr.Restore ();
			cr.LineWidth = 3;
			double height = ((double) Allocation.Height * 2 / 3 - 5) / (max - min);
			
			// high temp graph
			cr.Color = colorHigh;
			cr.MoveTo (BUTTON_SIZE + Allocation.Height / 2, 5 + height * (max - WeatherController.Weather.Forecasts [0].high));
			for (int day = 1; day < WeatherController.Weather.ForecastDays; day++)
				cr.LineTo (BUTTON_SIZE + day * Allocation.Height * 2 + Allocation.Height / 2, 5 + height * (max - WeatherController.Weather.Forecasts [day].high));
			cr.Stroke ();
			
			// low temp graph
			cr.Color = colorLow;
			cr.MoveTo (BUTTON_SIZE + Allocation.Height / 2, 5 + height * (max - WeatherController.Weather.Forecasts [0].low));
			for (int day = 1; day < WeatherController.Weather.ForecastDays; day++)
				cr.LineTo (BUTTON_SIZE + day * Allocation.Height * 2 + Allocation.Height / 2, 5 + height * (max - WeatherController.Weather.Forecasts [day].low));
			cr.Stroke ();
			
			// high temp points
			for (int day = 0; day < WeatherController.Weather.ForecastDays; day++)
				DrawDataPoint (cr, Allocation.Height, height, max, day, WeatherController.Weather.Forecasts [day].high);
			
			// low temp points
			for (int day = 0; day < WeatherController.Weather.ForecastDays; day++)
				DrawDataPoint (cr, Allocation.Height, height, max, day, WeatherController.Weather.Forecasts [day].low);
		}
		
		void DrawDataPoint (Cairo.Context cr, int cellWidth, double height, int max, int day, int temp)
		{
			cr.Color = new Cairo.Color (0, 0, 0, 0.4);
			cr.Arc (BUTTON_SIZE + day * cellWidth * 2 + cellWidth / 2 + 2, 7 + height * (max - temp), 3, 0, 2 * Math.PI);
			cr.Fill ();
			
			cr.Color = colorTitle;
			cr.Arc (BUTTON_SIZE + day * cellWidth * 2 + cellWidth / 2, 5 + height * (max - temp), 3, 0, 2 * Math.PI);
			cr.Fill ();
		}
		
		/// <summary>
		/// Paints the current condition.
		/// </summary>
		/// <param name="cr">
		/// A <see cref="Cairo.Context"/> to do the painting.
		/// </param>
		void DrawCurrentCondition (Cairo.Context cr)
		{
			int ySpacing = 2;
			int iconSize = Allocation.Height - 2 * ySpacing;
			int textWidth = iconSize * (WeatherController.Weather.ForecastDays < 6 ? 2 : 3);
			int xSpacing = (int) Math.Max (0, (Allocation.Width - 2 * BUTTON_SIZE - 3 * iconSize - 3 * textWidth) / 7);
			
			int topYPos = Allocation.Height / 3;
			int botYPos = 2 * topYPos;
			
			int xPos = BUTTON_SIZE + (int) (1.5 * xSpacing);
			
			// draw the temp
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (DockServices.Paths.SystemDataFolder.GetChild ("temp.svg").Path, iconSize)) {
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, xPos, ySpacing);
				cr.Paint ();
			}
			xPos += iconSize;
			
			if (!WeatherController.Weather.ShowFeelsLike)
				DrawConditionText (cr, xPos, textWidth, topYPos, WeatherController.Weather.Temp + AbstractWeatherSource.TempUnit);
			else
				DrawConditionText (cr, xPos, textWidth, topYPos, WeatherController.Weather.Temp + AbstractWeatherSource.TempUnit + " (" + WeatherController.Weather.FeelsLike + AbstractWeatherSource.TempUnit + ")");
			

			// draw humidity
			string humidity = String.Format (Catalog.GetString ("{0} humidity"), WeatherController.Weather.Humidity);
			DrawConditionText (cr, xPos, textWidth, botYPos, humidity);
			
			xPos += textWidth + 2 * xSpacing;
			

			// draw the wind
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (DockServices.Paths.SystemDataFolder.GetChild ("wind.svg").Path, iconSize)) {
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, xPos, ySpacing);
				cr.Paint ();
			}
			xPos += iconSize;
			
			DrawConditionText (cr, xPos, textWidth, topYPos, WeatherController.Weather.Wind + " " + AbstractWeatherSource.WindUnit);
			DrawConditionText (cr, xPos, textWidth, botYPos, WeatherController.Weather.WindDirection);
			
			xPos += textWidth + 2 * xSpacing;

			
			// draw sun
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (DockServices.Paths.SystemDataFolder.GetChild ("sun.svg").Path, iconSize)) {
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, xPos, ySpacing);
				cr.Paint ();
			}
			xPos += iconSize;
			
			DrawConditionText (cr, xPos, textWidth, topYPos, WeatherController.Weather.SunRise.ToShortTimeString ());
			DrawConditionText (cr, xPos, textWidth, botYPos, WeatherController.Weather.SunSet.ToShortTimeString ());
		}
		
		void DrawConditionText (Cairo.Context cr, int x, int xWidth, int yCenter, string text)
		{
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				Pango.Rectangle inkRect, logicalRect;
				
				layout.FontDescription = new Gtk.Style().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.Width = Pango.Units.FromPixels (Allocation.Width - BUTTON_SIZE - x);
				
				if (WeatherController.Weather.ForecastDays < 6)
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (Allocation.Height / 5));
				else
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (Allocation.Height / 3.5));
				
				cr.Color = new Cairo.Color (1, 1, 1, 0.9);
				layout.SetText (text);
				layout.GetPixelExtents (out inkRect, out logicalRect);
				cr.MoveTo (x + (xWidth - logicalRect.Width) / 2, yCenter - logicalRect.Height / 2);
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Fill ();
				
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();
			}
		}
		
		/// <summary>
		/// Paints the current condition.
		/// </summary>
		/// <param name="cr">
		/// A <see cref="Cairo.Context"/> to do the painting.
		/// </param>
		void DrawVertCurrentCondition (Cairo.Context cr)
		{
			int cellSize = Allocation.Height / 6;
			int iconSize = cellSize - 4;
			
			int yPos = 0;
			int topYPos = cellSize / 3;
			int botYPos = 2 * topYPos;
			
			// draw the temp
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (DockServices.Paths.SystemDataFolder.GetChild ("temp.svg").Path, iconSize)) {
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, (Allocation.Width - iconSize) / 2, yPos + (cellSize - iconSize) / 2);
				cr.Paint ();
			}
			yPos += cellSize;
			
			if (!WeatherController.Weather.ShowFeelsLike)
				DrawVertConditionText (cr, yPos + topYPos, WeatherController.Weather.Temp + AbstractWeatherSource.TempUnit);
			else
				DrawVertConditionText (cr, yPos + topYPos, WeatherController.Weather.Temp + AbstractWeatherSource.TempUnit + " (" + WeatherController.Weather.FeelsLike + AbstractWeatherSource.TempUnit + ")");
			

			// draw humidity
			string humidity = String.Format (Catalog.GetString ("{0} humidity"), WeatherController.Weather.Humidity);
			DrawVertConditionText (cr, yPos + botYPos, humidity);
			yPos += cellSize;
			

			// draw the wind
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (DockServices.Paths.SystemDataFolder.GetChild ("wind.svg").Path, iconSize)) {
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, (Allocation.Width - iconSize) / 2, yPos + (cellSize - iconSize) / 2);
				cr.Paint ();
			}
			yPos += iconSize;
			
			DrawVertConditionText (cr, yPos + topYPos, WeatherController.Weather.Wind + " " + AbstractWeatherSource.WindUnit);
			DrawVertConditionText (cr, yPos + botYPos, WeatherController.Weather.WindDirection);
			yPos += cellSize;

			
			// draw sun
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (DockServices.Paths.SystemDataFolder.GetChild ("sun.svg").Path, iconSize)) {
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, (Allocation.Width - iconSize) / 2, yPos + (cellSize - iconSize) / 2);
				cr.Paint ();
			}
			yPos += iconSize;
			
			DrawVertConditionText (cr, yPos + topYPos, WeatherController.Weather.SunRise.ToShortTimeString ());
			DrawVertConditionText (cr, yPos + botYPos, WeatherController.Weather.SunSet.ToShortTimeString ());
		}
		
		void DrawVertConditionText (Cairo.Context cr, int yCenter, string text)
		{
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				Pango.Rectangle inkRect, logicalRect;
				
				layout.FontDescription = new Gtk.Style().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.Width = Pango.Units.FromPixels (Allocation.Width);
				
				if (WeatherController.Weather.ForecastDays < 6)
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (Allocation.Width / 10));
				else
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (Allocation.Width / 7));
				
				cr.Color = new Cairo.Color (1, 1, 1, 0.9);
				layout.SetText (text);
				layout.GetPixelExtents (out inkRect, out logicalRect);
				cr.MoveTo ((Allocation.Width - logicalRect.Width) / 2, yCenter - logicalRect.Height / 2);
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Fill ();
				
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();
			}
		}
		
		protected override void OnShown ()
		{
			NumPages = IsVertical ? 2 : 3;
			base.OnShown ();
			Page = 1;
			ResetBuffers ();
			QueueRepaint ();
		}
		
		/// <summary>
		/// Called when new weather data arrives, to purge the buffers and redraw.
		/// </summary>
		public void WeatherChanged ()
		{
			ResetBuffers ();
			QueueRepaint ();
		}
	}
}
