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

using Ninject;
using Ninject.Parameters;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Ninject.Modules;
using RestSharp.Extensions;

[assembly: InternalsVisibleTo("AicTest")]
namespace AlarmInfoCenter.Base
{
    /// <summary>
    /// Coordination class between the core server components.
    /// </summary>
    public class DefaultListeningManager : IListeningManager, IAicMessageListener
    {
		/// <summary>
		/// Locker object of the listening manager.
		/// </summary>
		private readonly object mLockerObject = new object();

		/// <summary>
		/// Cancellation Token Source for the WAS reconnect task.
		/// </summary>
		private CancellationTokenSource mCTSWasReconnect;

        /// <summary>
        /// Representing the current alarm-state (DI).
        /// </summary>
        private readonly IAlarmState mAlarmState;

		/// <summary>
		/// WAS listener object responsible for listening to any changes on the WAS (DI).
		/// </summary>
        private readonly IWasListener mWasListener;

		/// <summary>
		/// Responsible for listening to incoming client (requests) (DI).
		/// </summary>
        private readonly IClientListener mClientListener;

		/// <summary>
		/// The default logger instance (DI).
		/// </summary>
		private readonly ILogger mLogger;

		/// <summary>
		/// The default ping service (DI).
		/// </summary>
		private readonly IPing mPing;

		private bool mIsClientListenerRunning; 
		/// <summary>
		/// True if the manager is currently listening to incoming client requests.
		/// </summary>
		public bool IsClientListenerRunning
		{
			get { return this.mIsClientListenerRunning; }
		}

		private bool mIsWasListenerRunning;
		/// <summary>
		/// True if the manager is currently listening to the WAS.
		/// </summary>
		public bool IsWasListenerRunning
		{
			get { return this.mIsWasListenerRunning; }
		}

		private bool mIsWasReconnectRunning;
		/// <summary>
		/// True if the WAS reconnect process is currently running.
		/// </summary>
		public bool IsWasReconnectRunning
		{
			get { return this.mIsWasReconnectRunning; }
		}

        public event Action<List<Alarm>> AlarmsChanged;

        public event Action<bool> WasConnectionStateChanged;


        public DefaultListeningManager()
		{
			using (StandardKernel kernel = new StandardKernel(new DefaultNinjectBindings()))
			{
				this.mLogger = kernel.Get<ILogger>();
				this.mAlarmState = kernel.Get<IAlarmState>();
				this.mWasListener = kernel.Get<IWasListener>();
				this.mClientListener = kernel.Get<IClientListener>();
				this.mPing = kernel.Get<IPing>();
			}

            TaskScheduler.UnobservedTaskException += (object sender, UnobservedTaskExceptionEventArgs ex) =>
            {
                this.mLogger.LogError("(ListeningManager/Constructor/UnobservedTaskException)", ErrorType.ListeningManager_UnobservedTaskException, ex.Exception);
            };
        }

        public DefaultListeningManager(params INinjectModule[] modules)
        {
            using (StandardKernel kernel = new StandardKernel(modules))
            {
                this.mLogger = kernel.Get<ILogger>();
                this.mAlarmState = kernel.Get<IAlarmState>();
                this.mWasListener = kernel.Get<IWasListener>();
                this.mClientListener = kernel.Get<IClientListener>();
                this.mPing = kernel.Get<IPing>();
            }

            TaskScheduler.UnobservedTaskException += (object sender, UnobservedTaskExceptionEventArgs ex) =>
            {
                this.mLogger.LogError("(ListeningManager/Constructor/UnobservedTaskException)", ErrorType.ListeningManager_UnobservedTaskException, ex.Exception);
            };
        }
        
        public DefaultListeningManager(ILogger logger, IAlarmState alarmState, IWasListener wasListener, IClientListener clientListener, IPing ping) 
        {
			this.mLogger = logger;
			this.mAlarmState = alarmState;
			this.mWasListener = wasListener;
			this.mClientListener = clientListener;
			this.mPing = ping;

            TaskScheduler.UnobservedTaskException += (object sender, UnobservedTaskExceptionEventArgs ex) =>
            {
				this.mLogger.LogError("(ListeningManager/Constructor/UnobservedTaskException)", ErrorType.ListeningManager_UnobservedTaskException, ex.Exception);
            };
        }


        /// <summary>
        /// Starts listening to the WAS.
		/// <exception cref="InvalidOperationException">Thrown when the WAS listener is running.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the reconnect process is currently running.</exception>
		/// <exception cref="AicException">If the ping to the WAS failed.</exception>
        /// </summary>
        public void StartWasListening()
        {
			lock (this.mLockerObject)
			{
				if (this.IsWasListenerRunning)
				{
					this.mLogger.LogError("WAS listener is already running", ErrorType.ListeningManager_WasListenerAlreadyRunning);
					throw new InvalidOperationException("WAS listener is already running");
				}

				if(this.mIsWasReconnectRunning)
				{
					this.mLogger.LogError("The WAS reconnect process is currently running", ErrorType.ListeningManager_WasReconnectCurrentlyRunning);
					throw new InvalidOperationException("The WAS reconnect process is currently running");
				}

				// Check if the WAS can be pinged
				if (!Network.PingAddress(AicSettings.Global.WasIp))
				{
					this.mLogger.LogWarning("WAS can't be pinged", WarningType.ListeningManager_CantPingWas);
					throw new AicException("WAS can't be pinged", AicExceptionType.ListeningManager_CantPingWas);
				}

				try
				{
					// isShutdown: True if the broken connection is forced (due to shutdown)
					this.mWasListener.WasConnectionStateChanged += delegate(bool connected, bool isShutdown)
					{
						this.mLogger.LogInformation(string.Format("WAS connection-state changed (Connected: {0} - Shutdown: {1})", connected, isShutdown), InformationType.ListeningManager_WasConnectionStateChanged);

						lock (this.mLockerObject)
						{
                            this.mIsWasListenerRunning = connected;

                            if (this.WasConnectionStateChanged != null)
                            {
                                this.WasConnectionStateChanged(connected);
                            }

							if (MessageReceived != null)
							{
								var aicMessage = new AicMessage { Alarms = mAlarmState.Alarms, ConnectionWasToServerOk = connected };
								var args = new MessageReceivedEventArgs(aicMessage);
								MessageReceived(null, args);
							}

							if (!connected && !isShutdown)
							{
								// Broken connection (not forced)
								this.mLogger.LogWarning("WAS connection got broken", WarningType.ListeningManager_BrokenWasConnection);
								this.TryWASReconnect();
							}
						}

						if (this.mClientListener != null)
						{
							// Push new state of the backend to the clients
							this.mClientListener.PushNewState();
						}
					};

					this.mWasListener.WasObjectChanged += delegate(WasObject obj)
					{
						this.mLogger.LogInformation("New WAS-Data available", InformationType.ListeningManager_NewWasDataAvailable);

						this.mAlarmState.UpdateAlarmObject(obj);

                        if(this.AlarmsChanged != null)
                        {
                            this.AlarmsChanged(this.mAlarmState.Alarms);
                        }

						if (MessageReceived != null)
						{
							var aicMessage = new AicMessage {Alarms = mAlarmState.Alarms, ConnectionWasToServerOk = true};
							var args = new MessageReceivedEventArgs(aicMessage);
							MessageReceived(null, args);
						}

						if (AicSettings.Global.UploadEnabled)
						{
							this.mAlarmState.Alarms.ForEach(alarm =>
								AlarmSend.SendAsync(AicSettings.Global.UploadUrl, alarm, (id, ex) => { this.mLogger.LogError("Error while uploading alarm (ID:" + id + ")!", ex); }));
						}

						if (this.mClientListener != null)
						{
							this.mClientListener.PushNewState();
						}
					};

					this.mWasListener.StartListening();

					this.mIsWasListenerRunning = true;
				}

				catch (Exception ex)
				{
					this.mLogger.LogError("(ListeningManager/StartWasListening/Exception)", ErrorType.Undefined, ex);
					throw ex;
				}
			}
        }

		/// <summary>
		/// Stops listening to the WAS.
		/// <exception cref="InvalidOperationException">Thrown when the WAS listener isn't running.</exception>
		/// </summary>
		public void StopWasListening()
		{
			lock (this.mLockerObject)
			{
				if (!this.IsWasListenerRunning)
				{
					this.mLogger.LogError("WAS listener isn't running", ErrorType.ListeningManager_WasListenerNotRunning);
					throw new InvalidOperationException("WAS listener isn't running");
				}

				this.mWasListener.StopListening();

				this.mLogger.LogInformation("Listening to the WAS stopped", InformationType.ListeningManager_WasListeningStopped);

				if (this.mIsWasReconnectRunning)
				{
					this.mCTSWasReconnect.Cancel();
				}

				this.mIsWasListenerRunning = false;
			}
		}


		/// <summary>
		/// Starts listening to incoming client requests (and handles then the session).
		/// <exception cref="InvalidOperationException">Thrown when the client listener is running.</exception>
		/// </summary>
		public void StartClientListening()
		{
            if (this.mClientListener == null)
            {
                throw new NullReferenceException("Can't find a suitable Client Listener definition!");
            }

			lock (this.mLockerObject)
			{
				if (this.IsClientListenerRunning)
				{
					this.mLogger.LogError("Client listener is already running", ErrorType.ListeningManager_ClientListenerAlreadyRunning);
					throw new InvalidOperationException("Client listener is already running");
				}

				this.mClientListener.StartClientListening();

				this.mIsClientListenerRunning = true;
			}
		}

		/// <summary>
		/// Stops the client listener.
		/// <exception cref="InvalidOperationException">Thrown when the client listener isn't running.</exception>
		/// </summary>
		public void StopClientListening()
		{
            if (this.mClientListener == null)
            {
                throw new NullReferenceException("Can't find a suitable Client Listener definition!");
            }

			lock (this.mLockerObject)
			{
				if (!this.IsClientListenerRunning)
				{
					this.mLogger.LogError("Client listener isn't running", ErrorType.ListeningManager_ClientListenerNotRunning);
					throw new InvalidOperationException("Client listener isn't running");
				}

				this.mClientListener.StopClientListening();

				this.mIsClientListenerRunning = false;
			}
		}

        

		/// <summary>
		/// Starts a background thread to continously ping the WAS (check if it's available again).
		/// There can be only one thread checking the connectivity at the same time.
		/// </summary>
        private void TryWASReconnect()
        {
			lock (this.mLockerObject)
			{
				if (this.mIsWasReconnectRunning)
				{
					this.mLogger.LogWarning("WAS reconnect is already running", WarningType.ListeningManager_WasReconnectAlreadyRunning);
					return;
				}

				this.mIsWasReconnectRunning = true;

				this.mLogger.LogInformation(string.Format("Try reconnect to WAS (Default WAS IP: {0})", AicSettings.Global.WasIp.ToString()), InformationType.ListeningManager_TryWasReconnect);

				this.mCTSWasReconnect = new CancellationTokenSource();

				Task.Factory.StartNew(state =>
					{
						CancellationToken token = (CancellationToken)state;

						while (!token.IsCancellationRequested)
						{
							Thread.Sleep((int)Constants.WasReconnectTimeout.TotalMilliseconds);

							if (this.mPing.PingAddress(AicSettings.Global.WasIp))
							{
								return true;
							}
						}

						return false;
					}, this.mCTSWasReconnect.Token).ContinueWith(task =>
					{
						lock (this.mLockerObject)
						{
							this.mIsWasReconnectRunning = false;
						}

						if (task.Result)
						{
							this.mLogger.LogInformation("WAS is connected again (ping succeded)", InformationType.ListeningManager_WasReconnectedAgain);
							this.mWasListener.StartListening();
						}
					});
			}
        }

		#region IAicMessageListener Members

		public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

		public event EventHandler<MessageReceivedEventArgs> MessageReceived;

		public void Start()
		{
			StartWasListening();
		}

		public void Stop()
		{
			StopWasListening();
		}

		public void Disconnect()
		{
			// use Stop()
		}

		public bool CheckConnection()
		{
			return true;
		}

		public void SendRequestAsync()
		{
			// do nothing
		}

		#endregion
	}
}
