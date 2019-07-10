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
using System.Web;
using System.Xml.Linq;

namespace NPR
{

	public enum StationUrlType : uint
	{
		ImageLogo = 0,
		OrgHomePage = 1,
		ProgramSchedule = 2,
		PledgePage = 4,
		AudioStream = 7,
		RssFeed = 8,
		Podcast = 9,
		AudioMP3Stream = 10,
		AudioWMAStream = 11,
		AudioRAMStream = 12,
	}

	public class StationUrl
	{

		public StationUrlType UrlType { get; private set; }
		public string Title { get; private set; }
		public string Target { get; private set; }
		
		public StationUrl (XElement urlElement)
		{
			Title = urlElement.Attribute ("title").Value;
			Target = urlElement.Value;
			UrlType = (StationUrlType)Enum.ToObject (typeof (StationUrlType), uint.Parse (urlElement.Attribute ("typeId").Value));
		}
	}
}
