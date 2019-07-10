# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Mounter

FILES = \
	src/MountItem.cs \
	src/MountProvider.cs

RESOURCES = \
	Resources/Mounter.addin.xml

PROJECT_REFERENCES = \
	Docky.Items \
	Docky.Services \
	Docky.Widgets

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)
