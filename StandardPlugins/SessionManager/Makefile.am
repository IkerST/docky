# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = SessionManager

FILES = \
	src/SessionManagerItemProvider.cs \
	src/SessionActionsProvider.cs \
	src/SessionManagerItem.cs \
	src/SessionManagerActionItem.cs \
	src/SystemManager.cs

RESOURCES = \
	Resources/SessionManager.addin.xml

PROJECT_REFERENCES= \
	Docky.CairoHelper \
	Docky.Items \
	Docky.Services

REFERENCES = \
	System \
	System.Xml \
	System.Xml.Linq \
	System.Core \
	Mono.Posix \
	$(GIO_SHARP_LIBS) \
	$(DBUS_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)
