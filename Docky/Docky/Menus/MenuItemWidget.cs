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

using Cairo;
using Gdk;
using Gtk;

using Docky.Services;
using Docky.CairoHelper;

namespace Docky.Menus
{
	internal class MenuItemWidget : Gtk.EventBox
	{
		const int MenuHeight = 22;
		const int MinWidth = 100;
		const int MaxWidth = 350;
		const int FontSize = 11;
		const int Padding = 4;
		const int IconBuffer = Padding - 1;
		
		public MenuItem item;
		
		public event EventHandler SelectedChanged;
		
 		public bool Selected { get; set; }
		
		bool menu_icons = false;
		public bool MenuShowingIcons {
			get {
				return menu_icons;
			}
			set {
				if (menu_icons == value)
					return;
				menu_icons = value;
				SetSize ();
			}
		}
		
		public Cairo.Color TextColor { get; set; }
		
		int TextWidth { get; set; }
		public int RequestedWidth { get; protected set; }
		
		DockySurface icon_surface, emblem_surface;
		
		internal MenuItemWidget (MenuItem item) : base()
		{
			TextColor = new Cairo.Color (1, 1, 1);
			this.item = item;
			item.IconChanged += ItemIconChanged;
			item.TextChanged += ItemTextChanged;
			item.DisabledChanged += ItemDisabledChanged;
			
			AddEvents ((int) Gdk.EventMask.AllEventsMask);
			
			HasTooltip = true;
			VisibleWindow = false;
			AboveChild = true;
			
			CalcTextWidth ();
		}
		
		void SetSize ()
		{
			RequestedWidth = TextWidth + 2 * Padding + 1;
			if (MenuShowingIcons)
				RequestedWidth += MenuHeight + Padding;
			
			SetSizeRequest (RequestedWidth, MenuHeight);
		}
		
		void CalcTextWidth ()
		{
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				char accel;
				
				string text = GLib.Markup.EscapeText (item.Text.Replace ("\n", ""));
				if (item.Mnemonic.HasValue)
					layout.SetMarkupWithAccel (text, '_', out accel);
				else
					layout.SetMarkup (text);
				layout.FontDescription = Style.FontDescription;
				layout.Ellipsize = Pango.EllipsizeMode.End;
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (FontSize);
				layout.FontDescription.Weight = Pango.Weight.Bold;
				
				Pango.Rectangle logical, ink;
				layout.GetPixelExtents (out ink, out logical);
				
				TextWidth = Math.Min (MaxWidth, Math.Max (MinWidth, logical.Width));
				HasTooltip = TextWidth < logical.Width;
				
				layout.Context.Dispose ();
			}
			
			SetSize ();
		}

		void ItemDisabledChanged (object sender, EventArgs e)
		{
			QueueDraw ();
		}

		void ItemTextChanged (object sender, EventArgs e)
		{
			CalcTextWidth ();
			QueueDraw ();
		}

		void ItemIconChanged (object sender, EventArgs e)
		{
			if (icon_surface != null)
				icon_surface.Dispose ();
			icon_surface = null;
			
			QueueDraw ();
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (!item.Disabled)
				item.SendClick ();
			return item.Disabled;
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			if (!item.Disabled && !Selected) {
				Selected = true;
				if (SelectedChanged != null)
					SelectedChanged (this, EventArgs.Empty);
				QueueDraw ();
			}
			return false;
		}
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			if (!item.Disabled && !Selected) {
				Selected = true;
				if (SelectedChanged != null)
					SelectedChanged (this, EventArgs.Empty);
				QueueDraw ();
			}
			return false;
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			if (!item.Disabled && Selected) {
				Selected = false;
				if (SelectedChanged != null)
					SelectedChanged (this, EventArgs.Empty);
				QueueDraw ();
			}
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnQueryTooltip (int x, int y, bool keyboard_tooltip, Tooltip tooltip)
		{
			tooltip.Text = item.Text;
			return true;
		}
		
		void PlaceSurface (Context cr, DockySurface surface, Gdk.Rectangle allocation)
		{
			int iconSize = allocation.Height - IconBuffer * 2;
			
			int x = allocation.X + Padding + ((iconSize - surface.Width) / 2);
			int y = allocation.Y + IconBuffer + ((iconSize - surface.Height) / 2);
			
			cr.SetSource (surface.Internal, x, y);
		}
		
		DockySurface LoadIcon (Pixbuf icon, int size)
		{
			DockySurface surface;
			using (Gdk.Pixbuf pixbuf = icon.Copy ().ARScale (size, size)) {
				surface = new DockySurface (pixbuf.Width, pixbuf.Height);
				Gdk.CairoHelper.SetSourcePixbuf (surface.Context, pixbuf, 0, 0);
				surface.Context.Paint ();
			}
			return surface;
		}
		
		DockySurface LoadIcon (string icon, int size)
		{
			bool monochrome = icon.StartsWith ("[monochrome]");
			if (monochrome)
				icon = icon.Substring ("[monochrome]".Length);
			
			Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (icon, size);
			DockySurface surface = LoadIcon (pbuf, size);
			pbuf.Dispose ();
			
			if (monochrome) {
				surface.Context.Operator = Operator.Atop;
				double v = TextColor.GetValue ();
				// reduce value by 20%
				surface.Context.Color = TextColor.SetValue (v * .8);
				surface.Context.Paint ();
				surface.ResetContext ();
			}
			
			return surface;
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return false;
			
			Gdk.Rectangle allocation = Allocation;
			
			int pixbufSize = allocation.Height - IconBuffer * 2;
			if (item.ShowIcons && (icon_surface == null || (icon_surface.Height != pixbufSize && icon_surface.Width != pixbufSize))) {
				if (icon_surface != null)
					icon_surface.Dispose ();
				if (emblem_surface != null)
					emblem_surface.Dispose ();
				
				if (item.ForcePixbuf == null)
					icon_surface = LoadIcon (item.Icon, pixbufSize);
				else
					icon_surface = LoadIcon (item.ForcePixbuf, pixbufSize);
				
				if (!string.IsNullOrEmpty (item.Emblem))
					emblem_surface = LoadIcon (item.Emblem, pixbufSize);
			}
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				if (Selected && !item.Disabled) {
					cr.Rectangle (allocation.X, allocation.Y, allocation.Width, allocation.Height);
					cr.Color = TextColor.SetAlpha (.1);
					cr.Fill ();
				}
				
				if (item.ShowIcons) {
					PlaceSurface (cr, icon_surface, allocation);
					cr.PaintWithAlpha (item.Disabled ? 0.5 : 1);
					
					if (item.Bold) {
						cr.Operator = Operator.Add;
						PlaceSurface (cr, icon_surface, allocation);
						cr.PaintWithAlpha (.8);
						cr.Operator = Operator.Over;
					}
					
					if (!string.IsNullOrEmpty (item.Emblem)) {
						PlaceSurface (cr, emblem_surface, allocation);
						cr.Paint ();
					}
				}
			
				using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
					char accel;
					string text = GLib.Markup.EscapeText (item.Text.Replace ("\n", ""));
					if (item.Mnemonic.HasValue)
						layout.SetMarkupWithAccel (text, '_', out accel);
					else
						layout.SetMarkup (text);
					layout.Width = Pango.Units.FromPixels (TextWidth);
					layout.FontDescription = Style.FontDescription;
					layout.Ellipsize = Pango.EllipsizeMode.End;
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (FontSize);
					layout.FontDescription.Weight = Pango.Weight.Bold;
					
					Pango.Rectangle logical, ink;
					layout.GetPixelExtents (out ink, out logical);
					
					int offset = Padding;
					if (MenuShowingIcons)
						offset += MenuHeight + Padding;
					cr.MoveTo (allocation.X + offset, allocation.Y + (allocation.Height - logical.Height) / 2);
					Pango.CairoHelper.LayoutPath (cr, layout);
					cr.Color = TextColor.SetAlpha (item.Disabled ? 0.5 : 1);
					cr.Fill ();
					
					layout.Context.Dispose ();
				}
				
				(cr.Target as IDisposable).Dispose ();
			}
			
			return true;
		}
		
		public override void Dispose ()
		{
			if (icon_surface != null)
				icon_surface.Dispose ();
			icon_surface = null;
			
			if (emblem_surface != null)
				emblem_surface.Dispose ();
			emblem_surface = null;
			
			item.IconChanged -= ItemIconChanged;
			item.TextChanged -= ItemTextChanged;
			item.DisabledChanged -= ItemDisabledChanged;
			item = null;
			
			base.Dispose ();
		}
	}
}
