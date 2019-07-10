/* NotificationHelper.cs
 *
 * GNOME Do is the legal property of its developers. Please refer to the
 * COPYRIGHT file distributed with this source distribution.
 *  
 * This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Gtk;
using Gdk;
using LibNotify = Notifications;
	
namespace Docky.Services
{	
	internal class NotificationService
	{
		enum NotificationCapability {
			actions,
			append,
			body,
			body_hyperlinks,
			body_images,
			body_markup,
			icon_multi,
			icon_static,
			image_svg,
			max,
			positioning, // not an official capability
			scaling,     // not an official capability
			sound
		}
		
		const string DefaultIconName = "docky";
		
		const int StatusIconSize = 24;
		const int NoteIconSize = 48;
		const int LettersPerWord = 7;
		const int MillisecondsPerWord = 350;
		const int MinNotifyShow = 5000;
		const int MaxNotifyShow = 10000;

		static Pixbuf DefaultIcon;
		static StatusIcon statusIcon;
		
		public static void Initialize ()
		{
			DefaultIcon = DockServices.Drawing.LoadIcon (DefaultIconName, NoteIconSize);
			statusIcon = new StatusIcon ();
			statusIcon.Pixbuf = DockServices.Drawing.LoadIcon (DefaultIconName, StatusIconSize);
			statusIcon.Visible = false;	
		}

		static int ReadableDurationForMessage (string title, string message)
		{
			int t = (title.Length + message.Length) / LettersPerWord * MillisecondsPerWord;	
			return Math.Min (Math.Max (t, MinNotifyShow), MaxNotifyShow);
		}

		public static LibNotify.Notification Notify (string title, string message, string icon)
		{	
			try {
				LibNotify.Notification notify = ToNotify (title, message, icon);
				
				// if we aren't using notify-osd, show a status icon
				if (!ServerIsNotifyOSD ()) {
					DockServices.System.RunOnMainThread (() => statusIcon.Visible = true );
					
					notify.Closed += delegate {
						DockServices.System.RunOnMainThread (() => statusIcon.Visible = false );
					};
				}
				
				notify.Show ();
				
				return notify;
			} catch (Exception e) {
				Log<NotificationService>.Warn ("Error showing notification: {0}", e.Message); 
				Log<NotificationService>.Debug (e.StackTrace);
				return null;
			}
		}
		
		static bool SupportsCapability (NotificationCapability capability)
		{
			// positioning and scaling are not actual capabilities, i just know for a fact most other servers
			// support geo. hints, and notify-osd is the only that auto scales images
			if (capability == NotificationCapability.positioning)
				return !ServerIsNotifyOSD ();
			else if (capability == NotificationCapability.scaling)
				return ServerIsNotifyOSD ();
			
			return Array.IndexOf (LibNotify.Global.Capabilities, Enum.GetName (typeof (NotificationCapability), capability)) > -1;
		}

		static LibNotify.Notification ToNotify (string title, string message, string icon)
		{
			LibNotify.Notification notify = new LibNotify.Notification ();
			notify.Body = GLib.Markup.EscapeText (message);
			notify.Summary = title;
			notify.Timeout = ReadableDurationForMessage (title, message);
			
			if (SupportsCapability (NotificationCapability.scaling) && !icon.Contains ("@"))
				notify.IconName = string.IsNullOrEmpty (icon)
					? DefaultIconName
					: icon;
			else
				notify.Icon = string.IsNullOrEmpty (icon)
					? DefaultIcon
					: DockServices.Drawing.LoadIcon (icon, NoteIconSize);

			return notify;
		}
		
		static bool ServerIsNotifyOSD ()
		{
			return LibNotify.Global.ServerInformation.Name == "notify-osd";
		}
	}
}

