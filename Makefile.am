EXTRA_DIST = \
	COPYRIGHT \
	NEWS

ACLOCAL_AMFLAGS = -I m4 -I m4/shamrock

# Install copied assemblies to our libdir
copiedlibsdir = $(pkglibdir)
copiedlibs_DATA = \
	$(GIO_SHARP_ASSEMBLIES)

SUBDIRS = \
	. \
	data \
	po \
	lib \
	Docky.CairoHelper \
	Docky.Services \
	Docky.Widgets \
	Docky.Items \
	Docky.DBus \
	StandardPlugins \
	Docky \
	man \
	m4

# Get rid of the build dir on clean.
clean-local:
	rm -rf $(top_builddir)/build
