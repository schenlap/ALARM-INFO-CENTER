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
using Newtonsoft.Json.Linq;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class contains helper methods concerning mapping functionality.
	/// </summary>
	public static class MapHelper
	{
		/// <summary>
		/// The URL of the route website.
		/// </summary>
		public static string RouteUrl;

		/// <summary>
		/// The origin of the route.
		/// </summary>
		public static string RouteOrigin;


		/// <summary>
		/// Geocodes the provided address using Google Maps. If several results are found, the address in the city of the fire brigade is preferred.
		/// </summary>
		/// <param name="address">The address to look for.</param>
		/// <param name="timeout">The timeout for the web request.</param>
		/// <returns>The coordinate of the geocoded location, or null if nothing has been found.</returns>
		/// <remarks>Tests showed that a minimum timeout of 150 milliseconds is recommended.</remarks>
		public static Coordinate Geocode(string address, int timeout = 3000)
		{
			Coordinate coordinate = null;
			dynamic result = GeocodeAddress(address, timeout);
			if (result != null)
			{
				double lat = result.geometry.location.lat;
				double lng = result.geometry.location.lng;
				coordinate = new Coordinate(lat, lng);
			}

			return coordinate;
		}

		/// <summary>
		/// Downloads a JSON object and returns the token specified by the path if it is set.
		/// </summary>
		/// <param name="url">The URL to download the JSON data.</param>
		/// <param name="path">The path the selects the JSON token. If null, the whole JSON object is returned.</param>
		/// <param name="timeout">The timeout for the web request.</param>
		/// <returns>The specified JSON object.</returns>
		private static JToken GetJsonObject(string url, string path = null, int timeout = 3000)
		{
			var stream = Network.DownloadStreamData(url, timeout);
			var json = Utilities.StreamToString(stream, Encoding.UTF8);
			JToken jObject = JObject.Parse(json);
			if (path != null)
			{
				jObject = jObject.SelectToken(path);
			}
			return jObject;
		}

		/// <summary>
		/// Creates the URI that shows the route to the destination.
		/// </summary>
		/// <param name="alarm">The alarm for the route.</param>
		/// <returns>A URI of the route website.</returns>
		public static Uri GetRouteUri(Alarm alarm)
		{
			string uri = string.Format("{0}?origin={1}&destination={2}", RouteUrl, RouteOrigin, alarm.MapLocation);
			return new Uri(Uri.EscapeUriString(uri));
		}

		/// <summary>
		/// Calculates the best zoom level that shows every single point.
		/// </summary>
		/// <param name="coordinates">A list of coordinates.</param>
		/// <param name="mapPixelWidth">The width of the map in pixels.</param>
		/// <param name="mapPixelHeight">The height of the map in pixels.</param>
		/// <returns>Returns the best zoom level or -1 if an error occured.</returns>
		/// <remarks>http://stackoverflow.com/questions/6048975/google-maps-v3-how-to-calculate-the-zoom-level-for-a-given-bounds</remarks>
		public static int CalculateZoom(List<Coordinate> coordinates, int mapPixelWidth, int mapPixelHeight)
		{
			int zoom = -1;
			if (coordinates != null && coordinates.Count > 1)
			{
				double minLat = coordinates.Min(x => x.Latitude);
				double maxLat = coordinates.Max(x => x.Latitude);
				double minLng = coordinates.Min(x => x.Longitude);
				double maxLng = coordinates.Max(x => x.Longitude);

				const int GLOBE_WIDTH = 256; // a constant in Google's map projection

				try
				{
					double latFraction = (latRad(maxLat) - latRad(minLat)) / Math.PI;
					double lngDiff = maxLng - minLng;
					double lngFraction = (lngDiff < 0 ? lngDiff + 360 : lngDiff) / 360;

					int latZoom = (int)Math.Floor(Math.Log(mapPixelHeight / GLOBE_WIDTH / latFraction) / Math.Log(2));
					int lngZoom = (int)Math.Floor(Math.Log(mapPixelWidth / GLOBE_WIDTH / lngFraction) / Math.Log(2));

					zoom = Math.Min(latZoom, lngZoom);
				}
				catch { zoom = -1; }
			}

			return zoom;
		}

		// Helper method for CalculateZoom(...)
		private static double latRad(double lat)
		{
			double sin = Math.Sin(lat * Math.PI / 180);
			double radX2 = Math.Log((1 + sin) / (1 - sin)) / 2;
			return Math.Max(Math.Min(radX2, Math.PI), -Math.PI) / 2;
		}

		/// <summary>
		/// Calculates the center of a number of coordinates.
		/// </summary>
		/// <param name="coordinates">A list of coordinates.</param>
		/// <returns>Returns the center coordinate or null if an error occurs.</returns>
		public static Coordinate CalculateCenter(List<Coordinate> coordinates)
		{
			Coordinate coordinate = null;
			if (coordinates != null && coordinates.Any())
			{
				double minLat = coordinates.Min(x => x.Latitude);
				double maxLat = coordinates.Max(x => x.Latitude);
				double minLng = coordinates.Min(x => x.Longitude);
				double maxLng = coordinates.Max(x => x.Longitude);

				coordinate = new Coordinate((minLat + maxLat) / 2, (minLng + maxLng) / 2);
			}
			return coordinate;
		}

		/// <summary>
		/// Returns the Google Maps URL for showing the provided address in the browser.
		/// </summary>
		/// <param name="address">The address to serch for.</param>
		/// <returns>A URL for showing the address in the browser.</returns>
		public static string GetGoogleMapsUrl(string address)
		{
			return Constants.GoogleMapsUrl + "?t=h&q=" + address;
		}

		/// <summary>
		/// Looks for the address and tries to get the corresponding postal code and city name.
		/// </summary>
		/// <param name="address">The address to look for.</param>
		/// <param name="timeout">The timeout for the web request.</param>
		/// <returns>A tuple, never null, with default 0 and string.Empty.</returns>
		public static Tuple<string, string> GetPostalCodeAndCity(string address, int timeout = 3000)
		{
			var result = GeocodeAddress(address, timeout);
			return GetPostalCodeAndCity(result);
		}

		/// <summary>
		/// Looks for the postal code and city name in the provided JSON object.
		/// </summary>
		/// <param name="result">The Google Maps Geocoding result object as JSON.</param>
		/// <returns>The postal code and city name, or empty strings if not found.</returns>
		private static Tuple<string, string> GetPostalCodeAndCity(JToken result)
		{
			string postalCode = string.Empty;
			string city = string.Empty;

			if (result != null)
			{
				dynamic r = result;
				foreach (var addressComponent in r.address_components)
				{
					JArray types = addressComponent.types;
					if (types.Any(x => x.ToString() == "administrative_area_level_3"))
					{
						postalCode = addressComponent.long_name;
					}
					else if (types.Any(x => x.ToString() == "postal_code"))
					{
						city = addressComponent.long_name;
					}
				}
			}

			return new Tuple<string, string>(postalCode, city);
		}

		/// <summary>
		/// Geocodes the provided address and returns the first Google Maps Geocoding result.
		/// </summary>
		/// <param name="address">The address to search for.</param>
		/// <param name="timeout">The timeout for the web request.</param>
		/// <returns>The first found result.</returns>
		private static JToken GeocodeAddress(string address, int timeout = 3000)
		{
			string url = Constants.GoogleMapsApiGeocodeUrl + "json?sensor=false&language=de&region=at&address=" + address;
			return GetJsonObject(url, "results[0]", timeout);
		}

		/// <summary>
		/// Checks if the provided address is a valid alarm location. The address is not valid if the geocoding result represents only the specified city and not a specific address. 
		/// </summary>
		/// <param name="address">The address to check.</param>
		/// <param name="invalidPostalCode">The postal code of the invalid city.</param>
		/// <param name="invalidCity">The name of the invalid city.</param>
		/// <param name="timeout">The timeout for the web request.</param>
		/// <returns>True, if the address is a valid alarm location, otherwise false.</returns>
		public static bool IsDestinationValid(string address, string invalidPostalCode, string invalidCity, int timeout = 3000)
		{
			bool ok = false;
			dynamic result = GeocodeAddress(address, timeout);

			if (result != null)
			{
				// See https://developers.google.com/maps/documentation/geocoding/?hl=de#Types for a list of valid types
				var validTypeStrings = new List<string> { "street_address", "route", "intersection", "premise", "subpremise", "airport", "park", "street_number", "floor", "room" };

				// Check the general types
				JArray types = result.types;
				if (types.Select(x => x.ToString()).Intersect(validTypeStrings).Any())
				{
					return true;
				}

				// Check the address component types
				foreach (var addressComponent in result.address_components)
				{
					types = addressComponent.types;
					if (types.Select(x => x.ToString()).Intersect(validTypeStrings).Any())
					{
						return true;
					}
				}

				// Postal code or city name must not be the same
				var postalCodeAndCity = GetPostalCodeAndCity(result);
				ok = !string.Equals(postalCodeAndCity.Item1, invalidPostalCode) && 
					!string.Equals(postalCodeAndCity.Item2, invalidCity, StringComparison.CurrentCultureIgnoreCase);
			}

			return ok;
		}

		/// <summary>
		/// Returns the string representation of the provided map type.
		/// </summary>
		/// <param name="mapType">The map type to get the string reprentation for.</param>
		/// <returns>The string representation of the provided map type</returns>
		public static string GetMapTypeString(GoogleMapType mapType)
		{
			switch (mapType)
			{
				case GoogleMapType.Hybrid:
					return "hybrid";
				case GoogleMapType.Road:
					return "roadmap";
				case GoogleMapType.Satellite:
					return "satellite";
				case GoogleMapType.Terrain:
					return "terrain";
				default:
					return "roadmap";
			}
		}

		/// <summary>
		/// Contacts the Google Maps directions service and asks for the point string that defines the route to the alarm location.
		/// </summary>
		/// <param name="start">The coordinate of the start point.</param>
		/// <param name="end">The coordinate of the end point.</param>
		/// <returns>An encoded string that defines a number of points or null if the string could not be found by the query.</returns>
		public static string GetPathString(Coordinate start, Coordinate end)
		{
			string url = Constants.GoogleMapsApiDirectionsUrl + "json?sensor=false&origin=" + start.ToQueryString() + "&destination=" + end.ToQueryString();
			var points = GetJsonObject(url, "routes[0].overview_polyline.points");
			return points == null ? null : points.ToObject<string>();
		}
	}
}
