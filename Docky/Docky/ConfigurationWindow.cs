//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer
//  Copyright (C) 2010 Chris Szikszoy
//  Copyright (C) 2011 Robert Dyer
//  Copyright (C) 2013 Rico Tzschichholz
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
using System.IO;
using System.Linq;
using System.Text;

using Cairo;
using Gdk;
using GLib;
using Gtk;
using KeyFile;
using Mono.Unix;

using Docky.Interface;
using Docky.Services;
using Docky.Services.Helpers;
using Docky.Widgets;
using Docky.Items;

namespace Docky
{
	enum Pages : uint {
		Docks = 0,
		Docklets,
		Helpers,
		NPages
	}
	
	enum HelperShowStates : uint {
		Usable = 0,
		Enabled,
		Disabled,
		All,
		NStates
	}
	
	enum DockletShowStates : uint {
		All = 0,
		Active,
		Disabled,
		NStates
	}
	
	public class CurtainWindow : Gtk.Window
	{
		readonly uint AnimationTime = 350;
		readonly double CurtainOpacity = 0.9;
		
		DateTime shown_time = DateTime.UtcNow;
		uint timer = 0;
		
		bool curtainDown = true;
		
		public static CurtainWindow Instance { get; protected set; }
		
		static CurtainWindow ()
		{
			Instance = new CurtainWindow ();
		}
		
		public CurtainWindow () : base(Gtk.WindowType.Toplevel)
		{
			AppPaintable = true;
			AcceptFocus = false;
			Decorated = false;
			SkipPagerHint = true;
			SkipTaskbarHint = true;
			Resizable = false;
			CanFocus = false;
			TypeHint = WindowTypeHint.Normal;
			
			Realized += HandleRealized;
			
			this.SetCompositeColormap ();
			Stick ();
			
			// make the window extend off screen in all directions
			// to work around problems with struts
			Move (-50, -50);
			SetSizeRequest (Screen.Width + 100, Screen.Height + 100);
		}
		
		void HandleRealized (object sender, EventArgs e)
		{
			GdkWindow.SetBackPixmap (null, false);
		}
		
		public void CurtainDown ()
		{
			curtainDown = true;
			StartTimer ();
			Show ();
			// this seems needed for Metacity
			Present ();
		}
		
		public void CurtainUp ()
		{
			curtainDown = false;
			StartTimer ();
		}
		
		void StopTimer ()
		{
			if (timer > 0) {
				GLib.Source.Remove (timer);
				timer = 0;
			}
		}
		
		void StartTimer ()
		{
			StopTimer ();
			shown_time = DateTime.UtcNow;
			timer = GLib.Timeout.Add (20, delegate {
				QueueDraw ();
				
				bool finished = (DateTime.UtcNow - shown_time).TotalMilliseconds / AnimationTime > 1;
				if (finished && !curtainDown)
					Hide ();

				if (finished)
					timer = 0;

				return !finished;
			});
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized)
				return true;
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.Save ();
				
				cr.Color = new Cairo.Color (0, 0, 0, 0);
				cr.Operator = Operator.Source;
				cr.Paint ();
				
				cr.Restore ();
				
				int width, height;
				GetSizeRequest (out width, out height);
				
				double opacity = Math.Min (1, (DateTime.UtcNow - shown_time).TotalMilliseconds / AnimationTime);
				if (!curtainDown)
					opacity = 1 - opacity;
				
				cr.Rectangle (0, 0, width, height);
				cr.Color = new Cairo.Color (0, 0, 0, CurtainOpacity * opacity);
				cr.Fill ();

				(cr.Target as IDisposable).Dispose ();
			}
			
			return false;
		}
		
		public override void Dispose ()
		{
			StopTimer ();
			Realized -= HandleRealized;
			base.Dispose ();
		}
	}
	
	public partial class ConfigurationWindow : Gtk.Window
	{
		TileView HelpersTileview, DockletsTileview;
		Widgets.SearchEntry HelperSearch, DockletSearch;
		
		List<HelperTile> helpertiles;
		List<DockletTile> docklettiles;
		
		internal static ConfigurationWindow Instance { get; private set; }
		
		static ConfigurationWindow () {
			Instance = new ConfigurationWindow ();
		}
		
		static Dock activeDock;
		public Dock ActiveDock {
			get { return activeDock; }
			private set {
				if (activeDock == value)
					return;
				
				if (activeDock != null) {
					activeDock.UnsetActiveGlow ();
					activeDock.Preferences.ItemProvidersChanged -= HandleItemProvidersChanged;
				}
				
				if (value != null) {
					value.SetActiveGlow ();
					value.Preferences.ItemProvidersChanged += HandleItemProvidersChanged;
				}
				
				activeDock = value;
				
				SetupConfigAlignment ();
				RefreshDocklets ();
				CheckButtons ();
			}
		}
		
		void HandleItemProvidersChanged (object o, ItemProvidersChangedEventArgs args)
		{
			RefreshDocklets ();
		}
		
		public override void Dispose ()
		{
			ActiveDock = null;
			if (HelpersTileview != null)
				HelpersTileview.Clear ();
			if (DockletsTileview != null)
				DockletsTileview.Clear ();
			
			helpertiles.ForEach (to => to.Dispose ());
			helpertiles.Clear ();
			docklettiles.ForEach (to => to.Dispose ());
			docklettiles.Clear ();
			
			base.Dispose ();
		}
		
		private ConfigurationWindow () : base(Gtk.WindowType.Toplevel)
		{
			this.Build ();
			
			SkipTaskbarHint = true;
			
			int i = 0;
			foreach (string theme in DockServices.Theme.DockThemes.Distinct ()) {
				theme_combo.AppendText (theme);
				if (DockServices.Theme.DockTheme == theme)
					theme_combo.Active = i;
				i++;
			}
			
			if (Docky.Controller.Docks.Count () == 1)
				ActiveDock = Docky.Controller.Docks.First ();

			start_with_computer_checkbutton.Sensitive = DesktopFile.Exists;
			if (start_with_computer_checkbutton.Sensitive)
				start_with_computer_checkbutton.Active = AutoStart;
			
			// setup docklets {
			docklettiles = new List<DockletTile> ();
			DockletSearch = new SearchEntry ();
			DockletSearch.EmptyMessage = Catalog.GetString ("Search Docklets...");
			DockletSearch.InnerEntry.Changed += delegate {
				RefreshDocklets ();
			};
			DockletSearch.Ready = true;
			DockletSearch.Show ();
			hbox1.PackStart (DockletSearch, true, true, 2);
			
			DockletsTileview = new TileView ();
			DockletsTileview.IconSize = 48;
			docklet_scroll.AddWithViewport (DockletsTileview);
			// }
			
			// setup helpers
			if (!UserArgs.DisableDockManager) {
				helpertiles = new List<HelperTile> ();
				HelperSearch = new SearchEntry ();
				HelperSearch.EmptyMessage = Catalog.GetString ("Search Helpers...");
				HelperSearch.InnerEntry.Changed += delegate {
					RefreshHelpers ();
				};
				HelperSearch.Ready = true;
				HelperSearch.Show ();
				hbox5.PackStart (HelperSearch, true, true, 2);
				
				HelpersTileview = new TileView ();
				HelpersTileview.IconSize = 48;
				helper_scroll.AddWithViewport (HelpersTileview);
				
				DockServices.Helpers.HelperInstalled += delegate {
					RefreshHelpers ();
				};
				DockServices.Helpers.HelperUninstalled += delegate {
					RefreshHelpers ();
				};
			}
			
			SetupConfigAlignment();
			
			ShowAll ();
		}
		
		protected override bool OnDeleteEvent (Event evnt)
		{
			Hide ();
			ActiveDock = null;
			return true;
		}

		protected virtual void OnCloseButtonClicked (object sender, System.EventArgs e)
		{
			Hide ();
			ActiveDock = null;
		}
		
		void SetupConfigAlignment ()
		{
			if (config_alignment.Child != null)
				config_alignment.Remove (config_alignment.Child);
			
			if (docklet_alignment.Child != null)
				docklet_alignment.Remove (docklet_alignment.Child);
			
			if (ActiveDock == null) {
				config_alignment.Add (MakeInfoBox ());
				docklet_alignment.Add (MakeInfoBox ());
			} else {
				config_alignment.Add (ActiveDock.PreferencesWidget);
				docklet_alignment.Add (docklet_scroll);
			}
			
			config_alignment.ShowAll ();
			docklet_alignment.ShowAll ();
		}
		
		void SetupHelperAlignment ()
		{
			if (helper_alignment.Child != null)
				helper_alignment.Remove (helper_alignment.Child);
			
			if (helpertiles.Count () == 0)
				helper_alignment.Add (MakeHelperInfoBox ());
			else
				helper_alignment.Add (vbox5);
			
			helper_alignment.ShowAll ();
		}
		
		VBox MakeInfoBox ()
		{
			VBox vbox = new VBox ();
			
			HBox hboxTop = new HBox ();
			HBox hboxBottom = new HBox ();
			Label label1 = new Gtk.Label (Mono.Unix.Catalog.GetString ("Click on any dock to configure."));
			Label label2 = new Gtk.Label (Mono.Unix.Catalog.GetString ("Drag any dock to reposition."));
			
			vbox.Add (hboxTop);
			vbox.Add (label1);
			vbox.Add (label2);
			vbox.Add (hboxBottom);
			
			vbox.SetChildPacking (hboxTop, true, true, 0, PackType.Start);
			vbox.SetChildPacking (label1, false, false, 0, PackType.Start);
			vbox.SetChildPacking (label2, false, false, 0, PackType.Start);
			vbox.SetChildPacking (hboxBottom, true, true, 0, PackType.Start);
			
			return vbox;
		}

		VBox MakeHelperInfoBox ()
		{
			VBox vbox = new VBox ();
			
			HBox hboxTop = new HBox ();
			HBox hboxBottom = new HBox ();
			Label label1 = new Gtk.Label (Mono.Unix.Catalog.GetString ("Helpers require DockManager be installed."));
			Label label2 = new Gtk.Label ();
			label2.Markup = "<a href=\"https://launchpad.net/dockmanager\">https://launchpad.net/dockmanager</a>";
			
			vbox.Add (hboxTop);
			vbox.Add (label1);
			vbox.Add (label2);
			vbox.Add (hboxBottom);
			
			vbox.SetChildPacking (hboxTop, true, true, 0, PackType.Start);
			vbox.SetChildPacking (label1, false, false, 0, PackType.Start);
			vbox.SetChildPacking (label2, false, false, 0, PackType.Start);
			vbox.SetChildPacking (hboxBottom, true, true, 0, PackType.Start);
			
			return vbox;
		}

		protected override void OnShown ()
		{
			CurtainWindow.Instance.CurtainDown ();

			foreach (Dock dock in Docky.Controller.Docks) {
				dock.EnterConfigurationMode ();
				dock.ConfigurationClick += HandleDockConfigurationClick;
			}
			
			if (Docky.Controller.Docks.Count () == 1)
				ActiveDock = Docky.Controller.Docks.First ();
			
			config_notebook.CurrentPage = (int) Pages.Docks;
			
			KeepAbove = true;
			Stick ();
			
			base.OnShown ();
		}

		void HandleDockConfigurationClick (object sender, EventArgs e)
		{
			ActiveDock = sender as Dock;
		}

		protected override void OnHidden ()
		{
			CurtainWindow.Instance.CurtainUp ();
			
			foreach (Dock dock in Docky.Controller.Docks) {
				dock.ConfigurationClick -= HandleDockConfigurationClick;
				dock.LeaveConfigurationMode ();
				dock.UnsetActiveGlow ();
			}
			
			base.OnHidden ();
		}

		protected virtual void OnThemeComboChanged (object sender, System.EventArgs e)
		{
			DockServices.Theme.DockTheme = theme_combo.ActiveText;
			if (Docky.Controller.NumDocks == 1)
				ActiveDock = null;
		}
	
		protected virtual void OnDeleteDockButtonClicked (object sender, System.EventArgs e)
		{
			if (!(Docky.Controller.Docks.Count () > 1))
				return;
			
			if (ActiveDock != null) {
				Gtk.MessageDialog md = new Gtk.MessageDialog (null, 
						  0,
						  Gtk.MessageType.Warning, 
						  Gtk.ButtonsType.None,
						  "<b><big>" + Catalog.GetString ("Delete the currently selected dock?") + "</big></b>");
				md.Icon = DockServices.Drawing.LoadIcon ("docky", 22);
				md.SecondaryText = Catalog.GetString ("If you choose to delete the dock, all settings\n" +
					"for the deleted dock will be permanently lost.");
				md.Modal = true;
				md.KeepAbove = true;
				md.Stick ();
				
				md.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
				md.AddButton (Catalog.GetString ("_Delete Dock"), Gtk.ResponseType.Ok);
				md.DefaultResponse = Gtk.ResponseType.Ok;
			
				if ((ResponseType)md.Run () == Gtk.ResponseType.Ok) {
					ActiveDock.ConfigurationClick -= HandleDockConfigurationClick;
					ActiveDock.LeaveConfigurationMode ();
					Docky.Controller.DeleteDock (ActiveDock);
					if (Docky.Controller.Docks.Count () == 1)
						ActiveDock = Docky.Controller.Docks.First ();
					else
						ActiveDock = null;
				}
				
				md.Destroy ();
			}
		}
		
		protected virtual void OnNewDockButtonClicked (object sender, System.EventArgs e)
		{
			Dock newDock = Docky.Controller.CreateDock ();
			
			if (newDock != null) {
				newDock.ConfigurationClick += HandleDockConfigurationClick;
				newDock.EnterConfigurationMode ();
				ActiveDock = newDock;
			}
		}
		
		void CheckButtons ()
		{
			int spotsAvailable = 0;
			for (int i = 0; i < Screen.Default.NMonitors; i++)
				spotsAvailable += Docky.Controller.PositionsAvailableForDock (i).Count ();
			
			delete_dock_button.Sensitive = (Docky.Controller.Docks.Count () == 1 || ActiveDock == null) ? false : true;
			new_dock_button.Sensitive = (spotsAvailable == 0) ? false : true;
		}

		GLib.File DesktopFile
		{
			get { return FileFactory.NewForPath (System.IO.Path.Combine (AssemblyInfo.InstallData, "applications/docky.desktop")); }
		}

		const string AutoStartKey = "Hidden";
		const string DesktopGroup = "Desktop Entry";
		GKeyFile autostart_keyfile;
		bool AutoStart 
		{
			get {
				if (autostart_keyfile == null) {
					
					GLib.File autostart_file = DockServices.Paths.AutoStartFile;
					
					try {
						autostart_keyfile = new GKeyFile (autostart_file.Path, KeyFile.Flags.None);
						if (autostart_keyfile.HasKey (DesktopGroup, AutoStartKey))
							return !String.Equals (autostart_keyfile.GetString (DesktopGroup, AutoStartKey), "true", StringComparison.OrdinalIgnoreCase);
						
					} catch (GLib.GException loadException) {
						Log<ConfigurationWindow>.Info ("Unable to load existing autostart file: {0}", loadException.Message);
						Log<SystemService>.Error ("Could not open autostart file {0}", autostart_file.Path);
						
						GLib.File desktop_file = DesktopFile;
						
						if (desktop_file.Exists) {
							Log<ConfigurationWindow>.Info ("Writing new autostart file to {0}", autostart_file.Path);
							autostart_keyfile = new GKeyFile (desktop_file.Path, KeyFile.Flags.None);
							try {
								if (!autostart_file.Parent.Exists)
									autostart_file.Parent.MakeDirectoryWithParents (null);
						
								autostart_keyfile.Save (autostart_file.Path);
								return true;
								
							} catch (Exception e) {
								Log<ConfigurationWindow>.Error ("Failed to write initial autostart file: {0}", e.Message);
							}
						}
						return false;
					}
				}
				if (autostart_keyfile.HasKey (DesktopGroup, AutoStartKey))
					return !String.Equals (autostart_keyfile.GetString (DesktopGroup, AutoStartKey), "true", StringComparison.OrdinalIgnoreCase);
				else
					return true;
			}
			set {
				if (autostart_keyfile != null) {
					autostart_keyfile.SetBoolean (DesktopGroup, AutoStartKey, !value);
					try {
						GLib.File autostart_file = DockServices.Paths.AutoStartFile;
						if (!autostart_file.Parent.Exists)
							autostart_file.Parent.MakeDirectoryWithParents (null);

						autostart_keyfile.Save (autostart_file.Path);
					} catch (Exception e) {
						Log<SystemService>.Error ("Failed to update autostart file: {0}", e.Message);
					}
				}
			}
		}
		
		protected virtual void OnStartWithComputerCheckbuttonToggled (object sender, System.EventArgs e)
		{
			if (AutoStart != start_with_computer_checkbutton.Active)
				AutoStart = start_with_computer_checkbutton.Active;
		}

		[GLib.ConnectBefore]
		protected virtual void OnPageSwitch (object o, Gtk.SwitchPageArgs args)
		{
			if (args.PageNum == (int)Pages.Helpers)
				RefreshHelpers ();
			if (args.PageNum == (int)Pages.Docklets)
				RefreshDocklets ();
		}

		protected virtual void OnInstallClicked (object sender, System.EventArgs e)
		{
			GLib.File file = null;
			Gtk.FileChooserDialog script_chooser = new Gtk.FileChooserDialog ("Helpers", this, FileChooserAction.Open, Gtk.Stock.Cancel, ResponseType.Cancel, Catalog.GetString ("_Select"), ResponseType.Ok);
			FileFilter filter = new FileFilter ();
			filter.AddPattern ("*.tar");
			filter.Name = Catalog.GetString (".tar Archives");
			script_chooser.AddFilter (filter);
			
			if ((ResponseType) script_chooser.Run () == ResponseType.Ok)
				file = GLib.FileFactory.NewForPath (script_chooser.Filename);

			script_chooser.Destroy ();
			
			if (file == null)
				return;
			
			DockServices.Helpers.InstallHelper (file.Path);
		}
		
		protected virtual void OnInstallThemeClicked (object sender, System.EventArgs e)
		{
			GLib.File file = null;
			Gtk.FileChooserDialog script_chooser = new Gtk.FileChooserDialog ("Themes", this, FileChooserAction.Open, Gtk.Stock.Cancel, ResponseType.Cancel, Catalog.GetString ("_Select"), ResponseType.Ok);
			FileFilter filter = new FileFilter ();
			filter.AddPattern ("*.tar");
			filter.Name = Catalog.GetString (".tar Archives");
			script_chooser.AddFilter (filter);
			
			if ((ResponseType) script_chooser.Run () == ResponseType.Ok)
				file = GLib.FileFactory.NewForPath (script_chooser.Filename);

			script_chooser.Destroy ();
			
			if (file == null)
				return;
			
			string theme = DockServices.Theme.InstallTheme (file);
			if (theme != null)
				theme_combo.AppendText (theme);
		}

		protected virtual void OnShowHelperChanged (object sender, System.EventArgs e)
		{
			RefreshHelpers ();
		}
		
		protected virtual void OnShowDockletChanged (object sender, System.EventArgs e)
		{
			RefreshDocklets ();
		}
		
		void RefreshHelpers ()
		{
			if (HelpersTileview == null)
				return;
			
			HelpersTileview.Clear ();
			
			List<Helper> helpers = DockServices.Helpers.Helpers;
			
			foreach (HelperTile tileobject in helpertiles.Where (to => !helpers.Contains (to.Helper))) {
				helpertiles.Remove (tileobject);
				tileobject.Dispose ();					
			}
			helpertiles = helpers.Where (helper => !helpertiles.Exists (to => helper == to.Helper))
				.Select (h => new HelperTile (h))
				.Union (helpertiles).ToList ();

			string query = HelperSearch.InnerEntry.Text.ToLower ();
			IEnumerable<HelperTile> showinghelpertiles = helpertiles
				.Where (hto => hto.Name.ToLower ().Contains (query) || hto.Description.ToLower ().Contains (query))
				.OrderBy (hto => hto.Name);
			
			if (helper_show_cmb.Active == (uint) HelperShowStates.Usable)
				showinghelpertiles = showinghelpertiles.Where (hto => hto.Helper.IsAppAvailable);
			else if (helper_show_cmb.Active == (uint) HelperShowStates.Enabled)
				showinghelpertiles = showinghelpertiles.Where (hto => hto.Enabled);
			else if (helper_show_cmb.Active == (uint) HelperShowStates.Disabled)
				showinghelpertiles = showinghelpertiles.Where (hto => !hto.Enabled);
			
			foreach (HelperTile tileobject in showinghelpertiles)
				HelpersTileview.AppendTile (tileobject);
			
			SetupHelperAlignment ();
		}
		
		void RefreshDocklets ()
		{
			if (DockletsTileview == null)
				return;
			
			AbstractDockItemProvider selectedProvider = null;
			DockletTile selectedTile = (DockletTile) DockletsTileview.CurrentTile ();
			if (selectedTile != null)
				selectedProvider = selectedTile.Provider;
			
			DockletsTileview.Clear ();
			docklettiles.ForEach (tile => tile.Dispose ());
			docklettiles.Clear ();
			
			if (ActiveDock == null)
				return;
			
			string query = DockletSearch.InnerEntry.Text.ToLower ();
			// build a list of DockletTiles, starting with the currently active tiles for the active dock,
			// and the available addins
			DockletTile currentTile = null;
			
			foreach (AbstractDockItemProvider provider in ActiveDock.Preferences.ItemProviders) {
				string providerID = PluginManager.AddinIDFromProvider (provider);
				if (string.IsNullOrEmpty (providerID))
					continue;
				
				docklettiles.Add (new DockletTile (providerID, provider));
				if (provider == selectedProvider)
					currentTile = docklettiles.Last ();
			}
			
			docklettiles = docklettiles.Concat (PluginManager.AvailableProviderIDs.Select (id => new DockletTile (id))).ToList ();
			
			if (docklet_show_cmb.Active == (int) DockletShowStates.Active)
				docklettiles = docklettiles.Where (t => t.Enabled).ToList ();
			else if (docklet_show_cmb.Active == (int) DockletShowStates.Disabled)
				docklettiles = docklettiles.Where (t => !t.Enabled).ToList ();
			
			docklettiles = docklettiles.Where (t => t.Description.ToLower ().Contains (query) || t.Name.ToLower ().Contains (query)).ToList ();
			
			foreach (DockletTile docklet in docklettiles)
				DockletsTileview.AppendTile (docklet);
			
			if (currentTile != null)
				DockletsTileview.Select (docklettiles.IndexOf (currentTile));
		}
		
		protected virtual void OnHelpClicked (object sender, System.EventArgs e)
		{
			DockServices.System.Open ("http://wiki.go-docky.com/index.php?title=Settings_dialog");
		}
	}
}
