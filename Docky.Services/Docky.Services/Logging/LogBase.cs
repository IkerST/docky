//  
//  Copyright (C) 2009 Chris Szikszoy, Robert Dyer
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
using System.Linq;
using System.Collections.Generic;

using GLib;

using Notifications;

namespace Docky.Services.Logging
{
	public abstract class LogBase
	{
		class LogCall
		{
			public readonly LogLevel Level;
			public readonly string Message;

			public LogCall (LogLevel level, string message)
			{
				Level = level;
				Message = message;
			}
		}

		public static LogLevel DisplayLevel { get; set; }

		static bool Writing { get; set; }
		static ICollection<LogCall> PendingLogCalls { get; set; }
		static readonly string[] domains = new string[] {
			"Gtk",
			"Gdk",
			"GLib",
			"GLib-GObject",
			"Pango",
			"GdkPixbuf",
			"GLib-GIO",
		};

		static LogBase ()
		{
			Writing = false;
			PendingLogCalls = new LinkedList<LogCall> ();
			
			foreach (string domain in domains)
				GLib.Log.SetLogHandler (domain, LogLevelFlags.All, GLibLogFunc);
		}
		
		static void GLibLogFunc (string domain, LogLevelFlags level, string message)
		{
			LogLevel docky_log_level;
			
			switch (level) {
			case LogLevelFlags.Critical:
				docky_log_level = LogLevel.Fatal;
				break;
			case LogLevelFlags.Error:
				docky_log_level = LogLevel.Error;
				break;
			case LogLevelFlags.Warning:
				docky_log_level = LogLevel.Warn;
				break;
			case LogLevelFlags.Info:
			case LogLevelFlags.Message:
				docky_log_level = LogLevel.Info;
				break;
			case LogLevelFlags.Debug:
				docky_log_level = LogLevel.Debug;
				break;
			default:
				docky_log_level = LogLevel.Warn;
				break;
			}
			Write (docky_log_level, "[{0}] {1}", domain, message);
		}

		public static void Write (LogLevel level, string msg, params object[] args)
		{
			if (level < DisplayLevel) return;
			
			if (args.Length > 0)
				msg = string.Format (msg, args);
			if (Writing) {
				// In the process of logging, another log call has been made.
				// We need to avoid the infinite regress this may cause.
				PendingLogCalls.Add (new LogCall (level, msg));
			} else {
				Writing = true;

				if (PendingLogCalls.Any ()) {
					// Flush delayed log calls.
					// First, swap PendingLogCalls with an empty collection so it
					// is not modified while we enumerate.
					IEnumerable<LogCall> calls = PendingLogCalls;
					PendingLogCalls = new LinkedList<LogCall> ();
	
					// Log all pending calls.
					foreach (LogCall call in calls)
							ConsoleLog.Log (call.Level, call.Message);
				}

				// Log message.
				ConsoleLog.Log (level, msg);
				
				Writing = false;
			}
		}
		
		public static Notification SendNote (string sender, string icon, string msg)
		{			
			string title = sender;
			
			if (string.IsNullOrEmpty (sender))
				title = "Docky";
			
			return NotificationService.Notify (title, msg, icon);
		}
	}
}
