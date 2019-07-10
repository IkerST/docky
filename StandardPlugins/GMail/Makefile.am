# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = GMail

FILES = \
	src/GMailItemProvider.cs \
	src/GMailAtom.cs \
	src/GMailErrorArgs.cs \
	src/GMailPreferences.cs \
	src/GMailMenuItem.cs \
	src/GMailDockItem.cs \
	src/GMailLoginConfig.cs \
	src/GMailLabel.cs \
	src/GMailLabelConfig.cs \
	src/GMailConfigDialog.cs \
	gtk-gui/GMail.GMailLabelConfig.cs \
	gtk-gui/generated.cs

RESOURCES = \
	Resources/GMail.addin.xml

PROJECT_REFERENCES = \
	Docky.CairoHelper \
	Docky.Items \
	Docky.Services \
	Docky.Widgets

REFERENCES =  \
	System \
	System.Web \
	System.Xml \
	System.Core \
	Mono.Posix \
	$(MONO_CAIRO_LIBS) \
	$(NOTIFY_SHARP_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)

MCS_FLAGS += -unsafe
