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
using System.Net;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class is used for uploading alarm data to the web.
	/// </summary>
	public static class AlarmSend
	{
		private static Action<string, Exception> mExceptionLog;

		/// <summary>
		/// Checks whether the URL for uploading alarm data and the response is valid.
		/// </summary>
		/// <returns>True if the website is valid.</returns>
		public static bool CheckUploadUrl(string url)
		{
			bool ok = false;
			if (!string.IsNullOrWhiteSpace(url))
			{
				try
				{
					var request = WebRequest.Create(url);
					request.Timeout = 5000;
					var response = request.GetResponse() as HttpWebResponse;
					ok = response != null && response.StatusCode == HttpStatusCode.OK;
				}
				catch (Exception)
				{
					ok = false;
				}
			}
			return ok;
		}

		/// <summary>
		/// Sends an alarm in a separate thread to the web. The web adds or updates the alarm.
		/// </summary>
		/// <param name="url">The url used for uploading alarm data.</param>
		/// <param name="alarm">The alarm to send to the web.</param>
		/// <param name="exceptionLog">A method for logging if an exception occurs.</param>
		public static void SendAsync(string url, Alarm alarm, Action<string, Exception> exceptionLog = null)
		{
			mExceptionLog = exceptionLog;
			var client = new RestSharp.RestClient(url);
			var request = new RestSharp.RestRequest(RestSharp.Method.POST);
			request.AddParameter("alarm", alarm.Serialize());
			client.ExecuteAsync(request, Done);
		}

		private static void Done(RestSharp.IRestResponse response, RestSharp.RestRequestAsyncHandle handle)
		{
			if (response.ErrorException != null && mExceptionLog != null)
			{
				mExceptionLog(response.ErrorMessage, response.ErrorException);
			}
		}
	}
}
