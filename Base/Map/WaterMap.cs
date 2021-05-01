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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class contains functionality for getting water supply points.
	/// </summary>
	public class WaterMapQuery
	{
		private readonly bool mUseWasserkarteInfo;
		private int mWaterSupplyPointCount = 5;
		private int mDistanceThreshold = 2000;
		private readonly string mCustomWaterMapApiUrl;
		private readonly string mWasserkarteInfoToken;


		/// <summary>
		/// The numer of water supply points that should be returned by the query.
		/// The value is always greater than 0.
		/// Default: 5
		/// </summary>
		public int WaterSupplyPointCount
		{
			get { return mWaterSupplyPointCount > 0 ? mWaterSupplyPointCount : 1; }
			set { mWaterSupplyPointCount = value > 0 ? value : 1; }
		}

		/// <summary>
		/// The maximum distance in meters between the location and the water supply point used by the query.
		/// The value is always greater than 0.
		/// Default: 2000
		/// </summary>
		public int SearchRadius
		{
			get { return mDistanceThreshold > 0 ? mDistanceThreshold : 1; }
			set { mDistanceThreshold = value > 0 ? value : 1; }
		}



		public WaterMapQuery(bool useWasserkarteInfo = true, string wasserkarteInfoToken = null, string customWaterMapApiUrl = null)
		{
			mUseWasserkarteInfo = useWasserkarteInfo;
			mWasserkarteInfoToken = wasserkarteInfoToken;
			mCustomWaterMapApiUrl = customWaterMapApiUrl;
		}

		/// <summary>
		/// Reads the nearest water supply points to a given coordinate.
		/// </summary>
		/// <param name="coordinate">The coordinate to search for.</param>
		/// <returns>A number of water supply points ordered by distance.</returns>
		/// <exception>Throws any exception that cann occur during downloading or processing the data.</exception>
		public List<WaterSupplyPoint> FindWaterSupplyPoints(Coordinate coordinate)
		{
			List<WaterSupplyPoint> waterSupplyPoints;
			string url = CreateUrl(coordinate.Latitude, coordinate.Longitude);
			using (var stream = Network.DownloadStreamData(url, Constants.NetworkTimeouts))
			{
				waterSupplyPoints = mUseWasserkarteInfo ? DeserializeWasserkarteInfoData(stream) : DeserializeCustomData(stream);
			}
			return waterSupplyPoints.Where(x => x.Distance <= SearchRadius).OrderBy(x => x.Distance).ToList();
		}

		/// <summary>
		/// Trys reading the nearest water supply points to a given coordinate.
		/// </summary>
		/// <param name="coordinate">The coordinate to search for.</param>
		/// <returns>A number of water supply points ordered by distance. Never null.</returns>
		public List<WaterSupplyPoint> TryFindWaterSupplyPoints(Coordinate coordinate)
		{
			List<WaterSupplyPoint> waterSupplyPoints;
			try
			{
				waterSupplyPoints = FindWaterSupplyPoints(coordinate);
				if (waterSupplyPoints == null)
				{
					throw new NullReferenceException();
				}
			}
			catch
			{
				waterSupplyPoints = new List<WaterSupplyPoint>();
			}
			return waterSupplyPoints;
		}

		/// <summary>
		/// Creates the URL that is used for downloading the water supply points.
		/// </summary>
		/// <param name="latitude">The latitude of the origin.</param>
		/// <param name="longitude">The longitude of the origin.</param>
		/// <returns>The URL including all parameters.</returns>
		private string CreateUrl(double latitude, double longitude)
		{
			string url;
			if (mUseWasserkarteInfo)
			{
				url = "https://api.wasserkarte.info/1.0/getSurroundingWaterSources/?" +
					"source=aic" +
					"&token=" + mWasserkarteInfoToken +
					"&lat=" + latitude.ToString(CultureInfo.InvariantCulture) +
					"&lng=" + longitude.ToString(CultureInfo.InvariantCulture) +
					"&range=" + (SearchRadius / 1000).ToString(CultureInfo.InvariantCulture) +		// wasserkarte.info uses kilometers
					"&numItems=" + WaterSupplyPointCount;
			}
			else
			{
				url = mCustomWaterMapApiUrl +
					"?latitude=" + latitude.ToString(CultureInfo.InvariantCulture) +
					"&longitude=" + longitude.ToString(CultureInfo.InvariantCulture) +
					"&count=" + WaterSupplyPointCount;
			}
			return url;
		}

		/// <summary>
		/// Deserializes the stream using the wasserkarte.info structure.
		/// </summary>
		/// <param name="stream">The stream containing the watter supply point data.</param>
		/// <returns>A list containing the water supply points.</returns>
		private static List<WaterSupplyPoint> DeserializeWasserkarteInfoData(Stream stream)
		{
			string text;
			using (var sr = new StreamReader(stream))
			{
				text = sr.ReadToEnd();
			}
			var waterSupplyPoints = JsonConvert.DeserializeObject<WasserkarteInfoData>(text);
			return waterSupplyPoints.ToWaterSupplyPoints();
		}

		/// <summary>
		/// Deserializes the stream using a custom structure.
		/// </summary>
		/// <param name="stream">The stream containing the watter supply point data.</param>
		/// <returns>A dictionary containing the water supply points and their corresponding distances to the origin.</returns>
		private static List<WaterSupplyPoint> DeserializeCustomData(Stream stream)
		{
			var waterSupplyPoints = new List<WaterSupplyPoint>();

			// Read data using XPath
			const string namespaceURI = "";
			var nav = new XPathDocument(stream).CreateNavigator();
			while (nav.MoveToFollowing("WaterSupplyPoint", namespaceURI))
			{
				int distance = 0;
				var point = new WaterSupplyPoint();

				// Coordinate
				nav.MoveToFollowing("Latitude", namespaceURI);
				double lat = nav.ValueAsDouble;
				nav.MoveToFollowing("Longitude", namespaceURI);
				double lng = nav.ValueAsDouble;
				point.Coordinate = new Coordinate(lat, lng);

				// Kind
				bool found = nav.MoveToFollowing("Kind", namespaceURI);
				if (found)
				{
					switch (nav.Value)
					{
						case "pillarhydrant":
							point.Kind = WaterSupplyPointKind.PillarHydrant;
							break;
						case "undergroundhydrant":
							point.Kind = WaterSupplyPointKind.UndergroundHydrant;
							break;
						case "fireextinguishingpool":
							point.Kind = WaterSupplyPointKind.FireExtinguishingPool;
							break;
						case "suctionpoint":
							point.Kind = WaterSupplyPointKind.SuctionPoint;
							break;
						default:
							point.Kind = WaterSupplyPointKind.Undefined;
							break;
					}
				}

				found = nav.MoveToFollowing("Location", namespaceURI);
				if (found)
				{
					point.Location = nav.Value;
				}

				found = nav.MoveToFollowing("Information", namespaceURI);
				if (found)
				{
					point.Information = nav.Value;
				}

				found = nav.MoveToFollowing("Size", namespaceURI);
				if (found && !string.IsNullOrWhiteSpace(nav.Value))
				{
					point.Size = nav.ValueAsInt;
				}

				found = nav.MoveToFollowing("Connections", namespaceURI);
				if (found)
				{
					point.Connections = nav.Value;
				}

				found = nav.MoveToFollowing("Distance", namespaceURI);
				if (found && !string.IsNullOrWhiteSpace(nav.Value))
				{
					distance = nav.ValueAsInt;
				}

				point.Distance = distance;

				waterSupplyPoints.Add(point);
			}

			return waterSupplyPoints;
		}
	}
}
