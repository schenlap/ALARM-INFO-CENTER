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

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// Logs the content to a normal console. This logger should
	/// be used for debugging and testing purpose only.
	/// </summary>
	public class CallbackLogger : AbstractLogger
	{
		/// <summary>
		/// All logged informations.
		/// </summary>
		public readonly List<InformationType> LoggedInformations = new List<InformationType>();

		/// <summary>
		/// All logged warnings.
		/// </summary>
		public readonly List<WarningType> LoggedWarnings = new List<WarningType>();

		/// <summary>
		/// All logged errors.
		/// </summary>
		public readonly List<ErrorType> LoggedErrors = new List<ErrorType>();


		public void ClearLogs()
		{
			LoggedErrors.Clear();
			LoggedInformations.Clear();
			LoggedWarnings.Clear();
		}

		/// <summary>
		/// Writes an entry of type 'Error' to the log.
		/// If the exception is not null, the exception is appended.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="exception">The exception to log.</param>
		public override void LogError(string message, Exception exception = null)
		{
			OnNewError(message, exception, ErrorType.None);
		}

		/// <summary>
		/// Writes an entry of type 'Error' to the log.
		/// If the exception is not null, the exception is appended.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="errorType">The type of the error.</param>
		/// <param name="exception">The exception to log.</param>
		public override void LogError(string message, ErrorType errorType, Exception exception = null)
		{
			LoggedErrors.Add(errorType);
			OnNewError(message, exception, errorType);
		}

		/// <summary>
		/// Writes an entry of type 'Information' to the log.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		public override void LogInformation(string message)
		{
			OnNewInformation(message, InformationType.None);
		}

		/// <summary>
		/// Writes an entry of type 'Information' to the log.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="informationType">The type of the information.</param>
		public override void LogInformation(string message, InformationType informationType)
		{
			LoggedInformations.Add(informationType);
			OnNewInformation(message, informationType);
		}

		/// <summary>
		/// Writes an entry of type 'Warning' to the log.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		public override void LogWarning(string message)
		{
			OnNewWarning(message, WarningType.None);
		}

		/// <summary>
		/// Writes an entry of type 'Warning' to the log.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="warningType">The type of the warning.</param>
		public override void LogWarning(string message, WarningType warningType)
		{
			LoggedWarnings.Add(warningType);
			OnNewWarning(message, warningType);
		}
	}
}
