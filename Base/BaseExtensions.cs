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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Controls.Primitives;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class contains extension methods for basic .NET types.
	/// </summary>
	public static class BaseExtensions
	{
		/// <summary>
		/// Returns all items with a specific key from the dictonary, in case there is any item of the keys which contains this specific key.
		/// </summary>
		/// <typeparam name="TKey">Any object.</typeparam>
		/// <typeparam name="TValue">Any object.</typeparam>
		/// <param name="dictionary">The current dictonary.</param>
		/// <param name="keys">The key list.</param>
		/// <returns>A dictionary containing all the elements with the specified keys.</returns>
		public static Dictionary<TKey, TValue> GetItemsByKeyList<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys)
		{
			return dictionary.Keys.Where(keys.Contains).ToDictionary(key => key, key => dictionary[key]);
		}

		/// <summary>
		/// Checks whether the provided string can be parsed to a DateTime object.
		/// </summary>
		/// <param name="dateTimeString">A string containing date and time.</param>
		/// <returns>True if the string can be parsed to a DateTime object.</returns>
		public static bool IsDateTime(this string dateTimeString)
		{
			DateTime result;
			return DateTime.TryParse(dateTimeString, out result);
		}

		/// <summary>
		/// Checks whether the provided string can be parsed to an Int32 object.
		/// </summary>
		/// <param name="numberString">A string containing a number.</param>
		/// <returns>True if the string can be parsed to a Int32 object.</returns>
		public static bool IsInt32(this string numberString)
		{
			int result;
			return int.TryParse(numberString, out result);
		}

		/// <summary>
		/// Tries to parse the provided DateTime string. Returns DateTime.MinValue if the value is null, empty or parsing fails.
		/// </summary>
		/// <param name="dateTime">A string value that represents a date and time value.</param>
		/// <returns>The parsed date and time or DateTime.MinValue if parsing fails.</returns>
		public static DateTime TryParseDateTime(this string dateTime)
		{
			DateTime result;
			DateTime.TryParse(dateTime, out result);
			return result;
		}

		/// <summary>
		/// Creates a connection to an IP address or host.
		/// </summary>
		/// <param name="tcpClient">A tcp client.</param>
		/// <param name="ipAddressOrHostName">A string containing either an IP address or a host name.</param>
		/// <param name="port">The port to connect.</param>
		public static void ConnectToIpOrHost(this TcpClient tcpClient, string ipAddressOrHostName, int port)
		{
			IPAddress ip;
			bool isIp = IPAddress.TryParse(ipAddressOrHostName, out ip);
			if (isIp)
			{
				tcpClient.Connect(ip, port);
			}
			else
			{
				tcpClient.Connect(ipAddressOrHostName, port);
			}
		}

		/// <summary>
		/// Completely reads data from the network stream into a memory stream. The memory stream must be closed when it is not needed anymore.
		/// </summary>
		/// <param name="tcpClient">A TcpClient.</param>
		/// <param name="delimiterRegex">Optional: A regular expression that defines the final string of the network stream.</param>
		/// <param name="streamText">The stream text that has already been read when recursion occurs.</param>
		/// <returns>A memory stream which is never null.</returns>
		/// <exception cref="TimeoutException">Thrown if reading exceeds the timeout.</exception>
		public static MemoryStream ReadNetworkStream(this TcpClient tcpClient, string delimiterRegex = null, string streamText = null)
		{
			MemoryStream ms = new MemoryStream();
			NetworkStream ns = tcpClient.GetStream();
			byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
			IAsyncResult asyncReader = ns.BeginRead(buffer, 0, buffer.Length, null, null);

			// Give the reader some seconds to respond with a value
			bool completed = asyncReader.AsyncWaitHandle.WaitOne(Constants.NetworkTimeout);
			if (completed)
			{
				// Read network stream and copy it to a memory stream
				bool continueReading;
				int bytesRead = ns.EndRead(asyncReader);
				ms.Write(buffer, 0, bytesRead);

				if (delimiterRegex != null)
				{
					if (streamText == null)
					{
						streamText = string.Empty;
					}

					// Check if the final string has been read
					ms.Position = 0;
					StreamReader sr = new StreamReader(ms);
					streamText += sr.ReadToEnd();
					continueReading = !System.Text.RegularExpressions.Regex.IsMatch(streamText, delimiterRegex);
				}
				else
				{
					// Check whether the buffer is full
					continueReading = bytesRead == buffer.Length;
				}

				// Read the next part of the network stream
				if (continueReading)
				{
					MemoryStream appendMs = tcpClient.ReadNetworkStream(delimiterRegex, streamText);
					appendMs.WriteTo(ms);
				}
			}
			else
			{
				throw new TimeoutException("The device failed to read in an appropriate amount of time.");
			}
			return ms;
		}

		/// <summary>
		/// Checks if a TCP-Client is (currently) connected to an end-point (listener).
		/// </summary>
		/// <param name="tcpClient">The client to check the connectivity.</param>
		/// <returns>True if the client is (currently) connected, otherwise false.</returns>
		public static bool IsConnected(this TcpClient tcpClient)
		{
			if (tcpClient == null || tcpClient.Client == null)
			{
				return false;
			}

			try
			{
				if (tcpClient.Client.Poll(0, SelectMode.SelectRead))
				{
					byte[] buff = new byte[1];
					if (tcpClient.Client.Receive(buff, SocketFlags.Peek) == 0)
					{
						return false;		// Client disconnected
					}
				}
				return true;
			}
			catch (SocketException ex)
			{
				if (ex.ErrorCode == 10054)		// Client gone
				{
					return false;
				}
				throw;
			}
			catch (ObjectDisposedException)		// Occurs when the network stream has been closed.
			{
				return false;
			}
		}

		/// <summary>
		/// Indicates whether the control is checked or not. If it is indeterminate the method returns false.
		/// </summary>
		/// <param name="control">The control that can be checked.</param>
		/// <returns>True if the control is checked otherwise false.</returns>
		public static bool GetIsChecked(this ToggleButton control)
		{
			return control.IsChecked.HasValue && control.IsChecked.Value;
		}

		public static void FocusAndSelectAll(this TextBoxBase control)
		{
			control.Focus();
			control.SelectAll();
		}
	}
}
