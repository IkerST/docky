//  
//  Copyright (C) 2009 Chris Szikszoy
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

using Notifications;

namespace Docky.Services
{
	public class Log<TSender> : Logging.LogBase
	{
		const string SenderFormat = "[{0}] {1}";

		static string AddSender (string message)
		{
			return string.Format (SenderFormat, typeof (TSender).Name, message);
		}

		public static void Debug (string msg, params object [] args)
		{
			Write (LogLevel.Debug, AddSender (msg), args);
		}
		
		public static void Info (string msg, params object [] args)
		{
			Write (LogLevel.Info, AddSender (msg), args);
		}
		
		public static Notification Notify (string msg)
		{
			// also write the log out to the console
			Write (LogLevel.Notify, AddSender (msg));
			
			return SendNote (typeof (TSender).Name, "", msg);
		}
		
		public static void Warn (string msg, params object [] args)
		{
			Write (LogLevel.Warn, AddSender (msg), args);
		}
		
		public static void Error (string msg, params object [] args)
		{
			Write (LogLevel.Error, AddSender (msg), args);
		}
		
		public static void Fatal (string msg, params object [] args)
		{
			Write (LogLevel.Fatal, AddSender (msg), args);
		}
	}
}
