using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlarmInfoCenter.Base
{
	public class Log
	{
		private static readonly ILogger mLogger = new EventLogger(Constants.EventLogName, Constants.EventLogSourceName);

		public static ILogger GetInstance()
		{
			return mLogger;
			//return new EventLogger(Constants.EventLogName, Constants.EventLogSourceName);
		}
	}
}
