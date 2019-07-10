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

namespace GMail
{
	/// <summary>
	/// A class for passing error messages from GMail events.
	/// </summary>
	public class GMailErrorArgs : EventArgs
	{
		/// <value>
		/// The error message generated.
		/// </value>
		public string Error { get; protected set; }
		
		/// <summary>
		/// Constructs a new GMailErrorArgs object.
		/// </summary>
		/// <param name="error">
		/// A <see cref="System.String"/> representing the error message.
		/// </param>
		public GMailErrorArgs (string error)
		{
			Error = error;
		}
	}
}
