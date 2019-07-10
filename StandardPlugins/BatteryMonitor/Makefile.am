# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = BatteryMonitor

FILES = \
	src/BatteryMonitorItemProvider.cs \
	src/BatteryMonitorAbstractItem.cs \
	src/BatteryMonitorSysItem.cs \
	src/BatteryMonitorUPowerItem.cs \
	src/BatteryMonitorProcItem.cs

RESOURCES = \
	Resources/BatteryMonitor.addin.xml \
	Resources/battery/battery_bottom.svg \
	Resources/battery/battery_inside_10.svg \
	Resources/battery/battery_inside_100.svg \
	Resources/battery/battery_inside_20.svg \
	Resources/battery/battery_inside_30.svg \
	Resources/battery/battery_inside_40.svg \
	Resources/battery/battery_inside_50.svg \
	Resources/battery/battery_inside_60.svg \
	Resources/battery/battery_inside_70.svg \
	Resources/battery/battery_inside_80.svg \
	Resources/battery/battery_inside_90.svg \
	Resources/battery/battery_inside_90_new.svg \
	Resources/battery/battery_plugged.svg \
	Resources/battery/battery_top.svg

PROJECT_REFERENCES= \
	Docky.CairoHelper \
	Docky.Items \
	Docky.Services

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(MONO_CAIRO_LIBS) \
	$(DBUS_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)
