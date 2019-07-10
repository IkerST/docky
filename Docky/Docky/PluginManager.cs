//  
//  Copyright (C) 2009 Jason Smith, Chris Szikszoy
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
using System.IO;
using System.Xml;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Mono.Addins;

using Docky.Items;
using Docky.Services;
using Docky.Widgets;

namespace Docky
{
	public class AddinStateChangedEventArgs : EventArgs
	{
		public Addin Addin { get; private set; }
		public bool State { get; private set; }
		
		public AddinStateChangedEventArgs (Addin addin, bool state)
		{
			Addin = addin;
			State = state;
		}
	}
	
	public class PluginManager
	{
		/// <summary>
		/// The default icon for addins that don't supply one.
		/// </summary>
		public static string DefaultPluginIcon {
			get {
				return "package";
			}
		}
		
		public static Dictionary<Addin, Dictionary<string, string>> AddinMetadata { get; private set; }
		
		const string IPExtensionPath = "/Docky/ItemProvider";
		const string ConfigExtensionPath = "/Docky/Configuration";
		
		public static event EventHandler<AddinStateChangedEventArgs> AddinStateChanged;

		//// <value>
		/// Directory where Docky saves its Mono.Addins repository cache.
		/// </value>
		public static GLib.File UserPluginsDirectory {
			get { return DockServices.Paths.UserDataFolder.GetChild ("plugins"); }
		}
		
		/// <summary>
		/// Directory where Docky saves Addin files.
		/// </summary>
		public static GLib.File UserAddinInstallationDirectory {
			get { return UserPluginsDirectory.GetChild ("addins"); }
		}

		/// <summary>
		/// Performs plugin system initialization. Should be called before this
		/// class or any Mono.Addins class is used. The ordering is very delicate.
		/// </summary>
		public static void Initialize ()
		{
			// Initialize Mono.Addins.
			try {
				AddinManager.Initialize (UserPluginsDirectory.Path);
			} catch (InvalidOperationException e) {
				Log<PluginManager>.Error ("AddinManager.Initialize: {0}", e.Message);
				Log<PluginManager>.Warn ("Rebuild Addin.Registry and reinitialize AddinManager");
				AddinManager.Registry.Rebuild (null);
				AddinManager.Shutdown ();
				AddinManager.Initialize (UserPluginsDirectory.Path);
			}

			AddinManager.Registry.Update (null);
			
			// parse the addin config files for extended metadata
			AddinMetadata = new Dictionary<Addin, Dictionary<string, string>> ();
			DockServices.System.RunOnThread (() => {
				AllAddins.ToList ().ForEach (a => ParseAddinConfig (a)); 
			});
			
			// Add feedback when addin is loaded or unloaded
			AddinManager.AddinLoaded += AddinManagerAddinLoaded;
			AddinManager.AddinUnloaded += AddinManagerAddinUnloaded;
			
			Log<PluginManager>.Debug ("Plugin manager initialized.");
		}
		
		/// <summary>
		/// Shut down the Addin Manager.
		/// </summary>
		public static void Shutdown ()
		{
			AddinManager.Shutdown ();
		}
		
		static void OnStateChanged (Addin addin, bool enabled)
		{
			if (AddinStateChanged != null)
				AddinStateChanged (null, new AddinStateChangedEventArgs (addin, enabled));
		}

		static void AddinManagerAddinLoaded (object sender, AddinEventArgs args)
		{
			Addin addin = AddinFromID (args.AddinId);
			OnStateChanged (addin, true);
			Log<PluginManager>.Info ("Loaded \"{0}\".", addin.Name);
		}

		static void AddinManagerAddinUnloaded (object sender, AddinEventArgs args)
		{
			Addin addin = AddinFromID (args.AddinId);
			OnStateChanged (addin, false);
			Log<PluginManager>.Info ("Unloaded \"{0}\".", addin.Name);
		}
		
		/// <summary>
		/// Look up an addin by supplying the Addin ID.
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="Addin"/>
		/// </returns>
		public static Addin AddinFromID (string id)
		{
			return AddinManager.Registry.GetAddin (id);
		}
		
		/// <summary>
		/// Enable the addin by supplying the Addin ID.
		/// </summary>
		/// <param name="addin">
		/// A <see cref="Addin"/>
		/// </param>
		/// <returns>
		/// A <see cref="AbstractDockItemProvider"/>
		/// </returns>
		public static AbstractDockItemProvider Enable (Addin addin)
		{
			addin.Enabled = true;
			return ItemProviderFromAddin (addin.Id);
		}
		
		/// <summary>
		/// Enable the addin by supplying the <see cref="AbstractDockItemProvider"/>.
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="AbstractDockItemProvider"/>
		/// </returns>
		public static AbstractDockItemProvider Enable (string id)
		{
			return Enable (AddinFromID (id));
		}
		
		/// <summary>
		/// Disable the Addin by supplying the <see cref="Addin"/>.
		/// </summary>
		/// <param name="addin">
		/// A <see cref="Addin"/>
		/// </param>
		public static void Disable (Addin addin)
		{
			addin.Enabled = false;
		}
		
		/// <summary>
		/// Disable the addin by supplying the Addin ID.
		/// </summary>
		/// <param name="id">
		/// A <see cref="System.String"/>
		/// </param>
		public static void Disable (string id)
		{
			Disable (AddinFromID (id));
		}
		
		/// <summary>
		/// Disable an addin by supplying the <see cref="AbstractDockItemProvider"/>.
		/// </summary>
		/// <param name="provider">
		/// A <see cref="AbstractDockItemProvider"/>
		/// </param>
		public static void Disable (AbstractDockItemProvider provider)
		{
			Disable (AddinIDFromProvider (provider));
		}
		
		/// <summary>
		/// All addins in the Addins registry.
		/// </summary>
		public static IEnumerable<Addin> AllAddins {
			get {
				return AddinManager.Registry.GetAddins ();
			}
		}
		/// <summary>
		/// Installs all addins from the user addin directory.
		/// </summary>
		public static void InstallLocalPlugins ()
		{	
			IEnumerable<string> manual;
			
			manual = UserAddinInstallationDirectory.GetFiles ("*.dll").Select (f => f.Basename);
					
			manual.ToList ().ForEach (dll => Log<PluginManager>.Info ("Installing {0}", dll));
			
			AddinManager.Registry.Rebuild (null);
				
			manual.ToList ().ForEach (dll => File.Delete (dll));
		}
		
		static T ObjectFromAddin<T> (string extensionPath, string addinID) where T : class
		{
			IEnumerable<TypeExtensionNode> nodes = AddinManager.GetExtensionNodes (extensionPath)
				.OfType<TypeExtensionNode> ()
				.Where (a => Addin.GetIdName (a.Addin.Id) == Addin.GetIdName (addinID));
			
			if (nodes.Any ())
				return nodes.First ().GetInstance () as T;
			return null;
		}
		
		/// <summary>
		/// Returns the <see cref="AbstractDockItemProvider"/> from the supplied Addin ID.
		/// </summary>
		/// <param name="addinID">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="AbstractDockItemProvider"/>
		/// </returns>
		public static AbstractDockItemProvider ItemProviderFromAddin (string addinID)
		{
			return ObjectFromAddin<AbstractDockItemProvider> (IPExtensionPath, addinID);
		}

		/// <summary>
		/// Returns the <see cref="ConfigDialog"/> from the supplied Addin ID.
		/// </summary>
		/// <param name="addinID">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="ConfigDialog"/>
		/// </returns>
		public static ConfigDialog ConfigForAddin (string addinID)
		{
			return  ObjectFromAddin<ConfigDialog> (ConfigExtensionPath, addinID);
		}
		
		/// <summary>
		/// Returns the Addin ID from an <see cref="AbstractDockItemProvider"/>.
		/// </summary>
		/// <param name="provider">
		/// A <see cref="AbstractDockItemProvider"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static string AddinIDFromProvider (AbstractDockItemProvider provider)
		{
			foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes (IPExtensionPath)) {
				AbstractDockItemProvider nodeProvider;
				
				try {
					nodeProvider = node.GetInstance () as AbstractDockItemProvider;
				} catch {
					continue;
				}
				
				if (nodeProvider.Name == provider.Name)
					return node.Addin.Id;
			}
			
			// shouldn't happen
			return "";
		}

		/// <value>
		/// All loaded ItemProviders.
		/// </value>
		public static IEnumerable<AbstractDockItemProvider> ItemProviders {
			get { 
				try {
					return AddinManager.GetExtensionObjects (IPExtensionPath).OfType<AbstractDockItemProvider> ();
				} catch (Exception e) {
					Log<PluginManager>.Error ("{0}", e.Message);
					Log<PluginManager>.Info (e.StackTrace);
					return Enumerable.Empty<AbstractDockItemProvider> ();
				}
			}
		}
		
		/// <summary>
		/// A list of Provider IDs that are currently not used by any docks
		/// </summary> 
		public static IEnumerable<string> AvailableProviderIDs {
			get {
				return AllAddins.Where (a => !a.Enabled).Select (a => Addin.GetIdName (a.Id));
			}
		}
		
		static void ParseAddinConfig (Addin addin)
		{
			Log<PluginManager>.Debug ("Processing config file for \"{0}\".", addin.Name);
			Assembly addinAssembly = Assembly.LoadFile (addin.AddinFile);
			
			string addinManifestName = addinAssembly.GetManifestResourceNames ().FirstOrDefault (res => res.Contains ("addin.xml"));
			
			if (string.IsNullOrEmpty (addinManifestName)) {
				Log<PluginManager>.Warn ("Could not find addin manifest for '{0}'.", addin.AddinFile);
				return;
			}
			
			using (Stream s = addinAssembly.GetManifestResourceStream (addinManifestName)) {
				XmlDocument addinManifest = new XmlDocument ();
				addinManifest.Load (s);
				
				if (!AddinMetadata.ContainsKey (addin))
					AddinMetadata[addin] = new Dictionary<string, string> ();
				
				foreach (XmlAttribute a in addinManifest.SelectSingleNode ("/Addin").Attributes)
					AddinMetadata [addin] [a.Name] = a.Value;	
			}
			
			AddinMetadata [addin] ["AssemblyFullName"] = addinAssembly.FullName;
			
			if (AddinMetadata [addin].ContainsKey ("icon") && AddinMetadata [addin] ["icon"].EndsWith ("@")) {
				AddinMetadata [addin] ["icon"] = string.Format ("{0}{1}", AddinMetadata [addin] ["icon"], addinAssembly.FullName);
			}
		}
	}
}
