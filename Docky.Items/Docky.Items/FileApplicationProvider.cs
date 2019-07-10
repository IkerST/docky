//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Cairo;
using Gdk;
using Mono.Unix;
using Wnck;

using Docky.Menus;
using Docky.Services;
using Docky.Services.Prefs;
using Docky.Services.Applications;
using Docky.Services.Windows;

namespace Docky.Items
{
	public class FileApplicationProvider : AbstractDockItemProvider
	{
		/// <summary>
		/// The window manager.
		/// </summary>
		public static FileApplicationProvider WindowManager;
		static List<FileApplicationProvider> Providers = new List<FileApplicationProvider> ();
		
		static IPreferences prefs = DockServices.Preferences.Get <FileApplicationProvider> ();
		static bool allowPinToDock = prefs.Get<bool> ("AllowPinToDock", true);
		
		internal static IEnumerable<Wnck.Window> ManagedWindows {
			get {
				return Providers
					.SelectMany (p => p.PermanentItems)
					.Where (i => i is WnckDockItem)
					.Cast<WnckDockItem> ()
					.SelectMany (w => w.Windows);
			}
		}
		
		static IEnumerable<Wnck.Window> UnmanagedWindows {
			get {
				IEnumerable<Wnck.Window> managed = ManagedWindows.ToArray ();
				return Wnck.Screen.Default.Windows
					.Where (w => !w.IsSkipTasklist && !managed.Contains (w));
			}
		}
		
		/// <summary>
		/// Occurs when window manager changed.
		/// </summary>
		public event EventHandler WindowManagerChanged;
		
		Dictionary<string, AbstractDockItem> items;
		List<WnckDockItem> transient_items;
		
		/// <summary>
		/// Gets the uris.
		/// </summary>
		/// <value>
		/// The uris.
		/// </value>
		public IEnumerable<string> Uris {
			get { return items.Keys.AsEnumerable (); }
		}
		
		/// <summary>
		/// Gets a value indicating whether this instance is window manager.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is window manager; otherwise, <c>false</c>.
		/// </value>
		public bool IsWindowManager {
			get { return WindowManager == this; }
		}
		
		IEnumerable<AbstractDockItem> PermanentItems {
			get {
				return items.Values.AsEnumerable ();
			}
		}
		
		IEnumerable<AbstractDockItem> InternalItems {
			get {
				return items.Values.Concat (transient_items.Cast<AbstractDockItem> ());
			}
		}
		
		/// <summary>
		/// Gets a value indicating whether this <see cref="Docky.Items.FileApplicationProvider"/> can be automaically disabled.
		/// </summary>
		/// <value>
		/// <c>true</c> if auto disable; otherwise, <c>false</c>.
		/// </value>
		public override bool AutoDisable { get { return false; } }
		
		bool longMatchInProgress = false;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Docky.Items.FileApplicationProvider"/> class.
		/// </summary>
		public FileApplicationProvider ()
		{
			items = new Dictionary<string, AbstractDockItem> ();
			transient_items = new List<WnckDockItem> ();
			
			Providers.Add (this);
			
			// update the transient items when something happens in a desktop file directory
			// It is possible that a .desktop file was created for a window that didn't have one before,
			// this would associate that desktop file with the existing window.
			DockServices.DesktopItems.DesktopFileChanged += HandleWindowMatcherDesktopFileChanged;
			
			Wnck.Screen.Default.WindowOpened += WnckScreenDefaultWindowOpened;
			Wnck.Screen.Default.WindowClosed += WnckScreenDefaultWindowClosed;
			
			if (WnckDockItem.CurrentDesktopOnly) {
				Wnck.Screen.Default.ViewportsChanged += WnckScreenDefaultViewportsChanged;
				Wnck.Screen.Default.ActiveWorkspaceChanged += WnckScreenDefaultActiveWorkspaceChanged;
				Wnck.Screen.Default.ActiveWindowChanged += WnckScreenDefaultActiveWindowChanged;
				if (Wnck.Screen.Default.ActiveWindow != null)
					Wnck.Screen.Default.ActiveWindow.GeometryChanged += HandleActiveWindowGeometryChangedChanged;
			}
		}
		
		#region CurrentDesktopOnly
		
		void WnckScreenDefaultActiveWindowChanged (object o, ActiveWindowChangedArgs args)
		{
			if (WnckDockItem.CurrentDesktopOnly) {
				if (args.PreviousWindow != null)
					args.PreviousWindow.GeometryChanged -= HandleActiveWindowGeometryChangedChanged;
				if (Wnck.Screen.Default.ActiveWindow != null)
					Wnck.Screen.Default.ActiveWindow.GeometryChanged += HandleActiveWindowGeometryChangedChanged;
			}
			UpdateTransientItems ();
		}
		
		void HandleActiveWindowGeometryChangedChanged (object o, EventArgs args)
		{
			UpdateTransientItems ();
		}
		
		void WnckScreenDefaultActiveWorkspaceChanged (object o, ActiveWorkspaceChangedArgs args)
		{
			UpdateTransientItems ();
		}
		
		void WnckScreenDefaultViewportsChanged (object o, EventArgs args)
		{
			UpdateTransientItems ();
		}
		
		#endregion
		
		void HandleWindowMatcherDesktopFileChanged (object sender, DesktopFileChangedEventArgs e)
		{
			UpdateTransientItems ();
		}
		
		void WnckScreenDefaultWindowOpened (object o, WindowOpenedArgs args)
		{
			if (args.Window.IsSkipTasklist)
				return;
			
			longMatchInProgress = !DockServices.WindowMatcher.WindowIsReadyForMatch (args.Window);
			
			// ensure we run last (more or less) so that all icons can update first
			GLib.Timeout.Add (150, delegate {
				if (DockServices.WindowMatcher.WindowIsReadyForMatch (args.Window)) {
					longMatchInProgress = false;
					UpdateTransientItems ();
				} else {
					// handle applications which set their proper (matchable) window title very late,
					// their windows will be monitored for name changes (give up after 5 seconds)
					uint matching_timeout = 5000;
					// wait for OpenOffice/LibreOffice up to 1min to startup before giving up
					if (DockServices.WindowMatcher.WindowIsOpenOffice (args.Window)
					    || DockServices.WindowMatcher.WindowIsLibreOffice (args.Window))
						matching_timeout = 60000;
					args.Window.NameChanged += HandleUnmatchedWindowNameChanged;
					GLib.Timeout.Add (matching_timeout, delegate {
						if (!DockServices.WindowMatcher.WindowIsReadyForMatch (args.Window)) {
							args.Window.NameChanged -= HandleUnmatchedWindowNameChanged;
							longMatchInProgress = false;
							UpdateTransientItems ();
						}
						return false;
					});
				}
				return false;
			});
		}

		void HandleUnmatchedWindowNameChanged (object sender, EventArgs e)
		{
			Wnck.Window window = (sender as Wnck.Window);
			if (DockServices.WindowMatcher.WindowIsReadyForMatch (window)) {
				window.NameChanged -= HandleUnmatchedWindowNameChanged;
				longMatchInProgress = false;
				UpdateTransientItems ();
			}
		}

		void WnckScreenDefaultWindowClosed (object o, WindowClosedArgs args)
		{
			if (args.Window.IsSkipTasklist)
				return;
			
			// we dont need to delay in this case as icons owning extra windows
			// is a non-event
			UpdateTransientItems ();
		}
		
		/// <summary>
		/// Updates the transient items.
		/// </summary>
		public void UpdateTransientItems ()
		{
			// if we are not a window-manager-provider then remove transient items
			if (!IsWindowManager) {
				RemoveTransientItems (transient_items.ToList ());
				return;
			}

			if (longMatchInProgress)
				return;

			// handle unmanaged windows
			foreach (Wnck.Window window in UnmanagedWindows) {
				if (transient_items.Where (adi => adi is WnckDockItem)
					.Cast<WnckDockItem> ()
					.SelectMany (wdi => wdi.Windows)
					.Contains (window))
					continue;
				
				DesktopItem desktopItem = DockServices.WindowMatcher.DesktopItemForWindow (window);
				WnckDockItem item;
				
				if (desktopItem != null) {
					// This fixes WindowMatching for OpenOffice which is a bit slow setting up its window title
					// Check if an existing ApplicationDockItem already uses this DesktopItem
					ApplicationDockItem appdi = InternalItems
						.Where (adi => (adi is ApplicationDockItem && (adi as ApplicationDockItem).OwnedItem == desktopItem))
						.Cast<ApplicationDockItem> ()
						.FirstOrDefault ();
					
					// Try again to gain this missing window
					if (appdi != null) {
						appdi.RecollectWindows ();
						continue;
					}
					
					item = new ApplicationDockItem (desktopItem);
				} else {
					item = new WindowDockItem (window);
				}
				
				transient_items.Add (item);
				item.WindowsChanged += HandleTransientWindowsChanged;
			}
			
			// remove old transient items
			List<WnckDockItem> removed_transient_items = new List<WnckDockItem> ();
			foreach (WnckDockItem wdi in transient_items.Where (adi => adi is WnckDockItem).Cast<WnckDockItem> ()) {
				foreach (Wnck.Window window in ManagedWindows)
					if (wdi.Windows.Contains (window)) {
						removed_transient_items.Add (wdi);
						continue;
					}
				if (!wdi.ManagedWindows.Any ())
					removed_transient_items.Add (wdi);
			}
			RemoveTransientItems (removed_transient_items);
		}
		
		void RemoveTransientItems (IEnumerable<WnckDockItem> items)
		{
			foreach (WnckDockItem adi in items) {
				adi.WindowsChanged -= HandleTransientWindowsChanged;
				transient_items.Remove (adi);
			}
			
			Items = InternalItems;
			
			foreach (AbstractDockItem adi in items)
				adi.Dispose();
		}

		void HandleTransientWindowsChanged (object sender, EventArgs e)
		{
			if (!(sender is WnckDockItem))
				return;
			
			WnckDockItem item = sender as WnckDockItem;
			if (!item.ManagedWindows.Any ())
				RemoveTransientItems (item.AsSingle ());
		}
		
		/// <summary>
		/// Raises the can accept drop event.
		/// </summary>
		/// <param name='uri'>
		/// If set to <c>true</c> URI.
		/// </param>
		protected override bool OnCanAcceptDrop (string uri)
		{
			return true;
		}

		/// <summary>
		/// Raises the accept drop event.
		/// </summary>
		/// <param name='uri'>
		/// URI.
		/// </param>
		protected override AbstractDockItem OnAcceptDrop (string uri)
		{
			return Insert (uri);
		}
		
		/// <summary>
		/// Inserts the item.
		/// </summary>
		/// <returns>
		/// Whether or not the item was inserted onto the dock.
		/// </returns>
		/// <param name='uri'>
		/// The URIfor the 
		/// </param>
		public bool InsertItem (string uri)
		{
			return Insert (uri) != null;
		}
		
		AbstractDockItem Insert (string uri)
		{
			if (uri == null)
				throw new ArgumentNullException ("uri");
			
			if (items.ContainsKey (uri))
				return null;
			
			AbstractDockItem item;
			
			try {
				if (uri.EndsWith (".desktop"))
					item = ApplicationDockItem.NewFromUri (uri);
				else
					item = FileDockItem.NewFromUri (uri);
			} catch (Exception e) {
				Log<FileApplicationProvider>.Debug (e.Message);
				Log<FileApplicationProvider>.Debug (e.StackTrace);
				item = null;
			}
			
			if (item == null)
				return null;
			
			items[uri] = item;
			
			
			Items = InternalItems;
			UpdateTransientItems ();
			
			return item;
		}
		
		/// <summary>
		/// Pins an <see cref="Docky.Items.ApplicationDockItem"/> to the dock.
		/// </summary>
		/// <param name='item'>
		/// Item.
		/// </param>
		public void PinToDock (ApplicationDockItem item)
		{
			if (items.ContainsKey (item.OwnedItem.Uri.AbsoluteUri))
				return;
			
			item.WindowsChanged -= HandleTransientWindowsChanged;
			transient_items.Remove (item);
			items.Add (item.OwnedItem.Uri.AbsoluteUri, item);

			OnItemsChanged (null, null);
		}
		
		/// <summary>
		/// Sets this <see cref="Docky.Items.FileApplicationProvider"/> as the window manager.
		/// </summary>
		public void SetWindowManager ()
		{
			if (WindowManager == this)
				return;
			
			if (WindowManager != null)
				WindowManager.UnsetWindowManager ();
			
			WindowManager = this;
			OnWindowManagerChanged ();
		}
		
		/// <summary>
		/// Unsets this <see cref="Docky.Items.FileApplicationProvider"/> as the window manager.
		/// </summary>
		public void UnsetWindowManager ()
		{
			if (WindowManager != this)
				return;
			
			WindowManager = null;
			OnWindowManagerChanged ();
		}
		
		void OnWindowManagerChanged ()
		{
			UpdateTransientItems ();
			if (WindowManagerChanged != null)
				WindowManagerChanged (this, EventArgs.Empty);
		}
		
		#region IDockItemProvider implementation
		public override string Name { get { return "File Application Provider"; } }
		
		public override bool Separated { get { return true; } }
		
		public override bool ItemCanBeRemoved (AbstractDockItem item)
		{
			return items.ContainsValue (item);
		}
		
		public override bool RemoveItem (AbstractDockItem item)
		{
			if (!items.ContainsValue (item))
				return false;
			
			string key = null;
			foreach (KeyValuePair<string, AbstractDockItem> kvp in items)
				if (kvp.Value == item) {
					key = kvp.Key;
					break;
				}
			
			// this should never happen...
			if (key == null)
				return false;
			
			items.Remove (key);
			
			Items = InternalItems;
			
			item.Dispose ();
			
			// this is so if the launcher has open windows and we manage those...
			UpdateTransientItems ();
			return true;
		}
		
		public override MenuList GetMenuItems (AbstractDockItem item)
		{
			MenuList list = base.GetMenuItems (item);
			
			if (item is ApplicationDockItem && !items.ContainsValue (item) && allowPinToDock)
				list[MenuListContainer.Actions].Insert (0, 
					new MenuItem (Catalog.GetString ("_Pin to Dock"), "[monochrome]pin.svg@" + GetType ().Assembly.FullName, (o, a) => PinToDock (item as ApplicationDockItem)));
			
			return list;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			
			DockServices.DesktopItems.DesktopFileChanged -= HandleWindowMatcherDesktopFileChanged;
			
			Wnck.Screen.Default.WindowOpened -= WnckScreenDefaultWindowOpened;
			Wnck.Screen.Default.WindowClosed -= WnckScreenDefaultWindowClosed;
			
			if (WnckDockItem.CurrentDesktopOnly) {
				Wnck.Screen.Default.ViewportsChanged -= WnckScreenDefaultViewportsChanged;
				Wnck.Screen.Default.ActiveWorkspaceChanged -= WnckScreenDefaultActiveWorkspaceChanged;
				Wnck.Screen.Default.ActiveWindowChanged -= WnckScreenDefaultActiveWindowChanged;
				if (Wnck.Screen.Default.ActiveWindow != null)
					Wnck.Screen.Default.ActiveWindow.GeometryChanged -= HandleActiveWindowGeometryChangedChanged;
			}
			
			Providers.Remove (this);
		}
		
		#endregion
	}
}
