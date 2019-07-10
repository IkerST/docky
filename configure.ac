dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.65])

AC_INIT([docky], [2.2.1.1],
        [https://bugs.launchpad.net/docky], [docky],
        [https://launchpad.net/docky])
AM_INIT_AUTOMAKE([1.11 foreign tar-pax dist-xz no-dist-gzip])

AC_CONFIG_MACRO_DIR([m4])
AC_CONFIG_SRCDIR([Makefile.am])
AC_CONFIG_SRCDIR([configure.ac])

dnl -- Intl
IT_PROG_INTLTOOL([0.35.0])
SHAMROCK_CONFIGURE_I18N(docky)

dnl -- Populate top_srcdir variable
top_srcdir=$(readlink -f $0 | sed -e s/configure$//)

dnl Plugin ABI version; bump whenever plugin interface changes
ABI_VERSION=0.1
AC_SUBST(ABI_VERSION)

dnl Bzr Revno, if release set BZR_REVNO to the release version number
if test -d $top_srcdir/.bzr ; then
	BZR_REVNO=`bzr version-info --custom \
	--template="{revno}"`
else
	BZR_REVNO=0.1
fi
AC_SUBST(BZR_REVNO)

if test -d $top_srcdir/.bzr ; then
   VERSION_INFO=`bzr version-info --custom \
   --template="bzr {branch_nick} r{revno}"`
elif test -e $top_srcdir/.version_info ; then
   VERSION_INFO=`cat .version_info`
else
   VERSION_INFO="Release"
fi
AC_SUBST(VERSION_INFO)

dnl pkg-config
PKG_PROG_PKG_CONFIG([0.21])

AC_PROG_INSTALL
AC_PROG_CC
AC_HEADER_STDC

SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_FIND_MONO_3_0_OR_4_0_COMPILER

dnl Find gconftool-2
PKG_CHECK_MODULES(GCONF, gconf-2.0)
AC_PATH_PROG(GCONFTOOL, gconftool-2)
AM_GCONF_SOURCE_2

dnl find gapi2-fixup
AC_PATH_PROG(GAPI2FIXUP, gapi2-fixup, no)
if test ! -x "$GAPI2FIXUP"; then
   AC_MSG_ERROR([No gapi post-processor found])
fi

AC_PATH_PROG(GAPI2CODEGEN, gapi2-codegen, no)
if test ! -x "$GAPI2CODEGEN"; then
   AC_MSG_ERROR([No gapi code generator found])
fi

dnl Expansions
SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

dnl minimal dependency versions
GTK_REQUIRED_VERSION=2.14.3
GLIB_REQUIRED_VERSION=2.22
GIO_SHARP_REQUIRED_VERSION=2.22.2
GDK_PIXBUF_REQUIRED_VERSION=2.14.3
DBUS_SHARP_REQUIRED_VERSION=0.7
DBUS_SHARP_GLIB_REQUIRED_VERSION=0.5
WNCK_REQUIRED_VERSION=2.20

PKG_CHECK_MODULES([GCONF_SHARP_20], [gconf-sharp-2.0])
PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0])

dnl gio-sharp is an unstable api library
PKG_CHECK_MODULES([GIO_SHARP], [gio-sharp-2.0 >= $GIO_SHARP_REQUIRED_VERSION])
files="`$PKG_CONFIG --variable=Libraries gio-sharp-2.0`"
for file in $files; do
	[ mkdir -p $top_srcdir/build ] && [ cp -vu $file $top_srcdir/build/ ]
	GIO_SHARP_ASSEMBLIES="$GIO_SHARP_ASSEMBLIES $top_srcdir/build/$(basename $file)"
	GIO_SHARP_LIBS="$GIO_SHARP_LIBS -r:$top_srcdir/build/$(basename $file)"
	[[ -r "$file.config" ]] && [ mkdir -p $top_srcdir/build ] && [ cp -vu $file.config $top_srcdir/build/ ]
	[[ -r "$file.config" ]] && GIO_SHARP_ASSEMBLIES="$GIO_SHARP_ASSEMBLIES $top_srcdir/build/$(basename $file).config"
done
AC_SUBST([GIO_SHARP_ASSEMBLIES])

PKG_CHECK_MODULES([GKEYFILE_SHARP], [gkeyfile-sharp])
PKG_CHECK_MODULES([GNOME_KEYRING_SHARP], [gnome-keyring-sharp-1.0])
PKG_CHECK_MODULES([GTK_SHARP_20], [gtk-sharp-2.0])
PKG_CHECK_MODULES([MONO_ADDINS], [mono-addins])
PKG_CHECK_MODULES([MONO_ADDINS_GUI], [mono-addins-gui])
PKG_CHECK_MODULES([MONO_ADDINS_SETUP], [mono-addins-setup])
PKG_CHECK_MODULES([MONO_CAIRO], [mono-cairo])
PKG_CHECK_MODULES([DBUS_SHARP], [dbus-sharp-2.0 >= $DBUS_SHARP_REQUIRED_VERSION])
PKG_CHECK_MODULES([DBUS_SHARP_GLIB], [dbus-sharp-glib-2.0 >= $DBUS_SHARP_GLIB_REQUIRED_VERSION])
PKG_CHECK_MODULES([NOTIFY_SHARP], [notify-sharp])

dnl check for native libraries (ones that are p/invoked)
PKG_CHECK_MODULES(GIO, gio-2.0 >= $GLIB_REQUIRED_VERSION)
PKG_CHECK_MODULES(GTK, gtk+-2.0 >= $GTK_REQUIRED_VERSION)
PKG_CHECK_MODULES(GLIB, gobject-2.0 >= $GLIB_REQUIRED_VERSION)
PKG_CHECK_MODULES(GDK_PIXBUF, gdk-pixbuf-2.0 >= $GDK_PIXBUF_REQUIRED_VERSION)
PKG_CHECK_MODULES(WNCK, libwnck-1.0 >= $WNCK_REQUIRED_VERSION)

MONO_CAIRO_LIBS="-r:Mono.Cairo"
AC_SUBST([MONO_CAIRO_LIBS])

WNCK_SHARP_10_LIBS="-r:$(pwd)/build/wnck-sharp1.dll"
AC_SUBST([WNCK_SHARP_10_LIBS])

AM_CONDITIONAL(CONFIG_REQUESTED, test -z "")

MCS_FLAGS="-noconfig -codepage:utf8 -warn:4"
AC_SUBST([MCS_FLAGS])

dnl {{{ run with heapshot
AC_ARG_ENABLE(heap-shot,
	AC_HELP_STRING([--enable-heap-shot],
		[Run with heap-shot [default=NO]]),
		enable_heap_shot=yes, enable_heap_shot=no)
AM_CONDITIONAL(ENABLE_HEAP_SHOT, test "x$enable_heap_shot" = "xyes")
if test -z "$ENABLE_HEAP_SHOT_TRUE" ; then
	CONFIG_REQUESTED="yes"
	MONO_DEBUG_ARGS+="--profile=heap-shot "
fi
dnl }}}

dnl {{{ enable debug release configuration
AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=NO]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test "x$enable_debug" = "xyes")
if test -z "$ENABLE_DEBUG_TRUE" ; then
	CONFIG_REQUESTED="yes"
	MONO_DEBUG_ARGS+="--debug "
	DOCKY_DEBUG_ARGS+="--debug "
	MCS_FLAGS+=" -debug -d:DEBUG"
fi
dnl }}}

dnl {{{ enable release build configuration
AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'RELEASE' Configuration [default=NO]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test "x$enable_release" = "xyes")
if test -z "$ENABLE_RELEASE_TRUE" ; then
	CONFIG_REQUESTED="yes"
	MCS_FLAGS+=" -warnaserror"
fi
dnl }}}

if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(CONFIG_REQUESTED, true)
	AC_SUBST(MONO_DEBUG_ARGS)
	AC_SUBST(DOCKY_DEBUG_ARGS)
fi

AC_CONFIG_FILES([
Docky/Makefile
Docky.CairoHelper/Makefile
Docky.DBus/Makefile
Docky.Items/Makefile
Docky.Widgets/Makefile
Docky.Services/Makefile
Docky.Services/AssemblyInfo.cs
Docky/docky
Docky/AssemblyInfo.cs
StandardPlugins/Makefile
StandardPlugins/BatteryMonitor/Makefile
StandardPlugins/BatteryMonitor/Resources/BatteryMonitor.addin.xml
StandardPlugins/Bookmarks/Makefile
StandardPlugins/Bookmarks/Resources/Bookmarks.addin.xml
StandardPlugins/Clippy/Makefile
StandardPlugins/Clippy/Resources/Clippy.addin.xml
StandardPlugins/Clock/Makefile
StandardPlugins/Clock/Resources/Clock.addin.xml
StandardPlugins/Clock/Themes/Makefile
StandardPlugins/Clock/Themes/Default/Makefile
StandardPlugins/Clock/Themes/Default24/Makefile
StandardPlugins/CPUMonitor/Makefile
StandardPlugins/CPUMonitor/Resources/CPUMonitor.addin.xml
StandardPlugins/Desktop/Makefile
StandardPlugins/Desktop/Resources/Desktop.addin.xml
StandardPlugins/GMail/Makefile
StandardPlugins/GMail/Resources/GMail.addin.xml
StandardPlugins/Mounter/Makefile
StandardPlugins/Mounter/Resources/Mounter.addin.xml
StandardPlugins/NetworkManager/Makefile
StandardPlugins/NetworkManager/Resources/NetworkManager.addin.xml
StandardPlugins/NetworkMonitor/Makefile
StandardPlugins/NetworkMonitor/Resources/NetworkMonitor.addin.xml
StandardPlugins/NPR/Makefile
StandardPlugins/NPR/Resources/NPR.addin.xml
StandardPlugins/RecentDocuments/Makefile
StandardPlugins/RecentDocuments/Resources/RecentDocuments.addin.xml
StandardPlugins/SessionManager/Makefile
StandardPlugins/SessionManager/Resources/SessionManager.addin.xml
StandardPlugins/Timer/Makefile
StandardPlugins/Timer/Resources/Timer.addin.xml
StandardPlugins/Trash/Makefile
StandardPlugins/Trash/Resources/Trash.addin.xml
StandardPlugins/Weather/Makefile
StandardPlugins/Weather/Resources/Weather.addin.xml
StandardPlugins/WorkspaceSwitcher/Makefile
StandardPlugins/WorkspaceSwitcher/Resources/WorkspaceSwitcher.addin.xml
Makefile
data/Makefile
data/icons/Makefile
data/icons/hicolor/16x16/Makefile
data/icons/hicolor/16x16/apps/Makefile
data/icons/hicolor/22x22/Makefile
data/icons/hicolor/22x22/apps/Makefile
data/icons/hicolor/24x24/Makefile
data/icons/hicolor/24x24/apps/Makefile
data/icons/hicolor/32x32/Makefile
data/icons/hicolor/32x32/apps/Makefile
data/icons/hicolor/48x48/Makefile
data/icons/hicolor/48x48/apps/Makefile
data/icons/hicolor/48x48/mimetypes/Makefile
data/icons/hicolor/64x64/Makefile
data/icons/hicolor/64x64/apps/Makefile
data/icons/hicolor/128x128/Makefile
data/icons/hicolor/128x128/apps/Makefile
data/icons/hicolor/Makefile
data/themes/Makefile
data/themes/Air/Makefile
data/themes/Glass/Makefile
data/themes/Grayscale/Makefile
data/themes/HUD/Makefile
data/themes/Matte/Makefile
data/themes/Smoke/Makefile
data/themes/Transparent/Makefile
lib/Makefile
lib/wnck-sharp1/Makefile
m4/Makefile
man/Makefile
po/Makefile.in
])

AC_OUTPUT

cat <<EOF

${PACKAGE}-${VERSION}

  Build Environment
    Install Prefix:     ${prefix}
    Mono C# Compiler:   ${MCS} ${MCS_FLAGS}
    Mono Runtime:       ${MONO}

  Build/Development
    Release Build:      ${enable_release}
    Debug Build:        ${enable_debug}
    Use HeapShot:       ${enable_heap_shot}

EOF
