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
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Media;

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// This class contains data for the Info-Center.
	/// </summary>
	[XmlRoot(Namespace = "", IsNullable = false)]
	public class InfoCenterData
	{
		private static readonly XmlSerializer serializer = new XmlSerializer(typeof(InfoCenterData), new XmlRootAttribute("InfoCenter"));

		/// <summary>
		/// The pages shown in the Info-Center.
		/// </summary>
		[XmlElement("Page")]
		public List<Page> Pages { get; set; }

		/// <summary>
		/// Deserializes the InfoCenterData from the provided stream.
		/// </summary>
		/// <param name="stream">An XML reader stream.</param>
		/// <returns>Returns the deserialized data or throws an exception if something goes wrong.</returns>
		public static InfoCenterData Deserialize(XmlReader stream)
		{
			return serializer.Deserialize(stream) as InfoCenterData;
		}
	}

	/// <summary>
	/// A single page that is shown in the Info-Center.
	/// </summary>
	[XmlRoot(Namespace = "", IsNullable = false)]
	public class Page
	{
		/// <summary>
		/// Initializes a new instance of the Page class.
		/// </summary>
		public Page()
		{
			Type = PageType.Default;
		}

		/// <summary>
		/// The title of the page.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// The url to an image.
		/// </summary>
		[XmlIgnore]
		public string ImageUrl
		{
			get { return Item as string; }
		}

		/// <summary>
		/// The text shown on the page.
		/// </summary>
		[XmlIgnore]
		public Text Text
		{
			get { return Item as Text; }
		}

		/// <summary>
		/// The item is either the ImageUrl or the Text.
		/// </summary>
		[XmlElement("ImageUrl", typeof(string)), XmlElement("Text", typeof(Text))]
		public object Item { get; set; }

		/// <summary>
		/// The text in the footer on the left side.
		/// </summary>
		public string FooterLeft { get; set; }

		/// <summary>
		/// The text in the footer on the right side.
		/// </summary>
		public string FooterRight { get; set; }

		/// <summary>
		/// The type of the page.
		/// </summary>
		[XmlAttribute, DefaultValue(PageType.Default)]
		public PageType Type { get; set; }
	}

	/// <summary>
	/// The text that is shown on a page.
	/// </summary>
	[XmlRoot(Namespace = "", IsNullable = false)]
	public class Text
	{
		/// <summary>
		/// The first line of the text.
		/// </summary>
		public TextTitle TextTitle { get; set; }

		/// <summary>
		/// A number of text lines.
		/// </summary>
		[XmlElement("TextLine")]
		public List<string> TextLine { get; set; }

		/// <summary>
		/// Indicates whether the text should be displayed as big as possible.
		/// </summary>
		[XmlAttribute]
		public bool UseMaxTextSize { get; set; }

		/// <summary>
		/// Indicates whether the text should be left-aligned.
		/// </summary>
		[XmlAttribute]
		public bool LeftAligned { get; set; }
	}

	/// <summary>
	/// This class describes the title of the text section.
	/// </summary>
	public class TextTitle
	{
		/// <summary>
		/// Initializes a new instance of the TextTitle class.
		/// </summary>
		public TextTitle()
		{ }

		/// <summary>
		/// Initializes a new instance of the TextTitle class.
		/// </summary>
		/// <param name="title">The text of the TextTitle.</param>
		public TextTitle(string title)
		{
			Value = title;
		}

		/// <summary>
		/// The first line of the text.
		/// </summary>
		[XmlText]
		public string Value { get; set; }

		/// <summary>
		/// The color of the TextTitle. Default is white.
		/// </summary>
		[XmlAttribute]
		public string Color { get; set; }

		/// <summary>
		/// The color of the TextTitle. Default is white.
		/// </summary>
		[XmlIgnore]
		public Color ColorAsColor
		{
			get
			{
				var color = Colors.White;
				try
				{
					if (!string.IsNullOrWhiteSpace(Color))
					{
						BrushConverter bc = new BrushConverter();
						SolidColorBrush colorBrush = bc.ConvertFrom(Color) as SolidColorBrush;
						if (colorBrush != null)
						{
							color = colorBrush.Color;
						}
					}
				}
				catch
				{
					color = Colors.White;
				}
				return color;
			}
		}
	}

	/// <summary>
	/// The type of a info page.
	/// </summary>
	public enum PageType
	{
		Default,
		Clock,
		Time,
		Date,
		Weather,
		Status
	}
}