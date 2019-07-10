# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = NetworkManager

FILES = \
	src/ConnectionManager.cs \
	src/DBusObject.cs \
	src/DeviceManager.cs \
	src/DeviceStateChangedArgs.cs \
	src/Enums.cs \
	src/NetworkConnection.cs \
	src/NetworkDevice.cs \
	src/NetworkManager.cs \
	src/NetworkManagerDocklet.cs \
	src/NetworkManagerItemProvider.cs \
	src/WiredConnection.cs \
	src/WiredDevice.cs \
	src/WirelessAccessPoint.cs \
	src/WirelessConnection.cs \
	src/WirelessDevice.cs \
	src/Interfaces/IAccessPoint.cs \
	src/Interfaces/IActiveConnection.cs \
	src/Interfaces/IConnectionManager.cs \
	src/Interfaces/IDHCP4Config.cs \
	src/Interfaces/IIP4Config.cs \
	src/Interfaces/INetManager.cs \
	src/Interfaces/INetworkConnection.cs \
	src/Interfaces/INetworkDevice.cs \
	src/Interfaces/IWiredDevice.cs \
	src/Interfaces/IWirelessDevice.cs

RESOURCES = \
	Resources/NetworkManager.addin.xml

PROJECT_REFERENCES = \
	Docky.Items \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(MONO_CAIRO_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(DBUS_SHARP_LIBS) \
	$(DBUS_SHARP_GLIB_LIBS) \
	$(GTK_SHARP_20_LIBS)
