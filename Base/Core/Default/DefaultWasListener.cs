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
using System.Linq;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Reflection;
using RestSharp.Contrib;

namespace AlarmInfoCenter.Base
{
    /// <summary>
    /// Handles the connection (request/response) with the WAS.
    /// </summary>
    public class DefaultWasListener : IWasListener
    {
		/// <summary>
		/// Default locker object for the class.
		/// </summary>
		private readonly object mLocker = new object();

		/// <summary>
		/// Last WASObject to determine if the recent received WASObject is the same.
		/// </summary>
		private WasObject mLastWasObject = null;

		/// <summary>
		/// Backgroundworker handling the requests / responses from the WAS.
		/// </summary>
		private readonly BackgroundWorker mWasListenWorker;

		/// <summary>
		/// Current logger instance.
		/// </summary>
		private readonly ILogger mLogger;

		/// <summary>
		/// Factory for several core AIC objects.
		/// </summary>
		private readonly ICoreObjectFactory mCoreObjectFactory;

		private bool mHasConnectionEstablished;
		/// <summary>
        /// True if the listener has established a connection to/with the WAS, otherwise false.
        /// </summary>
		public bool HasConnectionEstablished
		{
			get { return this.mHasConnectionEstablished; }
		}

		private bool mIsRunning;
        /// <summary>
        /// True if the listener is currently running, otherwise false.
        /// </summary>
		public bool IsRunning
		{
			get { return this.mIsRunning; }
		}

        /// <summary>
        /// Triggers the event in case of the connection to the WAS got established or lost.
        /// Second parameter determines if the shutdown is in progress.
        /// </summary>
        public event Action<bool, bool> WasConnectionStateChanged;

        /// <summary>
        /// Triggers the event in case the WAS has sent new data (a new WasObject).
        /// </summary>
        public event Action<WasObject> WasObjectChanged;

    
        
        public DefaultWasListener(ILogger logger, ICoreObjectFactory coreObjectFactory)
        {
			this.mLogger = logger;
			this.mCoreObjectFactory = coreObjectFactory;

            this.mWasListenWorker = new BackgroundWorker();
            this.mWasListenWorker.WorkerSupportsCancellation = true;
            this.mWasListenWorker.DoWork += StartWorker;
            this.mWasListenWorker.RunWorkerCompleted += WorkCompleted;

            this.WasConnectionStateChanged += delegate(bool state, bool isShutdown)
            {
                mHasConnectionEstablished = state;
            };
        }


        /// <summary>
        /// Starts listening to the WAS.
		/// <exception cref="InvalidOperationException">Thrown if the service is already running.</exception>
        /// </summary>
        public void StartListening()
        {
			lock (this.mLocker)
			{
				if (!this.IsRunning)
				{
					this.mWasListenWorker.RunWorkerAsync();
					this.mLogger.LogInformation("Listening to the WAS started", InformationType.WasListener_WorkerStarted);

					this.mIsRunning = true;
				}

				else
				{
					this.mLogger.LogError("The WAS-Listener worker has already been started", ErrorType.WasListener_WorkerAlreadyRunning);
					throw new InvalidOperationException("The WAS-Listener worker has already been started");
				}
			}
        }

        /// <summary>
        /// Stops listening to the WAS.
		/// <exception cref="InvalidOperationException">Thrown if the service isn't running.</exception>
        /// </summary>
        public void StopListening()
        {
			lock (this.mLocker)
			{
				if (this.IsRunning)
				{
					this.mWasListenWorker.CancelAsync();
					this.mLogger.LogInformation("Listening to the WAS stopped", InformationType.WasListener_WorkerStopped);

					this.mIsRunning = false;
				}

				else
				{
					this.mLogger.LogError("The WAS-Listener worker isn't running", ErrorType.WasListener_WorkerNotRunning);
					throw new InvalidOperationException("The WAS-Listener worker isn't running");
				}
			}
        }

        /// <summary>
        /// Starts listening to the WAS. Uses the event 'WasConnectionStateChanged' to report changes of the connection-state.
        /// If any data is on the client-stream available, the method will report the progress using the 'WasObjectChanged' event.
        /// This method also starts a new timer to send the KeepAlive-Message.
        /// </summary>
        private void StartWorker(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            ITcpClient tcpClient = null;
            System.Timers.Timer t = null;

			try
			{
				tcpClient = this.mCoreObjectFactory.CreateTcpClient();
				tcpClient.Connect(AicSettings.Global.WasIp, AicSettings.Global.WasPort);

				t = new System.Timers.Timer(Constants.WasKeepAliveTimeout.TotalMilliseconds);
				t.Elapsed += delegate { SendKeepAliveMessage(tcpClient); };
				t.Start();

				tcpClient.GetStream().Write(Constants.GetAlarmsCommandBytes, 0, Constants.GetAlarmsCommandBytes.Length);

				if (this.WasConnectionStateChanged != null)
				{
					Thread eventThread = new Thread(new ThreadStart(delegate
					{
						this.WasConnectionStateChanged(true, false);
					})) { IsBackground = true };

					eventThread.Start();
				}

				while (!worker.CancellationPending)
				{
					// Check if the connection to the WAS got lost
					if (!tcpClient.IsConnected())
					{
						this.mLogger.LogError("WAS disconnected while waiting for new data", ErrorType.WasListener_WasDisconnectWhileWaiting);
						throw new SocketException();
					}

					// Check if any data from the WAS is available
					else if (!tcpClient.GetStream().DataAvailable)
					{
						Thread.Sleep((int)Constants.WasRequestCycleTimeout.TotalMilliseconds);

						continue;
					}

					this.HandleWasData(tcpClient);
				}
			}

			catch (Exception ex)
			{
				this.mLogger.LogError("(WasListener/StartWorker/Exception)", ErrorType.Undefined, ex);
				throw ex;
			}

            finally
            {
				lock (this.mLocker)
				{
					if (t != null)
					{
						t.Dispose();
					}

					if (tcpClient != null)
					{
						if (tcpClient.Connected)
						{
							// The networkstream will not close itself - see MSDN
							IStream networkStream = tcpClient.GetStream();
							networkStream.Close();
						}

						tcpClient.Close();
					}

					this.mIsRunning = false;
				}
            }
        }

        /// <summary>
        /// The method will raise the WasConnectionStateChanged-Method with arguments
        /// depending on an possible error-state.
        /// </summary>
        private void WorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.WasConnectionStateChanged != null)
            {
                Thread eventThread = new Thread(new ThreadStart(delegate
                {
					if (e.Error != null)
					{
						// Socket error -> try to reconnect
						if (e.Error is SocketException)
						{
							this.mLogger.LogError("WAS Listener stopped due to Socket Error", ErrorType.WasListener_ListenerStoppedSocketError, e.Error);
							this.WasConnectionStateChanged(false, false);
						}

						// Shutdown the listener and don't try to reconnect due to unknown error
						else
						{
							this.mLogger.LogError("WAS Listener stopped due to Unknown Error", ErrorType.WasListener_ListenerStoppedUnknownError, e.Error);
							this.WasConnectionStateChanged(false, true);
						}
					}

					else
					{
						this.WasConnectionStateChanged(false, true);
					}
                })) { IsBackground = true };

                eventThread.Start();
            }
        }

        /// <summary>
        /// The method reads the network stream and will then decode the received WasObject. 
        /// Afterwards it calls the 'WasObjectChanged' event iff the WASObject
        /// has changed (Comparison with mLastWasObject).
        /// </summary>
        private void HandleWasData(ITcpClient tcpClient)
        {
            using(MemoryStream stream = tcpClient.ReadNetworkStream(WasObject.XmlWasObjectEndTagRegex))
			{
				WasObject wasObject = null;

				try
				{
					stream.Position = 0;
					wasObject = WasObject.Deserialize(stream);
				}

				catch
				{
					string xml = Utilities.StreamToString(stream, Constants.WasEncoding);
					wasObject = WasObject.Deserialize(xml);
				}

				this.mLogger.LogInformation("WAS XML deserialized", InformationType.WasListener_WasObjectDeserialized);

                bool wasObjectChanged = !wasObject.Equals(this.mLastWasObject);

                if (wasObjectChanged && this.WasObjectChanged != null)
                {
                    this.mLastWasObject = wasObject;

                    Thread eventThread = new Thread(new ThreadStart(delegate
                    {
                        this.WasObjectChanged(wasObject);
                    })) { IsBackground = true };

                    eventThread.Start();
                }
			}
        }

        /// <summary>
        /// Sends the KeepAlive-Message to the WAS. 
        /// Uses the GetAlarmsCommand (recommended from the LFK).
        /// </summary>
        /// <param name="client">The WAS-TcpClient.</param>
        private void SendKeepAliveMessage(ITcpClient tcpClient)
        {
			tcpClient.GetStream().Write(Constants.GetAlarmsCommandBytes, 0, Constants.GetAlarmsCommandBytes.Length);

			mLogger.LogInformation("Sent keep alive message to WAS", InformationType.WasListener_KeepAliveSent);
        }
    }
}
