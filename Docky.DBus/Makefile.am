# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.DBus
TARGET = library

FILES = \
	Docky.DBus/DBusManager.cs \
	Docky.DBus/DockManagerDBus.cs \
	Docky.DBus/DockManagerDBusItem.cs \
	Docky.DBus/DockyDBus.cs \
	Docky.DBus/IDockManagerDBus.cs \
	Docky.DBus/IDockManagerDBusItem.cs \
	Docky.DBus/IDockyDBus.cs \
	Docky.DBus/RemoteMenuEntry.cs \
	Docky.DBus/RemoteFileMenuEntry.cs

PROJECT_REFERENCES = \
	Docky.Items \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	$(GLIB_SHARP_20_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(WNCK_SHARP_10_LIBS) \
	$(DBUS_SHARP_LIBS) \
	$(DBUS_SHARP_GLIB_LIBS)
