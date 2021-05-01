/*
 *  Copyright 2013 
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

using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading;
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// This class contains functionality for listening to an AicServer.
	/// </summary>
	internal class AicServerListener : AicMessageListener, IAicMessageListener
	{
		private int mCurrentReconnectCount;
		private TcpClient mTcpClient;
		private BackgroundWorker mWorker;
		private readonly string mServerName;
		private readonly int mPort;



		/// <summary>
		/// Initializes a new instance of the AicServerListener class.
		/// </summary>
		/// <param name="ipOrHost">The IP address or host name of the AicServer.</param>
		/// <param name="port">The port number for listening.</param>
		public AicServerListener (string ipOrHost, int port)
		{
			mServerName = ipOrHost;
			mPort = port;
		}

		/// <summary>
		/// Starts listening to the AicServer in background.
		/// </summary>
		public void Start()
		{
			Stop();
			mWorker = new BackgroundWorker();
			mWorker.WorkerReportsProgress = true;
			mWorker.WorkerSupportsCancellation = true;
			mWorker.DoWork += Listener_DoWork;
			mWorker.ProgressChanged += Listener_ProgressChanged;
			mWorker.RunWorkerAsync();
		}

		/// <summary>
		/// Stops listening to the AicServer.
		/// </summary>
		public void Stop()
		{
			Disconnect();
			if (mWorker != null)
			{
				mWorker.CancelAsync();
				mWorker = null;
			}
		}

		// Connects to the AicServer and listens for messages
		private void Listener_DoWork(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = (BackgroundWorker)sender;
			
			while (!worker.CancellationPending)
			{
				#region Connect to server
				while (!worker.CancellationPending && mTcpClient == null)
				{
					Connect();
					var connectionStatusChangedEventArgs = new ConnectionStatusChangedEventArgs();
					connectionStatusChangedEventArgs.ConnectedToServer = mTcpClient != null;
					connectionStatusChangedEventArgs.CurrentReconnectCount = mCurrentReconnectCount;
					worker.ReportProgress(0, connectionStatusChangedEventArgs);
					if (mTcpClient == null)
					{
						Thread.Sleep(5000);
					}
				}
				#endregion

				#region Listen to server
				while (!worker.CancellationPending && mTcpClient != null)
				{
					Thread.Sleep(200);		// Prevent high cpu usage

					// Check last connection and send a keepalive message if necessary
					if (ConnectivityTimeoutExceeded)
					{
						var aicMessage = new AicMessage();
						aicMessage.MessageType = MessageType.KeepAlive;
						try
						{
							Send(aicMessage);
						}
						catch (Exception ex)
						{
							Log.GetInstance().LogError("Error in sending keepalive.", ex);
							Disconnect();
							break;
						}
						LastConnectivity = DateTime.Now;
					}

					// Get stream and check if it contains data
					bool dataAvailable;
					try
					{
						if (mTcpClient == null)
						{
							throw new Exception("mTcpClient is null.");
						}
						dataAvailable = mTcpClient.GetStream().DataAvailable;
					}
					catch (Exception ex)
					{
						Disconnect();
						Log.GetInstance().LogError("Error in getting stream and checking if data is available.", ex);
						break;
					}

					// Read stream data
					if (dataAvailable)
					{
						Log.GetInstance().LogInformation("Received a message from AIC server.");
						LastConnectivity = DateTime.Now;
						AicMessage aicMessage = null;
						try
						{
							// Deserialize stream
							using (var ms = mTcpClient.ReadNetworkStream(AicMessage.XmlEndTagRegex))
							{
								ms.Position = 0;
								aicMessage = AicMessage.Deserialize(ms);
							}
						}
						catch (Exception ex)
						{
							bool connectionOk = CheckConnection();
							if (!connectionOk)
							{
								Disconnect();
								break;
							}
							Log.GetInstance().LogError("Error in deserializing network stream.", ex);
						}

						if (aicMessage != null)
						{
							// Only report AicMessage if it is a request or a response
							if (aicMessage.MessageType == MessageType.Request || aicMessage.MessageType == MessageType.Response)
							{
								worker.ReportProgress(0, new MessageReceivedEventArgs(aicMessage));
							}

							#region Send response

							if (aicMessage.MessageType == MessageType.Request || aicMessage.MessageType == MessageType.KeepAlive)
							{
								try
								{
									string msg = string.Empty;
									var response = new AicMessage();
									if (aicMessage.MessageType == MessageType.Request)
									{
										msg = "response";
										response.Alarms = aicMessage.Alarms;
										response.ConnectionWasToServerOk = aicMessage.ConnectionWasToServerOk;
										response.MessageType = MessageType.Response;
									}
									else if (aicMessage.MessageType == MessageType.KeepAlive)
									{
										msg = "keepalive";
										response.MessageType = MessageType.KeepAlive;
									}
									
									Send(response);
									Log.GetInstance().LogInformation("Sent " + msg + " to AIC server.");
								}
								catch (Exception ex)
								{
									Log.GetInstance().LogError("Error in sending response/keepalive.", ex);
								}
							}

							#endregion
						}
					}
				}
				#endregion
			}
			e.Cancel = worker.CancellationPending;
		}

		// Raises events when the connection status has changed or a message has been received
		private void Listener_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (e.UserState is ConnectionStatusChangedEventArgs)
			{
				OnConnectionStatusChanged(e.UserState as ConnectionStatusChangedEventArgs);
			}
			else if (e.UserState is MessageReceivedEventArgs)
			{
				OnMessageReceived(e.UserState as MessageReceivedEventArgs);
			}
		}

		/// <summary>
		/// Disconnects the listener from the AicServer.
		/// </summary>
		public void Disconnect()
		{
			if (mTcpClient != null)
			{
				try
				{
					mTcpClient.Close();
				}
				catch (Exception ex)
				{
					Log.GetInstance().LogError("Error in closing TcpClient", ex);
				}
				mTcpClient = null;
			}
		}

		/// <summary>
		/// Connects to the AicServer.
		/// </summary>
		private void Connect()
		{
			try
			{
				mTcpClient = new TcpClient();
				mTcpClient.ReceiveTimeout = Constants.NetworkTimeout;
				mTcpClient.SendTimeout = Constants.NetworkTimeout;
				mTcpClient.ConnectToIpOrHost(mServerName, mPort);
				mCurrentReconnectCount = 0;
				Log.GetInstance().LogInformation("Connected successfully to server.");
			}
			catch (Exception ex)
			{
				if (mCurrentReconnectCount < int.MaxValue)		// Prevent OverflowException
				{
					mCurrentReconnectCount++;
				}
				mTcpClient = null;
				Log.GetInstance().LogError("Could not connect to server.", ex);
			}
		}

		/// <summary>
		/// Sends an AicMessage to the AicServer.
		/// </summary>
		/// <param name="aicMessage">The AicMessage to send.</param>
		private void Send(AicMessage aicMessage)
		{
			if (mTcpClient != null)
			{
				aicMessage.Serialize(mTcpClient.GetStream());
			}
		}

		/// <summary>
		/// Sends an AicMessage with MessageType Request to the AicServer.
		/// </summary>
		private void SendRequest()
		{
			var aicMessage = new AicMessage { MessageType = MessageType.Request };
			Send(aicMessage);
		}

		/// <summary>
		/// Ansynchronously sends an AicMessage with MessageType Request to the AicServer.
		/// </summary>
		public void SendRequestAsync()
		{
			var worker = new BackgroundWorker();
			worker.DoWork += (sender, e) => SendRequest();
			worker.RunWorkerAsync();
		}

		/// <summary>
		/// Checks whether the TcpClient is connected to the server.
		/// </summary>
		/// <returns>True if the connection is ok.</returns>
		public bool CheckConnection()
		{
			bool ok;
			try
			{
				ok = mTcpClient.IsConnected();
			}
			catch (Exception ex)
			{
				Log.GetInstance().LogError("Error in checking TcpClient connection.", ex);
				ok = false;
			}
			return ok;
		}
	}
}
