# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Trash

FILES = \
	src/TrashItemProvider.cs \
	src/TrashDockItem.cs

RESOURCES = \
	Resources/Trash.addin.xml

PROJECT_REFERENCES = \
	Docky.Items \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)
