//  
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

namespace Docky.Services
{
	public static class GdkPixbufExtension
	{
		// these methods all assume that the BitsPerSample is 8 (byte).  Pixbuf documentation
		// states that values from 1-16 are allowed, but currently only 8 bit samples are supported.
		// http://library.gnome.org/devel/gdk-pixbuf/unstable/gdk-pixbuf-gdk-pixbuf.html#GdkPixbuf--bits-per-sample
		
		/// <summary>
		/// Applies a color transformation to each pixel in a pixbuf.
		/// </summary>
		/// <param name="colorTransform">
		/// A <see cref="Func<Cairo.Color, Cairo.Color>"/>
		/// </param>
		/// <returns>
		/// A reference to the input Pixbuf (the 'this' reference).
		/// </returns>
		public static Pixbuf PixelColorTransform (this Pixbuf source, Func<Cairo.Color, Cairo.Color> colorTransform)
		{
			try {
				if (source.BitsPerSample != 8)
					throw new Exception ("Pixbuf does not have 8 bits per sample, it has " + source.BitsPerSample);
				
				unsafe {
					double r, g, b;
					byte* pixels = (byte*) source.Pixels;
					for (int i = 0; i < source.Height * source.Rowstride / source.NChannels; i++) {
						r = (double) pixels[0];
						g = (double) pixels[1];
						b = (double) pixels[2];
						
						Cairo.Color color = new Cairo.Color (r / byte.MaxValue, 
						                                     g / byte.MaxValue, 
						                                     b / byte.MaxValue);
						
						color = colorTransform.Invoke (color);
						
						pixels[0] = (byte) (color.R * byte.MaxValue);
						pixels[1] = (byte) (color.G * byte.MaxValue);
						pixels[2] = (byte) (color.B * byte.MaxValue);
						
						pixels += source.NChannels;
					}
				}
			} catch (Exception e) {
				Log<DrawingService>.Error ("Error transforming pixbuf: {0}", e.Message);
				Log<DrawingService>.Debug (e.StackTrace);
			}
			return source;
		}
		
		/// <summary>
		/// Returns a monochrome version of the supplied pixbuf.
		/// </summary>
		/// <returns>
		/// A reference to the input Pixbuf (the 'this' reference).
		/// </returns>
		public static Pixbuf MonochromePixbuf (this Pixbuf source)
		{
			return source.PixelColorTransform ((c) => c.DarkenBySaturation (0.5).SetSaturation (0));
		}
		
		/// <summary>
		/// Adds a hue shift to the supplpied pixbuf.
		/// </summary>
		/// <param name="shift">
		/// A <see cref="System.Double"/>
		/// </param>
		/// <returns>
		/// A reference to the input Pixbuf (the 'this' reference).
		/// </returns>
		public static Pixbuf AddHueShift (this Pixbuf source, double shift)
		{
			if (shift != 0)
				source.PixelColorTransform ((c) => c.AddHue (shift));
			return source;
		}
		
		/// <summary>
		/// Scale a pixbuf to the desired width or height maintaining the aspect ratio of the supplied pixbuf.
		/// Note that due to maintaining the aspect ratio, the returned pixbuf may not have the exact width AND height as is specified.
		/// Though it is guaranteed that one of these measurements will be correct.
		/// </summary>
		/// <param name="width">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="height">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="pixbuf">
		/// A <see cref="Pixbuf"/>
		/// </param>
		/// <returns>
		/// </returns>
		public static Pixbuf ARScale (this Pixbuf source, int width, int height)
		{			
			double xScale = (double) width / (double) source.Width;
			double yScale = (double) height / (double) source.Height;
			double scale = Math.Min (xScale, yScale);
			
			if (scale == 1)
				return source;
			
			Pixbuf tmp = source.ScaleSimple ((int) (source.Width * scale),
											   (int) (source.Height * scale),
											   InterpType.Hyper);
			source.Dispose ();
			
			return tmp;
		}

		public static Pixbuf FromFileAtSize (string filename, int width, int height)
		{
			return NativeInterop.GdkPixbufNewFromFileAtSize (filename, width, height);
		}
	}
}
