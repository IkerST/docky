# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = NPR

FILES = \
	src/NPRItemProvider.cs \
	src/NPR.cs \
	src/Station.cs \
	src/StationsUpdatedEventArgs.cs \
	src/StationUrl.cs \
	src/StationSearchWidget.cs \
	src/StationDockItem.cs \
	src/NPRConfigDialog.cs \
	gtk-gui/NPR.StationSearchWidget.cs \
	gtk-gui/generated.cs

RESOURCES = \
	Resources/NPR.addin.xml \
	Resources/nprlogo.gif

PROJECT_REFERENCES = \
	Docky.CairoHelper \
	Docky.Items \
	Docky.Services \
	Docky.Widgets

REFERENCES =  \
	System \
	System.Web \
	System.Xml \
	System.Xml.Linq \
	System.Core \
	Mono.Posix \
	$(NOTIFY_SHARP_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)
