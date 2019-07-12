//
// SearchEntry.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
 
using System;
using Gtk;

using Docky.Services;

namespace Docky.Widgets
{
    [System.ComponentModel.ToolboxItem(true)]
    public class SearchEntry : EventBox
    {
        private HBox box;
        private Entry entry;
        private HoverImageButton filter_button;
        private HoverImageButton clear_button;

        private Menu menu;
        private int active_filter_id = -1;

        private uint changed_timeout_id = 0;
        
        private string empty_message;
        private bool ready = false;

        private event EventHandler filter_changed;
        private event EventHandler entry_changed;

        public event EventHandler Changed {
            add { entry_changed += value; }
            remove { entry_changed -= value; }
        }

        public event EventHandler Activated {
            add { entry.Activated += value; }
            remove { entry.Activated -= value; }
        }

        public event EventHandler FilterChanged {
            add { filter_changed += value; }
            remove { filter_changed -= value; }
        }
        
        public Menu Menu {
            get { return menu; }
        }

        public SearchEntry()
        {
            AppPaintable = true;

            BuildWidget();
            BuildMenu();
            
            NoShowAll = true;
        }
            
        private void BuildWidget()
        {
            box = new HBox();
            entry = new FramelessEntry(this);
            filter_button = new HoverImageButton(IconSize.Menu, new string [] { "edit-find", Stock.Find });
            clear_button = new HoverImageButton(IconSize.Menu, new string [] { "edit-clear", Stock.Clear });

            box.PackStart(filter_button, false, false, 0);
            box.PackStart(entry, true, true, 0);
            box.PackStart(clear_button, false, false, 0);

            Add(box);
            box.ShowAll();

            entry.StyleSet += OnInnerEntryStyleSet;
            entry.StateChanged += OnInnerEntryStateChanged;
            entry.FocusInEvent += OnInnerEntryFocusEvent;
            entry.FocusOutEvent += OnInnerEntryFocusEvent;
            entry.Changed += OnInnerEntryChanged;

            filter_button.Image.Xpad = 2;
            clear_button.Image.Xpad = 2;
            filter_button.CanFocus = false;
            clear_button.CanFocus = false;

            filter_button.ButtonReleaseEvent += OnButtonReleaseEvent;
            clear_button.ButtonReleaseEvent += OnButtonReleaseEvent;
            clear_button.Clicked += OnClearButtonClicked;

            filter_button.Visible = false;
            clear_button.Visible = false;
        }

        private void BuildMenu()
        {
            menu = new Menu();
            menu.Deactivated += OnMenuDeactivated;
        }

        private void ShowMenu(uint time)
        {
            if(menu.Children.Length > 0) {
                menu.Popup(null, null, OnPositionMenu, 0, time);
                menu.ShowAll();
            }
        }

        private void ShowHideButtons()
        {
            clear_button.Visible = entry.Text.Length > 0;
            filter_button.Visible = menu != null && menu.Children.Length > 0;
        }

        private void OnPositionMenu(Menu menu, out int x, out int y, out bool push_in)
        {
            int origin_x, origin_y, tmp;
            
            filter_button.GdkWindow.GetOrigin(out origin_x, out tmp);
            GdkWindow.GetOrigin(out tmp, out origin_y);

            x = origin_x + filter_button.Allocation.X;
            y = origin_y + SizeRequest().Height;
            push_in = true;
        }

        private void OnMenuDeactivated(object o, EventArgs args)
        {
            filter_button.QueueDraw();
        }

        private bool toggling = false;

        private void OnMenuItemToggled(object o, EventArgs args)
        {
            if(toggling || !(o is FilterMenuItem)) {
                return;
            }
            
            toggling = true;
            FilterMenuItem item = (FilterMenuItem)o;
            
            foreach(MenuItem child_item in menu) {
                if(!(child_item is FilterMenuItem)) {
                    continue;
                }

                FilterMenuItem filter_child = (FilterMenuItem)child_item;
                if(filter_child != item) {
                    filter_child.Active = false;
                }
            }

            item.Active = true;
            ActiveFilterID = item.ID;
            toggling = false;
        }

        private void OnInnerEntryChanged(object o, EventArgs args)
        {
            ShowHideButtons();

            if (changed_timeout_id > 0) {
                GLib.Source.Remove (changed_timeout_id);
                changed_timeout_id = 0;
            }

            if (Ready)
                changed_timeout_id = GLib.Timeout.Add (25, delegate {
                    changed_timeout_id = 0;
                    OnChanged ();
                    return false;
                });
        }

        private void UpdateStyle ()
        {
            Gdk.Color color = entry.Style.Base (entry.State);
            filter_button.ModifyBg (entry.State, color);
            clear_button.ModifyBg (entry.State, color);
            
            box.BorderWidth = (uint)entry.Style.XThickness;
        }
        
        private void OnInnerEntryStyleSet (object o, StyleSetArgs args)
        {
            UpdateStyle ();
        }
        
        private void OnInnerEntryStateChanged (object o, EventArgs args)
        {
            UpdateStyle ();
        }
        
        private void OnInnerEntryFocusEvent(object o, EventArgs args)
        {
            QueueDraw();
        }

        private void OnButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
        {
            if(args.Event.Button != 1) {
                return;
            }

            entry.HasFocus = true;

            if(o == filter_button) {
                ShowMenu(args.Event.Time);
            }
        }

        private void OnClearButtonClicked(object o, EventArgs args)
        {
            active_filter_id = 0;
            entry.Text = String.Empty;
        }

        protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
        {
            if (evnt.Key == Gdk.Key.Escape) {
                active_filter_id = 0;
                entry.Text = String.Empty;
                return true;
            }
            return base.OnKeyPressEvent (evnt);
        }

        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            PropagateExpose(Child, evnt);
            Style.PaintShadow(entry.Style, GdkWindow, StateType.Normal, 
                ShadowType.In, evnt.Area, entry, "entry",
                0, 0, Allocation.Width, Allocation.Height); 
            return true;
        }

        protected override void OnShown()
        {
            base.OnShown();
            ShowHideButtons();
        }

        protected virtual void OnChanged()
        {
            if(!Ready) {
                return;
            }

            EventHandler handler = entry_changed;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnFilterChanged()
        {
            EventHandler handler = filter_changed;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
            
            if(IsQueryAvailable) {
                OnInnerEntryChanged(this, EventArgs.Empty);
            }
        }

        public void AddFilterOption(int id, string label)
        {
            if(id < 0) {
                throw new ArgumentException("id", "must be >= 0");
            }

            FilterMenuItem item = new FilterMenuItem(id, label);
            item.Toggled += OnMenuItemToggled;
            menu.Append(item);

            if(ActiveFilterID < 0) {
                item.Toggle();
            }

            filter_button.Visible = true;
        }

        public void AddFilterSeparator()
        {
            menu.Append(new SeparatorMenuItem());
        }

        public void RemoveFilterOption(int id)
        {
            FilterMenuItem item = FindFilterMenuItem(id);
            if(item != null) {
                menu.Remove(item);
            }
        }

        public void ActivateFilter(int id)
        {
            FilterMenuItem item = FindFilterMenuItem(id);
            if(item != null) {
                item.Toggle();
            }
        }

        private FilterMenuItem FindFilterMenuItem(int id)
        {
            foreach(MenuItem item in menu) {
                if(item is FilterMenuItem && ((FilterMenuItem)item).ID == id) {
                    return (FilterMenuItem)item;
                }
            }

            return null;
        }

        public string GetLabelForFilterID(int id)
        {
            FilterMenuItem item = FindFilterMenuItem(id);
            if(item == null) {
                return null;
            }

            return item.Label;
        }

        public void CancelSearch()
        {
            entry.Text = String.Empty;
            ActivateFilter(0);
        }

        public int ActiveFilterID {
            get { return active_filter_id; }
            private set { 
                if(value == active_filter_id) {
                    return;
                }

                active_filter_id = value;
                OnFilterChanged();
            }
        }

        public string EmptyMessage {
            get {
                return entry.Sensitive ? empty_message : String.Empty;
            }
            set {
                empty_message = value;
                entry.QueueDraw();
            }
        }

        public string Query {
            get { return entry.Text.Trim(); }
            set { entry.Text = value.Trim(); }
        }

        public bool IsQueryAvailable {
            get { return Query != null && Query != String.Empty; }
        }

        public bool Ready {
            get { return ready; }
            set { ready = value; }
        }
        
        public new bool HasFocus {
            get { return entry.HasFocus; }
            set { entry.HasFocus = true; }
        }

        
        public Entry InnerEntry {
            get { return entry; }
        }

        protected override void OnStateChanged (Gtk.StateType previous_state)
        {
            base.OnStateChanged (previous_state);

            entry.Sensitive = State != StateType.Insensitive;
            filter_button.Sensitive = State != StateType.Insensitive;
            clear_button.Sensitive = State != StateType.Insensitive;
        }

        private class FilterMenuItem : MenuItem /*CheckMenuItem*/
        {
            private int id;
            private string label;

            public FilterMenuItem(int id, string label) : base(label)
            {
                this.id = id;
                this.label = label;
                //DrawAsRadio = true;
            }

            public int ID {
                get { return id; }
            }

            public string Label {
                get { return label; }
            }
            
            // FIXME: Remove when restored to CheckMenuItem
            private bool active;
            public bool Active {
                get { return active; }
                set { active = value; }
            }
            
            public new event EventHandler Toggled;
            protected override void OnActivated ()
            {
                base.OnActivated ();
                if (Toggled != null) {
                    Toggled (this, EventArgs.Empty);
                }
            }

        }

        private class FramelessEntry : Entry
        {
            private Gdk.Window text_window;
            private SearchEntry parent;
            private Pango.Layout layout;
            private Gdk.GC text_gc;

            public FramelessEntry(SearchEntry parent) : base()
            {
                this.parent = parent;
                HasFrame = false;
                
                layout = new Pango.Layout(PangoContext);
                layout.FontDescription = PangoContext.FontDescription.Copy();

                parent.StyleSet += OnParentStyleSet;
                WidthChars = 1;
            }

            private void OnParentStyleSet(object o, EventArgs args)
            {
                RefreshGC();
                QueueDraw();
            }

            private void RefreshGC()
            {
                if(text_window == null) {
                    return;
                }

                text_gc = new Gdk.GC(text_window);
                text_gc.Copy(Style.TextGC(StateType.Normal));
                Gdk.Color color_a = parent.Style.Base(StateType.Normal);
                Gdk.Color color_b = parent.Style.Text(StateType.Normal);
                text_gc.RgbFgColor = DockServices.Drawing.ColorBlend(color_a, color_b);
            }

            protected override bool OnExposeEvent(Gdk.EventExpose evnt)
            {
                // The Entry's GdkWindow is the top level window onto which
                // the frame is drawn; the actual text entry is drawn into a
                // separate window, so we can ensure that for themes that don't
                // respect HasFrame, we never ever allow the base frame drawing
                // to happen
                if(evnt.Window == GdkWindow) {
                    return true;
                }

                bool ret = base.OnExposeEvent(evnt);

                if(text_gc == null || evnt.Window != text_window) {
                    text_window = evnt.Window;
                    RefreshGC();
                }

                if(Text.Length > 0 || HasFocus || parent.EmptyMessage == null) {
                    return ret;
                }

                int width, height;
                layout.SetMarkup(parent.EmptyMessage);
                layout.GetPixelSize(out width, out height);
                evnt.Window.DrawLayout(text_gc, 2, (SizeRequest().Height - height) / 2, layout);

                return ret;
            }
            
            public override void Dispose ()
            {
                parent.StyleSet -= OnParentStyleSet;

                if (layout != null) {
                    if (layout.FontDescription != null)
                        layout.FontDescription.Dispose ();
                    layout.Dispose ();
                }
                
                base.Dispose ();
            }
        }
    }
}
