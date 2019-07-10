# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = NetworkMonitor

FILES = \
	src/NetworkMonitorItemProvider.cs \
	src/NetworkMonitorDockItem.cs \
	src/DeviceInfo.cs \
	src/NetworkMonitor.cs 

RESOURCES = \
	Resources/NetworkMonitor.addin.xml

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
