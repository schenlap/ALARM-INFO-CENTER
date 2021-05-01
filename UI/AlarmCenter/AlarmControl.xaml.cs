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
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// This control displays one specific alarm which is set by the property Alarm.
	/// Dispose should be called if the control is no longer needed because otherwise the frame control does not release its resources.
	/// </summary>
	public partial class AlarmControl : IAlarmControl
	{
		private readonly bool mShowDateTime;
		private readonly DependencyAlarm mAlarm;

		/// <summary>
		/// The displayed alarm.
		/// </summary>
		public DependencyAlarm Alarm
		{
			get { return mAlarm; }
		}

		/// <summary>
		/// Creates a new AlarmControl showing the provided alarm.
		/// </summary>
		/// <param name="alarm">The alarm to display.</param>
		/// <param name="showDateTime">Indicates whether the current date and time should be shown.</param>
		public AlarmControl(Alarm alarm, bool showDateTime)
		{
			InitializeComponent();

			mShowDateTime = showDateTime;

			// After the WebBrowser control has been loaded the script error window is deactivated
			MapCtrl.Loaded += (sender, e) => AlarmControlHelper.DeactivateScripting(MapCtrl);

			// Set font size
			double screenHeight = SystemParameters.PrimaryScreenHeight;
			InfoLbl.FontSize = screenHeight / 40;
			double headerFontSize = screenHeight / 30;
			double alarmFontSize = screenHeight / 21;
			AlarmControlHelper.SetChildrenLabelFontSize(HeaderGrid, headerFontSize);
			AlarmControlHelper.SetChildrenLabelFontSize(AlarmInfoPnl, alarmFontSize);
			DateTimeLbl.FontSize = InfoLbl.FontSize;

			mAlarm = new DependencyAlarm(alarm);
			DataContext = mAlarm; // necessary for DataBinding

			// A timer that ticks every second
			var oneSecondTimer = new DispatcherTimer();
			oneSecondTimer.Interval = new TimeSpan(0, 0, 1);
			oneSecondTimer.Tick += oneSecondTimer_Tick;
			oneSecondTimer.Start();

			// Show the route
			try
			{
				MapCtrl.Source = MapHelper.GetRouteUri(mAlarm.BaseAlarm);
			}
			catch (Exception ex)
			{
				Log.GetInstance().LogError("Error when navigating to a URI", ex);
			}
			

			// Refresh the web browser when the route uri has been changed. Necessary because the web browser control does not have dependency properties
			// Todo: We need to check if we need to call RemoveValueChanged
			DependencyPropertyDescriptor.FromProperty(DependencyAlarm.LocationProperty, typeof(DependencyAlarm)).AddValueChanged(mAlarm, NavigateRoute);
			DependencyPropertyDescriptor.FromProperty(DependencyAlarm.LocationPropositionProperty, typeof(DependencyAlarm)).AddValueChanged(mAlarm, NavigateRoute);
		}

		private void NavigateRoute(object obj, EventArgs eventArgs)
		{
			try
			{
				var uri = MapHelper.GetRouteUri(mAlarm.BaseAlarm);
				MapCtrl.Navigate(uri);
			}
			catch (Exception ex)
			{
				Log.GetInstance().LogError("Error when navigating to a URI", ex);
			}
		}

		// This timer ticks every second
		private void oneSecondTimer_Tick(object sender, EventArgs e)
		{
			var now = DateTime.Now;
			var elapsedTime = now.Subtract(mAlarm.StartTime);
			ElapsedTimeLabel.Content = "(Einsatzdauer: " + Math.Floor(elapsedTime.TotalHours) + ":" + elapsedTime.ToString(@"mm\:ss") + ")";

			if (mShowDateTime)
			{
				if (mAlarm.Status == 0)
				{
					InfoBorder.BorderThickness = new Thickness(0, 2, 2, 0);
					DateTimeBorder.Visibility = Visibility.Collapsed;
				}
				else
				{
					InfoBorder.BorderThickness = new Thickness(0, 2, 2, 2);
					DateTimeBorder.Visibility = Visibility.Visible;
					DateTimeLbl.Content = now.ToLongDateString() + " - " + now.ToLongTimeString() + " Uhr";
				}
			}
		}

		// Refresh the website in order to show the entire route
		private void MapCtrl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			NavigateRoute(null, null);	 // Do not use MapCtrl.Refresh(); it does not always work
		}

		/// <summary>
		/// Updates the data of the alarm.
		/// </summary>
		/// <param name="alarm">The alarm with updated data.</param>
		/// <returns>True if the alarm has been updated, otherwise false.</returns>
		public bool UpdateAlarmData(Alarm alarm)
		{
			var updatedProperties = mAlarm.UpdateData(alarm);
			return updatedProperties != null && updatedProperties.Count > 0;
		}

		/// <summary>
		/// Disposes the alarm control. This is important for releasing memory of the WebBrowser control.
		/// </summary>
		public void Dispose()
		{
			MapCtrl.Dispose();
		}

		// Sets the focus to the main window. This is necessary for correct treatment of keyboard events.
		private void MapCtrl_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			var mainWindow = Window.GetWindow(this);
			if (mainWindow != null)
			{
				mainWindow.Focus();
			}
		}
	}
}
