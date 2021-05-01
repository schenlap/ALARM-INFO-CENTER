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
using System.Windows.Threading;
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// This control displays one specific alarm in a basic layout which is set by the property Alarm.
	/// Dispose should be called if the control is no longer needed because otherwise the frame control does not release its resources.
	/// </summary>
	public partial class BasicAlarmControl : IAlarmControl
	{
		private readonly DependencyAlarm mAlarm;

		/// <summary>
		/// The displayed alarm.
		/// </summary>
		public DependencyAlarm Alarm
		{
			get { return mAlarm; }
		}

		/// <summary>
		/// Creates a new BasicAlarmControl showing the provided alarm.
		/// </summary>
		/// <param name="alarm">The alarm to display.</param>
		public BasicAlarmControl(Alarm alarm)
		{
			InitializeComponent();

			// After the WebBrowser control has been loaded the script error window is deactivated
			MapCtrl.Loaded += (sender, e) => AlarmControlHelper.DeactivateScripting(MapCtrl);

			// Set font size
			double screenHeight = SystemParameters.PrimaryScreenHeight;
			double timeFontSize = screenHeight / 32;
			double alarmFontSize = screenHeight / 21;
			AlarmControlHelper.SetChildrenLabelFontSize(AlarmInfoPnl, alarmFontSize);
			AlarmTimeLbl.FontSize = timeFontSize;
			CurrentTimeLbl.FontSize = timeFontSize;

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
			CurrentTimeLbl.Content = DateTime.Now.ToShortTimeString();
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

		// Refresh the website in order to show the entire route
		private void MapCtrl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			NavigateRoute(null, null);		 // Do not use MapCtrl.Refresh(); it does not always work
		}
	}
}
