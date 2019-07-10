# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = RecentDocuments

FILES = \
	src/EmptyRecentItem.cs \
	src/RecentFilesProvider.cs \
	src/RecentDocumentsItemProvider.cs \
	src/RecentDocumentsItem.cs

RESOURCES = \
	Resources/RecentDocuments.addin.xml

PROJECT_REFERENCES = \
	Docky.Items \
	Docky.Services

REFERENCES =  \
	System \
	System.Xml \
	System.Xml.Linq \
	System.Core \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)
