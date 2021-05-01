using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlarmInfoCenter.Base
{
	public interface IClientListener
	{
		bool IsRunning { get; }

		void StartClientListening();
		void StopClientListening();
		void PushNewState();
	}
}
