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
using System.Xml.Serialization;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// The structure of an alarm that is sent by the WAS.
	/// </summary>
	[XmlType("order")]
	public class WasAlarm : IEquatable<WasAlarm>, IComparable<WasAlarm>
	{
		/// <summary>
		/// The DateTime format that is used by the WAS.
		/// </summary>
		public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

		public const string ReceiveStatusText = "Alarmiert";
		public const string LaunchStatusText = "Ausgerückt";

		public WasAlarm()
		{
			FireBrigades = new List<string>();
			ReceiveTime = DateTime.Now.ToString(DateTimeFormat);
		}


		[XmlElement("key")]
		public string Key { get; set; }

		[XmlElement("origin")]
		public string AlarmStation { get; set; }

		[XmlElement("receive-tad")]
		public string ReceiveTime { get; set; }

		[XmlElement("operation-id")]
		public string Id { get; set; }

		[XmlElement("level")]
		public int AlarmLevel { get; set; }

		[XmlElement("name")]
		public string CallerName { get; set; }

		[XmlElement("operation-name")]
		public string Subject { get; set; }

		[XmlElement("caller")]
		public string CallerTelephoneNumber { get; set; }

		[XmlElement("location")]
		public string Location { get; set; }

		[XmlElement("info")]
		public string AdditionalInformation { get; set; }

		[XmlElement("status")]
		public string StatusText { get; set; }

		[XmlElement("watch-out-tad")]
		public string WatchOutTime { get; set; }

		[XmlElement("finished-tad")]
		public string FinishedTime { get; set; }

		[XmlElement("program")]
		public string SirenProgram { get; set; }

		[XmlArray("destination-list")]
		[XmlArrayItem("destination")]
		public List<string> FireBrigades { get; set; }

		[XmlAttribute("index")]
		public int Index { get; set; }



		[XmlIgnore]
		public DateTime StartTime
		{
			get { return ReceiveTime.TryParseDateTime(); }
		}

		[XmlIgnore]
		public DateTime LaunchTime
		{
			get { return WatchOutTime.TryParseDateTime(); }
		}

		[XmlIgnore]
		public DateTime EndTime
		{
			get { return FinishedTime.TryParseDateTime(); }
		}

		[XmlIgnore]
		public int Status
		{
			get { return StatusText.ToLower() == ReceiveStatusText.ToLower() ? 0 : 1; }
		}

		/// <summary>
		/// Compares the object with another WasAlarm object by Id.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(WasAlarm other)
		{
			if (Id != null && other.Id != null)
			{
				return string.Compare(Id, other.Id, StringComparison.InvariantCulture);
			}
			return 0;
		}

		public override string ToString()
		{
			return Id == null ? string.Empty : StatusText + " - " + Id + ": " + Location;
		}

		/// <summary>
		/// Two WasAlarm objects are equal if each field has the same value.
		/// </summary>
		/// <param name="other">The other WasAlarm object.</param>
		/// <returns>True, if all each field has the same value.</returns>
		public bool Equals(WasAlarm other)
		{
			if (ReferenceEquals(other, null)) return false;

			// Check the list of fire brigades
			bool fireBrigades1IsNull = ReferenceEquals(FireBrigades, null);
			bool fireBrigades2IsNull = ReferenceEquals(other.FireBrigades, null);
			bool fireBrigadesEqual = fireBrigades1IsNull && fireBrigades2IsNull;
			if (!fireBrigades1IsNull && !fireBrigades2IsNull)
			{
				fireBrigadesEqual = FireBrigades.SequenceEqual(other.FireBrigades);
			}

			return 
				fireBrigadesEqual &&
				string.Equals(Key, other.Key) && 
				string.Equals(AlarmStation, other.AlarmStation) &&
				string.Equals(ReceiveTime, other.ReceiveTime) && 
				string.Equals(Id, other.Id) && 
				AlarmLevel == other.AlarmLevel && 
				string.Equals(CallerName, other.CallerName) && 
				string.Equals(CallerTelephoneNumber, other.CallerTelephoneNumber) && 
				string.Equals(Subject, other.Subject) && 
				string.Equals(Location, other.Location) && 
				string.Equals(AdditionalInformation, other.AdditionalInformation) && 
				string.Equals(StatusText, other.StatusText) && 
				string.Equals(WatchOutTime, other.WatchOutTime) && 
				string.Equals(SirenProgram, other.SirenProgram) && 
				string.Equals(FinishedTime, other.FinishedTime) && 
				Index == other.Index;
		}

		/// <summary>
		/// Two WasAlarm objects are equal if each field has the same value.
		/// </summary>
		/// <param name="obj">Another object to compare.</param>
		/// <returns>True, if each field has the same value.</returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((WasAlarm)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Key != null ? Key.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ (AlarmStation != null ? AlarmStation.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (ReceiveTime != null ? ReceiveTime.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ AlarmLevel;
				hashCode = (hashCode * 397) ^ (CallerName != null ? CallerName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (CallerTelephoneNumber != null ? CallerTelephoneNumber.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Subject != null ? Subject.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Location != null ? Location.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (AdditionalInformation != null ? AdditionalInformation.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (StatusText != null ? StatusText.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (WatchOutTime != null ? WatchOutTime.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (SirenProgram != null ? SirenProgram.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (FinishedTime != null ? FinishedTime.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (FireBrigades != null ? FireBrigades.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ Index;
				return hashCode;
			}
		}
	}
}
