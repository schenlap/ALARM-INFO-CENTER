using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace AlarmInfoCenter.Base
{
	public interface ICoreObjectFactory
	{
		ITcpClient CreateTcpClient();
		IClientSession CreateClientSession(ILogger logger, ITcpClient tcpClient, Func<List<Alarm>> getAlarmList, Func<bool> getWasConnectionState);
		ITcpListener CreateTcpListener(IPAddress ipAddress, int port);
	}
}
