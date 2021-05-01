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
using System.IO;
using System.Text;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class contains a number of constants that define the usage of AIC for a specific fire brigade.
	/// </summary>
	public static class Constants
	{
		/// <summary>
		/// The alarm subject for a fire detector alarm (BRANDMELDEALARM).
		/// </summary>
        public const string BMAType = "BRANDMELDEALARM";

		/// <summary>
		/// The BMA Identifier used by the ELS.
		/// </summary>
		public const string BMAIdentificationCharacter = "#";

		/// <summary>
		/// Command used to get alarms from the WAS and thus keep the connection alive.
		/// </summary>
		public const string GetAlarmsCommand = "get-alarms";

		/// <summary>
		/// The GetAlarmsCommand as a byte array.
		/// </summary>
		public static readonly byte[] GetAlarmsCommandBytes = new ASCIIEncoding().GetBytes(GetAlarmsCommand);

		/// <summary>
		/// The default timeout that is used for network transfers in milliseconds.
		/// </summary>
		public const int NetworkTimeout = 3000;

		/// <summary>
		/// The timeouts used to download data from the internet in milliseconds.
		/// </summary>
		public static int[] NetworkTimeouts = { NetworkTimeout, 10000, 30000 };

		/// <summary>
		/// Used in the ClientSession class of the server.
		/// Timeout between two listener-cycles (cycle checks if any request from the client is pending).
		/// </summary>
		public const int ClientCycleTimeout = 500;

		/// <summary>
		/// Used in the WasListener class of the server.
		/// Time between two check cycles if the WAS has sent data.
		/// </summary>
		public static readonly TimeSpan WasRequestCycleTimeout = TimeSpan.FromSeconds(0.5);

		/// <summary>
		/// Used in the ClientSession class of the server.
		/// Maximum number of retrys in case there hasn't been a response from the client.
		/// </summary>
		public const int MaxPushRetry = 5;

		/// <summary>
		/// The name of the log where the events will be written to.
		/// </summary>
		public const string EventLogName = "AIC";

		/// <summary>
		/// The source name for AIC event logs.
		/// </summary>
		public const string EventLogSourceName = "AIC";

		/// <summary>
		/// The path and filename of the alarm sound.
		/// </summary>
		public const string AlarmSoundPath = "alarm.wav";

		/// <summary>
		/// The name of the file that contains all AIC settings.
		/// </summary>
		public const string SettingsFile = "settings.xml";

		/// <summary>
		/// Sometimes this character sequence is part of a WAS XML which
		/// makes the whole XML invalid and the parser eventually crashes.
		/// </summary>
		public const string WasXmlAmpersandFailureSequence = "&amp";

        /// <summary>
        /// Timeout between each reconnect-try in case the WAS-Connection got broken.
        /// </summary>
        public static readonly TimeSpan WasReconnectTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Timeout between each WAS-Server push.
        /// </summary>
        public static readonly TimeSpan WasKeepAliveTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Timeout between each Client Keep-Alive push.
        /// </summary>
        public static TimeSpan ClientKeepAliveTimeout = TimeSpan.FromMinutes(30);

		/// <summary>
		/// The name of the directory with settings.
		/// </summary>
		public static readonly string SettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.Create), "AIC");

		/// <summary>
		/// The path and filename of the AIC message demo file.
		/// </summary>
		public static readonly string AicMessageDemoPath = Path.Combine(SettingsDirectory, AicMessageDemoFile);

		/// <summary>
		/// The filename of the AIC message demo file.
		/// </summary>
		public const string AicMessageDemoFile = "demo.xml";

		/// <summary>
		/// The default encoding of the WAS XML.
		/// </summary>
		public static readonly Encoding WasEncoding = Encoding.GetEncoding(28605);

		/// <summary>
		/// The file containing the WHITE BMA data.
		/// </summary>
		public static readonly string WhiteBmaFile = Path.Combine(SettingsDirectory, "WhiteBma.white");

		/// <summary>
		/// The file containing the WHITE normal places data.
		/// </summary>
		public static readonly string WhiteNormalPlaceFile = Path.Combine(SettingsDirectory, "WhiteNormalPlace.white");


		/***   URLs   ***/

		/// <summary>
		/// The URL of the Google Maps Geocode API service.
		/// </summary>
		public const string GoogleMapsApiGeocodeUrl = @"http://maps.googleapis.com/maps/api/geocode/";

		/// <summary>
		/// The URL of the Google Maps Directions API service.
		/// </summary>
		public const string GoogleMapsApiDirectionsUrl = @"http://maps.googleapis.com/maps/api/directions/";

		/// <summary>
		/// The URL of the Google Static Maps service.
		/// </summary>
		public const string GoogleStaticMapsUrl = @"http://maps.google.com/maps/api/staticmap";

		/// <summary>
		/// The URL of the webservice that returns the audio data.
		/// </summary>
		public const string GoogleTranslateUrl = @"http://translate.google.com/translate_tts?tl=de&q=";

		/// <summary>
		/// The URL of Google Maps.
		/// </summary>
		public const string GoogleMapsUrl = @"http://maps.google.de/maps";

		/// <summary>
		/// The URL of an image that represents the fire brigade.
		/// </summary>
		public const string FireBrigadeMarkerUrl = @"http://www.alarm-info-center.at/images/route_origin_32.png";

		/// <summary>
		/// The URL of an image that represents the alarm location.
		/// </summary>
		public const string AlarmLocationMarkerUrl = @"http://www.alarm-info-center.at/images/route_destination_32.png";

		/// <summary>
		/// The URL of the schema definition file (XSD) of the Info-Center.
		/// </summary>
		public const string InfoCenterSchemaDefinitionUrl = @"http://www.alarm-info-center.at/services/infocenter.xsd";
	}
}
