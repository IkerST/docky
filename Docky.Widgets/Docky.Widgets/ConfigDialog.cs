//  
// Copyright (C) 2009 Chris Szikszoy, Robery Dyer
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Linq;
using System.Collections.Generic;

using Gtk;

namespace Docky.Widgets
{
	public class ConfigDialog : Dialog
	{
		public ConfigDialog (string windowTitle, IEnumerable<Widget> widgets) : this (windowTitle, widgets, 350, 400)
		{
		}
		                     
		public ConfigDialog (string windowTitle, IEnumerable<Widget> widgets, int width, int height)
		{
			SkipTaskbarHint = true;
			TypeHint = Gdk.WindowTypeHint.Dialog;
			WindowPosition = WindowPosition.Center;
			KeepAbove = true;
			Stick ();
			
			IconName = Stock.Preferences;
			Title = windowTitle;
			
			if (widgets.Any ())
				AddWidgets (widgets);
			
			AddButton (Gtk.Stock.Close, Gtk.ResponseType.Close);
			
			SetDefaultSize (width, height);
		}
		
		public void AddWidgets (IEnumerable<Widget> widgets)
		{
			if (widgets.Count () > 1) {				
				Notebook notebook = new Notebook ();
				
				foreach (Widget widget in widgets) {
					Gtk.Alignment spacer = new Gtk.Alignment (0,0,1,1);
					spacer.LeftPadding = spacer.RightPadding = spacer.TopPadding = spacer.BottomPadding = 7;
					spacer.Child = widget;
					notebook.AppendPage (spacer, new Label (widget.Name));
				}
				
				VBox.PackStart (notebook);
			} else {
				VBox.PackStart (widgets.First ());
			}
			
			VBox.ShowAll ();
		}
		
		protected override void OnClose ()
		{
			Hide ();
		}
		
		protected override void OnResponse (ResponseType response_id)
		{
			Hide ();
		}
	}
}
