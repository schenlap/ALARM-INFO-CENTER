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

namespace AlarmInfoCenter.Base
{
    /// <summary>
    /// Generic logger interface for several Logger-components and classes.
    /// </summary>
    public interface ILogger
    {
		/// <summary>
		/// This event will be thrown after a new information got logged.
		/// </summary>
		event Action<string, InformationType> NewInformation;

		/// <summary>
		/// This event will be thrown after a new warning got logged.
		/// </summary>
		event Action<string, WarningType> NewWarning;

		/// <summary>
		/// This event will be thrown after a new error got logged.
		/// </summary>
		event Action<string, Exception, ErrorType> NewError;

		/// <summary>
		/// Writes an entry of type 'Error' to the log.
		/// If the exception is not null the exception is appended.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="errorType">The type of the error.</param>
		/// <param name="exception">The exception to log.</param>
        void LogError(string message, ErrorType errorType, Exception exception = null);

		/// <summary>
		/// Writes an entry of type 'Error' to the log.
		/// If the exception is not null the exception is appended.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="exception">The exception to log.</param>
		void LogError(string message, Exception exception = null);

		/// <summary>
		/// Writes an entry of type 'Information' to the log.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="informationType">The type of information.</param>
        void LogInformation(string message, InformationType informationType);

		/// <summary>
		/// Writes an entry of type 'Information' to the log.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		void LogInformation(string message);

		/// <summary>
		/// Writes an entry of type 'Warning' to the log.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="warningType">The type of the warning.</param>
        void LogWarning(string message, WarningType warningType);

		/// <summary>
		/// Writes an entry of type 'Warning' to the log.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		void LogWarning(string message);

		/// <summary>
		/// Indicates whether an entry should only be added if it is different to the previous log message. 
		/// The default value is false.
		/// </summary>
		bool IgnoreEqualLogMessages { get; set; }
    }
}
