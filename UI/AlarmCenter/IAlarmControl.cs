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
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// An interface that every alarm control must use.
	/// </summary>
	interface IAlarmControl: IDisposable
	{
		/// <summary>
		/// The displayed alarm.
		/// </summary>
		DependencyAlarm Alarm { get; }

		/// <summary>
		/// Updates the data of the alarm.
		/// </summary>
		/// <param name="alarm">The alarm with updated data.</param>
		/// <returns>True if the alarm has been updated, otherwise false.</returns>
		bool UpdateAlarmData(Alarm alarm);
	}
}
