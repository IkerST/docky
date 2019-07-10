# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = CPUMonitor

FILES = \
	src/CPUMonitorDockItem.cs \
	src/CPUMonitorItemProvider.cs

RESOURCES = \
	Resources/CPUMonitor.addin.xml

PROJECT_REFERENCES= \
	Docky.CairoHelper \
	Docky.Items \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(MONO_CAIRO_LIBS) \
	$(GTK_SHARP_20_LIBS)
