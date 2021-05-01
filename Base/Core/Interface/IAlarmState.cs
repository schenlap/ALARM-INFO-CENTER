using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlarmInfoCenter.Base
{
	public interface IAlarmState
	{
		List<Alarm> Alarms { get; }

		void UpdateAlarmObject(WasObject wasObject);
	}
}
