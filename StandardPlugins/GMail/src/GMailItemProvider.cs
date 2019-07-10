//  
//  Copyright (C) 2009 Robert Dyer
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
using System.Collections.Generic;
using System.Linq;

using Docky.Items;

namespace GMail
{
	public class GMailItemProvider : AbstractDockItemProvider
	{
		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "GMail";
			}
		}
		
		#endregion
		
		public void ItemVisibilityChanged (AbstractDockItem item, bool newVisible)
		{
			SetItems ();
		}
		
		void SetItems ()
		{
			Items = items.Where (adi => (adi as GMailDockItem).Visible);
		}
		
		void RemoveItem (string label)
		{
			if (label == GMailDockItem.DefaultLabel)
				return;
			
			AbstractDockItem item = items.First (adi => (adi as GMailDockItem).Atom.CurrentLabel == label);
			items.Remove (item);
			
			SetItems ();
			
			item.Dispose ();
		}
		
		void AddItem (string label)
		{
			GMailDockItem it = new GMailDockItem (label, this);
			items.Add (it);
			
			SetItems ();
		}

		public static List<AbstractDockItem> items;
		
		public GMailItemProvider ()
		{
			items = new List<AbstractDockItem> ();
			
			AddItem (GMailDockItem.DefaultLabel);
			
			foreach (string label in GMailPreferences.Labels)
				AddItem (label);
			
			GMailPreferences.LabelsChanged += HandleLabelsChanged;
			
			SetItems ();
		}
		
		void HandleLabelsChanged (object o, EventArgs e)
		{
			string[] currentLabels = items.Select (adi => (adi as GMailDockItem).Atom.CurrentLabel)
				.Where (label => label != GMailDockItem.DefaultLabel).ToArray ();
			string[] newLabels = GMailPreferences.Labels;

			if (currentLabels.Length == newLabels.Length)
				return;
			
			if (currentLabels.Length > newLabels.Length)
				RemoveItem (currentLabels.Except (newLabels).First ());
			else
				AddItem (newLabels.Except (currentLabels).First());

			Registered ();
		}
		
		public override void Registered ()
		{
			GLib.Idle.Add (delegate {
				items.ForEach (adi => (adi as GMailDockItem).Atom.ResetTimer (true));
				return false;
			});
		}
		
		public override void Unregistered ()
		{
			items.ForEach (adi => (adi as GMailDockItem).Atom.StopTimer ());
		}
		
		public override void Dispose ()
		{
			GMailPreferences.LabelsChanged -= HandleLabelsChanged;
			
			base.Dispose ();
		}		
	}
}
