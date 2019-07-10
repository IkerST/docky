//  
//  Copyright (C) 2009 Jason Smith
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
using System.Threading;

using Cairo;
using Gdk;

using Docky.Interface;

namespace Docky.CairoHelper
{


	/// <summary>
	/// Advanced methods for a DockySurface that are not fit for "general" consumption, usually due to
	/// performance implications or very specific use cases.
	/// </summary>
	public static class DockySurface_Extensions
	{

		public static void ShowWithOptions (this DockySurface self, DockySurface target, PointD point, double zoom, double rotation, double opacity)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			
			Cairo.Context cr = target.Context;
			
			double cos, sin;
			cos = Math.Cos (rotation);
			sin = Math.Sin (rotation);
			Matrix m = new Matrix (cos, sin, -sin, cos, point.X, point.Y);
			cr.Transform (m);
			
			if (zoom != 1)
				cr.Scale (zoom, zoom);
			
			cr.SetSource (self.Internal,
				-self.Width / 2, 
				-self.Height / 2);
			
			cr.PaintWithAlpha (opacity);
			
			cr.IdentityMatrix ();
		}
		
		public static void ShowAsReflection (this DockySurface self, DockySurface target, PointD point, double zoom, 
			double rotation, double opacity, double height, DockPosition position)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			
			Cairo.Context cr = target.Context;
			
			switch (position) {
			case DockPosition.Left:
				point.X -= self.Width * zoom + height;
				break;
			case DockPosition.Top:
				point.Y -= self.Height * zoom + height;
				break;
			case DockPosition.Right:
				point.X += self.Width * zoom + height;
				break;
			case DockPosition.Bottom:
				point.Y += self.Height * zoom + height;
				break;
			}
			
			double cos, sin;
			cos = Math.Cos (rotation);
			sin = Math.Sin (rotation);
			Matrix m = new Matrix (cos, sin, -sin, cos, point.X, point.Y);
			cr.Transform (m);
			
			if (zoom != 1)
				cr.Scale (zoom, zoom);
			
			if (position == DockPosition.Left || position == DockPosition.Right)
				cr.Scale (-1, 1);
			else
				cr.Scale (1, -1);
			
			cr.SetSource (self.Internal, 
				-self.Width / 2, 
				-self.Height / 2);
			
			cr.PaintWithAlpha (opacity * .3);
			
			cr.IdentityMatrix ();
		}
		
		public static void ShowAtEdge (this DockySurface self, DockySurface target, PointD point, DockPosition position)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			
			Cairo.Context cr = target.Context;
			double x = point.X;
			double y = point.Y;
			
			switch (position) {
			case DockPosition.Top:
				x -= self.Width / 2;
				break;
			case DockPosition.Left:
				y -= self.Height / 2;
				break;
			case DockPosition.Right:
				x -= self.Width;
				y -= self.Height / 2;
				break;
			case DockPosition.Bottom:
				x -= self.Width / 2;
				y -= self.Height;
				break;
			}
			
			cr.SetSource (self.Internal, (int) x, (int) y);
			cr.Paint ();
		}
		
		public static void TileOntoSurface (this DockySurface self, DockySurface target, Gdk.Rectangle area, int edgeBuffer, double tilt, DockPosition orientation)
		{
			if (orientation == DockPosition.Left || orientation == DockPosition.Right) {
				
				int x = area.X;
				if (orientation == DockPosition.Left)
					x -= self.Width - area.Width;
				
				Cairo.Context cr = target.Context;
				
				// draw left edge
				cr.Rectangle (area.X, area.Y, area.Width, edgeBuffer);
				cr.SetSource (self.Internal, x, area.Y);
				cr.Fill ();
				
				int maxMiddleMove = self.Height - 2 * edgeBuffer;
				int position = area.Y + edgeBuffer;
				int edgeTarget = area.Y + area.Height - edgeBuffer;
				while (position < edgeTarget) {
					int height = Math.Min (edgeTarget - position, maxMiddleMove);
					cr.Rectangle (area.X, position, area.Width, height);
					cr.SetSource (self.Internal, x, position - edgeBuffer);
					cr.Fill ();
					position += height;
				}
				
				cr.Rectangle (area.X, position, area.Width, edgeBuffer);
				cr.SetSource (self.Internal, x, area.Y + area.Height - self.Height);
				cr.Fill ();
				
			} else {
				if (tilt != 1) {
					area.Y += (int) (area.Height * tilt);
					area.Height -= (int) (area.Height * tilt);
				}
				
				int y = area.Y;
				if (orientation == DockPosition.Top)
					y -= self.Height - area.Height;
				
				Cairo.Context cr = target.Context;
				cr.Rectangle (area.X - 100, area.Y, edgeBuffer + 100, area.Height);
				
				Matrix m = new Matrix (1, 0, -tilt, 1, 0, y);
				cr.Transform (m);
				
				// draw left edge
				cr.SetSource (self.Internal, area.X, 0);
				cr.Fill ();
				
				cr.IdentityMatrix ();
				
				int maxMiddleMove = self.Width - 2 * edgeBuffer;
				int position = area.X + edgeBuffer;
				int edgeTarget = area.X + area.Width - edgeBuffer;
				while (position < edgeTarget) {
					int width = Math.Min (edgeTarget - position, maxMiddleMove);
					cr.Rectangle (position, area.Y, width, area.Height);
					cr.SetSource (self.Internal, position - edgeBuffer, y);
					cr.Fill ();
					position += width;
				}
				
				cr.Rectangle (position, area.Y, edgeBuffer + 100, area.Height);
				
				m = new Matrix (1, 0, tilt, 1, 0, y);
				cr.Transform (m);
				
				cr.SetSource (self.Internal, area.X + area.Width - self.Width, 0);
				cr.Fill ();

				cr.IdentityMatrix ();
			}
		}
		
		public static DockySurface CreateMask (this DockySurface self, double cutOff, out Gdk.Rectangle extents)
		{
			ImageSurface original = new ImageSurface (Format.ARGB32, self.Width, self.Height);
			
			using (Cairo.Context cr = new Cairo.Context (original)) {
				cr.Operator = Operator.Source;
				cr.SetSource (self.Internal);
				cr.Paint ();
			}
			
			int width = original.Width;
			int height = original.Height;
			byte slice = (byte) (byte.MaxValue * cutOff);
			
			int left = width;
			int right = 0;
			int top = height;
			int bottom = 0;
			
			unsafe {
				byte* dataPtr = (byte*) original.DataPtr;
				
				int src;
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						src = (y * width + x) * 4;
						
						bool mask = dataPtr[src + 3] > slice;
						
						dataPtr[src + 0] = 0;
						dataPtr[src + 1] = 0;
						dataPtr[src + 2] = 0;
						dataPtr[src + 3] = mask ? byte.MaxValue : (byte) 0;
						
						if (mask) {
							if (y < top)
								top = y;
							if (y > bottom)
								bottom = y;
							if (x < left)
								left = x;
							if (x > right)
								right = x;
						}
					}
				}
			}
			
			extents = new Gdk.Rectangle (left, top, right - left, bottom - top);
			
			return new DockySurface (original);
		}
		
		public unsafe static void GaussianBlur (this DockySurface self, int size)
		{
			// Note: This method is wickedly slow
			
			int gaussWidth = size * 2 + 1;
			double[] kernel = BuildGaussianKernel (gaussWidth);
			
			ImageSurface original = new ImageSurface (Format.Argb32, self.Width, self.Height);
			using (Cairo.Context cr = new Cairo.Context (original))
				self.Internal.Show (cr, 0, 0);
			
			double gaussSum = 0;
			foreach (double d in kernel)
				gaussSum += d;
			
			for (int i = 0; i < kernel.Length; i++)
				kernel[i] = kernel[i] / gaussSum;
			
			int width = self.Width;
			int height = self.Height;
			
			byte[] src = original.Data;
			double[] xbuffer = new double[original.Data.Length];
			double[] ybuffer = new double[original.Data.Length];
			
			int dest, dest2, shift, source;
			
			byte* srcPtr = (byte*) original.DataPtr;
			
			fixed (double* xbufferPtr = xbuffer)
			fixed (double* ybufferPtr = ybuffer) 
			fixed (double* kernelPtr = kernel) {
				// Horizontal Pass
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						dest = y * width + x;
						dest2 = dest * 4;
						
						for (int k = 0; k < gaussWidth; k++) {
							shift = k - size;
							
							source = dest + shift;
							
							if (x + shift <= 0 || x + shift >= width) {
								source = dest;
							}
							
							source = source * 4;
							xbufferPtr[dest2 + 0] = xbufferPtr[dest2 + 0] + (srcPtr[source + 0] * kernelPtr[k]);
							xbufferPtr[dest2 + 1] = xbufferPtr[dest2 + 1] + (srcPtr[source + 1] * kernelPtr[k]);
							xbufferPtr[dest2 + 2] = xbufferPtr[dest2 + 2] + (srcPtr[source + 2] * kernelPtr[k]);
							xbufferPtr[dest2 + 3] = xbufferPtr[dest2 + 3] + (srcPtr[source + 3] * kernelPtr[k]);
						}
					}
				}
			
				// Vertical Pass
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						dest = y * width + x;
						dest2 = dest * 4;
						
						for (int k = 0; k < gaussWidth; k++) {
							shift = k - size;
							
							source = dest + shift * width;
							
							if (y + shift <= 0 || y + shift >= height) {
								source = dest;
							}
							
							source = source * 4;
							ybufferPtr[dest2 + 0] = ybufferPtr[dest2 + 0] + (xbufferPtr[source + 0] * kernelPtr[k]);
							ybufferPtr[dest2 + 1] = ybufferPtr[dest2 + 1] + (xbufferPtr[source + 1] * kernelPtr[k]);
							ybufferPtr[dest2 + 2] = ybufferPtr[dest2 + 2] + (xbufferPtr[source + 2] * kernelPtr[k]);
							ybufferPtr[dest2 + 3] = ybufferPtr[dest2 + 3] + (xbufferPtr[source + 3] * kernelPtr[k]);
						}
					}
				}
				
				for (int i = 0; i < src.Length; i++)
					srcPtr[i] = (byte) ybufferPtr[i];
			}
			
			self.Context.Operator = Operator.Source;
			self.Context.SetSource (original);
			self.Context.Paint ();
			
			(original as IDisposable).Dispose ();
			original.Destroy ();
		}
		
		static double[] BuildGaussianKernel (int gaussWidth)
		{
			if (gaussWidth % 2 != 1)
				throw new ArgumentException ("Gaussian Width must be odd");
			
			double[] kernel = new double[gaussWidth];
			
			// Maximum value of curve
			double sd = 255;
			
			// Width of curve
			double range = gaussWidth;
			
			// Average value of curve
			double mean = range / sd;
			
			for (int i = 0; i < gaussWidth / 2 + 1; i++) {
				kernel[i] = Math.Pow (Math.Sin (((i + 1) * (Math.PI / 2) - mean) / range), 2) * sd;
				kernel[gaussWidth - i - 1] = kernel[i];
			}
			
			return kernel;
		}
		
		const int AlphaPrecision = 16; 
		const int ParamPrecision = 7;
		
		public unsafe static void ExponentialBlur (this DockySurface self, int radius)
		{
			self.ExponentialBlur (radius, new Gdk.Rectangle (0, 0, self.Width, self.Height));
		}
		
		public unsafe static void ExponentialBlur (this DockySurface self, int radius, Gdk.Rectangle area)
		{
			if (radius < 1)
				return;
			
			area.Intersect (new Gdk.Rectangle (0, 0, self.Width, self.Height));
			
			int alpha = (int) ((1 << AlphaPrecision) * (1.0 - Math.Exp (-2.3 / (radius + 1.0))));
			int height = self.Height;
			int width = self.Width;
			
			ImageSurface original;
			bool owned;
			if (self.Internal is ImageSurface) {
				original = self.Internal  as ImageSurface;
				owned = true;
			} else {
				original = new ImageSurface (Format.Argb32, width, height);
				owned = false;
			}
			
			if (!owned) {
				using (Cairo.Context cr = new Cairo.Context (original)) {
					cr.Operator = Operator.Source;
					cr.SetSource (self.Internal);
					cr.Paint ();
					(cr.Target as IDisposable).Dispose ();
				}
			}
			
			byte* pixels = (byte*) original.DataPtr;
			
			// Process Rows
			Thread th = new Thread ((ThreadStart) delegate {
				ExponentialBlurRows (pixels, width, height, 0, height / 2, area.X, area.Right, alpha);
			});
			th.Start ();
			
			ExponentialBlurRows (pixels, width, height, height / 2, height, area.X, area.Right, alpha);
			th.Join ();
			
			// Process Columns
			th = new Thread ((ThreadStart) delegate {
				ExponentialBlurColumns (pixels, width, height, 0, width / 2, area.Y, area.Bottom, alpha);
			});
			th.Start ();
			
			ExponentialBlurColumns (pixels, width, height, width / 2, width, area.Y, area.Bottom, alpha);
			th.Join ();
			
			original.MarkDirty ();
			
			if (!owned) {
				self.Context.Operator = Operator.Source;
				self.Context.SetSource (original);
				self.Context.Paint ();
				self.Context.Operator = Operator.Over;
				
				(original as IDisposable).Dispose ();
				original.Destroy ();
			}
		}
		
		unsafe static void ExponentialBlurColumns (byte* pixels, int width, int height, int startCol, int endCol, int startY, int endY, int alpha)
		{
			for (int columnIndex = startCol; columnIndex < endCol; columnIndex++) {
				int zR, zG, zB, zA;
				// blur columns
				byte *column = pixels + columnIndex * 4;
				
				zR = column[0] << ParamPrecision;
				zG = column[1] << ParamPrecision;
				zB = column[2] << ParamPrecision;
				zA = column[3] << ParamPrecision;
				
				// Top to Bottom
				for (int index = width * (startY + 1); index < (endY - 1) * width; index += width) {
					ExponentialBlurInner (&column[index * 4], ref zR, ref zG, ref zB, ref zA, alpha);
				}
				
				// Bottom to Top
				for (int index = (endY - 2) * width; index >= startY; index -= width) {
					ExponentialBlurInner (&column[index * 4], ref zR, ref zG, ref zB, ref zA, alpha);
				}
			}
		}
		
		unsafe static void ExponentialBlurRows (byte* pixels, int width, int height, int startRow, int endRow, int startX, int endX, int alpha)
		{
			for (int rowIndex = startRow; rowIndex < endRow; rowIndex++) {
				int zR, zG, zB, zA;
				// Get a pointer to our current row
				byte* row = pixels + rowIndex * width * 4;
				
				zR = row[startX + 0] << ParamPrecision;
				zG = row[startX + 1] << ParamPrecision;
				zB = row[startX + 2] << ParamPrecision;
				zA = row[startX + 3] << ParamPrecision;
				// Left to Right
				for (int index = startX + 1; index < endX; index++) {
					ExponentialBlurInner (&row[index * 4], ref zR, ref zG, ref zB, ref zA, alpha);
				}
				
				// Right to Left
				for (int index = endX - 2; index >= startX; index--) {
					ExponentialBlurInner (&row[index * 4], ref zR, ref zG, ref zB, ref zA, alpha);
				}
			}
		}
		
		unsafe static void ExponentialBlurInner (byte* pixel, ref int zR, ref int zG, ref int zB, ref int zA, int alpha)
		{
			int R, G, B, A;
			R = pixel[0];
			G = pixel[1];
			B = pixel[2];
			A = pixel[3];
			
			zR += (alpha * ((R << ParamPrecision) - zR)) >> AlphaPrecision;
			zG += (alpha * ((G << ParamPrecision) - zG)) >> AlphaPrecision;
			zB += (alpha * ((B << ParamPrecision) - zB)) >> AlphaPrecision;
			zA += (alpha * ((A << ParamPrecision) - zA)) >> AlphaPrecision;
			
			pixel[0] = (byte) (zR >> ParamPrecision);
			pixel[1] = (byte) (zG >> ParamPrecision);
			pixel[2] = (byte) (zB >> ParamPrecision);
			pixel[3] = (byte) (zA >> ParamPrecision);
		}
	}
}
