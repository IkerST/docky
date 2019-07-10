# Rules to more easily specify a C# build for automake.
#
# Inspired and adapted from Banshee's build system

include $(top_srcdir)/build.rules.common.mk

TARGET = library

MA_MANIFEST_FILE = $(filter %.addin.xml, $(RESOURCES_EXPANDED))
MA_ADDIN_NAME = $(shell egrep -o -m 1 'id=".*"' $(MA_MANIFEST_FILE) \
	          | sed 's/id="//g' | sed 's/"//g')
MA_ADDIN_VER = $(shell egrep -o -m 1 'version=".*"' $(MA_MANIFEST_FILE) \
	          | sed 's/version="//g' | sed 's/"//g')
MA_ADDIN_NAMESPACE = Docky
MA_PACKFILE = $(MA_ADDIN_NAMESPACE).$(MA_ADDIN_NAME)_$(MA_ADDIN_VER).mpack

# Install dockpets as data; there's no need for them to be excutable
plugindir = ${libdir}/docky/plugins
plugin_DATA = $(OUTPUT_FILES)

# All docklets should be translatable; every plugin will need to link to
# Mono.Addins for this.
COMPONENT_REFERENCES += $(MONO_ADDINS_LIBS)

all: $(OUTPUT_FILES)

reference-debug:
	@echo $(BUILD_REFERENCES) $(COMPONENT_REFERENCES)
	@echo $(RESOURCES_EXPANDED)
	@echo $(MA_MANIFEST_FILE)

$(BUILD_DIR)/$(MA_PACKFILE): $(ASSEMBLY_FILE)
	cd $(BUILD_DIR) && $(MAUTIL) pack $(ASSEMBLY_FILE)
