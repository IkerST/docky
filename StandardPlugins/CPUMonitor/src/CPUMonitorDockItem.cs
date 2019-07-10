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
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using Cairo;
using Gdk;
using GLib;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;
using Docky.Services.Prefs;

namespace CPUMonitor
{
	public class CPUMonitorDockItem : AbstractDockItem
	{
		static IPreferences prefs = DockServices.Preferences.Get<CPUMonitorDockItem> ();

		string click_command = prefs.Get<string> ("ClickCommand", "gnome-system-monitor");
		
		const int UpdateMS = 1000;
		const double RadiusPercent = .9;
		
		enum ProcFields {
			User = 0,   // normal processes executing in user mode
			Nice,       // niced processes executing in user mode
			System,     // processes executing in kernel mode
			Idle,       // twiddling thunbs
			IOWait,     // waiting for I/O to complete
			IRQ,        // servicing interrupts
			SoftIRQ,    // servicing soft irqs
		}
		
		bool disposed;
		
		long last_usage;
		long last_idle;
		
		Regex regex;
		
		double CPUUtilization { get; set; }
		double MemoryUtilization { get; set; }
		
		public override string UniqueID () { return "CPUMonitor"; }
		
		public CPUMonitorDockItem ()
		{
			regex = new Regex ("\\d+");
			
			DockServices.System.RunOnThread (() => {
				while (!disposed) {
					UpdateUtilization ();
					System.Threading.Thread.Sleep (UpdateMS);
				}
			});
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				DockServices.System.Execute (click_command);
				return ClickAnimation.Bounce;
			}
			
			return ClickAnimation.None;
		}
		
		void UpdateUtilization ()
		{
			using (StreamReader reader = new StreamReader ("/proc/stat")) {
				string cpu_line = reader.ReadLine ();
				MatchCollection collection = regex.Matches (cpu_line);
				try {
					long usage = Convert.ToInt64 (collection [(int) ProcFields.User].Value) +
								Convert.ToInt64 (collection [(int) ProcFields.Nice].Value) +
								Convert.ToInt64 (collection [(int) ProcFields.System].Value) +
								Convert.ToInt64 (collection [(int) ProcFields.Idle].Value) +
								Convert.ToInt64 (collection [(int) ProcFields.IOWait].Value) +
								Convert.ToInt64 (collection [(int) ProcFields.IRQ].Value) +
								Convert.ToInt64 (collection [(int) ProcFields.SoftIRQ].Value);
					long idle = Convert.ToInt64 (collection [(int) ProcFields.Idle].Value) +
								Convert.ToInt64 (collection [(int) ProcFields.IOWait].Value);
					
					long usage_diff = usage - last_usage;
					long idle_diff = idle - last_idle;
					
					last_idle = idle;
					last_usage = usage;
					
					// average it for smoothing
					if (usage_diff > 0)
						CPUUtilization = Math.Max (0.01, Math.Round (((1 - (idle_diff / (double) usage_diff)) + CPUUtilization) / 2, 2));
				} catch {
					CPUUtilization = 0.01;
				}
			}
			
			using (StreamReader reader = new StreamReader ("/proc/meminfo")) {
				try {
					string data = reader.ReadToEnd ();
					MatchCollection collection = regex.Matches (data);
					int memTotal = Convert.ToInt32 (collection [0].Value);
					int memFree  = Convert.ToInt32 (collection [1].Value);
					int buffers  = Convert.ToInt32 (collection [2].Value);
					int cached   = Convert.ToInt32 (collection [3].Value);
					
					MemoryUtilization = 1 - ((memFree + buffers + cached) / (double) memTotal);
				} catch { 
					// we dont care
				}
			}
			
			DockServices.System.RunOnMainThread (() => {
				QueueRedraw ();
				HoverText = string.Format ("CPU: {0:0}%  Mem: {1:0}%", CPUUtilization * 100, MemoryUtilization * 100);
			});
		}

		protected override void PaintIconSurface (DockySurface surface)
		{
			int size = Math.Min (surface.Width, surface.Height);
			Context cr = surface.Context;
			
			double center = size / 2.0;
			Cairo.Color base_color = new Cairo.Color (1, .3, .3, .5).SetHue (120 * (1 - CPUUtilization));
			
			double radius = Math.Max (Math.Min (CPUUtilization * 1.3, 1), .001);
			
			// draw underlay
			cr.Arc (center, center, center * RadiusPercent, 0, Math.PI * 2);
			cr.Color = new Cairo.Color (0, 0, 0, .5);
			cr.FillPreserve ();
			
			RadialGradient rg = new RadialGradient (center, center, 0, center, center, center * RadiusPercent);
			rg.AddColorStop (0, base_color);
			rg.AddColorStop (0.2, base_color);
			rg.AddColorStop (1, new Cairo.Color (base_color.R, base_color.G, base_color.B, 0.15));
			cr.Pattern = rg;
			cr.FillPreserve ();
			
			rg.Destroy ();
			
			// draw cpu indicator
			rg = new RadialGradient (center, center, 0, center, center, center * RadiusPercent * radius);
			rg.AddColorStop (0, new Cairo.Color (base_color.R, base_color.G, base_color.B, 1));
			rg.AddColorStop (0.2, new Cairo.Color (base_color.R, base_color.G, base_color.B, 1));
			rg.AddColorStop (1, new Cairo.Color (base_color.R, base_color.G, base_color.B, Math.Max (0, CPUUtilization * 1.3 - 1)));
			cr.Pattern = rg;
			cr.Fill ();
			
			rg.Destroy ();
			
			// draw highlight
			cr.Arc (center, center * .8, center * .6, 0, Math.PI * 2);
			LinearGradient lg = new LinearGradient (0, 0, 0, center);
			lg.AddColorStop (0, new Cairo.Color (1, 1, 1, .35));
			lg.AddColorStop (1, new Cairo.Color (1, 1, 1, 0));
			cr.Pattern = lg;
			cr.Fill ();
			lg.Destroy ();
			
			// draw outer circles
			cr.LineWidth = 1;
			cr.Arc (center, center, center * RadiusPercent, 0, Math.PI * 2);
			cr.Color = new Cairo.Color (1, 1, 1, .75);
			cr.Stroke ();
			
			cr.LineWidth = 1;
			cr.Arc (center, center, center * RadiusPercent - 1, 0, Math.PI * 2);
			cr.Color = new Cairo.Color (.8, .8, .8, .75);
			cr.Stroke ();
			
			// draw memory indicate
			cr.LineWidth = size / 32.0;
			cr.ArcNegative (center, center, center * RadiusPercent - 1, Math.PI, Math.PI - Math.PI * (2 * MemoryUtilization));
			cr.Color = new Cairo.Color (1, 1, 1, .8);
			cr.Stroke ();
		}

		public override void Dispose ()
		{
			disposed = true;
			base.Dispose ();
		}
	}
}
