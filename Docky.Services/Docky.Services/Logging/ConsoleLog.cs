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

namespace Docky.Services.Logging
{


	internal static class ConsoleLog
	{
		/// <value>
		/// A string to make printing the current time simpler
		/// </value>
		const string TimeFormat = "{0:00}:{1:00}:{2:00}.{3:000}";
		
		/// <value>
		/// A consistent way of printing [Time LogLevel]
		/// </value>
		const string LogFormat = "[{0,-5} {1}]";
		
		/// <value>
		/// the current time using the TimeFormat format.
		/// </value>
		static string Time {
			get {
				DateTime now = DateTime.Now;
				return string.Format (TimeFormat, now.Hour, now.Minute, now.Second, now.Millisecond);
			}
		}

		static string FormatLogPrompt (LogLevel level)
		{
			string levelString = Enum.GetName (typeof (LogLevel), level);
			return string.Format (LogFormat, levelString, Time);
		}
		
		public static void Log (LogLevel level, string message)
		{
			switch (level) {
			case LogLevel.Fatal:
				ConsoleCrayon.BackgroundColor = ConsoleColor.Red;
				ConsoleCrayon.ForegroundColor = ConsoleColor.White;
				break;
			case LogLevel.Error:
				ConsoleCrayon.ForegroundColor = ConsoleColor.Red;
				break;
			case LogLevel.Warn:
				ConsoleCrayon.ForegroundColor = ConsoleColor.Yellow;
				break;
			case LogLevel.Notify:
				ConsoleCrayon.ForegroundColor = ConsoleColor.DarkMagenta;
				break;
			case LogLevel.Info:
				ConsoleCrayon.ForegroundColor = ConsoleColor.Blue;
				break;
			case LogLevel.Debug:
				ConsoleCrayon.ForegroundColor = ConsoleColor.Green;
				break;
			}
			Console.Write (FormatLogPrompt (level));
			ConsoleCrayon.ResetColor ();

			Console.WriteLine (" {0}", message);
		}		
	}
}
