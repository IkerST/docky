# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.Items
TARGET = library

PKG_CONFIG_FILES = docky.items.pc

FILES = \
	Docky.Painters/AbstractDockPainter.cs \
	Docky.Painters/PagingDockPainter.cs \
	Docky.Items/Enumerable_Extensions.cs \
	Docky.Items/ItemState.cs \
	Docky.Menus/MenuItem.cs \
	Docky.Menus/ProxyMenuItem.cs \
	Docky.Menus/MenuList.cs \
	Docky.Menus/SeparatorMenuItem.cs \
	Docky.Menus/WindowMenuItem.cs \
	Docky.Menus/IconMenuItem.cs \
	Docky.Items/WindowDockItem.cs \
	Docky.Items/AbstractDockItem.cs \
	Docky.Items/AbstractDockItemProvider.cs \
	Docky.Items/ApplicationDockItem.cs \
	Docky.Items/FileDockItem.cs \
	Docky.Items/ItemsChangedArgs.cs \
	Docky.Items/CurrentItemChangedArgs.cs \
	Docky.Items/ActivityIndicator.cs \
	Docky.Items/ClickAnimation.cs \
	Docky.Items/PaintNeededEventArgs.cs \
	Docky.Items/PainterRequestEventArgs.cs \
	Docky.Items/ProxyDockItem.cs \
	Docky.Items/FileApplicationProvider.cs \
	Docky.Items/IconDockItem.cs \
	Docky.Items/ColoredIconDockItem.cs \
	Docky.Items/WnckDockItem.cs \
	Docky.Items/IconEmblem.cs

RESOURCES = \
	Resources/apply.svg \
	Resources/close.svg \
	Resources/maximize.svg \
	Resources/minimize.svg \
	Resources/painterleft.svg \
	Resources/painterright.svg \
	Resources/pause.svg \
	Resources/pin.svg \
	Resources/play.svg \
	Resources/run.svg \
	Resources/Docky.Items.addin.xml

PROJECT_REFERENCES= \
	Docky.CairoHelper \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(MONO_CAIRO_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(WNCK_SHARP_10_LIBS) \
	$(NOTIFY_SHARP_LIBS)

MCS_FLAGS += -unsafe
