//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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

namespace Docky.DBus
{
	public class DockyDBus : IDockyDBus
	{
		public event Action QuitCalled;
		public event Action SettingsCalled;
		public event Action AboutCalled;
		
		#region IDockyDBus implementation
		
		public event Action ShuttingDown;
		
		public void ShowAbout ()
		{
			if (AboutCalled != null)
				AboutCalled ();
		}
		
		public void ShowSettings ()
		{
			if (SettingsCalled != null)
				SettingsCalled ();
		}
		
		public void Quit ()
		{
			if (QuitCalled != null)
				QuitCalled ();
		}
		
		#endregion
		
		public void Shutdown ()
		{
			if (ShuttingDown != null)
				ShuttingDown ();
		}
	}
}
