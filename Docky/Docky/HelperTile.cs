//  
//  Copyright (C) 2009 Chris Szikszoy, Robert Dyer
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
using System.Text.RegularExpressions;

using Mono.Unix;

using Docky.Widgets;
using Docky.Services;
using Docky.Services.Helpers;

namespace Docky
{

	public class HelperTile : AbstractTileObject
	{
		public Helper Helper { get; private set; }
		Gtk.Button HelpButton;
		Gtk.Button UninstallButton;
		
		public HelperTile (Helper helper)
		{
			this.Helper = helper;
			DockServices.Helpers.HelperStatusChanged += HandleHelperStatusChanged;
			
			AddButtonStock = Gtk.Stock.Execute;
			SubDescriptionTitle = Catalog.GetString ("Status");
			
			Name = ((string) Helper.File.Basename).Split ('.')[0];
			Name = Regex.Replace (Name, "_(?<char>.)", " $1");
			Description = Helper.File.Path;
			Icon = "extension";
			
			if (helper.Data != null) {
				if (!string.IsNullOrEmpty (helper.Data.Name))
					Name = helper.Data.Name;
				if (!string.IsNullOrEmpty (helper.Data.Description))
					Description = helper.Data.Description;
				if (helper.Data.Icon != null)
					ForcePixbuf = helper.Data.Icon.Copy ();
			}
			
			if (helper.IsUser) {
				UninstallButton = new Gtk.Button (Catalog.GetString ("Uninstall"));
				UninstallButton.Clicked += HandleUninstallButtonClicked;
				AddUserButton (UninstallButton);
			}

			HelpButton = new Gtk.Button ();
			HelpButton.Image = new Gtk.Image (Gtk.Stock.Help, Gtk.IconSize.SmallToolbar);
			HelpButton.Clicked += HandleHelpButtonClicked;
			AddUserButton (HelpButton);
			
			SetProps ();
			
			HelpButton.TooltipMarkup = Catalog.GetString ("About this helper");
			AddButtonTooltip = Catalog.GetString ("Enable this helper");
			RemoveButtonTooltip = Catalog.GetString ("Disable this helper");
		}
		
		void HandleHelpButtonClicked (object sender, EventArgs e)
		{
			string id = ((string) Helper.File.Basename).Split ('.')[0];
			id = char.ToUpper (id[0]) + id.Substring (1).ToLower ();
			DockServices.System.Open ("http://wiki.go-docky.com/index.php?title=" + id + "_Helper");
		}
		
		void HandleUninstallButtonClicked (object sender, EventArgs e)
		{
			DockServices.Helpers.UninstallHelper (Helper);
		}
		
		void SetProps ()
		{
			SubDescriptionText = Helper.IsRunning ? Catalog.GetString ("Running") : Catalog.GetString ("Stopped");
			Enabled = Helper.Enabled;
		}
		
		public override void OnActiveChanged ()
		{
			Helper.Enabled = !Enabled;
		}
		
		void HandleHelperStatusChanged (object sender, HelperStatusChangedEventArgs e)
		{
			SetProps ();
		}
		
		public override void Dispose ()
		{
			DockServices.Helpers.HelperStatusChanged -= HandleHelperStatusChanged;
			Helper = null;

			HelpButton.Clicked -= HandleHelpButtonClicked;
			HelpButton.Dispose ();
			HelpButton.Destroy ();

			if (UninstallButton != null) {
				UninstallButton.Clicked -= HandleUninstallButtonClicked;
				UninstallButton.Dispose ();
				UninstallButton.Destroy ();
			}
			
			base.Dispose ();
		}		
	}
}
