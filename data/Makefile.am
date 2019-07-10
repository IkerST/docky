SUBDIRS = icons themes

EXTRA_DIST = $(schemas_in_files) docky.desktop.in \
	remaps.ini \
	sun.svg \
	wind.svg \
	temp.svg \
	poof.png
CLEANFILES = $(schemas_DATA) docky.desktop

schemasdir = $(GCONF_SCHEMA_FILE_DIR)
schemas_in_files = docky.schemas.in
schemas_DATA = $(schemas_in_files:.schemas.in=.schemas)

applicationsdir = $(datadir)/applications
applications_DATA = docky.desktop

remapsdir = $(datadir)/docky
remaps_DATA = remaps.ini

poofdir = $(datadir)/docky
poof_DATA = poof.png \
	sun.svg \
	wind.svg \
	temp.svg

@INTLTOOL_DESKTOP_RULE@

@INTLTOOL_SCHEMAS_RULE@

if GCONF_SCHEMAS_INSTALL
install-data-local:
	GCONF_CONFIG_SOURCE=$(GCONF_SCHEMA_CONFIG_SOURCE) \
	$(GCONFTOOL) --makefile-install-rule $(schemas_DATA)
endif
