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
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.UI
{
	internal abstract class AicMessageListener
	{
		/// <summary>
		/// Indicates the time when the last message has been sent or received.
		/// </summary>
		protected DateTime LastConnectivity = DateTime.Now;

		/// <summary>
		/// The maxiumum timespan in seconds to the last connectivity (default: 300).
		/// </summary>
		private const int ConnectivityTimeout = 300;

		/// <summary>
		/// Indicates whether the connectivity timeout has been exceeded.
		/// </summary>
		protected bool ConnectivityTimeoutExceeded
		{
			get { return LastConnectivity.AddSeconds(ConnectivityTimeout) < DateTime.Now; }
		}

		/// <summary>
		/// Occurs when the connection status has changed.
		/// </summary>
		public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

		/// <summary>
		/// Occurs when the listener has received a message.
		/// </summary>
		public event EventHandler<MessageReceivedEventArgs> MessageReceived;

		/// <summary>
		/// Raises the ConnectionStatusChanged event.
		/// </summary>
		/// <param name="eventArgs">A ConnectionStatusChangedEventArgs that contains the event data.</param>
		protected void OnConnectionStatusChanged(ConnectionStatusChangedEventArgs eventArgs)
		{
			if (ConnectionStatusChanged != null)
			{
				ConnectionStatusChanged(this, eventArgs);
			}
		}

		/// <summary>
		/// Raises the MessageReceived event.
		/// </summary>
		/// <param name="eventArgs">A MessageReceivedEventArgs that contains the event data.</param>
		protected void OnMessageReceived(MessageReceivedEventArgs eventArgs)
		{
			if (MessageReceived != null)
			{
				MessageReceived(this, eventArgs);
			}
		}
	}
}
