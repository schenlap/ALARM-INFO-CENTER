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
using System.Linq;
using System.Windows;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This is a wrapper around the BaseAlarm class that adds Dependency properties.
	/// </summary>
	public class DependencyAlarm : DependencyObject
	{
		/// <summary>
		/// The Alarm instance that forms the base of the DependencyAlarm.
		/// </summary>
		public Alarm BaseAlarm { get; private set; }

		/// <summary>
		/// This constructor creates an empty BaseAlarm. It is necessary for binding to a WPF control.
		/// </summary>
		public DependencyAlarm()
		{
			BaseAlarm = new Alarm();
		}

		/// <summary>
		/// Creates a new DependencyAlarm.
		/// </summary>
		/// <param name="alarm">The alarm object to create the DependencyAlarm for.</param>
		/// <exception cref="NullReferenceException">Throws an exception if the argument is null.</exception>
		public DependencyAlarm(Alarm alarm)
		{
			if (alarm == null)
			{
				throw new NullReferenceException("The argument 'alarm' must not be null.");
			}
			BaseAlarm = alarm;

			// Coerce every dependency property, otherwise WPF won't show the actual value
			CoerceValue(IdProperty);
			CoerceValue(SubjectProperty);
			CoerceValue(LocationProperty);
			CoerceValue(LocationPropositionProperty);
			CoerceValue(UseLocationPropositionProperty);
			CoerceValue(AdditionalInformationProperty);
			CoerceValue(StartTimeProperty);
			CoerceValue(LaunchTimeProperty);
			CoerceValue(EndTimeProperty);
			CoerceValue(StatusProperty);
			CoerceValue(AlarmStationProperty);
			CoerceValue(AlarmLevelProperty);
			CoerceValue(CallerNameProperty);
			CoerceValue(CallerTelephoneNumberProperty);
			CoerceValue(SirenProgramProperty);
			CoerceValue(FireBrigadesProperty);
		}



		#region Id

		private static readonly DependencyPropertyKey IdPropertyKey = DependencyProperty.RegisterReadOnly("Id", typeof(string), typeof(DependencyAlarm), new PropertyMetadata(string.Empty, null, CoerceId));
		public static readonly DependencyProperty IdProperty = IdPropertyKey.DependencyProperty;

		/// <summary>
		/// The ID of the alarm.
		/// </summary>
		public string Id
		{
			get { return (string)GetValue(IdProperty); }
		}

		private static object CoerceId(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.Id;
		}

		#endregion

		#region Subject

		private static readonly DependencyPropertyKey SubjectPropertyKey = DependencyProperty.RegisterReadOnly("Subject", typeof(string), typeof(DependencyAlarm), new PropertyMetadata(string.Empty, OnSubjectChanged, CoerceSubject));
		public static readonly DependencyProperty SubjectProperty = SubjectPropertyKey.DependencyProperty;

		/// <summary>
		/// The subject of the alarm (Einsatzstichwort).
		/// </summary>
		public string Subject
		{
			get { return (string)GetValue(SubjectProperty); }
		}

		private static void OnSubjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.CoerceValue(IsFireProperty);
		}

		private static object CoerceSubject(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.Subject;
		}

		#endregion

		#region IsFire

		private static readonly DependencyPropertyKey IsFirePropertyKey = DependencyProperty.RegisterReadOnly("IsFire", typeof(bool), typeof(DependencyAlarm), new PropertyMetadata(false, null, CoerceIsFire));
		public static readonly DependencyProperty IsFireProperty = IsFirePropertyKey.DependencyProperty;

		/// <summary>
		/// Indicates whether the alarm is of category "fire".
		/// </summary>
		public bool IsFire
		{
			get { return (bool)GetValue(IsFireProperty); }
		}

		private static object CoerceIsFire(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.IsFire;
		}

		#endregion

		#region Location

		private static readonly DependencyPropertyKey LocationPropertyKey = DependencyProperty.RegisterReadOnly("Location", typeof(string), typeof(DependencyAlarm), new PropertyMetadata(string.Empty, null, CoerceLocation));
		public static readonly DependencyProperty LocationProperty = LocationPropertyKey.DependencyProperty;

		/// <summary>
		///  The location of the alarm.
		/// </summary>
		public string Location
		{
			get { return (string)GetValue(LocationProperty); }
		}

		private static object CoerceLocation(DependencyObject d, object value)
		{
			DependencyAlarm alarm = (DependencyAlarm)d;
			if (!string.IsNullOrWhiteSpace(alarm.BaseAlarm.LocationProposition))
			{
				return "(" + alarm.BaseAlarm.Location + ")";
			}
			return alarm.BaseAlarm.Location;
		}

		#endregion

		#region LocationProposition

		private static readonly DependencyPropertyKey LocationPropositionPropertyKey = DependencyProperty.RegisterReadOnly("LocationProposition", typeof(string), typeof(DependencyAlarm), new PropertyMetadata(string.Empty, OnLocationPropositionChanged, CoerceLocationProposition));
		public static readonly DependencyProperty LocationPropositionProperty = LocationPropositionPropertyKey.DependencyProperty;

		/// <summary>
		///  The location of the alarm.
		/// </summary>
		public string LocationProposition
		{
			get { return (string)GetValue(LocationPropositionProperty); }
		}

		private static void OnLocationPropositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.CoerceValue(LocationProperty);
			d.CoerceValue(UseLocationPropositionProperty);
		}

		private static object CoerceLocationProposition(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.LocationProposition;
		}

		#endregion

		#region UseLocationProposition

		private static readonly DependencyPropertyKey UseLocationPropositionPropertyKey = DependencyProperty.RegisterReadOnly("UseLocationProposition", typeof(bool), typeof(DependencyAlarm), new PropertyMetadata(false, null, CoerceUseLocationProposition));
		public static readonly DependencyProperty UseLocationPropositionProperty = UseLocationPropositionPropertyKey.DependencyProperty;

		/// <summary>
		/// Indicates whether a location proposition is set.
		/// </summary>
		public bool UseLocationProposition
		{
			get { return (bool)GetValue(UseLocationPropositionProperty); }
		}

		private static object CoerceUseLocationProposition(DependencyObject d, object value)
		{
			return !string.IsNullOrWhiteSpace(((DependencyAlarm)d).BaseAlarm.LocationProposition);
		}

		#endregion

		#region AdditionalInformation

		private static readonly DependencyPropertyKey AdditionalInformationPropertyKey = DependencyProperty.RegisterReadOnly("AdditionalInformation", typeof(string), typeof(DependencyAlarm), new PropertyMetadata(string.Empty, null, CoerceAdditionalInformation));
		public static readonly DependencyProperty AdditionalInformationProperty = AdditionalInformationPropertyKey.DependencyProperty;

		/// <summary>
		/// Additional information concerning the alarm.
		/// </summary>
		public string AdditionalInformation
		{
			get { return (string)GetValue(AdditionalInformationProperty); }
		}

		private static object CoerceAdditionalInformation(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.AdditionalInformation;
		}

		#endregion

		#region StartTime

		private static readonly DependencyPropertyKey StartTimePropertyKey = DependencyProperty.RegisterReadOnly("StartTime", typeof(DateTime), typeof(DependencyAlarm), new PropertyMetadata(DateTime.MinValue, null, CoerceStartTime));
		public static DependencyProperty StartTimeProperty = StartTimePropertyKey.DependencyProperty;

		/// <summary>
		/// The date and time when the alarm starts.
		/// </summary>
		public DateTime StartTime
		{
			get { return (DateTime)GetValue(StartTimeProperty); }
		}

		private static object CoerceStartTime(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.StartTime;
		}

		#endregion

		#region LaunchTime

		private static readonly DependencyPropertyKey LaunchTimePropertyKey = DependencyProperty.RegisterReadOnly("LaunchTime", typeof(DateTime), typeof(DependencyAlarm), new PropertyMetadata(DateTime.MinValue, OnLaunchTimeChanged, CoerceLaunchTime));
		public static readonly DependencyProperty LaunchTimeProperty = LaunchTimePropertyKey.DependencyProperty;

		/// <summary>
		/// The time when the alarm is committed (F5 is pressed).
		/// </summary>
		public DateTime LaunchTime
		{
			get { return (DateTime)GetValue(LaunchTimeProperty); }
		}

		private static void OnLaunchTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.CoerceValue(FurtherInformationProperty);
		}

		private static object CoerceLaunchTime(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.LaunchTime;
		}

		#endregion

		#region EndTime

		private static readonly DependencyPropertyKey EndTimePropertyKey = DependencyProperty.RegisterReadOnly("EndTime", typeof(DateTime), typeof(DependencyAlarm), new PropertyMetadata(DateTime.MinValue, null, CoerceEndTime));
		public static readonly DependencyProperty EndTimeProperty = EndTimePropertyKey.DependencyProperty;

		/// <summary>
		/// The time when the alarm is finished.
		/// </summary>
		public DateTime EndTime
		{
			get { return (DateTime)GetValue(EndTimeProperty); }
		}

		private static object CoerceEndTime(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.EndTime;
		}

		#endregion

		#region Status

		private static readonly DependencyPropertyKey StatusPropertyKey = DependencyProperty.RegisterReadOnly("Status", typeof(int), typeof(DependencyAlarm), new PropertyMetadata(0, OnStatusChanged, CoerceStatus));
		public static readonly DependencyProperty StatusProperty = StatusPropertyKey.DependencyProperty;

		/// <summary>
		/// The status of the alarm. 0 means "not committed". 1 stands for "committed".
		/// </summary>
		public int Status
		{
			get { return (int)GetValue(StatusProperty); }
		}

		private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.CoerceValue(FurtherInformationProperty);
		}

		private static object CoerceStatus(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.Status;
		}

		#endregion

		#region AlarmStation

		private static readonly DependencyPropertyKey AlarmStationPropertyKey = DependencyProperty.RegisterReadOnly("AlarmStation", typeof(string), typeof(DependencyAlarm), new PropertyMetadata(string.Empty, OnAlarmStationChanged, CoerceAlarmStation));
		public static readonly DependencyProperty AlarmStationProperty = AlarmStationPropertyKey.DependencyProperty;

		/// <summary>
		/// The institute that dispatches the alarm.
		/// </summary>
		public string AlarmStation
		{
			get { return (string)GetValue(AlarmStationProperty); }
		}

		private static void OnAlarmStationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.CoerceValue(FurtherInformationProperty);
		}

		private static object CoerceAlarmStation(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.AlarmStation;
		}

		#endregion

		#region AlarmLevel

		private static readonly DependencyPropertyKey AlarmLevelPropertyKey = DependencyProperty.RegisterReadOnly("AlarmLevel", typeof(int), typeof(DependencyAlarm), new PropertyMetadata(0, OnAlarmLevelChanged, CoerceAlarmLevel));
		public static readonly DependencyProperty AlarmLevelProperty = AlarmLevelPropertyKey.DependencyProperty;

		/// <summary>
		/// The level of the alarm (Alarmstufe).
		/// </summary>
		public int AlarmLevel
		{
			get { return (int)GetValue(AlarmLevelProperty); }
		}

		private static void OnAlarmLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.CoerceValue(FurtherInformationProperty);
		}

		private static object CoerceAlarmLevel(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.AlarmLevel;
		}

		#endregion

		#region CallerName

		private static readonly DependencyPropertyKey CallerNamePropertyKey = DependencyProperty.RegisterReadOnly("CallerName", typeof(string), typeof(DependencyAlarm), new PropertyMetadata(string.Empty, OnCallerNameChanged, CoerceCallerName));
		public static readonly DependencyProperty CallerNameProperty = CallerNamePropertyKey.DependencyProperty;

		/// <summary>
		/// The name of the caller.
		/// </summary>
		public string CallerName
		{
			get { return (string)GetValue(CallerNameProperty); }
		}

		private static void OnCallerNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.CoerceValue(CallerProperty);
		}

		private static object CoerceCallerName(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.CallerName;
		}

		#endregion

		#region CallerTelephoneNumber

		private static readonly DependencyPropertyKey CallerTelephoneNumberPropertyKey = DependencyProperty.RegisterReadOnly("CallerTelephoneNumber", typeof(string), typeof(DependencyAlarm), new PropertyMetadata(string.Empty, OnCallerTelephoneNumberChanged, CoerceCallerTelephoneNumber));
		public static readonly DependencyProperty CallerTelephoneNumberProperty = CallerTelephoneNumberPropertyKey.DependencyProperty;

		/// <summary>
		/// The telephone number of the caller.
		/// </summary>
		public string CallerTelephoneNumber
		{
			get { return (string)GetValue(CallerTelephoneNumberProperty); }
		}

		private static void OnCallerTelephoneNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.CoerceValue(CallerProperty);
		}

		private static object CoerceCallerTelephoneNumber(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.CallerTelephoneNumber;
		}

		#endregion

		#region Caller

		private static readonly DependencyPropertyKey CallerKey = DependencyProperty.RegisterReadOnly("Caller", typeof(string), typeof(DependencyAlarm), new PropertyMetadata(string.Empty, OnCallerChanged, CoerceCaller));
		public static readonly DependencyProperty CallerProperty = CallerKey.DependencyProperty;

		/// <summary>
		/// The name and the telephone number of the caller.
		/// </summary>
		public string Caller
		{
			get { return (string)GetValue(CallerProperty); }
		}

		private static void OnCallerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.CoerceValue(FurtherInformationProperty);
		}

		private static object CoerceCaller(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.Caller;
		}

		#endregion

		#region SirenProgram

		private static readonly DependencyPropertyKey SirenProgramPropertyKey = DependencyProperty.RegisterReadOnly("SirenProgram", typeof(string), typeof(DependencyAlarm), new PropertyMetadata(string.Empty, OnSirenProgramChanged, CoerceSirenProgram));
		public static readonly DependencyProperty SirenProgramProperty = SirenProgramPropertyKey.DependencyProperty;

		/// <summary>
		/// The siren program that has been executed.
		/// </summary>
		public string SirenProgram
		{
			get { return (string)GetValue(SirenProgramProperty); }
		}

		private static void OnSirenProgramChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.CoerceValue(FurtherInformationProperty);
		}

		private static object CoerceSirenProgram(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.SirenProgram;
		}

		#endregion

		#region FireBrigades

		private static readonly DependencyPropertyKey FireBrigadesPropertyKey = DependencyProperty.RegisterReadOnly("FireBrigades", typeof(List<string>), typeof(DependencyAlarm), new PropertyMetadata(null, FireBrigadesChanged, CoerceFireBrigades));
		public static readonly DependencyProperty FireBrigadesProperty = FireBrigadesPropertyKey.DependencyProperty;

		/// <summary>
		/// All the fire brigades that participate the alarm.
		/// </summary>
		public List<string> FireBrigades
		{
			get { return (List<string>)GetValue(FireBrigadesProperty); }
		}

		private static void FireBrigadesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.CoerceValue(FurtherInformationProperty);
		}

		private static object CoerceFireBrigades(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.FireBrigades;
		}

		#endregion

		#region FurtherInformation

		private static readonly DependencyPropertyKey FurtherInformationKey = DependencyProperty.RegisterReadOnly("FurtherInformation", typeof(string), typeof(DependencyAlarm), new PropertyMetadata(string.Empty, null, CoerceFurtherInformation));
		public static readonly DependencyProperty FurtherInformationProperty = FurtherInformationKey.DependencyProperty;

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
		public string FurtherInformation
		{
			get { return (string)GetValue(FurtherInformationProperty); }
		}

		private static object CoerceFurtherInformation(DependencyObject d, object value)
		{
			return ((DependencyAlarm)d).BaseAlarm.FurtherInformation;
		}

		#endregion

		

		/// <summary>
		/// Updates each field of the current alarm if the provided alarm has the same ID.
		/// </summary>
		/// <param name="alarm">An alarm with new data.</param>
		/// <returns>Null if the ids are different. Empty if nothing has been updated. Otherwise the updated properties.</returns>
		public List<DependencyProperty> UpdateData(Alarm alarm)
		{
			if (BaseAlarm.Id != alarm.Id)
			{
				return null;
			}

			List<DependencyProperty> updatedProperties = new List<DependencyProperty>();

			if (BaseAlarm.Subject != alarm.Subject)
			{
				BaseAlarm.Subject = alarm.Subject;
				CoerceValue(SubjectProperty);
				updatedProperties.Add(SubjectProperty);
			}

			if (BaseAlarm.Location != alarm.Location)
			{
				BaseAlarm.Location = alarm.Location;
				CoerceValue(LocationProperty);
				updatedProperties.Add(LocationProperty);
			}

			if (BaseAlarm.LocationProposition != alarm.LocationProposition)
			{
				BaseAlarm.LocationProposition = alarm.LocationProposition;
				CoerceValue(LocationPropositionProperty);
				updatedProperties.Add(LocationPropositionProperty);
			}

			if (BaseAlarm.AdditionalInformation != alarm.AdditionalInformation)
			{
				BaseAlarm.AdditionalInformation = alarm.AdditionalInformation;
				CoerceValue(AdditionalInformationProperty);
				updatedProperties.Add(AdditionalInformationProperty);
			}

			if (BaseAlarm.StartTime != alarm.StartTime)
			{
				BaseAlarm.StartTime = alarm.StartTime;
				CoerceValue(StartTimeProperty);
				updatedProperties.Add(StartTimeProperty);
			}

			if (BaseAlarm.LaunchTime != alarm.LaunchTime)
			{
				BaseAlarm.LaunchTime = alarm.LaunchTime;
				CoerceValue(LaunchTimeProperty);
				updatedProperties.Add(LaunchTimeProperty);
			}

			if (BaseAlarm.EndTime != alarm.EndTime)
			{
				BaseAlarm.EndTime = alarm.EndTime;
				CoerceValue(EndTimeProperty);
				updatedProperties.Add(EndTimeProperty);
			}

			if (BaseAlarm.Status != alarm.Status)
			{
				BaseAlarm.Status = alarm.Status;
				CoerceValue(StatusProperty);
				updatedProperties.Add(StatusProperty);
			}

			if (BaseAlarm.AlarmStation != alarm.AlarmStation)
			{
				BaseAlarm.AlarmStation = alarm.AlarmStation;
				CoerceValue(AlarmStationProperty);
				updatedProperties.Add(AlarmStationProperty);
			}

			if (BaseAlarm.AlarmLevel != alarm.AlarmLevel)
			{
				BaseAlarm.AlarmLevel = alarm.AlarmLevel;
				CoerceValue(AlarmLevelProperty);
				updatedProperties.Add(AlarmLevelProperty);
			}

			if (BaseAlarm.CallerName != alarm.CallerName)
			{
				BaseAlarm.CallerName = alarm.CallerName;
				CoerceValue(CallerNameProperty);
				updatedProperties.Add(CallerNameProperty);
			}

			if (BaseAlarm.CallerTelephoneNumber != alarm.CallerTelephoneNumber)
			{
				BaseAlarm.CallerTelephoneNumber = alarm.CallerTelephoneNumber;
				CoerceValue(CallerTelephoneNumberProperty);
				updatedProperties.Add(CallerTelephoneNumberProperty);
			}

			if (BaseAlarm.SirenProgram != alarm.SirenProgram)
			{
				BaseAlarm.SirenProgram = alarm.SirenProgram;
				CoerceValue(SirenProgramProperty);
				updatedProperties.Add(SirenProgramProperty);
			}

			if (!BaseAlarm.FireBrigades.SequenceEqual(alarm.FireBrigades))
			{
				BaseAlarm.FireBrigades = alarm.FireBrigades;
				CoerceValue(FireBrigadesProperty);
				updatedProperties.Add(FireBrigadesProperty);
			}

			return updatedProperties;
		}
	}
}
