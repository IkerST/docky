# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Timer

FILES = \
	src/TimerItemProvider.cs \
	src/TimerMainDockItem.cs \
	src/TimerDockItem.cs

RESOURCES = \
	Resources/Timer.addin.xml \
	Resources/icons/timer.svg \
	Resources/icons/base.svg \
	Resources/icons/hand.svg \
	Resources/icons/overlay.svg

PROJECT_REFERENCES = \
	Docky.CairoHelper \
	Docky.Items \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(MONO_CAIRO_LIBS) \
	$(NOTIFY_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)
