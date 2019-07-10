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
using System.Collections.Generic;
using System.Linq;

using Cairo;
using Gdk;
using Gtk;

using Docky.CairoHelper;
using Docky.Services;

namespace Docky.Interface
{
	public class PoofWindow
	{
		TimeSpan run_length = new TimeSpan (0, 0, 0, 0, 300);

		Gdk.Pixbuf poof;
		Gtk.Window window;

		int size;
		int x, y;

		DateTime run_time;

		double AnimationState {
			get { return Math.Max (0, Math.Min (1, (DateTime.UtcNow - run_time).TotalMilliseconds / run_length.TotalMilliseconds)); }
		}

		public PoofWindow (int size)
		{
			this.size = size;
		}

		public void SetCenterPosition (Gdk.Point point)
		{
			x = point.X - (size / 2);
			y = point.Y - (size / 2);
		}

		public void Run ()
		{
			var poof_file = DockServices.Paths.SystemDataFolder.GetChild ("poof.png");
			if (!poof_file.Exists)
				return;
			
			poof = new Pixbuf (poof_file.Path);
			
			window = new Gtk.Window (Gtk.WindowType.Toplevel);
			window.AppPaintable = true;
			window.Resizable = false;
			window.KeepAbove = true;
			window.CanFocus = false;
			window.TypeHint = WindowTypeHint.Splashscreen;
			window.SetCompositeColormap ();
			
			window.Realized += delegate { window.GdkWindow.SetBackPixmap (null, false); };
			
			window.SetSizeRequest (size, size);
			window.ExposeEvent += HandleExposeEvent;
			
			GLib.Timeout.Add (30, delegate {
				if (AnimationState == 1) {
					window.Hide ();
					window.Destroy ();
					poof.Dispose ();
					return false;
				} else {
					window.QueueDraw ();
					return true;
				}
			});
			
			window.Move (x, y);
			window.ShowAll ();
			run_time = DateTime.UtcNow; 
		}

		void HandleExposeEvent (object o, ExposeEventArgs args)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (window.GdkWindow)) {
				cr.Scale ((double) size / 128, (double) size / 128);
				cr.AlphaPaint ();
				int offset;
				switch ((int) Math.Floor (5 * AnimationState)) {
				case 0:
					offset = 0;
					break;
				case 1:
					offset = 128;
					break;
				case 2:
					offset = 128 * 2;
					break;
				case 3:
					offset = 128 * 3;
					break;
				default:
					offset = 128 * 4;
					break;
				}
				
				Gdk.CairoHelper.SetSourcePixbuf (cr, poof, 0, -(offset));
				cr.Paint ();
				
				(cr.Target as IDisposable).Dispose ();
			}
		}
	}
}
