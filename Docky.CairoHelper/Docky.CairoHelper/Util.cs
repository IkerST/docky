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

using Gdk;

namespace Docky.CairoHelper
{
	public static class Util
	{
		/// <summary>
		/// Set a color to use a maximum value
		/// </summary>
		/// <param name="gdk_color">
		/// A <see cref="Gdk.Color"/>
		/// </param>
		/// <param name="max_value">
		/// A <see cref="System.Double"/>
		/// </param>
		/// <returns>
		/// A <see cref="Gdk.Color"/>
		/// </returns>
		public static Gdk.Color SetMinimumValue (this Gdk.Color gdk_color, double min_value)
		{
			byte r, g, b; 
			double h, s, v;
			
			r = (byte) ((gdk_color.Red)   >> 8);
			g = (byte) ((gdk_color.Green) >> 8);
			b = (byte) ((gdk_color.Blue)  >> 8);
			
			RGBToHSV (r, g, b, out h, out s, out v);
			v = Math.Max (v, min_value);
			HSVToRGB (h, s, v, out r, out g, out b);
			
			return new Gdk.Color (r, g, b);
		}
		
		public static void RGBToHSV (byte r, byte g, byte b, 
									 out double hue, out double sat, out double val)
		{
			// Ported from Murrine Engine.
			double red, green, blue;
			double max, min;
			double delta;
			
			red = (double) r;
			green = (double) g;
			blue = (double) b;
			
			hue = 0;
			
			max = Math.Max (red, Math.Max (blue, green));
			min = Math.Min (red, Math.Min (blue, green));
			delta = max - min;
			val = max / 255.0 * 100.0;
			
			if (Math.Abs (delta) < 0.0001) {
				sat = 0;
			} else {
				sat = (delta / max) * 100;
				
				if (red == max)   hue = (green - blue) / delta;
				if (green == max) hue = 2 + (blue - red) / delta;
				if (blue == max)  hue = 4 + (red - green) / delta;
				
				hue *= 60;
				if (hue < 0) hue += 360;
			}
		}
		
		public static void HSVToRGB (double hue, double sat, double val,
									 out byte red, out byte green, out byte blue)
		{
			double h, s, v;
			double r = 0, g = 0, b = 0;

			h = hue;
			s = sat / 100;
			v = val / 100;

			if (s == 0) {
				r = v;
				g = v;
				b = v;
			} else {
				int secNum;
				double fracSec;
				double p, q, t;
				
				secNum = (int) Math.Floor(h / 60);
				fracSec = h/60 - secNum;

				p = v * (1 - s);
				q = v * (1 - s*fracSec);
				t = v * (1 - s*(1 - fracSec));
				
				switch (secNum) {
					case 0:
						r = v;
						g = t;
						b = p;
						break;
					case 1:
						r = q;
						g = v;
						b = p;
						break;
					case 2:
						r = p;
						g = v;
						b = t;
						break;
					case 3:
						r = p;
						g = q;
						b = v;
						break;
					case 4:
						r = t;
						g = p;
						b = v;
						break;
					case 5:
						r = v;
						g = p;
						b = q;
						break;
				}
			}
			red   = Convert.ToByte(r*255);
			green = Convert.ToByte(g*255);
			blue  = Convert.ToByte(b*255);
		}
	}
}
