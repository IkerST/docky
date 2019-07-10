// 
// Copyright (C) 2009 Robert Dyer
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;

using Mono.Unix;

using Docky.Menus;
using Docky.Services;

namespace GMail
{
	public class GMailMenuItem : MenuItem
	{
		UnreadMessage message;
		
		public GMailMenuItem (UnreadMessage message, string icon) : base (message.Topic + " - " + Catalog.GetString ("From: ") + message.FromName, icon)
		{
			Mnemonic = null;
			this.message = message;
			Clicked += delegate {
				DockServices.System.Open (this.message.Link);
			};
		}
	}
}

