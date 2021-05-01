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

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// Wasserentnahmestelle
	/// </summary>
	public class WaterSupplyPoint
	{
		/// <summary>
		/// The coordinate of the water supply point.
		/// </summary>
		public Coordinate Coordinate { get; set; }

		/// <summary>
		/// The kind of the water supply point.
		/// </summary>
		public WaterSupplyPointKind Kind { get; set; }

		/// <summary>
		/// The name of the kind of the water supply point. Use this string when Kind is Undefined.
		/// </summary>
		public string KindName { get; set; }

		/// <summary>
		/// A textual description of the location of the water supply point.
		/// </summary>
		public string Location { get; set; }

		/// <summary>
		/// General information of the water supply point.
		/// </summary>
		public string Information { get; set; }

		/// <summary>
		/// The size of the water supply point (pipe diameter of hydrants or volume of fire-extinguishing-pools).
		/// </summary>
		public int Size { get; set; }

		/// <summary>
		/// The connections of a pillar hydrant.
		/// </summary>
		public string Connections { get; set; }

		/// <summary>
		/// The distance of the water supply point to any location (e.g. the alarm location).
		/// </summary>
		public int Distance { get; set; }

		/// <summary>
		/// Converts the water supply point kind into a human-readable value.
		/// </summary>
		/// <param name="kind">Ther water supply point kind.</param>
		/// <returns>A german value.</returns>
		public string GetKindText()
		{
			switch (Kind)
			{
				case WaterSupplyPointKind.FireExtinguishingPool:
					return "Löschbecken";
				case WaterSupplyPointKind.PillarHydrant:
					return "Hydrant (Oberflur)";
				case WaterSupplyPointKind.SuctionPoint:
					return "Saugstelle";
				case WaterSupplyPointKind.UndergroundHydrant:
					return "Hydrant (Unterflur)";
			}
			return KindName ?? string.Empty;
		}
	}
}
