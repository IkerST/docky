//  
//  Copyright (C) 2009 Jason Smith
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
using System.Text;

using Cairo;
using Gdk;

using Docky.CairoHelper;
using Docky.Services;

namespace Docky.Painters
{
	public class AbstractDockPainter : IDisposable
	{
		DockySurface surface;
		bool paint_needed;
		
		public event EventHandler PaintNeeded;
		public event EventHandler HideRequest;
		
		public virtual bool SupportsVertical {
			get { return false; }
		}
		
		public bool IsVertical { get; set; }
		
		public virtual int MinimumHeight {
			get { return 0; }
		}
		
		public virtual int MinimumWidth {
			get { return 400; }
		}
		
		public Gdk.Rectangle Allocation { get; private set; }
		
		protected Gtk.Style Style { get; private set; }
		
		protected AbstractDockPainter ()
		{
			paint_needed = true;
		}
		
		public void SetAllocation (Gdk.Rectangle allocation)
		{
			if (allocation == Allocation)
				return;
			
			Allocation = allocation;
			try {
				OnAllocationSet (allocation);
			} catch (Exception e) {
				Log<AbstractDockPainter>.Error (e.Message);
				Log<AbstractDockPainter>.Debug (e.StackTrace);
			}
		}
		
		protected virtual void OnAllocationSet (Gdk.Rectangle allocation)
		{
		}
		
		public void SetStyle (Gtk.Style style)
		{
			Style = style;
			try {
				OnStyleSet (style);
			} catch (Exception e) {
				Log<AbstractDockPainter>.Error (e.Message);
				Log<AbstractDockPainter>.Debug (e.StackTrace);
			}
			QueueRepaint ();
		}
		
		protected virtual void OnStyleSet (Gtk.Style style)
		{
		}
		
		public DockySurface GetSurface (DockySurface similar)
		{
			if (surface == null || surface.Width != Allocation.Width || surface.Height != Allocation.Height) {
				ResetBuffer ();
				surface = new DockySurface (Allocation.Width, Allocation.Height, similar);
			}
			
			if (paint_needed) {
				PaintSurface (surface);
				paint_needed = false;
			}
			
			return surface;
		}
		
		protected virtual void PaintSurface (DockySurface surface)
		{
		}
		
		public void Shown ()
		{
			try {
				OnShown ();
			} catch (Exception e) {
				Log<AbstractDockPainter>.Error (e.Message);
				Log<AbstractDockPainter>.Debug (e.StackTrace);
			}
		}
		
		protected virtual void OnShown ()
		{
		}
		
		public void Hidden ()
		{
			try {
				OnHidden ();
			} catch (Exception e) {
				Log<AbstractDockPainter>.Error (e.Message);
				Log<AbstractDockPainter>.Debug (e.StackTrace);
			}
			ResetBuffer ();
		}
		
		protected virtual void OnHidden ()
		{
		}
		
		public void MotionNotify (int x, int y, Gdk.ModifierType mod)
		{
			try {
				OnMotionNotify (x, y, mod);
			} catch (Exception e) {
				Log<AbstractDockPainter>.Error (e.Message);
				Log<AbstractDockPainter>.Debug (e.StackTrace);
			}
		}
		
		internal virtual void OnMotionNotify (int x, int y, Gdk.ModifierType mod)
		{
		}
		
		public void ButtonPressed (int x, int y, Gdk.ModifierType mod)
		{
			try {
				OnButtonPressed (x, y, mod);
			} catch (Exception e) {
				Log<AbstractDockPainter>.Error (e.Message);
				Log<AbstractDockPainter>.Debug (e.StackTrace);
			}
		}
		
		protected virtual void OnButtonPressed (int x, int y, Gdk.ModifierType mod)
		{
		}
		
		public void ButtonReleased (int x, int y, Gdk.ModifierType mod)
		{
			try {
				OnButtonReleased (x, y, mod);
			} catch (Exception e) {
				Log<AbstractDockPainter>.Error (e.Message);
				Log<AbstractDockPainter>.Debug (e.StackTrace);
			}
		}
		
		protected virtual void OnButtonReleased (int x, int y, Gdk.ModifierType mod)
		{
			Hide ();
		}
		
		public void Scrolled (ScrollDirection direction, int x, int y, Gdk.ModifierType mod)
		{
			try {
				OnScrolled (direction, x, y, mod);
			} catch (Exception e) {
				Log<AbstractDockPainter>.Error (e.Message);
				Log<AbstractDockPainter>.Debug (e.StackTrace);
			}
		}
		
		protected virtual void OnScrolled (ScrollDirection direction, int x, int y, Gdk.ModifierType type)
		{
		}
		
		protected void Hide ()
		{
			if (HideRequest != null)
				HideRequest (this, EventArgs.Empty);
		}
		
		protected void QueueRepaint ()
		{
			paint_needed = true;
			if (PaintNeeded != null)
				PaintNeeded (this, EventArgs.Empty);
		}
		
		void ResetBuffer ()
		{
			if (surface != null) {
				surface.Dispose ();
				surface = null;
			}
		}
		
		#region IDisposable implementation
		public virtual void Dispose ()
		{
			ResetBuffer ();
		}
		#endregion
	}
}
