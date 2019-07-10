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
using System.Linq;
using System.Collections.Generic;

using Docky.Widgets;
using Docky.Items;

namespace GMail
{

	public class GMailLabel : AbstractTileObject
	{
		GMailDockItem item;
		
		public GMailLabel (string labelName)
		{
			if (labelName == GMailDockItem.DefaultLabel)
				ShowActionButton = false;
			
			Name = labelName;
			item = GMailItemProvider.items.First (adi => (adi as GMailDockItem).Atom.CurrentLabel == labelName) as GMailDockItem;

			Icon = "gmail";
			if (item != null) {
				item.IconUpdated += HandleItemIconUpdated;
				SetIcon (item);
			}
		}

		void HandleItemIconUpdated (object sender, EventArgs e)
		{
			SetIcon (item);
		}
		
		void SetIcon (GMailDockItem item)
		{
			Icon = item.Icon;
			HueShift = item.HueShift;
		}
		
		public override void OnActiveChanged ()
		{
			List<string> labels = GMailPreferences.Labels.ToList ();
			labels.Remove (Name);
			GMailPreferences.Labels = labels.ToArray ();
		}
		
		public override void Dispose ()
		{
			if (item != null)
				item.IconUpdated -= HandleItemIconUpdated;
			
			base.Dispose ();
		}
	}
}
