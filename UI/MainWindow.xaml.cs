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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		// Debug variables. Do not change them here. Edit them for debug in the method PrepareForDebug.
		private bool mCheckSystemStatus = true;

		private const int MaxReconnectCount = 5;				// The maximum number of reconnect tries before the screen displays a message	
		private bool mConnectionAicToServerOk;
		private bool mConnectionWasToServerOk;
		private bool mExistentAlarm;

		private readonly DateTime mStartTime;
		private readonly Dictionary<string, TabItem> mAlarmId2Ctrl = new Dictionary<string, TabItem>();
		private readonly DispatcherTimer mChangeTabTimer;
		private readonly DispatcherTimer mNewAlarmTimer;
		private readonly DispatcherTimer mTurnScreenOffTimer;
		private readonly BackgroundWorker mCheckSystemWorker = new BackgroundWorker();
		private readonly SoundHelper mSoundHelper = new SoundHelper(Constants.AlarmSoundPath);
		private AlarmPrinter mAlarmPrinter;
		private IAicMessageListener mAicMessageListener;
		private PublicDisplayControl mPublicDisplayControl;

		/// <summary>
		/// This property is true during the first 5 seconds after program start.
		/// </summary>
		private bool Starting
		{
			get { return (DateTime.Now - mStartTime).TotalSeconds <= 5; }
		}

		private bool ConnectionAicToServerOk
		{
			get { return AicSettings.Global.StandAloneMode || mConnectionAicToServerOk; }
		}

		/// <summary>
		/// Returns the alarm control that is currently displayed. Null if there is no alarm.
		/// </summary>
		private IAlarmControl CurrentAlarmCtrl
		{
			get
			{
				if (AlarmTabCtrl.Items.Count > 0 && AlarmTabCtrl.SelectedItem is TabItem)
				{
					return (AlarmTabCtrl.SelectedItem as TabItem).Content as IAlarmControl;
				}
				return null;
			}
		}

		/// <summary>
		/// Initializes a new main window.
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();

			mStartTime = DateTime.Now;
			Log.GetInstance().IgnoreEqualLogMessages = true;
			Log.GetInstance().LogInformation("AIC started");

			// Timer that turns the screen off if there is no alarm anymore
			mTurnScreenOffTimer = new DispatcherTimer();
			mTurnScreenOffTimer.Tick += mTurnScreenOffTimer_Tick;

			try
			{
				AicSettings.Global = AicSettings.Load();
			}
			catch
			{
				AicSettings.Global = new AicSettings();
				ShowSettings();
			}

			PrepareForDebug();						// This method is only called in DEBUG mode

			ApplySettings();

			// Apply settings without data binding
			InfoCenterCtrl.LogInformation = Log.GetInstance().LogInformation;
			InfoCenterCtrl.LogWarning = Log.GetInstance().LogWarning;
			InfoCenterCtrl.LogError = Log.GetInstance().LogError;

			// Full screen mode
			if (AicSettings.Global.FullScreenMode)
			{
				Visibility = Visibility.Visible;
				WindowState = WindowState.Maximized;
			}
			else
			{
				Visibility = Visibility.Hidden;
			}

			// Multi alarm font size
			MultiAlarmLbl.FontSize = SystemParameters.PrimaryScreenHeight / 18;
			MultiAlarmPageLbl.FontSize = MultiAlarmLbl.FontSize;

			// Check system in a separate thread
			mCheckSystemWorker.DoWork += mCheckSystemWorker_DoWork;
			mCheckSystemWorker.RunWorkerCompleted += mCheckSystemWorker_RunWorkerCompleted;
			mCheckSystemWorker.RunWorkerAsync();

			// One minute timer (checks the system)
			var oneMinuteTimer = new DispatcherTimer();
			oneMinuteTimer.Interval = new TimeSpan(0, 1, 0);
			oneMinuteTimer.Tick += oneMinuteTimer_Tick;
			oneMinuteTimer.Start();

			// Change alarm tab every x seconds
			mChangeTabTimer = new DispatcherTimer();
			mChangeTabTimer.Interval = new TimeSpan(0, 0, AicSettings.Global.AlarmDisplayDuration);
			mChangeTabTimer.Tick += mChangeTabTimer_Tick;
			mChangeTabTimer.Start();

			// Show new alarm for x seconds
			mNewAlarmTimer = new DispatcherTimer();
			mNewAlarmTimer.Interval = new TimeSpan(0, 0, AicSettings.Global.NewAlarmDisplayDuration);
			mNewAlarmTimer.Tick += mNewAlarmTimer_Tick;

			//Thread.Sleep(200);		// Show the splash screen some more time. Do not waste time!
		}

		// Applies all the settings and starts timers
		private void ApplySettings()
		{
			mAlarmPrinter = new AlarmPrinter(AicSettings.Global.PrintServer, AicSettings.Global.PrinterName);
			InfoCenterCtrl.PageDisplayDuration = AicSettings.Global.InfoPageDisplayDuration;
			mTurnScreenOffTimer.Interval = new TimeSpan(0, AicSettings.Global.TurnScreenOffDelayMinutes, 0);
			DemoLbl.Visibility = AicSettings.Global.DemoMode ? Visibility.Visible : Visibility.Hidden;

			// Full screen mode
			bool fullScreen = AicSettings.Global.FullScreenMode;
			Topmost = fullScreen;
			WindowStyle = fullScreen ? WindowStyle.None : WindowStyle.SingleBorderWindow;
			Mouse.OverrideCursor = fullScreen ? Cursors.None : null;
			StatusBar.Visibility = fullScreen ? Visibility.Collapsed : Visibility.Visible;

			// Basic mode
			bool basic = AicSettings.Global.ShowBasicAlarmInfo;
			int colSpan = basic ? 2 : 1;
			MultiAlarmLbl.SetValue(Grid.ColumnSpanProperty, colSpan);
			MultiAlarmPageLbl.Visibility = basic ? Visibility.Collapsed : Visibility.Visible;
			MultiAlarmLbl.HorizontalAlignment = basic ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;

			// Control public display
			if (AicSettings.Global.PublicDisplayIp == null)
			{
				PublicDisplayControl.LogError = null;
				mPublicDisplayControl = null;
			}
			else
			{
				PublicDisplayControl.LogError = Log.GetInstance().LogError;
				mPublicDisplayControl = new PublicDisplayControl(AicSettings.Global.PublicDisplayIp.ToString(), AicSettings.Global.PublicDisplayPort);
			}

			// Listen to the AicServer
			if (AicSettings.Global.DemoMode)
			{
				mAicMessageListener = new FileListener();
			}
			else if (AicSettings.Global.StandAloneMode)
			{
				mAicMessageListener = new DefaultListeningManager();
			}
			else
			{
				mAicMessageListener = new AicServerListener(AicSettings.Global.NetworkServiceIpString, AicSettings.Global.NetworkServicePort);
			}
			mAicMessageListener.ConnectionStatusChanged += listener_ConnectionStatusChanged;
			mAicMessageListener.MessageReceived += listener_MessageReceived;
			try
			{
				mAicMessageListener.Start();
			}
			catch
			{
				mConnectionWasToServerOk = false;
				RefreshMainWindow();
			}
		}

		// Stops all timers and connections
		private void StopListener()
		{
			try
			{
				// Stop listening to server
				if (mAicMessageListener != null)
				{
					mAicMessageListener.ConnectionStatusChanged -= listener_ConnectionStatusChanged;
					mAicMessageListener.MessageReceived -= listener_MessageReceived;
					mAicMessageListener.Stop();
					mAicMessageListener = null;
				}
			}
			catch (Exception ex)
			{
				Log.GetInstance().LogError("Error in stopping listener from MainWindow.", ex);
			}
		}



		[Conditional("DEBUG")]
		private void PrepareForDebug()
		{
			mCheckSystemStatus = true;
		}

		// The connection status to the AicServer has changed
		private void listener_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
		{
			mConnectionAicToServerOk = e.ConnectedToServer;
			if ((e.ConnectedToServer || e.CurrentReconnectCount > MaxReconnectCount) && (mConnectionWasToServerOk || !Starting))
			{
				RefreshMainWindow();
			}
			RefreshStatusItem(e.CurrentReconnectCount);
		}

		// Received a message from the AicServer
		private void listener_MessageReceived(object sender, MessageReceivedEventArgs e)
		{
			if (Dispatcher.CheckAccess())
			{
				mConnectionAicToServerOk = true;
				mConnectionWasToServerOk = e.AicMessage.ConnectionWasToServerOk;
				mExistentAlarm = e.AicMessage.Alarms.Count > 0;
				RefreshMainWindow(e.AicMessage.Alarms);
				RefreshStatusItem();
			}
			else
			{
				Dispatcher.Invoke(new Action<object, MessageReceivedEventArgs>(listener_MessageReceived), sender, e);
			}
		}

		// Refreshes the main window
		private void RefreshMainWindow(ICollection<Alarm> alarms = null)
		{
			bool removeAlarms = false;

			if (!ConnectionAicToServerOk || !mConnectionWasToServerOk)
			{
				#region No connection client to server or no connection WAS to server

				if (!ConnectionAicToServerOk)
				{
					NoAlarmLbl.Content = "Keine Verbindung zum Server vorhanden.";
				}
				else
				{
					NoAlarmLbl.Content = AicSettings.Global.StandAloneMode ? "Keine Verbindung zum WAS vorhanden." : "Keine Verbindung zwischen WAS und Server vorhanden.";
				}
				
				NoAlarmPnl.Visibility = Visibility.Visible;
				InfoCenterCtrl.Visibility = Visibility.Hidden;
				InfoCenterCtrl.Stop();
				removeAlarms = true;

				#endregion
			}
			else if (alarms != null)
			{
				#region Handle alarms
				if (alarms.Count == 0)			// Remove all alarms and show InfoCenter
				{
					#region No alarm. Show InfoCenter

					// Stop sound
					mSoundHelper.Alarm = null;

					// Turn off the public screen if there was an alarm during the night hours
					if (mPublicDisplayControl != null && mAlarmId2Ctrl.Count > 0 && IsTurnScreenOffTime())
					{
						mPublicDisplayControl.TurnOff();
					}

					// Start the timer for turning off the screen
					if (mTurnScreenOffTimer != null && !mTurnScreenOffTimer.IsEnabled && mTurnScreenOffTimer.Interval.Ticks > 0)
					{
						mTurnScreenOffTimer.Start();		// Start() resets and starts the timer
					}

					removeAlarms = true;
					NoAlarmLbl.Content = "Zur Zeit liegt kein Alarm vor.";

					// Load the InfoCenter data and show it
					if (!AicSettings.Global.InfoCenterEnabled)
					{
						NoAlarmPnl.Visibility = Visibility.Visible;
					}
					else if (InfoCenterCtrl.Status == Status.Stopped)
					{
						NoAlarmPnl.Visibility = Visibility.Visible;
						InfoCenterCtrl.LoadData();
					}

					#endregion
				}
				else
				{
					RefreshAlarms(alarms);
				}
				#endregion
			}

			if (removeAlarms)
			{
				#region Remove all alarms

				AlarmGrid.Visibility = Visibility.Hidden;
				foreach (TabItem tab in AlarmTabCtrl.Items)
				{
					DisposeAlarm(tab);
				}
				AlarmTabCtrl.Items.Clear();
				mAlarmId2Ctrl.Clear();

				#endregion
			}
		}
		 
		// Refreshes the status text and the connection item
		private void RefreshStatusItem(int currentReconnectCount = 0)
		{
			string text;

			if (ConnectionAicToServerOk && mConnectionWasToServerOk)
			{
				ServerConnectionStatusItem.Visibility = Visibility.Collapsed;
				text = mExistentAlarm ? "'Strg + P' drücken um den aktuellen Einsatz zu drucken." : "Zur Zeit liegt kein Alarm vor.";
				if (!mExistentAlarm && InfoCenterCtrl.Status == Status.LoadingData)
				{
					text += " Info-Center wird gestartet.";
				}
			}
			else
			{
				ServerConnectionStatusItem.Visibility = Visibility.Visible;
				text = !ConnectionAicToServerOk ? "Keine Verbindung zum Server vorhanden." : "Keine Verbindung zwischen WAS und Server vorhanden.";
				ServerConnectionStatusItem.ToolTip = text;
				if (!ConnectionAicToServerOk && currentReconnectCount > 0 && currentReconnectCount <= MaxReconnectCount)
				{
					text += " Verbindung wird wiederhergestellt (Versuch " + currentReconnectCount + " von " + MaxReconnectCount + ").";
				}
			}

			StatusItem.Content = text;
		}

		// Removes, updates, and adds alarms
		private void RefreshAlarms(ICollection<Alarm> alarms)
		{
			// Show that several concurrent alarms exist
			AlarmGrid.Visibility = Visibility.Visible;
			NoAlarmPnl.Visibility = Visibility.Hidden;
			InfoCenterCtrl.Visibility = Visibility.Hidden;
			InfoCenterCtrl.Stop();
			MultiAlarmGrid.Visibility = alarms.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

			// Remove finished alarms, refresh existing alarms, show new alarms
			var removeIds = mAlarmId2Ctrl.Keys.Except(alarms.Select(a => a.Id)).ToList();
			int numberOfRemovedAlarms = RemoveAlarms(removeIds);
			UpdateAlarms(alarms);
			var newAlarms = AddAlarms(alarms);

			// Ensure that an alarm is shown
			if (newAlarms.Count == 0 && numberOfRemovedAlarms > 0)
			{
				ShowNextAlarm();
			}

			#region Turn screen on, bring window to front, play sound

			if (newAlarms.Count > 0)
			{
				// Turn on screen
				try
				{
					if (mPublicDisplayControl != null)
					{
						mPublicDisplayControl.TurnOn();
					}
					else
					{
						// Stop turning off screen
						if (mTurnScreenOffTimer != null)
						{
							mTurnScreenOffTimer.Stop();
						}
						ScreenPower.TurnOn(this);
					}
				}
				catch (Exception ex)
				{
					Log.GetInstance().LogError("Error in turning on screen.", ex);
				}

				// Bring the AIC window to front
				ShowAndActivate();

				// Play a sound
				if (!Starting)
				{
					try
					{
						mSoundHelper.Alarm = newAlarms.Last();

						if (AicSettings.Global.PlayAlarmSound && !AicSettings.Global.PlayAnnouncement)
						{
							mSoundHelper.PlayAlarmSound();
						}
						else if (AicSettings.Global.PlayAnnouncement)
						{
							mSoundHelper.PlaySequence(AicSettings.Global.PlayAlarmSound, AicSettings.Global.AnnouncementIntervals);
						}
					}
					catch (Exception ex)
					{
						Log.GetInstance().LogError("Could not play sound.", ex);
					}
				}
			}

			#endregion
		}

		// Removes a number of alarms from the main window
		private int RemoveAlarms(IEnumerable<string> alarmIds)
		{
			int count = 0;
			foreach (string id in alarmIds)
			{
				DisposeAlarm(mAlarmId2Ctrl[id]);
				AlarmTabCtrl.Items.Remove(mAlarmId2Ctrl[id]);
				bool removed = mAlarmId2Ctrl.Remove(id);
				if (removed)
				{
					Log.GetInstance().LogInformation("Alarm " + id + " has been removed.");
					count++;
				}
				else
				{
					Log.GetInstance().LogWarning("Could not remove alarm " + id + ".");
				}
			}
			return count;
		}

		// Disposes the alarm control
		private static void DisposeAlarm(ContentControl tabItem)
		{
			if (tabItem != null && tabItem.Content is IAlarmControl)
			{
				(tabItem.Content as IAlarmControl).Dispose();
			}
		}

		// Updates a number of alarms
// ReSharper disable UnusedMethodReturnValue.Local
		private IEnumerable<Alarm> UpdateAlarms(IEnumerable<Alarm> alarms)
// ReSharper restore UnusedMethodReturnValue.Local
		{
			var updateAlarms = new List<Alarm>();

			foreach (Alarm alarm in alarms)
			{
				if (mAlarmId2Ctrl.ContainsKey(alarm.Id))
				{
					bool changed = ((IAlarmControl)mAlarmId2Ctrl[alarm.Id].Content).UpdateAlarmData(alarm);
					if (changed)
					{
						updateAlarms.Add(alarm);
						Log.GetInstance().LogInformation("Alarm " + alarm.Id + " has been updated.");
					}
				}
			}

			return updateAlarms;
		}

		// Adds a number of alarms
		private List<Alarm> AddAlarms(IEnumerable<Alarm> alarms)
		{
			var newAlarms = new List<Alarm>();
			foreach (Alarm alarm in alarms)
			{
				if (mAlarmId2Ctrl.ContainsKey(alarm.Id))
				{
					continue;
				}

				IAlarmControl alarmCtrl;
				if (AicSettings.Global.ShowBasicAlarmInfo)
				{
					alarmCtrl = new BasicAlarmControl(alarm);
				}
				else
				{
					alarmCtrl = new AlarmControl(alarm, AicSettings.Global.FullScreenMode);
				}
				var tabItem = new TabItem();
				tabItem.Content = alarmCtrl;

				int tabIndex = AlarmTabCtrl.Items.Add(tabItem);
				mAlarmId2Ctrl.Add(alarm.Id, tabItem);

				if (!Starting)
				{
					// Show the new alarm for some time
					AlarmTabCtrl.SelectedIndex = tabIndex;
					mNewAlarmTimer.Start();
				}
				newAlarms.Add(alarm);
				Log.GetInstance().LogInformation("Alarm " + alarm.Id + " has been added.");

				if (!AicSettings.Global.DemoMode)
				{
					mAlarmPrinter.Print(alarm, AicSettings.Global.PrintCount);
				}
			}

			return newAlarms;
		}

		// This happens every minute
		private void oneMinuteTimer_Tick(object sender, EventArgs e)
		{
			// Try to turn on the screen if there is an alarm. This must be performed on the UI thread
			if (mPublicDisplayControl == null && mExistentAlarm)
			{
				ScreenPower.TurnOn(this);
			}

			// Turn on/off the public screen at defined hours
			if (mPublicDisplayControl != null && !mExistentAlarm)
			{
				var now = DateTime.Now.TimeOfDay;
				if (Math.Abs((now - AicSettings.Global.PublicDisplayTurnScreenOnTime).TotalMinutes) < 1)
				{
					mPublicDisplayControl.TurnOn();
				}
				else if (Math.Abs((now - AicSettings.Global.PublicDisplayTurnScreenOffTime).TotalMinutes) < 1)
				{
					mPublicDisplayControl.TurnOff();
				}
			}

			// Check system in background
			if (!mCheckSystemWorker.IsBusy)
			{
				mCheckSystemWorker.RunWorkerAsync();
			}
		}

		// Turn on/off screen and check system status
		private void mCheckSystemWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			if (!mCheckSystemStatus)
			{
				return;
			}
			
			var status = new SystemStatus();
			e.Result = status;

			// Check connection
			if (!Starting && mAicMessageListener != null)
			{
				bool connectionOk = mAicMessageListener.CheckConnection();
				if (!connectionOk)
				{
					mAicMessageListener.Disconnect();
				}
			}

			// Check internet and printer
			status.ConnectedToInternet = Network.IsConnectedToInternet();
			status.PrinterServerOk = PrintHelper.CheckPrintServer(mAlarmPrinter.ServerName);
			if (status.PrinterServerOk)
			{
				status.PrinterOk = PrintHelper.CheckPrinter(mAlarmPrinter.ServerName, mAlarmPrinter.PrinterName);
			}
		}

		// Process results of system check
		private void mCheckSystemWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				Log.GetInstance().LogError("Error in checking system status.", e.Error);
			}
			else if (e.Result is SystemStatus)
			{
				var status = e.Result as SystemStatus;

				// Internet connection
				InternetConnectionStatusItem.Visibility = status.ConnectedToInternet ? Visibility.Collapsed : Visibility.Visible;
				if (!status.ConnectedToInternet)
				{
					Log.GetInstance().LogWarning("Not connected to internet.");
				}

				// Printer connection
				string toolTip = null;
				if (status.PrinterServerOk)
				{
					if (!status.PrinterOk)
					{
						if (string.IsNullOrEmpty(mAlarmPrinter.PrinterName))
						{
							toolTip = "Es wurde kein Drucker defniniert.";
						}
						else
						{
							toolTip = "Keine Verbindung zum Drucker vorhanden.";
							Log.GetInstance().LogWarning("Not connected to print server.");
						}
					}
				}
				else
				{
					toolTip = "Keine Verbindung zum Druckserver vorhanden.";
					Log.GetInstance().LogWarning("Not connected to printer.");
				}

				PrinterConnectionStatusItem.ToolTip = toolTip;
				PrinterConnectionStatusItem.Visibility = toolTip == null ? Visibility.Collapsed : Visibility.Visible;
			}
		}

		// Change the active alarm tab every few seconds
		private void mChangeTabTimer_Tick(object sender, EventArgs e)
		{
			if (!mNewAlarmTimer.IsEnabled)
			{
				ShowNextAlarm();
			}
		}

		// Displays the next alarm
		private void ShowNextAlarm()
		{
			if (AlarmTabCtrl.Items.Count > 0)
			{
				if (AlarmTabCtrl.SelectedIndex < AlarmTabCtrl.Items.Count - 1)
				{
					AlarmTabCtrl.SelectedIndex++;
				}
				else
				{
					AlarmTabCtrl.SelectedIndex = 0;
				}
			}
		}

		// Displays the previous alarm
		private void ShowPreviousAlarm()
		{
			if (AlarmTabCtrl.Items.Count > 0)
			{
				if (AlarmTabCtrl.SelectedIndex > 0)
				{
					AlarmTabCtrl.SelectedIndex--;
				}
				else
				{
					AlarmTabCtrl.SelectedIndex = AlarmTabCtrl.Items.Count - 1;
				}
			}
		}

		// The time a new alarm is displayed
		private void mNewAlarmTimer_Tick(object sender, EventArgs e)
		{
			mNewAlarmTimer.Stop();
		}

		// Turns the local screens off after a defined timespan
		private void mTurnScreenOffTimer_Tick(object sender, EventArgs e)
		{
			if (AicSettings.Global.TurnScreenOffDelayMinutes > 0)
			{
				ScreenPower.TurnOff(this);
			}
		}

		// Hide the window to the tray instead of closing
		protected override void OnClosing(CancelEventArgs e)
		{
			Hide();
			TaskBarNotifier.ShowNotifyMessage("AIC wurde in den Infobereich minimiert.");
			e.Cancel = true;
		}

		/// <summary>
		/// Sets the WindowState to Maximized, shows, activates it and gives the focus.
		/// </summary>
		public void ShowAndActivate()
		{
			bool hasBeenTopmost = Topmost;
			Show();
			WindowState = WindowState.Maximized;
			bool activated = Activate();
			if (!activated)
			{
				Log.GetInstance().LogWarning("Window.Activate did not work.");
			}
			Topmost = true;
			Topmost = hasBeenTopmost;
			Focus();
		}

		// Occurs whenever a key has been pressed
		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F1)
			{
				TaskBarNotifier.ShowInfo(this);
			}
			else if (e.Key == Key.F5)
			{
				bool connectionOk = mAicMessageListener.CheckConnection();
				if (connectionOk)
				{
					mAicMessageListener.SendRequestAsync();
				}
				else
				{
					mAicMessageListener.Disconnect();
				}
				if (!mExistentAlarm && AicSettings.Global.InfoCenterEnabled)
				{
					InfoCenterCtrl.LoadData();
				}
			}
			else if (e.Key == Key.F7)
			{
				if (CurrentAlarmCtrl != null)
				{
					mSoundHelper.Alarm = CurrentAlarmCtrl.Alarm.BaseAlarm;
					mSoundHelper.PlayAnnouncement();
				}
			}
			else if (e.Key == Key.F8)
			{
				mSoundHelper.Stop();
			}
			else if (e.Key == Key.F12)
			{
				ShowSettings();
			}
			else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
			{
				if (CurrentAlarmCtrl != null)
				{
					new PrintWindow(mAlarmPrinter.ServerName, mAlarmPrinter.PrinterName).ShowAndClose();
					mAlarmPrinter.Print(CurrentAlarmCtrl.Alarm.BaseAlarm);
				}
			}
			else if (e.Key == Key.Left)
			{
				mChangeTabTimer.Stop();			// Restart the timer
				mChangeTabTimer.Start();
				ShowPreviousAlarm();
				if (InfoCenterCtrl.IsActive)
				{
					InfoCenterCtrl.ShowPreviousPage();
					InfoCenterCtrl.ResetTimer();
				}
			}
			else if (e.Key == Key.Right)
			{
				mChangeTabTimer.Stop();			// Restart the timer
				mChangeTabTimer.Start();
				ShowNextAlarm();
				if (InfoCenterCtrl.IsActive)
				{
					InfoCenterCtrl.ShowNextPage();
					InfoCenterCtrl.ResetTimer();
				}
			}
		}

		// Occurs when another alarm is shown
		private void AlarmTabCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			int count = AlarmTabCtrl.Items.Count;
			int currentAlarmNumber = AlarmTabCtrl.SelectedIndex + 1;
			if (AicSettings.Global.ShowBasicAlarmInfo)
			{
				MultiAlarmLbl.Content = "Einsatz " + currentAlarmNumber + " von " + count;
			}
			else
			{
				MultiAlarmLbl.Content = "Achtung: " + count + " laufende Einsätze!";
				MultiAlarmPageLbl.Content = (currentAlarmNumber) + "/" + count;
			}
			Keyboard.Focus(this);
		}

		/// <summary>
		/// Checks whether the current time is between the the turn screen off and turn on time.
		/// </summary>
		/// <returns>True means that the public display should be turned off.</returns>
		private static bool IsTurnScreenOffTime()
		{
			return Utilities.IsBetweenTime(DateTime.Now.TimeOfDay, AicSettings.Global.PublicDisplayTurnScreenOffTime, AicSettings.Global.PublicDisplayTurnScreenOnTime);
		}

		// The status of the Info-Center has changed
		private void InfoCenterCtrl_StatusChanged(object sender, StatusChangedEventArgs e)
		{
			if (ConnectionAicToServerOk && mConnectionWasToServerOk && !mExistentAlarm)
			{
				if (e.Status == Status.Stopped)
				{
					NoAlarmPnl.Visibility = Visibility.Visible;
					InfoCenterCtrl.Visibility = Visibility.Hidden;
				}
				else if (e.Status == Status.Running)
				{
					NoAlarmPnl.Visibility = Visibility.Hidden;
					InfoCenterCtrl.Visibility = Visibility.Visible;
				}
				RefreshStatusItem();
			}
		}

		// Open the settings window
		private void SettingsButton_Click(object sender, RoutedEventArgs e)
		{
			ShowSettings();
		}

		// Open the settings window
		private void ShowSettings()
		{
			RefreshMainWindow(new List<Alarm>());		// Remove all the alarms from the screen
			Topmost = false;
			Mouse.OverrideCursor = null;		// Use the default cursor when the settings window is opened
			StopListener();
			var window = new SettingsWindow(AicSettings.Global);
			var result = window.ShowDialog();
			if (result.HasValue && result.Value)
			{
				try
				{
					AicSettings.Global = AicSettings.Load();
				}
				catch (Exception)
				{
					MessageBox.Show("Die neuen Einstellungen konnten nicht geladen werden. Bitte starten Sie das Programm neu.", "AIC", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			ApplySettings();
		}
	}
}
