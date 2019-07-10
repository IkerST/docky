//  
//  Copyright (C) 2010-2011 Robert Dyer
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

using Gdk;

using Docky.CairoHelper;
using Docky.Menus;
using Docky.Services.Prefs;

namespace Docky.Items
{
	public class ProxyDockItem : AbstractDockItem
	{
		AbstractDockItem currentItem;
		
		protected bool StripMnemonics { get; set; }
		
		int currentPos;
		int CurrentPosition {
			get { return currentPos; }
			set {
				if (value == currentPos)
					return;
				
				currentPos = value;
				if (prefs != null)
					prefs.Set<int> ("CurrentIndex", value);
			}
		}
		
		public event EventHandler<CurrentItemChangedArgs> CurrentItemChanged;
		
		IPreferences prefs;
		
		public AbstractDockItemProvider Provider { get; protected set; }
		
		public ProxyDockItem (AbstractDockItemProvider provider) : this (provider, null)
		{
		}
		
		public ProxyDockItem (AbstractDockItemProvider provider, IPreferences prefs)
		{
			StripMnemonics = false;
			
			Provider = provider;
			Provider.ItemsChanged += HandleProviderItemsChanged;
			this.prefs = prefs;
			
			if (prefs == null) {
				currentPos = 0;
			} else {
				currentPos = prefs.Get<int> ("CurrentIndex", 0);
				
				if (CurrentPosition >= Provider.Items.Count ()) 
					CurrentPosition = 0;
			}
			
			ItemChanged ();
		}
		
		void HandleProviderItemsChanged (object o, EventArgs args)
		{
			if (!SetItem (currentItem))
				ItemChanged ();
		}
		
		public bool SetItem (AbstractDockItem item)
		{
			return SetItem (item, false);
		}
		
		public bool SetItem (AbstractDockItem item, bool temporary)
		{
			if (!Provider.Items.Contains (item))
				return false;
			
			AbstractDockItem[] items = Provider.Items.ToArray ();
			
			for (int i = 0; i < items.Count (); i++)
				if (item == items [i]) {
					if (temporary)
						currentPos = i;
					else
						CurrentPosition = i;
					break;
				}
			
			ItemChanged ();
			return true;
		}
		
		void ItemChanged ()
		{
			AbstractDockItem oldItem = currentItem;
			
			if (oldItem != null) {
				oldItem.HoverTextChanged -= HandleHoverTextChanged;
				oldItem.PaintNeeded      -= HandlePaintNeeded;
				oldItem.PainterRequest   -= HandlePainterRequest;
			}
			
			if (CurrentPosition < 0)
				CurrentPosition = Provider.Items.Count () - 1;
			else if (CurrentPosition >= Provider.Items.Count ()) 
				CurrentPosition = 0;
			
			currentItem = Provider.Items.ToArray ()[CurrentPosition];
			currentItem.HoverTextChanged += HandleHoverTextChanged;
			currentItem.PaintNeeded      += HandlePaintNeeded;
			currentItem.PainterRequest   += HandlePainterRequest;
			
			if (CurrentItemChanged != null)
				CurrentItemChanged (currentItem, new CurrentItemChangedArgs (currentItem, oldItem));

			QueueRedraw ();
			OnHoverTextChanged ();
		}
		
		public override string UniqueID () {
			// dont proxy, *this* item needs a unique and stable ID
			return "ProxyItem#" + Provider.Name;
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{
			// do nothing, this should never be called
		}
		
		void HandleHoverTextChanged (object o, EventArgs args)
		{
			// just propagate the event up
			OnHoverTextChanged ();
		}
		
		void HandlePaintNeeded (object o, PaintNeededEventArgs args)
		{
			// just propagate the event up
			OnPaintNeeded ();
		}
		
		void HandlePainterRequest (object o, PainterRequestEventArgs args)
		{
			// just propagate the event up
			if (args.Type == ShowHideType.Show)
				ShowPainter (args.Painter);
			else
				HidePainter (args.Painter);
		}
		
		#region Overridden (but not directly proxied) calls
		
		public override void Scrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (direction == ScrollDirection.Up)
				CurrentPosition--;
			else
				CurrentPosition++;
			
			ItemChanged ();
		}
		
		public override Docky.Menus.MenuList GetMenuItems ()
		{
			// add each proxied item as a menu item
			List<Docky.Menus.MenuItem> items = new List<Docky.Menus.MenuItem> ();
			
			foreach (AbstractDockItem adi in Provider.Items)
				items.Add (new Docky.Menus.ProxyMenuItem (adi));
			
			// add the items into the proxy menu group
			MenuList list = currentItem.GetMenuItems ();
			list[MenuListContainer.ProxiedItems].InsertRange (0, items);
			return list;
		}
		
		public override void SetStyle (Gtk.Style style)
		{
			// need to ensure every proxied item gets this update
			foreach (AbstractDockItem adi in Provider.Items)
				adi.SetStyle (style);
		}
		
		public override void SetScreenRegion (Gdk.Screen screen, Gdk.Rectangle region)
		{
			// need to ensure every proxied item gets this update
			foreach (AbstractDockItem adi in Provider.Items)
				adi.SetScreenRegion (screen, region);
		}
		
		#endregion
		
		#region Proxied calls
		
		public override ActivityIndicator Indicator {
			get { return currentItem.Indicator; }
		}
		
		public override ItemState State {
			get { return currentItem.State; }
		}
		
		public override DateTime AddTime {
			get { return currentItem.AddTime; }
		}
		
		public override DateTime LastClick {
			get { return currentItem.LastClick; }
		}
		
		public override MenuButton MenuButton {
			get { return currentItem.MenuButton; }
		}
		
		public override string DropText {
			get { return currentItem.DropText; }
		}
		
		public override ClickAnimation ClickAnimation {
			get { return currentItem.ClickAnimation; }
		}
		
		public override bool ScalableRendering {
			get { return currentItem.ScalableRendering; }
		}
		
		public override bool RotateWithDock {
			get { return currentItem.RotateWithDock; }
		}
		
		public override bool Square {
			get { return currentItem.Square; }
		}
		
		public override bool Zoom {
			get { return currentItem.Zoom; }
		}

		public override double Progress {
			get { return currentItem.Progress; }
		}
		
		public override string HoverText {
			get { return StripMnemonics ? currentItem.HoverText.Replace ("_", "") : currentItem.HoverText; }
		}
		
		public override string ShortName {
			get { return currentItem.ShortName; }
		}
		
		public override string BadgeText {
			get { return currentItem.BadgeText; }
		}
		
		public override int Position {
			get { return currentItem.Position; }
		}
		
		public override int LastPosition {
			get { return currentItem.LastPosition; }
		}
		
		public override AbstractDockItemProvider Owner {
			get { return currentItem.Owner; }
		}
		
		public override Docky.Menus.MenuList RemoteMenuItems {
			get { return currentItem.RemoteMenuItems; }
		}
		
		public override DateTime StateSetTime (ItemState state)
		{
			return currentItem.StateSetTime (state);
		}
		
		public override bool CanAcceptDrop (IEnumerable<string> uris)
		{
			return currentItem.CanAcceptDrop (uris);
		}
		
		public override bool AcceptDrop (IEnumerable<string> uris)
		{
			return currentItem.AcceptDrop (uris);
		}
		
		public override bool CanAcceptDrop (AbstractDockItem item)
		{
			return currentItem.CanAcceptDrop (item);
		}
		
		public override bool AcceptDrop (AbstractDockItem item)
		{
			return currentItem.AcceptDrop (item);
		}
		
		public override void Clicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			currentItem.Clicked (button, mod, xPercent, yPercent);
		}
		
		public override DockySurface IconSurface (DockySurface model, int size, int iconSize, int threeDimHeight)
		{
			return currentItem.IconSurface (model, size, iconSize, threeDimHeight);
		}
		
		#endregion
		
		#region IDisposable implementation
		
		public override void Dispose ()
		{
			Provider.ItemsChanged -= HandleProviderItemsChanged;
			
			if (currentItem != null) {
				currentItem.HoverTextChanged -= HandleHoverTextChanged;
				currentItem.PaintNeeded      -= HandlePaintNeeded;
				currentItem.PainterRequest   -= HandlePainterRequest;
			}
			
			Provider.Dispose ();
			
			base.Dispose ();
		}

		#endregion
	}
}
