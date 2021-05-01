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
	/// Base class of all loggers.
	/// </summary>
	public abstract class AbstractLogger : ILogger
	{
		protected string mLastMessage;

		public event Action<string, InformationType> NewInformation;
		public event Action<string, WarningType> NewWarning;
		public event Action<string, Exception, ErrorType> NewError;

		public bool IgnoreEqualLogMessages { get; set; }

		public abstract void LogInformation(string text);
		public abstract void LogInformation(string text, InformationType informationType);
		public abstract void LogWarning(string text);
		public abstract void LogWarning(string text, WarningType warningType);
		public abstract void LogError(string text, Exception exception = null);
		public abstract void LogError(string text, ErrorType errorType, Exception exception = null);


		/// <summary>
		/// Pushes a new information to subscribed event handlers.
		/// </summary>
		/// <param name="message">The message which got written to the log.</param>
		/// <param name="informationType">The type of information.</param>
		protected void OnNewInformation(string message, InformationType informationType)
		{
			if (NewInformation != null)
			{
				NewInformation(message, informationType);
			}
		}

		/// <summary>
		/// Pushes a new warning to subscribed event handlers.
		/// </summary>
		/// <param name="message">The message which got written to the log.</param>
		/// <param name="warningType">The type of the warning.</param>
		protected void OnNewWarning(string message, WarningType warningType)
		{
			if (NewWarning != null)
			{
				NewWarning(message, warningType);
			}
		}

		/// <summary>
		/// Pushes a new error to subscribed event handlers.
		/// </summary>
		/// <param name="message">The message which got written to the log.</param>
		/// <param name="exception">An exception related to the error or null.</param>
		/// <param name="errorType">The type of the error.</param>
		protected void OnNewError(string message, Exception exception, ErrorType errorType)
		{
			if (NewError != null)
			{
				NewError(message, exception, errorType);
			}
		}
	}
}
