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

using GLib;

namespace Docky.Services.Helpers
{

	public class HelperStatusChangedEventArgs : EventArgs
	{
		public File File { get; private set; }
		public bool Enabled { get; private set; }
		public bool IsRunning { get; private set; }
		
		public HelperStatusChangedEventArgs (File file, bool enabled, bool isRunning)
		{
			this.File = file;
			this.Enabled = enabled;
			this.IsRunning = isRunning;
		}
	}
}
