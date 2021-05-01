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
using System.Reflection;
using System.Windows.Controls;
using AlarmInfoCenter.Base;

namespace AlarmInfoCenter.UI
{
	class AlarmControlHelper
	{
		public static void DeactivateScripting(WebBrowser webBrowser)
		{
			try
			{
				var fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
				if (fiComWebBrowser != null)
				{
					var objComWebBrowser = fiComWebBrowser.GetValue(webBrowser);
					if (objComWebBrowser != null)
					{
						objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { true });
					}
				}
			}
			catch (Exception exc)
			{
				Log.GetInstance().LogError("Could not deactivate the script error popup window.", exc);
			}
		}

		public static void SetChildrenLabelFontSize(Panel panel, double size)
		{
			foreach (var child in panel.Children)
			{
				if (child is Panel)
				{
					SetChildrenLabelFontSize(child as Panel, size);
				}
				else if (child is Label)
				{
					(child as Label).FontSize = size;
				}
			}
		}
	}
}
