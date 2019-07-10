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

using Cairo;
using Gdk;

namespace Docky.CairoHelper
{
	public class DockySurface : IDisposable
	{
		bool disposed;
		Surface surface;
		Context context;
		
		public Surface Internal {
			get { 
				if (surface == null && !disposed)
					surface = new ImageSurface (Format.Argb32, Width, Height);
				return surface; 
			}
			private set { surface = value; }
		}
		
		bool HasInternal {
			get { return surface != null; }
		}
		
		public int Width { get; private set; }
		
		public int Height { get; private set; }
		
		public Context Context {
			get {
				if (context == null && !disposed)
					context = new Context (Internal);
				return context;
			}
		}
		
		public DockySurface (int width, int height, Surface model) : this(width, height)
		{
			if (model != null)
				EnsureSurfaceModel (model);
		}

		public DockySurface (int width, int height, DockySurface model) : this(width, height)
		{
			if (model != null)
				EnsureSurfaceModel (model.Internal);
		}
		
		public DockySurface (int width, int height)
		{
			Width = width;
			Height = height;
		}
		
		public DockySurface (ImageSurface image) : this(image.Width, image.Height)
		{
			Internal = image;
		}

		public void ResetContext ()
		{
			if (context == null)
				return;
			
			(context as IDisposable).Dispose ();
			context = null;
		}
		
		public void Clear ()
		{
			if (disposed)
				return;
			Context.Save ();
			
			Context.Color = new Cairo.Color (0, 0, 0, 0);
			Context.Operator = Operator.Source;
			Context.Paint ();
			
			Context.Restore ();
		}
		
		public DockySurface DeepCopy ()
		{
			if (disposed)
				return null;
			Surface copy = Internal.CreateSimilar (Content.ColorAlpha, Width, Height);
			using (Cairo.Context cr = new Cairo.Context (copy)) {
				Internal.Show (cr, 0, 0);
				(cr.Target as IDisposable).Dispose ();
			}
			
			DockySurface result = new DockySurface (Width, Height);
			result.Internal = copy;
			
			return result;
		}
		
		public virtual void EnsureSurfaceModel (Surface reference)
		{
			if (disposed)
				return;
			if (reference == null)
				throw new ArgumentNullException ("Reference Surface", "Reference Surface may not be null");
			
			bool hadInternal = HasInternal;
			Surface last = null;
			if (hadInternal)
				last = Internal;
			
			// we dont need to copy to a model we are already on
			if (hadInternal && reference.SurfaceType == Internal.SurfaceType)
				return;
			
			Internal = reference.CreateSimilar (Cairo.Content.ColorAlpha, Width, Height);
			
			if (hadInternal) {
				using (Cairo.Context cr = new Cairo.Context (Internal)) {
					last.Show (cr, 0, 0);
					(cr.Target as IDisposable).Dispose ();
				}
				if (context != null) {
					(context as IDisposable).Dispose ();
					context = null;
				}
				(last as IDisposable).Dispose ();
				last.Destroy ();
			}
		}
		
		public DockySurface CreateSlice (Gdk.Rectangle area)
		{
			DockySurface result = new DockySurface (area.Width, area.Height, this);
			
			Internal.Show (result.Context, -area.X, -area.Y);
			
			return result;
		}
		
		public void DrawSlice (DockySurface slice, Gdk.Rectangle area)
		{
			// simple case goes here
			if (area.Width == slice.Width && area.Height == slice.Height) {
				slice.Internal.Show (Context, area.X, area.Y);
				return;
			}
			
			int columns = (area.Width / slice.Width) + 1;
			int rows = (area.Height / slice.Height) + 1;
			
			Context.Rectangle (area.X, area.Y, area.Width, area.Height);
			Context.Clip ();
			
			for (int c = 0; c < columns; c++) {
				for (int r = 0; r < rows; r++) {
					int x = area.X + c * slice.Width;
					int y = area.Y + r * slice.Height;
					
					Context.SetSource (slice.Internal, x, y);
					Context.Rectangle (x, y, slice.Width, slice.Height);
					Context.Fill ();
				}
			}
			
			Context.ResetClip ();
		}
		
		public Gdk.Pixbuf LoadToPixbuf ()
		{
			bool needsDispose = false;
			ImageSurface image_surface = (Internal as ImageSurface);
			if (image_surface == null) {
				image_surface = new ImageSurface (Format.Argb32, Width, Height);
				using (Cairo.Context cr = new Cairo.Context (image_surface)) {
					cr.Operator = Operator.Source;
					cr.SetSource (Internal);
					cr.Paint ();
				}
				needsDispose = true;
			}
			
			int width = image_surface.Width;
			int height = image_surface.Height;

			Gdk.Pixbuf pb = new Gdk.Pixbuf (Colorspace.Rgb, true, 8, width, height);
			pb.Fill (0x00000000);
			
			unsafe {
				byte *data = (byte*)image_surface.DataPtr;
				byte *pixels = (byte*)pb.Pixels;
				int length = width * height;
				
				if (image_surface.Format == Format.Argb32) {
					for (int i = 0; i < length; i++) {
						// if alpha is 0 set nothing
						if (data[3] > 0) {
							pixels[0] = (byte) (data[2] * 255 / data[3]);
							pixels[1] = (byte) (data[1] * 255 / data[3]);
							pixels[2] = (byte) (data[0] * 255 / data[3]);
							pixels[3] = data[3];
						}
			
						pixels += 4;
						data += 4;
					}
				} else if (image_surface.Format == Format.Rgb24) {
					for (int i = 0; i < length; i++) {
						pixels[0] = data[2];
						pixels[1] = data[1];
						pixels[2] = data[0];
						pixels[3] = data[3];
			
						pixels += 4;
						data += 4;
					}
				}
			}
			
			if (needsDispose)
				(image_surface as IDisposable).Dispose ();
			
			return pb;
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			
			if (context != null)
				(context as IDisposable).Dispose ();
			context = null;
			
			if (surface != null) {
				(surface as IDisposable).Dispose ();
				surface.Destroy ();
			}
			surface = null;
		}
		#endregion

		~DockySurface ()
		{
			Dispose ();
		}
	}
}
