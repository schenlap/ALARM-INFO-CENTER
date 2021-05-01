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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// Interaction logic for InfoControl.xaml
	/// </summary>
	public partial class InfoControl
	{
		private readonly DispatcherTimer mTimer = new DispatcherTimer();
		private readonly double mTextPanelFontSize;
		private int mPageIndex;
		private InfoCenterData mInfoCenterData;
		private WeatherData mWeatherData;
		private Dictionary<string, ImageSource> mUrl2Image = new Dictionary<string, ImageSource>();
		private readonly BackgroundWorker mLoadDataWorker = new BackgroundWorker();



		/// <summary>
		/// Assign a logging method to this delegate for logging informations.
		/// </summary>
		public Action<string> LogInformation;

		/// <summary>
		/// Assign a logging method to this delegate for logging warnings.
		/// </summary>
		public Action<string> LogWarning;

		/// <summary>
		/// Assign a logging method to this delegate for logging errors.
		/// </summary>
		public Action<string, Exception> LogError;

		/// <summary>
		/// Indicates whether the InfoCenter is currently running.
		/// </summary>
		[Browsable(false)]
		public bool IsActive
		{
			get { return mTimer.IsEnabled; }
		}

		/// <summary>
		/// Sets the time each page is displayed.
		/// </summary>
		[Browsable(false)]
		public int PageDisplayDuration
		{
			set
			{
				if (value > 0)
				{
					mTimer.Interval = new TimeSpan(0, 0, value);
				}
			}
		}

		/// <summary>
		/// Get the status of the Info-Center.
		/// </summary>
		public Status Status
		{
			get
			{
				var status = Status.Stopped;
				if (mLoadDataWorker.IsBusy)
				{
					status = Status.LoadingData;
				}
				if (mTimer.IsEnabled)
				{
					status = Status.Running;
				}
				return status;
			}
		}

		/// <summary>
		/// Occurs whenever the status of the InfoCenter has changed.
		/// </summary>
		public event EventHandler<StatusChangedEventArgs> StatusChanged;

		/// <summary>
		/// Raises the StatusChanged event by using the status provided.
		/// </summary>
		/// <param name="status"></param>
		protected void OnStatusChanged(Status status)
		{
			if (StatusChanged != null)
			{
				StatusChanged(this, new StatusChangedEventArgs(status));
			}
		}



		/// <summary>
		/// Initializes a new instance of the InfoControl class.
		/// </summary>
		public InfoControl()
		{
			InitializeComponent();

			mLoadDataWorker.WorkerSupportsCancellation = true;
			mLoadDataWorker.DoWork += loadWorker_DoWork;
			mLoadDataWorker.RunWorkerCompleted += loadWorker_RunWorkerCompleted;

			// One hour timer. Reload data every hour
			var oneHourTimer = new DispatcherTimer();
			oneHourTimer.Interval = new TimeSpan(1, 0, 0);
			oneHourTimer.Tick += (sender, e) => { if (IsActive) LoadData(); };
			oneHourTimer.Start();

			mTextPanelFontSize = SystemParameters.PrimaryScreenHeight / 8;
			mTimer.Interval = new TimeSpan(0, 0, 8);
			mTimer.Tick += (sender, e) => ShowNextPage();
		}

		/// <summary>
		/// Stops the InfoCenter and clears the data.
		/// </summary>
		public void Stop(bool sendEvent = true)
		{
			mLoadDataWorker.CancelAsync();
			mTimer.Stop();
			mInfoCenterData = null;
			mWeatherData = null;
			mUrl2Image.Clear();
			if (sendEvent)
			{
				OnStatusChanged(Status.Stopped);
			}
		}

		/// <summary>
		/// Resets the timer (the current page is shown the full time). This is useful when navigating manually through the pages.
		/// </summary>
		public void ResetTimer()
		{
			bool startTimer = mTimer.IsEnabled;
			mTimer.Stop();
			if (startTimer)
			{
				mTimer.Start();
			}
		}

		/// <summary>
		/// Loads the data used for the InfoCenter and starts the InfoCenter if reading succeeds.
		/// </summary>
		/// <returns>True if no exception occured otherwise false.</returns>
		/// <remarks>This method can be called in order to refresh the data when InfoCenter is active.</remarks>
		public void LoadData()
		{
			if (!mLoadDataWorker.IsBusy)
			{
				OnStatusChanged(Status.LoadingData);
				mLoadDataWorker.RunWorkerAsync();
			}
		}

		// Download data from web
		private void loadWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			var worker = (BackgroundWorker)sender;
			var data = new WebData();
			using (var reader = XmlReader.Create(AicSettings.Global.InfoCenterDataUrl))
			{
				data.InfoCenterData = InfoCenterData.Deserialize(reader);
			}
			if (worker.CancellationPending)
			{
				e.Cancel = true;
				return;
			}
			if (AicSettings.Global.WeatherEnabled)
			{
				data.WeatherData = ReadWeatherData();
			}
			if (worker.CancellationPending)
			{
				e.Cancel = true;
				return;
			}

			data.Url2Image = new Dictionary<string, ImageSource>();
			foreach (var page in data.InfoCenterData.Pages)
			{
				if (!string.IsNullOrWhiteSpace(page.ImageUrl))
				{
					//BitmapImage bitmapImage = new BitmapImage(new Uri(page.ImageUrl));
					try
					{
						BitmapImage bitmapImage = new BitmapImage();
						bitmapImage.BeginInit();
						bitmapImage.StreamSource = Network.DownloadStreamData(page.ImageUrl);
						bitmapImage.EndInit();
						bitmapImage.Freeze();				// Freezing is very important because the data is given to another thread
						data.Url2Image[page.ImageUrl] = bitmapImage;
					}
					catch
					{
						data.Url2Image[page.ImageUrl] = null;
					}
					if (worker.CancellationPending)
					{
						e.Cancel = true;
						return;
					}
				}
			}

			// Remove pages where the image could not be downloaded
			data.InfoCenterData.Pages.RemoveAll(x => !string.IsNullOrWhiteSpace(x.ImageUrl) && data.Url2Image[x.ImageUrl] == null);

			if (worker.CancellationPending)
			{
				e.Cancel = true;
				return;
			}

			e.Result = data;
		}

		// Starts the Info-Center after downloading the data
		private void loadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled)
			{
				Stop();
			}
			else if (e.Error != null)
			{
				Stop();
				LogError("Error at loading data for Info-Center.", e.Error);
			}
			else if (e.Result is WebData)
			{
				Stop(false);
				mPageIndex = 0;

				var webData = e.Result as WebData;
				mInfoCenterData = webData.InfoCenterData;
				mUrl2Image = webData.Url2Image;
				mWeatherData = webData.WeatherData;

				if (mInfoCenterData != null && mInfoCenterData.Pages.Count > 0)
				{
					ShowPage();		// Start the timer immediately
					mTimer.Start();
					OnStatusChanged(Status.Running);
				}
			}
		}

		/// <summary>
		/// Reads weather data from wetter.com.
		/// </summary>
		/// <returns>Current weather data of null if reading failed.</returns>
		private WeatherData ReadWeatherData()
		{
			var weatherData = new WeatherData();
			XmlReader reader = null;

			try
			{
				reader = XmlReader.Create(AicSettings.Global.WeatherUrl);
				reader.ReadToFollowing("forecast");
				while (reader.Read())
				{
					if (reader.NodeType == XmlNodeType.Element)
					{
						if (reader.Name == "time")		// Skip the forecasts for specific hours, only take current weather data
						{
							reader.Skip();
						}
						else if (reader.Name == "tn")
						{
							weatherData.TempMin = reader.ReadElementContentAsDouble();
						}
						else if (reader.Name == "tx")
						{
							weatherData.TempMax = reader.ReadElementContentAsDouble();
						}
						else if (reader.Name == "ws")
						{
							weatherData.WindSpeed = reader.ReadElementContentAsDouble();
						}
						else if (reader.Name == "wd")
						{
							weatherData.WindDirectionDegree = reader.ReadElementContentAsInt();
						}
						else if (reader.Name == "wd_txt")
						{
							weatherData.WindDirection = reader.ReadElementContentAsString();
						}
						else if (reader.Name == "w_txt")
						{
							weatherData.Description = reader.ReadElementContentAsString();
						}
					}
				}
			}
			catch (Exception e)
			{
				weatherData = null;
				if (LogError != null)
				{
					LogError("Error at reading weather data.", e);
				}
			}
			finally
			{
				if (reader != null)
				{
					reader.Close();
				}
			}

			return weatherData;
		}

		/// <summary>
		/// Shows the next page.
		/// </summary>
		public void ShowNextPage()
		{
			mPageIndex++;
			ShowPage();
		}

		/// <summary>
		/// Shows the previous page.
		/// </summary>
		public void ShowPreviousPage()
		{
			mPageIndex--;
			ShowPage();
		}

		// Shows the page with the current index
		// The index is checked and changed if necessary
		private void ShowPage()
		{
			if (mInfoCenterData == null || mInfoCenterData.Pages == null || mInfoCenterData.Pages.Count == 0)
			{
				if (LogWarning != null)
				{
					LogWarning("ShowPage could not show a page because something is null or empty.");
				}
				return;
			}

			// Get the current page index and the corresponding page
			if (mPageIndex >= mInfoCenterData.Pages.Count)
			{
				mPageIndex = 0;
			}
			else if (mPageIndex < 0)
			{
				mPageIndex = mInfoCenterData.Pages.Count - 1;
			}
			var page = mInfoCenterData.Pages[mPageIndex];

			// Skip pages that cannot be shown or are not available yet
			if (page.Type == PageType.Weather && mWeatherData == null)
			{
				ShowNextPage();
				return;
			}

			CreatePageByType(page);
			LayoutPage(page);
		}

		/// <summary>
		/// Creates the content of the page using its type. This is necessary for types that are defined at runtime.
		/// </summary>
		/// <param name="page">The page to be created.</param>
		private void CreatePageByType(Page page)
		{
			if (page.Type == PageType.Clock)
			{
				#region Clock

				page.Text.TextTitle.Value = DateTime.Now.ToShortTimeString();
				page.Text.TextLine.Clear();
				page.Text.TextLine.Add(DateTime.Now.ToShortDateString());

				#endregion
			}
			else if (page.Type == PageType.Time)
			{
				#region Time
				
				page.Text.TextTitle.Value = DateTime.Now.ToShortTimeString();

				#endregion
			}
			else if (page.Type == PageType.Date)
			{
				#region Date

				page.Text.TextTitle.Value = DateTime.Now.ToShortDateString();

				#endregion
			}
			else if (page.Type == PageType.Weather && mWeatherData != null)
			{
				#region Weather

				page.Item = new Text
				            	{
				            		TextLine = new List<string>
				            		           	{
				            		           		"Temperatur (min/max): " + mWeatherData.TempMin + "° C / " + mWeatherData.TempMax + "° C",
				            		           		"Wind: " + mWeatherData.WindSpeed + " km/h (" + mWeatherData.WindDirection + ")",
				            		           		mWeatherData.Description
				            		           	}
				            	};
				page.FooterLeft = "Powered by wetter.com";
				page.FooterRight = "";

				#endregion
			}
		}

		/// <summary>
		/// Applies the settings of the page to the WPF controls.
		/// </summary>
		/// <param name="page">The page to layout.</param>
		private void LayoutPage(Page page)
		{
			TitleLbl.Content = page.Title;

			if (page.Text != null)
			{
				#region Text

				TextPnl.Children.Clear();

				// Text title (first row)
				if (page.Text.TextTitle != null)
				{
					var textBlock = new TextBlock(new Bold(new Run(page.Text.TextTitle.Value)));
					textBlock.Foreground = new SolidColorBrush(page.Text.TextTitle.ColorAsColor);
					TextPnl.Children.Add(textBlock);
				}

				// Text lines
				if (page.Text.TextLine != null)
				{
					foreach (string text in page.Text.TextLine)
					{
						TextPnl.Children.Add(new TextBlock(new Run(text)));
					}
				}

				// Settings for all textblocks
				foreach (TextBlock textBlock in TextPnl.Children)
				{
					textBlock.Margin = new Thickness(0, 0, 0, 10);
					textBlock.HorizontalAlignment = page.Text.LeftAligned ? HorizontalAlignment.Left : HorizontalAlignment.Center;
					textBlock.FontSize = page.Text.UseMaxTextSize ? SystemParameters.PrimaryScreenHeight : mTextPanelFontSize;
				}

				// Footer labels
				FooterLeftLbl.Content = page.FooterLeft;
				FooterRightLbl.Content = page.FooterRight;

				// Set correct visibility
				TextPnl.Visibility = Visibility.Visible;
				ImageCtrl.Visibility = Visibility.Hidden;
				FooterLeftLbl.Visibility = Visibility.Visible;
				FooterRightLbl.Visibility = page.Type == PageType.Weather ? Visibility.Collapsed : Visibility.Visible;
				WeatherLogoImg.Visibility = page.Type == PageType.Weather ? Visibility.Visible : Visibility.Collapsed;
				FooterRightPnl.Visibility = Visibility.Visible;

				#endregion
			}
			else if (page.ImageUrl != null)
			{
				#region Image

				// Set correct visibility
				ImageCtrl.Source = mUrl2Image[page.ImageUrl];
				ImageCtrl.Visibility = Visibility.Visible;
				TextPnl.Visibility = Visibility.Hidden;
				FooterLeftLbl.Visibility = Visibility.Hidden;
				FooterRightPnl.Visibility = Visibility.Hidden;

				#endregion
			}
			else
			{
				if (LogWarning != null)
				{
					LogWarning("Invalid page: " + page);
				}
			}
		}
	}
}
