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

using Gdk;

namespace Docky.Interface
{
	public class CursorTracker
	{
		static Dictionary<Gdk.Display, CursorTracker> trackers = new Dictionary<Gdk.Display, CursorTracker> ();
		
		public static CursorTracker ForDisplay (Gdk.Display display)
		{
			if (!trackers.ContainsKey (display))
				trackers [display] = new CursorTracker (display);
			
			return trackers [display];
		}
		
		const uint LowResTimeout = 250;
		const uint HighResTimeout = 20;
			
		Gdk.Display display;
		DateTime last_update;
		List<object> resolution_senders;
		uint timer;
		uint timer_speed;
		
		public event EventHandler<CursorPostionChangedArgs> CursorPositionChanged;
		
		public Gdk.Point Cursor { get; private set; }
		
		public ModifierType Modifier { get; private set; }
		
		public Gdk.Screen Screen { get; private set; }
		
		public bool Enabled { get; set; }
		
		CursorTracker (Gdk.Display display)
		{
			Enabled = true;
			this.display = display;
			resolution_senders = new List<object> ();
			ResetTimer ();
		}
		
		public void RequestHighResolution (object sender)
		{
			resolution_senders.Add (sender);
			
			if (timer_speed == HighResTimeout)
				return;
			
			ResetTimer ();
		}
		
		public void CancelHighResolution (object sender)
		{
			resolution_senders.Remove (sender);
			
			if (resolution_senders.Any ()) 
				return;
			
			ResetTimer ();
		}
		
		public void SendManualUpdate (Gdk.EventCrossing evnt)
		{
			// we get screwy inputs sometimes
			if (Math.Abs (evnt.XRoot - Cursor.X) > 100 || Math.Abs (evnt.YRoot - Cursor.Y) > 100) {
				OnTimerTick ();
				return;
			}
			Update (evnt.Window.Screen, (int) evnt.XRoot, (int) evnt.YRoot, evnt.State);
		}
		
		public void SendManualUpdate (Gdk.EventMotion evnt)
		{
			if (Math.Abs (evnt.XRoot - Cursor.X) > 100 || Math.Abs (evnt.YRoot - Cursor.Y) > 100) {
				OnTimerTick ();
				return;
			}
			Update (evnt.Window.Screen, (int) evnt.XRoot, (int) evnt.YRoot, evnt.State);
		}
		
		void ResetTimer ()
		{
			uint length = resolution_senders.Any () ? HighResTimeout : LowResTimeout;
			if (timer_speed != length) {
				if (timer > 0) {
					GLib.Source.Remove (timer);
					timer = 0;
				}
				if (!UserArgs.NoPollCursor)
					timer = GLib.Timeout.Add (length, OnTimerTick);
				timer_speed = length;
			}
		}
		
		bool OnTimerTick ()
		{
			if ((DateTime.UtcNow - last_update).TotalMilliseconds < 10) {
				return true;
			}
			
			int x, y;
			ModifierType mod;
			Gdk.Screen screen;
			display.GetPointer (out screen, out x, out y, out mod);
			
			Update (screen, x, y, mod);
			
			return true;
		}
		
		void Update (Gdk.Screen screen, int x, int y, Gdk.ModifierType mod)
		{
			last_update = DateTime.UtcNow;
				
			if (!Enabled)
				return;
			
			Gdk.Point lastPostion = Cursor;
			
			Cursor = new Gdk.Point (x, y);
			Modifier = mod;
			Screen = screen;
			
			if (lastPostion != Cursor)
				OnCursorPositionChanged (lastPostion);
		}
		
		void OnCursorPositionChanged (Gdk.Point oldPoint)
		{
			if (CursorPositionChanged != null) {
				CursorPostionChangedArgs args = new CursorPostionChangedArgs {
					LastPosition = oldPoint,
				};
				
				CursorPositionChanged (this, args);
			}
		}
	}
}
