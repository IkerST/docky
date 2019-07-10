# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Clippy

FILES = \
	src/ClippyItemProvider.cs \
	src/ClippyMenuItem.cs \
	src/ClippyItem.cs

RESOURCES = \
	Resources/Clippy.addin.xml

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
