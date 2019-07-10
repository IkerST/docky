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

using Mono.Unix;

using Cairo;
using Gtk;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace Timer
{
	public class TimerDockItem : AbstractDockItem
	{
		static uint id_counter = 0;
		uint id = 0;
		public override string UniqueID ()
		{
			return "TimerItem#" + id;
		}
		
		public event EventHandler Finished;
		
		uint Length { get; set; }
		
		uint remaining;
		uint Remaining {
			get {
				return remaining;
			}
			set {
				if (remaining == value)
					return;
				
				remaining = value;
				
				UpdateHoverText ();
				QueueRedraw ();
				
				if (remaining == 0)
					OnFinished (true, TimerMainDockItem.AutoDismissTimers);
			}
		}
		
		string label = null;
		string Label {
			get {
				if (string.IsNullOrEmpty (label))
					return "";
				return label + " - ";
			}
			set {
				label = value;
				UpdateHoverText ();
			}
		}
		
		DateTime LastRender { get; set; }
		
		bool Running { get; set; }
		
		uint timer;
		
		void OnFinished (bool notify, bool remove)
		{
			if (notify) {
				Log.Notify (Label + "Docky Timer", "clock", string.Format (Catalog.GetString ("A timer set for {0} has expired."), TimerMainDockItem.TimeRemaining (Length)));
				DockServices.System.Execute ("canberra-gtk-play -i \"system-ready\"");
			}
			
			if (remove && Finished != null)
				Finished (this, EventArgs.Empty);
		}
		
		public TimerDockItem ()
		{
			ScalableRendering = false;
			
			id = id_counter++;
			Remaining = Length = TimerMainDockItem.DefaultTimer;
			LastRender = DateTime.UtcNow;
			Running = false;
			
			if (TimerMainDockItem.AutoStartTimers)
				Toggle ();
		}

		protected override void PaintIconSurface (DockySurface surface)
		{
			Context cr = surface.Context;
			int size = Math.Min (surface.Width, surface.Height);
			double center = size / 2.0;
			
			// set from the svg files
			double svgWidth = 48.0;
			double centerRadius = 16;
			
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon ("base.svg@" + GetType ().Assembly.FullName, size)) {
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, (surface.Width - pbuf.Width) / 2, (surface.Height - pbuf.Height) / 2);
				cr.Paint ();
			}
			
			cr.Translate (0, size / svgWidth);
			
			Gdk.Color gtkColor = Style.Backgrounds [(int) StateType.Selected].SetMinimumValue (100);
			Cairo.Color color = new Cairo.Color ((double) gtkColor.Red / ushort.MaxValue,
										(double) gtkColor.Green / ushort.MaxValue,
										(double) gtkColor.Blue / ushort.MaxValue,
										1.0);
			
			double percent = (double) Remaining / (double) Length;
			if (Running)
				percent -= ((double) (DateTime.UtcNow - LastRender).TotalMilliseconds / 1000.0) / (double) Length;
			
			if (Remaining > 0) {
				cr.MoveTo (center, center);
				cr.Arc (center, center, size * centerRadius / svgWidth, -Math.PI / 2.0, Math.PI * 2.0 * percent - Math.PI / 2.0);
				cr.LineTo (center, center);
				cr.Color = color;
			} else {
				cr.Arc (center, center, size * centerRadius / svgWidth, 0, 2.0 * Math.PI);
				cr.Color = color.AddHue (150).SetSaturation (1);
			}
			cr.Fill ();
			
			cr.Save ();
			using (DockySurface hand = new DockySurface (surface.Width, surface.Height, surface)) {
				using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon ("hand.svg@" + GetType ().Assembly.FullName, size)) {
					Gdk.CairoHelper.SetSourcePixbuf (hand.Context, pbuf, (surface.Width - pbuf.Width) / 2, (surface.Height - pbuf.Height) / 2);
					hand.Context.Paint ();
				}
				cr.Translate (hand.Width / 2.0, hand.Height / 2.0 + 1);
				cr.Rotate (2.0 * Math.PI * percent);
				cr.Translate (- hand.Width / 2.0, - (hand.Height / 2.0 + 1));
				
				cr.SetSource (hand.Internal);
				cr.Paint ();
			}
			cr.Restore ();
			
			cr.Translate (0, size / -svgWidth);
			
			using (Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon ("overlay.svg@" + GetType ().Assembly.FullName, size)) {
				Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, (surface.Width - pbuf.Width) / 2, (surface.Height - pbuf.Height) / 2);
				cr.Paint ();
			}
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			uint amount = 1;
			
			if ((mod & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask)
				amount = 60;
			else if ((mod & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask)
				amount = 3600;
			
			if (direction == Gdk.ScrollDirection.Up || direction == Gdk.ScrollDirection.Right) {
				Length += amount;
				Remaining += amount;
			} else if (Remaining > amount) {
				Length -= amount;
				Remaining -= amount;
			}
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				if (Remaining > 0)
					Toggle ();
				else if (TimerMainDockItem.DismissOnClick)
					OnFinished (false, true);
				else
					Reset ();
			}
			
			return ClickAnimation.None;
		}
		
		void Reset ()
		{
			Remaining = Length;
			Running = false;
			QueueRedraw ();
			UpdateHoverText ();
		}
		
		public void Toggle ()
		{
			if (timer > 0) {
				GLib.Source.Remove (timer);
				timer = 0;
			}
			
			Running = !Running;
			
			if (Running) {
				LastRender = DateTime.UtcNow;
				
				timer = GLib.Timeout.Add (200, () => { 
					if (DateTime.UtcNow.Second != LastRender.Second) {
						Remaining--;
						LastRender = DateTime.UtcNow;
					} else {
						QueueRedraw ();
					}
					
					if (Remaining > 0)
						return true;
					
					timer = 0;
					return false;
				});
			}
			
			UpdateHoverText ();
		}
		
		void UpdateHoverText ()
		{
			String text;
			
			if (Running)
				text = Catalog.GetString ("Time remaining:") + " ";
			else
				text = Catalog.GetString ("Timer paused, time remaining:") + " ";
			
			HoverText = Label + text + TimerMainDockItem.TimeRemaining (remaining);
		}
		
		void SetLabel ()
		{
			Gtk.MessageDialog md = new Gtk.MessageDialog (null, 
					  0,
					  Gtk.MessageType.Question, 
					  Gtk.ButtonsType.None,
					  "<b>" + Catalog.GetString ("Set the timer's label to:") + "</b>");
			md.Title = "Docky Timer";
			md.Icon = DockServices.Drawing.LoadIcon ("docky", 22);
			md.Modal = false;
			
			md.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
			md.AddButton (Catalog.GetString ("_Set Label"), Gtk.ResponseType.Ok);
			md.DefaultResponse = Gtk.ResponseType.Ok;
			
			Gtk.Entry labelEntry = new Gtk.Entry ("" + label);
			labelEntry.Activated += delegate {
				Label = labelEntry.Text;
				md.Destroy ();
			};
			labelEntry.Show ();
			md.VBox.PackEnd (labelEntry);

			md.Response += (o, args) => {
				if (args.ResponseId != Gtk.ResponseType.Cancel)
					Label = labelEntry.Text;
				md.Destroy ();
			};
			
			md.Show ();
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			
			if (Remaining > 0)
				list[MenuListContainer.Actions].Add (new Docky.Menus.MenuItem (Running ? Catalog.GetString ("_Pause Timer") : Catalog.GetString ("_Start Timer"), Running ? "media-playback-pause" : "media-playback-start", delegate {
					Toggle ();
				}));
			
			list[MenuListContainer.Actions].Add (new Docky.Menus.MenuItem (Catalog.GetString ("R_eset Timer"), "document-revert", delegate {
				Reset ();
			}));
			
			list[MenuListContainer.Actions].Add (new Docky.Menus.MenuItem (Catalog.GetString ("_Remove Timer"), "gtk-remove", delegate {
				if (Finished != null)
					Finished (this, EventArgs.Empty);
			}));
			
			list[MenuListContainer.Actions].Add (new Docky.Menus.MenuItem (Catalog.GetString ("_Set Label"), "gtk-edit", delegate {
				SetLabel ();
			}));
			
			return list;
		}
		
		public override void Dispose ()
		{
			if (Running)
				Toggle ();
			base.Dispose ();
		}
	}
}
