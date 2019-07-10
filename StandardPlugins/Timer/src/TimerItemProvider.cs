//  
//  Copyright (C) 2010 Robert Dyer
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

namespace Timer
{
	public class TimerItemProvider : AbstractDockItemProvider
	{
		#region IDockItemProvider implementation
		
		public override string Name {
			get {
				return "Timer";
			}
		}
		
		public override void Dispose ()
		{
			foreach (AbstractDockItem adi in items)
				if (adi is TimerDockItem)
					(adi as TimerDockItem).Finished -= HandleTimerFinished;
			
			base.Dispose ();
		}
		
		public override bool RemoveItem (AbstractDockItem item)
		{
			if (item is TimerMainDockItem) {
				Items = Enumerable.Empty<AbstractDockItem> ();
				foreach (AbstractDockItem adi in items) {
					if (adi is TimerDockItem)
						(adi as TimerDockItem).Finished -= HandleTimerFinished;
					adi.Dispose ();
				}
			} else {
				TimerDockItem timer = item as TimerDockItem;
				timer.Finished -= HandleTimerFinished;
				items.Remove (timer);
				Items = items;
				timer.Dispose ();
			}
			
			return true;
		}
		
		#endregion

		List<AbstractDockItem> items;
		
		public TimerItemProvider ()
		{
			items = new List<AbstractDockItem> ();
			items.Add (new TimerMainDockItem (this));
			Items = items;
		}
		
		public void NewTimer ()
		{
			TimerDockItem timer = new TimerDockItem ();
			timer.Finished += HandleTimerFinished;
			items.Add (timer);
			Items = items;
		}
		
		void HandleTimerFinished (object o, EventArgs args)
		{
			TimerDockItem timer = o as TimerDockItem;
			timer.Finished -= HandleTimerFinished;
			items.Remove (timer);
			Items = items;
			timer.Dispose ();
		}
	}
}
