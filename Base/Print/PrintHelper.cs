/*
 *  Copyright 2013 
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

using System.Printing;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class contains helper methods concerning printing.
	/// </summary>
	public static class PrintHelper
	{
		/// <summary>
		/// Checks if the print server exists. This method should be called in a separate thread because it can be very time consuming.
		/// </summary>
		/// <param name="serverName">The name of the print server to check.</param>
		/// <returns>True if the print server exists, otherwise false.</returns>
		public static bool CheckPrintServer(string serverName)
		{
			bool ok = true;
			try
			{
				new PrintServer(serverName);
			}
			catch
			{
				ok = false;
			}
			return ok;
		}

		/// <summary>
		/// Checks whether the printer exists. This method should be called in a separate thread because it can be very time consuming.
		/// </summary>
		/// <param name="serverName">The name of the print server to check.</param>
		/// <param name="printerName">The name of the printer to check.</param>
		/// <returns>True if the printer exists, otherwise false.</returns>
		public static bool CheckPrinter(string serverName, string printerName)
		{
			if (string.IsNullOrWhiteSpace(printerName))
			{
				return false;
			}

			bool ok = true;
			try
			{
				new PrintQueue(new PrintServer(serverName), printerName);
			}
			catch
			{
				ok = false;
			}
			return ok;
		}
	}
}
