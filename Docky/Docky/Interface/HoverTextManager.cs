//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
//  Copyright (C) 2010 Robert Dyer, Rico Tzschichholz
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
using System.Linq;

using Cairo;
using Gdk;
using Gtk;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Services;

namespace Docky.Interface
{
	public class HoverTextManager : IDisposable
	{
		public DockPosition Gravity { get; set; }
		
		public bool Visible { 
			get { return window != null && window.Visible; }
		}
		
		Gtk.Window window;
		Gdk.Point currentPoint;
		DockySurface currentSurface;
		
		static DockySurface [] slices;
		DockySurface background_buffer;
		
		public static bool IsLight { get; private set; }
		
		static void SetLight ()
		{
			IsLight = DockServices.Drawing.IsIconLight (DockServices.Theme.TooltipSvg);
		}
		
		public int Monitor { get; set; }
		
		public HoverTextManager ()
		{
			SetLight ();
			
			window = new Gtk.Window (Gtk.WindowType.Popup);
			
			window.AppPaintable = true;
			window.AcceptFocus = false;
			window.Decorated = false;
			window.DoubleBuffered = true;
			window.SkipPagerHint = true;
			window.SkipTaskbarHint = true;
			window.Resizable = false;
			window.CanFocus = false;
			window.TypeHint = WindowTypeHint.Dock;
			window.Stick ();
			
			window.SetCompositeColormap ();
			window.ExposeEvent += HandleWindowExposeEvent;
						
			DockServices.Theme.ThemeChanged += DockyControllerThemeChanged;
		}
		
		void DockyControllerThemeChanged (object sender, EventArgs e)
		{
			if (slices != null) {
				foreach (DockySurface s in slices)
					s.Dispose ();
				slices = null;
			}
			ResetBackgroundBuffer ();
			SetLight ();
		}

		public void SetSurfaceAtPoint (DockySurface surface, Gdk.Point point)
		{
			if (surface == currentSurface && point == currentPoint) {
				window.QueueDraw ();
				return;
			}
			
			ResetBackgroundBuffer ();
			currentSurface = surface;
			currentPoint = point;
			
			if (surface == null) {
				Hide ();
				return;
			}
			
			window.SetSizeRequest (surface.Width, surface.Height);
				
			Gdk.Point center = Gdk.Point.Zero;
			int padding = 5;
			switch (Gravity) {
			case DockPosition.Top:
				center = new Gdk.Point (point.X - surface.Width / 2, point.Y + padding);
				break;
			case DockPosition.Left:
				center = new Gdk.Point (point.X + padding, point.Y - surface.Height / 2);
				break;
			case DockPosition.Right:
				center = new Gdk.Point (point.X - surface.Width - padding, point.Y - surface.Height / 2);
				break;
			case DockPosition.Bottom:
				center = new Gdk.Point (point.X - surface.Width / 2, point.Y - surface.Height - padding);
				break;
			}
			
			Gdk.Rectangle monitor_geo = window.Screen.GetMonitorGeometry (Monitor);
			center.X = Math.Max (monitor_geo.X, Math.Min (center.X, monitor_geo.X + monitor_geo.Width - surface.Width));
			center.Y = Math.Max (monitor_geo.Y, Math.Min (center.Y, monitor_geo.Y + monitor_geo.Height - surface.Height));
			
			if (Visible)
				window.QueueDraw ();
			window.Move (center.X, center.Y);
		}

		void HandleWindowExposeEvent (object o, ExposeEventArgs args)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (args.Event.Window)) {
				cr.Operator = Operator.Source;
				
				if (currentSurface == null || currentSurface.Internal == null) {
					cr.Color = new Cairo.Color (1, 1, 1, 0);
				} else {
					if (background_buffer == null) {
						background_buffer = new DockySurface (currentSurface.Width, currentSurface.Height, cr.Target);
						DrawBackground (background_buffer);
					}
					
					background_buffer.Internal.Show (cr, 0, 0);
					cr.Operator = Operator.Over;
					
					cr.SetSource (currentSurface.Internal);
				}
				
				cr.Paint ();

				(cr.Target as IDisposable).Dispose ();
			}
		}
		
		public void Show ()
		{
			if (currentSurface != null && window != null && !window.Visible)
				window.Show ();
		}
		
		public void Hide ()
		{
			if (Visible)
				window.Hide ();
		}
		
		static DockySurface[] GetSlices (DockySurface model)
		{
			if (slices != null)
				return slices;
			
			DockySurface main = new DockySurface (3 * AbstractDockItem.HoverTextHeight / 2, AbstractDockItem.HoverTextHeight, model);
			
			using (Gdk.Pixbuf pixbuf = DockServices.Drawing.LoadIcon (DockServices.Theme.TooltipSvg)) {
				Gdk.CairoHelper.SetSourcePixbuf (main.Context, pixbuf, 0, 0);
				main.Context.Paint ();
			}
			
			DockySurface[] results = new DockySurface[3];
			
			results[0] = main.CreateSlice (new Gdk.Rectangle (0, 0, AbstractDockItem.HoverTextHeight / 2, AbstractDockItem.HoverTextHeight));
			results[1] = main.CreateSlice (new Gdk.Rectangle (AbstractDockItem.HoverTextHeight / 2, 0, AbstractDockItem.HoverTextHeight / 2, AbstractDockItem.HoverTextHeight));
			results[2] = main.CreateSlice (new Gdk.Rectangle (AbstractDockItem.HoverTextHeight, 0, AbstractDockItem.HoverTextHeight / 2, AbstractDockItem.HoverTextHeight));
			
			slices = results;
			
			main.Dispose ();
			
			return slices;
		}
		
		void DrawBackground (DockySurface surface)
		{
			DockySurface[] slices = GetSlices (surface);
			
			surface.DrawSlice (slices[0], new Gdk.Rectangle (0, 0, AbstractDockItem.HoverTextHeight / 2, AbstractDockItem.HoverTextHeight));
			surface.DrawSlice (slices[1], new Gdk.Rectangle (AbstractDockItem.HoverTextHeight / 2, 0, Math.Max (0, surface.Width - AbstractDockItem.HoverTextHeight), AbstractDockItem.HoverTextHeight));
			surface.DrawSlice (slices[2], new Gdk.Rectangle (surface.Width - AbstractDockItem.HoverTextHeight / 2, 0, AbstractDockItem.HoverTextHeight / 2, AbstractDockItem.HoverTextHeight));
		}
		
		void ResetBackgroundBuffer ()
		{
			if (background_buffer != null) {
				background_buffer.Dispose ();
				background_buffer = null;
			}
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
			window.ExposeEvent -= HandleWindowExposeEvent;
			DockServices.Theme.ThemeChanged -= DockyControllerThemeChanged;
			
			currentSurface = null;

			if (window != null) {
				window.Destroy ();
				window.Dispose ();
				window = null;
			}
			if (slices != null) {
				foreach (DockySurface s in slices)
					s.Dispose ();
				slices = null;
			}
			ResetBackgroundBuffer ();
		}
		#endregion
	}
}
