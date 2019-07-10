# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky
TARGET = exe

FILES = \
	AssemblyInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/Docky.Interface.DockPreferences.cs \
	gtk-gui/Docky.ConfigurationWindow.cs \
	Gtk/Widget_Extensions.cs \
	Mono/Options.cs \
	Docky/ConfigurationWindow.cs \
	Docky/DockController.cs \
	Docky/Docky.cs \
	Docky/UserArgs.cs \
	Docky/PluginManager.cs \
	Docky/HelperTile.cs \
	Docky/DockletTile.cs \
	Docky/CairoHelper/DockySurface_Extensions.cs \
	Docky/Interface/AnimationState.cs \
	Docky/Interface/AutohideManager.cs \
	Docky/Interface/AutohideType.cs \
	Docky/Interface/CursorPostionChangedArgs.cs \
	Docky/Interface/CursorTracker.cs \
	Docky/Interface/Dock.cs \
	Docky/Interface/DockDragTracker.cs \
	Docky/Interface/DockPosition.cs \
	Docky/Interface/DockWindow.cs \
	Docky/Interface/DockPreferences.cs \
	Docky/Interface/HoveredItemChangedArgs.cs \
	Docky/Interface/HoverTextManager.cs \
	Docky/Interface/IDockPreferences.cs \
	Docky/Interface/ItemProvidersChangedEventArgs.cs \
	Docky/Interface/PoofWindow.cs \
	Docky/Items/DockyItem.cs \
	Docky/Items/INonPersistedItem.cs \
	Docky/Items/SeparatorItem.cs \
	Docky/Items/SpacingItem.cs \
	Docky/Menus/DockMenu.cs \
	Docky/Menus/MenuItemWidget.cs \
	Docky/Menus/SeparatorWidget.cs \
	Docky/Menus/DockItemMenu.cs

RESOURCES = \
	gtk-gui/gui.stetic \
	Resources/about.svg \
	Resources/badge.svg \
	Resources/close.svg \
	Resources/help.svg \
	Resources/preferences.svg \
	Resources/stretch_separator.svg

PROJECT_REFERENCES= \
	Docky.CairoHelper \
	Docky.DBus \
	Docky.Items \
	Docky.Services \
	Docky.Widgets

REFERENCES =  \
	System \
	System.Xml \
	System.Core \
	Mono.Posix \
	ICSharpCode.SharpZipLib \
	$(MONO_CAIRO_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(GKEYFILE_SHARP_LIBS) \
	$(WNCK_SHARP_10_LIBS) \
	$(NOTIFY_SHARP_LIBS) \
	$(DBUS_SHARP_LIBS) \
	$(DBUS_SHARP_GLIB_LIBS) \
	$(MONO_ADDINS_LIBS) \
	$(MONO_ADDINS_GUI_LIBS) \
	$(MONO_ADDINS_SETUP_LIBS)

#
# Extra rules for our binary, not worth factoring out
#

CLEANFILES += docky
EXTRA_DIST += docky.in Docky.addins

module_DATA += Docky.addins
bin_SCRIPTS = docky

MCS_FLAGS += -d:LINQ -unsafe
