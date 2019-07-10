# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Desktop

FILES = \
	src/Wink/Viewport.cs \
	src/Wink/ScreenUtils.cs \
	src/DesktopDockItem.cs \
	src/ShowDesktopItem.cs \
	src/TileDesktopItem.cs \
	src/CascadeDesktopItem.cs \
	src/DesktopActionsProvider.cs \
	src/DesktopItemProvider.cs
	
RESOURCES = \
	Resources/icons/show-desktop.svg \
	Resources/icons/window-cascade.svg \
	Resources/icons/window-tile.svg \
	Resources/Desktop.addin.xml

PROJECT_REFERENCES= \
	Docky.CairoHelper \
	Docky.Items \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(MONO_CAIRO_LIBS) \
	$(GCONF_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(WNCK_SHARP_10_LIBS)
