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
using System.Xml.Serialization;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class describes an alarm for firefighters.
	/// </summary>
	[XmlType("Alarm")]
	public class Alarm
	{
		// This must be a static variable otherwise it will consume a lot of memory
		private static readonly XmlSerializer serializer = new XmlSerializer(typeof(Alarm));

		/// <summary>
		/// The name of the fire brigade that should be shown on the first position in the list of fire brigades.
		/// </summary>
		public static string MainFireBrigade;


		/***   Properties (do not modify the order!)   ***/

		/// <summary>
		/// The ID of the alarm.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// The subject of the alarm (Einsatzstichwort).
		/// </summary>
		public string Subject { get; set; }

		/// <summary>
		/// Indicates whether the alarm is of type "fire".
		/// </summary>
		[XmlIgnore]
		public bool IsFire
		{
			get
			{
				return Subject != null && Subject.ToLower().Contains("brand");
			}
		}

		/// <summary>
		///  The location of the alarm.
		/// </summary>
		public string Location { get; set; }

		/// <summary>
		/// The proposed location of the alarm. It is derived from the AdditionalInformation property.
		/// </summary>
		public string LocationProposition { get; set; }

		/// <summary>
		/// Get the location that should be used for the map. Primarily, the location proposition is used.
		/// </summary>
		[XmlIgnore]
		public string MapLocation
		{
			get { return string.IsNullOrWhiteSpace(LocationProposition) ? Location : LocationProposition; }
		}

		/// <summary>
		/// Additional information concerning the alarm.
		/// </summary>
		public string AdditionalInformation { get; set; }

		/// <summary>
		/// The date and time when the alarm starts.
		/// </summary>
		public DateTime StartTime { get; set; }

		/// <summary>
		/// The time when the alarm is committed (F5 is pressed).
		/// </summary>
		public DateTime LaunchTime { get; set; }

		/// <summary>
		/// The time when the alarm is finished.
		/// </summary>
		public DateTime EndTime { get; set; }

		/// <summary>
		/// The status of the alarm. 0 means "not committed". 1 stands for "committed".
		/// </summary>
		public int Status { get; set; }

		/// <summary>
		/// The institute that dispatches the alarm.
		/// </summary>
		public string AlarmStation { get; set; }

		/// <summary>
		/// The level of the alarm (Alarmstufe).
		/// </summary>
		public int AlarmLevel { get; set; }

		/// <summary>
		/// The name of the caller.
		/// </summary>
		public string CallerName { get; set; }

		/// <summary>
		/// The telephone number of the caller.
		/// </summary>
		public string CallerTelephoneNumber { get; set; }

        /// <summary>
        /// The choosen program of the siren.
        /// </summary>
        public string SirenProgram { get; set; }

		/// <summary>
		/// Callername and callernumber in format "Max Muster (9875 / 123456789)".
		/// </summary>
		[XmlIgnore]
		public string Caller
		{
			get
			{
				string caller = string.Empty;

				// The name of the caller
				if (!string.IsNullOrWhiteSpace(CallerName))
				{
					caller = CallerName;
				}

				// The telephone number of the caller
				if (!string.IsNullOrWhiteSpace(CallerTelephoneNumber))
				{
					bool hasCallerName = caller.Length > 0;
					if (hasCallerName)
					{
						caller += " (";
					}
					caller += CallerTelephoneNumber;
					if (hasCallerName)
					{
						caller += ")";
					}
				}

				return caller;
			}
		}

		/// <summary>
		/// All the fire brigades that participate the alarm. This property never returns null.
		/// </summary>
		[XmlArray("FireBrigades")]
		[XmlArrayItem("FireBrigade")]
		public List<string> FireBrigades
		{
			get { return mFireBrigades ?? (mFireBrigades = new List<string>()); }
			set { mFireBrigades = value; }
		}
		private List<string> mFireBrigades;
		
		/// <summary>
		/// Further information that is not essential for the alarm.
		/// It is a multiline string that consists of following components:
		///  - AlarmStation
		///  - SirenProgram
		///  - AlarmLevel
		///  - FireBrigades
		///  - Caller
		///  - Status (if it is 1)
		/// </summary>
		[XmlIgnore]
		public string FurtherInformation
		{
			get
			{
				string info = string.Empty;

				// AlarmStation
				if (!string.IsNullOrWhiteSpace(AlarmStation))
				{
					info += "Alarmierende Stelle: " + AlarmStation + Environment.NewLine;
				}

				// SirenProgram
				if (!string.IsNullOrWhiteSpace(SirenProgram))
				{
					info += "Sirenenprogramm: " + SirenProgram + Environment.NewLine;
				}

				// AlarmLevel
				info += "Alarmstufe: " + AlarmLevel + Environment.NewLine;

				// FireBrigades (move the home fire brigade to the first position)
				if (FireBrigades.Count > 0 && !string.IsNullOrEmpty(MainFireBrigade))
				{
					int index = FireBrigades.IndexOf(MainFireBrigade.ToUpperInvariant());
					if (index > 0)
					{
						FireBrigades.RemoveAt(index);
						FireBrigades.Insert(0, MainFireBrigade.ToUpperInvariant());
					}
					info += "Alarmierte Feuerwehren (" + FireBrigades.Count + "): " +
						FireBrigades.Aggregate((x, y) => x + ", " + y) + Environment.NewLine + Environment.NewLine;
				}

				// Caller
				info += "Anrufer: " + Caller;

				// Status
				if (Status == 1)
				{
					info += Environment.NewLine + "F5 gedrückt um: " + LaunchTime.ToLongTimeString();
				}
				return info;
			}
		}



		/***   Methods   ***/

		/// <summary>
		/// Checks if two Alarm objects are equal.
		/// </summary>
		/// <param name="other">Another alarm instance.</param>
		/// <returns>True if all values are the same.</returns>
		public bool Equals(Alarm other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return
				string.Equals(Id, other.Id) &&
				string.Equals(Subject, other.Subject) &&
				IsFire == other.IsFire &&
				string.Equals(Location, other.Location) &&
				string.Equals(LocationProposition, other.LocationProposition) &&
				string.Equals(MapLocation, other.MapLocation) &&
				string.Equals(AdditionalInformation, other.AdditionalInformation) &&
				StartTime == other.StartTime &&
				LaunchTime == other.LaunchTime &&
				EndTime == other.EndTime &&
				Status == other.Status &&
				string.Equals(AlarmStation, other.AlarmStation) &&
				AlarmLevel == other.AlarmLevel &&
				string.Equals(CallerName, other.CallerName) &&
				string.Equals(CallerTelephoneNumber, other.CallerTelephoneNumber) &&
				string.Equals(SirenProgram, other.SirenProgram) &&
				string.Equals(Caller, other.Caller) &&
				string.Equals(FurtherInformation, other.FurtherInformation) &&
				FireBrigades.SequenceEqual(other.FireBrigades);
		}

		/// <summary>
		/// Checks if two objects are equal.
		/// </summary>
		/// <param name="obj">Another object.</param>
		/// <returns>True if all values are the same.</returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((Alarm)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (Id != null ? Id.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Subject != null ? Subject.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Location != null ? Location.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (LocationProposition != null ? LocationProposition.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (AdditionalInformation != null ? AdditionalInformation.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ StartTime.GetHashCode();
				hashCode = (hashCode * 397) ^ LaunchTime.GetHashCode();
				hashCode = (hashCode * 397) ^ EndTime.GetHashCode();
				hashCode = (hashCode * 397) ^ Status;
				hashCode = (hashCode * 397) ^ (AlarmStation != null ? AlarmStation.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ AlarmLevel;
				hashCode = (hashCode * 397) ^ (CallerName != null ? CallerName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (CallerTelephoneNumber != null ? CallerTelephoneNumber.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (SirenProgram != null ? SirenProgram.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ FireBrigades.GetHashCode();
				return hashCode;
			}
		}

		/// <summary>
		/// Creates a number of Alarm instances.
		/// </summary>
		/// <param name="count">The number of Alarm instances to create.</param>
		/// <returns>A list of Alarms. The list is empty if count is lower than one.</returns>
		/// <remarks>This method is intended for the usage in test cases.</remarks>
		public static List<Alarm> Create(int count)
		{
			var alarms = new List<Alarm>();
			for (int i = 1; i <= count; i++)
			{
				var alarm = new Alarm
				{
					Id = "ID" + i,
					Subject = "EINSATZ " + i,
					Location = "MARCHTRENK MARKUSTRAßE 4 " + i,
					LocationProposition = "MARCHTRENK MARKUSWEG 4",
					AdditionalInformation = "More infos " + i,
					StartTime = DateTime.Now,
					LaunchTime = DateTime.Now.AddMinutes(2),
					EndTime = DateTime.Now.AddMinutes(48),
					Status = i % 2,
					AlarmStation = "BWST WELS",
					AlarmLevel = 1,
					CallerName = "HERMANN WOSWASI",
					CallerTelephoneNumber = "07243 / 58112",
                    SirenProgram = "Feuer",
					FireBrigades = new List<string> { "MARCHTRENK", "KAPPERN" }
				};
				alarms.Add(alarm);
			}
			return alarms;
		}

		/// <summary>
		/// Creates a list of alarms from a WasObject.
		/// </summary>
		/// <param name="wasObject">A WasObject.</param>
		/// <returns>A list of alarms. Never null.</returns>
		public static List<Alarm> Create(WasObject wasObject)
		{
			var alarms = new List<Alarm>();

			if (wasObject != null && wasObject.Alarms != null && wasObject.Alarms.Count > 0)
			{
				alarms.AddRange(wasObject.Alarms.Select(Create));
			}

			return alarms;
		}

		/// <summary>
		/// Creates an alarm from a WasAlarm.
		/// </summary>
		/// <param name="wasAlarm">A WasAlarm.</param>
		/// <returns>An alarm.</returns>
		public static Alarm Create(WasAlarm wasAlarm)
		{
			var alarm = new Alarm
			                  	{
			                  		Id = wasAlarm.Id,
			                  		Subject = wasAlarm.Subject,
			                  		Location = wasAlarm.Location,
			                  		LocationProposition = string.Empty,
			                  		AdditionalInformation = wasAlarm.AdditionalInformation,
			                  		StartTime = wasAlarm.StartTime,
			                  		LaunchTime = wasAlarm.LaunchTime,
			                  		EndTime = wasAlarm.EndTime,
			                  		Status = wasAlarm.Status,
			                  		AlarmStation = wasAlarm.AlarmStation,
			                  		AlarmLevel = wasAlarm.AlarmLevel,
			                  		CallerName = wasAlarm.CallerName,
			                  		CallerTelephoneNumber = wasAlarm.CallerTelephoneNumber,
			                  		FireBrigades = new List<string>(),
                                    SirenProgram = wasAlarm.SirenProgram
			                  	};

			if (wasAlarm.FireBrigades != null && wasAlarm.FireBrigades.Count > 0)
			{
				foreach (string fireBrigade in wasAlarm.FireBrigades.Where(fireBrigade => fireBrigade != null))
				{
					alarm.FireBrigades.Add(fireBrigade);
				}
			}

			return alarm;
		}

		/// <summary>
		/// Serializes the alarm to a string.
		/// </summary>
		/// <returns>An xml-string containing the alarm.</returns>
		public string Serialize()
		{
			using (var sw = new StringWriter())
			{
				serializer.Serialize(sw, this);
				return sw.ToString();
			}
		}
	}
}
