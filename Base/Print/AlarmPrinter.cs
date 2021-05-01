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

using System;
using System.Printing;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Xps;
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// A class for creating a printout of alarms.
	/// </summary>
	public class AlarmPrinter
	{
		private int mPrintCount = 1;

		/// <summary>
		/// The name of the print server.
		/// </summary>
		public string ServerName { get; private set; }

		/// <summary>
		/// The name of the printer.
		/// </summary>
		public string PrinterName { get; private set; }


		/// <summary>
		/// Initializes a new instance of the AlarmPrinter class.
		/// </summary>
		/// <param name="serverName">The name of the print server.</param>
		/// <param name="printerName">The name of the printer.</param>
		public AlarmPrinter(string serverName, string printerName)
		{
			ServerName = string.IsNullOrWhiteSpace(serverName) ? null : serverName;
			PrinterName = printerName;
		}

		/// <summary>
		/// Prints the alarm for the given number of times. Important: This method must be started from an STA thread.
		/// Under some circumstances printing in background causes threading problems (maybe it depends on the printer).
		/// </summary>
		/// <param name="alarm">The alarm to be printed.</param>
		/// <param name="printCount">The number of print copies.</param>
		/// <param name="backgroundThread">Indicates whether the printing should occur in a separate STA thread.</param>
		public void Print(Alarm alarm, int printCount = 1, bool backgroundThread = false)
		{
			if (alarm == null || string.IsNullOrWhiteSpace(PrinterName) || printCount < 1)
			{
				return;
			}

			mPrintCount = printCount;

			if (backgroundThread)
			{
				Thread t = new Thread(() => CreatePrinting(alarm));
				t.SetApartmentState(ApartmentState.STA);				// Always use STA, otherwise printing will fail
				t.IsBackground = true;
				t.Start();
			}
			else
			{
				CreatePrinting(alarm);
			}
		}

		/// <summary>
		/// Creates the document for printing and sends it to the printer.
		/// </summary>
		/// <param name="alarm">The alarm to be printed.</param>
		private void CreatePrinting(Alarm alarm)
		{
			try
			{
				Printout printout = new Printout(alarm, AicSettings.Global.FireBrigadeCoordinate);
				FlowDocument doc = printout.CreatePrintout();
				PrintQueue queue = new PrintQueue(new PrintServer(ServerName), PrinterName);
				XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(queue);
				PrintTicket ticket = new PrintTicket();
				ticket.CopyCount = mPrintCount;
				writer.Write((doc as IDocumentPaginatorSource).DocumentPaginator, ticket);
			}
			catch (Exception ex)
			{
				Log.GetInstance().LogError("Error in printing the alarm.", ex);
			}
		}
	}
}
