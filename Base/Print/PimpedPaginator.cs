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

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This paginator provides document headers, footers and repeating table headers 
	/// </summary>
	public class PimpedPaginator : DocumentPaginator
	{
		private readonly DocumentPaginator mPaginator;
		private readonly Definition mDefinition;

		public PimpedPaginator(FlowDocument document, Definition def)
		{
			// Create a copy of the flow document,  so we can modify it without modifying the original.
			mPaginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
			mDefinition = def;
			mPaginator.PageSize = def.ContentSize;

			// Change page size of the document to the size of the content area
			document.ColumnWidth = double.MaxValue; // Prevent columns
			document.PageWidth = mDefinition.ContentSize.Width;
			document.PageHeight = mDefinition.ContentSize.Height;
			document.PagePadding = new Thickness(0);
		}

		public override DocumentPage GetPage(int pageNumber)
		{
			// Use default paginator to handle pagination
			Visual originalPage = mPaginator.GetPage(pageNumber).Visual;

			ContainerVisual visual = new ContainerVisual();
			ContainerVisual pageVisual = new ContainerVisual();
			pageVisual.Transform = new TranslateTransform(mDefinition.ContentOrigin.X, mDefinition.ContentOrigin.Y);
			pageVisual.Children.Add(originalPage);
			visual.Children.Add(pageVisual);

			// Create headers and footers
			if (mDefinition.Header != null)
			{
				visual.Children.Add(CreateHeaderFooterVisual(mDefinition.Header, mDefinition.HeaderRect, pageNumber));
			}
			if (mDefinition.Footer != null)
			{
				visual.Children.Add(CreateHeaderFooterVisual(mDefinition.Footer, mDefinition.FooterRect, pageNumber));
			}

			return new DocumentPage(visual, mDefinition.PageSize, new Rect(new Point(), mDefinition.PageSize), new Rect(mDefinition.ContentOrigin, mDefinition.ContentSize));
		}

		/// <summary>
		/// Creates a visual to draw the header/footer
		/// </summary>
		/// <param name="draw"></param>
		/// <param name="bounds"></param>
		/// <param name="pageNumber"></param>
		/// <returns></returns>
		private static Visual CreateHeaderFooterVisual(DrawHeaderFooter draw, Rect bounds, int pageNumber)
		{
			DrawingVisual visual = new DrawingVisual();
			using (DrawingContext context = visual.RenderOpen())
			{
				draw(context, bounds, pageNumber);
			}
			return visual;
		}

		#region DocumentPaginator members

		public override bool IsPageCountValid
		{
			get { return mPaginator.IsPageCountValid; }
		}

		public override int PageCount
		{
			get { return mPaginator.PageCount; }
		}

		public override Size PageSize
		{
			get
			{
				return mPaginator.PageSize;
			}
			set
			{
				mPaginator.PageSize = value;
			}
		}

		public override IDocumentPaginatorSource Source
		{
			get { return mPaginator.Source; }
		}

		#endregion

		public class Definition
		{
			#region Page sizes

			/// <summary>
			/// PageSize in DIUs
			/// </summary>
			public Size PageSize
			{
				get { return _PageSize; }
				set { _PageSize = value; }
			}
			private Size _PageSize = new Size(793.5987, 1122.3987); // Default: A4

			/// <summary>
			/// Margins
			/// </summary>
			public Thickness Margins
			{
				get { return _Margins; }
				set { _Margins = value; }
			}
			private Thickness _Margins = new Thickness(96); // Default: 1" margins


			/// <summary>
			/// Space reserved for the header in DIUs
			/// </summary>
			public double HeaderHeight { get; set; }

			/// <summary>
			/// Space reserved for the footer in DIUs
			/// </summary>
			public double FooterHeight { get; set; }

			#endregion

			public DrawHeaderFooter Header;
			public DrawHeaderFooter Footer;

			#region Some convenient helper properties

			internal Size ContentSize
			{
				get
				{
					//return PageSize.Subtract(new Size(Margins.Left + Margins.Right,Margins.Top + Margins.Bottom + HeaderHeight + FooterHeight));
					return new Size(PageSize.Width - Margins.Left - Margins.Right, PageSize.Height - Margins.Top - Margins.Bottom - HeaderHeight - FooterHeight);
				}
			}

			internal Point ContentOrigin
			{
				get { return new Point(Margins.Left, Margins.Top + HeaderRect.Height); }
			}

			internal Rect HeaderRect
			{
				get { return new Rect(Margins.Left, Margins.Top, ContentSize.Width, HeaderHeight); }
			}

			internal Rect FooterRect
			{
				get { return new Rect(Margins.Left, ContentOrigin.Y + ContentSize.Height, ContentSize.Width, FooterHeight); }
			}

			#endregion
		}

		/// <summary>
		/// Allows drawing headers and footers
		/// </summary>
		/// <param name="context">This is the drawing context that should be used</param>
		/// <param name="bounds">The bounds of the header. You can ignore these at your own peril</param>
		/// <param name="pageNr">The page nr (0-based)</param>
		public delegate void DrawHeaderFooter(DrawingContext context, Rect bounds, int pageNr);

	}
}
