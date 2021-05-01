/*
 *  Copyright 
 *		Thomas Sadleder
 *		Christoph Zimprich
 *
 *  This file is part of Alarm-Info-Center.
 *
 *  Alarm-Info-Center is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Alarm-Info-Center is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Alarm-Info-Center. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Globalization;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class contains functions for creating alarm announcements.
	/// </summary>
	public class Announcement
	{
		/// <summary>
		/// Performs formatting for announcements.
		/// </summary>
		/// <param name="text">The text to format.</param>
		/// <returns>Returns the formatted text.</returns>
		private static string FormatText(string text)
		{
			text = text.Replace("ß", "ss");
			text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());

			// Replace special german characters
			text = ReplaceGermanCharacters(text);

			// Replace texts that should be in uppercase or replace abbreviations
			text = text.Replace("Kfz", "KFZ");
			text = text.Replace("Km", "Kilometer");

			// Put a comma behind every word
			text = text.Replace(" ", ", ");

			return text;
		}

		/// <summary>
		/// Replaces german characters. E.g. ä -> ae, Ü -> Ue, ß -> ss,...
		/// </summary>
		/// <param name="text">The text to be replaced.</param>
		/// <returns>The text with the replaced characters.</returns>
		private static string ReplaceGermanCharacters(string text)
		{
			text = text.Replace("Ä", "Ae");
			text = text.Replace("Ö", "Oe");
			text = text.Replace("Ü", "Ue");
			text = text.Replace("ä", "ae");
			text = text.Replace("ö", "oe");
			text = text.Replace("ü", "ue");
			text = text.Replace("ß", "ss");
			return text;
		}

		/// <summary>
		/// Creates the URI for the announcement.
		/// </summary>
		/// <param name="alarm">The alarm to be announced.</param>
		/// <returns>The URI of the announcement.</returns>
		public static Uri CreateUrl(Alarm alarm)
		{
			string subject = FormatText(alarm.Subject);
			string location = FormatText(alarm.MapLocation);

			string url = Constants.GoogleTranslateUrl + subject + ", " + location;
			return new Uri(url);
		}
	}
}
