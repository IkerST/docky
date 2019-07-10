# Simple component buildsystem
include $(top_srcdir)/build.rules.core.mk

ASSEMBLY = Docky.Services
TARGET = library

PKG_CONFIG_FILES = docky.services.pc

FILES = \
	AssemblyInfo.cs \
	Docky.Services/Applications/DesktopFileChangedEventArgs.cs \
	Docky.Services/Applications/DesktopItem.cs \
	Docky.Services/Helpers/Helper.cs \
	Docky.Services/Helpers/HelperStatusChangedEventArgs.cs \
	Docky.Services/Helpers/HelperMetadata.cs \
	Docky.Services/Extensions/GdkPixbufExtension.cs \
	Docky.Services/Extensions/GLibFileExtension.cs \
	Docky.Services/Extensions/GLibMountExtension.cs \
	Docky.Services/Extensions/GtkIconThemeExtension.cs \
	Docky.Services/Extensions/WnckWindowExtension.cs \
	Docky.Services/Logging/ConsoleCrayon.cs \
	Docky.Services/Logging/ConsoleLog.cs \
	Docky.Services/Logging/LogBase.cs \
	Docky.Services/Preferences/IPreferences.cs \
	Docky.Services/Preferences/Preferences.cs \
	Docky.Services/Preferences/PreferencesChangedEventArgs.cs \
	Docky.Services/Windows/Position.cs \
	Docky.Services/Windows/WindowControl.cs \
	Docky.Services/Xlib/X11Atoms.cs \
	Docky.Services/Xlib/Xlib.cs \
	Docky.Services/ConnectionStatusChangeEventArgs.cs \
	Docky.Services/DesktopItemService.cs \
	Docky.Services/DockServices.cs \
	Docky.Services/DrawingService.cs \
	Docky.Services/HelperService.cs \
	Docky.Services/Log.cs \
	Docky.Services/LogTSender.cs \
	Docky.Services/NativeInterop.cs \
	Docky.Services/NotificationService.cs \
	Docky.Services/PathsService.cs \
 	Docky.Services/PreferencesService.cs \
	Docky.Services/SystemService.cs \
	Docky.Services/ThemeService.cs \
	Docky.Services/WindowMatcherService.cs \
	Gtk/MountOperation.cs \
	GLibSharp/AsyncReadyCallbackNative.cs

RESOURCES = \
	Resources/background.svg \
	Resources/menu.svg \
	Resources/tooltip.svg 

PROJECT_REFERENCES= \
	Docky.CairoHelper

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	ICSharpCode.SharpZipLib \
	$(GIO_SHARP_LIBS) \
	$(MONO_CAIRO_LIBS) \
	$(DBUS_SHARP_LIBS) \
	$(DBUS_SHARP_GLIB_LIBS) \
	$(GCONF_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(GNOME_KEYRING_SHARP_LIBS) \
	$(NOTIFY_SHARP_LIBS) \
	$(WNCK_SHARP_10_LIBS)

EXTRA_DIST += Docky.Services.dll.config

module_DATA += Docky.Services.dll.config

MCS_FLAGS += -unsafe
