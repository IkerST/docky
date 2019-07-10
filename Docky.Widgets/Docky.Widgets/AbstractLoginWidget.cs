/* AbstractLoginWidget.cs 
 *
 * GNOME Do is the legal property of its developers. Please refer to the
 * COPYRIGHT file distributed with this
 * source distribution.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;

using Gtk;
using Mono.Unix;

using Docky.Services;

namespace Docky.Widgets
{

	/// <summary>
	/// A class providing a generic login widget for plugins that will need
	/// to log into an external service. Provides a clean UI and enforces
	/// asynchronous validation so the plugin developer doesn't need to know
	/// about delegates or any complex concepts.
	/// </summary>
	[System.ComponentModel.ToolboxItem(true)]
	public abstract partial class AbstractLoginWidget : Bin
	{
		const string ValidatingFormat = "<i>{0}</i>";
		
		protected readonly string NewAccountButtonFormat = Catalog.GetString ("Sign up for {0}");
		protected readonly string BusyValidatingLabel = string.Format (ValidatingFormat, Catalog.GetString ("Validating..."));
		protected readonly string NewAccountLabelFormat = string.Format (ValidatingFormat, Catalog.GetString ("Don't have {0}?"));
		protected readonly string AccountValidationFailedLabel = string.Format (ValidatingFormat, Catalog.GetString ("Account validation failed!"));
		protected readonly string DefaultValidatingLabel = string.Format (ValidatingFormat, Catalog.GetString ("Verify and save account information"));
		protected readonly string AccountValidationSucceededLabel = string.Format (ValidatingFormat, Catalog.GetString ("Account validation succeeded!"));

		LinkButton new_account_button;		
		
		public AbstractLoginWidget (string serviceName) : this (serviceName, "")
		{
		}

		public AbstractLoginWidget (string serviceName, string serviceUri)
		{
			Build();

			// setup the link button
			new_account_button = new LinkButton (serviceUri, string.Format (NewAccountButtonFormat, serviceName));
			new_account_button_box.Add (new_account_button);

			// masks chars for passwords
			password_entry.InnerEntry.Visibility = false;

			password_entry.InnerEntry.Activated += OnPasswordEntryActivated;
			new_account_button.Clicked += OnNewAccountBtnClicked;

			ChangeService (serviceName, serviceUri);
			
			username_entry.Show ();
			password_entry.Show ();

			ShowAll ();

		}

		// give our subclasses access to certain properties of our widgets
		/// <value>
		/// The text in the username entry box
		/// </value>
		protected string Username { 
			get { return username_entry.InnerEntry.Text; }
			set { username_entry.InnerEntry.Text = value; }
		}

		/// <value>
		/// The text in the password entry box
		/// </value>
		protected string Password { 
			get { return password_entry.InnerEntry.Text; }
			set { password_entry.InnerEntry.Text = value; }
		}

		/// <value>
		/// The label next to the username entry box
		/// </value>
		protected string UsernameLabel { 
			get { return username_lbl.Text; }
			set { username_lbl.Markup = value; }
		}

		/// <summary>
		/// The label next to the password entry box
		/// </summary>
		protected string PasswordLabel { 
			get { return password_lbl.Text; }
			set { password_lbl.Markup = value; }
		}

		/// <value>
		/// The label above the validate button
		/// </value>
		protected string ValidateLabel { 
			get { return validate_lbl.Text; }
			set { validate_lbl.Markup = string.Format (ValidatingFormat, value); }
		}

		/// <summary>
		/// Reset widget properties when the service changes, make sure when you use this
		/// that you set the password/username entries to new values. When set to empty strting
		/// this will hide the new account sign up link and label.
		/// </summary>
		/// <param name="serviceName">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="serviceUri">
		/// A <see cref="System.String"/>
		/// </param>
		protected void ChangeService (string serviceName, string serviceUri)
		{
			if (string.IsNullOrEmpty (serviceName) || string.IsNullOrEmpty (serviceUri)) {
				new_account_lbl.Visible = false;
				new_account_button.Visible = false;

				return;
			}

			new_account_button.Uri = serviceUri;
			validate_lbl.Markup = DefaultValidatingLabel;
			new_account_lbl.Markup = string.Format (NewAccountLabelFormat, serviceName);
			new_account_button.Label = string.Format (NewAccountButtonFormat, serviceName);
		}
		
		/// <summary>
		/// Puts a widget at the top of the page above the username entry.
		/// </summary>
		/// <param name="widget">
		/// A <see cref="Widget"/>
		/// </param>
		protected void InsertWidgetAtTop (Widget widget)
		{
			wrapper_vbox.Add (widget);
			Box.BoxChild wrapperSpace = (Box.BoxChild) wrapper_vbox[widget];
			wrapperSpace.Position = 0;
			wrapperSpace.Fill = false;
			wrapperSpace.Expand = false;
			wrapperSpace.Padding = 5;
		}

		/// <summary>
		/// Save account data to disk
		/// </summary>
		/// <param name="username">
		/// A <see cref="System.String"/> username
		/// </param>
		/// <param name="password">
		/// A <see cref="System.String"/> password
		/// </param>
		abstract protected void SaveAccountData (string username, string password);

		/// <summary>
		/// Check that username and password entered by user are valid
		/// This method is executed asychronously so don't be afraid to make slow
		/// web calls or something, we desensitize the apply button, and alert the
		/// user that their credentials are being validated.
		/// </summary>
		/// <param name="username">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="password">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		abstract protected bool Validate (string username, string password);

		void OnValidateBtnClicked (object sender, System.EventArgs e)
		{
			validate_lbl.Markup = BusyValidatingLabel;
			validate_btn.Sensitive = false;
			
			string username = username_entry.InnerEntry.Text;
			string password = password_entry.InnerEntry.Text;
			
			Thread thread = new Thread (new ThreadStart(() => ValidateCredentials (username, password)));
			
			thread.IsBackground = true;
			thread.Start ();
		}

		void OnNewAccountBtnClicked (object sender, EventArgs e)
		{
			DockServices.System.Open (new_account_button.Uri);
		}
		
		void ValidateCredentials (string username, string password)
		{
			bool valid = Validate (username, password);
			DockServices.System.RunOnMainThread (delegate { UpdateInterface (username, password, valid); });
		}

		void UpdateInterface (string username, string password, bool valid)
		{
			if (valid) {
				validate_lbl.Markup = AccountValidationSucceededLabel;
				SaveAccountData (username, password);
			} else {
				validate_lbl.Markup = AccountValidationFailedLabel;
			}
			validate_btn.Sensitive = true;
		}

		void OnPasswordEntryActivated (object sender, System.EventArgs e)
		{
			validate_btn.Activate ();
		}
	}
}
