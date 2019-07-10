//  
//  Copyright (C) 2009-2010 Jason Smith, Robert Dyer, Rico Tzschichhholz
//  Copyright (C) 2011 Robert Dyer
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
using System.Linq;

using Cairo;
using Gdk;
using Gtk;
using Mono.Unix;
using Wnck;

using Docky.DBus;
using Docky.Items;
using Docky.CairoHelper;
using Docky.Menus;
using Docky.Painters;
using Docky.Services;
using Docky.Services.Xlib;

namespace Docky.Interface
{
	public class DockWindow : Gtk.Window
	{
		struct DrawValue 
		{
			public PointD Center;
			public PointD StaticCenter;
			public Gdk.Rectangle HoverArea;
			public double Zoom;
			
			public DrawValue MoveIn (DockPosition position, double amount)
			{
				DrawValue result = new DrawValue {
					Center = Center,
					StaticCenter = StaticCenter,
					HoverArea = HoverArea,
					Zoom = Zoom
				};
				
				switch (position) {
				case DockPosition.Top:
					result.Center.Y += amount;
					result.StaticCenter.Y += amount;
					break;
				case DockPosition.Left:
					result.Center.X += amount;
					result.StaticCenter.X += amount;
					break;
				case DockPosition.Right:
					result.Center.X -= amount;
					result.StaticCenter.X -= amount;
					break;
				case DockPosition.Bottom:
					result.Center.Y -= amount;
					result.StaticCenter.Y -= amount;
					break;
				}
				
				return result;
			}
			
			public DrawValue MoveRight (DockPosition position, double amount)
			{
				DrawValue result = new DrawValue {
					Center = Center,
					StaticCenter = StaticCenter,
					HoverArea = HoverArea,
					Zoom = Zoom
				};
				
				switch (position) {
				case DockPosition.Top:
					result.Center.X += amount;
					result.StaticCenter.X += amount;
					break;
				case DockPosition.Left:
					result.Center.Y += amount;
					result.StaticCenter.Y += amount;
					break;
				case DockPosition.Right:
					result.Center.Y -= amount;
					result.StaticCenter.Y -= amount;
					break;
				case DockPosition.Bottom:
					result.Center.X -= amount;
					result.StaticCenter.X -= amount;
					break;
				}
				
				return result;
			}
		}
		
		static DateTime UpdateTimeStamp (DateTime lastStamp, TimeSpan animationLength)
		{
			TimeSpan delta = DateTime.UtcNow - lastStamp;
			if (delta < animationLength)
				return DateTime.UtcNow.Subtract (animationLength - delta);
			return DateTime.UtcNow;
		}
		
		static DockyItem DockyItem { get; set; }
		
		/*******************************************
		 * Note to reader:
		 * All values labeled X or width reference x or width as thought of from a horizontally positioned dock.
		 * This is because as the dock rotates, the math is largely unchanged, however there needs to be a consistent
		 * name for these directions regardless of orientation. The catch is that when speaking to cairo, x/y are
		 * normal
		 * *****************************************/
		
		
		
		const int UrgentBounceHeight = 80;
		const int LaunchBounceHeight = 30;
		const int BackgroundWidth = 1000;
		const int BackgroundHeight = 150;
		const int NormalIndicatorSize = 20;
		const int UrgentIndicatorSize = 26;
		const int GlowSize = 30;
		
		const int WaitGroups = 3;
		const int WaitArmsPerGroup = 8;
		
		readonly TimeSpan BaseAnimationTime = new TimeSpan (0, 0, 0, 0, 150);
		readonly TimeSpan PainterAnimationTime = new TimeSpan (0, 0, 0, 0, 350);
		readonly TimeSpan PanelAnimationTime = new TimeSpan (0, 0, 0, 0, 300);
		readonly TimeSpan BounceTime = new TimeSpan (0, 0, 0, 0, 600);
		readonly TimeSpan SlideTime = new TimeSpan (0, 0, 0, 0, 200);
		readonly TimeSpan PulseTime = new TimeSpan (0, 0, 0, 0, 2000);
		
		DateTime hidden_change_time;
		DateTime dock_hovered_change_time;
		DateTime items_change_time;
		DateTime painter_change_time;
		DateTime threedimensional_change_time;
		DateTime panel_change_time;
		DateTime render_time;
		DateTime remove_time;
		
		IDockPreferences preferences;
		DockySurface main_buffer, background_buffer, icon_buffer, painter_buffer;
		DockySurface normal_indicator_buffer, urgent_indicator_buffer, urgent_glow_buffer;
		DockySurface config_hover_buffer, drop_hover_buffer, launch_hover_buffer;
		DockySurface wait_buffer;
		AbstractDockItem next_hoveredItem;
		AbstractDockItem hoveredItem;
		AbstractDockItem lastClickedItem;
		AbstractDockPainter painter;
		AbstractDockItem painterOwner;
		
		Gdk.Rectangle monitor_geo;
		Gdk.Rectangle current_mask_area;
		Gdk.Rectangle painter_area;
		Gdk.Point window_position;
		
		double? zoom_in_buffer;
		bool rendering;
		bool update_screen_regions;
		bool repaint_painter;
		bool active_glow;
		bool config_mode;
		
		/// <summary>
		/// Used as a decimal representation of the "index" of where the old item used to be
		/// </summary>
		double remove_index;
		int remove_size;
		
		uint animation_timer;
		uint icon_size_timer;
		uint size_request_timer;
		
		public event EventHandler<HoveredItemChangedArgs> HoveredItemChanged;
		
		public int Width { get; private set; }
		
		public int Height { get; private set; }
		
		bool ExternalDragActive { get { return DragTracker.ExternalDragActive; } }
		
		bool InternalDragActive { get { return DragTracker.InternalDragActive; } }
		
		bool HoveredAcceptsDrop { get; set; }
		
		internal AutohideManager AutohideManager { get; private set; }
		
		internal CursorTracker CursorTracker { get; private set; }
		
		internal DockDragTracker DragTracker { get; private set; }
		
		internal HoverTextManager TextManager { get; private set; }
		
		AnimationState AnimationState { get; set; }
		
		DockItemMenu Menu { get; set; }
		
		Dictionary<AbstractDockItem, DrawValue> DrawValues { get; set; }
		
		public IDockPreferences Preferences {
			get { return preferences; }
			set {
				if (preferences == value)
					return;
				if (preferences != null)
					UnregisterPreferencesEvents (preferences);
				preferences = value;
				RegisterPreferencesEvents (value);
				
				// Initialize value
				MaxIconSize = Preferences.IconSize;
				UpdateMaxIconSize ();
			}
		}
		
		public bool ActiveGlow {
			get {
				return active_glow;
			}
			set {
				if (active_glow == value)
					return;
				active_glow = value;
				UpdateHoverText ();
				SetTooltipVisibility ();
				AnimatedDraw ();
			}
		}
		
		public bool ConfigurationMode {
			get {
				return config_mode;
			}
			set {
				if (config_mode == value)
					return;
				config_mode = value;
				DragTracker.RepositionMode = config_mode;
				if (value)
					DragTracker.DragDisabled = false;
				AutohideManager.ConfigMode = value;
				UpdateScreenRegions ();
				
				SetTooltipVisibility ();
				if (background_buffer != null) {
					background_buffer.Dispose ();
					background_buffer = null;
				}
				AnimatedDraw ();
			}
		}
		
		List<AbstractDockItem> collection_backend;
		ReadOnlyCollection<AbstractDockItem> collection_frontend;
		
		/// <summary>
		/// Provides a list of all items to be displayed on the dock. Nulls are
		/// inserted where separators should go.
		/// </summary>
		public ReadOnlyCollection<AbstractDockItem> Items {
			get {
				if (collection_backend.Count == 0) {
					UpdateScreenRegions ();
					bool priorItems = false;
					bool separatorNeeded = false;
					
					if (Preferences.DefaultProvider.IsWindowManager && DockyItem.Show) {
						collection_backend.Add (DockyItem);
					
						if (!Preferences.DefaultProvider.Items.Any())
							collection_backend.Add (new SeparatorItem ());
					}
					
					foreach (AbstractDockItemProvider provider in ItemProviders) {
						if (!provider.Items.Any ())
							continue;
						
						if ((provider.Separated && priorItems) || separatorNeeded)
							collection_backend.Add (new SeparatorItem ());
					
						collection_backend.AddRange (provider.Items.OrderBy (i => i.Position));
						priorItems = true;
						
						separatorNeeded = provider.Separated;
					}
					
					for (int i = collection_backend.Count; i < 2; i++)
						collection_backend.Add (new SpacingItem ());
				}
				
				return collection_frontend;
			}
		}
		
		#region Shortcuts
		AutohideType Autohide {
			get { return Preferences.Autohide; }
		}
		
		internal bool DockHovered {
			get { return AutohideManager.DockHovered; }
		}
		
		internal AbstractDockItem HoveredItem {
			get {
				if (!DockHovered)
					return null;
				return hoveredItem;
			}
			private set {
				if (hoveredItem == value)
					return;
				AbstractDockItem last = hoveredItem;
				hoveredItem = value;
				SetHoveredAcceptsDrop ();
				OnHoveredItemChanged (last);
				
				UpdateHoverText ();
				SetTooltipVisibility ();
			}
		}
		
		DockySurface DrawHoverText (DockySurface surface, string text)
		{
			using (Pango.Layout layout = DockServices.Drawing.ThemedPangoLayout ()) {
				layout.FontDescription = Style.FontDescription;
				layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (11);
				layout.FontDescription.Weight = Pango.Weight.Bold;
				layout.Ellipsize = Pango.EllipsizeMode.End;
				layout.Width = Pango.Units.FromPixels (500);
				
				layout.SetText (text);
				
				Pango.Rectangle inkRect, logicalRect;
				layout.GetPixelExtents (out inkRect, out logicalRect);
				
				const int HoverTextHeight = 26;
				int textWidth = inkRect.Width;
				int textHeight = logicalRect.Height;
				int buffer = HoverTextHeight - textHeight;
				surface = new DockySurface (Math.Max (HoverTextHeight, textWidth + buffer), HoverTextHeight, background_buffer);
				
				surface.Context.MoveTo ((surface.Width - textWidth) / 2, buffer / 2);
				Pango.CairoHelper.LayoutPath (surface.Context, layout);
				surface.Context.Color = HoverTextManager.IsLight ? new Cairo.Color (0.1, 0.1, 0.1) : new Cairo.Color (1, 1, 1);
				surface.Context.Fill ();
				
				layout.Context.Dispose ();
			}
			
			return surface;
		}
		
		internal void UpdateHoverText ()
		{
			int top = (int) (IconSize * (ZoomPercent + .2));
			Gdk.Point point = new Gdk.Point (-1, -1);
			DockySurface hover = null;
			
			if (ActiveGlow) {
				if (config_hover_buffer == null)
					config_hover_buffer = DrawHoverText (config_hover_buffer, Catalog.GetString ("Drag to reposition"));
				hover = config_hover_buffer;
				top = IconSize + 2 * DockHeightBuffer;
			} else if (hoveredItem != null && background_buffer != null) {
				if (ExternalDragActive) {
					if (DragTracker.ItemAcceptsDrop ()) {
						if (launch_hover_buffer != null)
							launch_hover_buffer.Dispose ();
						launch_hover_buffer = DrawHoverText (launch_hover_buffer, hoveredItem.DropText);
						hover = launch_hover_buffer;
					} else if (DragTracker.ProviderAcceptsDrop ()) {
						if (drop_hover_buffer == null)
							drop_hover_buffer = DrawHoverText (drop_hover_buffer, Catalog.GetString ("Drop to add to dock"));
						hover = drop_hover_buffer;
					}
				} else {
					hover = hoveredItem.HoverTextSurface (background_buffer, Style, HoverTextManager.IsLight);
					
					DrawValue loc = DrawValues[hoveredItem].MoveIn (Position, IconSize * (ZoomPercent + .1) - IconSize / 2);
					
					point = new Gdk.Point ((int) loc.StaticCenter.X, (int) loc.StaticCenter.Y);
					point.X += window_position.X;
					point.Y += window_position.Y;
				}
			} else if (hoveredItem == null && ExternalDragActive && DragTracker.ProviderAcceptsDrop ()) {
				if (drop_hover_buffer == null)
					drop_hover_buffer = DrawHoverText (drop_hover_buffer, Catalog.GetString ("Drop to add to dock"));
				hover = drop_hover_buffer;
			} else {
				return;
			}
			
			// default centers it on the dock
			if (point.X == -1 && point.Y == -1) {
				int offset = 8;
				switch (Position) {
				default:
				case DockPosition.Top:
					point = new Gdk.Point (window_position.X + Allocation.Width / 2, offset + window_position.Y + top);
					break;
				case DockPosition.Bottom:
					point = new Gdk.Point (window_position.X + Allocation.Width / 2, window_position.Y + Allocation.Height - top - offset);
					break;
				case DockPosition.Left:
					point = new Gdk.Point (offset + window_position.X + top, window_position.Y + Allocation.Height / 2);
					break;
				case DockPosition.Right:
					point = new Gdk.Point (window_position.X + Allocation.Width - top - offset, window_position.Y + Allocation.Height / 2);
					break;
				}
			}
			
			TextManager.Gravity = Position; // FIXME
			TextManager.Monitor = Monitor;
			TextManager.SetSurfaceAtPoint (hover, point);
		}
		
		internal AbstractDockItem ClosestItem {
			get {
				return Items
					.Where (adi => !(adi is INonPersistedItem) && DrawValues.ContainsKey (adi))
					.OrderBy (adi => Math.Abs (Preferences.IsVertical ? DrawValues[adi].Center.Y - LocalCursor.Y : DrawValues[adi].Center.X - LocalCursor.X))
					.DefaultIfEmpty (null)
					.FirstOrDefault ();
			}
		}
		
		internal AbstractDockItem RightMostItem {
			get {
				return Items
					.Where (adi => !(adi is INonPersistedItem) && DrawValues.ContainsKey (adi))
					.Where (adi => (Preferences.IsVertical ? DrawValues[adi].Center.Y - LocalCursor.Y : DrawValues[adi].Center.X - LocalCursor.X) > 0)
					.OrderBy (adi => Math.Abs (Preferences.IsVertical ? DrawValues[adi].Center.Y - LocalCursor.Y : DrawValues[adi].Center.X - LocalCursor.X))
					.DefaultIfEmpty (null)
					.FirstOrDefault ();
			}
		}
		
		internal AbstractDockItemProvider HoveredProvider {
			get {
				if (!DockHovered)
					return null;
				
				AbstractDockItem closest = HoveredItem ?? ClosestItem;
				if (closest != null && closest.Owner != null) {
					return closest.Owner;
				}
				
				return Preferences.DefaultProvider;
			}
		}
		
		internal IEnumerable<AbstractDockItemProvider> ItemProviders {
			get { return Preferences.ItemProviders; }
		}

		AbstractDockPainter Painter {
			get { return painter; }
			set { 
				if (painter == value)
					return;
				painter_change_time = DateTime.UtcNow;
				painter = value; 
			}
		}
		
		int PainterBufferSize {
			get { return 2 * IconSize + 3 * DockWidthBuffer; }
		}
		
		/// <summary>
		/// Only valid durring a rendering sequence
		/// </summary>
		double PainterOpacity {
			get {
				double progress =
					Math.Min (1, ((rendering ? render_time : DateTime.UtcNow) - painter_change_time).TotalMilliseconds / PainterAnimationTime.TotalMilliseconds);
				if (Painter != null)
					return progress;
				return 1 - progress;
			}
		}
		
		internal int IconSize {
			get { return Math.Min (MaxIconSize, Preferences.IconSize); }
		}
		
		int MaxIconSize { get; set; }
		
		int Monitor {
			get { return Preferences.MonitorNumber; }
		}
		
		internal DockPosition Position {
			get { return Preferences.Position; }
		}
		
		bool ThreeDimensional {
			get { return Position == DockPosition.Bottom && Preferences.ThreeDimensional; }
		}
		
		
		bool ZoomEnabled {
			get { return !Preferences.PanelMode && Preferences.ZoomEnabled; }
		}
		
		double ZoomPercent {
			get {
				if (!ZoomEnabled)
					return 1;
				return (Preferences.IconSize * Preferences.ZoomPercent) / MaxIconSize;
			}
		}
		#endregion
		
		#region Internal Properties
		Gdk.Point Cursor {
			get {
				if (Screen != CursorTracker.Screen) {
					return new Gdk.Point (-1000, -1000);
				}
				return CursorTracker.Cursor; 
			}
		}
		
		Gdk.Point LocalCursor {
			get { return new Gdk.Point (Cursor.X - window_position.X, Cursor.Y - window_position.Y); }
		}
		
		int DockHeight {
			get {
				if (Painter != null)
					if (Preferences.IsVertical)
						return Math.Max (IconSize, Painter.MinimumWidth) + 2 * DockHeightBuffer;
					else
						return Math.Max (IconSize, Painter.MinimumHeight) + 2 * DockHeightBuffer;
				return IconSize + 2 * DockHeightBuffer;
			}
		}
		
		int VisibleDockHeight {
			get {
				return ThreeDimensional ? (int) ((Preferences.PanelMode ? 0.33 : 0.4 ) * DockHeight) : DockHeight;
			}
		}
		
		int DockWidth {
			get {
				if (GdkWindow == null)
					return 0;
				
				int dockWidth = Items.Sum (adi => (int) ((adi.Square ? IconSize : adi.IconSurface (background_buffer, IconSize, IconSize, VisibleDockHeight).Width) * 
						Math.Min (1, (DateTime.UtcNow - adi.AddTime).TotalMilliseconds / BaseAnimationTime.TotalMilliseconds)));
				dockWidth += 2 * DockWidthBuffer + (Items.Count - 1) * ItemWidthBuffer;
				
				if (remove_index != 0)
					dockWidth += (int) ((ItemWidthBuffer + remove_size) *
							(1 - Math.Min (1, (DateTime.UtcNow - remove_time).TotalMilliseconds / BaseAnimationTime.TotalMilliseconds)));
				
				return dockWidth;
			}
		}
		
		int last_painter_width;
		int PainterDockWidth {
			get {
				if (Painter == null)
					return last_painter_width;
				
				int max, last;
				
				if (Preferences.IsVertical) {
					max = monitor_geo.Height;
					
					Gdk.Rectangle allocation = new Gdk.Rectangle (
						0,
						0,
						IconSize,
						DockWidth - PainterBufferSize);
					
					if (Painter.Allocation != allocation) {
						Painter.SetAllocation (allocation);
						allocation.Height = Math.Min (max - PainterBufferSize, Painter.MinimumHeight);
						allocation.Width = Math.Max (allocation.Width, Painter.MinimumWidth);
						Painter.SetAllocation (allocation);
					}
					last = allocation.Height;
				} else {
					max = monitor_geo.Width;
					
					Gdk.Rectangle allocation = new Gdk.Rectangle (
						0,
						0,
						DockWidth - PainterBufferSize,
						IconSize);
					
					if (Painter.Allocation != allocation) {
						Painter.SetAllocation (allocation);
						allocation.Width = Math.Min (max - PainterBufferSize, Painter.MinimumWidth);
						allocation.Height = Math.Max (allocation.Height, Painter.MinimumHeight);
						Painter.SetAllocation (allocation);
					}
					last = allocation.Width;
				}
				
				last_painter_width = Math.Min (max, last + PainterBufferSize);
				return last_painter_width;
			}
		}
		
		int DockHeightBuffer {
			get { return (Preferences.PanelMode) ? 3 : 7 + (ThreeDimensional ? 5 : 0); }
		}
		
		int DockWidthBuffer {
			get { return 5; }
		}
		
		int ItemWidthBuffer {
			get { return (int) (0.08 * IconSize); }
		}
		
		int HotAreaPadding {
			get { return Preferences.HotAreaPadding; }
		}
		
		/// <summary>
		/// The int size a fully zoomed icon will display at.
		/// </summary>
		internal int ZoomedIconSize {
			get { 
				return ZoomEnabled ? (int) (IconSize * ZoomPercent) : IconSize; 
			}
		}
		
		int ZoomedDockHeight {
			get { return ZoomedIconSize + 2 * DockHeightBuffer; }
		}
		
		double HideOffset {
			get {
				if (Painter != null)
					return 0;
				double progress = Math.Min (1, (render_time - hidden_change_time).TotalMilliseconds / 
				                            BaseAnimationTime.TotalMilliseconds);
				if (AutohideManager.Hidden)
					return progress;
				return 1 - progress;
			}
		}

		double PanelModeToggleProgress {
			get {
				return Math.Min (1, ((rendering ? render_time : DateTime.UtcNow) - panel_change_time).TotalMilliseconds / PanelAnimationTime.TotalMilliseconds);
			}
		}
		
		double DockOpacity {
			get {
				return Math.Min (1, (1 - HideOffset) + Preferences.FadeOpacity);
			}
		}
		
		double ZoomIn {
			get {
				if (ConfigurationMode)
					return 0;
				
				// we buffer this value during renders since it will be checked many times and we dont need to 
				// recalculate it each time
				if (zoom_in_buffer.HasValue && rendering)
					return zoom_in_buffer.Value;
				
				double zoom = Math.Min (1, (render_time - dock_hovered_change_time).TotalMilliseconds / 
				                        BaseAnimationTime.TotalMilliseconds);
				
				if (!DockHovered)
					zoom = 1 - zoom;
				
				zoom *= 1 - PainterOpacity;
				
				if (rendering)
					zoom_in_buffer = zoom;
				
				return zoom;
			}
		}

		#endregion
		
		static DockWindow ()
		{
			DockyItem = new DockyItem ();
		}
	
		public DockWindow () : base(Gtk.WindowType.Toplevel)
		{
			DrawValues = new Dictionary<AbstractDockItem, DrawValue> ();
			Menu = new DockItemMenu (this);
			Menu.Shown  += HandleMenuShown;
			Menu.Hidden += HandleMenuHidden;
			
			TextManager = new HoverTextManager ();
			DragTracker = new DockDragTracker (this);
			AnimationState = new AnimationState ();
			BuildAnimationEngine ();
			
			collection_backend = new List<AbstractDockItem> ();
			collection_frontend = collection_backend.AsReadOnly ();
			
			AppPaintable = true;
			AcceptFocus = false;
			Decorated = false;
			DoubleBuffered = false;
			SkipPagerHint = true;
			SkipTaskbarHint = true;
			Resizable = false;
			CanFocus = false;
			TypeHint = WindowTypeHint.Dock;
			
			this.SetCompositeColormap ();
			Stick ();
			
			AddEvents ((int) (Gdk.EventMask.ButtonPressMask |
			                  Gdk.EventMask.ButtonReleaseMask |
			                  Gdk.EventMask.EnterNotifyMask |
			                  Gdk.EventMask.LeaveNotifyMask |
					          Gdk.EventMask.PointerMotionMask |
			                  Gdk.EventMask.ScrollMask));
			
			Wnck.Screen.Default.WindowOpened += HandleWindowOpened;
			Realized                         += HandleRealized;
			DockServices.Theme.ThemeChanged  += DockyControllerThemeChanged;
			
			// fix for nvidia bug
			if (UserArgs.BufferTime > 0)
				GLib.Timeout.Add (UserArgs.BufferTime * 60 * 1000, delegate {
					ResetBuffers ();
					return true;
				});
		}


		#region Event Handling
		void BuildAnimationEngine ()
		{
			AnimationState.AddCondition (Animations.DockHoveredChanged, 
			                             () => (DockHovered && ZoomIn != 1) || (!DockHovered && ZoomIn != 0));
			AnimationState.AddCondition (Animations.HideChanged,
			                             () => ((DateTime.UtcNow - hidden_change_time) < BaseAnimationTime));
			AnimationState.AddCondition (Animations.ItemsChanged,
				                         () => ((DateTime.UtcNow - items_change_time) < BaseAnimationTime));
			AnimationState.AddCondition (Animations.PainterChanged,
			                             () => ((DateTime.UtcNow - painter_change_time) < PainterAnimationTime));
			AnimationState.AddCondition (Animations.ThreeDimensionalChanged,
			                             () => ((DateTime.UtcNow - threedimensional_change_time) < BaseAnimationTime));
			AnimationState.AddCondition (Animations.PanelChanged,
			                             () => ((DateTime.UtcNow - panel_change_time) < PanelAnimationTime));
			AnimationState.AddCondition (Animations.ItemStatesChanged, ItemsWithStateChange);
		}
		
		bool ItemsWithStateChange () {
			DateTime now = DateTime.UtcNow;
			
			foreach (AbstractDockItem adi in Items) {
				//Waiting Items
				if ((adi.State & ItemState.Wait) != 0)
					return true;
				//Moving Items
				if (now - adi.StateSetTime (ItemState.Move) < SlideTime)
					return true;
				//Bouncing Items
				if ((now - adi.LastClick) < BounceTime)
					return true;
				if ((adi.State & ItemState.Urgent) != ItemState.Urgent)
					continue;
				if ((now - adi.StateSetTime (ItemState.Urgent)) < BounceTime)
					return true;
				//Glowing Items
				if ((now - adi.StateSetTime (ItemState.Urgent)) < DockServices.Theme.GlowTime)
					return true;
			}
			return false;
		}
		
		void HandleMenuHidden (object sender, EventArgs e)
		{
			UpdateScreenRegions ();
			SetTooltipVisibility ();
			AnimatedDraw ();
		}

		void HandleMenuShown (object sender, EventArgs e)
		{
			UpdateScreenRegions ();
			SetTooltipVisibility ();
			AnimatedDraw ();
		}

		void DockyControllerThemeChanged (object sender, EventArgs e)
		{
			ResetBuffers ();
			AnimatedDraw ();
		}
		
		void HandleRealized (object sender, EventArgs e)
		{
			GdkWindow.SetBackPixmap (null, false);
			
			CursorTracker = CursorTracker.ForDisplay (Display);
			CursorTracker.CursorPositionChanged += HandleCursorPositionChanged;	
			
			AutohideManager = new AutohideManager (Screen);
			AutohideManager.StartupMode = true;
			AutohideManager.Behavior = Preferences.Autohide;
			
			AutohideManager.HiddenChanged      += HandleHiddenChanged;
			AutohideManager.DockHoveredChanged += HandleDockHoveredChanged;
			
			Screen.SizeChanged += ScreenSizeChanged;
			
			SetSizeRequest ();
		}

		void ScreenSizeChanged (object sender, EventArgs e)
		{
			Reconfigure ();
			UpdateMaxIconSize ();
			AnimatedDraw ();
		}

		void HandleDockHoveredChanged (object sender, EventArgs e)
		{
			dock_hovered_change_time = UpdateTimeStamp (dock_hovered_change_time, BaseAnimationTime);
			
			if (DockHovered)
				CursorTracker.RequestHighResolution (this);
			else
				CursorTracker.CancelHighResolution (this);
			
			DragTracker.EnsureDragAndDropProxy ();
			SetTooltipVisibility ();
			AnimatedDraw ();
		}

		void HandleHiddenChanged (object sender, EventArgs e)
		{
			hidden_change_time = UpdateTimeStamp (hidden_change_time, BaseAnimationTime);
			AnimatedDraw ();
		}

		void HandleCursorPositionChanged (object sender, CursorPostionChangedArgs e)
		{
			if (DockHovered && e.LastPosition != Cursor)
				AnimatedDraw ();
			DragTracker.EnsureDragAndDropProxy ();
			
			UpdateMonitorGeometry ();
			int distance;
			switch (Position) {
			default:
			case DockPosition.Top:
				distance = e.LastPosition.Y - ZoomedDockHeight;
				break;
			case DockPosition.Left:
				distance = e.LastPosition.X - ZoomedDockHeight;
				break;
			case DockPosition.Bottom:
				distance = monitor_geo.Height - ZoomedDockHeight - e.LastPosition.Y;
				break;
			case DockPosition.Right:
				distance = monitor_geo.Width - ZoomedDockHeight - e.LastPosition.X;
				break;
			}
			if (distance > 0.3 * DockHeight)
				HidePainter ();
		}
		
		void RegisterItemProvider (AbstractDockItemProvider provider)
		{
			provider.ItemsChanged += ProviderItemsChanged;
			
			foreach (AbstractDockItem item in provider.Items)
				RegisterItem (item);
			
			provider.Registered ();
			
			UpdateMaxIconSize ();
			DelayedSetSizeRequest ();
		}
		
		void UnregisterItemProvider (AbstractDockItemProvider provider)
		{
			provider.ItemsChanged -= ProviderItemsChanged;
			
			foreach (AbstractDockItem item in provider.Items)
				UnregisterItem (item);
			
			provider.Unregistered ();
			
			UpdateMaxIconSize ();
			DelayedSetSizeRequest ();
		}
		
		void ProviderItemsChanged (object sender, ItemsChangedArgs args)
		{
			foreach (AbstractDockItem item in args.AddedItems)
				RegisterItem (item);
			
			foreach (AbstractDockItem item in args.RemovedItems) {
				remove_time = DateTime.UtcNow;
				UnregisterItem (item);
				
				remove_index = Items.IndexOf (item) - .5;
				remove_size = IconSize; //FIXME
			}
			
			UpdateCollectionBuffer ();
			UpdateMaxIconSize ();
			DelayedSetSizeRequest ();
			
			AnimatedDraw ();
			
			// if the provider has no more items and its set to auto disable, remove it from the dock
			AbstractDockItemProvider provider = sender as AbstractDockItemProvider;
			
			if (provider.Items.Count () == 0 && provider.AutoDisable)
				preferences.RemoveProvider (provider);
		}
		
		void RegisterItem (AbstractDockItem item)
		{
			item.SetStyle (Style);
			item.HoverTextChanged += ItemHoverTextChanged;
			item.PaintNeeded      += ItemPaintNeeded;
			item.PainterRequest   += ItemPainterRequest;
			
			DBusManager.Default.RegisterItem (item);
		}

		void UnregisterItem (AbstractDockItem item)
		{
			item.HoverTextChanged -= ItemHoverTextChanged;
			item.PaintNeeded      -= ItemPaintNeeded;
			item.PainterRequest   -= ItemPainterRequest;
			DrawValues.Remove (item);
			
			DBusManager.Default.UnregisterItem (item);
		}
		
		void ItemHoverTextChanged (object sender, EventArgs e)
		{
			if ((sender as AbstractDockItem) == HoveredItem)
				UpdateHoverText ();
			AnimatedDraw ();
		}
		
		void ItemPaintNeeded (object sender, PaintNeededEventArgs e)
		{
			AnimatedDraw ();
		}

		void ItemPainterRequest (object sender, PainterRequestEventArgs e)
		{
			AbstractDockItem owner = sender as AbstractDockItem;
			
			if (!Items.Contains (owner) || e.Painter == null)
				return;
			
			if (e.Type == ShowHideType.Show) {
				ShowPainter (owner, e.Painter);
			} else if (e.Type == ShowHideType.Hide && Painter == e.Painter) {
				HidePainter ();
			}
		}
		
		void RegisterPreferencesEvents (IDockPreferences preferences)
		{
			preferences.AutohideChanged         += PreferencesAutohideChanged;
			preferences.PanelModeChanged        += PreferencesPanelModeChanged;
			preferences.IconSizeChanged         += PreferencesIconSizeChanged;
			preferences.PositionChanged         += PreferencesPositionChanged;
			preferences.ThreeDimensionalChanged += PreferencesThreeDimensionalChanged;
			preferences.ZoomEnabledChanged      += PreferencesZoomEnabledChanged;
			preferences.ZoomPercentChanged      += PreferencesZoomPercentChanged;
			preferences.ItemProvidersChanged    += PreferencesItemProvidersChanged;
			
			foreach (AbstractDockItemProvider provider in preferences.ItemProviders)
				RegisterItemProvider (provider);
		}

		void UnregisterPreferencesEvents (IDockPreferences preferences)
		{
			preferences.AutohideChanged         -= PreferencesAutohideChanged;
			preferences.PanelModeChanged        -= PreferencesPanelModeChanged;
			preferences.IconSizeChanged         -= PreferencesIconSizeChanged;
			preferences.PositionChanged         -= PreferencesPositionChanged;
			preferences.ThreeDimensionalChanged -= PreferencesThreeDimensionalChanged;
			preferences.ZoomEnabledChanged      -= PreferencesZoomEnabledChanged;
			preferences.ZoomPercentChanged      -= PreferencesZoomPercentChanged;
			preferences.ItemProvidersChanged    -= PreferencesItemProvidersChanged;
			
			foreach (AbstractDockItemProvider provider in preferences.ItemProviders)
				UnregisterItemProvider (provider);
		}
		
		void PreferencesItemProvidersChanged (object sender, ItemProvidersChangedEventArgs e)
		{
			foreach (AbstractDockItemProvider provider in e.AddedProviders)
				RegisterItemProvider (provider);
			foreach (AbstractDockItemProvider provider in e.RemovedProviders)
				UnregisterItemProvider (provider);
			UpdateCollectionBuffer ();
			AnimatedDraw ();
		}
		
		void PreferencesThreeDimensionalChanged (object sender, EventArgs e)
		{
			threedimensional_change_time = DateTime.UtcNow;
			ResetBuffers ();
			AnimatedDraw ();
		}

		void PreferencesZoomPercentChanged (object sender, EventArgs e)
		{
			SetSizeRequest ();
			AnimatedDraw ();
		}

		void PreferencesZoomEnabledChanged (object sender, EventArgs e)
		{
			SetSizeRequest ();
			AnimatedDraw ();
		}

		void PreferencesPositionChanged (object sender, EventArgs e)
		{
			Reconfigure ();
		}

		void PreferencesIconSizeChanged (object sender, EventArgs e)
		{
			Reconfigure ();
			UpdateMaxIconSize ();
			AnimatedDraw ();
		}

		void PreferencesPanelModeChanged (object sender, EventArgs e)
		{
			panel_change_time = DateTime.UtcNow;
			Reconfigure ();
		}

		void PreferencesAutohideChanged (object sender, EventArgs e)
		{
			AutohideManager.Behavior = Autohide;
			SetStruts ();
		}
		
		void OnHoveredItemChanged (AbstractDockItem lastItem)
		{
			if (HoveredItemChanged != null) {
				HoveredItemChanged (this, new HoveredItemChangedArgs (lastItem));
			}
		}
		#endregion
		
		#region Input Handling
		
		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			if (Painter != null) {
				if (evnt.Key == Gdk.Key.Escape) {
					HidePainter ();
				} else if (Painter is PagingDockPainter) {
					if (evnt.Key == Gdk.Key.Left)
						(Painter as PagingDockPainter).PreviousPage ();
					else if (evnt.Key == Gdk.Key.Right)
						(Painter as PagingDockPainter).NextPage ();
				}
			}
			
			return base.OnKeyPressEvent (evnt);
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			if (!ConfigurationMode)
				CursorTracker.SendManualUpdate (evnt);
			
			if (Painter != null) {
				int x, y;
				
				x = LocalCursor.X - painter_area.X;
				y = LocalCursor.Y - painter_area.Y;
				
				if (painter_area.Contains (LocalCursor))
					Painter.MotionNotify (x, y, evnt.State);
				else
					Painter.MotionNotify (-1, -1, ModifierType.None);
			}
			
			return base.OnMotionNotifyEvent (evnt);
		}
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			if (!ConfigurationMode)
				CursorTracker.SendManualUpdate (evnt);
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			if (!ConfigurationMode)
				CursorTracker.SendManualUpdate (evnt);
			
			if (Painter != null)
				Painter.MotionNotify (-1, -1, ModifierType.None);
			
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (InternalDragActive || ConfigurationMode)
				return base.OnButtonPressEvent (evnt);
			
			lastClickedItem = null;
			
			MenuButton button;
			switch (evnt.Button) {
			case 1:
				button = MenuButton.Left;
				break;
			case 2:
				button = MenuButton.Middle;
				break;
			case 3:
				button = MenuButton.Right;
				break;
			default:
				button = MenuButton.None;
				break;
			}
			
			if (Painter != null) {
				int x, y;
				
				x = LocalCursor.X - painter_area.X;
				y = LocalCursor.Y - painter_area.Y;
				
				if (painter_area.Contains (LocalCursor))
					Painter.ButtonPressed (x, y, evnt.State);
			} else if (HoveredItem != null && !(HoveredItem is SeparatorItem) && !(HoveredItem is SpacingItem)) {
				if ((button & HoveredItem.MenuButton) == button) {
					MenuList list;
					
					if (HoveredItem.Owner != null)
						list = HoveredItem.Owner.GetMenuItems (HoveredItem);
					else
						list = HoveredItem.GetMenuItems ();
					
					if (list.Any ()) {
						DrawValue val = DrawValues[HoveredItem];
						val = val.MoveIn (Position, ZoomedIconSize / 2 + DockHeightBuffer);
						Menu.Anchor = new Gdk.Point ((int) val.Center.X + window_position.X, (int) val.Center.Y + window_position.Y);
						Menu.Orientation = Position;
						Menu.Monitor = Monitor;
						Menu.SetItems (list);
						Menu.Show ();
					}
				} else {
					lastClickedItem = HoveredItem;
				}
			} else if (button == MenuButton.Right) {
				ShowDockyItemMenu ();
			}
			
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			// This event gets fired before the drag end event, in this case
			// we ignore it.
			if (InternalDragActive || ConfigurationMode)
				return base.OnButtonPressEvent (evnt);
			
			MenuButton button;
			switch (evnt.Button) {
			case 1:
				button = MenuButton.Left;
				break;
			case 2:
				button = MenuButton.Middle;
				break;
			case 3:
				button = MenuButton.Right;
				break;
			default:
				button = MenuButton.None;
				break;
			}
			
			if (Painter != null) {
				int x, y;
				
				x = LocalCursor.X - painter_area.X;
				y = LocalCursor.Y - painter_area.Y;
				
				if (!painter_area.Contains (LocalCursor))
					HidePainter ();
				else
					Painter.ButtonReleased (x, y, evnt.State);
			} else if (HoveredItem != null && !(HoveredItem is SeparatorItem) && !(HoveredItem is SpacingItem)) {
				if (lastClickedItem == HoveredItem) {
					Gdk.Rectangle region = DrawRegionForItem (HoveredItem);
					
					double x;
					double y = Math.Min (1, Math.Max (0, (Cursor.Y - window_position.Y - region.Y) / (double) region.Height));
					
					if (Preferences.IsVertical)
						x = Math.Min (1, Math.Max (0, (Cursor.X - window_position.X - region.X) / (double) region.Width));
					else
						x = Math.Min (1, Math.Max (0, (Cursor.X + window_position.X - region.X) / (double) region.Width));
					
					if (HoveredItem.RotateWithDock) {
						if (Position == DockPosition.Top) {
							x = 1 - x;
							y = 1 - y;
						} else if (Preferences.IsVertical) {
							double tmp = x;
							x = y;
							y = tmp;
							
							if (Position == DockPosition.Left)
								y = 1 - y;
							else
								x = 1 - x;
						}
					}
					
					HoveredItem.Clicked (evnt.Button, evnt.State, x, y);

					AnimatedDraw ();
				}
			} else if (button == MenuButton.Right) {
				ShowDockyItemMenu ();
			}
			
			return base.OnButtonReleaseEvent (evnt);
		}
		
		void ShowDockyItemMenu ()
		{
			switch (Position) {
			case DockPosition.Bottom:
				Menu.Anchor = new Gdk.Point (LocalCursor.X + window_position.X, window_position.Y + Allocation.Height - DockHeight + 10);
				break;
			case DockPosition.Top:
				Menu.Anchor = new Gdk.Point (LocalCursor.X + window_position.X, window_position.Y + DockHeight - 10);
				break;
			case DockPosition.Left:
				Menu.Anchor = new Gdk.Point (window_position.X + DockHeight - 10, LocalCursor.Y + window_position.Y);
				break;
			case DockPosition.Right:
				Menu.Anchor = new Gdk.Point (window_position.X + Allocation.Width - DockHeight + 10, LocalCursor.Y + window_position.Y);
				break;
			}
			Menu.Orientation = Position;
			Menu.Monitor = Monitor;
			Menu.SetItems (DockyItem.GetMenuItems ());
			Menu.Show ();
		}

		protected override bool OnScrollEvent (EventScroll evnt)
		{
			if (InternalDragActive || ConfigurationMode)
				return base.OnScrollEvent (evnt);
			
			if (Painter != null) {
				int x, y;
				
				x = LocalCursor.X - painter_area.X;
				y = LocalCursor.Y - painter_area.Y;
				
				if (painter_area.Contains (LocalCursor))
					Painter.Scrolled (evnt.Direction, x, y, evnt.State);
			} else if ((evnt.State & ModifierType.ControlMask) == ModifierType.ControlMask) {
				if (evnt.Direction == ScrollDirection.Up)
					Preferences.IconSize++;
				else if (evnt.Direction == ScrollDirection.Down)
					Preferences.IconSize--;
				return false;
			} else if (HoveredItem != null) {
				HoveredItem.Scrolled (evnt.Direction, evnt.State);
			}
			
			return base.OnScrollEvent (evnt);
		}
		#endregion
		
		#region Misc.
		void HandleWindowOpened (object o, WindowOpenedArgs args)
		{
			UpdateScreenRegions ();
			AnimatedDraw ();
		}
		
		void Reconfigure ()
		{
			SetSizeRequest ();
			Reposition ();
			ResetBuffers ();
			AnimatedDraw ();
		}
		
		internal void SetTooltipVisibility ()
		{
			bool visible = (HoveredItem != null && !InternalDragActive && !ExternalDragActive &&
								!Menu.Visible && !ConfigurationMode && Painter == null) ||
							ActiveGlow ||
							(ExternalDragActive && DockHovered && !(hoveredItem is INonPersistedItem));
						
			if (visible)
				TextManager.Show ();
			else
				TextManager.Hide ();
		}
		
		internal void SetHoveredAcceptsDrop ()
		{
			HoveredAcceptsDrop = false;
			
			if (HoveredItem != null && Painter == null && !ConfigurationMode)
				DragTracker.DragDisabled = HoveredItem is INonPersistedItem;
			
			if (HoveredItem != null && ExternalDragActive && DragTracker.DragData != null && HoveredItem.CanAcceptDrop (DragTracker.DragData))
				HoveredAcceptsDrop = true;
		}
		
		internal void UpdateCollectionBuffer ()
		{
			if (rendering) {
				// resetting during a render is bad. Complete the render, then reset.
				GLib.Idle.Add (delegate {
					// dispose of our separators as we made them ourselves,
					// this could be a bit more elegant
					foreach (AbstractDockItem item in Items.Where (adi => adi is INonPersistedItem && adi != DockyItem)) {
						DrawValues.Remove (item);
						item.Dispose ();
					}
					
					collection_backend.Clear ();
					return false;
				});
			} else {
				foreach (AbstractDockItem item in Items.Where (adi => adi is INonPersistedItem && adi != DockyItem)) {
					DrawValues.Remove (item);
					item.Dispose ();
				}
				
				collection_backend.Clear ();
			}
			
			items_change_time = DateTime.UtcNow;
		}
		
		void ResetBuffers ()
		{
			if (main_buffer != null) {
				main_buffer.Dispose ();
				main_buffer = null;
			}
			
			if (painter_buffer != null) {
				painter_buffer.Dispose ();
				painter_buffer = null;
			}
			
			if (background_buffer != null) {
				background_buffer.Dispose ();
				background_buffer = null;
			}
			
			if (icon_buffer != null) {
				icon_buffer.Dispose ();
				icon_buffer = null;
			}
			
			if (normal_indicator_buffer != null) {
				normal_indicator_buffer.Dispose ();
				normal_indicator_buffer = null;
			}
			
			if (urgent_indicator_buffer != null) {
				urgent_indicator_buffer.Dispose ();
				urgent_indicator_buffer = null;
			}

			if (urgent_glow_buffer != null) {
				urgent_glow_buffer.Dispose ();
				urgent_glow_buffer = null;
			}
			
			if (wait_buffer != null) {
				wait_buffer.Dispose ();
				wait_buffer = null;
			}
			
			if (config_hover_buffer != null) {
				config_hover_buffer.Dispose ();
				config_hover_buffer = null;
			}
			
			if (launch_hover_buffer != null) {
				launch_hover_buffer.Dispose ();
				launch_hover_buffer = null;
			}
			
			if (drop_hover_buffer != null) {
				drop_hover_buffer.Dispose ();
				drop_hover_buffer = null;
			}
			
			foreach (AbstractDockItem item in Items)
				item.ResetBuffers ();
		}
		#endregion
		
		#region Painters
		void ShowPainter (AbstractDockItem owner, AbstractDockPainter painter)
		{
			if (Painter != null || owner == null || painter == null)
				return;
			
			if (!painter.SupportsVertical && Preferences.IsVertical) {
				Log<DockWindow>.Notify ("The docklet's painter only works on horizontal (bottom or top) docks.");
				return;
			}
			
			painter.IsVertical = Preferences.IsVertical;
			Painter = painter;
			painterOwner = owner;
			Painter.HideRequest += HandlePainterHideRequest;
			Painter.PaintNeeded += HandlePainterPaintNeeded;
			
			repaint_painter = true;
			UpdateScreenRegions ();
			DragTracker.DragDisabled = true;
			
			SetTooltipVisibility ();
			Painter.SetStyle (Style);
			
			SetSizeRequest ();
			
			Painter.Shown ();
			Keyboard.Grab (GdkWindow, true, Gtk.Global.CurrentEventTime);
			AnimatedDraw ();
		}
		
		void HidePainter ()
		{
			if (Painter == null)
				return;
			
			Painter.HideRequest -= HandlePainterHideRequest;
			Painter.PaintNeeded -= HandlePainterPaintNeeded;
			
			DragTracker.DragDisabled = false;
			UpdateScreenRegions ();
			
			Painter.Hidden ();
			Painter = null;
			Keyboard.Ungrab (Gtk.Global.CurrentEventTime);
			
			SetSizeRequest ();
			
			SetTooltipVisibility ();
			AnimatedDraw ();
		}
		
		void HandlePainterPaintNeeded (object sender, EventArgs e)
		{
			repaint_painter = true;
			AnimatedDraw ();
		}

		void HandlePainterHideRequest (object sender, EventArgs e)
		{
			if (Painter == sender as AbstractDockPainter)
				HidePainter ();
		}
		#endregion
		
		#region Size and Position
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			ResetBuffers ();
			Reposition ();
		}

		protected override void OnShown ()
		{
			base.OnShown ();
			Reposition ();
		}
		
		protected override bool OnConfigureEvent (EventConfigure evnt)
		{
			window_position.X = evnt.X;
			window_position.Y = evnt.Y;
			
			return base.OnConfigureEvent (evnt);
		}
		
		void Reposition ()
		{
			UpdateMonitorGeometry ();
			
			switch (Position) {
			default:
			case DockPosition.Top:
				Move (monitor_geo.X + (monitor_geo.Width - Width) / 2, monitor_geo.Y);
				break;
			case DockPosition.Left:
				Move (monitor_geo.X, monitor_geo.Y + (monitor_geo.Height - Height) / 2);
				break;
			case DockPosition.Right:
				Move (monitor_geo.X + monitor_geo.Width - Width, monitor_geo.Y + (monitor_geo.Height - Height) / 2);
				break;
			case DockPosition.Bottom:
				Move (monitor_geo.X + (monitor_geo.Width - Width) / 2, monitor_geo.Y + monitor_geo.Height - Height);
				break;
			}
			
			SetStruts ();
			
			// move the hover after we redraw
			GLib.Idle.Add (delegate {
				UpdateHoverText ();
				return false;
			});
		}
		
		void UpdateMonitorGeometry ()
		{
			monitor_geo = Screen.GetMonitorGeometry (Monitor);
		}
		
		void DelayedSetSizeRequest ()
		{
			if (size_request_timer > 0)
				GLib.Source.Remove (size_request_timer);
			
			size_request_timer = GLib.Timeout.Add ((uint) BaseAnimationTime.TotalMilliseconds, delegate {
				size_request_timer = 0;
				SetSizeRequest ();
				return false;
			});
		}
		
		void SetSizeRequest ()
		{
			UpdateMonitorGeometry ();
			
			int height = ZoomedDockHeight;
			if (Painter != null)
				height = Math.Max (2 * IconSize + DockHeightBuffer, Painter.MinimumHeight + 2 * DockHeightBuffer);
			else if (Gdk.Screen.Default.IsComposited)
				height += UrgentBounceHeight;
			
			int width = Math.Min (UserArgs.MaxSize, Preferences.IsVertical ? monitor_geo.Height : monitor_geo.Width);
			if (!Gdk.Screen.Default.IsComposited && !Preferences.PanelMode) {
				if (Painter != null)
					width = Math.Min (PainterDockWidth, width);
				else
					width = Math.Min (DockWidth, width);
			}
			
			if (Preferences.IsVertical) {
				Width = height;
				Height = width;
			} else {
				Width = width;
				Height = height;
			}
			
			if (UserArgs.NetbookMode) {
				// Currently the intel i945 series of cards (used on netbooks frequently) will 
				// for some mystical reason get terrible drawing performance if the window is
				// between 1009 pixels and 1024 pixels in width OR height. We just pad it out an extra
				// pixel
				if (Width >= 1009 && Width <= 1024)
					Width = 1026;
				
				if (Height >= 1009 && Height <= 1024)
					Height = 1026;
				
			}
			
			SetSizeRequest (Width, Height);
		}
		#endregion
		
		#region Drawing
		internal void AnimatedDraw ()
		{
			if (animation_timer > 0) 
				return;
			
			// the presense of this queue draw has caused some confusion, so I will explain.
			// first its here to draw the "first frame".  Without it, we have a 16ms delay till that happens,
			// however minor that is.
			QueueDraw ();
			
			if (AnimationState.AnimationNeeded)
				animation_timer = GLib.Timeout.Add (1000/60, OnDrawTimeoutElapsed);
		}
		
		bool OnDrawTimeoutElapsed ()
		{
			QueueDraw ();
			
			if (AnimationState.AnimationNeeded)
				return true;
			
			//reset the timer to 0 so that the next time AnimatedDraw is called we fall back into
			//the draw loop.
			if (animation_timer > 0) {
				GLib.Source.Remove (animation_timer);
				animation_timer = 0;
			}

			// one final draw to clea out the end of previous animations
			QueueDraw ();
			return false;
		}
		
		Gdk.Rectangle DrawRegionForItem (AbstractDockItem item)
		{
			if (!DrawValues.ContainsKey (item))
				return Gdk.Rectangle.Zero;
			
			return DrawRegionForItemValue (item, DrawValues[item]);
		}
		
		Gdk.Rectangle DrawRegionForItemValue (AbstractDockItem item, DrawValue val)
		{
			return DrawRegionForItemValue (item, val, false);
		}
		
		Gdk.Rectangle DrawRegionForItemValue (AbstractDockItem item, DrawValue val, bool hoverRegion)
		{
			int width = IconSize, height = IconSize;
			
			if (!item.Square) {
				DockySurface surface = item.IconSurface (main_buffer, IconSize, IconSize, VisibleDockHeight);
				
				width = surface.Width;
				height = surface.Height;
				
				if (item.RotateWithDock && Preferences.IsVertical) {
					int tmp = width;
					width = height;
					height = tmp;
				}
			}
			
			if (hoverRegion)
				if (Preferences.IsVertical)
					return new Gdk.Rectangle ((int) (val.Center.X - (width * val.Zoom / 2)),
						(int) (val.StaticCenter.Y - height / 2),
						(int) (width * val.Zoom),
						height);
				else
					return new Gdk.Rectangle ((int) (val.StaticCenter.X - width / 2),
						(int) (val.Center.Y - (height * val.Zoom / 2)),
						width,
						(int) (height * val.Zoom));
			
			return new Gdk.Rectangle ((int) (val.Center.X - (width * val.Zoom / 2)),
				(int) (val.Center.Y - (height * val.Zoom / 2)),
				(int) (width * val.Zoom),
				(int) (height * val.Zoom));
		}
		
		/// <summary>
		/// Updates drawing regions for the supplied surface
		/// </summary>
		/// <param name="surface">
		/// The <see cref="DockySurface"/> surface off which the coordinates will be based
		/// </param>
		void UpdateDrawRegionsForSurface (DockySurface surface)
		{
			// first we do the math as if this is a top dock, to do this we need to set
			// up some "pretend" variables. we pretend we are a top dock because 0,0 is
			// at the top.
			int width = surface.Width;
			int height = surface.Height;
			double zoom;
			
			Gdk.Point cursor = LocalCursor;
			Gdk.Point localCursor = cursor;
			
			// "relocate" our cursor to be on the top
			switch (Position) {
			case DockPosition.Right:
				cursor.X = (Width - 1) - cursor.X;
				break;
			case DockPosition.Bottom:
				cursor.Y = (Height - 1) - cursor.Y;
				break;
			default:
				break;
			}
			
			if (Preferences.IsVertical) {
				int tmpY = cursor.Y;
				cursor.Y = cursor.X;
				cursor.X = tmpY;
				
				// our width and height switch around if we have a veritcal dock
				width = surface.Height;
				height = surface.Width;
			}
			
			// this offset is used to split the icons into left/right aligned for panel mode
			double panelanim = PanelModeToggleProgress;
			int panel_item_offset;
			
			if (Preferences.IsVertical)
				panel_item_offset = (monitor_geo.Height - DockWidth) / 2;
			else
				panel_item_offset = (monitor_geo.Width - DockWidth) / 2;
			
			if (panelanim >= 1) {
				if (!Preferences.PanelMode)
					panel_item_offset = 0;
			} else {
				if (Preferences.PanelMode)
					panel_item_offset = (int) (panel_item_offset * panelanim);
				else
					panel_item_offset -= (int) (panel_item_offset * panelanim);
			}
			
			// the line along the dock width about which the center of unzoomed icons sit
			int midline = DockHeight / 2;
			
			// the left most edge of the first dock item
			int startX = ((width - DockWidth) / 2) + DockWidthBuffer - panel_item_offset;
			
			Gdk.Point center = new Gdk.Point (startX, midline);
			
			// right align docklets
			bool rightAlign = (Items [0].Owner != Preferences.DefaultProvider && Items [0] != DockyItem);
			if (rightAlign)
				center.X += 2 * panel_item_offset;
			
			int index = 0;
			foreach (AbstractDockItem adi in Items) {
				// anything after the first separator is a docklet, and right aligned
				if (!rightAlign && adi is SeparatorItem) {
					rightAlign = true;
					center.X += 2 * panel_item_offset;
				}
				
				// used to handle remove animations
				if (remove_index != 0 && remove_index < index && remove_index > index - 1) {
					double removePercent = 1 - Math.Min (1, (DateTime.UtcNow - remove_time).TotalMilliseconds / BaseAnimationTime.TotalMilliseconds);
					if (removePercent == 0)
						remove_index = 0;
					else
						center.X += (int) ((remove_size + ItemWidthBuffer) * removePercent);
				}
				
				DrawValue val = new DrawValue ();
				int iconSize = IconSize;
				
				// div by 2 may result in rounding errors? Will this render OK? Shorts WidthBuffer by 1?
				double halfSize = iconSize / 2.0;
				
				if (!adi.Square) {
					DockySurface icon = adi.IconSurface (surface, iconSize, iconSize, VisibleDockHeight);
					halfSize = ((adi.RotateWithDock || !Preferences.IsVertical) ? icon.Width : icon.Height) / 2.0;
				}
				
				// animate adding new icon
				halfSize *= Math.Min (1, (DateTime.UtcNow - adi.AddTime).TotalMilliseconds / BaseAnimationTime.TotalMilliseconds);
				
				// center now represents our midpoint
				center.X += (int) Math.Floor (halfSize);
				val.StaticCenter = new PointD (center.X, center.Y);
				
				// get us some handy doubles with fancy names
				double cursorPosition = cursor.X;
				double centerPosition = center.X;
				
				// ZoomPercent is a number greater than 1.  It should never be less than one.
				
				// zoomInPercent is a range of 1 to ZoomPercent.
				// We need a number that is 1 when ZoomIn is 0, and ZoomPercent when ZoomIn is 1.
				// Then we treat this as if it were the ZoomPercent for the rest of the calculation.
				double zoomInPercent = 1 + (ZoomPercent - 1) * ZoomIn;
				
				double zoomSize = ZoomEnabled ? ZoomedIconSize : 2.0 * IconSize;
				
				// offset from the center of the true position, ranged between 0 and the zoom size
				double offset = Math.Min (Math.Abs (cursorPosition - centerPosition), zoomSize);
				
				double offsetPercent;
				if (ExternalDragActive && DragTracker.ProviderAcceptsDrop ()) {
					// Provide space for dropping between items
					offset += offset * zoomSize / IconSize;
					offsetPercent = Math.Min (1, offset / (zoomSize + ZoomedIconSize));
				} else {
					offsetPercent = offset / zoomSize;
				}
				
				if (offsetPercent > .99)
					offsetPercent = 1;
				
				// pull in our offset to make things less spaced out
				// explaination since this is a bit tricky...
				// we have three terms, basically offset = f(x) * h(x) * g(x)
				// f(x) == offset identity
				// h(x) == a number from 0 to DockPreference.ZoomPercent - 1.  This is used to get the smooth "zoom in" effect.
				//         additionally serves to "curve" the offset based on the max zoom
				// g(x) == a term used to move the ends of the zoom inward.  Precalculated that the edges should be 66% of the current
				//         value. The center is 100%. (1 - offsetPercent) == 0,1 distance from center
				// The .66 value comes from the area under the curve.  Dont ask me to explain it too much because it's too clever for me.
				
				// for external drags with no zoom, we pretend there is actually a zoom of 200%
				if (ExternalDragActive && ZoomPercent == 1 && DragTracker.ProviderAcceptsDrop ())
					offset *= ZoomIn / 2.0;
				else
					offset *= zoomInPercent - 1;
				offset *= 1 - offsetPercent / 3;
				
				if (cursorPosition > centerPosition)
					centerPosition -= offset;
				else
					centerPosition += offset;
				
				if (!adi.Zoom) {
					val.Zoom = 1;
					val.Center = new Cairo.PointD ((int) centerPosition, center.Y);
				} else {
					// zoom is calculated as 1 through target_zoom (default 2).  
					// The larger your offset, the smaller your zoom
					
					// First we get the point on our curve that defines our current zoom
					// offset is always going to fall on a point on the curve >= 0
					zoom = 1 - Math.Pow (offsetPercent, 2);
					
					// scale this to match our zoomInPercent
					zoom = 1 + zoom * (zoomInPercent - 1);
					
					double zoomedCenterHeight = DockHeightBuffer + (iconSize * zoom / 2.0);
					
					if (zoom == 1)
						centerPosition = Math.Round (centerPosition);
					
					val.Center = new Cairo.PointD (centerPosition, zoomedCenterHeight);
					val.Zoom = zoom;
				}
				
				// now we undo our transforms to the point
				if (Preferences.IsVertical) {
					double tmpY = val.Center.Y;
					val.Center.Y = val.Center.X;
					val.Center.X = tmpY;
					
					tmpY = val.StaticCenter.Y;
					val.StaticCenter.Y = val.StaticCenter.X;
					val.StaticCenter.X = tmpY;
				}
				
				switch (Position) {
				case DockPosition.Right:
					val.Center.X = (height - 1) - val.Center.X;
					val.StaticCenter.X = (height - 1) - val.StaticCenter.X;
					break;
				case DockPosition.Bottom:
					val.Center.Y = (height - 1) - val.Center.Y;
					val.StaticCenter.Y = (height - 1) - val.StaticCenter.Y;
					break;
				default:
					break;
				}
				
				Gdk.Rectangle hoverArea = DrawRegionForItemValue (adi, val, true);
				
				if (Preferences.IsVertical) {
					hoverArea.Inflate ((int) (ZoomedDockHeight * .3), ItemWidthBuffer / 2);
					hoverArea.Width += DockHeightBuffer;
				} else {
					hoverArea.Inflate (ItemWidthBuffer / 2, (int) (ZoomedDockHeight * .3));
					hoverArea.Height += DockHeightBuffer;
				}
				
				switch (Position) {
				case DockPosition.Right:
					hoverArea.X -= DockHeightBuffer;
					break;
				case DockPosition.Bottom:
					hoverArea.Y -= DockHeightBuffer;
					break;
				default:
					break;
				}
				
				val.HoverArea = hoverArea;
				DrawValues[adi] = val;

				// keep the hovereditem in mind, but don't change it while rendering
				if (hoverArea.Contains (localCursor) && !AutohideManager.Hidden && !(adi is SeparatorItem))
					next_hoveredItem = adi;
				
				if (update_screen_regions) {
					if (Menu.Visible || ConfigurationMode || Painter != null) {
						adi.SetScreenRegion (Screen, new Gdk.Rectangle (0, 0, 0, 0));
					} else {
						Gdk.Rectangle region = hoverArea;
						region.X += window_position.X;
						region.Y += window_position.Y;
						adi.SetScreenRegion (Screen, region);
					}
				}
				
				// move past midpoint to end of icon
				center.X += (int) Math.Ceiling (halfSize) + ItemWidthBuffer;
				
				index++;
			}
			
			update_screen_regions = false;
		}
		
		void UpdateScreenRegions ()
		{
			GLib.Timeout.Add (10 + (uint) Math.Max (BaseAnimationTime.TotalMilliseconds, SlideTime.TotalMilliseconds), delegate {
				update_screen_regions = true;
				AnimatedDraw ();
				return false;
			});
		}
		
		void UpdateMaxIconSize ()
		{
			if (Painter != null)
				return;
			
			if (icon_size_timer > 0)
				GLib.Source.Remove (icon_size_timer);
			
			icon_size_timer = GLib.Timeout.Add (1000, delegate {
				icon_size_timer = 0;

				int dockWidth = DockWidth;
				int maxWidth = Preferences.IsVertical ? monitor_geo.Height : monitor_geo.Width;
				
				if (dockWidth > maxWidth) {
					// MaxIconSize is too large, must fix
					MaxIconSize = Preferences.IconSize;
					while (dockWidth > maxWidth) {
						MaxIconSize--;
						dockWidth = DockWidth;
					}
				} else if (MaxIconSize < Preferences.IconSize) {
					// Perhaps MaxIconSize is too small, lets find out!
					while (dockWidth < maxWidth && MaxIconSize < Preferences.IconSize) {
						MaxIconSize++;
						dockWidth = DockWidth;
					}
				} else {
					MaxIconSize = Preferences.IconSize;
				}
				AnimatedDraw ();
				return false;
			});
		}
		
		Gdk.Rectangle StaticDockArea (DockySurface surface)
		{	
			Gdk.Rectangle area = Gdk.Rectangle.Zero;
			
			int dockWidth = Painter != null ? PainterDockWidth : DockWidth;
			
			switch (Position) {
			case DockPosition.Top:
				area = new Gdk.Rectangle ((surface.Width - dockWidth) / 2, 0, dockWidth, DockHeight);
				break;
			case DockPosition.Left:
				area = new Gdk.Rectangle (0, (surface.Height - dockWidth) / 2, DockHeight, dockWidth);
				break;
			case DockPosition.Right:
				area = new Gdk.Rectangle (surface.Width - DockHeight, (surface.Height - dockWidth) / 2, DockHeight, dockWidth);
				break;
			case DockPosition.Bottom:
				area = new Gdk.Rectangle ((surface.Width - dockWidth) / 2, surface.Height - DockHeight, dockWidth, DockHeight);
				break;
			}
			
			if (Preferences.PanelMode) {
				if (Preferences.IsVertical) {
					area.Y = 0;
					area.Height = surface.Height;
				} else {
					area.X = 0;
					area.Width = surface.Width;
				}
			}
			
			return area;
		}
		
		void GetDockAreaOnSurface (DockySurface surface, out Gdk.Rectangle dockArea, out Gdk.Rectangle cursorArea)
		{
			Gdk.Rectangle first, last, staticArea;
			
			first = DrawRegionForItem (Items[0]);
			last = DrawRegionForItem (Items[Items.Count - 1]);
			
			dockArea = new Gdk.Rectangle (0, 0, 0, 0);
			staticArea = StaticDockArea (surface);
			
			int dockStart;
			int dockWidth;
			if (Preferences.IsVertical) {
				dockStart = first.Y - DockWidthBuffer;
				dockWidth = (last.Y + last.Height + DockWidthBuffer) - dockStart;
			} else {
				dockStart = first.X - DockWidthBuffer;
				dockWidth = (last.X + last.Width + DockWidthBuffer) - dockStart;
			}
			
			// update the dock area to animate the panel toggle
			if (PanelModeToggleProgress < 1) {
				int difference = 2 * ((dockStart + dockWidth / 2) - ((Preferences.IsVertical ? monitor_geo.Height : monitor_geo.Width) / 2));
				// no left items
				if (Items [0].Owner != Preferences.DefaultProvider && Items [0] != DockyItem) {
					dockStart -= difference;
					dockWidth += difference;
				}
				// no right items
				if (Items [Items.Count - 1].Owner == Preferences.DefaultProvider ||
					Items [Items.Count - 1] == DockyItem ||
					Items [Items.Count - 1] is SpacingItem)
					dockWidth -= difference;
			}
			
			if (PainterOpacity > 0) {
				// we are in a transition state
				int difference = (int) ((dockWidth - PainterDockWidth) * PainterOpacity);
				dockWidth -= difference;
				dockStart += difference / 2;
			}
			
			int hotAreaSize;
			if ((!Preferences.FadeOnHide || Preferences.FadeOpacity == 0) && Painter == null && AutohideManager.Hidden && !ConfigurationMode) {
				hotAreaSize = 1;
			} else if (DockHovered && !ConfigurationMode) {
				hotAreaSize = ZoomedDockHeight + HotAreaPadding;
			} else {
				hotAreaSize = DockHeight;
			}
			
			switch (Position) {
			case DockPosition.Top:
				dockArea.X = dockStart;
				dockArea.Y = 0;
				dockArea.Width = dockWidth;
				dockArea.Height = DockHeight;
				
				cursorArea = new Gdk.Rectangle (staticArea.X,
				                                -window_position.Y,
				                                staticArea.Width,
				                                hotAreaSize + window_position.Y);
				break;
			case DockPosition.Left:
				dockArea.X = 0;
				dockArea.Y = dockStart;
				dockArea.Width = DockHeight;
				dockArea.Height = dockWidth;
				
				cursorArea = new Gdk.Rectangle (-window_position.X,
				                                staticArea.Y,
				                                hotAreaSize + window_position.X,
				                                staticArea.Height);
				break;
			case DockPosition.Right:
				dockArea.X = surface.Width - DockHeight;
				dockArea.Y = dockStart;
				dockArea.Width = DockHeight;
				dockArea.Height = dockWidth;
				
				cursorArea = new Gdk.Rectangle (dockArea.X + dockArea.Width - hotAreaSize,
				                                staticArea.Y,
				                                hotAreaSize + (Screen.Width - monitor_geo.Width),
				                                staticArea.Height);
				break;
			default:
			case DockPosition.Bottom:
				dockArea.X = dockStart;
				dockArea.Y = surface.Height - DockHeight;
				dockArea.Width = dockWidth;
				dockArea.Height = DockHeight;
				
				cursorArea = new Gdk.Rectangle (staticArea.X,
				                                dockArea.Y + dockArea.Height - hotAreaSize,
				                                staticArea.Width,
				                                hotAreaSize + (Screen.Height - monitor_geo.Height));
				break;
			}
		}
		
		void DrawDock (DockySurface surface)
		{
			surface.Clear ();
			
			UpdateDrawRegionsForSurface (surface);
			
			Gdk.Rectangle dockArea, cursorArea;
			GetDockAreaOnSurface (surface, out dockArea, out cursorArea);
			
			if (Preferences.PanelMode) {
				Gdk.Rectangle panelArea = dockArea;
				if (PanelModeToggleProgress == 1) {
					if (Preferences.IsVertical) {
						panelArea.Y = -100;
						panelArea.Height = Height + 200;
					} else {
						panelArea.X = -100;
						panelArea.Width = Width + 200;
					}
				}
				DrawDockBackground (surface, panelArea);
			} else {
				DrawDockBackground (surface, dockArea);
			}
				
			double painterVisibility = PainterOpacity;
			if (painterVisibility < 1) {
			
				if (icon_buffer == null || icon_buffer.Width != surface.Width || icon_buffer.Height != surface.Height) {
					if (icon_buffer != null)
						icon_buffer.Dispose ();
					icon_buffer = new DockySurface (surface.Width, surface.Height, surface);
				}
				
				icon_buffer.Clear ();
				foreach (AbstractDockItem adi in Items)
					DrawItem (icon_buffer, dockArea, adi);
			
				surface.Context.SetSource (icon_buffer.Internal, 0, 0);
				surface.Context.PaintWithAlpha (1 - painterVisibility);
			
			} 
			
			if (Painter != null && painterVisibility > 0)
				DrawPainter (surface, dockArea);
			
			if (ActiveGlow) {
				Gdk.Color color = Style.BaseColors[(int) Gtk.StateType.Selected];
				
				Gdk.Rectangle extents;
				using (DockySurface tmp = surface.CreateMask (0, out extents)) {
					extents.Inflate (GlowSize * 2, GlowSize * 2);
					tmp.ExponentialBlur (GlowSize, extents);
					tmp.Context.Color = new Cairo.Color (
						(double) color.Red / ushort.MaxValue, 
						(double) color.Green / ushort.MaxValue, 
						(double) color.Blue / ushort.MaxValue, 
						.90).SetValue (1).MultiplySaturation (4);
					tmp.Context.Operator = Operator.Atop;
					tmp.Context.Paint ();
				
					surface.Context.Operator = Operator.DestOver;
					surface.Context.SetSource (tmp.Internal);
					surface.Context.Paint ();
					surface.Context.Operator = Operator.Over;
				}
			}
			
			if (DockOpacity < 1)
				SetDockOpacity (surface);
			
			// Draw UrgentGlow which is visible when Docky is hidden and an item need attention
			if (AutohideManager.Hidden && !ConfigurationMode && (!Preferences.FadeOnHide || Preferences.FadeOpacity == 0)) {
				foreach (AbstractDockItem adi in Items) {
					double diff = (render_time - adi.StateSetTime (ItemState.Urgent)).TotalMilliseconds;
					if (adi.Indicator != ActivityIndicator.None && (adi.State & ItemState.Urgent) == ItemState.Urgent &&
					    (DockServices.Theme.GlowTime.Days > 0 || diff < DockServices.Theme.GlowTime.TotalMilliseconds)) {
						
						if (urgent_glow_buffer == null)
							urgent_glow_buffer = CreateUrgentGlowBuffer ();
		
						DrawValue val = DrawValues [adi];
						DrawValue glowloc;
						if (Preferences.FadeOnHide)
							glowloc = val.MoveIn (Position, -urgent_glow_buffer.Height / 2 + DockHeightBuffer);
						else
							glowloc = val.MoveIn (Position, -urgent_glow_buffer.Height / 2 + DockHeightBuffer + ZoomedDockHeight);

						double opacity = 0.2 + (0.75 * (Math.Sin (diff / PulseTime.TotalMilliseconds * 2 * Math.PI) + 1) / 2);
						
						urgent_glow_buffer.ShowWithOptions (surface, glowloc.Center, 1, 0, opacity);
					}
				}
			}
			
			SetInputMask (cursorArea);
			
			Gdk.Rectangle staticArea = StaticDockArea (surface);
			staticArea.X += window_position.X;
			staticArea.Y += window_position.Y;
			staticArea.Intersect (monitor_geo);
			AutohideManager.SetIntersectArea (staticArea);
			
			cursorArea.X += window_position.X;
			cursorArea.Y += window_position.Y;
			AutohideManager.SetCursorArea (cursorArea);
		}
		
		void DrawPainter (DockySurface surface, Gdk.Rectangle dockArea)
		{
			double painterOpacity = PainterOpacity;
			
			if (painter_buffer == null || painter_buffer.Width != surface.Width || painter_buffer.Height != surface.Height) {
				if (painter_buffer != null)
					painter_buffer.Dispose ();
				painter_buffer = new DockySurface (surface.Width, surface.Height, surface);
				repaint_painter = true;
			}
			
			if (repaint_painter || painterOpacity != 1) {
				painter_buffer.ResetContext ();
				painter_buffer.Clear ();
				
				DockySurface painterSurface = Painter.GetSurface (surface);
			
				if (Preferences.IsVertical)
					painter_area = new Gdk.Rectangle (dockArea.X + DockWidthBuffer,
						dockArea.Y + PainterBufferSize - DockWidthBuffer,
						painterSurface.Width,
						painterSurface.Height);
				else
					painter_area = new Gdk.Rectangle (dockArea.X + PainterBufferSize - DockWidthBuffer,
						dockArea.Y + DockWidthBuffer,
						painterSurface.Width,
						painterSurface.Height);
				
				painterSurface.Internal.Show (painter_buffer.Context, painter_area.X, painter_area.Y);
			
				int offset = IconSize + DockHeightBuffer;
				if (dockArea.Height > 2 * IconSize) {
					if (Preferences.IsVertical)
						offset += (dockArea.Width - 2 * IconSize) / 2;
					else
						offset += (dockArea.Height - 2 * IconSize) / 2;
				}
				
				PointD center;
				switch (Position) {
				default:
				case DockPosition.Top:
					center = new PointD (dockArea.X + IconSize + DockWidthBuffer,
						dockArea.Y + offset);
					break;
				case DockPosition.Bottom:
					center = new PointD (dockArea.X + IconSize + DockWidthBuffer,
						dockArea.Y + dockArea.Height - offset);
					break;
				case DockPosition.Left:
					center = new PointD (dockArea.X + offset,
						dockArea.Y + IconSize + DockWidthBuffer);
					break;
				case DockPosition.Right:
					center = new PointD (dockArea.X + dockArea.Width - offset,
						dockArea.Y + IconSize + DockWidthBuffer);
					break;
				}
				
				DockySurface icon = painterOwner.IconSurface (painter_buffer, 2 * IconSize, IconSize, VisibleDockHeight);
				icon.ShowWithOptions (painter_buffer, center, 1, 0, 1);
				
				repaint_painter = false;
			}
			
			surface.Context.SetSource (painter_buffer.Internal, 0, 0);
			surface.Context.PaintWithAlpha (painterOpacity);
			
			// Ensure we repaint the next time around
			if (painterOpacity < 1)
				repaint_painter = true;
		}
		
		void SetDockOpacity (DockySurface surface)
		{
			if (!Preferences.FadeOnHide)
				return;
			
			surface.Context.Save ();
			
			surface.Context.Color = new Cairo.Color (0, 0, 0, 0);
			surface.Context.Operator = Operator.Source;
			surface.Context.PaintWithAlpha (1 - DockOpacity);
			
			surface.Context.Restore ();
		}
		
		void DrawItem (DockySurface surface, Gdk.Rectangle dockArea, AbstractDockItem item)
		{
			if (DragTracker.DragItem == item)
				return;
			
			double zoomOffset = ZoomedIconSize / (double) IconSize;
			
			DrawValue val = DrawValues [item];
			
			//create slide animation by adjusting DrawValue before drawing
			//we could handle longer distances, but 1 is enough
			if ((render_time - item.StateSetTime (ItemState.Move)) < SlideTime
			    && Math.Abs(item.LastPosition - item.Position) == 1 ) {
				
				double slideProgress = (render_time - item.StateSetTime (ItemState.Move)).TotalMilliseconds / SlideTime.TotalMilliseconds;

				double move = (item.Position - item.LastPosition) * (IconSize * val.Zoom + ItemWidthBuffer) 
					//draw the animation backwards cause item has already moved
					* (1 - slideProgress);
                
				if (Position == DockPosition.Top || Position == DockPosition.Left)
					move *= -1;
				
				val = val.MoveRight (Position, move);
			}

			DrawValue center = val;
			
			double clickAnimationProgress = 0;
			double lighten = 0;
			double darken = 0;
			
			if ((render_time - item.LastClick) < BounceTime) {
				clickAnimationProgress = (render_time - item.LastClick).TotalMilliseconds / BounceTime.TotalMilliseconds;
			
				switch (item.ClickAnimation) {
				case ClickAnimation.Bounce:
					if (!Gdk.Screen.Default.IsComposited)
						break;
					double move = Math.Abs (Math.Sin (2 * Math.PI * clickAnimationProgress) * LaunchBounceHeight);
					center = center.MoveIn (Position, move);
					break;
				case ClickAnimation.Darken:
					darken = Math.Max (0, Math.Sin (Math.PI * 2 * clickAnimationProgress)) * .5;
					break;
				case ClickAnimation.Lighten:
					lighten = Math.Max (0, Math.Sin (Math.PI * 2 * clickAnimationProgress)) * .5;
					break;
				}
			}
			
			if (HoveredAcceptsDrop && HoveredItem == item && ExternalDragActive)
				lighten += .4;
			else if (!ZoomEnabled && !Menu.Visible && HoveredItem == item && !ExternalDragActive && !InternalDragActive && !ConfigurationMode)
				lighten += .2;
			
			if ((item.State & ItemState.Wait) != 0)
				darken += .5;
			else if (Menu.Visible && HoveredItem == item)
				darken += .4;
			
			if (ExternalDragActive && DragTracker.DragData != null && DockHovered && !item.CanAcceptDrop (DragTracker.DragData))
				darken += .6;
			
			if (Gdk.Screen.Default.IsComposited &&
				(item.State & ItemState.Urgent) == ItemState.Urgent && 
				(render_time - item.StateSetTime (ItemState.Urgent)) < BounceTime) {
				double urgentProgress = (render_time - item.StateSetTime (ItemState.Urgent)).TotalMilliseconds / BounceTime.TotalMilliseconds;
				
				double move = Math.Abs (Math.Sin (Math.PI * urgentProgress) * UrgentBounceHeight);
				center = center.MoveIn (Position, move);
			}
			
			double opacity = Math.Max (0, Math.Min (1, (render_time - item.AddTime).TotalMilliseconds / BaseAnimationTime.TotalMilliseconds));
			opacity = Math.Pow (opacity, 2);
			DockySurface icon;
			
			double renderZoom = 1, renderRotation = 0;
			
			if (item.RotateWithDock)
				switch (Position) {
				case DockPosition.Top:
					renderRotation = Math.PI;
					break;
				case DockPosition.Left:
					renderRotation = Math.PI * .5;
					break;
				case DockPosition.Right:
					renderRotation = Math.PI * 1.5;
					break;
				default:
				case DockPosition.Bottom:
					renderRotation = 0;
					break;
				}

			if (item.Zoom && !(item.ScalableRendering && center.Zoom == 1)) {
				icon = item.IconSurface (surface, ZoomedIconSize, IconSize, VisibleDockHeight);
				renderZoom = center.Zoom / zoomOffset;
			} else {
				icon = item.IconSurface (surface, IconSize, IconSize, VisibleDockHeight);
			}
			
			// The big expensive paint happens right here!
			if (ThreeDimensional) {
				if (item is SeparatorItem) {
					center = center.MoveIn (Position, -DockHeightBuffer);
				} else {
					double offset = 2 * Math.Max (Math.Abs (val.Center.X - center.Center.X), Math.Abs (val.Center.Y - center.Center.Y));
					offset -= .07 * IconSize * renderZoom;
					icon.ShowAsReflection (surface, center.Center, renderZoom, renderRotation, opacity, offset, Position);
				}
			}
			icon.ShowWithOptions (surface, center.Center, renderZoom, renderRotation, opacity);
			
			// glow the icon
			if (lighten > 0) {
				surface.Context.Operator = Operator.Add;
				icon.ShowWithOptions (surface, center.Center, renderZoom, renderRotation, opacity * lighten);
				surface.Context.Operator = Operator.Over;
			}
			
			// darken the icon
			if (darken > 0) {
				Gdk.Rectangle area = DrawRegionForItemValue (item, center);
				surface.Context.Rectangle (area.X, area.Y, area.Width, area.Height);
				
				surface.Context.Color = new Cairo.Color (0, 0, 0, darken);
				
				surface.Context.Operator = Operator.Atop;
				surface.Context.Fill ();
				surface.Context.Operator = Operator.Over;
			}
			
			// draw the active glow
			if ((item.State & ItemState.Active) == ItemState.Active && !ThreeDimensional) {
				Gdk.Rectangle area;
				
				if (Preferences.IsVertical)
					area = new Gdk.Rectangle (
						dockArea.X, (int) (val.Center.Y - (IconSize * val.Zoom) / 2) - ItemWidthBuffer / 2,
						DockHeight, (int) (IconSize * val.Zoom) + ItemWidthBuffer);
				else
					area = new Gdk.Rectangle (
						(int) (val.Center.X - (IconSize * val.Zoom) / 2) - ItemWidthBuffer / 2, dockArea.Y, 
						(int) (IconSize * val.Zoom) + ItemWidthBuffer, DockHeight);
				
				surface.Context.Operator = Operator.DestOver;
				DrawActiveIndicator (surface, area, item.AverageColor (), opacity);
				surface.Context.Operator = Operator.Over;
			}
			
			// draw the wait spinner
			if ((item.State & ItemState.Wait) != 0) {
				if (wait_buffer == null)
					wait_buffer = CreateWaitBuffer ();
				
				int rotate = ((int) ((DateTime.UtcNow - item.StateSetTime (ItemState.Wait)).TotalMilliseconds / 80)) % (WaitArmsPerGroup * WaitGroups);
				wait_buffer.ShowWithOptions (surface, center.Center, center.Zoom, rotate * 2 * Math.PI / (WaitArmsPerGroup * WaitGroups), 1);
			}
			
			// draw the normal/urgent indicator(s)
			if (item.Indicator != ActivityIndicator.None) {
				if (normal_indicator_buffer == null)
					normal_indicator_buffer = CreateNormalIndicatorBuffer ();
				if (urgent_indicator_buffer == null)
					urgent_indicator_buffer = CreateUrgentIndicatorBuffer ();
				
				DrawValue loc = val.MoveIn (Position, 1 - IconSize * val.Zoom / 2 - DockHeightBuffer);
				
				DockySurface indicator = (item.State & ItemState.Urgent) == ItemState.Urgent ? urgent_indicator_buffer : normal_indicator_buffer;
				
				if (item.Indicator == ActivityIndicator.Single || !Preferences.IndicateMultipleWindows) {
					indicator.ShowWithOptions (surface, loc.Center, 1, 0, 1);
				} else {
					indicator.ShowWithOptions (surface, loc.MoveRight (Position, 3).Center, 1, 0, 1);
					indicator.ShowWithOptions (surface, loc.MoveRight (Position, -3).Center, 1, 0, 1);
				}
			}
		}
		
		void DrawActiveIndicator (DockySurface surface, Gdk.Rectangle area, Cairo.Color color, double opacity)
		{
			surface.Context.Rectangle (area.X, area.Y, area.Width, area.Height);
			LinearGradient lg;
			
			switch (Position) {
			case DockPosition.Top:
				lg = new LinearGradient (0, area.Y, 0, area.Y + area.Height);
				break;
			case DockPosition.Left:
				lg = new LinearGradient (area.X, 0, area.X + area.Width, 0);
				break;
			case DockPosition.Right:
				lg = new LinearGradient (area.X + area.Width, 0, area.X, 0);
				break;
			default:
			case DockPosition.Bottom:
				lg = new LinearGradient (0, area.Y + area.Height, 0, area.Y);
				break;
			}
			lg.AddColorStop (0, color.SetAlpha (0.6 * opacity));
			lg.AddColorStop (1, color.SetAlpha (0.0));
			
			surface.Context.Pattern = lg;
			surface.Context.Fill ();
			
			lg.Destroy ();
		}
		
		DockySurface CreateWaitBuffer ()
		{
			DockySurface surface = new DockySurface (IconSize, IconSize, background_buffer);
			surface.Clear ();
			
			surface.Context.Color = new Cairo.Color (0, 0, 0, 0);
			surface.Context.Operator = Operator.Source;
			surface.Context.PaintWithAlpha (0.5);
			
			double baseLog = Math.Log (WaitArmsPerGroup + 1);
			int size = Math.Min (surface.Width, surface.Height);
			
			// Ensure that LineWidth is a multiple of 2 for nice drawing
			surface.Context.LineWidth = (int) Math.Max (1, size / 40.0);
			surface.Context.LineCap = LineCap.Round;
			
			surface.Context.Translate (surface.Width / 2, surface.Height / 2);
			
			if (surface.Context.LineWidth % 2 == 1)
				surface.Context.Translate (.5, .5);
			
			Gdk.Color color = Style.Backgrounds [(int) Gtk.StateType.Selected].SetMinimumValue (100);
			Cairo.Color baseColor = new Cairo.Color ((double) color.Red / ushort.MaxValue,
										(double) color.Green / ushort.MaxValue,
										(double) color.Blue / ushort.MaxValue,
										1);
			
			for (int i = 0; i < WaitArmsPerGroup * WaitGroups; i++) {
				int position = 1 + (i % WaitArmsPerGroup);
				surface.Context.Color = baseColor.SetAlpha (1 - Math.Log (position) / baseLog);
				surface.Context.MoveTo (0, size / 8);
				surface.Context.LineTo (0, size / 4);
				surface.Context.Rotate (-2 * Math.PI / (WaitArmsPerGroup * WaitGroups));
				surface.Context.Stroke ();
			}
			
			return surface;
		}
		
		DockySurface CreateNormalIndicatorBuffer ()
		{
			// FIXME: create gconf value
			Gdk.Color gdkColor = Style.Backgrounds [(int) StateType.Selected].SetMinimumValue (90);
			Cairo.Color color = new Cairo.Color ((double) gdkColor.Red / ushort.MaxValue,
											(double) gdkColor.Green / ushort.MaxValue,
											(double) gdkColor.Blue / ushort.MaxValue,
											1.0);
			return CreateIndicatorBuffer (NormalIndicatorSize, color.MinimumSaturation (0.4));
		}
		
		DockySurface CreateUrgentIndicatorBuffer ()
		{
			// FIXME: create gconf value
			Gdk.Color gdkColor = Style.Backgrounds [(int) StateType.Selected].SetMinimumValue (90);
			Cairo.Color color = new Cairo.Color ((double) gdkColor.Red / ushort.MaxValue,
											(double) gdkColor.Green / ushort.MaxValue,
											(double) gdkColor.Blue / ushort.MaxValue,
											1.0);
			return CreateIndicatorBuffer (UrgentIndicatorSize, color.AddHue (DockServices.Theme.UrgentHueShift).SetSaturation (1));
		}
		
		DockySurface CreateIndicatorBuffer (int size, Cairo.Color color)
		{
			DockySurface surface = new DockySurface (size, size, background_buffer);
			surface.Clear ();
			
			Cairo.Context cr = surface.Context;
			
			double x = size / 2;
			double y = x;
				
			cr.MoveTo (x, y);
			cr.Arc (x, y, size / 2, 0, Math.PI * 2);
				
			RadialGradient rg = new RadialGradient (x, y, 0, x, y, size / 2);
			rg.AddColorStop (0, new Cairo.Color (1, 1, 1, 1));
			rg.AddColorStop (.10, color.SetAlpha (1.0));
			rg.AddColorStop (.20, color.SetAlpha (.60));
			rg.AddColorStop (.25, color.SetAlpha (.25));
			rg.AddColorStop (.50, color.SetAlpha (.15));
			rg.AddColorStop (1.0, color.SetAlpha (0.0));
			
			cr.Pattern = rg;
			cr.Fill ();
			rg.Destroy ();
			
			return surface;
		}

		DockySurface CreateUrgentGlowBuffer ()
		{
			// FIXME: create gconf value
			Gdk.Color gdkColor = Style.Backgrounds [(int) StateType.Selected].SetMinimumValue (90);
			Cairo.Color color = new Cairo.Color ((double) gdkColor.Red / ushort.MaxValue,
											(double) gdkColor.Green / ushort.MaxValue,
											(double) gdkColor.Blue / ushort.MaxValue,
											1.0);
			color = color.AddHue (DockServices.Theme.UrgentHueShift).SetSaturation (1);
			
			int size = (int) 2.5 * IconSize;
			
			DockySurface surface = new DockySurface (size, size, background_buffer);
			surface.Clear ();
			
			Cairo.Context cr = surface.Context;
			
			double x = size / 2;
			double y = x;
				
			cr.MoveTo (x, y);
			cr.Arc (x, y, size / 2, 0, Math.PI * 2);
				
			RadialGradient rg = new RadialGradient (x, y, 0, x, y, size / 2);
			rg.AddColorStop (0, new Cairo.Color (1, 1, 1, 1));
			rg.AddColorStop (.33, color.SetAlpha (.66));
			rg.AddColorStop (.66, color.SetAlpha (.33));
			rg.AddColorStop (1.0, color.SetAlpha (0.0));
			
			cr.Pattern = rg;
			cr.Fill ();
			rg.Destroy ();
			
			return surface;
		}

		void DrawDockBackground (DockySurface surface, Gdk.Rectangle backgroundArea)
		{
			if (background_buffer == null) {
				if (Preferences.IsVertical)
					background_buffer = new DockySurface (BackgroundHeight, BackgroundWidth, surface);
				else
					background_buffer = new DockySurface (BackgroundWidth, BackgroundHeight, surface);
				
				// FIXME we should probably compute if the theme is transparent, but for now this works
				if (ConfigurationMode && DockServices.Theme.DockTheme.Equals ("Transparent")) {
					background_buffer.Context.Rectangle (0, 0, background_buffer.Width, background_buffer.Height);
					background_buffer.Context.Color = new Cairo.Color (1, 1, 1, Items.Where (adi => !(adi is SpacingItem)).Count () == 0 ? 0.10 : 0.04);
					background_buffer.Context.Fill ();
				}
				
				Gdk.Pixbuf background = DockServices.Drawing.LoadIcon (ThreeDimensional ? DockServices.Theme.Background3dSvg : DockServices.Theme.BackgroundSvg);
				Gdk.Pixbuf tmp;
				
				switch (Position) {
				case DockPosition.Top:
					tmp = background.RotateSimple (PixbufRotation.Upsidedown);
					background.Dispose ();
					background = tmp;
					break;
				case DockPosition.Left:
					tmp = background.RotateSimple (PixbufRotation.Clockwise);
					background.Dispose ();
					background = tmp;
					break;
				case DockPosition.Right:
					tmp = background.RotateSimple (PixbufRotation.Counterclockwise);
					background.Dispose ();
					background = tmp;
					break;
				case DockPosition.Bottom:
					;
					break;
				}
				
				Gdk.CairoHelper.SetSourcePixbuf (background_buffer.Context, background, 0, 0);
				background_buffer.Context.Paint ();
				
				background.Dispose ();
			}
			
			double tilt = .6 - (double) DockHeightBuffer / (double) backgroundArea.Height;
			tilt *= 1 - PainterOpacity;
			double tiltanim = Math.Min (1, ((rendering ? render_time : DateTime.UtcNow) - threedimensional_change_time).TotalMilliseconds / BaseAnimationTime.TotalMilliseconds);
			tilt *= ThreeDimensional ? tiltanim : 1 - tiltanim;
			background_buffer.TileOntoSurface (surface, backgroundArea, 50, tilt, Position);
		}
		
		protected override void OnStyleSet (Style previous_style)
		{
			if (GdkWindow != null)
				GdkWindow.SetBackPixmap (null, false);
			foreach (AbstractDockItem adi in Items)
				adi.SetStyle (Style);
			if (Painter != null)
				Painter.SetStyle (Style);
			base.OnStyleSet (previous_style);
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!IsRealized || !Items.Any ()) {
				SetInputMask (new Gdk.Rectangle (0, 0, 1, 1));
				return true;
			}
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				render_time = DateTime.UtcNow;
				rendering = true;
				zoom_in_buffer = null;
				
				if (main_buffer == null || main_buffer.Width != Width || main_buffer.Height != Height) {
					if (main_buffer != null)
						main_buffer.Dispose ();
					main_buffer = new DockySurface (Width, Height, cr.Target);
				}
				
				DrawDock (main_buffer);
				cr.Operator = Operator.Source;
				
				if (Preferences.FadeOnHide) {
					cr.SetSource (main_buffer.Internal);
				} else {
					switch (Position) {
					case DockPosition.Top:
						cr.SetSource (main_buffer.Internal, 0, -HideOffset * ZoomedDockHeight);
						break;
					case DockPosition.Left:
						cr.SetSource (main_buffer.Internal, -HideOffset * ZoomedDockHeight, 0);
						break;
					case DockPosition.Right:
						cr.SetSource (main_buffer.Internal, HideOffset * ZoomedDockHeight, 0);
						break;
					case DockPosition.Bottom:
						cr.SetSource (main_buffer.Internal, 0, HideOffset * ZoomedDockHeight);
						break;
					}
				}
				
				cr.Paint ();

				(cr.Target as IDisposable).Dispose ();

				rendering = false;
				
				//now after rendering we can set the new HoveredItem
				HoveredItem = next_hoveredItem;
				next_hoveredItem = null;
			}
			
			if (AutohideManager.StartupMode)
				GLib.Timeout.Add ((uint) (2 * SlideTime.TotalMilliseconds), delegate {
					AutohideManager.StartupMode = false;
					return false;
				});
			
			return false;
		}

		#endregion
		
		#region XServer Related
		
		void SetInputMask (Gdk.Rectangle area)
		{
			if (!IsRealized || current_mask_area == area)
				return;

			current_mask_area = area;
			if (area.Width == 0 || area.Height == 0) {
				InputShapeCombineMask (null, 0, 0);
				return;
			}

			using (Gdk.Pixmap pixmap = new Gdk.Pixmap (null, area.Width, area.Height, 1)) {
				using (Cairo.Context cr = Gdk.CairoHelper.Create (pixmap)) {
					cr.Color = new Cairo.Color (0, 0, 0, 1);
					cr.Paint ();
					
					InputShapeCombineMask (pixmap, area.X, area.Y);
					(cr.Target as IDisposable).Dispose ();
				}
			}
		}
		
		void SetStruts ()
		{
			if (!IsRealized) return;
			
			X11Atoms atoms = X11Atoms.Instance;
			
			IntPtr [] struts = new IntPtr [12];
			
			if (Autohide == AutohideType.None) {
				switch (Position) {
				case DockPosition.Top:
					struts [(int) Struts.Top] = (IntPtr) (DockHeight + monitor_geo.Y);
					struts [(int) Struts.TopStart] = (IntPtr) monitor_geo.X;
					struts [(int) Struts.TopEnd] = (IntPtr) (monitor_geo.X + monitor_geo.Width - 1);
					break;
				case DockPosition.Left:
					struts [(int) Struts.Left] = (IntPtr) (monitor_geo.X + DockHeight);
					struts [(int) Struts.LeftStart] = (IntPtr) monitor_geo.Y;
					struts [(int) Struts.LeftEnd] = (IntPtr) (monitor_geo.Y + monitor_geo.Height - 1);
					break;
				case DockPosition.Right:
					struts [(int) Struts.Right] = (IntPtr) (DockHeight + (Screen.Width - (monitor_geo.X + monitor_geo.Width)));
					struts [(int) Struts.RightStart] = (IntPtr) monitor_geo.Y;
					struts [(int) Struts.RightEnd] = (IntPtr) (monitor_geo.Y + monitor_geo.Height - 1);
					break;
				case DockPosition.Bottom:
					struts [(int) Struts.Bottom] = (IntPtr) (DockHeight + (Screen.Height - (monitor_geo.Y + monitor_geo.Height)));
					struts [(int) Struts.BottomStart] = (IntPtr) monitor_geo.X;
					struts [(int) Struts.BottomEnd] = (IntPtr) (monitor_geo.X + monitor_geo.Width - 1);
					break;
				}
			}
			
			IntPtr [] first_struts = new [] { struts [0], struts [1], struts [2], struts [3] };

			X.XChangeProperty (GdkWindow, atoms._NET_WM_STRUT_PARTIAL, atoms.XA_CARDINAL,
			                      (int) PropertyMode.PropModeReplace, struts);
			
			X.XChangeProperty (GdkWindow, atoms._NET_WM_STRUT, atoms.XA_CARDINAL, 
			                      (int) PropertyMode.PropModeReplace, first_struts);
		}
		#endregion
		
		public override void Dispose ()
		{
			if (size_request_timer > 0) {
				GLib.Source.Remove (size_request_timer);
				size_request_timer = 0;
			}
			if (animation_timer > 0) {
				GLib.Source.Remove (animation_timer);
				animation_timer = 0;
			}
			if (icon_size_timer > 0) {
				GLib.Source.Remove (icon_size_timer);
				icon_size_timer = 0;
			}

			if (Menu != null)
				Menu.Dispose ();
			
			AutohideManager.Dispose ();
			UnregisterPreferencesEvents (Preferences);
			
			TextManager.Dispose ();
			DragTracker.Dispose ();
			
			CursorTracker.CursorPositionChanged -= HandleCursorPositionChanged;
			AutohideManager.HiddenChanged       -= HandleHiddenChanged;
			AutohideManager.DockHoveredChanged  -= HandleDockHoveredChanged;
			Screen.SizeChanged                  -= ScreenSizeChanged;
			Wnck.Screen.Default.WindowOpened    -= HandleWindowOpened;
			Realized                            -= HandleRealized;
			DockServices.Theme.ThemeChanged     -= DockyControllerThemeChanged;
			
			// clear out our separators
			foreach (AbstractDockItem adi in Items.Where (adi => adi is INonPersistedItem && adi != DockyItem)) {
				DrawValues.Remove (adi);
				adi.Dispose ();
			}
			
			ResetBuffers ();
			
			Hide ();
			Destroy ();
			base.Dispose ();
		}
	}
}
