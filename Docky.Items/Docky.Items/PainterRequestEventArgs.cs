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
using Docky.Painters;

namespace Docky.Items
{

	public enum ShowHideType {
		Show,
		Hide,
	}
	
	public class PainterRequestEventArgs : EventArgs
	{
		public AbstractDockItem Owner { get; private set; }
		
		public AbstractDockPainter Painter { get; private set; }
		
		public ShowHideType Type { get; private set; }
		
		public PainterRequestEventArgs (AbstractDockItem owner, AbstractDockPainter painter, ShowHideType type)
		{
			Owner = owner;
			Painter = painter;
			Type = type;
		}
	}
}
