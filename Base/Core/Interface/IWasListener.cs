using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlarmInfoCenter.Base
{
	public interface IWasListener
	{
		bool HasConnectionEstablished { get; }
		bool IsRunning { get; }
		event Action<bool, bool> WasConnectionStateChanged;
		event Action<WasObject> WasObjectChanged;

		void StartListening();
		void StopListening();
	}
}
