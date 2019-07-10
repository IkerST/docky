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
using System.Globalization;

using Cairo;
using Gdk;
using Gtk;

using Docky.CairoHelper;
using Docky.Painters;
using Docky.Services;

namespace Clock
{
	public class CalendarPainter : PagingDockPainter
	{
		int LineHeight { get; set; }
		
		public DateTime StartDate { get; set; }
		
		DateTime CalendarStartDate {
			get {
				return StartDate.AddDays ((int) DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek - (int) StartDate.DayOfWeek);
			}
		}
		
		public CalendarPainter () : base (3)
		{
			StartDate = DateTime.Today.AddDays (1 - DateTime.Today.Day);
		}
		
		#region IDockPainter implementation 

		public override int MinimumHeight {
			get {
				return 105;
			}
		}
		
		public override int MinimumWidth {
			get {
				return 600;
			}
		}
		
		protected override void DrawPageOnSurface (int page, DockySurface surface)
		{
			DateTime[] months = new DateTime[3];
			months [0] = months [1] = months [2] = CalendarStartDate;
			
			switch (page)
			{
				default:
				case 0:
					months [2] = CalendarStartDate.AddMonths (-1);
					months [1] = CalendarStartDate.AddMonths (1);
					break;
				
				case 1:
					months [0] = CalendarStartDate.AddMonths (-1);
					months [2] = CalendarStartDate.AddMonths (1);
					break;
				
				case 2:
					months [1] = CalendarStartDate.AddMonths (-1);
					months [0] = CalendarStartDate.AddMonths (1);
					break;
			}
			
			DrawMonth (surface.Context, months [page]);
		}
		
		#endregion 
		
		protected void DrawMonth (Context cr, DateTime start)
		{
			DateTime eow = start.AddDays (6);
			int height = 2 + (int) Math.Ceiling ((DateTime.DaysInMonth (eow.Year, eow.Month) - eow.Day) / 7.0);
			LineHeight = Allocation.Height / height;
			
			RenderMonthName (cr);
			RenderHeader (cr, start);
			for (int i = 1; i < height; i++)
				RenderLine (cr, start, i);
		}
		
		void RenderMonthName (Context cr)
		{
			string month = StartDate.ToString ("MMMM").ToUpper ();
			
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				layout.FontDescription = new Gtk.Style().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.Width = Pango.Units.FromPixels (Allocation.Height);
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (Allocation.Height / 6);
				
				cr.Save ();
				cr.Color = new Cairo.Color (1, 1, 1, .5);
				layout.SetText (month);
				
				Pango.Rectangle inkRect, logicalRect;
				layout.GetPixelExtents (out inkRect, out logicalRect);
				double scale = Math.Min (1, Math.Min (Allocation.Height / (double) inkRect.Width, (Allocation.Width / 9.0) / (double) logicalRect.Height));
				
				cr.Rotate (Math.PI / -2.0);
				cr.MoveTo ((Allocation.Height - scale * inkRect.Width) / 2 - Allocation.Height, Allocation.Width / 9 - scale * logicalRect.Height);
				if (scale < 1)
					cr.Scale (scale, scale);
				
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Fill ();
				cr.Restore ();
				
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();
			}
		}
		
		void RenderHeader (Context cr, DateTime start)
		{
			int centerLine = LineHeight + ((Allocation.Height % LineHeight) / 2);
			int offsetSize = Allocation.Width / 9;
			
			DateTime day = CalendarStartDate;
			
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				layout.FontDescription = new Gtk.Style().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.Width = Pango.Units.FromPixels (offsetSize);
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (0.625 * LineHeight));
				
				cr.Color = new Cairo.Color (1, 1, 1, .5);
				for (int i = 0; i < 7; i++) {
					layout.SetText (day.ToString ("ddd").ToUpper ());
					
					Pango.Rectangle inkRect, logicalRect;
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (offsetSize + offsetSize * i + (offsetSize - inkRect.Width) / 2, centerLine - logicalRect.Height);
					
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Fill ();
					day = day.AddDays (1);
				}
				
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();
			}
		}
		
		void RenderLine (Context cr, DateTime start, int line)
		{
			DateTime lineStart = start.AddDays ((line - 1) * 7);
			int offsetSize = Allocation.Width / 9;
			int centerLine = LineHeight + LineHeight * line + ((Allocation.Height % LineHeight) / 2);
			int dayOffset = 0;
			
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				Pango.Rectangle inkRect, logicalRect;
				
				layout.FontDescription = new Gtk.Style().FontDescription;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				layout.Width = Pango.Units.FromPixels (offsetSize);
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (0.625 * LineHeight));
				
				for (int i = 0; i < 7; i++) {
					layout.FontDescription.Weight = Pango.Weight.Normal;
					
					DateTime day = lineStart.AddDays (dayOffset);
					
					if (day.Month == CalendarStartDate.AddDays (6).Month)
						cr.Color = new Cairo.Color (1, 1, 1);
					else
						cr.Color = new Cairo.Color (1, 1, 1, 0.5);
					
					if (day.Date == DateTime.Today)
					{
						layout.FontDescription.Weight = Pango.Weight.Bold;
						Gdk.Color color = Style.Backgrounds [(int) StateType.Selected].SetMinimumValue (100);
						cr.Color = new Cairo.Color ((double) color.Red / ushort.MaxValue,
													(double) color.Green / ushort.MaxValue,
													(double) color.Blue / ushort.MaxValue,
													1.0);
					}
					dayOffset++;
					
					layout.SetText (string.Format ("{0:00}", day.Day));
					layout.GetPixelExtents (out inkRect, out logicalRect);
					cr.MoveTo (offsetSize + offsetSize * i + (offsetSize - inkRect.Width) / 2, centerLine - logicalRect.Height);
					
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Fill ();
				}
				
				cr.Color = new Cairo.Color (1, 1, 1, 0.4);
				int woy = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear (lineStart.AddDays (6), 
																			 DateTimeFormatInfo.CurrentInfo.CalendarWeekRule, 
																			 DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek);
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.SetText (string.Format ("W{0:00}", woy));
				layout.GetPixelExtents (out inkRect, out logicalRect);
				cr.MoveTo (offsetSize * 8, centerLine - logicalRect.Height);
				
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.Fill ();
				
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();
			}
		}
		
		protected override void OnShown ()
		{
			StartDate = DateTime.Today.AddDays (1 - DateTime.Today.Day);
			ResetBuffers ();
		}
		
		protected override void OnPageChanged ()
		{
			if (MovedLeft)
				StartDate = StartDate.AddMonths (-1);
			else
				StartDate = StartDate.AddMonths (1);
			ResetBuffers ();
		}
	}
}
