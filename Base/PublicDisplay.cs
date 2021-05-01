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
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// A class for controlling the public display of Sanyo.
	/// </summary>
	public class PublicDisplayControl
	{
		private const string CmdTurnOn = "C00";
		private const string CmdTurnOff = "C01";

		/// <summary>
		/// Every command must start with this string.
		/// </summary>
		private const string CommandStart = "A";

		/// <summary>
		/// This string represents the standard monitor.
		/// </summary>
		private const string Monitor = "001";

		/// <summary>
		/// Every command must end with this string.
		/// </summary>
		private const string CommandEnd = "\r";

		/// <summary>
		/// The IP-address of the public display.
		/// </summary>
		public string Ip { get; set; }

		/// <summary>
		/// The port of the public display.
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// All the functional execution commands withouth start, monitor and end and their descriptions.
		/// </summary>
		public readonly Dictionary<string, string> FunctionalExecutionCommand2Text = new Dictionary<string, string>();

		/// <summary>
		/// All the status read commands withouth start, monitor and end and their descriptions.
		/// </summary>
		public readonly Dictionary<string, string> StatusReadCommand2Text = new Dictionary<string, string>();

		/// <summary>
		/// Creates a new instance of a public display object.
		/// </summary>
		/// <param name="ip">The IP-address of the public display.</param>
		/// <param name="port">The port of the public display.</param>
		public PublicDisplayControl(string ip, int port)
		{
			Ip = ip;
			Port = port;

			FunctionalExecutionCommand2Text.Add("C00", "Power ON");
			FunctionalExecutionCommand2Text.Add("C01", "Power OFF");
			FunctionalExecutionCommand2Text.Add("C23", "Wide 'Auto' direct");
			FunctionalExecutionCommand2Text.Add("C24", "Wide 'Natural' direct");
			FunctionalExecutionCommand2Text.Add("C29", "Wide 'Full' direct");
			FunctionalExecutionCommand2Text.Add("C0F", "Wide 'Normal' direct");
			FunctionalExecutionCommand2Text.Add("C30", "Picture (Toggle Dynamic, Standard, Eco, Personal)");
			FunctionalExecutionCommand2Text.Add("C70", "AV1 direct");
			FunctionalExecutionCommand2Text.Add("C71", "RGB direct");
			FunctionalExecutionCommand2Text.Add("C72", "AV2 RGBHV direct");
			FunctionalExecutionCommand2Text.Add("C73", "AV2 YPbPr direct");
			FunctionalExecutionCommand2Text.Add("C74", "AV3 direct");
			FunctionalExecutionCommand2Text.Add("C75", "DVI direct");
			FunctionalExecutionCommand2Text.Add("C76", "PC direct");
			FunctionalExecutionCommand2Text.Add("C64", "PC Auto adjust");
			FunctionalExecutionCommand2Text.Add("C92", "Factory settings");
			FunctionalExecutionCommand2Text.Add("CF DPMS ON", "Power Save ON");
			FunctionalExecutionCommand2Text.Add("CF DPMS OFF", "Power Save OFF");
			FunctionalExecutionCommand2Text.Add("CF CLOK ON", "Child Lock ON");
			FunctionalExecutionCommand2Text.Add("CF CLOK OFF", "Child Lock OFF");
			FunctionalExecutionCommand2Text.Add("CF DEA RMCY", "RC Inhibition OFF");
			FunctionalExecutionCommand2Text.Add("CF DEA RMCN", "RC Inhibition ON");

			StatusReadCommand2Text.Add("CR0", "Power (On, Standby, power error,...)");
			StatusReadCommand2Text.Add("CR1", "Input Mode (AV1, AV2,...,HDMI, PC)");
			StatusReadCommand2Text.Add("CR WIDE", "Wide Mode (Auto, Normal, Full,...)");
			StatusReadCommand2Text.Add("CR PICTURE", "Picture mode (Dynamic, Standard, ...)");
			StatusReadCommand2Text.Add("CR SIGNAL", "Signal existence (Signal / No signal)");
			StatusReadCommand2Text.Add("CR CHILD", "Child Lock (On / Off)");
			StatusReadCommand2Text.Add("CR DPMS", "DPMS (On / Off)");
			StatusReadCommand2Text.Add("CR TM", "Panel Operating Time");
		}

		/// <summary>
		/// Assign a logging method to this delegate for logging errors.
		/// </summary>
		public static Action<string, Exception> LogError;

		/// <summary>
		/// Turns the public screen on.
		/// </summary>
		public void TurnOn()
		{
			Send(CmdTurnOn);
		}

		/// <summary>
		/// Turns the public screen off.
		/// </summary>
		public void TurnOff()
		{
			Send(CmdTurnOff);
		}

		/// <summary>
		/// Sends a command to the public screen.
		/// </summary>
		/// <param name="command">The command to send without start, monitor and end.</param>
		/// <returns>Returns the response if the command was a request, otherwise null.</returns>
		public string Send(string command)
		{
			string response = null;
			TcpClient client = null;

			try
			{
				client = new TcpClient();							// Connect	
				client.SendTimeout = 1000;
				client.ReceiveTimeout = 1000;
				client.Connect(Ip, Port);
				var networkStream = client.GetStream();

				var enc = new ASCIIEncoding();			// Write stream
				networkStream.Write(enc.GetBytes(CommandStart + Monitor + command + CommandEnd), 0, 8);

				if (StatusReadCommand2Text.ContainsKey(command))	// Read answer
				{
					Thread.Sleep(2000);	// Wait for response
					using (var ms = client.ReadNetworkStream())
					{
						var sr = new StreamReader(ms);
						response = sr.ReadToEnd();
					}
				}
			}
			catch (Exception e)
			{
				if (LogError != null)
				{
					LogError("Error in sending command to public screen.", e);
				}
			}
			finally
			{
				if (client != null)			// Disconnect
				{
					client.Close();
				}
			}

			return response;
		}
	}
}
