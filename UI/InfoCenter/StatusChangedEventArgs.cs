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

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// Provides data for the StatusChanged event.
	/// </summary>
	public class StatusChangedEventArgs : EventArgs
	{
		/// <summary>
		/// The status of the Info-Center.
		/// </summary>
		public Status Status;

		/// <summary>
		/// Initializes a new instance of the StatusChangedEventArgs class.
		/// </summary>
		/// <param name="status">The current status.</param>
		public StatusChangedEventArgs(Status status)
		{
			Status = status;
		}
	}
}
