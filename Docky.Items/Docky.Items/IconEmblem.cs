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

using Gdk;

using Docky.Services;

namespace Docky.Items
{
	public class IconEmblem
	{
		public event EventHandler Changed;
		
		string icon;
		public string Icon {
			get { return icon; }
			protected set {
				if (icon == value)
					return;
				icon = value;
				// if we set this, clear the forced pixbuf
				ForcePixbuf = null;
				OnChanged ();
			}
		}		
		
		Pixbuf forced_pixbuf;
		public Pixbuf ForcePixbuf {
			get { return forced_pixbuf; }
			set {
				if (forced_pixbuf == value)
					return;
				if (forced_pixbuf != null)
					forced_pixbuf.Dispose ();
				forced_pixbuf = value;
				OnChanged ();
			}
		}
		
		double? percent_of_parent;
		public virtual double PercentOfParent {
			get {
				if (!percent_of_parent.HasValue)
				    percent_of_parent = 0.50;
				return percent_of_parent.Value;
			}
			set {
				if (percent_of_parent.HasValue && percent_of_parent.Value == value)
					return;
				percent_of_parent = value;
				OnChanged ();
			}
		}
		
		// 0 represents top left, continuing clockwise
		public int Position { get; private set; }
		int IconSize { get; set; }

		public IconEmblem (int position, string icon, int size)
		{
			Position = position;
			IconSize = size;
			Icon = icon;
		}
		
		public IconEmblem (int position, Pixbuf icon, int size)
		{
			Position = position;
			IconSize = size;
			ForcePixbuf = icon.Copy ();
		}
		
		public IconEmblem (int position, GLib.Icon icon, int size)
		{
			Position = position;
			IconSize = size;
			Gtk.IconInfo info = Gtk.IconTheme.Default.LookupByGIcon (icon, size, Gtk.IconLookupFlags.GenericFallback);
			if (info == null) {
				Log<IconEmblem>.Warn ("IconInfo lookup failed, using fallback of '{0}'", Gtk.Stock.Cancel);
				Icon = Gtk.Stock.Cancel;
				return;
			}
			ForcePixbuf = info.LoadIcon ();
		}
		
		public Pixbuf GetPixbuf (int parentWidth, int parentHeight) {
			Pixbuf p = ForcePixbuf;
			if (p == null)
				p = DockServices.Drawing.LoadIcon (Icon, IconSize);
			// constrain the icon to PercentOfParent if needed,
			if (p.Width > (int) (parentWidth * PercentOfParent) || p.Height > (int) (parentHeight * PercentOfParent))
				p = p.ARScale ((int) (parentWidth * PercentOfParent), (int) (parentHeight * PercentOfParent));
			return p;
		}
		
		void OnChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public void Dispose ()
		{
			if (forced_pixbuf != null)
				forced_pixbuf.Dispose ();
			forced_pixbuf = null;
		}
	}
}
