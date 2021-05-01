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
using System.Linq;
using System.Globalization;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class defines a coordinate consisting of latitude and longitude. The unit used is degree.
	/// </summary>
	[Serializable]
	public class Coordinate
	{
		private double mLatitude;
		private double mLongitude;

		/// <summary>
		/// Creates a new instance of a coordinate.
		/// </summary>
		public Coordinate()
		{ }

		/// <summary>
		/// Creates a new instance of a coordinate.
		/// </summary>
		/// <param name="latitude">The latitude in degrees.</param>
		/// <param name="longitude">The longitude in degrees.</param>
		public Coordinate(double latitude, double longitude)
		{
			Latitude = latitude;
			Longitude = longitude;
		}

		/// <summary>
		/// Latitude in degrees (-90 to 90).
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Throws an exception if the value is lower than -90 or greater than 90.</exception>
		public double Latitude
		{
			get { return mLatitude; }
			set
			{
				if (value > 90)
				{
					throw new ArgumentOutOfRangeException("value", "Latitude value cannot be greater than 90.");
				}
				if (value < -90)
				{
					throw new ArgumentOutOfRangeException("value", "Latitude value cannot be less than -90.");
				}
				mLatitude = value;
			}
		}

		/// <summary>
		/// Longitude in degree (-180 to 180)
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Throws an exception if the value is lower than -180 or greater than 180.</exception>
		public double Longitude
		{
			get { return mLongitude; }
			set
			{
				if (value > 180)
				{
					throw new ArgumentOutOfRangeException("value", "Longitude value cannot be greater than 180.");
				}
				if (value < -180)
				{
					throw new ArgumentOutOfRangeException("value", "Longitude value cannot be less than -180.");
				}
				mLongitude = value;
			}
		}

		/// <summary>
		/// Returns the formatted coordinate (e.g. N 47,5° - E 14,2°).
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string text = Latitude >= 0 ? "N" : "S";
			text += " " + Math.Abs(Latitude) + "° - ";
			text += Longitude >= 0 ? "E" : "W";
			text += " " + Math.Abs(Longitude) + "°";
			return text;
		}

		/// <summary>
		/// Returns the formatted coordinate (e.g. 47,5 14,2).
		/// </summary>
		/// <returns></returns>
		public string ToDecimalString()
		{
			return Latitude + " " + Longitude;
		}

		/// <summary>
		/// Parses a string to a coordinate. The format must be latitude, space character, longitude in decimal degrees.
		/// </summary>
		/// <param name="text">The string to be parsed.</param>
		/// <returns></returns>
		public static Coordinate Parse(string text)
		{
			text = text.Replace("°", "");
			var texts = text.Split(' ');

			string latText = texts[0];
			string lonText = texts[1];

			if (latText[0] == 'N')
			{
				latText = latText.Remove(0, 1);
			}
			else if (latText[0] == 'S')
			{
				latText = "-" + latText.Remove(0, 1);
			}

			if (lonText[0] == 'E')
			{
				lonText = lonText.Remove(0, 1);
			}
			else if (lonText[0] == 'W')
			{
				lonText = "-" + lonText.Remove(0, 1);
			}

			double lat = double.Parse(latText);
			double lon = double.Parse(lonText);
			return new Coordinate(lat, lon);
		}

		/// <summary>
		/// Two coordinates are equal if they have the same value for latitude and longitude.
		/// </summary>
		/// <param name="obj">Any object.</param>
		/// <returns>True if latitude and longitude are equal, respectively.</returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			var other = obj as Coordinate;
			if (other != null)
			{
				return other.mLatitude.Equals(mLatitude) && other.mLongitude.Equals(mLongitude);
			}
			return false;
		}

		/// <summary>
		/// The hashcode.
		/// </summary>
		/// <returns>The hashcode</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				return (Latitude.GetHashCode() * 397) ^ Longitude.GetHashCode();
			}
		}

		/// <summary>
		/// Returns the coordinate as a string in the format lat,lon using . as decimal sign (e.g. 48.5323,14.4923).
		/// </summary>
		/// <returns>A string used for queries (e.g. in Google Maps).</returns>
		public string ToQueryString()
		{
			return Latitude.ToString(CultureInfo.InvariantCulture) + "," + Longitude.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Calculates the distance between two points of latitude and longitude in meters.
		/// Formulars and implementations on http://www.movable-type.co.uk/scripts/latlong.html
		/// </summary>
		/// <param name="coordinate1">First coordinate.</param>
		/// <param name="coordinate2">Second coordinate.</param>
		/// <returns>The distance in meters.</returns>
		public static Double CalculateDistance(Coordinate coordinate1, Coordinate coordinate2)
		{
			double lat1 = ConvertDegToRad(coordinate1.Latitude);
			double lng1 = ConvertDegToRad(coordinate1.Longitude);
			double lat2 = ConvertDegToRad(coordinate2.Latitude);
			double lng2 = ConvertDegToRad(coordinate2.Longitude);
			double dlng = lng2 - lng1;

			return 6371001 * Math.Acos(Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(dlng));
		}

		/// <summary>
		/// Calculates the initial bearing (forward azimuth) of two points.
		/// </summary>
		/// <param name="coordinate1">First coordinate.</param>
		/// <param name="coordinate2">Second coordinate</param>
		/// <returns>The bearing in degrees.</returns>
		public static double CalcuateBearing(Coordinate coordinate1, Coordinate coordinate2)
		{
			double lat1 = ConvertDegToRad(coordinate1.Latitude);
			double lng1 = ConvertDegToRad(coordinate1.Longitude);
			double lat2 = ConvertDegToRad(coordinate2.Latitude);
			double lng2 = ConvertDegToRad(coordinate2.Longitude);
			double dlng = lng2 - lng1;

			double y = Math.Sin(dlng) * Math.Cos(lat2);
			double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dlng);
			return ConvertRadToDeg(Math.Atan2(y, x));
		}

		/// <summary>
		/// Converts the direction of degrees into a cardinal value.
		/// </summary>
		/// <param name="bearing">The bearing in degrees. It can have any value, also greater than 360 or lower than 0.</param>
		/// <returns>The cardinal or intercardinal direction, respectively.</returns>
		public static CardinalDirection GetCardinalDirection(double bearing)
		{
			while (bearing > 360)
			{
				bearing -= 360;
			}
			while (bearing < 0)
			{
				bearing += 360;
			}

			if (bearing > 22.5 && bearing < 67.5)
			{
				return CardinalDirection.NE;
			}

			if (bearing >= 67.5 && bearing <= 112.5)
			{
				return CardinalDirection.E;
			}

			if (bearing > 112.5 && bearing < 157.5)
			{
				return CardinalDirection.SE;
			}

			if (bearing >= 157.5 && bearing <= 202.5)
			{
				return CardinalDirection.S;
			}

			if (bearing > 202.5 && bearing < 247.5)
			{
				return CardinalDirection.SW;
			}

			if (bearing >= 247.5 && bearing <= 292.5)
			{
				return CardinalDirection.W;
			}

			if (bearing > 292.5 && bearing < 337.5)
			{
				return CardinalDirection.NW;
			}

			if (bearing >= 337.5 && bearing <= 360 || bearing >= 0 && bearing <= 22.5)
			{
				return CardinalDirection.N;
			}

			return CardinalDirection.None;
		}

		/// <summary>
		/// Converts a cardinal direction value to an arrow symbol (unicode value).
		/// </summary>
		/// <param name="direction">A cardinal direction.</param>
		/// <returns>A unicode character.</returns>
		public static char GetArrowSymbol(CardinalDirection direction)
		{
			switch (direction)
			{
				case CardinalDirection.E:
					return '\u2192';
				case CardinalDirection.N:
					return '\u2191';
				case CardinalDirection.NE:
					return '\u2197';
				case CardinalDirection.NW:
					return '\u2196';
				case CardinalDirection.S:
					return '\u2193';
				case CardinalDirection.SE:
					return '\u2198';
				case CardinalDirection.SW:
					return '\u2199';
				case CardinalDirection.W:
					return '\u2190';
			}

			return '\u0000';
		}

		/// <summary>
		/// Converts a degrees to radians.
		/// </summary>
		/// <param name="deg">A degree value.</param>
		/// <returns>The converted value in radians.</returns>
		private static double ConvertDegToRad(double deg)
		{
			return deg * Math.PI / 180;
		}

		/// <summary>
		/// Converts radians to degrees.
		/// </summary>
		/// <param name="rad">A radian value.</param>
		/// <returns>The converted value in degrees.</returns>
		private static double ConvertRadToDeg(double rad)
		{
			return rad / Math.PI * 180;
		}
	}
}
