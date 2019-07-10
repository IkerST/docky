//  
//  Copyright (C) 2009 Robert Dyer, Chris Szikszoy
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
using System.Text.RegularExpressions;

using Gtk;
using Mono.Unix;

using Docky.Widgets;

namespace GMail
{

	public class GMailLoginConfig : AbstractLoginWidget
	{
		const string EmailPattern = @"[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\."
            + @"[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*"
            + @"[a-zA-Z0-9])?\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?";
            
        const string Uri = "https://www.google.com/accounts/NewAccount?service=mail";
		
		public GMailLoginConfig () : base (Catalog.GetString ("Gmail"), Uri)
		{
			Username = GMailPreferences.User;
			Password = GMailPreferences.Password;
			Name = Catalog.GetString ("Login");
		}
		
		protected override void SaveAccountData (string username, string password)
		{
			GMailPreferences.User = username;
			GMailPreferences.Password = password;
			GMailAtom.SettingsChanged ();
		}
		
		protected override bool Validate (string username, string password)
		{
			if (!ValidateUsername (username))
				return false;
			return GMailAtom.ValidateCredentials (username, password);
		}
		
		bool ValidateUsername (string username)
		{			
			if (username.IndexOf ('@') != -1)
				return new Regex (EmailPattern, RegexOptions.Compiled).IsMatch (username);
			return true;
		}
	}
}
