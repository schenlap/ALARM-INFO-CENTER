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
using System.Reflection;
using System.Windows;
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.UI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : ISingleInstanceApp
	{
		/// <summary>
		/// The main entry point.
		/// </summary>
		/// <remarks>
		/// The application can only be started once.
		/// http://blogs.microsoft.co.il/blogs/arik/archive/2010/05/28/wpf-single-instance-application.aspx
		/// </remarks>
		[STAThread]
		public static void Main()
		{
			if (SingleInstance<App>.InitializeAsFirstInstance(Assembly.GetExecutingAssembly().GetType().GUID.ToString()))
			{
				var splashScreen = new SplashScreen("resources/splash.png");
				splashScreen.Show(true);
				App app = new App();
				app.InitializeComponent();
				app.Run();

				// Allow single instance code to perform cleanup operations
				SingleInstance<App>.Cleanup();
			}
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			DispatcherUnhandledException += (sender, exc) => Log.GetInstance().LogError("Unhandled application error", exc.Exception);		// Log any unhandled exception

			// Context menu
			System.Windows.Forms.ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
			System.Windows.Forms.MenuItem infoItem = new System.Windows.Forms.MenuItem("Info", delegate { TaskBarNotifier.ShowInfo(MainWindow); }, System.Windows.Forms.Shortcut.F1);
			System.Windows.Forms.MenuItem openItem = new System.Windows.Forms.MenuItem("AIC öffnen", delegate { OpenMainWindow(); });
			openItem.DefaultItem = true;
			contextMenu.MenuItems.Add(infoItem);
			contextMenu.MenuItems.Add(openItem);
			contextMenu.MenuItems.Add("-");
			contextMenu.MenuItems.Add("Beenden", delegate { Shutdown(); });

			// Notify icon in tray
			System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();
			notifyIcon.Text = "AIC";
			notifyIcon.Icon = UI.Properties.Resources.aic16;
			notifyIcon.Visible = true;
			notifyIcon.ContextMenu = contextMenu;
			notifyIcon.MouseDoubleClick += delegate { OpenMainWindow(); };
			TaskBarNotifier.SetNotifyIcon(notifyIcon);
		}

		// Open the window (show it and make it active)
		private void OpenMainWindow()
		{
			var mainWindow = MainWindow as MainWindow;
			if (mainWindow != null)
			{
				mainWindow.ShowAndActivate();
			}
		}

		/// <summary>
		/// Happens when the application is about to end.
		/// </summary>
		/// <param name="e">ExitEvent arguments.</param>
		protected override void OnExit(ExitEventArgs e)
		{
			TaskBarNotifier.DisposeNotifyIcon();
			base.OnExit(e);
		}

		/// <summary>
		/// Opens the main application window and show a task bar message that AIC is already running.
		/// </summary>
		/// <param name="args">Command line arguments.</param>
		/// <returns>Always returns true.</returns>
		public bool SignalExternalCommandLineArgs(IList<string> args)
		{
			OpenMainWindow();
			TaskBarNotifier.ShowNotifyMessage("AIC wird bereits ausgeführt.");
			return true;
		}
	}
}
