//  
//  Copyright (C) 2009 Robert Dyer
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

using Gdk;
using Cairo;
using Mono.Unix;

using Docky.CairoHelper;
using Docky.Items;
using Docky.Menus;
using Docky.Services;
using Docky.Widgets;

namespace WeatherDocklet
{
	/// <summary>
	/// Indicates what mode the docklet currently is in.
	/// </summary>
	public enum WeatherDockletStatus
	{
		Initializing,
		Normal,
		Reloading,
		ManualReload,
		Error
	}
	
	/// <summary>
	/// A docklet to display the current temp and condition as an icon and
	/// use a painter to display forecast information.
	/// </summary>
	public class WeatherDocklet : IconDockItem
	{
		public override string UniqueID ()
		{
			return "WeatherDockItem";
		}
		
		/// <value>
		/// Indicates what mode the docklet currently is in.
		/// </value>
		WeatherDockletStatus Status { get; set; }
		
		WeatherPainter painter;
		
		ConfigDialog Config;
		
		/// <summary>
		/// Creates a new weather docklet.
		/// </summary>
		public WeatherDocklet ()
		{
			ScalableRendering = false;
			
			painter = new WeatherPainter ();
			
			Status = WeatherDockletStatus.Initializing;
			
			WeatherController.WeatherReloading += HandleWeatherReloading;
			WeatherController.WeatherError += HandleWeatherError;
			WeatherController.WeatherUpdated += HandleWeatherUpdated;
			
			HoverText = Catalog.GetString ("Click to add a location.");
			Icon = "weather-few-clouds";
		}
		
		/// <summary>
		/// Handles when a weather source reloads data.
		/// </summary>
		void HandleWeatherReloading ()
		{
			DockServices.System.RunOnMainThread (delegate {
				HoverText = Catalog.GetString ("Fetching data...");
				if (Status != WeatherDockletStatus.Initializing && Status != WeatherDockletStatus.ManualReload) {
					Status = WeatherDockletStatus.Reloading;
					State &= ~ItemState.Wait;
				} else {
					State |= ItemState.Wait;
				}
				QueueRedraw ();
			});
		}
		
		/// <summary>
		/// Handles an error with the weather source.
		/// </summary>
		/// <param name="sender">
		/// Ignored
		/// </param>
		/// <param name="e">
		/// A <see cref="WeatherErrorArgs"/> which contains the error message.
		/// </param>
		void HandleWeatherError (object sender, WeatherErrorArgs e)
		{
			DockServices.System.RunOnMainThread (delegate {
				HoverText = e.Error;
				Icon = "weather-few-clouds";
				Status = WeatherDockletStatus.Error;
				State &= ~ItemState.Wait;
				QueueRedraw ();
			});
		}
		
		/// <summary>
		/// Handles when a weather source successfully reloads data.
		/// </summary>
		void HandleWeatherUpdated ()
		{
			DockServices.System.RunOnMainThread (delegate {
				AbstractWeatherSource weather = WeatherController.Weather;
				
				string feelsLike = "";
				if (weather.ShowFeelsLike)
					feelsLike = " (" + weather.FeelsLike + AbstractWeatherSource.TempUnit + ")";
				
				HoverText = weather.Condition + "   " +
					weather.Temp + AbstractWeatherSource.TempUnit + feelsLike +
					"   " + weather.City;
				Icon = WeatherController.Weather.Image;
				Status = WeatherDockletStatus.Normal;
				State &= ~ItemState.Wait;
				
				QueueRedraw ();
				
				if (painter != null) 
					painter.WeatherChanged ();
			});
		}
		
		protected override void PostProcessIconSurface (DockySurface surface)
		{
			if (Status == WeatherDockletStatus.Error)
				return;
			if (Status == WeatherDockletStatus.Initializing)
				return;
			
			int size = Math.Min (surface.Width, surface.Height);
			Context cr = surface.Context;
			
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ())
			{
				layout.FontDescription = new Gtk.Style().FontDescription;
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.None;
				
				Pango.Rectangle inkRect, logicalRect;
				
				layout.Width = Pango.Units.FromPixels (size);
				layout.SetText (WeatherController.Weather.Temp + AbstractWeatherSource.TempUnit);
				if (IsSmall)
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (size / 2.5));
				else
					layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels ((int) (size / 3.5));
				
				layout.GetPixelExtents (out inkRect, out logicalRect);
				cr.MoveTo ((size - inkRect.Width) / 2, size - logicalRect.Height);
				
				Pango.CairoHelper.LayoutPath (cr, layout);
				cr.LineWidth = 2;
				cr.Color = new Cairo.Color (0, 0, 0, 0.8);
				cr.StrokePreserve ();
				
				cr.Color = new Cairo.Color (1, 1, 1, 0.8);
				cr.Fill ();
				
				layout.FontDescription.Dispose ();
				layout.Context.Dispose ();				
			}
		}
		
		protected override Gdk.Pixbuf ProcessPixbuf (Gdk.Pixbuf pbuf)
		{
			if (Status != WeatherDockletStatus.Error && Status != WeatherDockletStatus.Initializing)
				return pbuf;
			
			return pbuf.MonochromePixbuf ();
		}
		
		protected override ClickAnimation OnClicked (uint button, Gdk.ModifierType mod, double xPercent, double yPercent)
		{
			if (button == 1) {
				if (WeatherPreferences.Locations.Length == 0) {
					if (Config == null)
						Config = new WeatherConfigDialog ();
					Config.Show ();
				} else {
					ShowPainter (painter);
				}
			}
			return ClickAnimation.None;
		}
		
		protected override void OnScrolled (Gdk.ScrollDirection direction, Gdk.ModifierType mod)
		{
			if (WeatherPreferences.Locations.Length <= 1)
				return;

			Status = WeatherDockletStatus.ManualReload;
			State |= ItemState.Wait;
			QueueRedraw ();
			
			if (direction == Gdk.ScrollDirection.Up)
				WeatherController.PreviousLocation ();
			else
				WeatherController.NextLocation ();
		}
		
		protected override MenuList OnGetMenuItems ()
		{
			MenuList list = base.OnGetMenuItems ();
			
			if (WeatherController.Weather.Condition != null)
			{
				list[MenuListContainer.Header].Add (new MenuItem (Catalog.GetString ("Radar _Map"),
						WeatherController.Weather.Image, (o, a) => WeatherController.Weather.ShowRadar ()));
			}
			
			
			list.SetContainerTitle (MenuListContainer.Actions, Mono.Unix.Catalog.GetString ("Forecasts"));
			for (int i = 0; i < WeatherController.Weather.ForecastDays; i++)
				if (WeatherController.Weather.Forecasts [i].dow != null)
				{
					list[MenuListContainer.Actions].Add (new ForecastMenuItem (i,
							string.Format ("{0}", WeatherForecast.DayName (WeatherController.Weather.Forecasts [i].dow)), WeatherController.Weather.Forecasts [i].image));
				}
			
			list[MenuListContainer.CustomOne].Add (new MenuItem (Catalog.GetString ("_Settings"), Gtk.Stock.Preferences, 
					delegate {
						if (Config == null)
							Config = new WeatherConfigDialog ();
						Config.Show ();
					}));
			
			if (WeatherController.CurrentLocation != "")
				list[MenuListContainer.CustomOne].Add (new MenuItem (Catalog.GetString ("Check _Weather"), Gtk.Stock.Refresh,
						delegate {
							Status = WeatherDockletStatus.ManualReload;
							State |= ItemState.Wait;
							QueueRedraw ();
							WeatherController.ResetTimer ();
						}));
			
			return list;
		}
		
		public override void Dispose ()
		{			
			WeatherController.WeatherReloading -= HandleWeatherReloading;
			WeatherController.WeatherError -= HandleWeatherError;
			WeatherController.WeatherUpdated -= HandleWeatherUpdated;
			
			base.Dispose ();
		}
	}
}
