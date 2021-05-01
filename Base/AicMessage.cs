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

using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Text;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class describes the XML document that is sent between AIC server and AIC client.
	/// </summary>
	[XmlType("AIC")]
	public class AicMessage
	{
		/// <summary>
		/// This is the name of the XML root tag for an AIC message.
		/// </summary>
		private const string XmlRootTagName = "AIC";

		/// <summary>
		/// The regular expression of the end tag of an AIC message.
		/// </summary>
		public const string XmlEndTagRegex = "</" + XmlRootTagName + ">|<" + XmlRootTagName + "[^>]*/>";

		// This must be a member variable otherwise it will consume a lot of memory.
		private static readonly XmlSerializer serializer = new XmlSerializer(typeof(AicMessage));


		/// <summary>
		/// The type of the AIC message.
		/// </summary>
		public MessageType MessageType { get; set; }

		/// <summary>
		/// Indicates whether the connection between WAS and AIC server is ok.
		/// </summary>
		public bool ConnectionWasToServerOk { get; set; }

		/// <summary>
		/// A number of alarms. This property never returns null.
		/// </summary>
		[XmlArray("Alarms")]
		[XmlArrayItem("Alarm")]
		public List<Alarm> Alarms
		{
			get { return mAlarms ?? (mAlarms = new List<Alarm>()); }
			set { mAlarms = value; }
		}
		private List<Alarm> mAlarms;



		/// <summary>
		/// Checks if two AicMessage objects have the same values.
		/// </summary>
		/// <param name="other">Another AicMessage.</param>
		/// <returns>True if the values are equal.</returns>
		public bool Equals(AicMessage other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return MessageType == other.MessageType &&
				   ConnectionWasToServerOk == other.ConnectionWasToServerOk &&
				   Alarms.SequenceEqual(other.Alarms);
		}

		/// <summary>
		/// Checks if two AicMessage objects have the same values.
		/// </summary>
		/// <param name="obj">Another object.</param>
		/// <returns>True if the values are the same.</returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((AicMessage)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Alarms.GetHashCode();
				hashCode = (hashCode * 397) ^ (int)MessageType;
				hashCode = (hashCode * 397) ^ ConnectionWasToServerOk.GetHashCode();
				return hashCode;
			}
		}

		/// <summary>
		/// Serializes an AIC message to a string.
		/// </summary>
		/// <returns>An XML string containg the AIC message.</returns>
		public string Serialize()
		{
			using (var sw = new StringWriter())
			{
				Serialize(sw);
				return sw.ToString();
			}
		}
			
		/// <summary>
		/// Serialize the AIC message as XML into a stream. The stream will not be closed.
		/// Throws an exception if serializing fails.
		/// </summary>
		/// <param name="stream">The stream for serialization.</param>
		public void Serialize(Stream stream)
		{
			serializer.Serialize(stream, this);
		}

		/// <summary>
		/// Serialize the AIC message as XML into a text writer. The writer will not be closed.
		/// Throws an exception if serializing fails.
		/// </summary>
		/// <param name="textWriter">The text writer for serialization.</param>
		public void Serialize(TextWriter textWriter)
		{
			serializer.Serialize(textWriter, this);
		}

		/// <summary>
		/// Serializes the AIC message and saves it to a file.
		/// </summary>
		/// <param name="path">The name of the file including the path.</param>
		public void Serialize(string path)
		{
			using (var fs = new FileStream(path, FileMode.Create))
			{
				Serialize(fs);
			}
		}

		/// <summary>
		/// Reads the AIC message from a stream. Throws an exception if deserializing fails.
		/// </summary>
		/// <param name="stream">A stream containing the XML.</param>
		/// <returns>An AIC message. Is never null.</returns>
		/// <remarks>This method does not close the stream.</remarks>
		public static AicMessage Deserialize(Stream stream)
		{
			return (AicMessage)serializer.Deserialize(stream);
		}

		/// <summary>
		/// Reads the AIC message from a string. Throws an exception if deserializing fails.
		/// </summary>
		/// <param name="textReader">A string containing the XML.</param>
		/// <returns>An AIC message. Is never null.</returns>
		/// <remarks>This method does not close the stream.</remarks>
		public static AicMessage Deserialize(TextReader textReader)
		{
			return (AicMessage)serializer.Deserialize(textReader);
		}

		/// <summary>
		/// Deserializes an XML string with an AIC message.
		/// </summary>
		/// <param name="xml">The XML string containing the AIC message.</param>
		/// <returns>An AIC message.</returns>
		public static AicMessage Deserialize(string xml)
		{
			using (var sr = new StringReader(xml))
			{
				return Deserialize(sr);
			}
		}

		/// <summary>
		/// Reads the AIC message from a file.
		/// </summary>
		/// <param name="path">The path including the filename.</param>
		/// <returns>An AIC message. Is never null.</returns>
		public static AicMessage DeserializeFromFile(string path)
		{
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				return Deserialize(fs);
			}
		}

		/// <summary>
		/// Reads the AIC message from a file.
		/// </summary>
		/// <param name="path">The path including the filename.</param>
		/// <returns>An AIC message. Null if reading fails.</returns>
		public static AicMessage TryDeserializeFromFile(string path)
		{
			AicMessage aicMessage;
			try
			{
				aicMessage = DeserializeFromFile(path);
			}
			catch
			{
				aicMessage = null;
			}
			return aicMessage;
		}
	}



	/// <summary>
	/// The type of a network message.
	/// </summary>
	public enum MessageType
	{
		Undefined,
		Request,
		Response,
        KeepAlive
	}
}
