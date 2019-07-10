//  
//  Copyright (C) 2010 Claudio Melis, Rico Tzschichholz, Robert Dyer
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

using Gtk;

using Docky.Items;

namespace SessionManager
{
	public class SessionManagerActionItem : IconDockItem
	{
		System.Action Command { get; set; }

		public override string UniqueID ()
		{
			return "SessionManagerAction" + Icon;
		}

		public SessionManagerActionItem (string icon, string text, System.Action command)
		{
			Icon = icon;
			HoverText = text;
			Command = command;
		}

		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				Command.Invoke ();
				return ClickAnimation.Bounce;
			}

			return ClickAnimation.None;
		}

		public static void ShowConfirmationDialog (string title, string text, string icon_name, System.Action action)
		{
			MessageDialog md = new MessageDialog (null, 0, MessageType.Question, ButtonsType.None, text);
			
			md.Title = title;
			md.Image = Image.NewFromIconName (icon_name, Gtk.IconSize.Dialog);
			md.Image.Visible = true;
			md.Image.Show ();
			
			md.AddButton (Stock.Cancel, ResponseType.Cancel);
			md.AddButton (title, ResponseType.Ok);
			md.DefaultResponse = ResponseType.Ok;
			
			md.Response += (o, args) => { 
				if (args.ResponseId == ResponseType.Ok)
					action.Invoke ();
				md.Destroy ();
			};
			
			md.Show ();
		}
	}
}
