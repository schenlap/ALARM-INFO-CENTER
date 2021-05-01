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

namespace AlarmInfoCenter.Base
{
	public interface IAicMessageListener
	{
		/// <summary>
		/// Occurs when the connection status has changed.
		/// </summary>
		event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

		/// <summary>
		/// Occurs when the listener has received a message.
		/// </summary>
		event EventHandler<MessageReceivedEventArgs> MessageReceived;

		/// <summary>
		/// Starts listening in background.
		/// </summary>
		void Start();

		/// <summary>
		/// Stops listening.
		/// </summary>
		void Stop();

		/// <summary>
		/// Disconnects from the client.
		/// </summary>
		void Disconnect();

		/// <summary>
		/// Checks whether connection is ok.
		/// </summary>
		/// <returns>True if the connection is ok.</returns>
		bool CheckConnection();

		/// <summary>
		/// Sends a request asynchronuously.
		/// </summary>
		void SendRequestAsync();
	}
}
