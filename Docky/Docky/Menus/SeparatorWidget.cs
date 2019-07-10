//  
//  Copyright (C) 2009 Jason Smith
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

using Cairo;
using Gdk;
using Gtk;

using Docky.Services;

namespace Docky.Menus
{
	public class SeparatorWidget : EventBox
	{
		string title;
		
		public Cairo.Color TextColor { get; set; }
		
		public bool DrawLine { get; set; }
		
		public SeparatorWidget (string title)
		{
			this.title = title;
			HasTooltip = true;
			VisibleWindow = false;
			AboveChild = true;
			DrawLine = true;
			
			// Y-size must be odd to look pretty
			if (title == null)
				SetSizeRequest (-1, 3);
			else
				SetSizeRequest (-1, 11);
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return false;
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.LineWidth = 1;
				
				int x = Allocation.X;
				int width = Allocation.Width;
				int right = x + width;
				int xMiddle = x + width / 2;
				double yMiddle = Allocation.Y + Allocation.Height / 2.0;
				
				if (!string.IsNullOrEmpty (title))
					using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
						layout.SetText (title);
						layout.Width = Pango.Units.FromPixels (Allocation.Width - Allocation.Height);
						layout.FontDescription = Style.FontDescription;
						layout.Ellipsize = Pango.EllipsizeMode.End;
						layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (8);
						layout.FontDescription.Weight = Pango.Weight.Bold;
						
						Pango.Rectangle logical, ink;
						layout.GetPixelExtents (out ink, out logical);
						
						cr.MoveTo (Allocation.X + 2, Allocation.Y + (Allocation.Height - logical.Height) / 2);
						Pango.CairoHelper.LayoutPath (cr, layout);
						cr.Color = TextColor.SetAlpha (.6);
						cr.Fill ();
						
						x += logical.Width + 5;
						
						layout.Context.Dispose ();
					}
				
				if (DrawLine) {
					cr.MoveTo (x, yMiddle);
					cr.LineTo (right, yMiddle);
					
					RadialGradient rg = new RadialGradient (
						xMiddle, 
						yMiddle, 
						0, 
						xMiddle, 
						yMiddle, 
						width / 2);
					rg.AddColorStop (0, new Cairo.Color (0, 0, 0, 0.4));
					rg.AddColorStop (1, new Cairo.Color (0, 0, 0, 0));
					
					cr.Pattern = rg;
					cr.Stroke ();
					rg.Destroy ();
					
					cr.MoveTo (x, yMiddle + 1);
					cr.LineTo (right, yMiddle + 1);
					
					rg = new RadialGradient (
						xMiddle, 
						yMiddle + 1, 
						0, 
						xMiddle, 
						yMiddle + 1, 
						width / 2);
					rg.AddColorStop (0, new Cairo.Color (1, 1, 1, .4));
					rg.AddColorStop (1, new Cairo.Color (1, 1, 1, 0));
					
					cr.Pattern = rg;
					cr.Stroke ();
					rg.Destroy ();
				}
				
				(cr.Target as IDisposable).Dispose ();
			}
			return false;
		}
	}
}
