/* *  Copyright 
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
using System.Net;
using System.Linq;
using Newtonsoft.Json;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class represents the structure of water supply point data returned by wasserkarte.info.
	/// </summary>
	internal class WasserkarteInfoData
	{
		[JsonProperty("waterSources")]
		public List<WaterSource> WaterSources;

		[JsonProperty("sourceTypes")]
		public List<SourceType> SourceTypes;

		/// <summary>
		/// Converts this object to a WaterSupplyPoint object.
		/// </summary>
		/// <returns>A WaterSupplyPoint object.</returns>
		public List<WaterSupplyPoint> ToWaterSupplyPoints()
		{
			const string language = "de";
			var points = new List<WaterSupplyPoint>();
			foreach (var source in WaterSources)
			{
				var point = source.ToWaterSupplyPoint();
				var sourceType = SourceTypes.FirstOrDefault(x => x.Id == source.SourceType);
				if (sourceType != null && sourceType.Names != null && sourceType.Names.ContainsKey(language))
				{
					point.KindName = sourceType.Names[language];
				}
				points.Add(point);
			}
			return points;
		}
	}

	/// <summary>
	/// This class represents the water source types returned by wasserkarte.info.
	/// </summary>
	internal class SourceType
	{
		[JsonProperty("id")]
		public int Id;

		[JsonProperty("name")]
		public Dictionary<string, string> Names;
	}

	/// <summary>
	/// This class represents the water sources returned by wasserkarte.info.
	/// </summary>
	internal class WaterSource
	{
		[JsonProperty("id")]
		public int Id;

		[JsonProperty("name")]
		public string Name;

		[JsonProperty("sourceType")]
		public int SourceType;

		[JsonProperty("connections")]
		public string Connections;

		[JsonProperty("address")]
		public string Address;

		[JsonProperty("capacity")]
		public double Capacity;

		[JsonProperty("flowrate")]
		public double Flowrate;

		[JsonProperty("nominalDiameter")]
		public double NominalDiameter;

		[JsonProperty("notes")]
		public string Notes;

		[JsonProperty("driveway")]
		public string Driveway;

		[JsonProperty("longitude")]
		public double Longitude;

		[JsonProperty("latitude")]
		public double Latitude;

		[JsonProperty("distanceInMetres")]
		public double Distance;



		/// <summary>
		/// Converts this object to a WaterSupplyPoint object.
		/// </summary>
		/// <returns>A WaterSupplyPoint object.</returns>
		public WaterSupplyPoint ToWaterSupplyPoint()
		{
			var wsp = new WaterSupplyPoint
			{
				Connections = Connections,
				Coordinate = new Coordinate(Latitude, Longitude),
				Distance = (int) Math.Round(Distance, 0),
				Information = GetInformation(),
				Location = WebUtility.HtmlDecode(Address),
				Size = (int)Math.Round(NominalDiameter, 0),
			};
			return wsp;
		}

		private string GetInformation()
		{
			string info = string.Empty;

			if (Capacity > 0)
			{
				info = Capacity + " m\u00B3";		// e.g. 80 m³
			}

			if (NominalDiameter > 0)
			{
				if (!string.IsNullOrWhiteSpace(info))
				{
					info += ", ";
				}
				info = "\u2300 " + NominalDiameter + " mm";		// e.g. ø 50 mm 
			}

			if (Flowrate > 0)
			{
				if (!string.IsNullOrWhiteSpace(info))
				{
					info += ", ";
				}
				info += Flowrate + " l/min";
			}

			if (!string.IsNullOrWhiteSpace(Connections))
			{
				if (!string.IsNullOrWhiteSpace(info))
				{
					info += ", ";
				}
				info += Connections;
			}

			if (!string.IsNullOrWhiteSpace(Notes))
			{
				if (!string.IsNullOrWhiteSpace(info))
				{
					info += Environment.NewLine;
				}
				info += Notes;
			}

			if (!string.IsNullOrWhiteSpace(Driveway))
			{
				if (!string.IsNullOrWhiteSpace(info))
				{
					info += Environment.NewLine;
				}
				info += Driveway;
			}

			return info;
		}
	}
}
