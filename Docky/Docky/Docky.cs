//  
//  Copyright (C) 2009-2010 Jason Smith, Robert Dyer, Chris Szikszoy
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

using System.Threading;

using Mono.Unix;

using DBus;
using Gtk;
using Notifications;

using Docky.DBus;
using Docky.Services;

namespace Docky
{
	public static class Docky
	{
		public static UserArgs CommandLinePreferences { get; private set; }
		
		static DockController controller;
		internal static DockController Controller { 
			get {
				if (controller == null)
					controller = new DockController ();
				return controller;
			}
		}
		
		public static void Main (string[] args)
		{
			// output the version number & system info
			Log.DisplayLevel = LogLevel.Info;
			Log.Info ("Docky version: {0} {1}", AssemblyInfo.DisplayVersion, AssemblyInfo.VersionDetails);
			Log.Info ("Kernel version: {0}", System.Environment.OSVersion.Version);
			Log.Info ("CLR version: {0}", System.Environment.Version);
			
			//Init gtk and GLib related
			Catalog.Init ("docky", AssemblyInfo.LocaleDirectory);
			if (!GLib.Thread.Supported)
				GLib.Thread.Init ();
			Gdk.Threads.Init ();
			Gtk.Application.Init ("Docky", ref args);
			GLib.GType.Init ();
			try {
				BusG.Init ();
			} catch {
				Log.Fatal ("DBus could not be found and is required by Docky. Exiting.");
				return;	
			}
			
			// process the command line args
			if (!UserArgs.Parse (args))
				return;
			
			DockServices.Init (UserArgs.DisableDockManager);
			
			Wnck.Global.ClientType = Wnck.ClientType.Pager;
			
			// set process name
			DockServices.System.SetProcessName ("docky");
			
			// cache main thread
			DockServices.System.MainThread = Thread.CurrentThread;
			
			// check compositing
			if (Controller.CompositeCheckEnabled) {
				CheckComposite (8);
				Gdk.Screen.Default.CompositedChanged += delegate {
					CheckComposite (2);
				};
			}
			
			if (!DBusManager.Default.Initialize (UserArgs.DisableDockManager)) {
				Log.Fatal ("Another Docky instance was detected - exiting.");
				return;	
			}
				
			DBusManager.Default.QuitCalled += delegate {
				Quit ();
			};
			DBusManager.Default.SettingsCalled += delegate {
				ConfigurationWindow.Instance.Show ();
			};
			DBusManager.Default.AboutCalled += delegate {
				ShowAbout ();
			};
			PluginManager.Initialize ();
			Controller.Initialize ();
			
			Gdk.Threads.Enter ();
			Gtk.Application.Run ();
			Gdk.Threads.Leave ();
			
			Controller.Dispose ();
			DockServices.Dispose ();
			PluginManager.Shutdown ();
		}
		
		static uint checkCompositeTimer = 0;
		static Notification compositeNotify = null;
		
		static void CheckComposite (uint timeout)
		{
			if (checkCompositeTimer > 0) {
				GLib.Source.Remove (checkCompositeTimer);
				checkCompositeTimer = 0;
			}
			
			// compositing is enabled and we are still showing a notify, so close it
			if (Gdk.Screen.Default.IsComposited && compositeNotify != null) {
				compositeNotify.Close ();
				compositeNotify = null;
				return;
			}
			
			checkCompositeTimer = GLib.Timeout.Add (timeout * 1000, delegate {
				checkCompositeTimer = 0;

				// no matter what, close any notify open
				if (compositeNotify != null) {
					compositeNotify.Close ();
					compositeNotify = null;
				}
				
				if (!Gdk.Screen.Default.IsComposited)
					compositeNotify = Log.Notify (Catalog.GetString ("Docky requires compositing to work properly. " +
						"Certain options are disabled and themes/animations will look incorrect. "));
				
				return false;
			});
		}
		
		public static void ShowAbout ()
		{
			Gtk.AboutDialog about = new Gtk.AboutDialog ();
			about.ProgramName = "Docky";
			about.Version = AssemblyInfo.DisplayVersion + "\n" + AssemblyInfo.VersionDetails;
			about.IconName = "docky";
			about.LogoIconName = "docky";
			about.Website = "https://launchpad.net/docky";
			about.WebsiteLabel = "Website";
			Gtk.AboutDialog.SetUrlHook ((dialog, link) => DockServices.System.Open (link));
			about.Copyright = "Copyright \xa9 2009-2015 Docky Developers";
			about.Comments = "Docky. Simply Powerful.";
			about.Authors = new[] {
				"Jason Smith <jason@go-docky.com>",
				"Robert Dyer <robert@go-docky.com>",
				"Chris Szikszoy <chris@go-docky.com>",
				"Rico Tzschichholz <rtz@go-docky.com>",
				"Seif Lotfy <seif@lotfy.com>",
				"Chris Halse Rogers <raof@ubuntu.com>",
				"Alex Launi <alex.launi@gmail.com>",
				"Florian Dorn <florian.dorn@h3c.de>",
			};
			about.Artists = new[] { 
				"Daniel Foré <bunny@go-docky.com>",
			};
			about.Documenters = new[] {
				"Sven Mauhar <zekopeko@gmail.com>",
				"Robert Dyer <robert@go-docky.com>",
				"Daniel Foré <bunny@go-docky.com>",
				"Chris Szikszoy <chris@go-docky.com>",
				"Rico Tzschichholz <rtz@go-docky.com>",
			};
			about.TranslatorCredits = 
				"Asturian\n" +
				" Xuacu Saturio\n" +
				"\n" +
				
				"Basque\n" +
				" Ibai Oihanguren\n" +
				"\n" +
				
				"Bengali\n" +
				" Scio\n" +
				"\n" +
				
				"Brazilian Portuguese\n" +
				" André Gondim, Fabio S Monteiro, Flávio Etrusco, Glauco Vinicius\n" +
				" Lindeval, Thiago Bellini, Victor Mello\n" +
				"\n" +
				
				"Bulgarian\n" +
				" Boyan Sotirov, Krasimir Chonov\n" +
				"\n" +
				
				"Catalan\n" +
				" BadChoice, Siegfried Gevatter\n" +
				"\n" +
				
				"Chinese (Simplified)\n" +
				" Chen Tao, G.S.Alex, Xhacker Liu, fighterlyt, lhquark, skatiger, 冯超\n" +
				"\n" +
				
				"Croatian\n" +
				" Saša Teković, zekopeko\n" +
				"\n" +
				
				"English (United Kingdom)\n" +
				" Alex Denvir, Daniel Bell, David Wood, Joel Auterson, SteVe Cook\n" +
				"\n" +
				
				"Finnish\n" +
				" Jiri Grönroos\n" +
				"\n" +
				
				"French\n" +
				" Hugo M., Kévin Gomez, Pierre Slamich\n" +
				" Simon Richard, alienworkshop, maxime Cheval\n" +
				"\n" +
				
				"Galician\n" +
				" Francisco Diéguez, Indalecio Freiría Santos, Miguel Anxo Bouzada, NaNo\n" +
				"\n" +
				
				"German\n" +
				" Cephinux, Gabriel Shahzad, Jan-Christoph Borchardt, Mark Parigger\n" + 
				" Martin Lettner, augias, fiction, pheder, tai\n" +
				"\n" +
				
				"Hebrew\n" +
				" Uri Shabtay\n" +
				"\n" +
				
				"Hindi\n" +
				" Bilal Akhtar\n" +
				"\n" +
				
				"Hungarian\n" +
				" Bognár András, Gabor Kelemen, Jezsoviczki Ádám, NewPlayer\n" +
				"\n" +
				
				"Icelandic\n" +
				" Baldur, Sveinn í Felli\n" +
				"\n" +
				
				"Indonesian\n" +
				" Andika Triwidada, Fakhrul Rijal\n" +
				"\n" +
				
				"Italian\n" +
				" Andrea Amoroso, Blaster, Ivan, MastroPino, Michele, Milo Casagrande, Quizzlo\n" +
				"\n" +
				
				"Japanese\n" +
				" kawaji\n" +
				"\n" +
				
				"Korean\n" +
				" Bugbear5, Cedna\n" +
				"\n" +
				
				"Polish\n" +
				" 313, Adrian Grzemski, EuGene, Rafał Szalecki, Stanisław Gackowski, bumper, emol007\n" +
				"\n" +
				
				"Romanian\n" +
				" Adi Roiban, George Dumitrescu\n" +
				"\n" +
				
				"Russian\n" +
				" Alexander Semyonov, Alexey Nedilko, Andrey Sitnik, Artem Yakimenko\n" +
				" Dmitriy Bobylev, Ivan, Phenomen, Sergey Demurin, Sergey Sedov\n" +
				" SochiX, Vladimir, legin, sX11\n" +
				"\n" +
				
				"Spanish\n" +
				" Alejandro Navarro, David, DiegoJ, Edgardo Fredz, FAMM, Fuerteventura\n" +
				" Gus, José A. Fuentes Santiago, Julián Alarcón, Malq, Martín V.\n" +
				" Omar Campagne, Ricardo Pérez López, Sebastián Porta, alvin23, augias, elXATU\n" +
				"\n" +
				
				"Swedish\n" +
				" Daniel Nylander, Rovanion, riiga\n" +
				"\n" +
				
				"Turkish\n" +
				" Yalçın Can, Yiğit Ateş\n" +
				"\n" +
				
				"Ukrainian\n" +
				" naker.ua\n";
			
			about.ShowAll ();
			
			about.Response += delegate {
				about.Hide ();
				about.Destroy ();
			};
			
		}
		
		public static void Quit ()
		{
			DBusManager.Default.Shutdown ();
			Gtk.Application.Quit ();
		}
	}
}
