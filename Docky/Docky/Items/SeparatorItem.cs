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

using Cairo;

using Docky.CairoHelper;

namespace Docky.Items
{
	internal class SeparatorItem : AbstractDockItem, INonPersistedItem
	{
		public override bool RotateWithDock {
			get { return true; }
		}
		
		public override bool Square {
			get { return false; }
		}
		
		public override bool Zoom {
			get { return false; }
		}
		
		public SeparatorItem ()
		{
		}
		
		public override string UniqueID ()
		{
			return "separator";
		}
		
		protected override DockySurface CreateIconBuffer (DockySurface model, int size)
		{
			return new DockySurface ((int) (size * .2), size, model);
		}
		
		protected override void PaintIconSurface3d (DockySurface surface, int height)
		{
			surface.Context.LineCap = LineCap.Round;
			surface.Context.LineWidth = 1;
			
			// Calculate the line count depending on the available height 
			int num_seps = (int) Math.Round (0.04 * surface.Height + 1.5);
			
			for (int i = 1; i <= num_seps; i++) {
				// Create some perspective illusion: lines are getting closer to eachother at the top
				double vertOffset = (int) (surface.Height + height * ((double) (num_seps - i) / num_seps - 1));
				double offset = 0.8 * (i - 1);

				surface.Context.Color = new Cairo.Color (1, 1, 1, 0.5);
				surface.Context.MoveTo (offset, vertOffset + 0.5);
				surface.Context.LineTo (surface.Width - offset, vertOffset + 0.5);
				surface.Context.Stroke ();
				
				surface.Context.Color = new Cairo.Color (0, 0, 0, 0.5);
				surface.Context.MoveTo (offset, vertOffset - 0.5);
				surface.Context.LineTo (surface.Width - offset, vertOffset - 0.5);
				surface.Context.Stroke ();
			}
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			surface.Context.LineWidth = 1;
			surface.Context.MoveTo ((surface.Width / 2) - 0.5, 0);
			surface.Context.LineTo ((surface.Width / 2) - 0.5, surface.Height);
			
			RadialGradient rg = new RadialGradient (surface.Width / 2, surface.Height / 2, 0, surface.Width / 2, surface.Height / 2, surface.Height / 2);
			rg.AddColorStop (0, new Cairo.Color (1, 1, 1, .5));
			rg.AddColorStop (1, new Cairo.Color (1, 1, 1, 0));
		
			surface.Context.Pattern = rg;
			surface.Context.Stroke ();
			rg.Destroy ();
			
			surface.Context.MoveTo ((surface.Width / 2) + 0.5, 0);
			surface.Context.LineTo ((surface.Width / 2) + 0.5, surface.Height);
			
			rg = new RadialGradient (surface.Width / 2, surface.Height / 2, 0, surface.Width / 2, surface.Height / 2, surface.Height / 2);
			rg.AddColorStop (0, new Cairo.Color (0, 0, 0, 0.5));
			rg.AddColorStop (1, new Cairo.Color (0, 0, 0, 0));
			
			surface.Context.Pattern = rg;
			surface.Context.Stroke ();
			rg.Destroy ();
		}
	}
}
