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
using System.Windows.Threading;

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// Interaction logic for PrintWindow.xaml
	/// </summary>
	public partial class PrintWindow
	{
		public PrintWindow(string printServer, string printerName)
		{
			InitializeComponent();

			// Show print server and printer name
			string text = string.IsNullOrWhiteSpace(printerName) ? string.Empty : printerName;
			if (!string.IsNullOrWhiteSpace(printServer))
			{
				if (text.Length > 0)
				{
					text += " auf ";
				}
				text += printServer;
			}
			PrinterInfo.Text = text;
		}

		/// <summary>
		/// Shows the window and closes it after the specified time.
		/// </summary>
		/// <param name="seconds">The amount of time in seconds that the windows should be shown.</param>
		public void ShowAndClose(int seconds = 5)
		{
			Show();

			var timer = new DispatcherTimer();
			timer.Interval = new TimeSpan(0, 0, seconds);
			timer.Tick += delegate
			{
				timer.Stop();
				Close();
			};
			timer.Start();
		}
	}
}
