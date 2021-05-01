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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class represents the alarm printout.
	/// </summary>
	public class Printout
	{
		private const int WaterSupplyPointDistanceThreshold = 1000;			// The maximum distance in meters for the first water supply point

		private readonly Alarm mAlarm;
		private readonly FlowDocument mDocument;
		private Coordinate mCoordinate;
		private readonly Coordinate mFireBrigadeCoordinate;

		/// <summary>
		/// Initializes a new instance of the Printout class.
		/// </summary>
		/// <param name="alarm">The alarm to print.</param>
		/// <param name="fireBrigadeCoordinate">The coordinate of the fire brigade as start point for the route.</param>
		public Printout(Alarm alarm, Coordinate fireBrigadeCoordinate)
		{
			mAlarm = alarm;
			mFireBrigadeCoordinate = fireBrigadeCoordinate;
			mDocument = new FlowDocument();
		}

		/// <summary>
		/// Create the document for printing.
		/// </summary>
		public FlowDocument CreatePrintout()
		{
			Image locationMap = null;
			Image waterMap = null;
			Image detailMap = null;
			List<WaterSupplyPoint> waterSupplyPoints = null;

			#region Download all the data for the printing

			try
			{
				// Check if the alarm location is valid (it must not be the same as the center of the city)
				var postalCodeAndCity = MapHelper.GetPostalCodeAndCity(AicSettings.Global.FireBrigadeName);
				bool destinationIsValid = MapHelper.IsDestinationValid(mAlarm.MapLocation, postalCodeAndCity.Item1, postalCodeAndCity.Item2);

				if (destinationIsValid)
				{
					mCoordinate = MapHelper.Geocode(mAlarm.MapLocation); // Try to get the coordinate of the alarm location

					if (mCoordinate != null || mFireBrigadeCoordinate == null)
					{
						try
						{
							// Location map
							locationMap = CreateMapImage();
						}
						catch (Exception ex)
						{
							Log.GetInstance().LogError("Error in downloading location map data.", ex);
						}

						// Water/Detail map
						try
						{
							// Show the water map if water supply points are found and the nearest one is not more than 2000 m away
							// otherwise a detail map is shown
							if (AicSettings.Global.WaterMapMode != 0)
							{
								bool useWassserkarteInfo = AicSettings.Global.WaterMapMode == 1;
								var waterMapQuery = new WaterMapQuery(useWassserkarteInfo, AicSettings.Global.WasserkarteInfoToken, AicSettings.Global.WaterMapApiUrl);
								waterSupplyPoints = waterMapQuery.FindWaterSupplyPoints(mCoordinate);
								if (waterSupplyPoints.Count > 0)
								{
									waterMap = CreateMapImage(waterSupplyPoints);
								}
							}

							if (waterMap == null)
							{
								detailMap = CreateMapImage(null, true);
							}
						}
						catch (Exception ex)
						{
							Log.GetInstance().LogError("Error in downloading water map data.", ex);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.GetInstance().LogError("Error in downloading map data.", ex);
			}

			#endregion

			#region Document

			mDocument.PagePadding = new Thickness(2 * 96 / 2.54);		// 2 cm
			mDocument.FontFamily = new FontFamily("Arial");
			mDocument.PageWidth = 8.27 * 96;
			mDocument.PageHeight = 11.69 * 96;
			mDocument.ColumnWidth = mDocument.PageWidth;
			mDocument.LineHeight = 10;

			#endregion

			#region Title

			AddParagraph("Alarmausdruck - FF " + AicSettings.Global.FireBrigadeName, 32, true, false, TextAlignment.Center);
			mDocument.Blocks.Add(new Paragraph(new LineBreak { FontSize = 2 }));

			#endregion

			#region Subject, location proposition, location, and additional information

			const int infoFontSize = 24;
			AddParagraph(mAlarm.Subject, infoFontSize);
			if (!string.IsNullOrWhiteSpace(mAlarm.LocationProposition))
			{
				AddParagraph(mAlarm.LocationProposition, infoFontSize, false, true);
			}
			else
			{
				AddParagraph(mAlarm.Location, infoFontSize);
			}
			AddParagraph(mAlarm.AdditionalInformation, infoFontSize);

			#endregion

			AddHorizontalLine();

			#region Info table

			TableColumn col1 = new TableColumn();
			TableColumn col2 = new TableColumn();
			col1.Width = new GridLength(220);
			col2.Width = GridLength.Auto;
			Table table = new Table();
			table.Columns.Add(col1);
			table.Columns.Add(col2);
			table.FontSize = 16;

			string fireBrigades = "";
			if (mAlarm.FireBrigades.Count > 0)
			{
				fireBrigades += mAlarm.FireBrigades.Aggregate((x, y) => x + ", " + y);
			}
			table.RowGroups.Add(new TableRowGroup());
			table.RowGroups[0].Rows.Add(new TableRow());
			table.RowGroups[0].Rows.Add(new TableRow());
			table.RowGroups[0].Rows.Add(new TableRow());
			table.RowGroups[0].Rows[0].Cells.Add(new TableCell(new Paragraph(new Run(mAlarm.StartTime.ToShortDateString() + ", " + mAlarm.StartTime.ToLongTimeString() + " Uhr"))));
			table.RowGroups[0].Rows[0].Cells.Add(new TableCell(new Paragraph(new Run(mAlarm.Caller))));
			table.RowGroups[0].Rows[1].Cells.Add(new TableCell(new Paragraph(new Run(mAlarm.AlarmStation + " (Alarmstufe " + mAlarm.AlarmLevel + ")"))));
			table.RowGroups[0].Rows[1].Cells.Add(new TableCell(new Paragraph(new Run("Sirenenprogramm: " + mAlarm.SirenProgram))));
			var cell = new TableCell(new Paragraph(new Run(fireBrigades)));
			cell.ColumnSpan = 2;
			table.RowGroups[0].Rows[2].Cells.Add(cell);
			mDocument.Blocks.Add(table);

			#endregion

			#region Location map

			if (locationMap != null)
			{
				AddHorizontalLine(); // Horizontal line
				try
				{
					mDocument.Blocks.Add(new BlockUIContainer(locationMap) { Padding = new Thickness(0, 20, 0, 0) });
				}
				catch (Exception ex)
				{
					Log.GetInstance().LogError("Error in creating the image for the printout.", ex);
				}
			}

			#endregion

			#region Water map

			if (waterMap != null)
			{
				Table waterTable = CreateWaterSupplyPointTable(waterSupplyPoints);
				AddMapPage("Wasserkarte - " + mAlarm.MapLocation, waterMap, waterTable);
			}

			#endregion

			#region Detail map

			if (detailMap != null)
			{
				AddMapPage("Detailkarte - " + mAlarm.MapLocation, detailMap);
			}

			#endregion

			#region Footer

			//TableColumn footerCol1 = new TableColumn();
			//TableColumn footerCol2 = new TableColumn();
			//Table footerTable = new Table();
			//footerTable.Columns.Add(footerCol1);
			//footerTable.Columns.Add(footerCol2);
			//footerTable.FontSize = 12;
			//footerTable.RowGroups.Add(new TableRowGroup());
			//footerTable.RowGroups[0].Rows.Add(new TableRow());
			//footerTable.RowGroups[0].Rows[0].Cells.Add(new TableCell((new Paragraph(new Run(alarm.Id)))));
			//footerTable.RowGroups[0].Rows[0].Cells.Add(new TableCell((new Paragraph(new Run("AIC FF Marchtrenk")) { TextAlignment = TextAlignment.Right})));
			//doc.Blocks.Add(footerTable);

			#endregion

			// Create XPS, just for testing
			//string filename = @"C:\Users\Thomas\Downloads\" + alarm.Id + ".xps";
			//XpsDocument xpsDoc = new XpsDocument(filename, FileAccess.Write);
			//XpsDocumentWriter xpsWriter = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
			//xpsWriter.Write((doc as IDocumentPaginatorSource).DocumentPaginator);
			//xpsDoc.Close();

			return mDocument;
		}

		/// <summary>
		/// Tries to download a (water) map for the provided location.
		/// </summary>
		/// <param name="waterSupplyPoints">A list of water supply points ordered by distance to the start point.</param>
		/// <param name="showDetailMap">If true it shows only the coordinate at zoom factor 15.</param>
		/// <returns>A stream containing the image, null if downloading failed.</returns>
		private Image CreateMapImage(IList<WaterSupplyPoint> waterSupplyPoints = null, bool showDetailMap = false)
		{
			// Create Google Static Maps url
			int imageWidth = showDetailMap ? 500 : 640;
			int imageHeight = showDetailMap ? 640 : 560;
			string url = Constants.GoogleStaticMapsUrl + "?sensor=false&scale=2&size=" + imageWidth + "x" + imageHeight;

			// Add the map type
			GoogleMapType mapType;
			if (waterSupplyPoints == null && !showDetailMap)
			{
				mapType = AicSettings.Global.RouteMapType;
			}
			else if (waterSupplyPoints != null)
			{
				mapType = AicSettings.Global.WaterMapType;
			}
			else
			{
				mapType = AicSettings.Global.DetailMapType;
			}
			url += "&maptype=" + MapHelper.GetMapTypeString(mapType);

			// Add the route path
			string path = MapHelper.GetPathString(mFireBrigadeCoordinate, mCoordinate);
			if (!string.IsNullOrWhiteSpace(path))
			{
				//path = fireBrigadeCoordinate + "|" + mCoordinate.ToQueryString();
				url += "&path=enc:" + path;
			}
			
			// Add the fire brigade marker
			string fireBrigadeCoordinate = mFireBrigadeCoordinate.ToQueryString();
			url += "&markers=icon:" + Constants.FireBrigadeMarkerUrl + "|" + fireBrigadeCoordinate;

			// Add the alarm location marker
			url += "&markers=icon:" + Constants.AlarmLocationMarkerUrl + "|" + mCoordinate.ToQueryString();
			
			if (showDetailMap)
			{
				url += "&zoom=15&center=" + mCoordinate.ToQueryString();
			}
			else if (waterSupplyPoints != null && waterSupplyPoints.Count > 0)
			{
				var coordinates = waterSupplyPoints.Select(x => x.Coordinate).ToList();
				coordinates.Add(mCoordinate);

				// Calculate a suitable zoom level
				int zoom = MapHelper.CalculateZoom(coordinates, imageWidth, imageHeight);
				if (zoom > 16)
				{
					zoom = 16;
				}
				else if (zoom < 14)
				{
					zoom = 14;
				}
				var center = MapHelper.CalculateCenter(coordinates);

				url += "&zoom=" + zoom + "&center=" + center.ToQueryString();
				for (int i = 0; i < waterSupplyPoints.Count; i++)
				{
					var point = waterSupplyPoints[i];
					url += "&markers=color:blue|label:" + (i + 1) + "|" + point.Coordinate.ToQueryString();
				}
			}


			

			// Try to download the image from Google Maps using defined web timeouts
			MemoryStream ms = null;
			try
			{
				ms = Network.DownloadStreamData(url, Constants.NetworkTimeouts);
			}
			catch (WebException exc)
			{
				string timeout = "-1";
				if (exc.Data.Contains("Timeout"))
				{
					timeout = exc.Data["Timeout"].ToString();
				}
				Log.GetInstance().LogError("Error in downloading image after " + timeout + " milliseconds.", exc);
			}
			catch (Exception exc)
			{
				Log.GetInstance().LogError("General error in downloading image. ", exc);
			}

			return CreateImage(ms);
		}

		/// <summary>
		/// Creates an image from a stream that can be added to a FlowDocument. The stream must not be closed until the document has been printed.
		/// </summary>
		/// <param name="imageStream">An image stream.</param>
		/// <returns>The image.</returns>
		private static Image CreateImage(Stream imageStream)
		{
			// Create an image using the stream
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = imageStream;
			bitmapImage.EndInit();

			Image image = new Image();
			image.Source = bitmapImage;

			// This approach always prints the firste image that has been created
			//image.Source = BitmapFrame.Create(imageStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

			return image;
		}

		/// <summary>
		/// Creates a page with a title, image, and a final block element.
		/// </summary>
		/// <param name="title">The title of the document.</param>
		/// <param name="image">The image that should be displayed.</param>
		/// <param name="table">The final block element of the page (e.g. a table).</param>
		private void AddMapPage(string title, UIElement image, Block table = null)
		{
			// Map title and location
			Paragraph waterMapTitle = new Paragraph(new Bold(new Run(title)));
			waterMapTitle.FontSize = 24;
			waterMapTitle.TextAlignment = TextAlignment.Center;
			waterMapTitle.BreakPageBefore = true;
			mDocument.Blocks.Add(waterMapTitle);

			// Coordinate text
			AddParagraph("(" + mCoordinate + ")", 18, false, false, TextAlignment.Center);

			// Some space to the next element
			//doc.Blocks.Add(new Paragraph(new LineBreak { FontSize = 2 }));

			// Add the map
			if (image != null)
			{
				try
				{
					mDocument.Blocks.Add(new BlockUIContainer(image));
				}
				catch (Exception ex)
				{
					Log.GetInstance().LogError("Error in adding the map image to the printout.", ex);
				}
			}

			// Add the table
			if (table != null)
			{
				mDocument.Blocks.Add(table);
			}
		}

		/// <summary>
		/// Creates a table containing water supply points.
		/// </summary>
		/// <param name="waterSupplyPoints">A number of water supply points ordered by distance to the alarm location.</param>
		/// <returns>A table containing water supply points.</returns>
		private Table CreateWaterSupplyPointTable(List<WaterSupplyPoint> waterSupplyPoints)
		{
			// Create table
			Table table = new Table();
			table.BorderThickness = new Thickness(2);
			table.BorderBrush = Brushes.Black;
			table.FontSize = 14;

			// Create columns
			const int colLineWidth = 1;
			const int colCount = 5;
			Brush colLineBrush = Brushes.Black;
			Thickness colLineThickness = new Thickness(colLineWidth, 0, 0, 0);
			List<TableColumn> dataCols = new List<TableColumn>();
			List<TableColumn> borderCols = new List<TableColumn>();
			for (int i = 0; i < colCount; i++)
			{
				if (i > 0)
				{
					TableColumn borderCol = new TableColumn();
					borderCol.Width = new GridLength(colLineWidth);
					borderCols.Add(borderCol);
					table.Columns.Add(borderCol);
				}

				TableColumn dataCol = new TableColumn();
				dataCols.Add(dataCol);
				table.Columns.Add(dataCol);
			}
			Debug.Assert(dataCols.Count == 5);

			// Set width of each column
			dataCols[0].Width = new GridLength(20);
			dataCols[1].Width = new GridLength(140);
			dataCols[2].Width = new GridLength(190);
			dataCols[3].Width = new GridLength(190);
			dataCols[4].Width = GridLength.Auto;

			// Set headers
			TableRowGroup headerGroup = new TableRowGroup();
			table.RowGroups.Add(headerGroup);
			headerGroup.Foreground = Brushes.White;
			headerGroup.Background = Brushes.DarkGray;
			headerGroup.Rows.Add(new TableRow());
			TableCellCollection headerCells = headerGroup.Rows[0].Cells;
			headerCells.Add(new TableCell(new Paragraph(new Bold(new Run("#")))) { TextAlignment = TextAlignment.Center });
			headerCells.Add(new TableCell());
			headerCells.Add(new TableCell(new Paragraph(new Bold(new Run("Typ")))) { TextAlignment = TextAlignment.Center });
			headerCells.Add(new TableCell());
			headerCells.Add(new TableCell(new Paragraph(new Bold(new Run("Ort")))) { TextAlignment = TextAlignment.Center });
			headerCells.Add(new TableCell());
			headerCells.Add(new TableCell(new Paragraph(new Bold(new Run("Info")))) { TextAlignment = TextAlignment.Center });
			headerCells.Add(new TableCell());
			headerCells.Add(new TableCell(new Paragraph(new Bold(new Run("Entfernung")))) { TextAlignment = TextAlignment.Center });

			// Draw a horizontal line below the header row
			//TableCell headerLineCell = new TableCell
			//                            {
			//                                BorderBrush = Brushes.Black,
			//                                BorderThickness = new Thickness(0, 0, 0, 1),
			//                                ColumnSpan = table.Columns.Count,
			//                                FontSize = 0.1
			//                            };
			//headerGroup.Rows[1].Cells.Add(headerLineCell);

			// Put data into the table
			table.RowGroups.Add(new TableRowGroup());
			int index = 0;

			foreach (var point in waterSupplyPoints)
			{
				// Calculate bearing
				int bearing = (int)Math.Round(Coordinate.CalcuateBearing(mCoordinate, point.Coordinate));
				CardinalDirection dir = Coordinate.GetCardinalDirection(bearing);
				char arrow = Coordinate.GetArrowSymbol(dir);

				// Create info text
				string info = string.Empty;
				if (point.Kind != WaterSupplyPointKind.Undefined)
				{
					if (point.Size > 0)
					{
						if (point.Kind == WaterSupplyPointKind.FireExtinguishingPool || point.Kind == WaterSupplyPointKind.SuctionPoint)
						{
							info = point.Size + " m\u00B3";		// e.g. 80 m³
						}
						else if (point.Kind == WaterSupplyPointKind.PillarHydrant || point.Kind == WaterSupplyPointKind.UndergroundHydrant)
						{
							info = "\u2300 " + point.Size + " mm";		// e.g. ø 50 mm
						}
					}
					if (!string.IsNullOrWhiteSpace(point.Connections))
					{
						if (info.Length > 0)
						{
							info += ", ";
						}
						info += point.Connections;
					}
				}
				if (!string.IsNullOrWhiteSpace(point.Information))
				{
					if (info.Length > 0)
					{
						info += Environment.NewLine;
					}
					info += point.Information;
				}

				// Add new row
				TableRowCollection rows = table.RowGroups.Last().Rows;
				rows.Add(new TableRow());
				if (index % 2 == 1)
				{
					rows[index].Background = Brushes.LightGray;
				}
				TableCellCollection cells = rows[index].Cells;

				// Write data and draw lines
				cells.Add(new TableCell(new Paragraph(new Run((index + 1).ToString(CultureInfo.InvariantCulture)))));
				if (index == 0)
				{
					cells.Add(CreateInnerTableCell(colLineBrush, colLineThickness, waterSupplyPoints.Count));
				}
				cells.Add(new TableCell(new Paragraph(new Run(point.GetKindText()))));
				if (index == 0)
				{
					cells.Add(CreateInnerTableCell(colLineBrush, colLineThickness, waterSupplyPoints.Count));
				}
				cells.Add(new TableCell(new Paragraph(new Run(point.Location))));
				if (index == 0)
				{
					cells.Add(CreateInnerTableCell(colLineBrush, colLineThickness, waterSupplyPoints.Count));
				}
				cells.Add(new TableCell(new Paragraph(new Run(info))));
				if (index == 0)
				{
					cells.Add(CreateInnerTableCell(colLineBrush, colLineThickness, waterSupplyPoints.Count));
				}
				cells.Add(new TableCell(new Paragraph(new Run(arrow + " " + point.Distance + " m"))));

				index++;
			}

			foreach (TableCell cell in table.RowGroups.Last().Rows.SelectMany(row => row.Cells))
			{
				cell.Padding = new Thickness(2);
				cell.TextAlignment = TextAlignment.Left;
			}

			return table;
		}

		// Creates a table cell with the defined settings
		private static TableCell CreateInnerTableCell(Brush brush, Thickness thickness, int rowSpan)
		{
			return new TableCell { BorderBrush = brush, BorderThickness = thickness, RowSpan = rowSpan };
		}

		// Adds a paragraph to the document with the defined settings
		private void AddParagraph(string text, int fontSize, bool bold = false, bool italic = false, TextAlignment alignment = TextAlignment.Left)
		{
			Inline inline = new Run(text);
			if (bold)
			{
				inline = new Bold(inline);
			}
			if (italic)
			{
				inline = new Italic(inline);
			}
			Paragraph paragraph = new Paragraph(inline);
			paragraph.FontSize = fontSize;
			paragraph.TextAlignment = alignment;
			mDocument.Blocks.Add(paragraph);
		}

		// Adds a horizontal line to the document
		private void AddHorizontalLine()
		{
			double pageWidth = mDocument.PageWidth - mDocument.PagePadding.Left - mDocument.PagePadding.Right;
			try
			{
				Line line = new Line();
				line.Stroke = Brushes.DarkGray;
				line.StrokeThickness = 2;
				line.Height = 10;
				line.StrokeStartLineCap = PenLineCap.Round;
				line.StrokeEndLineCap = PenLineCap.Round;
				line.X1 = 4;
				line.X2 = pageWidth - 4;
				line.Y1 = line.Height / 2;
				line.Y2 = line.Height / 2;
				BlockUIContainer container = new BlockUIContainer(line);
				mDocument.Blocks.Add(container);
			}
			catch (Exception ex)
			{
				Log.GetInstance().LogError("Error in creating horizontal line for the printout.", ex);
			}
		}
	}
}
