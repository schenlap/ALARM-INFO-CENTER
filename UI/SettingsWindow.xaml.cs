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
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Schema;
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		private bool mInfoCenterDataValid;
		private SoundHelper mSoundHelper;
		private readonly AicSettings mSettings = new AicSettings();
		private AicServerListener mListener;



		/// <summary>
		/// Creates a new instance of the settings window and applies the provided settings.
		/// </summary>
		/// <param name="settings">The settings to be shown in the window.</param>
		public SettingsWindow(AicSettings settings = null)
		{
			InitializeComponent();

			DemoModeBox.ToolTip = "Im Demo-Modus wird keine Verbindung zum Service bzw. WAS hergestellt. Die Daten werden von der Datei " + Constants.AicMessageDemoPath + " gelesen.";

			PrepareMapTypeBox(RouteMapTypeBox);
			PrepareMapTypeBox(DetailMapTypeBox);
			PrepareMapTypeBox(WaterMapTypeBox);

			if (settings != null)
			{
				mSettings = settings;
				ApplySettings(mSettings);
			}
		}

		// Tries to load the demo alarm
		private static Alarm GetDemoAlarm()
		{
			// Check if demo file exists
			if (!File.Exists(Constants.AicMessageDemoPath))
			{
				string msg = string.Format("Die Demo-Datei wurde nicht gefunden ({0}).", Constants.AicMessageDemoPath);
				MessageBox.Show(msg, "AIC", MessageBoxButton.OK, MessageBoxImage.Information);
				return null;
			}

			// Check if demo file is valid
			Alarm alarm = null;
			var aicMessage = AicMessage.TryDeserializeFromFile(Constants.AicMessageDemoPath);
			if (aicMessage == null)
			{
				// Demo file is invalid
				string msg = string.Format("Die Datei {0} ist ungültig.", Constants.AicMessageDemoPath);
				MessageBox.Show(msg, "AIC", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else if (aicMessage.Alarms.Count == 0)
			{
				// Demo file contains no alarms
				string msg = string.Format("Die Datei {0} enthält keinen Alarm.", Constants.AicMessageDemoPath);
				MessageBox.Show(msg, "AIC", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else
			{
				alarm = aicMessage.Alarms.First();
			}
			return alarm;
		}

		// Adjust layout not before after everything has been loaded
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			SetModeLayout();
			SetAnnouncementLayout();
			SetWaterMapBoxLayout();
			SetInfoCenterLayout();
		}

		// Client-Server vs. Stand-Alone mode
		private void ModeBox_Checked(object sender, RoutedEventArgs e)
		{
			SetModeLayout();
		}

		// Client-Server vs. Stand-Alone mode
		private void SetModeLayout()
		{
			if (!IsLoaded)
				return;

			// Get row indexes
			int serviceHostNameBoxRow = Grid.GetRow(NetworksServiceIpBox);
			int servicePortBoxRow = Grid.GetRow(ServicePortBox);
			int wasIpBoxRow = Grid.GetRow(WasIpBox);
			int wasPortBoxRow = Grid.GetRow(WasPortBox);

			if (ModeClientBox.GetIsChecked())
			{
				// Show service boxes, hide WAS boxes
				GeneralGrid.RowDefinitions[serviceHostNameBoxRow].Height = new GridLength(0, GridUnitType.Auto);
				GeneralGrid.RowDefinitions[servicePortBoxRow].Height = new GridLength(0, GridUnitType.Auto);
				GeneralGrid.RowDefinitions[wasIpBoxRow].Height = new GridLength(0);
				GeneralGrid.RowDefinitions[wasPortBoxRow].Height = new GridLength(0);
			}
			else
			{
				// Hide service boxes, show WAS boxes
				GeneralGrid.RowDefinitions[serviceHostNameBoxRow].Height = new GridLength(0);
				GeneralGrid.RowDefinitions[servicePortBoxRow].Height = new GridLength(0);
				GeneralGrid.RowDefinitions[wasIpBoxRow].Height = new GridLength(0, GridUnitType.Auto);
				GeneralGrid.RowDefinitions[wasPortBoxRow].Height = new GridLength(0, GridUnitType.Auto);
			}
		}

		// Enable/Disable announcemnt interval box
		private void SetAnnouncementLayout()
		{
			if (IsLoaded)
			{
				AnnouncementIntervalsBox.IsEnabled = PlayAnnouncementBox.GetIsChecked();
			}
		}

		private void WatermapBox_Checked(object sender, RoutedEventArgs e)
		{
			if (IsLoaded)
			{
				SetWaterMapBoxLayout();
			}
		}

		// Show/Hide water map URL box
		private void SetWaterMapBoxLayout()
		{
			if (NoWaterMapBox.GetIsChecked())
			{
				WaterMapTokenBox.Visibility = Visibility.Collapsed;
				WaterMapUrlBox.Visibility = Visibility.Collapsed;
			}
			else if (WasserkarteInfoBox.GetIsChecked())
			{
				WaterMapTokenBox.Visibility = Visibility.Visible;
				WaterMapUrlBox.Visibility = Visibility.Collapsed;
			}
			else
			{
				WaterMapTokenBox.Visibility = Visibility.Collapsed;
				WaterMapUrlBox.Visibility = Visibility.Visible;
			}

			TestWaterMapButton.IsEnabled = !NoWaterMapBox.GetIsChecked();
		}

		// Enable/Disable info-center boxes
		private void SetInfoCenterLayout()
		{
			bool enabled = InfoCenterEnabledBox.GetIsChecked();
			InfoCenterDataUrlBox.IsEnabled = enabled;
			InfoPageDisplayDurationBox.IsEnabled = enabled;
			WeatherGroupBox.IsEnabled = enabled;
		}

		// Shows the fire brigade on Google Maps
		private void ShowFireBrigadeOnMapButton_Click(object sender, RoutedEventArgs e)
		{
			var uri = MapHelper.GetGoogleMapsUrl(FireBrigadeAddressBox.Text);
			System.Diagnostics.Process.Start(uri);
		}

		// Geocodes the fire brigade address
		private void GeocodeFireBrigadeButton_Click(object sender, RoutedEventArgs e)
		{
			string address = FireBrigadeAddressBox.Text;
			if (string.IsNullOrWhiteSpace(address))
			{
				MessageBox.Show("Es wurde keine Adresse eingegeben.", "AIC", MessageBoxButton.OK, MessageBoxImage.Error);
				FireBrigadeCoordinatesBox.Focus();
			}
			else
			{
				try
				{
					var coordinate = MapHelper.Geocode(address, 2000);
					FireBrigadeCoordinatesBox.Text = coordinate.ToDecimalString();
				}
				catch
				{
					MessageBox.Show("Es konnten keine Koordinaten zur angegebenen Adresse gefunden werden.", "AIC", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Checks the connection between client and server
		private void CheckServiceConnectionButton_Click(object sender, RoutedEventArgs e)
		{
			if (mListener == null)
			{
				// Try to ping the service computer
				bool pingOk;
				try
				{
					pingOk = Network.PingAddress(NetworksServiceIpBox.Text);
				}
				catch (Exception)
				{
					pingOk = false;
				}
				if (!pingOk)
				{
					MessageBox.Show("Ein Ping auf den angegebenen PC war nicht erfolgreich.", "AIC", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				// Check port
				int port;
				bool portOk = int.TryParse(ServicePortBox.Text, out port);
				if (!portOk)
				{
					MessageBox.Show("Der Port wurde nicht korrekt eingegeben.", "AIC", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				mListener = new AicServerListener(NetworksServiceIpBox.Text, port);
				mListener.ConnectionStatusChanged += mListener_ConnectionStatusChanged;
				try
				{
					mListener.Start();
				}
				catch (Exception)
				{
					mListener.Stop();
					mListener.ConnectionStatusChanged -= mListener_ConnectionStatusChanged;
					mListener = null;
					MessageBox.Show("Es konnte keine Verbindung zum AIC-Service hergestellt werden.", "AIC", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			else
			{
				MessageBox.Show(this, "Die Verbindung zum AIC-Service wird bereits getestet.", "AIC", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		// Event for checking the connection to the service
		private void mListener_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
		{
			if (e.ConnectedToServer)
			{
				MessageBox.Show("Die Verbindung zum Service funktioniert.", "AIC", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else
			{
				MessageBox.Show(this, "Es konnte keine Verbindung zum AIC-Service hergestellt werden.", "AIC", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			mListener.Stop();
			mListener.ConnectionStatusChanged -= mListener_ConnectionStatusChanged;
			mListener = null;
		}

		// Play alarm sequence
		private void PlayAlarmSequenceButton_Click(object sender, RoutedEventArgs e)
		{
			// Stop player if it has already been started
			if (mSoundHelper != null)
			{
				mSoundHelper.Stop();
			}

			// Check if alarm sound file exists
			if (PlayAlarmSoundBox.GetIsChecked() && !File.Exists(Constants.AlarmSoundPath))
			{
				string msg = string.Format("Die Datei {0} wurde nicht gefunden. Sie muss sich im Programmordner ({1}) befinden um abegespielt werden zu können.", Constants.AlarmSoundPath, AppDomain.CurrentDomain.BaseDirectory);
				MessageBox.Show(msg, "AIC", MessageBoxButton.OK, MessageBoxImage.Warning);
			}

			// Play sounds according to current settings
			mSoundHelper = new SoundHelper(Constants.AlarmSoundPath);
			List<int> announcementIntervals = null;
			if (PlayAnnouncementBox.GetIsChecked())
			{
				mSoundHelper.Alarm = GetDemoAlarm();
				announcementIntervals = Utilities.GetAnnouncmentIntervals(AnnouncementIntervalsBox.Text);
			}
			mSoundHelper.PlaySequence(PlayAlarmSoundBox.GetIsChecked(), announcementIntervals);
		}

		// Set announcement layout
		private void PlayAnnouncementdBox_CheckChanged(object sender, RoutedEventArgs e)
		{
			SetAnnouncementLayout();
		}

		// Stop playing sound
		private void StopButton_Click(object sender, RoutedEventArgs e)
		{
			if (mSoundHelper != null)
			{
				mSoundHelper.Stop();
			}
		}

		// Enable/Disable Info-Center boxes
		private void InfoCenterEnabledBox_CheckChanged(object sender, RoutedEventArgs e)
		{
			if (IsLoaded)
			{
				SetInfoCenterLayout();
			}
		}

		// Ok clicked
		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			bool ok = Save();
			if (ok)
			{
				DialogResult = true;
				Close();
			}
		}

		// Save clicked
		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			Save();
		}

		// Cancel clicked
		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		// Applies the provided settings to the form
		private void ApplySettings(AicSettings settings)
		{
			// General
			FireBrigadeNameBox.Text = settings.FireBrigadeName;
			FireBrigadeAddressBox.Text = settings.FireBrigadeAddress;
			FireBrigadeCoordinatesBox.Text = settings.FireBrigadeCoordinate.ToDecimalString();
			ModeClientBox.IsChecked = !settings.StandAloneMode;
			ModeStandAloneBox.IsChecked = settings.StandAloneMode;
			NetworksServiceIpBox.Text = settings.NetworkServiceIpString;
			ServicePortBox.Text = settings.NetworkServicePort.ToString();
			WasIpBox.Text = settings.WasIp.ToString();
			WasPortBox.Text = settings.WasPort.ToString();
			FullScreenModeBox.IsChecked = settings.FullScreenMode;
			DemoModeBox.IsChecked = settings.DemoMode;

			// Alarm-Center
			AlarmDisplayDurationBox.Text = settings.AlarmDisplayDuration.ToString();
			NewAlarmDisplayDurationBox.Text = settings.NewAlarmDisplayDuration.ToString();
			ShowBasicAlarmInfoBox.IsChecked = settings.ShowBasicAlarmInfo;
			TurnScreenOffDelayBox.Text = settings.TurnScreenOffDelayMinutes.ToString();
			PlayAlarmSoundBox.IsChecked = settings.PlayAlarmSound;
			PlayAnnouncementBox.IsChecked = settings.PlayAnnouncement;
			AnnouncementIntervalsBox.Text = string.Join(", ", settings.AnnouncementIntervals);
			RouteUrlBox.Text = settings.RouteUrl;
			if (settings.WaterMapMode == 1)
			{
				WasserkarteInfoBox.IsChecked = true;
			}
			else if (settings.WaterMapMode == 2)
			{
				CustomWaterMapBox.IsChecked = true;
			}
			else
			{
				NoWaterMapBox.IsChecked = true;
			}
			WaterMapTokenBox.Text = settings.WasserkarteInfoToken;
			WaterMapUrlBox.Text = settings.WaterMapApiUrl;

			// Print
			PrintServerBox.Text = settings.PrintServer;
			PrinterNameBox.Text = settings.PrinterName;
			PrintCountBox.Text = settings.PrintCount.ToString();
			SetMapType(RouteMapTypeBox, settings.RouteMapType);
			SetMapType(DetailMapTypeBox, settings.DetailMapType);
			SetMapType(WaterMapTypeBox, settings.WaterMapType);

			// Info-Center
			InfoCenterEnabledBox.IsChecked = settings.InfoCenterEnabled;
			InfoCenterDataUrlBox.Text = settings.InfoCenterDataUrl;
			InfoPageDisplayDurationBox.Text = settings.InfoPageDisplayDuration.ToString();
			WeatherProjectNameBox.Text = settings.WeatherProjectName;
			WeatherCityCodeBox.Text = settings.WeatherCityCode;
			WeatherKeyBox.Text = settings.WeatherKey;

			// Public Display
			PublicDisplayIpBox.Text = settings.PublicDisplayIp == null ? string.Empty : settings.PublicDisplayIp.ToString();
			PublicDisplayPortBox.Text = settings.PublicDisplayPort.ToString();
			PublicDisplayTurnOnTimeBox.Text = settings.PublicDisplayTurnScreenOnTime.ToString();
			PublicDisplayTurnOffTimeBox.Text = settings.PublicDisplayTurnScreenOffTime.ToString();
		}

		// Tries to save all the settings and gives a messgae if something fails
		private bool Save()
		{
			const string caption = "AIC - Fehler beim Speichern";

			#region General Settings

			// Fire Brigade Name
			try
			{
				mSettings.FireBrigadeName = FireBrigadeNameBox.Text;
			}
			catch
			{
				GeneralTab.Focus();
				MessageBox.Show("Der Name der Feuerwehr wurde nicht korrekt eingegeben.", caption);
				FireBrigadeNameBox.FocusAndSelectAll();
				return false;
			}

			// Fire Brigade Address
			try
			{
				mSettings.FireBrigadeAddress = FireBrigadeAddressBox.Text;
			}
			catch
			{
				GeneralTab.Focus();
				MessageBox.Show("Die Adresse der Feuerwehr wurde nicht korrekt eingegeben.", caption);
				FireBrigadeAddressBox.FocusAndSelectAll();
				return false;
			}

			// Fire Brigade Coordinates
			try
			{
				mSettings.FireBrigadeCoordinate = Coordinate.Parse(FireBrigadeCoordinatesBox.Text);
				FireBrigadeCoordinatesBox.Text = mSettings.FireBrigadeCoordinate.ToDecimalString();
			}
			catch
			{
				GeneralTab.Focus();
				MessageBox.Show("Die Koordinaten des Feuerwehrhauses wurde nicht korrekt eingegeben.", caption);
				FireBrigadeCoordinatesBox.FocusAndSelectAll();
				return false;
			}

			mSettings.StandAloneMode = ModeStandAloneBox.GetIsChecked();

			if (!ModeStandAloneBox.GetIsChecked())
			{
				// Service Host IP
				try
				{
					mSettings.NetworkServiceIpString = NetworksServiceIpBox.Text;
				}
				catch
				{
					GeneralTab.Focus();
					MessageBox.Show("Der Service Hostname wurde nicht korrekt eingegeben.", caption);
					NetworksServiceIpBox.FocusAndSelectAll();
					return false;
				}

				// Service Port
				try
				{
					mSettings.NetworkServicePort = int.Parse(ServicePortBox.Text);
				}
				catch
				{
					GeneralTab.Focus();
					MessageBox.Show("Der Service Port wurde nicht korrekt eingegeben (1-65535).", caption);
					ServicePortBox.FocusAndSelectAll();
					return false;
				}
			}
			else
			{
				// WAS IP
				try
				{
					mSettings.WasIp = IPAddress.Parse(WasIpBox.Text);
				}
				catch
				{
					GeneralTab.Focus();
					MessageBox.Show("Die WAS IP-Adresse wurde nicht korrekt eingegeben.", caption);
					WasIpBox.FocusAndSelectAll();
					return false;
				}

				// WAS Port
				try
				{
					mSettings.WasPort = int.Parse(WasPortBox.Text);
				}
				catch
				{
					GeneralTab.Focus();
					MessageBox.Show("Der WAS Port wurde nicht korrekt eingegeben (1-65535).", caption);
					WasPortBox.FocusAndSelectAll();
					return false;
				}
			}

			// Full Screen Mode
			mSettings.FullScreenMode = FullScreenModeBox.GetIsChecked();

			// Demo Mode
			mSettings.DemoMode = DemoModeBox.GetIsChecked();

			#endregion

			#region Alarm-Center Settings

			// Alarm display duration
			try
			{
				mSettings.AlarmDisplayDuration = int.Parse(AlarmDisplayDurationBox.Text);
			}
			catch
			{
				AlarmCenterTab.Focus();
				MessageBox.Show("Die Alarmanzeigedauer wurde nicht korrekt eingegeben (1-3600).", caption);
				AlarmDisplayDurationBox.FocusAndSelectAll();
				return false;
			}

			// New alarm display duration
			try
			{
				mSettings.NewAlarmDisplayDuration = int.Parse(NewAlarmDisplayDurationBox.Text);
			}
			catch
			{
				AlarmCenterTab.Focus();
				MessageBox.Show("Die Anzeigedauer eines neuen Alarms wurde nicht korrekt eingegeben (1-3600).", caption);
				NewAlarmDisplayDurationBox.FocusAndSelectAll();
				return false;
			}

			// Basic alarm info
			mSettings.ShowBasicAlarmInfo = ShowBasicAlarmInfoBox.GetIsChecked();

			// Turn screen off
			try
			{
				mSettings.TurnScreenOffDelayMinutes = int.Parse(TurnScreenOffDelayBox.Text);
			}
			catch
			{
				AlarmCenterTab.Focus();
				MessageBox.Show("Die Abschaltzeit des Bildschirms nach einem Einsatz wurde nicht korrekt eingegeben.", caption);
				TurnScreenOffDelayBox.FocusAndSelectAll();
				return false;
			}

			// Play alarm sound
			mSettings.PlayAlarmSound = PlayAlarmSoundBox.GetIsChecked();

			// Play announcement
			mSettings.PlayAnnouncement = PlayAnnouncementBox.GetIsChecked();

			// Announcement intervals
			try
			{
				var intervals = new List<int>();
				string text = AnnouncementIntervalsBox.Text.Trim();
				if (text.Length > 0)
				{
					intervals = text.Split(',').Select(int.Parse).ToList();
				}
				mSettings.AnnouncementIntervals = intervals;
			}
			catch
			{
				PlayAnnouncementBox.IsChecked = true;
				AlarmCenterTab.Focus();
				MessageBox.Show("Die Sprachdurchsagezeiten wurden nicht korrekt eingegeben.", caption);
				AnnouncementIntervalsBox.FocusAndSelectAll();
				return false;
			}

			// Route URL
			try
			{
				mSettings.RouteUrl = RouteUrlBox.Text;
			}
			catch
			{
				AlarmCenterTab.Focus();
				MessageBox.Show("Die URL zur Anzeige der Anfahrtsroute wurde nicht korrekt eingegeben.", caption);
				RouteUrlBox.FocusAndSelectAll();
				return false;
			}

			// Water Map mode
			try
			{
				if (NoWaterMapBox.GetIsChecked())
				{
					mSettings.WaterMapMode = 0;
				}
				else if (WasserkarteInfoBox.GetIsChecked())
				{
					mSettings.WaterMapMode = 1;
				}
				else
				{
					mSettings.WaterMapMode = 2;
				}
			}
			catch (Exception)
			{
				AlarmCenterTab.Focus();
				MessageBox.Show("Die Wasserkarte wurde nicht korrekt konfiguriert.", caption);
				return false;
			}

			// wasserkarte.info token
			try
			{
				mSettings.WasserkarteInfoToken = WaterMapTokenBox.Text;
			}
			catch
			{
				AlarmCenterTab.Focus();
				MessageBox.Show("Der Token für wasserkarte.info wurde nicht korrekt eingegeben.", caption);
				WaterMapTokenBox.FocusAndSelectAll();
				return false;
			}

			// Water Map URL
			try
			{
				mSettings.WaterMapApiUrl = WaterMapUrlBox.Text;
			}
			catch
			{
				AlarmCenterTab.Focus();
				MessageBox.Show("Die URL der Wasserkarte wurde nicht korrekt eingegeben.", caption);
				WaterMapUrlBox.FocusAndSelectAll();
				return false;
			}

			#endregion

			#region Print Settings

			// Print server
			try
			{
				mSettings.PrintServer = PrintServerBox.Text;
			}
			catch
			{
				PrintTab.Focus();
				MessageBox.Show("Der Druckserver wurde nicht korrekt eingegeben.", caption);
				PrintServerBox.FocusAndSelectAll();
				return false;
			}

			// Printer name
			try
			{
				mSettings.PrinterName = PrinterNameBox.Text;
			}
			catch
			{
				PrintTab.Focus();
				MessageBox.Show("Der Name des Druckers wurde nicht korrekt eingegeben.", caption);
				PrinterNameBox.FocusAndSelectAll();
				return false;
			}

			// Print count
			try
			{
				mSettings.PrintCount = int.Parse(PrintCountBox.Text);
			}
			catch
			{
				PrintTab.Focus();
				MessageBox.Show("Der Anzahl der Ausdrucke wurde nicht korrekt eingegeben (0-100).", caption);
				PrintCountBox.FocusAndSelectAll();
				return false;
			}

			// Route map type
			mSettings.RouteMapType = GetSelectedMapType(RouteMapTypeBox);

			// Detail map type
			mSettings.DetailMapType = GetSelectedMapType(DetailMapTypeBox);

			// Water map type
			mSettings.WaterMapType = GetSelectedMapType(WaterMapTypeBox);

			#endregion

			#region Info-Center Settings

			// Info Center enabled
			mSettings.InfoCenterEnabled = InfoCenterEnabledBox.GetIsChecked();

			// Info Center URL
			try
			{
				mSettings.InfoCenterDataUrl = InfoCenterDataUrlBox.Text;
			}
			catch
			{
				InfoCenterTab.Focus();
				MessageBox.Show("Die URL des Info-Center wurde nicht korrekt eingegeben.", caption);
				InfoCenterDataUrlBox.FocusAndSelectAll();
				return false;
			}

			// Info Center page duration
			try
			{
				mSettings.InfoPageDisplayDuration = int.Parse(InfoPageDisplayDurationBox.Text);
			}
			catch
			{
				InfoCenterTab.Focus();
				MessageBox.Show("Die Anzeigedauer einer Seite des Info-Center wurde nicht korrekt eingegeben (1-3600).", caption);
				InfoPageDisplayDurationBox.FocusAndSelectAll();
				return false;
			}

			// Weather project name
			try
			{
				mSettings.WeatherProjectName = WeatherProjectNameBox.Text;
			}
			catch
			{
				InfoCenterTab.Focus();
				MessageBox.Show("Der Projektname von wetter.com wurde nicht korrekt eingegeben.", caption);
				WeatherProjectNameBox.FocusAndSelectAll();
				return false;
			}

			// Weather city code
			try
			{
				mSettings.WeatherCityCode = WeatherCityCodeBox.Text;
			}
			catch
			{
				InfoCenterTab.Focus();
				MessageBox.Show("Der City Code von wetter.com wurde nicht korrekt eingegeben.", caption);
				WeatherCityCodeBox.FocusAndSelectAll();
				return false;
			}

			// Weather API Key
			try
			{
				mSettings.WeatherKey = WeatherKeyBox.Text;
			}
			catch
			{
				InfoCenterTab.Focus();
				MessageBox.Show("Der API Key von wetter.com wurde nicht korrekt eingegeben.", caption);
				WeatherKeyBox.FocusAndSelectAll();
				return false;
			}

			#endregion

			#region Public Display Settings

			// IP
			try
			{
				string ip = PublicDisplayIpBox.Text.Trim();
				mSettings.PublicDisplayIp = ip.Length > 0 ? IPAddress.Parse(PublicDisplayIpBox.Text) : null;
			}
			catch
			{
				PublicDisplayTab.Focus();
				MessageBox.Show("Die IP-Adresse des Public Display wurde nicht korrekt eingegeben.", caption);
				PublicDisplayIpBox.FocusAndSelectAll();
				return false;
			}

			// Port
			try
			{
				mSettings.PublicDisplayPort = int.Parse(PublicDisplayPortBox.Text);
			}
			catch
			{
				PublicDisplayTab.Focus();
				MessageBox.Show("Der Port des Public Display wurde nicht korrekt eingegeben (0-65535).", caption);
				PublicDisplayPortBox.FocusAndSelectAll();
				return false;
			}

			// Turn on time
			try
			{
				mSettings.PublicDisplayTurnScreenOnTime = TimeSpan.Parse(PublicDisplayTurnOnTimeBox.Text);
			}
			catch
			{
				PublicDisplayTab.Focus();
				MessageBox.Show("Die Einschaltzeit des Public Display wurde nicht korrekt eingegeben (00:00-23:59).", caption);
				PublicDisplayTurnOnTimeBox.FocusAndSelectAll();
				return false;
			}

			// Turn on time
			try
			{
				mSettings.PublicDisplayTurnScreenOffTime = TimeSpan.Parse(PublicDisplayTurnOffTimeBox.Text);
			}
			catch
			{
				PublicDisplayTab.Focus();
				MessageBox.Show("Die Ausschaltzeit des Public Display wurde nicht korrekt eingegeben (00:00-23:59).", caption);
				PublicDisplayTurnOffTimeBox.FocusAndSelectAll();
				return false;
			}

			#endregion

			// Save settings to settings file
			try
			{
				mSettings.Save();
			}
			catch
			{
				MessageBox.Show("Die Einstellungen konnten nicht gespeichert werden.", caption);
				return false;
			}

			return true;
		}

		// Check print server
		private void CheckPrintServerButton_Click(object sender, RoutedEventArgs e)
		{
			string printServer = string.IsNullOrEmpty(PrintServerBox.Text) ? null : PrintServerBox.Text;
			bool ok = PrintHelper.CheckPrintServer(printServer);
			if (ok)
			{
				MessageBox.Show("Die Verbindung zum Druckserver ist in Ordnung.", "AIC");
			}
			else
			{
				MessageBox.Show("Es besteht keine Verbindung zum Druckserver", "AIC", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// Check printer
		private void CheckPrinterButton_Click(object sender, RoutedEventArgs e)
		{
			string printServer = string.IsNullOrEmpty(PrintServerBox.Text) ? null : PrintServerBox.Text;
			bool ok = PrintHelper.CheckPrinter(printServer, PrinterNameBox.Text);
			if (ok)
			{
				MessageBox.Show("Die Verbindung zum Drucker ist in Ordnung.", "AIC");
			}
			else
			{
				MessageBox.Show("Es besteht keine Verbindung zum Drucker", "AIC", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// Test the info-center url
		private void TestInfoCenterUrlButton_Click(object sender, RoutedEventArgs e)
		{
			mInfoCenterDataValid = true;
			try
			{
				var xmlReaderSettings = new XmlReaderSettings();
				xmlReaderSettings.Schemas.Add(null, Constants.InfoCenterSchemaDefinitionUrl);
				xmlReaderSettings.ValidationType = ValidationType.Schema;
				xmlReaderSettings.ValidationEventHandler += settings_ValidationEventHandler;
				using (var xmlReader = XmlReader.Create(InfoCenterDataUrlBox.Text, xmlReaderSettings))
				{
					while (xmlReader.Read()) { }
				}
			}
			catch
			{
				mInfoCenterDataValid = false;
				MessageBox.Show("Die angegebenen Info-Center Daten sind nicht gültig.", "AIC", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			if (mInfoCenterDataValid)
			{
				MessageBox.Show("Die angegebene Info-Center Datei ist gültig.", "AIC");
			}
		}

		// Info-Center url validation handler
		private void settings_ValidationEventHandler(object sender, ValidationEventArgs e)
		{
			mInfoCenterDataValid = false;
			var icon = e.Severity == XmlSeverityType.Error ? MessageBoxImage.Error : MessageBoxImage.Warning;
			MessageBox.Show(e.Message, "AIC", MessageBoxButton.OK, icon);
		}

		// Clears all items, adds the map types, and selects the first item
		private void PrepareMapTypeBox(ComboBox box)
		{
			box.Items.Clear();
			box.Items.Add(new ComboBoxItem {Content = "Straße", Tag = GoogleMapType.Road});
			box.Items.Add(new ComboBoxItem { Content = "Satellit", Tag = GoogleMapType.Satellite });
			box.Items.Add(new ComboBoxItem { Content = "Hybrid (Straße + Satellit)", Tag = GoogleMapType.Hybrid });
			box.Items.Add(new ComboBoxItem { Content = "Terrain (Gelände)", Tag = GoogleMapType.Terrain });
			box.SelectedIndex = 0;
		}

		// Returns the selected map type of the provided box
		private GoogleMapType GetSelectedMapType(ComboBox box)
		{
			var mapType = GoogleMapType.Road;
			var item = box.SelectedItem as ComboBoxItem;
			if (item != null)
			{
				try
				{
					mapType = (GoogleMapType)item.Tag;
				}
				catch (Exception ex)
				{
					Log.GetInstance().LogError("The tag property of " + box.Name + " is not of type StaticMapItem.", ex);
				}
			}
			return mapType;
		}

		// Set the selected item of the box to the provided value
		private void SetMapType(ComboBox box, GoogleMapType mapTye)
		{
			foreach (var item in box.Items)
			{
				var boxItem = item as ComboBoxItem;
				if (boxItem != null && (GoogleMapType)boxItem.Tag == mapTye)
				{
					box.SelectedItem = item;
					return;
				}
			}
		}

		private void TestWaterMap_Click(object sender, RoutedEventArgs e)
		{
			Coordinate coordinate;
			try
			{
				coordinate = Coordinate.Parse(FireBrigadeCoordinatesBox.Text);
			}
			catch (Exception)
			{
				MessageBox.Show("Der Test konnte nicht durchgeführt werden, da die Koordinaten des Feuerwehrhauses nicht im korrekten Format sind.", "AIC", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			var waterMapQuery = new WaterMapQuery(WasserkarteInfoBox.GetIsChecked(), WaterMapTokenBox.Text, WaterMapUrlBox.Text);
			try
			{
				var result = waterMapQuery.FindWaterSupplyPoints(coordinate);
				if (result == null)
				{
					throw new NullReferenceException();
				}
				MessageBox.Show("Der Service 'Wasserkarte' funktioniert.", "AIC", MessageBoxButton.OK);
			}
			catch (Exception)
			{
				MessageBox.Show("Der Service 'Wasserkarte' funktioniert nicht.", "AIC", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
