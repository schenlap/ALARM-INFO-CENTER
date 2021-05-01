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
	public enum ErrorType
	{
		None = 0,
		Undefined = 1,
		UnknownSocketException = 2,

		ListeningManager_UnobservedTaskException = 100,
		ListeningManager_WasListenerAlreadyRunning = 101,
		ListeningManager_ClientListenerAlreadyRunning = 102,
		ListeningManager_WasListenerNotRunning = 103,
		ListeningManager_ClientListenerNotRunning = 104,
		ListeningManager_WasReconnectCurrentlyRunning = 105,

		WasListener_WorkerAlreadyRunning = 200,
		WasListener_WorkerNotRunning = 201,
		WasListener_ListenerStoppedSocketError = 202,
		WasListener_ListenerStoppedUnknownError = 203,
		WasListener_WasDisconnectWhileWaiting = 204,

		ClientListener_ListenerAlreadyRunning = 300,
		ClientListener_ListenerNotRunning = 301,

		ClientSession_SessionNotStarted = 400,
		ClientSession_SessionAlreadyDestroyed = 401,
		ClientSession_SessionAlreadyStarted = 402,
		ClientSession_NoClientResponse = 403,
		ClientSession_UnknownMessageType = 404,

		AicServerSettings_ErrorWhileLoadingWhiteFiles = 500
	}

	public enum WarningType
	{
		None = 0,

		ClientSession_NoClientResponse = 10,
		ClientSession_CantPushMessage = 11,

		ListeningManager_BrokenWasConnection = 100,
		ListeningManager_CantPingWas = 101,
		ListeningManager_WasReconnectAlreadyRunning = 102,

		ClientListener_CantPushNewState = 200,

		AicServerSettings_NoWhiteFilesFound = 300
	}

	public enum InformationType
	{
		None = 0,

		ClientSession_ReceivedRequest = 10,
		ClientSession_ReceivedResponse = 11,
		ClientSession_ReceivedKeepAlive = 12,
		ClientSession_ThreadCanceled = 13,
		ClientSession_StoppedListening = 14,
		ClientSession_StartedSession = 15,
		ClientSession_ClosedConnection = 16,
		ClientSession_RaisedSessionDestroyedEvent = 17,
		ClientSession_PerformedPush = 18,

		ListeningManager_WasConnectionStateChanged = 100,
		ListeningManager_NewWasDataAvailable = 101,
		ListeningManager_TryWasReconnect = 102,
		ListeningManager_WasReconnectedAgain = 103,
		ListeningManager_WasListeningStopped = 104,
		ListeningManager_ClientListeningStopped = 105,

		WasListener_WorkerStarted = 200,
		WasListener_WorkerStopped = 201,
		WasListener_KeepAliveSent = 202,
		WasListener_WasObjectDeserialized = 203,

		AlarmState_WhiteSucceded = 300,
		AlarmState_WhiteFailed = 301,

		ClientListener_StartedListening = 400,
		ClientListener_ClientSessionRemoved = 401,
		ClientListener_AllClientSessionsDestroyed = 402,
		ClientListener_NewStatePushed = 403
	}
}
