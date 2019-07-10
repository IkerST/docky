//  
//  Copyright (C) 2010 Chris Szikszoy, Robert Dyer
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
using System;
using System.Collections.Generic;
using System.Linq;

using GLib;

namespace Docky.Services
{
	/// <summary>
	/// This class provides all paths used in Docky.
	///
	/// It follows the XDG Base Directory specification.  For more information see:
	///   http://standards.freedesktop.org/basedir-spec/basedir-spec-latest.html 
	/// </summary>
	public class PathsService
	{
		#region XDG Base Directory independent paths
		
		/// <summary>
		/// User's home folder - $HOME
		/// </summary>
		public File HomeFolder { get; protected set; }
		
		/// <summary>
		/// Docky's installed data directory - defaults to /usr/share/docky for PPA
		/// </summary>
		public File SystemDataFolder { get; protected set; }
		
		#endregion
		
		
		#region XDG Base Directory paths
		
		/// <summary>
		/// $XDG_CONFIG_HOME - defaults to $HOME/.config
		/// </summary>
		public File XdgConfigHomeFolder { get; protected set; }

		/// <summary>
		/// $XDG_DATA_HOME - defaults to $HOME/.local/share
		/// </summary>
		public File XdgDataHomeFolder { get; protected set; }
		
		/// <summary>
		/// $XDG_CACHE_HOME - defaults to $HOME/.cache
		/// </summary>
		public File XdgCacheHomeFolder { get; protected set; }
		
		/// <summary>
		/// $XDG_DATA_DIRS - defaults to /usr/local/share/:/usr/share/
		/// </summary>
		public IEnumerable<File> XdgDataDirFolders { get; protected set; }
		
		
		/// <summary>
		/// defaults to XdgCacheHomeFolder/docky
		/// </summary>
		public File UserCacheFolder { get; protected set; }
		
		/// <summary>
		/// defaults to XdgDataHomeFolder/docky
		/// </summary>
		public File UserDataFolder { get; protected set; }
		
		/// <summary>
		/// defaults to XdgDataHomeFolder/dockmanager
		/// </summary>
		public File DockManagerUserDataFolder { get; protected set; }
		
		
		/// <summary>
		/// Path to the autostart file - defaults to 
		/// </summary>
		public File AutoStartFile { get; protected set; }
		
		#endregion
		
		
		public void Initialize ()
		{
			// get environment-based settings
			File env_home         = FileFactory.NewForPath (Environment.GetFolderPath (Environment.SpecialFolder.Personal));
			File env_data_install = FileFactory.NewForPath (AssemblyInfo.DataDirectory);
			
			
			// set the non-XDG Base Directory specified directories to use
			HomeFolder       = env_home;
			SystemDataFolder = env_data_install.GetChild ("docky");
			
			
			// get XDG Base Directory settings
			string xdg_config_home  = Environment.GetEnvironmentVariable ("XDG_CONFIG_HOME");
			string xdg_data_home  = Environment.GetEnvironmentVariable ("XDG_DATA_HOME");
			string xdg_data_dirs  = Environment.GetEnvironmentVariable ("XDG_DATA_DIRS");
			string xdg_cache_home = Environment.GetEnvironmentVariable ("XDG_CACHE_HOME");
			
			
			// determine directories based on XDG with fallbacks
			if (string.IsNullOrEmpty (xdg_config_home))
				XdgConfigHomeFolder = HomeFolder.GetChild (".config");
			else
				XdgConfigHomeFolder = FileFactory.NewForPath (xdg_config_home);
			
			if (string.IsNullOrEmpty (xdg_cache_home))
				XdgCacheHomeFolder = HomeFolder.GetChild (".cache");
			else
				XdgCacheHomeFolder = FileFactory.NewForPath (xdg_cache_home);
			
			if (string.IsNullOrEmpty (xdg_data_home))
				XdgDataHomeFolder = HomeFolder.GetChild (".local").GetChild ("share");
			else
				XdgDataHomeFolder = FileFactory.NewForPath (xdg_data_home);
			
			if (string.IsNullOrEmpty (xdg_data_dirs))
				XdgDataDirFolders = new [] { GLib.FileFactory.NewForPath ("/usr/local/share"), GLib.FileFactory.NewForPath ("/usr/share") };
			else
				XdgDataDirFolders = xdg_data_dirs.Split (':').Select (d => GLib.FileFactory.NewForPath (d));
			
			
			// set the XDG Base Directory specified directories to use
			UserCacheFolder           = XdgCacheHomeFolder.GetChild ("docky");
			UserDataFolder            = XdgDataHomeFolder.GetChild ("docky");
			DockManagerUserDataFolder = XdgDataHomeFolder.GetChild ("dockmanager");
			AutoStartFile             = XdgConfigHomeFolder.GetChild ("autostart").GetChild ("docky.desktop");
			
			
			// ensure all writable directories exist
			EnsureDirectoryExists (UserCacheFolder);
			EnsureDirectoryExists (UserDataFolder);
			EnsureDirectoryExists (DockManagerUserDataFolder);
			EnsureDirectoryExists (XdgConfigHomeFolder.GetChild ("autostart"));
		}
		
		void EnsureDirectoryExists (File dir)
		{
			if (!dir.Exists)
				try {
					dir.MakeDirectoryWithParents (null);
				} catch {
					Log<PathsService>.Fatal ("Could not access the directory '{0}' or create it.  Docky will not work properly unless this folder is writable.", dir.Path);
				}
		}
	}
}

