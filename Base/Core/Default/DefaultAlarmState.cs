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
using System.Linq;
using System.Collections.Generic;

namespace AlarmInfoCenter.Base
{
    /// <summary>
    /// Keeps the internal list of transformed alarms.
    /// </summary>
    public class DefaultAlarmState : IAlarmState
    {
		/// <summary>
		/// Current logger instance.
		/// </summary>
		private readonly ILogger mLogger;

		private List<Alarm> mAlarms = new List<Alarm>();
        /// <summary>
        /// List of all (already parsed) alarms.
        /// </summary>
        public List<Alarm> Alarms 
		{
			get { return this.mAlarms; }
		}

        /// <summary>
        /// Contains already parsed alarms (alarm locations). Key: AlarmId, Value: White parsed location (NULL if no location was found).
        /// </summary>
		private Dictionary<string, string> mWhiteParsedAlarms = new Dictionary<string, string>();



        public DefaultAlarmState(ILogger logger)
        {
			this.mLogger = logger;
        }


        /// <summary>
        /// Updates the (internal) list of alarms using the current WAS-Object.
        /// </summary>
        /// <param name="wasObject">The current object from the WAS.</param>
        public void UpdateAlarmObject(WasObject wasObject)
        {
            this.mAlarms = wasObject.Alarms.
                Select(alarm => Transformate(alarm)).
                ToList();

            // Update the WHITE-parsed alarm list
            Dictionary<string, string> tempParsedAlarms = new Dictionary<string, string>();

            foreach (Alarm alarm in this.Alarms)
            {
                if (this.mWhiteParsedAlarms.ContainsKey(alarm.Id))
                {
                    tempParsedAlarms.Add(alarm.Id, this.mWhiteParsedAlarms[alarm.Id]);
                }
            }

            this.mWhiteParsedAlarms = tempParsedAlarms;
        }

        /// <summary>
        /// Transforms an WAS-Alarm (WAS-specific) to an internal alarm object.
        /// Uses WHITE if it is specified (in the AIC-Settings).
        /// </summary>
        /// <param name="wasAlarm">The WAS-Alarm object.</param>
        /// <returns>The transformed alarm.</returns>
        private Alarm Transformate(WasAlarm wasAlarm)
        {
            Alarm alarm = Alarm.Create(wasAlarm);

            // Remove the BMA identifier at the end
            if (string.Equals(alarm.Subject, Constants.BMAType, StringComparison.OrdinalIgnoreCase) && alarm.Location.Contains(Constants.BMAIdentificationCharacter))
            {
				alarm.Location = alarm.Location.Substring(0, alarm.Location.IndexOf(Constants.BMAIdentificationCharacter)).Trim();
            }

            return alarm;

			/*if (!AicSettings.Global.UseWhite)
            {
                return alarm;
            }

            // Evaluate if there is already a valid location
			bool hasValidLocation = !alarm.Location.ToLower().Contains(AicServerSettings.NoLocationPattern.ToLower());

            if (hasValidLocation)
            {
                return alarm;
            }

			// Already parsed strings (alarm update)
            if (mWhiteParsedAlarms.ContainsKey(alarm.Id))
            {
                alarm.LocationProposition = this.mWhiteParsedAlarms[alarm.Id];

                return alarm;
            }

            string locationProposition = null;

            try
            {
                locationProposition = WhiteUtilities.FindPlace(alarm);

                if (locationProposition == null)
                {
                    this.mLogger.LogInformation("White hasn't found the place in the info text", InformationType.AlarmState_WhiteFailed);
                }

                else
                {
                    alarm.LocationProposition = locationProposition;
                    this.mLogger.LogInformation(string.Format("White found the place: {0}", locationProposition), InformationType.AlarmState_WhiteSucceded);
                }

                this.mWhiteParsedAlarms.Add(alarm.Id, locationProposition);
            }

            catch (Exception ex)
            {
				this.mLogger.LogError("(AlarmState/Transformate/Exception)", ErrorType.Undefined, ex);
            }

            return alarm;*/
        }
    }
}
