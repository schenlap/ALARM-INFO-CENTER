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
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// This class holds some handy tools.
	/// </summary>
	public static class TaskBarNotifier
	{
		private static NotifyIcon mNotifyIcon;

		/// <summary>
		/// Sets the notify icon so that it can be used.
		/// </summary>
		/// <param name="notifyIcon">A valid and instantiated notify icon.</param>
		public static void SetNotifyIcon(NotifyIcon notifyIcon)
		{
			mNotifyIcon = notifyIcon;
		}

		/// <summary>
		/// Displays a text in the tray for one second.
		/// </summary>
		/// <param name="text">The text to display.</param>
		public static void ShowNotifyMessage(string text)
		{
			if (mNotifyIcon != null)
			{
				mNotifyIcon.ShowBalloonTip(1000, "Alarm-Info-Center (AIC)", text, ToolTipIcon.Info);
			}
		}

		/// <summary>
		/// Removes the notify icon from the tray. If you do not dispose it, the icon will stay in the tray after the program exited.
		/// </summary>
		public static void DisposeNotifyIcon()
		{
			if (mNotifyIcon != null)
			{
				mNotifyIcon.Dispose();
				mNotifyIcon = null;
			}
		}

		/// <summary>
		/// Shows an information screen for the program.
		/// </summary>
		/// <param name="owner">The owner is necessary when the info is called via the tray.</param>
		public static void ShowInfo(Window owner)
		{
			string nl = Environment.NewLine;

			string info =
				"Alarm-Info-Center (AIC)" + Environment.NewLine +
				"Version: " + Assembly.GetExecutingAssembly().GetName().Version + nl + nl +
				"Tastenkürzel:" + nl +
				"[F1] ... Info" + nl +
				"[F5] ... Alarmdaten anfordern, Info-Center Daten neu laden" + nl +
				"[F7] ... Sprachdurchsage starten" + nl +
				"[F8] ... Sprachdurchsage stoppen" + nl +
				"[→] ... nächsten Einsatz / nächste Infoseite anzeigen" + nl +
				"[←] ... vorherigen Einsatz / vorherige Infoseite anzeigen" + nl +
				"[STRG + P] ... Alarm drucken";
			
			System.Windows.MessageBox.Show(owner, info, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}
