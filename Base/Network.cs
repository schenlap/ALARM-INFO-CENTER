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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class contains methods that are concerned to networking.
	/// </summary>
	public static class Network
	{
		/// <summary>
		/// Gets the first local IPv4 address.
		/// </summary>
		/// <returns></returns>
		public static IPAddress GetLocalIp()
		{
			// Get local IP
			string hostName = Dns.GetHostName();
			var hostEntry = Dns.GetHostEntry(hostName);
			return hostEntry.AddressList.FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);
		}

		/// <summary>
		/// Copies any stream to a MemoryStream and sets the position to 0 and does not close the stream.
		/// </summary>
		/// <param name="stream">The stream to copy. The position should be 0.</param>
		/// <param name="bufferSize">The size of the buffer to be used</param>
		/// <returns>A MemoryStream with the copied data. Position is 0.</returns>
		public static MemoryStream CopyToMemoryStream(Stream stream, int bufferSize = 8192)
		{
			Debug.Assert(!stream.CanSeek || stream.Position == 0);

			var ms = new MemoryStream();
			var buffer = new byte[bufferSize];

			int bytesRead;
			while ((bytesRead = stream.Read(buffer, 0, bufferSize)) > 0)
			{
				ms.Write(buffer, 0, bytesRead);
			}
			ms.Position = 0;
			return ms;
		}

		/// <summary>
		/// Downloads data from an URL using a timeout and saves it as a stream.
		/// </summary>
		/// <param name="url">The URL to get data from.</param>
		/// <param name="timeout">The timeout in milliseconds.</param>
		/// <returns>A MemoryStream with data. Never null.</returns>
		public static MemoryStream DownloadStreamData(string url, int timeout = 3000)
		{
			MemoryStream ms;

			// Create a webrequest in order to use the timeout
			var request = WebRequest.Create(url);
			request.Timeout = timeout;
			request.UseDefaultCredentials = true;
			
			using (var response = request.GetResponse())
			{
				var stream = response.GetResponseStream();
				if (stream == null)
				{
					throw new WebException("ResponseStream is null.");
				}
				ms = CopyToMemoryStream(stream);
			}

			return ms;
		}

		/// <summary>
		/// Downloads data from an URL using a number of timeouts and saves it as a stream.
		/// </summary>
		/// <param name="url">The URL to get data from.</param>
		/// <param name="timeouts">The timeouts in milliseconds.</param>
		/// <returns>A MemoryStream with data. Never null.</returns>
		/// <exception>Throws any exception if something goes wrong.</exception>
		public static MemoryStream DownloadStreamData(string url, int[] timeouts)
		{
			MemoryStream ms = null;
			Exception exception = null;

			foreach (int timeout in timeouts)
			{
				try
				{
					ms = DownloadStreamData(url, timeout);
					exception = null;
					break;
				}
				catch (UriFormatException)
				{
					throw;
				}
				catch (Exception exc)
				{
					exc.Data.Add("Timeout", timeout);
					exception = exc;
				}
			}

			if (exception != null)
			{
				throw exception;
			}

			return ms;
		}

		// Creating the extern function
		[DllImport("wininet.dll")]
		private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);

		/// <summary>
		/// Checks whether there is an active internet connection.
		/// </summary>
		/// <returns>True if the computer is connected to the internet.</returns>
		public static bool IsConnectedToInternet()
		{
			int desc;
			return InternetGetConnectedState(out desc, 0);
		}

		/// <summary>
		/// Pings given ip-address.
		/// </summary>
		/// <param name="ip">The address to ping.</param>
		/// <returns>Returns true if the PingReply-Status is 'Success', otherwise false.</returns>
		public static bool PingAddress(IPAddress ip)
		{
			using (var ping = new Ping())
			{
				var reply = ping.Send(ip);
				return reply != null && reply.Status == IPStatus.Success;
			}
		}

		/// <summary>
		/// Pings given ip-address.
		/// </summary>
		/// <param name="hostNameOrAddress">The host name or ip address to ping.</param>
		/// <returns>Returns true if the PingReply-Status is 'Success', otherwise false.</returns>
		public static bool PingAddress(string hostNameOrAddress)
		{
			using (var ping = new Ping())
			{
				var reply = ping.Send(hostNameOrAddress);
				return reply != null && reply.Status == IPStatus.Success;
			}
		}
	}
}
