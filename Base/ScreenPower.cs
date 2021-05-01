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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AlarmInfoCenter.Base
{
	///<summary>
	/// A class that manages the power options of the screen.
	///</summary>
	public static class ScreenPower
	{
		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(int hWnd, int Msg, int wParam, int lParam);

		[DllImport("user32.dll")]
		static extern void mouse_event(Int32 dwFlags, Int32 dx, Int32 dy, Int32 dwData, UIntPtr dwExtraInfo);

		private const int WM_SYSCOMMAND = 0x0112;
		private const int SC_MONITORPOWER = 0xF170;
		private const int HWND_BROADCAST = 0xFFFF;
		private const int MOUSEEVENTF_MOVE = 0x0001;

		/// <summary>
		/// The different power modes of a screen.
		/// </summary>
		enum ScreenPowerMode
		{
			On = -1,
			StandBy = 1,
			Off = 2
		}

		///<summary>
		/// Turns the screen on.
		///</summary>
		///<param name="handle">The main window of the application.</param>
		public static void TurnOn(int handle)
		{
			// This method does not work on every OS and monitor
			ChangeScreenPower(handle, ScreenPowerMode.On);

			// Move the mouse in order to wake up the monitor
			// http://stackoverflow.com/questions/12572441/sendmessage-sc-monitorpower-wont-turn-monitor-on-when-running-windows-8/14171736#14171736
			mouse_event(MOUSEEVENTF_MOVE, 0, 1, 0, UIntPtr.Zero);
			System.Threading.Thread.Sleep(40);
			mouse_event(MOUSEEVENTF_MOVE, 0, -1, 0, UIntPtr.Zero);
		}

		///<summary>
		/// Turns the screen on.
		///</summary>
		///<param name="window">The main window of the application.</param>
		public static void TurnOn(Window window)
		{
			// This method does not work on every OS and monitor
			ChangeScreenPower(window, ScreenPowerMode.On);

			// Move the mouse in order to wake up the monitor
			// http://stackoverflow.com/questions/12572441/sendmessage-sc-monitorpower-wont-turn-monitor-on-when-running-windows-8/14171736#14171736
			mouse_event(MOUSEEVENTF_MOVE, 0, 1, 0, UIntPtr.Zero);
			System.Threading.Thread.Sleep(40);
			mouse_event(MOUSEEVENTF_MOVE, 0, -1, 0, UIntPtr.Zero);
		}

		///<summary>
		/// Turns the screen off.
		///</summary>
		///<param name="window">The main window of the application.</param>
		public static void TurnOff(Window window)
		{
			ChangeScreenPower(window, ScreenPowerMode.Off);
		}

		///<summary>
		/// Turns the screen off.
		///</summary>
		///<param name="handle">The window handle of the application.</param>
		public static void TurnOff(int handle)
		{
			ChangeScreenPower(handle, ScreenPowerMode.Off);
		}

		///<summary>
		/// Turns the screen to stand-by mode.
		///</summary>
		///<param name="window">The main window of the application.</param>
		public static void TurnStandBy(Window window)
		{
			ChangeScreenPower(window, ScreenPowerMode.StandBy);
		}

		/// <summary>
		/// Changes the power mode of the screen to the provided value.
		/// </summary>
		/// <param name="window">The window of the application.</param>
		/// <param name="mode">The mode to set.</param>
		/// <param name="sendMessageCount">Sets how often the SEND command should be sent.</param>
		private static void ChangeScreenPower(Window window, ScreenPowerMode mode, int sendMessageCount = 3)
		{
			int handle = HWND_BROADCAST;
			if (window != null)
			{
				var helper = new WindowInteropHelper(window);
				handle = (int)helper.Handle;
			}
			ChangeScreenPower(handle, mode, sendMessageCount);
		}

		/// <summary>
		/// Changes the power mode of the screen to the provided value.
		/// </summary>
		/// <param name="handle">The window handle of the application.</param>
		/// <param name="mode">The mode to set.</param>
		/// <param name="sendMessageCount">Sets how often the SEND command should be sent.</param>
		private static void ChangeScreenPower(int handle, ScreenPowerMode mode, int sendMessageCount = 3)
		{
			if (handle < 0)
			{
				handle = HWND_BROADCAST;
			}
			for (int i = 0; i < sendMessageCount; i++)
			{
				SendMessage(handle, WM_SYSCOMMAND, SC_MONITORPOWER, (int)mode);
			}
		}
	}
}
