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

using System.Collections.Generic;
using System.Windows.Media;

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// This class contains all the data that is loaded form the web.
	/// </summary>
	internal class WebData
	{
		/// <summary>
		/// Indicates whether reading from the web failed.
		/// </summary>
		public bool ReadingFailed;

		/// <summary>
		/// Basic data for the Info-Center.
		/// </summary>
		public InfoCenterData InfoCenterData;

		/// <summary>
		/// Data about the weather.
		/// </summary>
		public WeatherData WeatherData;

		/// <summary>
		/// A dictionary that contains the URL and the corresponding image.
		/// </summary>
		public Dictionary<string, ImageSource> Url2Image;
	}
}
