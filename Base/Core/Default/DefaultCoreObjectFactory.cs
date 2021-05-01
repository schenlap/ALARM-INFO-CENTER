using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlarmInfoCenter.Base
{
	public class DefaultCoreObjectFactory : ICoreObjectFactory
	{
		public ITcpClient CreateTcpClient()
		{
			return new DefaultTcpClient();
		}

		public IClientSession CreateClientSession(ILogger logger, ITcpClient tcpClient, Func<List<Alarm>> getAlarmList, Func<bool> getWasConnectionState)
		{
			return new DefaultClientSession(logger, tcpClient, getAlarmList, getWasConnectionState);
		}

		public ITcpListener CreateTcpListener(System.Net.IPAddress ipAddress, int port)
		{
			return new DefaultTcpListener(ipAddress, port);
		}
	}
}
