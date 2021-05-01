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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Reflection;

namespace AlarmInfoCenter.Base
{
    /// <summary>
    /// Used for listening and communicating with new / existing clients.
    /// </summary>
    public class DefaultClientListener : IClientListener
    {
		/// <summary>
		/// TcpListener which listens to incoming request from AIC-Clients.
		/// </summary>
		private readonly ITcpListener mTcpListener;

		/// <summary>
		/// Factory for several core AIC objects.
		/// </summary>
		private readonly ICoreObjectFactory mCoreObjectFactory;

        /// <summary>
        /// Function providing the current list of alarms.
        /// </summary>
        private readonly Func<List<Alarm>> mGetAlarmList;

        /// <summary>
        /// Function providing the current connection-state to / of the WAS (connected / disconnected).
        /// </summary>
        private readonly Func<bool> mGetWasConnectionState;

        /// <summary>
        /// List of all sessions with clients.
        /// </summary>
		private readonly List<IClientSession> mClientSessions = new List<IClientSession>();

        /// <summary>
        /// Locker object regarding to the mClientSessions list.
        /// </summary>
        private readonly object mClientSessionLocker = new object();

		/// <summary>
		/// Logger instance from IoC-Container
		/// </summary>
		private readonly ILogger mLogger;

		private bool mIsRunning;
        /// <summary>
        /// Indicates whether the current client listener is running / active.
        /// </summary>
		public bool IsRunning
		{
			get { return this.mIsRunning; }
		}


        public DefaultClientListener(
			ILogger logger,
            IAlarmState alarmState,
            IWasListener wasListener,
			ICoreObjectFactory coreObjectFactory)
        {
			this.mLogger = logger;

			// Function that returns all active alarms
			Func<List<Alarm>> getAlarmList =
				() =>
				{
					return alarmState.Alarms;
				};

			// Function that returns the current WAS-connection state
			Func<bool> getWasState =
				() =>
				{
					return wasListener.HasConnectionEstablished;
				};

            this.mGetAlarmList = getAlarmList;
            this.mGetWasConnectionState = getWasState;
			this.mCoreObjectFactory = coreObjectFactory;

			this.mTcpListener = this.mCoreObjectFactory.CreateTcpListener(AicSettings.Global.NetworkServiceIp, AicSettings.Global.NetworkServicePort);
        }

        /// <summary>
        /// Starts listening to incoming connection requests from clients.
        /// Creates a new ClientSession in case of an incoming connection request from a client.
		/// <exception cref="InvalidOperationException">Thrown when the listener is already running.</exception>
        /// </summary>
        public void StartClientListening()
        {
			lock (this.mClientSessionLocker)
			{
				if (this.IsRunning)
				{
					this.mLogger.LogError("Client listener is already running", ErrorType.ClientListener_ListenerAlreadyRunning);
					throw new InvalidOperationException("Client listener is already running");
				}

				this.mTcpListener.Start();

				// Listen to the clients
				this.mTcpListener.BeginAcceptTcpClient(this.HandleNewClient, this.mTcpListener);

				this.mLogger.LogInformation("Listening to clients started", InformationType.ClientListener_StartedListening);	

				this.mIsRunning = true;
			}
        }

		/// <summary>
		/// Handles the new connected client.
		/// </summary>
		/// <param name="result">The result of this asynchronous operation.</param>
		private void HandleNewClient(IAsyncResult result)
		{
			lock (this.mClientSessionLocker)
			{
				// Begin to listen again
				this.mTcpListener.BeginAcceptTcpClient(this.HandleNewClient, this.mTcpListener);

				ITcpClient client = this.mTcpListener.EndAcceptTcpClient(result);

				IClientSession clientSession = this.mCoreObjectFactory.CreateClientSession(this.mLogger, client, this.mGetAlarmList, this.mGetWasConnectionState);

				clientSession.SessionDestroyedEvent += delegate
				{
					lock (this.mClientSessionLocker)
					{
						this.mClientSessions.Remove(clientSession);
						this.mLogger.LogInformation(string.Format("Client-Session (IP: {0}) from list of all sessions removed", clientSession.ClientAddress), InformationType.ClientListener_ClientSessionRemoved);
					}
				};
				this.mClientSessions.Add(clientSession);
				clientSession.StartSession();
			}
		}

        /// <summary>
        /// Stops listening to incoming connection requests from clients.
        /// Destroys all active client sessions and stops the listening thread.
		/// <exception cref="InvalidOperationException">Thrown when the listener isn't running.</exception>
        /// </summary>
        public void StopClientListening()
        {
			lock (this.mClientSessionLocker)
			{
				if (!this.IsRunning)
				{
					this.mLogger.LogError("Client listener isn't running", ErrorType.ListeningManager_ClientListenerNotRunning);
					throw new InvalidOperationException("Client listener isn't running");
				}

				this.mTcpListener.Stop();

				foreach (IClientSession session in this.mClientSessions)
				{
					session.DestroySession(false);
				}

				this.mClientSessions.Clear();

				this.mIsRunning = false;

				this.mLogger.LogInformation("All client sessions destroyed", InformationType.ClientListener_AllClientSessionsDestroyed);
			}
        }

        /// <summary>
        /// Pushes a new state to all client sessions. Use this method if new data from 
		/// the WAS is available or the WAS connection-state has changed.
        /// </summary>
        public void PushNewState()
        {
			lock (this.mClientSessionLocker)
			{
				if (this.IsRunning)
				{
					foreach (IClientSession session in this.mClientSessions)
					{
						session.PushNewState();
						this.mLogger.LogInformation(string.Format("New State to Client (IP: {0}) pushed", session.ClientAddress), InformationType.ClientListener_NewStatePushed);
					}
				}

				else
				{
					this.mLogger.LogWarning("Can't push any message because the client listener is not running", WarningType.ClientListener_CantPushNewState);
				}
			}
        }
    }
}
