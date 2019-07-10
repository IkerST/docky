//  
// Copyright (C) 2009 Robert Dyer
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;

using Docky.Services;
using Docky.Services.Prefs;

namespace GMail
{
	/// <summary>
	/// Handles all GMail preferences.
	/// </summary>
	internal class GMailPreferences
	{
		const string UserKey = "User";
		const string PasswordKey = "Password";
		const string RefreshRateKey = "RefreshRate";
		const string FeedUrlKey = "Feed";
		const string LabelsKey = "Labels";
		const string LastCheckedKey = "LastChecked";
		const string NeedsAttentionKey = "NeedsAttention";
		const string NotifyKey = "Notify";
		
		public static IPreferences prefs = DockServices.Preferences.Get<GMailPreferences> ();
		
		/// <value>
		/// </value>
		public static event EventHandler UserChanged;
		
		/// <value>
		/// </value>
		public static void OnUserChanged ()
		{
			if (UserChanged != null)
				UserChanged (null, EventArgs.Empty);
		}
		
		/// <value>
		/// </value>
		public static string User {
 			get { return prefs.Get<string> (UserKey, ""); }
 			set { prefs.Set<string> (UserKey, value); OnUserChanged (); }
 		}
		
		/// <value>
		/// </value>
		public static event EventHandler PasswordChanged;
		
		/// <value>
		/// </value>
		public static void OnPasswordChanged ()
		{
			if (PasswordChanged != null)
				PasswordChanged (null, EventArgs.Empty);
		}
		
		/// <value>
		/// </value>
		public static string Password {
 			get { return prefs.GetSecure<string> (PasswordKey, ""); }
 			set { prefs.SetSecure<string> (PasswordKey, value); OnPasswordChanged (); }
 		}
		
		/// <value>
		/// </value>
		public static event EventHandler RefreshRateChanged;
		
		/// <value>
		/// </value>
		public static void OnRefreshRateChanged ()
		{
			if (RefreshRateChanged != null)
				RefreshRateChanged (null, EventArgs.Empty);
		}
		
		/// <value>
		/// </value>
		public static uint RefreshRate {
 			get { return (uint) prefs.Get<int> (RefreshRateKey, 15); }
 			set { prefs.Set<int> (RefreshRateKey, (int) value); OnRefreshRateChanged (); }
 		}
		
		/// <value>
		/// </value>
		public static event EventHandler FeedUrlChanged;
		
		/// <value>
		/// </value>
		public static void OnFeedUrlChanged ()
		{
			if (FeedUrlChanged != null)
				FeedUrlChanged (null, EventArgs.Empty);
		}
		
		/// <value>
		/// </value>
		public static string FeedUrl {
 			get { return prefs.Get<string> (FeedUrlKey, "mail.google.com/mail/feed/atom"); }
 			set { prefs.Set<string> (FeedUrlKey, value); OnFeedUrlChanged (); }
 		}
		
		/// <value>
		/// </value>
		public static event EventHandler LabelsChanged;
		
		/// <value>
		/// </value>
		public static void OnLabelsChanged ()
		{
			if (LabelsChanged != null)
				LabelsChanged (null, EventArgs.Empty);
		}
		
		/// <value>
		/// </value>
		public static string[] Labels {
 			get { return prefs.Get<string[]> (LabelsKey, new string[] {}); }
 			set { prefs.Set<string[]> (LabelsKey, value); OnLabelsChanged (); }
 		}
		
		/// <value>
		/// </value>
		public static event EventHandler LastCheckedChanged;
		
		/// <value>
		/// </value>
		public static void OnLastCheckedChanged ()
		{
			if (LastCheckedChanged != null)
				LastCheckedChanged (null, EventArgs.Empty);
		}
		
		/// <value>
		/// </value>
		public static DateTime LastChecked {
 			get {
				try {
					return DateTime.Parse (prefs.Get<string> (LastCheckedKey, DateTime.Now.ToString ()));
				} catch (Exception) {
					return DateTime.Now;
				}
			}
 			set { prefs.Set<string> (LastCheckedKey, value.ToString ()); OnLastCheckedChanged (); }
 		}
		
		/// <value>
		/// </value>
		public static event EventHandler NeedsAttentionChanged;
		
		/// <value>
		/// </value>
		public static void OnNeedsAttentionChanged ()
		{
			if (NeedsAttentionChanged != null)
				NeedsAttentionChanged (null, EventArgs.Empty);
		}
		
		/// <value>
		/// </value>
		public static bool NeedsAttention {
 			get { return prefs.Get<bool> (NeedsAttentionKey, true); }
 			set { prefs.Set<bool> (NeedsAttentionKey, value); OnNeedsAttentionChanged (); }
 		}
		
		/// <value>
		/// </value>
		public static event EventHandler NotifyChanged;
		
		/// <value>
		/// </value>
		public static void OnNotifyChanged ()
		{
			if (NotifyChanged != null)
				NotifyChanged (null, EventArgs.Empty);
		}
		
		/// <value>
		/// </value>
		public static bool Notify {
 			get { return prefs.Get<bool> (NotifyKey, true); }
 			set { prefs.Set<bool> (NotifyKey, value); OnNotifyChanged (); }
 		}
	}
}
