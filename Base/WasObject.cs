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

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using RestSharp.Contrib;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class reprensents the structure of the XML returned by the WAS.
	/// </summary>
	[XmlType("pdu")]
	public class WasObject
	{
		/// <summary>
		/// This is the name of the XML root tag for the WasObject.
		/// </summary>
		private const string XmlRootTagName = "pdu";

		/// <summary>
		/// The regular expression of the end tag of the WasObject.
		/// </summary>
		public const string XmlWasObjectEndTagRegex = "</" + XmlRootTagName + ">|<" + XmlRootTagName + "[^>]*/>";

		// This must be a member variable otherwise it will consume a lot of memory
		private static readonly XmlSerializer serializer = new XmlSerializer(typeof(WasObject));

		[XmlArray("order-list")]
		[XmlArrayItem("order")]
		public List<WasAlarm> Alarms { get; set; }


		/// <summary>
		/// Serialize the alarms as XML into a stream. The stream will not be closed.
		/// Throws an exception if serializing fails.
		/// </summary>
		/// <param name="stream">The stream for serialization.</param>
		/// <param name="useWasEncoding">Indicates whether the stream should use WAS encoding.</param>
		public void Serialize(Stream stream, bool useWasEncoding = true)
		{
			if (useWasEncoding)
			{
				var sw = new StreamWriter(stream, Constants.WasEncoding);
				serializer.Serialize(sw, this);
			}
			else
			{
				serializer.Serialize(stream, this);
			}
		}

		/// <summary>
		/// Serializes the alarm to a string using WAS encoding.
		/// </summary>
		/// <returns>An xml-string containing the alarm.</returns>
		public string Serialize(bool applyAmpersandError = false)
		{
			using (var sw = new StringWriterWas())
			{
				serializer.Serialize(sw, this);
				string s = sw.ToString();
				if (applyAmpersandError)
				{
					s = s.Replace(HttpUtility.HtmlEncode("&"), Constants.WasXmlAmpersandFailureSequence);
				}
				return s;
			}
		}

		/// <summary>
		/// Reads a WasObject from a stream. Throws an exception if deserializing fails.
		/// </summary>
		/// <param name="stream">A stream containing the WasObject.</param>
		/// <returns>A WasObject.</returns>
		/// <remarks>This method does not close the stream. The encoding of the stream is detected automatically.</remarks>
		public static WasObject Deserialize(Stream stream)
		{
			return (WasObject)serializer.Deserialize(stream);
		}

		/// <summary>
		/// Reads a WasObject from a string. Throws an exception if deserializing fails.
		/// </summary>
		/// <param name="xml">A string containing the WasObject.</param>
		/// <param name="replaceAmpersand">Indicates whether the invalid ampersand encoding should be replaced by the correct encoding.</param>
		/// <returns>A WasObject.</returns>
		public static WasObject Deserialize(string xml, bool replaceAmpersand = true)
		{
			if (replaceAmpersand)
			{
				xml = xml.Replace(Constants.WasXmlAmpersandFailureSequence, HttpUtility.HtmlEncode("&"));
			}
			using (var sr = new StringReader(xml))
			{
				return (WasObject)serializer.Deserialize(sr);
			}
		}

		/// <summary>
		/// Deserializes a WasObject from an XML file.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static WasObject DeserializeFromFile(string path)
		{
			using (FileStream fs = new FileStream(path, FileMode.Open))
			{
				return Deserialize(fs);
			}
		}

		/// <summary>
		/// Sorts the Alarm objects and sets the Index properties.
		/// </summary>
		public void SetIndexes()
		{
			if (Alarms != null)
			{
				Alarms.Sort();
				for (int i = 0; i < Alarms.Count; i++)
				{
					Alarms[i].Index = i + 1;
				}
			}
		}

		protected bool Equals(WasObject other)
		{
			if (ReferenceEquals(null, other)) return false;
			bool alarms1IsNull = ReferenceEquals(Alarms, null);
			bool alarms2IsNull = ReferenceEquals(other.Alarms, null);
			if (alarms1IsNull && alarms2IsNull) return true;
			if (alarms1IsNull || alarms2IsNull) return false;
			return Alarms.SequenceEqual(other.Alarms);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((WasObject) obj);
		}

		public override int GetHashCode()
		{
			return Alarms == null ? 0 : Alarms.GetHashCode();
		}



		/// <summary>
		/// A StringWriter that uses the WAS encoding.
		/// </summary>
		class StringWriterWas : StringWriter
		{
			public override Encoding Encoding
			{
				get { return Constants.WasEncoding; }
			}
		}
	}
}
