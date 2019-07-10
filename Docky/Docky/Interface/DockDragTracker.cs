//  
//  Copyright (C) 2009-2010 Jason Smith, Robert Dyer, Rico Tzschichholz
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
using System.Text;
using System.Text.RegularExpressions;

using Gdk;
using Gtk;

using Docky.Items;
using Docky.CairoHelper;
using Docky.Services;
using Docky.Services.Prefs;

namespace Docky.Interface
{
	internal class DockDragTracker : IDisposable
	{
		Gdk.Window proxy_window;
		
		static IPreferences prefs = DockServices.Preferences.Get <DockDragTracker> ();
		
		bool drag_known;
		bool drag_data_requested;
		bool drag_is_desktop_file;
		bool repo_mode = false;
		bool drag_disabled = false;
		int marker = 0;
		uint drag_hover_timer;
		IDictionary<AbstractDockItem, int> original_item_pos = new Dictionary<AbstractDockItem, int> ();
		
		AbstractDockItem drag_item;
		
		IEnumerable<string> drag_data;
		
		public DockWindow Owner { get; private set; }
		
		static bool lockDrags = prefs.Get<bool> ("LockDrags", false);
		static bool providersAcceptDrops = prefs.Get<bool> ("ProvidersAcceptDrops", true);

		
		bool externalDragActive;
		public bool ExternalDragActive {
			get { return externalDragActive; }
			private set {
				externalDragActive = value;
				if (!value) {
					drag_known = false;
					drag_data = null;
					drag_data_requested = false;
					drag_is_desktop_file = false;
				}
			} 
		}

		public bool InternalDragActive { get; private set; }

		public bool HoveredAcceptsDrop { get; private set; }
		
		public bool DragDisabled {
			get {
				return lockDrags || drag_disabled;
			}
			set {
				if (lockDrags || drag_disabled == value)
					return;
				drag_disabled = value;
				
				if (drag_disabled) {
					DisableDragTo ();
					DisableDragFrom ();
				} else {
					EnableDragTo ();
					EnableDragFrom ();
				}
			}
		}
		
		public bool RepositionMode {
			get {
				return repo_mode;
			}
			set {
				if (repo_mode == value)
					return;
				repo_mode = value;
				
				if (repo_mode)
					DisableDragTo ();
				else
					EnableDragTo ();
			}
		}
		
		public IEnumerable<string> DragData {
			get { return drag_data; }
		}
		
		public AbstractDockItem DragItem {
			get { return drag_item; }
		}
		
		public DockDragTracker (DockWindow owner)
		{
			Owner = owner;
			RegisterDragEvents ();
			
			EnableDragTo ();
			if (!lockDrags)
				EnableDragFrom ();
			
			Owner.HoveredItemChanged += HandleHoveredItemChanged;
		}
		
		void RegisterDragEvents ()
		{
			Owner.DragMotion += HandleDragMotion;
			Owner.DragBegin += HandleDragBegin;
			Owner.DragDataReceived += HandleDragDataReceived;
			Owner.DragDataGet += HandleDragDataGet;
			Owner.DragDrop += HandleDragDrop;
			Owner.DragEnd += HandleDragEnd;
			Owner.DragLeave += HandleDragLeave;
			Owner.DragFailed += HandleDragFailed;
			
			Owner.MotionNotifyEvent += HandleOwnerMotionNotifyEvent;
		}

		void HandleOwnerMotionNotifyEvent (object o, MotionNotifyEventArgs args)
		{
			ExternalDragActive = false;
		}
		
		void UnregisterDragEvents ()
		{
			Owner.DragMotion -= HandleDragMotion;
			Owner.DragBegin -= HandleDragBegin;
			Owner.DragDataReceived -= HandleDragDataReceived;
			Owner.DragDataGet -= HandleDragDataGet;
			Owner.DragDrop -= HandleDragDrop;
			Owner.DragEnd -= HandleDragEnd;
			Owner.DragLeave -= HandleDragLeave;
			Owner.DragFailed -= HandleDragFailed;
			
			Owner.MotionNotifyEvent -= HandleOwnerMotionNotifyEvent;
		}

		/// <summary>
		/// Emitted on the drag source to fetch drag data
		/// </summary>
		void HandleDragDataGet (object o, DragDataGetArgs args)
		{
			if (InternalDragActive && drag_item != null && !(drag_item is INonPersistedItem)) {
				string uri = string.Format ("docky://{0}\r\n", drag_item.UniqueID ());
				byte[] data = System.Text.Encoding.UTF8.GetBytes (uri);
				args.SelectionData.Set (args.SelectionData.Target, 8, data, data.Length);
			}
		}

		/// <summary>
		/// Emitted on the drag source when drag is started
		/// </summary>
		void HandleDragBegin (object o, DragBeginArgs args)
		{
			Owner.CursorTracker.RequestHighResolution (this);
			InternalDragActive = true;
			Keyboard.Grab (Owner.GdkWindow, true, Gtk.Global.CurrentEventTime);
			drag_canceled = false;
			
			if (proxy_window != null) {
				EnableDragTo ();
				proxy_window = null;
			}
			
			Gdk.Pixbuf pbuf;
			drag_item = Owner.HoveredItem;
			original_item_pos.Clear ();
			
			// If we are in Reposition Mode or over a non-draggable item
			// dont drag it!
			if (drag_item is INonPersistedItem || RepositionMode)
				drag_item = null;
			
			if (drag_item != null) {
				foreach (AbstractDockItem adi in ProviderForItem (drag_item).Items)
					original_item_pos [adi] = adi.Position;
				
				using (DockySurface surface = new DockySurface ((int) (1.2 * Owner.ZoomedIconSize), (int) (1.2 * Owner.ZoomedIconSize))) {
					pbuf = Owner.HoveredItem.IconSurface (surface, (int) (1.2 * Owner.ZoomedIconSize), Owner.IconSize, 0).LoadToPixbuf ();
				}
			} else {
				pbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, 1, 1);
			}
			
			Gtk.Drag.SetIconPixbuf (args.Context, pbuf, pbuf.Width / 2, pbuf.Height / 2);
			pbuf.Dispose ();
			
			// Set up a cursor tracker so we can move the window on the fly
			if (RepositionMode)
				Owner.CursorTracker.CursorPositionChanged += HandleCursorPositionChanged;
		}

		void HandleCursorPositionChanged (object sender, CursorPostionChangedArgs e)
		{
			Gdk.Point cursor = Owner.CursorTracker.Cursor;
			int monitor = Owner.Screen.GetMonitorAtPoint (cursor.X, cursor.Y);
			
			Gdk.Rectangle geo = Owner.Screen.GetMonitorGeometry (monitor);
			
			int activeRegion = Math.Min (geo.Height / 3, geo.Width / 3);
			Gdk.Rectangle left = new Gdk.Rectangle (geo.X, geo.Y + activeRegion, activeRegion, geo.Height - activeRegion * 2);
			Gdk.Rectangle top = new Gdk.Rectangle (geo.X + activeRegion, geo.Y, geo.Width - activeRegion * 2, activeRegion);
			Gdk.Rectangle right = new Gdk.Rectangle (geo.X + geo.Width - activeRegion, geo.Y + activeRegion, activeRegion, geo.Height - activeRegion * 2);
			Gdk.Rectangle bottom = new Gdk.Rectangle (geo.X + activeRegion, geo.Y + geo.Height - activeRegion, geo.Width - activeRegion * 2, activeRegion);
			
			DockPosition target;
			if (top.Contains (cursor))
				target = DockPosition.Top;
			else if (bottom.Contains (cursor))
				target = DockPosition.Bottom;
			else if (left.Contains (cursor))
				target = DockPosition.Left;
			else if (right.Contains (cursor))
				target = DockPosition.Right;
			else
				return;
			
			IDockPreferences prefs = Owner.Preferences;
			if (prefs.Position != target || prefs.MonitorNumber != monitor) {
				Dock d = Docky.Controller.DocksForMonitor (monitor).FirstOrDefault (dock => dock.Preferences.Position == target);
				if (d == null) {
					prefs.MonitorNumber = monitor;
					prefs.Position = target;
				} else {
					d.Preferences.MonitorNumber = prefs.MonitorNumber;
					d.Preferences.Position = prefs.Position;
					prefs.MonitorNumber = monitor;
					prefs.Position = target;
				}
			}
		}

		/// <summary>
		/// Emitted on the drop site. If the data was recieved to preview the data, call
		/// Gdk.Drag.Status (), else call Gdk.Drag.Finish () 
		/// RetVal = true on success
		/// </summary>
		void HandleDragDataReceived (object o, DragDataReceivedArgs args)
		{
			if (drag_data_requested) {
				SelectionData data = args.SelectionData;
				
				string uris = Encoding.UTF8.GetString (data.Data);
				
				drag_data = Regex.Split (uris, "\r\n")
					.Where (uri => uri.StartsWith ("file://"));
				
				drag_data_requested = false;
				drag_is_desktop_file = drag_data.Any (d => d.EndsWith (".desktop"));
				Owner.SetHoveredAcceptsDrop ();
			}
			
			Gdk.Drag.Status (args.Context, DragAction.Copy, Gtk.Global.CurrentEventTime);
			args.RetVal = true;
		}
		
		public bool ItemAcceptsDrop ()
		{
			if (drag_data == null)
				return false;
			
			AbstractDockItem item = Owner.HoveredItem;
			
			if (!drag_is_desktop_file && item != null && item.CanAcceptDrop (drag_data))
				return true;
			
			return false;
		}
		
		public bool ProviderAcceptsDrop ()
		{
			if (drag_data == null || !providersAcceptDrops)
				return false;
			
			foreach (string s in drag_data)
				if (Owner.HoveredProvider != null && Owner.HoveredProvider.CanAcceptDrop (s))
					return true;
				else if (Owner.Preferences.DefaultProvider.CanAcceptDrop (s))
					return true;
			
			// cant accept anything!
			return false;
		}

		/// <summary>
		/// Emitted on the drop site when the user drops data on the widget.
		/// </summary>
		void HandleDragDrop (object o, DragDropArgs args)
		{
			args.RetVal = true;
			Gtk.Drag.Finish (args.Context, true, false, args.Time);
			
			if (drag_data == null)
				return;
			
			AbstractDockItem item = Owner.HoveredItem;
			
			if (ItemAcceptsDrop ()) {
				item.AcceptDrop (drag_data);
			} else if (providersAcceptDrops) {
				AbstractDockItem rightMost = Owner.RightMostItem;
				int newPosition = rightMost != null ? rightMost.Position : 0;
			
				foreach (string s in drag_data) {
					AbstractDockItemProvider provider;
					if (Owner.HoveredProvider != null && Owner.HoveredProvider.CanAcceptDrop (s))
						provider = Owner.HoveredProvider;
					else if (Owner.Preferences.DefaultProvider.CanAcceptDrop (s))
						provider = Owner.Preferences.DefaultProvider;
					else
						// nothing will take it, continue!
						continue;
					
					provider.AcceptDrop (s, newPosition);
					
					if (FileApplicationProvider.WindowManager != null)
						FileApplicationProvider.WindowManager.UpdateTransientItems ();
				}
			}
			
			ExternalDragActive = false;
		}
		
		bool drag_canceled;
		
		/// <summary>
		/// Emitted on the drag source when the drag finishes
		/// </summary>
		void HandleDragEnd (object o, DragEndArgs args)
		{
			if (RepositionMode)
				Owner.CursorTracker.CursorPositionChanged -= HandleCursorPositionChanged;
			
			if (!drag_canceled && drag_item != null) {
				if (!Owner.DockHovered) {
					// Remove from dock
					AbstractDockItemProvider provider = ProviderForItem (drag_item);
					bool poof = false;
					
					if (provider != null && provider.ItemCanBeRemoved (drag_item)) {
						// provider can manually remove
						provider.RemoveItem (drag_item);
						if (FileApplicationProvider.WindowManager != null)
							FileApplicationProvider.WindowManager.UpdateTransientItems ();
						poof = true;
					}
					
					if (poof) {
						PoofWindow window = new PoofWindow (128);
						window.SetCenterPosition (Owner.CursorTracker.Cursor);
						window.Run ();
					}
				} else {
					// Dropped somewhere on dock
					AbstractDockItem item = Owner.HoveredItem;
					if (item != null && item.CanAcceptDrop (drag_item))
						item.AcceptDrop (drag_item);
				}
			}
			
			InternalDragActive = false;
			drag_item = null;
			Keyboard.Ungrab (Gtk.Global.CurrentEventTime);
			
			Owner.AnimatedDraw ();
			Owner.CursorTracker.CancelHighResolution (this);
		}

		/// <summary>
		/// Emitted on drop site when drag leaves widget
		/// </summary>
		void HandleDragLeave (object o, DragLeaveArgs args)
		{
			drag_known = false;
		}
		
		/// <summary>
		/// Emitted on drag source. Return true to disable drag failed animation
		/// </summary>
		void HandleDragFailed (object o, DragFailedArgs args)
		{
			drag_canceled = args.DragResult == DragResult.UserCancelled;
			
			if (drag_canceled) {
				foreach (KeyValuePair<AbstractDockItem, int> kvp in original_item_pos)
					kvp.Key.Position = kvp.Value;
				
				Owner.UpdateCollectionBuffer ();
				Owner.Preferences.SyncPreferences ();
			}
			
			args.RetVal = !drag_canceled;
		}

		/// <summary>
		/// Emitted on drop site.
		/// Set RetVal == cursor is over drop zone
		/// if (RetVal) Gdk.Drag.Status, unless the decision cannot be made, in which case it may be defered by
		/// a get data call
		/// </summary>
		void HandleDragMotion (object o, DragMotionArgs args)
		{
			if (RepositionMode)
				return;
			
			ExternalDragActive = !InternalDragActive;
			Owner.UpdateHoverText ();
			Owner.SetTooltipVisibility ();
			
			if (marker != args.Context.GetHashCode ()) {
				marker = args.Context.GetHashCode ();
				drag_known = false;
			}
			
			// we own the drag if InternalDragActive is true, lets not be silly
			if (!drag_known && !InternalDragActive) {
				drag_known = true;
				Gdk.Atom atom = Gtk.Drag.DestFindTarget (Owner, args.Context, null);
				if (atom != null) {
					Gtk.Drag.GetData (Owner, args.Context, atom, args.Time);
					drag_data_requested = true;
				} else {
					Gdk.Drag.Status (args.Context, DragAction.Private, args.Time);
				}
			} else {
				Gdk.Drag.Status (args.Context, DragAction.Copy, args.Time);
			}
			args.RetVal = true;
		}
		
		Gdk.Window BestProxyWindow ()
		{
			try {
				int pid = System.Diagnostics.Process.GetCurrentProcess ().Id;
				IEnumerable<ulong> xids = Wnck.Screen.Default.WindowsStacked
					.Reverse () // top to bottom order
					.Where (wnk => wnk.IsVisibleOnWorkspace (Wnck.Screen.Default.ActiveWorkspace) && 
							                                 wnk.Pid != pid &&
							                                 wnk.EasyGeometry ().Contains (Owner.CursorTracker.Cursor))
					.Select (wnk => wnk.Xid);
				
				if (!xids.Any ())
					return null;
				
				return Gdk.Window.ForeignNew ((uint) xids.First ());
			} catch {
				return null;
			}
		}
		
		void HandleHoveredItemChanged (object sender, HoveredItemChangedArgs e)
		{
			if (InternalDragActive && DragItemsCanInteract (drag_item, Owner.HoveredItem)) {
				int destPos = Owner.HoveredItem.Position;
				
				// drag right
				if (drag_item.Position < destPos) {
					foreach (AbstractDockItem adi in ProviderForItem (drag_item).Items
								.Where (i => i.Position > drag_item.Position && i.Position <= destPos))
						adi.Position--;
				// drag left
				} else if (drag_item.Position > destPos) {
					foreach (AbstractDockItem adi in ProviderForItem (drag_item).Items
								.Where (i => i.Position < drag_item.Position && i.Position >= destPos))
						adi.Position++;
				}
				drag_item.Position = destPos;
				
				Owner.UpdateCollectionBuffer ();
				Owner.Preferences.SyncPreferences ();
			}
			
			if (drag_hover_timer > 0) {
				GLib.Source.Remove (drag_hover_timer);
				drag_hover_timer = 0;
			}
			
			if (ExternalDragActive && drag_data != null)
				drag_hover_timer = GLib.Timeout.Add (1500, delegate {
					AbstractDockItem item = Owner.HoveredItem;
					if (item != null)
						item.Scrolled (ScrollDirection.Down, Gdk.ModifierType.None);
					return true;
				});
		}
		
		AbstractDockItemProvider ProviderForItem (AbstractDockItem item)
		{
			return Owner.ItemProviders
				.DefaultIfEmpty (null)
				.Where (p => p.Items.Contains (item))
				.FirstOrDefault ();
		}
		
		bool DragItemsCanInteract (AbstractDockItem dragItem, AbstractDockItem hoveredItem)
		{
			return dragItem != hoveredItem &&
				   ProviderForItem (dragItem) == ProviderForItem (hoveredItem) && 
				   ProviderForItem (dragItem) != null;
		}
		
		public void EnsureDragAndDropProxy ()
		{
			// having a proxy window here is VERY bad ju-ju
			if (InternalDragActive)
				return;
			
			if (Owner.DockHovered) {
				if (proxy_window == null)
					return;
				proxy_window = null;
				EnableDragTo ();
			} else if ((Owner.CursorTracker.Modifier & ModifierType.Button1Mask) == ModifierType.Button1Mask) {
				Gdk.Window bestProxy = BestProxyWindow ();
				if (bestProxy != null && proxy_window != bestProxy) {
					proxy_window = bestProxy;
					Gtk.Drag.DestSetProxy (Owner, proxy_window, DragProtocol.Xdnd, true);
				}
			}
		}

		void EnableDragTo ()
		{
			TargetEntry[] dest = new [] {
				new TargetEntry ("text/uri-list", 0, 0),
				new TargetEntry ("text/docky-uri-list", 0, 0),
			};
			Gtk.Drag.DestSet (Owner, 0, dest, Gdk.DragAction.Copy);
		}
		
		void DisableDragTo ()
		{
			Gtk.Drag.DestUnset (Owner);
		}
		
		void EnableDragFrom ()
		{
			// we dont really want to offer the drag to anything, merely pretend to, so we set a mimetype nothing takes
			TargetEntry te = new TargetEntry ("text/docky-uri-list", TargetFlags.App, 0);
			Gtk.Drag.SourceSet (Owner, Gdk.ModifierType.Button1Mask, new[] { te }, DragAction.Private);
		}
		
		void DisableDragFrom ()
		{
			Gtk.Drag.SourceUnset (Owner);
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
			UnregisterDragEvents ();
			Owner.HoveredItemChanged -= HandleHoveredItemChanged;
		}
		#endregion
	}
}
