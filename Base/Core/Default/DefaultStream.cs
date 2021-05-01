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
using System.Net.Sockets;
using System.Text;

namespace AlarmInfoCenter.Base
{
	public sealed class DefaultStream : IStream
	{
		private readonly Func<NetworkStream> mGetNetworkStream;

		public DefaultStream(Func<NetworkStream> getNetworkStream)
		{
			this.mGetNetworkStream = getNetworkStream;
		}

		public bool DataAvailable
		{
			get { return this.mGetNetworkStream().DataAvailable; }
		}

		public void Close()
		{
			this.mGetNetworkStream().Close();
		}

		public Stream GetUnderlyingStream()
		{
			return this.mGetNetworkStream();
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			this.mGetNetworkStream().Write(buffer, offset, count);
		}
	}
}
