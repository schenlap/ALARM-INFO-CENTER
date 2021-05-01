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
using System.IO;
using System.Windows.Threading;
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.UI
{
	internal class FileListener : AicMessageListener, IAicMessageListener
	{
		private DispatcherTimer mTimer;

		public void Start()
		{
			Stop();
			mTimer = new DispatcherTimer();
			mTimer.Interval = new TimeSpan(0, 0, 5);
			mTimer.Tick += mTimer_Tick;
			mTimer.Start();
		}

		private void mTimer_Tick(object sender, EventArgs e)
		{
			AicMessage aicMessage = null;
			try
			{
				aicMessage = AicMessage.DeserializeFromFile(Constants.AicMessageDemoPath);
			}
			catch (Exception)
			{
				OnConnectionStatusChanged(new ConnectionStatusChangedEventArgs { ConnectedToServer = false });
			}

			if (aicMessage != null)
			{
				OnMessageReceived(new MessageReceivedEventArgs(aicMessage));
			}
		}

		public void Stop()
		{
			if (mTimer != null)
			{
				mTimer.Stop();
			}
		}

		public bool CheckConnection()
		{
			return File.Exists(Constants.AicMessageDemoPath);
		}

		public void Disconnect() { }

		public void SendRequestAsync() { }
	}
}
