# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Bookmarks

FILES = \
	src/BookmarksItemProvider.cs

RESOURCES = \
	Resources/Bookmarks.addin.xml

PROJECT_REFERENCES = \
	Docky.Items \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)
