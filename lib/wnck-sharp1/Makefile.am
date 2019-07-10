UNSAFE = -unsafe
DEBUG = -debug

EXTRA_DIST = \
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/snk \
	$(srcdir)/wnck-api.raw \
	$(srcdir)/wnck-sharp1.dll.config \
	$(srcdir)/Wnck.metadata

SRCS = \
	$(srcdir)/AssemblyInfo.cs

SNK = \
	$(srcdir)/snk

ASSEMBLIES = \
	-pkg:glib-sharp-2.0 \
	-pkg:gtk-sharp-2.0

all: wnck-sharp1.dll

wnck-api.xml: $(srcdir)/wnck-api.raw $(srcdir)/Wnck.metadata
	@echo -e "\n*** Massaging the raw api into $@"
	cp $(srcdir)/wnck-api.raw wnck-api.xml
	chmod +w wnck-api.xml
	gapi2-fixup --api=wnck-api.xml --metadata=$(srcdir)/Wnck.metadata

generated/Window.cs: $(builddir)/wnck-api.xml
	@echo -e "\n*** Generating C# code"
	gapi2-codegen $(GLIB_SHARP_20_CFLAGS) $(GTK_SHARP_20_CFLAGS) --outdir=generated --generate wnck-api.xml

gtk-sharp.snk: $(SNK)
	cp $< $@

wnck-sharp1.dll: generated/Window.cs $(SRCS) gtk-sharp.snk $(srcdir)/wnck-sharp1.dll.config
	@echo -e "\n*** Building $@"
	$(MCS) -target:library -out:$@ $(UNSAFE) $(DEBUG) $(ASSEMBLIES) $(SRCS) generated/*.cs
	mkdir -p $(top_builddir)/build
	cp $@ $(srcdir)/wnck-sharp1.dll.config $(top_builddir)/build/

assemblydir = $(pkglibdir)
assembly_DATA =	\
	wnck-sharp1.dll \
	$(srcdir)/wnck-sharp1.dll.config

CLEANFILES = wnck-sharp1.dll wnck-sharp1.dll.mdb gtk-sharp.snk wnck-api.xml

# Get rid of the generated source on clean.
clean-local:
	rm -rf $(builddir)/generated
