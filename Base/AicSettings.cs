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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AlarmInfoCenter.Base
{
	[Serializable]
	public class AicSettings
	{
		private int mAlarmDisplayDuration = 8;
		private int mInfoPageDisplayDuration = 8;
		private int mNewAlarmDisplayDuration = 30;
		private int mPrintCount = 1;
		private int mPublicDisplayPort;
		private int mNetworkServicePort = 55555;
		private int mTurnScreenOffDelayMinutes;
		private int mWasPort = 47000;
		private int mWaterMapMode;
		private bool mDemoMode = true;
		private bool mStandAloneMode = true;
		private string mFireBrigadeAddress = "Unbekannte Adresse";
		private string mFireBrigadeName = "Unbekannt";
		private string mInfoCenterDataUrl = @"http://www.alarm-info-center.at/services/infocenter.xml";
		private string mPrintServer = string.Empty;
		private string mPrinterName = string.Empty;
		private string mRouteUrl = @"http://www.alarm-info-center.at/services/alarmroute.php";
		private string mUploadUrl = string.Empty;
		private string mWasserkarteInfoToken = string.Empty;
		private string mWaterMapApiUrl = string.Empty;
		private string mWeatherCityCode = string.Empty;
		private string mWeatherKey = string.Empty;
		private string mWeatherProjectName = string.Empty;
		private IPAddress mWasIp = IPAddress.Parse("192.168.130.100");
		private IPAddress mNetworkServiceIp = IPAddress.Parse("0.0.0.0");
		private Coordinate mFireBrigadeCoordinate = new Coordinate(0, 0);
		private TimeSpan mPublicDisplayTurnScreenOffTime = new TimeSpan(20, 0, 0);
		private TimeSpan mPublicDisplayTurnScreenOnTime = new TimeSpan(8, 0, 0);
		private List<int> mAnnouncementIntervals = new List<int>();
		private GoogleMapType mRouteMapType = GoogleMapType.Road;
		private GoogleMapType mDetailMapType = GoogleMapType.Hybrid;
		private GoogleMapType mWaterMapType = GoogleMapType.Hybrid;

		// This must be a member variable otherwise it will consume a lot of memory
		private static readonly XmlSerializer serializer = new XmlSerializer(typeof(AicSettings));

		private static AicSettings mGlobal = new AicSettings();
		public static AicSettings Global
		{
			get
			{
				return mGlobal ?? (mGlobal = new AicSettings());
			}
			set
			{
				if (value != null)
				{
					mGlobal = value;

					Alarm.MainFireBrigade = value.FireBrigadeName;
					MapHelper.RouteOrigin = value.FireBrigadeAddress;
					MapHelper.RouteUrl = value.RouteUrl;
				}
			}
		}

		/// <summary>
		/// The timespan in seconds an alarm is displayed if several alarms exist.
		/// </summary>
		[XmlElement("AlarmDisplayDuration")]
		public int AlarmDisplayDuration
		{
			get { return mAlarmDisplayDuration; }
			set
			{
				if (value < 1 || value > 3600)
				{
					throw new ArgumentException();
				}
				mAlarmDisplayDuration = value;
			}
		}

		/// <summary>
		/// The intervals in seconds between announcements of the alarm.
		/// </summary>
		public List<int> AnnouncementIntervals
		{
			get
			{
				return mAnnouncementIntervals ?? new List<int>();
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}

				if (value.Any(x => x < 1 || x > 300))
				{
					throw new ArgumentException();
				}
				mAnnouncementIntervals = value;
			}
		}

		/// <summary>
		/// Indicates whether the demo mode is active.
		/// In the demo mode the client reads the alarms from the file menu and does not establish a connection to the server.
		/// </summary>
		public bool DemoMode
		{
			get { return mDemoMode; }
			set { mDemoMode = value; }
		}

		/// <summary>
		/// The map type for detail map on the printing.
		/// </summary>
		public GoogleMapType DetailMapType
		{
			get { return mDetailMapType; }
			set { mDetailMapType = value; }
		}

		/// <summary>
		/// The address of the fire brigade.
		/// </summary>
		public string FireBrigadeAddress
		{
			get
			{
				return mFireBrigadeAddress ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}

				value = value.Trim();
				if (value.Length == 0)
				{
					throw new ArgumentException();
				}

				mFireBrigadeAddress = value;
			}
		}

		/// <summary>
		/// The coordinate of the fire brigade.
		/// </summary>
		public Coordinate FireBrigadeCoordinate
		{
			get
			{
				return mFireBrigadeCoordinate ?? new Coordinate(0, 0);
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}
				mFireBrigadeCoordinate = value;
			}
		}

		/// <summary>
		/// The name of the fire brigade.
		/// </summary>
		public string FireBrigadeName
		{
			get
			{
				return mFireBrigadeName ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}

				value = value.Trim();
				if (value.Length == 0)
				{
					throw new ArgumentException();
				}

				mFireBrigadeName = value;
			}
		}

		/// <summary>
		/// Indicates whether the AIC client should run in full screen mode.
		/// </summary>
		public bool FullScreenMode { get; set; }

		/// <summary>
		/// The URL of the Info-Center data.
		/// </summary>
		public string InfoCenterDataUrl
		{
			get
			{
				return mInfoCenterDataUrl ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}

				Uri result;
				bool ok = Uri.TryCreate(value.Trim(), UriKind.RelativeOrAbsolute, out result);
				if (!ok)
				{
					throw new ArgumentException();
				}

				mInfoCenterDataUrl = result.OriginalString;
			}
		}

		/// <summary>
		/// Indicates whether the Info-Center is enabled.
		/// </summary>
		public bool InfoCenterEnabled { get; set; }

		/// <summary>
		/// The timespan in seconds a page of the Info-Center should be displayed.
		/// </summary>
		public int InfoPageDisplayDuration
		{
			get { return mInfoPageDisplayDuration; }
			set
			{
				if (value < 1 || value > 3600)
				{
					throw new ArgumentException();
				}

				mInfoPageDisplayDuration = value;
			}
		}

		/// <summary>
		/// The IP address of the AIC network service.
		/// </summary>
		[XmlIgnore]
		public IPAddress NetworkServiceIp
		{
			get
			{
				return mNetworkServiceIp;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}
				mNetworkServiceIp = value;
			}
		}

		/// <summary>
		/// The string representation of the AIC network service IP address.
		/// This property is used for serialization.
		/// </summary>
		[XmlElement("NetworkServiceIp")]
		public string NetworkServiceIpString
		{
			get
			{
				return NetworkServiceIp.ToString();
			}
			set
			{
				NetworkServiceIp = IPAddress.Parse(value);
			}
		}

		/// <summary>
		/// The port for communicating with the service.
		/// </summary>
		public int NetworkServicePort
		{
			get
			{
				return mNetworkServicePort;
			}
			set
			{
				if (value < 1 || value > 65535)
				{
					throw new ArgumentException();
				}

				mNetworkServicePort = value;
			}
		}

		/// <summary>
		/// The timespan in seconds a new alarm is displayed before switching to the next alarm.
		/// </summary>
		public int NewAlarmDisplayDuration
		{
			get
			{
				return mNewAlarmDisplayDuration;
			}
			set
			{
				if (value < 1 || value > 3600)
				{
					throw new ArgumentException();
				}

				mNewAlarmDisplayDuration = value;
			}
		}

		/// <summary>
		/// Indicates whether an alarm sound should be played.
		/// </summary>
		public bool PlayAlarmSound { get; set; }

		/// <summary>
		/// Indicates whether an announcement should be played.
		/// </summary>
		public bool PlayAnnouncement { get; set; }

		/// <summary>
		/// The IP address of the public display.
		/// </summary>
		[XmlIgnore]
		public IPAddress PublicDisplayIp;

		/// <summary>
		/// The string representation of the IP address of the public display.
		/// This property is used for serialization.
		/// </summary>
		[XmlElement("PublicDisplayIp")]
		public string PublicDisplayIpString
		{
			get
			{
				return PublicDisplayIp == null ? string.Empty : PublicDisplayIp.ToString();
			}
			set
			{
				IPAddress.TryParse(value, out PublicDisplayIp);
			}
		}

		/// <summary>
		/// The port of the public display.
		/// </summary>
		public int PublicDisplayPort
		{
			get
			{
				return mPublicDisplayPort;
			}
			set
			{
				if (value < 0 || value > 65535)
				{
					throw new ArgumentException();
				}

				mPublicDisplayPort = value;
			}
		}

		/// <summary>
		/// The time that a signal should be sent to turn the screen off.
		/// </summary>
		[XmlIgnore]
		public TimeSpan PublicDisplayTurnScreenOffTime
		{
			get
			{
				return mPublicDisplayTurnScreenOffTime;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}

				if (value.TotalHours > 24)
				{
					throw new ArgumentException();
				}

				mPublicDisplayTurnScreenOffTime = value;
			}
		}

		/// <summary>
		/// The time in ticks that a signal should be sent to turn the screen off.
		/// This property is necessary for correct XML serialization.
		/// </summary>
		[XmlElement("PublicDisplayTurnScreenOffTime")]
		public long PublicDisplayTurnScreenOffTimeTicks
		{
			get
			{
				return PublicDisplayTurnScreenOffTime.Ticks;
			}
			set
			{
				mPublicDisplayTurnScreenOffTime = new TimeSpan(value);
			}
		}

		/// <summary>
		/// The time that a signal should be sent to turn the screen on.
		/// </summary>
		[XmlIgnore]
		public TimeSpan PublicDisplayTurnScreenOnTime
		{
			get
			{
				return mPublicDisplayTurnScreenOnTime;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}

				if (value.TotalHours > 24)
				{
					throw new ArgumentException();
				}

				mPublicDisplayTurnScreenOnTime = value;
			}
		}

		/// <summary>
		/// The time in ticks that a signal should be sent to turn the screen on.
		/// This property is necessary for correct XML serialization.
		/// </summary>
		[XmlElement("PublicDisplayTurnScreenOnTime")]
		public long PublicDisplayTurnScreenOnTimeTicks
		{
			get
			{
				return PublicDisplayTurnScreenOnTime.Ticks;
			}
			set
			{
				mPublicDisplayTurnScreenOnTime = new TimeSpan(value);
			}
		}

		/// <summary>
		/// The name of the print server. Null or empty for the local print server.
		/// </summary>
		public string PrintServer
		{
			get
			{
				return mPrintServer ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}

				mPrintServer = value.Trim();
			}
		}

		/// <summary>
		/// The name of the printer.
		/// </summary>
		public string PrinterName
		{
			get
			{
				return mPrinterName ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}
				mPrinterName = value.Trim();
			}
		}

		/// <summary>
		/// The number of alarm printings to be printed automatically.
		/// </summary>
		public int PrintCount
		{
			get
			{
				return mPrintCount;
			}
			set
			{
				if (value < 0 || value > 100)
				{
					throw new ArgumentException();
				}
				mPrintCount = value;
			}
		}

		/// <summary>
		/// The map type for route on the printing.
		/// </summary>
		public GoogleMapType RouteMapType
		{
			get { return mRouteMapType; }
			set { mRouteMapType = value; }
		}

		/// <summary>
		/// The URL pointing to the map that shows the alarm route.
		/// </summary>
		public string RouteUrl
		{
			get
			{
				return mRouteUrl ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}

				Uri result;
				bool ok = Uri.TryCreate(value.Trim(), UriKind.RelativeOrAbsolute, out result);
				if (!ok)
				{
					throw new ArgumentException();
				}

				mRouteUrl = result.ToString();
			}
		}

		/// <summary>
		/// Indicates whether only basic alarm information should be displayed.
		/// </summary>
		public bool ShowBasicAlarmInfo { get; set; }

		/// <summary>
		/// Indicates whether the AIC should connect directly to the WAS (no service).
		/// </summary>
		public bool StandAloneMode
		{
			get { return mStandAloneMode; }
			set { mStandAloneMode = value; }
		}

		/// <summary>
		/// The timespan in minutes that the screen should be turned off after an alarm. A value less than 1 means that no signal is sent.
		/// </summary>
		public int TurnScreenOffDelayMinutes
		{
			get
			{
				return mTurnScreenOffDelayMinutes < 0 ? 0 : mTurnScreenOffDelayMinutes;
			}
			set
			{
				mTurnScreenOffDelayMinutes = value;
			}
		}

		/// <summary>
		/// Indicates whether new alarms or changes of connections are uploaded.
		/// </summary>
		public bool UploadEnabled { get; set; }

		/// <summary>
		/// The URL of the upload of connection changes an alarms.
		/// </summary>
		public string UploadUrl
		{
			get
			{
				return mUploadUrl ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}

				Uri result;
				bool ok = Uri.TryCreate(value.Trim(), UriKind.RelativeOrAbsolute, out result);
				if (!ok)
				{
					throw new ArgumentException();
				}

				mUploadUrl = result.ToString();
			}
		}

        /// <summary>
        /// If true, the service will use WHITE.
        /// </summary>
		[XmlElement("UseWhite")]
        public bool UseWhite { get; set; }

		/// <summary>
		/// The IP address of the WAS.
		/// </summary>
		[XmlIgnore]
		public IPAddress WasIp
		{
			get
			{
				return mWasIp;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}
				mWasIp = value;
			}
		}

		/// <summary>
		/// The string representation of the IP address of the WAS.
		/// This property is used for serialization.
		/// </summary>
		[XmlElement("WasIp")]
		public string WasIpString
		{
			get
			{
				return WasIp.ToString();
			}
			set
			{
				WasIp = IPAddress.Parse(value);
			}
		}

		/// <summary>
		/// The token to grant access to the wasserkarte.info service.
		/// </summary>
		public string WasserkarteInfoToken
		{
			get { return mWasserkarteInfoToken; }
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}
				mWasserkarteInfoToken = value.Trim();
			}
		}

		/// <summary>
		/// The port for communicating with the WAS.
		/// </summary>
		public int WasPort
		{
			get
			{
				return mWasPort;
			}
			set
			{
				if (value < 1 || value > 65535)
				{
					throw new ArgumentException();
				}
				mWasPort = value;
			}
		}

		/// <summary>
		/// The URL of the water map API service.
		/// </summary>
		public string WaterMapApiUrl
		{
			get
			{
				return mWaterMapApiUrl ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}

				Uri result;
				bool ok = Uri.TryCreate(value.Trim(), UriKind.RelativeOrAbsolute, out result);
				if (!ok)
				{
					throw new ArgumentException();
				}

				mWaterMapApiUrl = result.ToString();
			}
		}

		/// <summary>
		/// The mode of the water map service.
		/// 0: No water map
		/// 1: wasserkarte.info
		/// 2: Custom
		/// </summary>
		public int WaterMapMode
		{
			get { return mWaterMapMode; }
			set
			{
				if (value < 0 || value > 2)
				{
					throw new ArgumentException();
				}
				mWaterMapMode = value;
			}
		}

		/// <summary>
		/// The map type for water map on the printing.
		/// </summary>
		public GoogleMapType WaterMapType
		{
			get { return mWaterMapType; }
			set { mWaterMapType = value; }
		}

		/// <summary>
		/// The city code on wetter.com.
		/// </summary>
		public string WeatherCityCode
		{
			get
			{
				return mWeatherCityCode ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}
				mWeatherCityCode = value.Trim();
			}
		}

		/// <summary>
		/// Indicates wether all the weather data is available.
		/// </summary>
		public bool WeatherEnabled
		{
			get { return !string.IsNullOrWhiteSpace(WeatherCityCode) && !string.IsNullOrWhiteSpace(WeatherKey) && !string.IsNullOrWhiteSpace(WeatherProjectName); }
		}

		/// <summary>
		/// The key on wetter.com.
		/// </summary>
		public string WeatherKey
		{
			get
			{
				return mWeatherKey ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}
				mWeatherKey = value.Trim();
			}
		}

		/// <summary>
		/// The project name on wetter.com.
		/// </summary>
		public string WeatherProjectName
		{
			get
			{
				return mWeatherProjectName ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}
				mWeatherProjectName = value.Trim();
			}
		}

		/// <summary>
		/// The URL of wetter.com
		/// </summary>
		public string WeatherUrl
		{
			get
			{
				string checkSum = "";
				byte[] data = MD5.Create().ComputeHash(Encoding.Default.GetBytes(WeatherProjectName + WeatherKey + WeatherCityCode));
				for (int i = 0; i < data.Length; i++)
				{
					checkSum += data[i].ToString("x2");
				}
				return string.Format("http://api.wetter.com/forecast/weather/city/{0}/project/{1}/cs/{2}", WeatherCityCode, WeatherProjectName, checkSum);
			}
		}



		/// <summary>
		/// Saves the settings as XML to the common application data folder.
		/// </summary>
		public void Save(string fileName = null)
		{
			// Copy application settings to ProgramData folder
			// Search for settings in common application data
			if (!Directory.Exists(Constants.SettingsDirectory))
			{
				Directory.CreateDirectory(Constants.SettingsDirectory);
			}

			if (string.IsNullOrEmpty(fileName))
			{
				fileName = Path.Combine(Constants.SettingsDirectory, Constants.SettingsFile);
			}
			using (var writer = XmlWriter.Create(fileName, new XmlWriterSettings { Indent = true }))
			{
				serializer.Serialize(writer, this);
			}
		}

		/// <summary>
		/// Serializes the settings object as string.
		/// </summary>
		/// <returns>An XML string.</returns>
		public string SerializeAsString()
		{
			var stringBuilder = new StringBuilder();
			using (var writer = XmlWriter.Create(stringBuilder, new XmlWriterSettings { Indent = true,  }))
			{
				serializer.Serialize(writer, this);
			}
			return stringBuilder.ToString();
		}

		/// <summary>
		/// Loads the settings from the default or provided file name.
		/// </summary>
		/// <param name="fileName">The file name of the settings file.</param>
		/// <returns>An AicSettings instance.</returns>
		public static AicSettings Load(string fileName = null)
		{
			if (string.IsNullOrEmpty(fileName))
			{
				fileName = Path.Combine(Constants.SettingsDirectory, Constants.SettingsFile);
			}

			AicSettings settings;
			using (var reader = XmlReader.Create(fileName))
			{
				settings = serializer.Deserialize(reader) as AicSettings;
			}
			return settings;
		}

		/// <summary>
		/// Loads the settings from the provided xml text.
		/// </summary>
		/// <param name="xml">The settings as xml text.</param>
		/// <returns>An AicSettings instance.</returns>
		public static AicSettings LoadFromText(string xml)
		{
			AicSettings settings;
			using (var reader = new StringReader(xml))
			{
				settings = serializer.Deserialize(reader) as AicSettings;
			}
			return settings;
		}
	}
}
