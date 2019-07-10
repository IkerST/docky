//  
//  Copyright (C) 2009 Chris Szikszoy
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

using Mono.Unix;

namespace Docky.Widgets
{
	public abstract class AbstractTileObject : IDisposable
	{
		/// <summary>
		/// Triggered when the icon for this tile is updated
		/// </summary>
		public event EventHandler IconUpdated;
		
		/// <summary>
		/// Triggered when any text field for this tile is updated
		/// </summary>
		public event EventHandler TextUpdated;
		
		/// <summary>
		/// Triggered when a button is updated (Show changed, added, or removed)
		/// </summary>
		public event EventHandler ButtonsUpdated;

		void OnIconUpdated ()
		{
			if (IconUpdated != null)
				IconUpdated (this, EventArgs.Empty);
		}
		
		void OnTextUpdated ()
		{
			if (TextUpdated != null)
				TextUpdated (this, EventArgs.Empty);
		}
		
		void OnButtonsUpdated ()
		{
			if (ButtonsUpdated != null)
				ButtonsUpdated (this, EventArgs.Empty);
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Docky.Widgets.AbstractTileObject"/> class.
		/// </summary>
		public AbstractTileObject ()
		{
			ExtraButtons = new List<Gtk.Button> ();
		}

		string icon;
		/// <summary>
		/// Gets or sets the icon for the tile.
		/// </summary>
		/// <value>
		/// The icon.
		/// </value>
		public virtual string Icon {
			get {
				if (icon == null)
					icon = "";
				return icon;
			}
			protected set {
				if (icon == value)
					return;
				// if we set an icon, clear the forced pixbuf
				ForcePixbuf = null;
				icon = value;
				OnIconUpdated ();
			}
		}
		
		int? shift;
		/// <summary>
		/// Gets or sets the hue shift to be applied to the icon.
		/// </summary>
		/// <value>
		/// The hue shift.
		/// </value>
		public virtual int HueShift {
			get {
				if (!shift.HasValue)
				    shift = 0;
				return shift.Value;
			}
			protected set {
				if (shift.HasValue && shift.Value == value)
					return;
				shift = value;
				OnIconUpdated ();
			}
		}
		
		Gdk.Pixbuf force_pbuf;
		/// <summary>
		/// Gets or sets the pixbuf to be used for the icon.
		/// Note: If set this will override the Icon property.
		/// </summary>
		/// <value>
		/// The force pixbuf.
		/// </value>
		public virtual Gdk.Pixbuf ForcePixbuf {
			get {
				return force_pbuf;
			}
			protected set {
				if (force_pbuf == value)
					return;
				if (force_pbuf != null)
					force_pbuf.Dispose ();
				force_pbuf = value;
				OnIconUpdated ();
			}
		}
		
		string desc;
		/// <summary>
		/// Gets or sets the description of this tile.
		/// </summary>
		/// <value>
		/// The description.
		/// </value>
		public virtual string Description {
			get { 
				if (desc == null)
					desc = "";
				return desc;
			}
			protected set {
				if (desc == value)
					return;
				desc = value;
				OnTextUpdated ();
			}
		}		

		string name;
		/// <summary>
		/// Gets or sets the name of this tile.
		/// </summary>
		/// <value>
		/// The name.
		/// </value>
		public virtual string Name {
			get { 
				if (name == null)
					name = "";
				return name;
			}
			protected set {
				if (name == value)
					return;
				name = value;
				OnTextUpdated ();
			}
		}		

		/// <summary>
		/// Raises the active changed event.
		/// </summary>
		public virtual void OnActiveChanged ()
		{
		}
		
		string sub_desc_title;
		/// <summary>
		/// Gets or sets the sub description title.
		/// </summary>
		/// <value>
		/// The sub description title.
		/// </value>
		public virtual string SubDescriptionTitle {
			get { 
				if (sub_desc_title == null)
					sub_desc_title = "";
				return sub_desc_title;
			}
			protected set {
				if (sub_desc_title == value)
					return;
				sub_desc_title = value;
				OnTextUpdated ();
			}
		}
		
		string sub_desc_text;
		/// <summary>
		/// Gets or sets the sub description text.
		/// </summary>
		/// <value>
		/// The sub description text.
		/// </value>
		public virtual string SubDescriptionText {
			get { 
				if (sub_desc_text == null)
					sub_desc_text = "";
				return sub_desc_text;
			}
			protected set {
				if (sub_desc_text == value)
					return;
				sub_desc_text = value;
				OnTextUpdated ();
			}
		}

		bool? show_button;
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Docky.Widgets.AbstractTileObject"/> 
		/// should show the action button.
		/// </summary>
		/// <value>
		/// <c>true</c> if show action button; otherwise, <c>false</c>.
		/// </value>
		public virtual bool ShowActionButton {
			get { 
				if (!show_button.HasValue)
					show_button = true;
				return show_button.Value;
			}
			protected set {
				if (show_button.HasValue && show_button.Value == value)
					return;
				show_button = value;
				OnButtonsUpdated ();
			}
		}
		
		bool? enabled;
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Docky.Widgets.AbstractTileObject"/> is enabled.
		/// </summary>
		/// <value>
		/// <c>true</c> if enabled; otherwise, <c>false</c>.
		/// </value>
		public virtual bool Enabled {
			get {
				if (!enabled.HasValue)
					enabled = false;
				return enabled.Value;
			}
			set {
				if (enabled.HasValue && enabled.Value == value)
					return;
				enabled = value;
			}
		}
		
		string add_button_stock;
		/// <summary>
		/// Gets or sets the add button stock.
		/// </summary>
		/// <value>
		/// The add button stock.
		/// </value>
		public virtual string AddButtonStock {
			get {
				if (add_button_stock == null)
					add_button_stock = Gtk.Stock.Add;
				return add_button_stock;
			}
			protected set {
				if (add_button_stock == value)
					return;
				add_button_stock = value;
			}
		}
		
		string remove_button_stock;
		/// <summary>
		/// Gets or sets the remove button stock.
		/// </summary>
		/// <value>
		/// The remove button stock.
		/// </value>
		public virtual string RemoveButtonStock {
			get {
				if (remove_button_stock == null)
					remove_button_stock = Gtk.Stock.Delete;
				return remove_button_stock;
			}
			protected set {
				if (remove_button_stock == value)
					return;
				remove_button_stock = value;
			}
		}
		
		/// <summary>
		/// Gets or sets the add button tooltip.
		/// </summary>
		/// <value>
		/// The add button tooltip.
		/// </value>
		public string AddButtonTooltip { get; protected set; }
		
		/// <summary>
		/// Gets or sets the remove button tooltip.
		/// </summary>
		/// <value>
		/// The remove button tooltip.
		/// </value>
		public string RemoveButtonTooltip { get; protected set; }
		
		internal List<Gtk.Button> ExtraButtons;
		
		/// <summary>
		/// Adds extra buttons to the tile
		/// </summary>
		/// <param name="button">
		/// A <see cref="Gtk.Button"/>
		/// </param>
		public void AddUserButton (Gtk.Button button) {
			if (!ExtraButtons.Contains (button)) {
				ExtraButtons.Add (button);
				OnButtonsUpdated ();
			}
		}
		
		/// <summary>
		/// Removes extra buttons from the tile
		/// </summary>
		/// <param name="button">
		/// A <see cref="Gtk.Button"/>
		/// </param>
		public void RemoveUserButton (Gtk.Button button) {
			if (ExtraButtons.Contains (button)) {
				ExtraButtons.Remove (button);
				OnButtonsUpdated ();
			}
		}
		
		#region IDisposable implementation
		/// <summary>
		/// Releases all resource used by the <see cref="Docky.Widgets.AbstractTileObject"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="Docky.Widgets.AbstractTileObject"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Docky.Widgets.AbstractTileObject"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="Docky.Widgets.AbstractTileObject"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Docky.Widgets.AbstractTileObject"/> was occupying.
		/// </remarks>
		public virtual void Dispose ()
		{
			if (force_pbuf != null)
				force_pbuf.Dispose ();
			force_pbuf = null;
			
			ExtraButtons.ForEach (button => {
				button.Dispose ();
				button.Destroy ();
			});
			ExtraButtons.Clear ();
		}
		#endregion
	}
}
