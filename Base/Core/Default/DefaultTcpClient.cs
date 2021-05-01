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
using System.Text;

namespace AlarmInfoCenter.Base
{
	public sealed class DefaultTcpClient : ITcpClient
	{
		private readonly TcpClient mWrapperTcpClient;

		private readonly DefaultStream mWrapperStream;

		public DefaultTcpClient(TcpClient client)
		{
			this.mWrapperTcpClient = client;
			this.mWrapperStream = new DefaultStream(() => { return this.mWrapperTcpClient.GetStream(); });
		}

		public DefaultTcpClient()
		{
			this.mWrapperTcpClient = new TcpClient();
			this.mWrapperStream = new DefaultStream(() => { return this.mWrapperTcpClient.GetStream(); } );
		}


		public bool Connected
		{
			get { return this.mWrapperTcpClient.Connected; }
		}

		public IPAddress RemoteAddress
		{
			get { return ((IPEndPoint)this.mWrapperTcpClient.Client.RemoteEndPoint).Address; }
		}

		public IStream GetStream()
		{
			return this.mWrapperStream;
		}

		public bool IsConnected()
		{
			return this.mWrapperTcpClient.IsConnected();
		}

		public MemoryStream ReadNetworkStream(string delimiterRegex = null, string streamText = null)
		{
			return this.mWrapperTcpClient.ReadNetworkStream(delimiterRegex, streamText);
		}

		public void Close()
		{
			this.mWrapperTcpClient.Close();
		}

		public void Connect(IPAddress address, int port)
		{
			this.mWrapperTcpClient.Connect(address, port);
		}
	}
}
