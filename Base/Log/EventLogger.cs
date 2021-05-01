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
using System.ComponentModel;
using System.Diagnostics;

namespace AlarmInfoCenter.Base
{
	public class EventLogger : AbstractLogger
	{
		private bool mIsValid = true;
		private readonly EventLog mLog;
		private readonly object mLocker = new object();

		/// <summary>
		/// Indicates whether writing to the event log is possible.
		/// This property only returns a valid value after the first entry has been written.
		/// </summary>
		public bool IsValid
		{
			get { return mIsValid; }
		}

		// Necessary for ninject
		public EventLogger() : this(Constants.EventLogName, Constants.EventLogSourceName)
		{ }

		/// <summary>
		/// Creates a new logger that writes entries to the Windows event log.
		/// </summary>
		/// <param name="logName">The name of the event log.</param>
		/// <param name="source">The name of the event source.</param>
		public EventLogger(string logName, string source)
		{
			try
			{
				mLog = new EventLog(logName);
				mLog.Source = source;
			}
			catch
			{
				mIsValid = false;
			}
		}

		/// <summary>
		/// Writes an entry to the log. Default entry type is 'Information'.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="type">The entry type of this message.</param>
		public void WriteEntry(string message, EventLogEntryType type = EventLogEntryType.Information)
		{
			lock (mLocker)
			{
				if (mIsValid && (!IgnoreEqualLogMessages || mLastMessage != message))
				{
					try
					{
						mLog.WriteEntry(message, type); // Errors in this method have been experienced using Windows XP
						mLastMessage = message;
					}
					catch (Win32Exception)
					{
						// Maybe the event log is full
						mIsValid = false;
					}
					catch
					{
						mIsValid = false;
					}
				}
			}
		}

		/// <summary>
		/// Writes an entry of type 'Information' to the log.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="informationType">The type of the information.</param>
		public override void LogInformation(string message, InformationType informationType)
		{
			WriteEntry(message);
			OnNewInformation(message, informationType);
		}

		/// <summary>
		/// Writes an entry of type 'Information' to the log.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		public override void LogInformation(string message)
		{
			LogInformation(message, InformationType.None);
		}

		/// <summary>
		/// Writes an entry of type 'Warning' to the log.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		public override void LogWarning(string message)
		{
			LogWarning(message, WarningType.None);
		}

		/// <summary>
		/// Writes an entry of type 'Warning' to the log.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="warningType">The type of the warning.</param>
		public override void LogWarning(string message, WarningType warningType)
		{
			WriteEntry(message, EventLogEntryType.Warning);
			OnNewWarning(message, warningType);
		}

		/// <summary>
		/// Writes an entry of type 'Error' to the log.
		/// If the exception is not null a line break is added after the message text and the exception is appended.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="exception">The exception to log.</param>
		public override void LogError(string message, Exception exception = null)
		{
			LogError(message, ErrorType.None, exception);
		}

		/// <summary>
		/// Writes an entry of type 'Error' to the log.
		/// If the exception is not null a line break is added after the message text and the exception is appended.
		/// </summary>
		/// <param name="message">The message to write to the log.</param>
		/// <param name="errorType">The type of the error.</param>
		/// <param name="exception">The exception to log.</param>
		public override void LogError(string message, ErrorType errorType, Exception exception = null)
		{
			if (exception != null)
			{
				message += Environment.NewLine + exception;
			}

			WriteEntry(message, EventLogEntryType.Error);
			OnNewError(message, exception, errorType);
		}
	}
}
