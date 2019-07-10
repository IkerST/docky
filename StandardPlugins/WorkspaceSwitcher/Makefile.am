# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = WorkspaceSwitcher

FILES = \
	src/Desk.cs \
	src/WorkspaceSwitcherDockItem.cs \
	src/WorkspaceSwitcherItemProvider.cs
	
RESOURCES = \
	Resources/WorkspaceSwitcher.addin.xml

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
