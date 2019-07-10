//  
//  Copyright (C) 2009-2010 Jason Smith, Robert Dyer, Chris Szikszoy
//  Copyright (C) 2011 Robert Dyer
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

using Gtk;

using Docky.Items;
using Docky.Services;
using Docky.Services.Prefs;
using Mono.Addins;

namespace Docky.Interface
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class DockPreferences : Gtk.Bin, IDockPreferences
	{
		static T Clamp<T> (T value, T max, T min)
		where T : IComparable<T>
		{
			T result = value;
			if (value.CompareTo (max) > 0)
				result = max;
			if (value.CompareTo (min) < 0)
				result = min;
			return result;
		}

		IPreferences prefs;
		string name;
		List<AbstractDockItemProvider> item_providers;
				
		public event EventHandler PositionChanged;
		public event EventHandler PanelModeChanged;
		public event EventHandler IconSizeChanged;
		public event EventHandler AutohideChanged;
		public event EventHandler FadeOnHideChanged;
		public event EventHandler FadeOpacityChanged;
		public event EventHandler IndicatorSettingChanged;
		public event EventHandler ThreeDimensionalChanged;
		public event EventHandler ZoomEnabledChanged;
		public event EventHandler ZoomPercentChanged;
		
		public event EventHandler<ItemProvidersChangedEventArgs> ItemProvidersChanged;
		
		public FileApplicationProvider DefaultProvider { get; set; }
		
		#region Public Properties
		public IEnumerable<string> SortList {
			get { return GetOption<string[]> ("SortList", new string[0]); }
			set { SetOption<string[]> ("SortList", value.ToArray ()); }
		}
		
		public IEnumerable<AbstractDockItemProvider> ItemProviders { 
			get { return item_providers.AsEnumerable (); }
		}
		
		AutohideType hide_type;
		public AutohideType Autohide {
			get { return hide_type; }
			set {
				if (hide_type == value)
					return;
				hide_type = value;
				SetOption<string> ("Autohide", hide_type.ToString ());
				OnAutohideChanged ();
			}
		}
		
		bool? panel_mode;
		public bool PanelMode {
			get {
				if (!panel_mode.HasValue)
					panel_mode = GetOption<bool> ("PanelMode", false);
				return panel_mode.Value;
			}
			set {
				if (panel_mode == value)
					return;
				panel_mode = value;
				SetOption<bool> ("PanelMode", panel_mode.Value);
				OnPanelModeChanged ();
			}
		}
		
		bool? fade_on_hide;
		public bool FadeOnHide {
			get {
				if (!fade_on_hide.HasValue)
					fade_on_hide = GetOption<bool> ("FadeOnHide", false);
				return fade_on_hide.Value; 
			}
			set {
				if (fade_on_hide == value)
					return;
				fade_on_hide = value;
				SetOption<bool> ("FadeOnHide", fade_on_hide.Value);
				OnFadeOnHideChanged ();
			}
		}
		
		double? fade_opacity;
		public double FadeOpacity {
			get {
				if (!fade_opacity.HasValue)
					fade_opacity = GetOption<double> ("FadeOpacity", 0);
				return fade_opacity.Value;
			}
			set {
				if (fade_opacity == value)
					return;
				fade_opacity = value;
				SetOption<double> ("FadeOpacity", fade_opacity.Value);
				OnFadeOpacityChanged ();
			}
		}
		
		public bool IsVertical {
			get { return Position == DockPosition.Left || Position == DockPosition.Right; }
		}
		
		DockPosition position;
		public DockPosition Position {
			get { return position; }
			set {
				if (position == value)
					return;
				position = value;
				SetOption<string> ("Position", position.ToString ());
				OnPositionChanged ();
				threedee_check.Sensitive = Position == DockPosition.Bottom && Gdk.Screen.Default.IsComposited;
			}
		}
		
		int? hot_area_padding;
		public int HotAreaPadding {
			get {
				if (!hot_area_padding.HasValue)
					hot_area_padding = Clamp (GetOption<int> ("HotAreaPadding", 24), 100, 0);
				return hot_area_padding.Value;
			}
			set {
				value = Clamp (value, 100, 0);
				if (hot_area_padding == value)
					return;
				hot_area_padding = value;
				SetOption<int?> ("HotAreaPadding", value);
				OnIconSizeChanged ();
			}
		}
		
		int? icon_size;
		public int IconSize {
			get {
				if (!icon_size.HasValue)
					icon_size = Clamp (GetOption<int> ("IconSize", 48), 128, 24);
				return icon_size.Value;
			}
			set {
				value = Clamp (value, 128, 24);
				if (icon_size == value)
					return;
				icon_size = value;
				icon_scale.Value = IconSize;
				SetOption<int?> ("IconSize", icon_size.Value);
				OnIconSizeChanged ();
			}
		}
		
		bool? indicate_multiple_windows;
		public bool IndicateMultipleWindows {
			get {
				if (!indicate_multiple_windows.HasValue)
					indicate_multiple_windows = GetOption<bool?> ("IndicateMultipleWindows", true);
				return indicate_multiple_windows.Value;
			}
			set {
				if (indicate_multiple_windows == value)
					return;
				indicate_multiple_windows = value;
				SetOption<bool?> ("IndicateMultipleWindows", indicate_multiple_windows.Value);
				OnIndicatorSettingChanged ();
			}
		}
		
		bool? three_dimensional;
		public bool ThreeDimensional {
			get {
				if (!three_dimensional.HasValue)
					three_dimensional = GetOption<bool?> ("ThreeDimensional", false);
				return three_dimensional.Value;
			}
			set {
				if (three_dimensional == value)
					return;
				three_dimensional = value;
				SetOption<bool?> ("ThreeDimensional", three_dimensional.Value);
				OnThreeDimensionalChanged ();
			}
		}
		
		bool? zoom_enabled;
		public bool ZoomEnabled {
			get {
				if (!zoom_enabled.HasValue)
					zoom_enabled = GetOption<bool?> ("ZoomEnabled", true);
				return zoom_enabled.Value; 
			}
			set {
				if (zoom_enabled == value)
					return;
				zoom_enabled = value;
				SetOption<bool?> ("ZoomEnabled", zoom_enabled.Value);
				OnZoomEnabledChanged ();
			}
		}
		
		double? zoom_percent;
		public double ZoomPercent {
			get {
				if (!zoom_percent.HasValue)
					zoom_percent = Clamp (GetOption<double> ("ZoomPercent", 2.0), 4, 1);
				return zoom_percent.Value;
			}
			set {
				value = Clamp (value, 4, 1);
				if (zoom_percent == value)
					return;
				
				zoom_percent = value;
				SetOption<double?> ("ZoomPercent", zoom_percent.Value);
				OnZoomPercentChanged ();
			}
		}
		
		int? monitor_number;
		public int MonitorNumber {
			get {
				if (!monitor_number.HasValue)
					monitor_number = GetOption<int?> ("MonitorNumber", 0);
				if (monitor_number.Value >= Gdk.Screen.Default.NMonitors)
					monitor_number = 0;
				return monitor_number.Value;
			}
			set {
				if (monitor_number == value)
					return;
				monitor_number = value;
				SetOption<int?> ("MonitorNumber", monitor_number.Value);
				OnPositionChanged ();
			}
		}
		#endregion
		
		bool? window_manager;
		public bool WindowManager {
			get {
				if (!window_manager.HasValue)
					window_manager = GetOption<bool?> ("WindowManager", false);
				return window_manager.Value; 
			}
			set {
				if (value == window_manager)
					return;
				
				window_manager = value;
				SetOption<bool?> ("WindowManager", window_manager);
			}
		}
		
		IEnumerable<string> Launchers {
			get {
				return GetOption<string[]> ("Launchers", new string[0]).AsEnumerable ();
			}
			set {
				SetOption<string[]> ("Launchers", value.ToArray ());
			}
		}
		
		IEnumerable<string> Plugins {
			get {
				return GetOption<string[]> ("Plugins", new string[0]).AsEnumerable ();
			}
			set {
				SetOption<string[]> ("Plugins", value.ToArray ());
			}
		}
		
		bool FirstRun {
			get { return prefs.Get<bool> ("FirstRun", true); }
			set { prefs.Set<bool> ("FirstRun", value); }
		}
		
		public void ResetPreferences ()
		{
			SetOption<string> ("Autohide", "None");
			SetOption<bool> ("FadeOnHide", false);
			SetOption<double> ("FadeOpacity", 0);
			SetOption<int?> ("HotAreaPadding", 24);
			SetOption<int?> ("IconSize", 48);
			SetOption<bool?> ("IndicateMultipleWindows", true);
			SetOption<string[]> ("Launchers", new string[0]);
			SetOption<int?> ("MonitorNumber", 0);
			SetOption<string[]> ("Plugins", new string[0]);
			SetOption<string[]> ("SortList", new string[0]);
			SetOption<bool?> ("ThreeDimensional", false);
			SetOption<bool?> ("WindowManager", false);
			SetOption<bool?> ("ZoomEnabled", true);
			SetOption<double?> ("ZoomPercent", 2.0);
		}
		
		public DockPreferences (string dockName, int monitorNumber) : this(dockName)
		{
			MonitorNumber = monitorNumber;
		}
		
		public DockPreferences (string dockName)
		{
			prefs = DockServices.Preferences.Get<DockPreferences> ();
			
			// ensures position actually gets set
			position = (DockPosition) 100;
			
			this.Build ();
			
			// Manually set the tooltips <shakes fist at MD...>
			window_manager_check.TooltipMarkup = Mono.Unix.Catalog.GetString (
			    "When set, windows which do not already have launchers on a dock will be added to this dock.");
			
			icon_scale.Adjustment.SetBounds (24, 129, 1, 12, 1);
			zoom_scale.Adjustment.SetBounds (1, 4.01, .01, .1, .01);
			
			zoom_scale.FormatValue += delegate(object o, FormatValueArgs args) {
				args.RetVal = string.Format ("{0:#}%", args.Value * 100);
			};
			
			name = dockName;
			
			BuildItemProviders ();
			BuildOptions ();
			
			Gdk.Screen.Default.CompositedChanged += HandleCompositedChanged;
			icon_scale.ValueChanged += IconScaleValueChanged;
			zoom_scale.ValueChanged += ZoomScaleValueChanged;
			zoom_checkbutton.Toggled += ZoomCheckbuttonToggled;
			autohide_box.Changed += AutohideBoxChanged;
			fade_on_hide_check.Toggled += FadeOnHideToggled;
			
			DefaultProvider.ItemsChanged += HandleDefaultProviderItemsChanged;
			
			if (FirstRun)
				FirstRun = false;
			
			ShowAll ();
		}

		void HandleCompositedChanged (object o, EventArgs args) {
			// disable zoom
			if (!Gdk.Screen.Default.IsComposited)
				zoom_enabled = false;
			else
				zoom_enabled = GetOption<bool?> ("ZoomEnabled", true);
			
			OnZoomEnabledChanged ();
			
			zoom_checkbutton.Sensitive = !PanelMode && Gdk.Screen.Default.IsComposited;
			zoom_scale.Sensitive = zoom_checkbutton.Sensitive && zoom_checkbutton.Active;
			zoom_checkbutton.Active = ZoomEnabled && Gdk.Screen.Default.IsComposited;
			
			// disable 3d
			if (!Gdk.Screen.Default.IsComposited)
				three_dimensional = false;
			else
				three_dimensional = GetOption<bool?> ("ThreeDimensional", false);
			
			OnThreeDimensionalChanged ();
			
			threedee_check.Sensitive = Position == DockPosition.Bottom && Gdk.Screen.Default.IsComposited;
			threedee_check.Active = ThreeDimensional && Gdk.Screen.Default.IsComposited;
			
			// disable hiding
			if (!Gdk.Screen.Default.IsComposited)
				hide_type = AutohideType.None;
			else
				try {
					hide_type = (AutohideType) Enum.Parse (typeof(AutohideType), 
														  GetOption ("Autohide", AutohideType.None.ToString ()));
				} catch {
					hide_type = AutohideType.None;
				}
			
			OnAutohideChanged ();
			
			autohide_box.Sensitive = Gdk.Screen.Default.IsComposited;
			autohide_box.Active = (int) Autohide;
			fade_on_hide_check.Sensitive = (int) Autohide > 0 && Gdk.Screen.Default.IsComposited;
		}
		
		void HandleDefaultProviderItemsChanged (object sender, ItemsChangedArgs e)
		{
			Launchers = DefaultProvider.Uris;
		}
		
		void AutohideBoxChanged (object sender, EventArgs e)
		{
			Autohide = (AutohideType) autohide_box.Active;
			
			if (autohide_box.Active != (int) Autohide)
				autohide_box.Active = (int) Autohide;

			fade_on_hide_check.Sensitive = (int) Autohide > 0;
		}

		void FadeOnHideToggled (object sender, EventArgs e)
		{
			FadeOnHide = fade_on_hide_check.Active;
		}

		void ZoomCheckbuttonToggled (object sender, EventArgs e)
		{
			ZoomEnabled = zoom_checkbutton.Active;
			
			// may seem odd but its just a check
			zoom_checkbutton.Active = ZoomEnabled;
			zoom_checkbutton.Sensitive = !PanelMode && Gdk.Screen.Default.IsComposited;
			zoom_scale.Sensitive = zoom_checkbutton.Sensitive && zoom_checkbutton.Active;
		}

		void ZoomScaleValueChanged (object sender, EventArgs e)
		{
			ZoomPercent = zoom_scale.Value;
			
			if (ZoomPercent != zoom_scale.Value)
				zoom_scale.Value = ZoomPercent;
		}

		void IconScaleValueChanged (object sender, EventArgs e)
		{
			IconSize = (int) icon_scale.Value;
			
			if (IconSize != icon_scale.Value)
				icon_scale.Value = IconSize;
		}
		
		public bool SetName (string name)
		{
			
			return false;
		}
		
		public string GetName ()
		{
			return name;
		}
		
		public void RemoveProvider (AbstractDockItemProvider provider)
		{
			item_providers.Remove (provider);
			
			PluginManager.Disable (provider);
			provider.Dispose ();
			
			OnItemProvidersChanged (null, provider.AsSingle ());
		}
		
		public void AddProvider (AbstractDockItemProvider provider)
		{
			item_providers.Add (provider);
			provider.IsOnVerticalDock = IsVertical;
			
			OnItemProvidersChanged (provider.AsSingle (), null);
		}
		
		public bool ProviderCanMoveUp (AbstractDockItemProvider provider)
		{
			return provider != item_providers.Where (p => p != DefaultProvider).First ();
		}
		
		public bool ProviderCanMoveDown (AbstractDockItemProvider provider)
		{
			return provider != item_providers.Where (p => p != DefaultProvider).Last ();
		}
		
		public void MoveProviderUp (AbstractDockItemProvider provider)
		{
			int index = item_providers.IndexOf (provider);
			if (index < 1) return;
			
			AbstractDockItemProvider temp = item_providers [index - 1];
			item_providers [index - 1] = provider;
			item_providers [index] = temp;
			
			OnItemProvidersChanged (null, null);
		}
		
		public void MoveProviderDown (AbstractDockItemProvider provider)
		{
			int index = item_providers.IndexOf (provider);
			if (index < 0 || index > item_providers.Count - 2) return;
			
			AbstractDockItemProvider temp = item_providers [index + 1];
			item_providers [index + 1] = provider;
			item_providers [index] = temp;
			
			OnItemProvidersChanged (null, null);
		}
		
		public void SyncPreferences ()
		{
			UpdateSortList ();
		}
		
		void BuildOptions ()
		{
			try {
				Autohide = (AutohideType) Enum.Parse (typeof(AutohideType), 
													  GetOption ("Autohide", AutohideType.None.ToString ()));
			} catch {
				Autohide = AutohideType.None;
			}
			
			try {
				Position = (DockPosition) Enum.Parse (typeof(DockPosition), 
													   GetOption ("Position", DockPosition.Bottom.ToString ()));
			} catch {
				Position = DockPosition.Bottom;
			}
			
			if (WindowManager)
				DefaultProvider.SetWindowManager ();
			
			// on first run, add default plugins to the dock
			if (FirstRun) {
				Log<DockPreferences>.Info ("Adding default plugins.");
				foreach (AbstractDockItemProvider provider in PluginManager.ItemProviders)
					item_providers.Add (provider);
				SyncPlugins ();
			}
			
			autohide_box.Active = (int) Autohide;
			autohide_box.Sensitive = Gdk.Screen.Default.IsComposited;
			UpdateAutohideDescription ();
			fade_on_hide_check.Sensitive = (int) Autohide > 0 && Gdk.Screen.Default.IsComposited;
			
			panel_mode_button.Active = PanelMode;
			zoom_checkbutton.Active = ZoomEnabled && Gdk.Screen.Default.IsComposited;
			zoom_checkbutton.Sensitive = !PanelMode && Gdk.Screen.Default.IsComposited;
			zoom_scale.Value = ZoomPercent;
			zoom_scale.Sensitive = zoom_checkbutton.Sensitive && zoom_checkbutton.Active;
			icon_scale.Value = IconSize;
			fade_on_hide_check.Active = FadeOnHide;
			threedee_check.Active = ThreeDimensional && Gdk.Screen.Default.IsComposited;
			threedee_check.Sensitive = Position == DockPosition.Bottom && Gdk.Screen.Default.IsComposited;
			
			window_manager_check.Active = DefaultProvider.IsWindowManager;
			window_manager_check.Sensitive = !window_manager_check.Active;
			DefaultProvider.WindowManagerChanged += delegate {
				WindowManager = window_manager_check.Active = DefaultProvider.IsWindowManager;
				window_manager_check.Sensitive = !window_manager_check.Active;
			};
			
			if (!Gdk.Screen.Default.IsComposited) {
				zoom_enabled = false;
				three_dimensional = false;
				hide_type = AutohideType.None;
				autohide_box.Active = (int) Autohide;
			}
		}
		
		T GetOption<T> (string key, T def)
		{
			return prefs.Get<T> (name + "/" + key, def);
		}
		
		bool SetOption<T> (string key, T val)
		{
			return prefs.Set<T> (name + "/" + key, val);
		}
		
		bool GnomeBuildLaunchers ()
		{
			IPreferences prefs = DockServices.Preferences.Get ("/desktop/gnome/applications");
			
			// browser
			string browser = prefs.Get<string> ("browser/exec", "");
			// terminal
			string terminal = prefs.Get<string> ("terminal/exec", "");
			// calendar
			string calendar = prefs.Get<string> ("calendar/exec", "");
			// media
			string media = prefs.Get<string> ("media/exec", "");
			
			if (string.IsNullOrEmpty (browser) && string.IsNullOrEmpty (terminal) &&
				string.IsNullOrEmpty (calendar) && string.IsNullOrEmpty (media))
				return false;
			
			Launchers = new[] {
				DockServices.DesktopItems.DesktopItemsFromExec (browser).FirstOrDefault (),
				DockServices.DesktopItems.DesktopItemsFromExec (terminal).FirstOrDefault (),
				DockServices.DesktopItems.DesktopItemsFromExec (calendar).FirstOrDefault (),
				DockServices.DesktopItems.DesktopItemsFromExec (media).FirstOrDefault (),
			}.Where (s => s != null).Select (s => s.Path);
			
			return true;
		}
		
		// this is the fallback for finding launchers
		// try to find files and pick the first one we find
		void DefaultBuildLaunchers ()
		{
			// browser
			string launcher_browser = new[] {
				"file:///usr/share/applications/firefox.desktop",
				"file:///usr/share/applications/chromium-browser.desktop",
				"file:///usr/local/share/applications/google-chrome.desktop",
				"file:///usr/share/applications/epiphany.desktop",
				"file:///usr/share/applications/kde4/konqbrowser.desktop",
			}.Where (s => System.IO.File.Exists (new Uri (s).LocalPath)).FirstOrDefault ();
			
			// terminal
			string launcher_terminal = new[] {
				"file:///usr/share/applications/terminator.desktop",
				"file:///usr/share/applications/gnome-terminal.desktop",
				"file:///usr/share/applications/kde4/konsole.desktop",
			}.Where (s => System.IO.File.Exists (new Uri (s).LocalPath)).FirstOrDefault ();
			
			// music player
			string launcher_music = new[] {
				"file:///usr/share/applications/exaile.desktop",
				"file:///usr/share/applications/songbird.desktop",
				"file:///usr/share/applications/banshee-1.desktop",
				"file:///usr/share/applications/rhythmbox.desktop",
				"file:///usr/share/applications/kde4/amarok.desktop",
			}.Where (s => System.IO.File.Exists (new Uri (s).LocalPath)).FirstOrDefault ();
			
			// IM client
			string launcher_im = new[] {
				"file:///usr/share/applications/pidgin.desktop",
				"file:///usr/share/applications/empathy.desktop",
			}.Where (s => System.IO.File.Exists (new Uri (s).LocalPath)).FirstOrDefault ();
			
			Launchers = new[] {
				launcher_browser,
				launcher_terminal,
				launcher_music,
				launcher_im,
			}.Where (s => !String.IsNullOrEmpty (s));
		}
		
		void BuildItemProviders ()
		{
			if (FirstRun) {
				WindowManager = true;
				
				if (!GnomeBuildLaunchers ())
					DefaultBuildLaunchers ();
			}
			
			item_providers = new List<AbstractDockItemProvider> ();
			
			DefaultProvider = new FileApplicationProvider ();
			item_providers.Add (DefaultProvider);
			
			foreach (string launcher in Launchers)
				DefaultProvider.InsertItem (launcher);
			
			foreach (string pluginId in Plugins) {
				Addin addin = PluginManager.AllAddins.FirstOrDefault (a => a.LocalId == pluginId);
				if (addin == null)
					continue;
				addin.Enabled = true;

				AbstractDockItemProvider provider = PluginManager.ItemProviderFromAddin (addin.Id);
				if (provider != null)
					item_providers.Add (provider);
			}
			
			List<string> sortList = SortList.ToList ();
			foreach (AbstractDockItemProvider provider in item_providers)
				SortProviderOnList (provider, sortList);
			
			UpdateSortList ();
		}
		
		void SortProviderOnList (AbstractDockItemProvider provider, List<string> sortList)
		{
			int defaultRes = 1000;
			Func<AbstractDockItem, int> indexFunc = delegate(AbstractDockItem a) {
				int res = sortList.IndexOf (a.UniqueID ());
				return res >= 0 ? res : defaultRes++;
			};
			
			int i = 0;
			foreach (AbstractDockItem item in provider.Items.OrderBy (indexFunc))
				item.Position = i++;
		}
		
		void UpdateSortList ()
		{
			SortList = item_providers
				.SelectMany (p => p.Items)
				.OrderBy (i => i.Position)
				.Select (i => i.UniqueID ());
		}
		
		void UpdateAutohideDescription ()
		{
			switch (autohide_box.Active) {
			case 0:
				hide_desc.Markup = Mono.Unix.Catalog.GetString ("<i>Never hides; maximized windows do not overlap the dock.</i>");
				break;
			case 1:
				hide_desc.Markup = Mono.Unix.Catalog.GetString ("<i>Hides whenever the mouse is not over it.</i>");
				break;
			case 2:
				hide_desc.Markup = Mono.Unix.Catalog.GetString ("<i>Hides when dock obstructs the active application.</i>");
				break;
			case 3:
				hide_desc.Markup = Mono.Unix.Catalog.GetString ("<i>Hides when dock obstructs any window.</i>");
				break;
			}
		}
		
		void OnAutohideChanged ()
		{
			UpdateAutohideDescription ();
			if (AutohideChanged != null)
				AutohideChanged (this, EventArgs.Empty);
		}
		
		void OnFadeOnHideChanged ()
		{
			if (FadeOnHideChanged != null)
				FadeOnHideChanged (this, EventArgs.Empty);
		}
		
		void OnFadeOpacityChanged ()
		{
			if (FadeOpacityChanged != null)
				FadeOpacityChanged (this, EventArgs.Empty);
		}
		
		void OnPositionChanged ()
		{
			item_providers.ForEach (provider => provider.IsOnVerticalDock = IsVertical);
			
			if (PositionChanged != null)
				PositionChanged (this, EventArgs.Empty);
		}
		
		void OnIconSizeChanged ()
		{
			if (IconSizeChanged != null)
				IconSizeChanged (this, EventArgs.Empty);
		}
		
		void OnIndicatorSettingChanged ()
		{
			if (IndicatorSettingChanged != null)
				IndicatorSettingChanged (this, EventArgs.Empty);
		}
		
		void OnPanelModeChanged ()
		{
			if (PanelModeChanged != null)
				PanelModeChanged (this, EventArgs.Empty);
		}
			
		void OnThreeDimensionalChanged ()
		{
			if (ThreeDimensionalChanged != null)
				ThreeDimensionalChanged (this, EventArgs.Empty);
		}
		
		void OnZoomEnabledChanged ()
		{
			if (ZoomEnabledChanged != null)
				ZoomEnabledChanged (this, EventArgs.Empty);
		}
		
		void OnZoomPercentChanged ()
		{
			if (ZoomPercentChanged != null)
				ZoomPercentChanged (this, EventArgs.Empty);
		}
		
		protected virtual void OnWindowManagerCheckToggled (object sender, System.EventArgs e)
		{
			if (window_manager_check.Active)
				DefaultProvider.SetWindowManager ();
			WindowManager = window_manager_check.Active = DefaultProvider.IsWindowManager;
			window_manager_check.Sensitive = !window_manager_check.Active;
		}
		
		protected virtual void OnPanelModeButtonToggled (object sender, System.EventArgs e)
		{
			PanelMode = panel_mode_button.Active;
			panel_mode_button.Active = PanelMode;
			zoom_checkbutton.Sensitive = !PanelMode && Gdk.Screen.Default.IsComposited;
			zoom_scale.Sensitive = zoom_checkbutton.Sensitive && zoom_checkbutton.Active;
		}
		
		protected virtual void OnThreedeeCheckToggled (object sender, System.EventArgs e)
		{
			ThreeDimensional = threedee_check.Active;
			threedee_check.Active = ThreeDimensional;
		}

		void OnItemProvidersChanged (IEnumerable<AbstractDockItemProvider> addedProviders, IEnumerable<AbstractDockItemProvider> removedProviders)
		{
			SyncPlugins ();
			if (ItemProvidersChanged != null)
				ItemProvidersChanged (this, new ItemProvidersChangedEventArgs (addedProviders, removedProviders));
		}
		
		void SyncPlugins ()
		{
			Plugins = item_providers.Where (p => p != DefaultProvider)
				.Select (p => PluginManager.AddinFromID (PluginManager.AddinIDFromProvider (p)))
				.Select (a => a.LocalId);
		}
		
		public void FreeProviders ()
		{
			OnItemProvidersChanged (null, item_providers);
			
			foreach (AbstractDockItemProvider provider in item_providers.Where (adip => adip != DefaultProvider))
				PluginManager.Disable (provider);
			
			foreach (AbstractDockItemProvider provider in item_providers)
				provider.Dispose ();
			
			item_providers = new List<AbstractDockItemProvider> ();
			FileApplicationProvider.WindowManager.UpdateTransientItems ();
		}
		
		public override void Dispose ()
		{
			Gdk.Screen.Default.CompositedChanged -= HandleCompositedChanged;
			icon_scale.ValueChanged -= IconScaleValueChanged;
			zoom_scale.ValueChanged -= ZoomScaleValueChanged;
			zoom_checkbutton.Toggled -= ZoomCheckbuttonToggled;
			autohide_box.Changed -= AutohideBoxChanged;
			fade_on_hide_check.Toggled -= FadeOnHideToggled;
			DefaultProvider.ItemsChanged -= HandleDefaultProviderItemsChanged;
			
			SyncPlugins ();
			SyncPreferences ();
			UpdateSortList ();
			
			base.Dispose ();
		}
	}
}
