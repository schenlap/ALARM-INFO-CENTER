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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AlarmInfoCenter.Base
{
	public sealed class DefaultTcpListener : ITcpListener
	{
		private readonly TcpListener mWrapperObject;

		private readonly IPAddress mIpAddress;

		private readonly int mPort;


		public IPAddress IpAddress
		{
			get { return this.mIpAddress; }
		}

		public int Port
		{
			get { return this.mPort; }
		}

		public DefaultTcpListener(IPAddress ipAddress, int port)
		{
			this.mIpAddress = ipAddress;
			this.mPort = port;
			this.mWrapperObject = new TcpListener(ipAddress, port);
		}



		public void Start()
		{
			this.mWrapperObject.Start();
		}

		public void Stop()
		{
			this.mWrapperObject.Stop();
		}

		public IAsyncResult BeginAcceptTcpClient(AsyncCallback callback, object state)
		{
			return this.mWrapperObject.BeginAcceptTcpClient(callback, state);
		}

		public ITcpClient EndAcceptTcpClient(IAsyncResult result)
		{
			return new DefaultTcpClient(this.mWrapperObject.EndAcceptTcpClient(result));
		}
	}
}
