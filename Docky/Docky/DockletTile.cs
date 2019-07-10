//  
//  Copyright (C) 2010 Chris Szikszoy, Robert Dyer
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

using Mono.Unix;
using Mono.Addins;

using Docky.Widgets;
using Docky.Items;
using Docky.Services;

namespace Docky
{

	public class DockletTile : AbstractTileObject
	{
		public Addin Addin { get; private set; }
		public AbstractDockItemProvider Provider { get; private set; }
		
		Gtk.Button ConfigButton;
		Gtk.Button UpButton;
		Gtk.Button DownButton;
		Gtk.Button HelpButton;

		public DockletTile (string addinID) : this (addinID, null)
		{
		}
		
		public DockletTile (string addinID, AbstractDockItemProvider provider)
		{
			Addin = PluginManager.AddinFromID (addinID);
			Provider = provider;
			
			SubDescriptionTitle = Catalog.GetString ("Author");
			Enabled = Addin.Enabled;
			
			Name = Addin.Name;
			Description = Addin.Description.Description;
			SubDescriptionText = Addin.Description.Author;
			
			Icon = PluginManager.DefaultPluginIcon;
			
			if (PluginManager.AddinMetadata [Addin].ContainsKey ("icon")) {
				if (PluginManager.AddinMetadata [Addin] ["icon"].Contains ("@"))
					ForcePixbuf = DockServices.Drawing.LoadIcon (PluginManager.AddinMetadata [Addin] ["icon"], 128, -1);
				else
					Icon = string.Format ("{0};;{1}", PluginManager.AddinMetadata [Addin] ["icon"], Icon);
			}
				
			HelpButton = new Gtk.Button ();
			HelpButton.Image = new Gtk.Image (Gtk.Stock.Help, Gtk.IconSize.SmallToolbar);
			HelpButton.Clicked += HandleHelpButtonClicked;
			
			ConfigButton = new Gtk.Button ();
			ConfigButton.Image = new Gtk.Image (Gtk.Stock.Preferences, Gtk.IconSize.SmallToolbar);
			ConfigButton.Clicked += HandleConfigButtonClicked;
			
			UpButton = new Gtk.Button ();
			UpButton.Clicked += HandleUpButtonClicked;
			
			DownButton = new Gtk.Button ();
			DownButton.Clicked += HandleDownButtonClicked;
			
			UpdateInfo ();
			
			if (ConfigurationWindow.Instance.ActiveDock.Preferences.IsVertical) {
				UpButton.Image = new Gtk.Image (Gtk.Stock.GoUp, Gtk.IconSize.SmallToolbar);
				DownButton.Image = new Gtk.Image (Gtk.Stock.GoDown, Gtk.IconSize.SmallToolbar);
				UpButton.TooltipMarkup = Catalog.GetString ("Move this docklet up on the selected dock");
				DownButton.TooltipMarkup = Catalog.GetString ("Move this docklet down on the selected dock");
			} else {
				UpButton.Image = new Gtk.Image (Gtk.Stock.GoBack, Gtk.IconSize.SmallToolbar);
				DownButton.Image = new Gtk.Image (Gtk.Stock.GoForward, Gtk.IconSize.SmallToolbar);
				UpButton.TooltipMarkup = Catalog.GetString ("Move this docklet left on the selected dock");
				DownButton.TooltipMarkup = Catalog.GetString ("Move this docklet right on the selected dock");
			}
			ConfigButton.TooltipMarkup = Catalog.GetString ("Configure this docklet");
			HelpButton.TooltipMarkup = Catalog.GetString ("About this docklet");
			AddButtonTooltip = Catalog.GetString ("Add this docklet to the selected dock");
			RemoveButtonTooltip = Catalog.GetString ("Remove this docklet from the selected dock");
		}
		
		void HandleHelpButtonClicked (object sender, EventArgs e)
		{
			string id = Addin.Id.Substring (0, Addin.Id.IndexOf (","));
			id = id.Substring (id.IndexOf (".") + 1);
			DockServices.System.Open ("http://wiki.go-docky.com/index.php?title=" + id + "_Docklet");
		}
		
		void HandleConfigButtonClicked (object sender, EventArgs e)
		{
			if (PluginManager.ConfigForAddin (Addin.Id) != null)
				PluginManager.ConfigForAddin (Addin.Id).Show ();
		}
		
		void HandleUpButtonClicked (object sender, EventArgs e)
		{
			ConfigurationWindow.Instance.ActiveDock.Preferences.MoveProviderUp (Provider);
			UpdateInfo ();
		}
		
		void HandleDownButtonClicked (object sender, EventArgs e)
		{
			ConfigurationWindow.Instance.ActiveDock.Preferences.MoveProviderDown (Provider);
			UpdateInfo ();
		}
		
		void UpdateInfo ()
		{
			RemoveUserButton (HelpButton);
			
			if (Enabled) {
				if (ConfigurationWindow.Instance.ActiveDock.Preferences.ProviderCanMoveUp (Provider))
					AddUserButton (UpButton);
				else
					RemoveUserButton (UpButton);
				
				if (ConfigurationWindow.Instance.ActiveDock.Preferences.ProviderCanMoveDown (Provider))
					AddUserButton (DownButton);
				else
					RemoveUserButton (DownButton);
			} else {
				RemoveUserButton (UpButton);
				RemoveUserButton (DownButton);
			}
			
			if (Enabled && Addin != null && PluginManager.ConfigForAddin (Addin.Id) != null)
				AddUserButton (ConfigButton);
			else
				RemoveUserButton (ConfigButton);
			
			AddUserButton (HelpButton);
		}
		
		public override void OnActiveChanged ()
		{
			if (ConfigurationWindow.Instance.ActiveDock == null)
				return;
			
			Enabled = !Enabled;
			
			if (Enabled) {
				Provider = PluginManager.Enable (Addin);
				ConfigurationWindow.Instance.ActiveDock.Preferences.AddProvider (Provider);
			} else {
				ConfigurationWindow.Instance.ActiveDock.Preferences.RemoveProvider (Provider);
				Provider = null;
			}
			
			UpdateInfo ();
		}
		
		public override void Dispose ()
		{
			Addin = null;
			Provider = null;
			
			ConfigButton.Clicked -= HandleConfigButtonClicked;
			ConfigButton.Dispose ();
			ConfigButton.Destroy ();
			UpButton.Clicked -= HandleUpButtonClicked;
			UpButton.Dispose ();
			UpButton.Destroy ();
			DownButton.Clicked -= HandleDownButtonClicked;
			DownButton.Dispose ();
			DownButton.Destroy ();
			HelpButton.Clicked -= HandleHelpButtonClicked;
			HelpButton.Dispose ();
			HelpButton.Destroy ();
			
			base.Dispose ();
		}
	}
}
