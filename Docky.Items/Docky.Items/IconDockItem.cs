//  
//  Copyright (C) 2009 Jason Smith, Robert Dyer, Chris Szikszoy
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
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using Gdk;
using Gtk;
using Cairo;
using Mono.Unix;

using Docky.Menus;
using Docky.Services;
using Docky.CairoHelper;

namespace Docky.Items
{
	public abstract class IconDockItem : AbstractDockItem
	{
		public event EventHandler IconUpdated;
		
		string remote_icon;
		string icon;
		public string Icon {
			get { return string.IsNullOrEmpty (remote_icon) ? icon : remote_icon; }
			protected set {
				if (icon == value)
					return;
				icon = value;
				
				// if we set this, clear the forced pixbuf
				ForcePixbuf = null;
				
				if (icon != null)
					using (Gtk.IconInfo info = Gtk.IconTheme.Default.LookupIcon (icon, 48, Gtk.IconLookupFlags.ForceSvg))
						ScalableRendering = info != null && info.Filename != null && info.Filename.EndsWith (".svg");
				
				OnIconUpdated ();
				QueueRedraw ();
			}
		}
		
		Pixbuf forced_pixbuf;
		protected Pixbuf ForcePixbuf {
			get { return forced_pixbuf; }
			set {
				if (forced_pixbuf == value)
					return;
				if (forced_pixbuf != null)
					forced_pixbuf.Dispose ();
				forced_pixbuf = value;
				QueueRedraw ();
			}
		}
		
		List<IconEmblem> Emblems;
		
		protected void SetIconFromGIcon (GLib.Icon gIcon)
		{
			Icon = DockServices.Drawing.IconFromGIcon (gIcon);
		}
		
		protected void SetIconFromPixbuf (Pixbuf pbuf)
		{
			ForcePixbuf = pbuf.Copy ();
		}
		
		public IconDockItem ()
		{
			Emblems = new List<IconEmblem> ();
			Icon = "";
		}
		
		public void AddEmblem (IconEmblem emblem)
		{
			// remove current emblems at this position
			foreach (IconEmblem e in Emblems.Where (e => e.Position == emblem.Position).ToList ())
				RemoveEmblem (e);
			// add the new emblem
			Emblems.Add (emblem);
			emblem.Changed += HandleEmblemChanged;
			QueueRedraw ();
		}

		void HandleEmblemChanged (object sender, EventArgs e)
		{
			QueueRedraw ();
		}
		
		public void RemoveEmblem (IconEmblem emblem)
		{
			if (Emblems.Contains (emblem)) {
				emblem.Changed -= HandleEmblemChanged;
				Emblems.Remove (emblem);
				emblem.Dispose ();
				QueueRedraw ();
			}
		}
		
		public void SetRemoteIcon (string icon)
		{
			remote_icon = icon;
			
			OnIconUpdated ();
			QueueRedraw ();
		}
		
		protected override void PaintIconSurface (DockySurface surface)
		{			
			Gdk.Pixbuf pbuf;
			
			if (ForcePixbuf == null) {
				pbuf = DockServices.Drawing.LoadIcon (Icon, surface.Width, surface.Height);
			} else {
				pbuf = ForcePixbuf.Copy ();
				pbuf = pbuf.ARScale (surface.Width, surface.Height);
			}
			
			pbuf = ProcessPixbuf (pbuf);

			Gdk.CairoHelper.SetSourcePixbuf (surface.Context, 
			                                 pbuf, 
			                                 (surface.Width - pbuf.Width) / 2, 
			                                 (surface.Height - pbuf.Height) / 2);
			surface.Context.Paint ();
			
			// draw the emblems
			foreach (IconEmblem emblem in Emblems)
				using (Pixbuf p = emblem.GetPixbuf (surface.Width, surface.Height)) {
					int x, y;
					switch (emblem.Position) {
					case 1:
						x = surface.Width - p.Width;
						y = 0;
						break;
					case 2:
						x = surface.Width - p.Width;
						y = surface.Height - p.Height;
						break;
					case 3:
						x = 0;
						y = surface.Height - p.Height;
						break;
					default:
						x = y = 0;
						break;
					}
					Gdk.CairoHelper.SetSourcePixbuf (surface.Context, p, x, y);
					surface.Context.Paint ();
				}

			pbuf.Dispose ();
			
			try {
				PostProcessIconSurface (surface);
			} catch (Exception e) {
				Log<IconDockItem>.Error (e.Message);
				Log<IconDockItem>.Debug (e.StackTrace);
			}
		}
		
		protected virtual Gdk.Pixbuf ProcessPixbuf (Gdk.Pixbuf pbuf)
		{
			return pbuf;
		}
		
		protected virtual void PostProcessIconSurface (DockySurface surface)
		{
		}
		
		protected void OnIconUpdated ()
		{
			if (IconUpdated != null)
				IconUpdated (this, EventArgs.Empty);
		}
		
		public override void Dispose ()
		{
			if (Emblems.Any ())
				Emblems.ForEach (emblem => {
					emblem.Changed -= HandleEmblemChanged;
					emblem.Dispose ();
				});
			Emblems.Clear ();
			
			if (forced_pixbuf != null)
				forced_pixbuf.Dispose ();
			forced_pixbuf = null;
			
			base.Dispose ();
		}				
	}
}
