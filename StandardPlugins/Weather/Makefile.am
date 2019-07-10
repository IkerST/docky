# Simple component buildsystem
include $(top_srcdir)/build.rules.docklets.mk

ASSEMBLY = Weather

FILES = \
	src/ForecastMenuItem.cs \
	src/WeatherForecast.cs \
	src/Sources/AbstractWeatherSource.cs \
	src/Sources/WeatherChannelWeatherSource.cs \
	src/Sources/GoogleWeatherSource.cs \
	src/Sources/WunderWeatherSource.cs \
	src/WeatherItemProvider.cs \
	src/WeatherPreferences.cs \
	src/WeatherController.cs \
	src/WeatherDocklet.cs \
	src/WeatherPainter.cs \
	src/WeatherErrorArgs.cs \
	src/WeatherConfig.cs \
	src/WeatherConfigDialog.cs \
	src/WeatherTile.cs \
	gtk-gui/WeatherDocklet.WeatherConfig.cs \
	gtk-gui/generated.cs

RESOURCES = \
	Resources/Weather.addin.xml

PROJECT_REFERENCES = \
	Docky.CairoHelper \
	Docky.Items \
	Docky.Services \
	Docky.Widgets

REFERENCES =  \
	System \
	System.Core \
	System.Web \
	System.Xml \
	Mono.Posix \
	$(MONO_CAIRO_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(GTK_SHARP_20_LIBS)
