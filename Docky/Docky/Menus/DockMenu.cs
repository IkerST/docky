//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
//  Copyright (C) 2010 Rico Tzschichholz
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

using Docky.CairoHelper;
using Docky.Interface;
using Docky.Services;

namespace Docky.Menus
{
	public class DockMenu : Gtk.Window
	{
		// the size of the entire image
		const int SvgWidth     = 100;
		const int SvgHeight    = 120;
		
		// the size of the tail slice
		const int TailHeight   = 20;
		const int TailWidth    = 30;
		
		const int SliceSize    = 24;
		
		// size of the blur/shadow
		static int ShadowSize  = 12;
		
		// internal padding for the menu
		// aka, distance from slice edge to menuitems
		static int Padding     = ShadowSize + 4;
		
		// the minimum size of the windo's width
		// so slices dont overlap
		static int MinSize     = (ShadowSize + SliceSize) * 2 + TailWidth;
		
		// the size of the drawn menu
		static int TotalHeight = SvgHeight + 2 * ShadowSize;
		static int TotalWidth  = SvgWidth  + 2 * ShadowSize;
		
		static DockMenu ()
		{
			SetSizes ();
		}
		
		static void SetSizes ()
		{
			ShadowSize  = Gdk.Screen.Default.IsComposited ? 12 : 0;
			Padding     = ShadowSize + 4;
			TotalHeight = SvgHeight + 2 * ShadowSize;
			TotalWidth  = SvgWidth + 2 * ShadowSize;
			MinSize     = (ShadowSize + SliceSize) * 2 + TailWidth;
		}
		
		void HandleCompositedChanged (object o, EventArgs args) {
			SetSizes ();
			SetPadding ();
			ResetSlices ();
		}
		
		static int tailOffset;
		
		enum Slice {
			Top,
			Left,
			Right,
			Tail,
			TopLeft,
			TopRight,
			BottomLeft,
			BottomRight,
			TailLeft,
			TailRight,
			Center,
		}
		
		protected static bool IsLight { get; private set; }
		
		static void SetLight ()
		{
			IsLight = DockServices.Drawing.IsIconLight (DockServices.Theme.MenuSvg);
		}
		
		static DockySurface [] menu_slices;
		
		static DockySurface[] GetSlices (DockySurface model)
		{
			if (menu_slices != null)
				return menu_slices;
			
			DockySurface main = new DockySurface (TotalWidth, TotalHeight, model);
			
			using (Gdk.Pixbuf pixbuf = DockServices.Drawing.LoadIcon (DockServices.Theme.MenuSvg)) {
				Gdk.CairoHelper.SetSourcePixbuf (main.Context, pixbuf, ShadowSize, ShadowSize);
				main.Context.Paint ();
			}
			
			Gdk.Rectangle extents;
			if (ShadowSize > 0)
				using (DockySurface shadow = main.CreateMask (0, out extents)) {
					shadow.GaussianBlur (ShadowSize);
					shadow.Context.Operator = Operator.DestOut;
					for (int i = 0; i < 4; i++) {
						shadow.Context.SetSource (main.Internal);
						shadow.Context.Paint ();
					}
					
					main.Context.Operator = Operator.DestOver;
					main.Context.SetSource (shadow.Internal);
					main.Context.PaintWithAlpha (1);
					main.Context.Operator = Operator.Over;
				}
			
			int middleWidth = TotalWidth - 2 * SliceSize;
			int middleHeight = TotalHeight - 2 * SliceSize - TailHeight;
			int tailSliceSize = TailHeight + SliceSize;
			int tailSideSize = (middleWidth - TailWidth) / 2;
			
			DockySurface[] results = new DockySurface[11];
			results[(int) Slice.TopLeft] = main.CreateSlice (new Gdk.Rectangle (
					0, 
					0, 
					SliceSize, 
					SliceSize));
			
			results[(int) Slice.Top] = main.CreateSlice (new Gdk.Rectangle (
					SliceSize, 
					0, 
					middleWidth, 
					SliceSize));
			
			results[(int) Slice.TopRight] = main.CreateSlice (new Gdk.Rectangle (
					TotalWidth - SliceSize, 
					0, 
					SliceSize, 
					SliceSize));
			
			results[(int) Slice.Left] = main.CreateSlice (new Gdk.Rectangle (
					0, 
					SliceSize, 
					SliceSize, 
					middleHeight));
			
			results[(int) Slice.Center] = main.CreateSlice (new Gdk.Rectangle (
					SliceSize, 
					SliceSize, 
					middleWidth, 
					middleHeight));
			
			results[(int) Slice.Right] = main.CreateSlice (new Gdk.Rectangle (
					TotalWidth - SliceSize, 
					SliceSize, 
					SliceSize, 
					middleHeight));
			
			results[(int) Slice.BottomLeft] = main.CreateSlice (new Gdk.Rectangle (
					0, 
					TotalHeight - tailSliceSize, 
					SliceSize, 
					tailSliceSize));
			
			results[(int) Slice.TailLeft] = main.CreateSlice (new Gdk.Rectangle (
					SliceSize, 
					TotalHeight - tailSliceSize, 
					tailSideSize, 
					tailSliceSize));
			
			results[(int) Slice.Tail] = main.CreateSlice (new Gdk.Rectangle (
					SliceSize + tailSideSize,
					TotalHeight - tailSliceSize,
					TailWidth,
					tailSliceSize));
				
			results[(int) Slice.TailRight] = main.CreateSlice (new Gdk.Rectangle (
					SliceSize + middleWidth - tailSideSize,
					TotalHeight - tailSliceSize,
					tailSideSize,
					tailSliceSize));
			
			results[(int) Slice.BottomRight] = main.CreateSlice (new Gdk.Rectangle (
					SliceSize + middleWidth,
					TotalHeight - tailSliceSize,
					SliceSize,
					tailSliceSize));
			
			menu_slices = results;
			
			main.Dispose ();
			
			return menu_slices;
		}
		
		DockySurface background_buffer;
		Gdk.Rectangle allocation;
		DockPosition orientation;
		DateTime show_time;
		
		protected Gtk.Bin Container { get; private set; }
		
		public Gdk.Point Anchor { get; set; }
		
		public new bool Visible { get; set; }
		
		public DockPosition Orientation {
			get { return orientation; }
			set {
				if (orientation == value)
					return;
				orientation = value;
				SetPadding ();
				ResetBackgroundBuffer ();
			} 
		}
		
		public int Monitor { get; set; }
		
		public DockMenu (Gtk.Window parent) : base(Gtk.WindowType.Popup)
		{
			SetLight ();
			
			AcceptFocus = false;
			Decorated = false;
			KeepAbove = true;
			AppPaintable = true;
			SkipPagerHint = true;
			SkipTaskbarHint = true;
			Resizable = false;
			Modal = true;
			TypeHint = WindowTypeHint.Dock;
			
			AddEvents ((int) Gdk.EventMask.AllEventsMask);
			
			this.SetCompositeColormap ();
			
			Container = new Gtk.Alignment (0.5f, 0.5f, 0, 0);
			Container.Show ();
			
			Add (Container);
			
			SetPadding ();
			
			DockServices.Theme.ThemeChanged += DockyControllerThemeChanged;
			Gdk.Screen.Default.CompositedChanged += HandleCompositedChanged;
		}

		void DockyControllerThemeChanged (object sender, EventArgs e)
		{
			ResetSlices ();
			
			ResetBackgroundBuffer ();
			
			SetLight ();
		}
		
		void SetPadding ()
		{
			(Container as Alignment).LeftPadding   = Orientation == DockPosition.Left   ? (uint) (TailHeight + Padding) : (uint) Padding;
			(Container as Alignment).RightPadding  = Orientation == DockPosition.Right  ? (uint) (TailHeight + Padding) : (uint) Padding;
			(Container as Alignment).TopPadding    = Orientation == DockPosition.Top    ? (uint) (TailHeight + Padding) : (uint) Padding;
			(Container as Alignment).BottomPadding = Orientation == DockPosition.Bottom ? (uint) (TailHeight + Padding) : (uint) Padding;
		}
		
		void Reposition ()
		{
			Gdk.Rectangle monitor_geo = Screen.GetMonitorGeometry (Monitor);
			int oldX, oldY, x, y;
			
			switch (Orientation) {
			default:
			case DockPosition.Bottom:
				oldX = Anchor.X - allocation.Width / 2;
				oldY = Anchor.Y - allocation.Height;
				break;
			case DockPosition.Top:
				oldX = Anchor.X - allocation.Width / 2;
				oldY = Anchor.Y;
				break;
			case DockPosition.Left:
				oldX = Anchor.X;
				oldY = Anchor.Y - allocation.Height / 2;
				break;
			case DockPosition.Right:
				oldX = Anchor.X - allocation.Width;
				oldY = Anchor.Y - allocation.Height / 2;
				break;
			}
			
			// this magic keeps the menu on screen and makes the tail still point to the item
			switch (Orientation) {
			default:
			case DockPosition.Bottom:
			case DockPosition.Top:
				y = oldY;
				x = Math.Max (monitor_geo.X, Math.Min (oldX, monitor_geo.X + monitor_geo.Width - allocation.Width));
				tailOffset = x - oldX;
				break;
			
			case DockPosition.Left:
			case DockPosition.Right:
				x = oldX;
				y = Math.Max (monitor_geo.Y, Math.Min (oldY, monitor_geo.Y + monitor_geo.Height - allocation.Height));
				tailOffset = y - oldY;
			
				// rotation breaks this
				if (Orientation == DockPosition.Right)
					tailOffset = oldY - y;
				break;
			}
			
			Move (x, y);
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			this.allocation = allocation;
			ResetBackgroundBuffer ();
			Reposition ();
			base.OnSizeAllocated (allocation);
			
			if (Orientation == DockPosition.Bottom || Orientation == DockPosition.Top) {
				if (allocation.Width < MinSize)
					WidthRequest = MinSize;
			} else {
				if (allocation.Height < MinSize)
					HeightRequest = MinSize;
			}
		}
		
		protected override void OnShown ()
		{
			SetSizeRequest (-1, -1);
			Visible = true;
			show_time = DateTime.UtcNow;
			Reposition ();

			CursorTracker.ForDisplay (Display).Enabled = false;
			
			GLib.Timeout.Add (10, delegate {
				Gdk.GrabStatus status = Gdk.Pointer.Grab (
					GdkWindow, 
					true, 
					Gdk.EventMask.ButtonPressMask | 
					Gdk.EventMask.ButtonReleaseMask, 
					null, 
					null, 
					Gtk.Global.CurrentEventTime);
				
				if (status == GrabStatus.AlreadyGrabbed || status == GrabStatus.Success) {
					Gdk.Keyboard.Grab (GdkWindow, true, Gtk.Global.CurrentEventTime);
					Gtk.Grab.Add (this);
					return false;
				}
				return true;
			
			});
			
			base.OnShown ();
		}
		
		protected override void OnHidden ()
		{
			Visible = false;
			CursorTracker.ForDisplay (Display).Enabled = true;
			base.OnHidden ();
		}

		
		void ResetBackgroundBuffer ()
		{
			if (background_buffer != null) {
				background_buffer.Dispose ();
				background_buffer = null;
			}
		}
		
		void ResetSlices ()
		{
			if (menu_slices != null) {
				foreach (DockySurface s in menu_slices)
					s.Dispose ();
				menu_slices = null;
			}
		}
		
		void DrawBackground (DockySurface surface)
		{
			// This method is just annoying enough to turn into a loop that its hardly worth it
			
			DockySurface[] slices = GetSlices (surface);
			int middle = surface.Width / 2;
			
			// left to right
			int left = 0;
			int right = surface.Width;
			int leftMiddle = left + SliceSize;
			int rightMiddle = right - SliceSize;
			int leftTailMiddle = middle - (TailWidth / 2) - tailOffset;
			int rightTailMiddle = middle + (TailWidth / 2) - tailOffset;
			
			// keep the tail on the menu
			if (leftTailMiddle < SliceSize) {
				leftTailMiddle = SliceSize;
				rightTailMiddle = leftTailMiddle + TailWidth;
			}
			if (rightTailMiddle > right - SliceSize) {
				rightTailMiddle = right - SliceSize;
				leftTailMiddle = rightTailMiddle - TailWidth;
			}
			
			// top to bottom
			int top = 0;
			int bottom = surface.Height;
			int topMiddle = top + SliceSize;
			int bottomMiddle = bottom - (SliceSize + TailHeight);
			
			int yTop = top;
			int yBottom = topMiddle - top;
			int xLeft = left;
			int xRight = leftMiddle;
			surface.DrawSlice (slices[(int) Slice.TopLeft], new Gdk.Rectangle (
					xLeft, 
					yTop, 
					xRight - xLeft, 
					yBottom - yTop));
			
			xLeft = leftMiddle;
			xRight = rightMiddle;
			surface.DrawSlice (slices[(int) Slice.Top], new Gdk.Rectangle (
					xLeft, 
					yTop, 
					xRight - xLeft, 
					yBottom - yTop));
			
			xLeft = rightMiddle;
			xRight = right;
			surface.DrawSlice (slices[(int) Slice.TopRight], new Gdk.Rectangle (
					xLeft, 
					yTop, 
					xRight - xLeft, 
					yBottom - yTop));
			
			xLeft = left;
			xRight = leftMiddle;
			yTop = topMiddle;
			yBottom = bottomMiddle;
			surface.DrawSlice (slices[(int) Slice.Left], new Gdk.Rectangle (
					xLeft, 
					yTop, 
					xRight - xLeft, 
					yBottom - yTop));
			
			xLeft = leftMiddle;
			xRight = rightMiddle;
			surface.DrawSlice (slices[(int) Slice.Center], new Gdk.Rectangle (
					xLeft, 
					yTop, 
					xRight - xLeft, 
					yBottom - yTop));
			
			xLeft = rightMiddle;
			xRight = right;
			surface.DrawSlice (slices[(int) Slice.Right], new Gdk.Rectangle (
					xLeft, 
					yTop, 
					xRight - xLeft, 
					yBottom - yTop));
			
			xLeft = left;
			xRight = leftMiddle;
			yTop = bottomMiddle;
			yBottom = bottom;
			surface.DrawSlice (slices[(int) Slice.BottomLeft], new Gdk.Rectangle (
					xLeft, 
					yTop, 
					xRight - xLeft, 
					yBottom - yTop));
			
			xLeft = leftMiddle;
			xRight = leftTailMiddle;
			surface.DrawSlice (slices[(int) Slice.TailLeft], new Gdk.Rectangle (
					xLeft, 
					yTop, 
					xRight - xLeft, 
					yBottom - yTop));
			
			xLeft = leftTailMiddle;
			xRight = rightTailMiddle;
			surface.DrawSlice (slices[(int) Slice.Tail], new Gdk.Rectangle (
					xLeft, 
					yTop, 
					xRight - xLeft, 
					yBottom - yTop));
			
			xLeft = rightTailMiddle;
			xRight = rightMiddle;
			surface.DrawSlice (slices[(int) Slice.TailRight], new Gdk.Rectangle (
					xLeft, 
					yTop, 
					xRight - xLeft, 
					yBottom - yTop));
			
			xLeft = rightMiddle;
			xRight = right;
			surface.DrawSlice (slices[(int) Slice.BottomRight], new Gdk.Rectangle (
					xLeft, 
					yTop, 
					xRight - xLeft, 
					yBottom - yTop));
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return false;
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				if (background_buffer == null) {
					if (Orientation == DockPosition.Bottom || Orientation == DockPosition.Top) {
						background_buffer = new DockySurface (allocation.Width, allocation.Height, cr.Target);
					} else {
						// switch width and height so we can rotate it later
						background_buffer = new DockySurface (allocation.Height, allocation.Width, cr.Target);
					}
					DrawBackground (background_buffer);
				}
				
				switch (Orientation) {
				case DockPosition.Top:
					cr.Scale (1, -1);
					cr.Translate (0, -background_buffer.Height);
					break;
				case DockPosition.Left:
					cr.Rotate (Math.PI * .5);
					cr.Translate (0, -background_buffer.Height);
					break;
				case DockPosition.Right:
					cr.Rotate (Math.PI * -0.5);
					cr.Translate (-background_buffer.Width, 0);
					break;
				}
				
				cr.Operator = Operator.Source;
				background_buffer.Internal.Show (cr, 0, 0);

				(cr.Target as IDisposable).Dispose ();
			}
			
			return base.OnExposeEvent (evnt);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if ((DateTime.UtcNow - show_time).TotalMilliseconds > 500) {
				Hide ();
			}
			return base.OnButtonReleaseEvent (evnt);
		}
		
		public override void Dispose ()
		{
			DockServices.Theme.ThemeChanged -= DockyControllerThemeChanged;
			Gdk.Screen.Default.CompositedChanged -= HandleCompositedChanged;
			
			ResetSlices ();
			
			ResetBackgroundBuffer ();
			
			base.Dispose ();
		}

	}
}
