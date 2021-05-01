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
using System.Text;

namespace AlarmInfoCenter.Base
{
	public class AicException : Exception
	{
		public readonly AicExceptionType ExceptionType;

		public AicException(AicExceptionType type) 
			: base()
		{
			this.ExceptionType = type;
		}

		public AicException(string message, AicExceptionType type)
			: base(message)
		{
			this.ExceptionType = type;
		}
	}

	public enum AicExceptionType
	{
		None = 0,

		ClientSession_NoClientResponse = 100,
		ClientSession_UnknownMessageType = 101,

		ListeningManager_CantPingWas = 200
	}
}
