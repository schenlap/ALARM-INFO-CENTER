using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlarmInfoCenter.Base
{
	public interface IListeningManager
	{
		bool IsClientListenerRunning { get; }
		bool IsWasListenerRunning { get; }
		bool IsWasReconnectRunning { get; }

		void StartWasListening();
		void StopWasListening();
		void StartClientListening();
		void StopClientListening();
	}
}
