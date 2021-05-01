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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AlarmInfoCenter.Base
{
	public static class Utilities
	{
		/// <summary>
		/// Returns the current date and time as formatted string (e.g. 02.12.2012 - 14:31 Uhr).
		/// </summary>
		public static string GetFormattedDateTime()
		{
			var now = DateTime.Now;
			return string.Format("{0} - {1} Uhr", now.ToLongDateString(), now.ToShortTimeString());
		}

		/// <summary>
		/// Indicates whether a specific moment is between to other points of time.
		/// </summary>
		/// <param name="current">The current time.</param>
		/// <param name="start">The start time.</param>
		/// <param name="end">The end time</param>
		/// <returns>True if the current time is between start and end time.</returns>
		public static bool IsBetweenTime(TimeSpan current, TimeSpan start, TimeSpan end)
		{
			// Do not include the boundaries
			if (start == end || current == start || current == end)
			{
				return false;
			}

			// Easy algorithm if the start time is always less than the end time
			bool invertResult = false;
			if (start > end)
			{
				var ts = end;
				end = start;
				start = ts;

				invertResult = true;
			}

			bool yes = start < current && current < end;

			if (invertResult)
			{
				yes = !yes;
			}

			return yes;
		}

		/// <summary>
		/// Gets integer values of a string that is separated by any separation character.
		/// </summary>
		/// <param name="text">The string to be splitted.</param>
		/// <returns>Null if no integer value has been found otherwise a list of integer values.</returns>
		public static List<int> GetAnnouncmentIntervals(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}

			var ints = new List<int>();
			var strings = text.Split(' ', ';', ',', '|', '-', '_', '.');
			foreach (string s in strings)
			{
				int result;
				bool ok = int.TryParse(s, out result);
				if (ok)
				{
					ints.Add(result);
				}
			}
			return ints.Count == 0 ? null : ints;
		}

		/// <summary>
		/// Converts the given stream data to a string.
		/// </summary>
		/// <param name="stream">The stream to convert to a string.</param>
		/// <param name="encoding">The encoding used during reading.</param>
		/// <returns>String representation of the stream's data.</returns>
		public static string StreamToString(Stream stream, Encoding encoding)
		{
			stream.Position = 0;

			using (StreamReader reader = new StreamReader(stream, encoding))
			{
				return reader.ReadToEnd();
			}
		}
	}
}
