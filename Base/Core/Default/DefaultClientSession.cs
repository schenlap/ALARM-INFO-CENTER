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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AlarmInfoCenter.Base
{
    /// <summary>
    /// A concrete session handling the connection with one client.
    /// To start a session there must be StartSession explicitly called.
    /// You must not call the StartSession twice or after the session got destroyed.
    /// In case the connection to the client got broken, the session
    /// will destroy itself.
    /// </summary>
    public class DefaultClientSession : IClientSession
    {
        /// <summary>
        /// Default locker object for the class.
        /// </summary>
        private readonly object mLocker = new object();

        /// <summary>
        /// The AbstractTCP-Client for this session / connection.
        /// </summary>
        private readonly ITcpClient mTcpClient;

        /// <summary>
        /// The task for this session handling the requests / responses with / from the client.
        /// The task will start in case the StartSession method gets called.
        /// </summary>
        private readonly Task mSessionTask;

		/// <summary>
		/// The cancellation token source of the working task.
		/// </summary>
		private readonly CancellationTokenSource mSessionTaskCancellationTokenSource;

		/// <summary>
		/// Logger instance from IoC-Container
		/// </summary>
		private readonly ILogger mLogger;

        /// <summary>
        /// Indicates if a push has been sent to the client and the session is now waiting for the response.
        /// </summary>
        private bool mPushAckPending;

        /// <summary>
        /// Total number of push retries. 
        /// A retry will be performed if the client isn't responding in an appropriate amount of time.
        /// </summary>
        private int mPushRetryCount;

        /// <summary>
        /// Total number of milliseconds between the last (retry) push and 'now'.
        /// </summary>
        private int mAckCycleTimeElapsed;

        /// <summary>
        /// Function which returns the current list of alarms.
        /// </summary>
        private readonly Func<List<Alarm>> mGetAlarmList;

        /// <summary>
        /// Function which returns the current state of the connection with the WAS (connected or disconnected).
        /// </summary>
        private readonly Func<bool> mGetWasConnectionState;

        /// <summary>
        /// Time between each KeepAlive-Push.
        /// </summary>
        private readonly TimeSpan mClientKeepAliveTimeout;



		/// <summary>
		/// Event will be raised in case the session got destroyed.
		/// You must not start the session again afterwards, instead create a new session-object.
		/// </summary>
		public event Action SessionDestroyedEvent;

		private bool mSessionStarted;
		/// <summary>
		/// Indicates whether the StartSession method has been called yet.
		/// </summary>
		public bool SessionStarted
		{
			get { return this.mSessionStarted; }
		}

		private bool mSessionDestroyed;
		/// <summary>
		/// Indicates whether the DestroySession method has been called yet.
		/// </summary>
		public bool SessionDestroyed
		{
			get { return this.mSessionDestroyed; }
		}

		private readonly IPAddress mClientAddress;
		/// <summary>
		/// The client's IP-Address.
		/// </summary>
		public IPAddress ClientAddress
		{
			get { return mClientAddress; }
		}



        public DefaultClientSession(
			ILogger logger,
            ITcpClient tcpClient, 
            Func<List<Alarm>> getAlarmList,
            Func<bool> getWasConnectionState)
        {
			this.mLogger = logger;
            this.mTcpClient = tcpClient;
            this.mGetAlarmList = getAlarmList;
            this.mGetWasConnectionState = getWasConnectionState;
            this.mClientKeepAliveTimeout = Constants.ClientKeepAliveTimeout;
			this.mClientAddress = tcpClient.RemoteAddress;
			
			this.mSessionTaskCancellationTokenSource = new CancellationTokenSource();
			this.mSessionTask = new Task(new Action<object>(this.SessionHandling), this.mSessionTaskCancellationTokenSource.Token);
        }

        /// <summary>
        /// Method for handling the connection (requests / responses) with the client. You must not call this method twice or after the session got destroyed.
        /// In case the connection to the client got broken, the session will destroy itself.
        /// </summary>
        private void SessionHandling(object state)
        {
			CancellationToken cancellationToken = (CancellationToken)state;

            System.Timers.Timer timer = null;

            this.PushNewState();

			try
			{
				timer = new System.Timers.Timer(this.mClientKeepAliveTimeout.TotalMilliseconds);
				timer.Elapsed += delegate { SendKeepAliveMessage(); };
				timer.Start();

				while (!cancellationToken.IsCancellationRequested)
				{
					// Wait while there's no request from the client and check if the connection got refused / broken
					while (!this.mTcpClient.GetStream().DataAvailable)
					{
						cancellationToken.ThrowIfCancellationRequested();

						Thread.Sleep(Constants.ClientCycleTimeout);

						// Check connection
						if (!this.mTcpClient.IsConnected())
						{
							throw new SocketException((int)SocketError.ConnectionReset);
						}

						// If a response after a push is pending
						if (this.mPushAckPending)
						{
							this.mAckCycleTimeElapsed += Constants.ClientCycleTimeout;

							// Appropriate amount of time is over - now send a request again
							if (this.mAckCycleTimeElapsed >= Constants.NetworkTimeout)
							{
								this.PushAicMessage(this.mGetAlarmList(), this.mGetWasConnectionState(), MessageType.Request);
								this.mAckCycleTimeElapsed = 0;
								this.mPushRetryCount++;

								this.mLogger.LogWarning(
									string.Format("No response from client yet (IP: {0}). Retry {1} from {2}", this.ClientAddress, mPushRetryCount, Constants.MaxPushRetry), 
									WarningType.ClientSession_NoClientResponse);

								// No response after several retries
								if (this.mPushRetryCount == Constants.MaxPushRetry)
								{
									this.mLogger.LogError(string.Format("No response-message after {0} retries", Constants.MaxPushRetry), ErrorType.ClientSession_NoClientResponse);
									throw new AicException(string.Format("No response-message after {0} retries", Constants.MaxPushRetry), AicExceptionType.ClientSession_NoClientResponse);
								}
							}
						}
					}

					AicMessage message = null;

					try
					{
						using (MemoryStream memoryStream = this.mTcpClient.ReadNetworkStream(AicMessage.XmlEndTagRegex))
						{
							memoryStream.Position = 0;
							message = AicMessage.Deserialize(memoryStream);
						}
					}

					catch (Exception ex)
					{
						this.mLogger.LogError("(ClientSession/SessionHandling/Exception)", ErrorType.Undefined, ex);
					}

					if (message == null)
					{
						continue;
					}

					else if (message.MessageType == MessageType.Request)
					{
						this.mLogger.LogInformation(string.Format("Request from client (IP: {0})", this.ClientAddress), InformationType.ClientSession_ReceivedRequest);

						PushAicMessage(this.mGetAlarmList(), this.mGetWasConnectionState(), MessageType.Response);
					}

					else if (message.MessageType == MessageType.Response)
					{
						this.mLogger.LogInformation(string.Format("Response from client (IP: {0})", this.ClientAddress), InformationType.ClientSession_ReceivedResponse);

						lock (this.mLocker)
						{
							this.mPushAckPending = false;
							this.mPushRetryCount = 0;

							Monitor.Pulse(this.mLocker);
						}
					}

					else if (message.MessageType == MessageType.KeepAlive)
					{
						this.mLogger.LogInformation(string.Format("Keep-Alive from client (IP: {0})", this.ClientAddress), InformationType.ClientSession_ReceivedKeepAlive);
					}

					else
					{
						this.mLogger.LogError(string.Format("Message type '{0}' isn't defined", message.MessageType), ErrorType.ClientSession_UnknownMessageType);
						throw new AicException(string.Format("Message type '{0}' isn't defined", message.MessageType), AicExceptionType.ClientSession_UnknownMessageType);
					}
				}
			}

			catch (OperationCanceledException)
			{
				this.mLogger.LogInformation(string.Format("Thread handling connection with client (IP: {0}) canceled", this.ClientAddress), InformationType.ClientSession_ThreadCanceled);
			}

			catch (SocketException ex)
			{
				if (ex.SocketErrorCode == SocketError.ConnectionReset)
				{
					this.mLogger.LogInformation(string.Format("Stopped listening to client (IP: {0})", this.ClientAddress), InformationType.ClientSession_StoppedListening);
				}

				else
				{
					this.mLogger.LogError("(ClientSession/SessionHandling/SocketException)", ErrorType.UnknownSocketException, ex);
				}
			}

			catch (Exception ex)
			{
				this.mLogger.LogError("(ClientSession/SessionHandling/Exception)", ErrorType.Undefined, ex);
			}

            finally
            {
                if (timer != null)
                {
                    timer.Dispose();
                }

				lock (this.mLocker)
				{
					if (!this.SessionDestroyed)
					{
						this.DestroySession(true, false);
					}
				}
            }
        }

        /// <summary>
        /// Starts the session for handling the connection (requests / responses) with the client.
        /// In case the connection to the client got broken, the session will destroy itself.
		/// <exception cref="InvalidOperationException">Thrown when the session has already been destroyed.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the session has already been started.</exception>
        /// </summary>
        public void StartSession()
        {
			lock (this.mLocker)
			{
				if (this.SessionDestroyed)
				{
					this.mLogger.LogError("You must not call StartSession after the session got destroyed", ErrorType.ClientSession_SessionAlreadyDestroyed);
					throw new InvalidOperationException("You must not call StartSession after the session got destroyed");
				}

				if (this.SessionStarted)
				{
					this.mLogger.LogError("ClientSession has already been started", ErrorType.ClientSession_SessionAlreadyStarted);
					throw new InvalidOperationException("ClientSession has already been started");
				}

				this.mSessionTask.Start();

				this.mSessionStarted = true;

				this.mLogger.LogInformation(string.Format("Started session with client (IP: {0})", this.ClientAddress), InformationType.ClientSession_StartedSession);
			}
        }

        /// <summary>
        /// Destroys the whole session.
		/// <param name="throwDestroyedEvent">If true the destroy event will be thrown.</param>
		/// <exception cref="InvalidOperationException">Thrown when the session has already been destroyed.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the session hasn't started yet.</exception>
        /// </summary>
        public void DestroySession(bool throwDestroyedEvent)
        {
			this.DestroySession(throwDestroyedEvent, true);
        }

		/// <summary>
		/// Destroys the whole session.
		/// <param name="throwDestroyedEvent">If true the destroy event will be thrown.</param>
		/// <param name="throwDestroyedEvent">If true the Session Handling Task will be canceled.</param>
		/// <exception cref="InvalidOperationException">Thrown when the session has already been destroyed.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the session hasn't started yet.</exception>
		/// </summary>
		private void DestroySession(bool throwDestroyedEvent, bool cancelSessionTask)
		{
			lock (this.mLocker)
			{
				if (this.SessionDestroyed)
				{
					this.mLogger.LogError("You must not call DestroySession after the session got already destroyed", ErrorType.ClientSession_SessionAlreadyDestroyed);
					throw new InvalidOperationException("You must not call DestroySession after the session got already destroyed");
				}

				if (!this.SessionStarted)
				{
					this.mLogger.LogError("ClientSession hasn't been started yet", ErrorType.ClientSession_SessionNotStarted);
					throw new InvalidOperationException("ClientSession hasn't been started yet");
				}

				if (this.mTcpClient != null)
				{
					if (this.mTcpClient.Connected && this.mTcpClient.GetStream() != null)
					{
						this.mTcpClient.GetStream().Close();
					}

					this.mTcpClient.Close();
				}

				this.mLogger.LogInformation(string.Format("Closed TCP-Connection with client. (IP: {0})", this.ClientAddress), InformationType.ClientSession_ClosedConnection);

				this.mSessionDestroyed = true;
			}

			if (throwDestroyedEvent && this.SessionDestroyedEvent != null)
			{
				this.SessionDestroyedEvent();

				this.mLogger.LogInformation(string.Format("SessionDestroyed-Event got called in ClientSession. (IP: {0})", this.ClientAddress), InformationType.ClientSession_RaisedSessionDestroyedEvent);
			}

			if (cancelSessionTask && !this.mSessionTask.IsCanceled)
			{
				this.mSessionTaskCancellationTokenSource.Cancel();
			}
		}

        /// <summary>
        /// Pushes a new state to the client. It will send a request to the client.
        /// Call this method if either the list of alarms or the connection-state to the WAS changed.
        /// </summary>
        public void PushNewState()
        {
			bool lockTaken = false;

			try
			{
				Monitor.Enter(this.mLocker, ref lockTaken);

				while (this.mPushAckPending)
				{
					Monitor.Wait(this.mLocker);
				}

				PushAicMessage(this.mGetAlarmList(), this.mGetWasConnectionState(), MessageType.Request);

				this.mPushAckPending = true;
			}

			finally
			{
				if (lockTaken)
				{
					Monitor.Exit(this.mLocker);
				}
			}
        }

		/// <summary>
		/// Pushes a new AIC-Message to the client.
		/// </summary>
		/// <param name="alarms">The list of active alarms.</param>
		/// <param name="wasConnectionState">The WAS connection-state.</param>
		/// <param name="type">The type of message (request or response).</param>
        private void PushAicMessage(List<Alarm> alarms, bool wasConnectionState, MessageType type)
        {
            if (this.mTcpClient == null || !this.mTcpClient.IsConnected())
            {
                this.mLogger.LogWarning(string.Format("Can't push message because TCP-Client (IP: {0}) has already gone", this.ClientAddress), WarningType.ClientSession_CantPushMessage);
                return;
            }

            AicMessage message = new AicMessage
            {
                Alarms = alarms,
                ConnectionWasToServerOk = wasConnectionState,
                MessageType = type
            };

            try
            {
                message.Serialize(this.mTcpClient.GetStream().GetUnderlyingStream());
				this.mLogger.LogInformation(string.Format("New {0}-push to client (IP: {1}) performed", type, this.ClientAddress), InformationType.ClientSession_PerformedPush);
            }

            catch (Exception ex)
            {
                this.mLogger.LogError("(ClientSession/PushNewState/Exception)", ErrorType.Undefined, ex);
            }
        }

        /// <summary>
        /// Sends the KeepAlive-Message to the client to avoid dirty connection states.
        /// </summary>
        private void SendKeepAliveMessage()
        {
            try
            {
                AicMessage message = new AicMessage
                {
                    Alarms = new List<Alarm>(),
                    ConnectionWasToServerOk = true,
                    MessageType = MessageType.KeepAlive
                };

                message.Serialize(this.mTcpClient.GetStream().GetUnderlyingStream());
            }

            catch (Exception ex)
            {
                this.mLogger.LogError("(ClientListener/SendKeepAliveMessage/Exception)", ErrorType.Undefined, ex);
            }
        }
    }
}