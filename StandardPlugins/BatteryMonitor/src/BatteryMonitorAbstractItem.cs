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
using Mono.Unix;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;

namespace BatteryMonitor
{
	public abstract class BatteryMonitorAbstractItem : AbstractDockItem
	{
		const string BottomSvg  = "battery_bottom.svg";
		const string InsideSvg  = "battery_inside_{0}.svg";
		const string PluggedSvg = "battery_plugged.svg";
		const string TopSvg     = "battery_top.svg";
		
		protected int max_capacity;
		protected int current_capacity;
		protected int current_rate;
		uint timer;
		
		AbstractDockItemProvider owner;
		
		double Capacity {
			get {
				return (double) current_capacity / max_capacity;
			}
		}
		
		int RoundedCapacity {
			get {
				return (int) (Math.Round (Capacity, 1) * 100);
			}
		}
		
		public override string UniqueID ()
		{
			return "BatteryMonitor";
		}
		
		public BatteryMonitorAbstractItem (AbstractDockItemProvider owner)
		{
			this.owner = owner;
			DockServices.System.BatteryStateChanged += HandleBatteryStateChanged;
			
			timer = GLib.Timeout.Add (20 * 1000, UpdateBattStat);
		}
		
		void HandleBatteryStateChanged (object sender, EventArgs args)
		{
			UpdateBattStat ();
		}
		
		protected abstract void GetMaxBatteryCapacity ();
		
		protected abstract bool GetCurrentBatteryCapacity ();
		
		protected virtual double GetBatteryTime (bool charging)
		{
			if (charging)
				return (double) (max_capacity - current_capacity) / (double) current_rate;
			
			return (double) current_capacity / (double) current_rate;
		}
		
		public bool UpdateBattStat ()
		{
			max_capacity = 0;
			current_capacity = 0;
			current_rate = 0;
			
			bool charging = GetCurrentBatteryCapacity ();
			
			if (current_capacity == 0) {
				max_capacity = -1;
				HoverText = Catalog.GetString ("No Battery Found");
			} else {
				GetMaxBatteryCapacity ();
				
				if (current_rate == 0) {
					HoverText = string.Format ("{0:0.0}%", Capacity * 100);
				} else {
					double time = GetBatteryTime (charging);
					int hours = (int) time;
					int mins = (int) (60 * (time - hours));
					
					HoverText = "";
					if (hours > 0)
						HoverText += string.Format (Catalog.GetPluralString ("{0} hour", "{0} hours", hours), hours) + " ";
					if (mins > 0)
						HoverText += string.Format (Catalog.GetPluralString ("{0} minute", "{0} minutes", mins), mins) + " ";
					if (charging)
						HoverText += Catalog.GetString ("until charged ");
					else
						HoverText += Catalog.GetString ("remaining ");
					
					HoverText += string.Format ("({0:0.0}%)", Capacity * 100);
				}
			}
			
			bool hidden = max_capacity == -1 || /*!DockServices.System.OnBattery ||*/ Capacity > .98 || Capacity < .01;
			
			if (hidden && !(owner as BatteryMonitorItemProvider).Hidden)
				Log<BatteryMonitorProcItem>.Debug ("Hiding battery item (capacity=" + Capacity +
						") (max_capacity=" + max_capacity +
						") (OnBattery=" + DockServices.System.OnBattery + ")");

			(owner as BatteryMonitorItemProvider).Hidden = hidden;
			
			QueueRedraw ();

			return true;
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1 && DockServices.System.IsValidExecutable ("gnome-power-statistics")) {
				DockServices.System.Execute ("gnome-power-statistics --device /org/freedesktop/UPower/devices/battery_BAT0");
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			
			List<MenuItem> items = new List<MenuItem> ();
			
			if (DockServices.System.IsValidExecutable ("gnome-power-statistics"))
				items.Add (new MenuItem (Catalog.GetString ("_Statistics"),
						"gnome-power-statistics",
						delegate {
							DockServices.System.Execute ("gnome-power-statistics --device /org/freedesktop/UPower/devices/battery_BAT0");
						}));
			if (DockServices.System.IsValidExecutable ("gnome-power-preferences"))
				items.Add (new MenuItem (Catalog.GetString ("_Settings"),
						Gtk.Stock.Preferences,
						delegate {
							DockServices.System.Execute ("gnome-power-preferences");
						}));
			
			if (items.Count > 0)
				list[MenuListContainer.Actions].InsertRange (0, items);
			
			return list;
		}
		
		void RenderSvgOnContext (Context cr, string file, int size)
		{
			Gdk.Pixbuf pbuf = DockServices.Drawing.LoadIcon (file, size);
			Gdk.CairoHelper.SetSourcePixbuf (cr, pbuf, (pbuf.Width - size) / 2, (pbuf.Height - size) / 2);
			cr.Paint ();
			pbuf.Dispose ();
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			Context cr = surface.Context;
			int size = Math.Min (surface.Width, surface.Height);
			
			RenderSvgOnContext (cr, BottomSvg + "@" + GetType ().Assembly.FullName, size);
			if (RoundedCapacity > 0)
				RenderSvgOnContext (cr, string.Format (InsideSvg, RoundedCapacity) + "@" + GetType ().Assembly.FullName, size);
			RenderSvgOnContext (cr, TopSvg + "@" + GetType ().Assembly.FullName, size);
			if (!DockServices.System.OnBattery)
				RenderSvgOnContext (cr, PluggedSvg + "@" + GetType ().Assembly.FullName, size);
		}
		
		public override void Dispose ()
		{
			DockServices.System.BatteryStateChanged -= HandleBatteryStateChanged;

			if (timer > 0) {
				GLib.Source.Remove (timer);
				timer = 0;
			}

			base.Dispose ();
		}
	}
}
