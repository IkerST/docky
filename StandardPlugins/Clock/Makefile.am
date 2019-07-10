SUBDIRS = Themes

# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Clock

FILES = \
	src/ClockItemProvider.cs \
	src/ClockDockItem.cs \
	src/ClockThemeSelector.cs \
	src/CalendarPainter.cs

RESOURCES = \
	Resources/Clock.addin.xml

PROJECT_REFERENCES= \
	Docky.CairoHelper \
	Docky.Items \
	Docky.Services \
	Docky.Widgets

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(MONO_CAIRO_LIBS) \
	$(GTK_SHARP_20_LIBS)
