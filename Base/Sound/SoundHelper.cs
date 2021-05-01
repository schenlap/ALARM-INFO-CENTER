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
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Threading;

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// This class includes functionality for outputting sound.
	/// </summary>
	public class SoundHelper
	{
		private readonly int mAlarmSoundLength;
		private readonly SoundPlayer mAlarmSoundPlayer = new SoundPlayer();
		private readonly MediaPlayer mAnnouncementPlayer = new MediaPlayer();
		private readonly DispatcherTimer mAnnouncementTimer = new DispatcherTimer();

		private bool mStartAnnouncementTimer;
		private int mIntervalIndex;				// The index of the current interval
		private Alarm mAlarm;					// The alarm to be announced
		private List<int> mIntervals;			// The intervals of sound outputs in milliseconds

		
		/// <summary>
		/// The alarm to be announced.
		/// </summary>
		public Alarm Alarm
		{
			set
			{
				if (value == null)
				{
					Stop();
				}
				else
				{
					var uri = Announcement.CreateUrl(value);
					mAnnouncementPlayer.Open(uri);
				}
				mAlarm = value;
			}
		}



		/// <summary>
		/// Creates a new instance of the SoundHelper class.
		/// </summary>
		/// <param name="alarmSoundPath">The path of the alarm sound file.</param>
		public SoundHelper(string alarmSoundPath)
		{
			if (File.Exists(alarmSoundPath))
			{
				mAlarmSoundPlayer.SoundLocation = alarmSoundPath;
			}
			mAlarmSoundLength = GetSoundLength(alarmSoundPath);
			mAnnouncementPlayer.Volume = 1;
			mAnnouncementTimer.Tick += AnnouncementTimer_Tick;
			mAnnouncementPlayer.MediaOpened += mAnnouncementPlayer_MediaOpened;
		}

		// Start the announcement timer as soon as the media has been downloaded (necessary for the length of the media)
		private void mAnnouncementPlayer_MediaOpened(object sender, EventArgs e)
		{
			if (mStartAnnouncementTimer)
			{
				mStartAnnouncementTimer = false;
				mAnnouncementTimer.Start();
			}
		}

		// Plays the announcement in defined intervals and stops after the last announcement
		private void AnnouncementTimer_Tick(object sender, EventArgs e)
		{
			mIntervalIndex++;
			mAnnouncementPlayer.Stop();
			mAnnouncementPlayer.Play();
			if (mIntervalIndex < mIntervals.Count)
			{
				int milliseconds = (int) mAnnouncementPlayer.NaturalDuration.TimeSpan.TotalMilliseconds + mIntervals[mIntervalIndex] * 1000;
				mAnnouncementTimer.Interval = new TimeSpan(0, 0, 0, 0, milliseconds);
			}
			else
			{
				mAnnouncementTimer.Stop();
			}
		}


		[DllImport("winmm.dll")]
		private static extern uint mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr winHandle);

		/// <summary>
		/// Returns the length of a wave file. Returns 0 if the file cannot be found.
		/// </summary>
		/// <param name="fileName">The name of the file.</param>
		/// <returns>The length of the file in milliseconds, 0 if an error occurs.</returns>
		public static int GetSoundLength(string fileName)
		{
			var lengthBuf = new StringBuilder(32);

			mciSendString(string.Format("open \"{0}\" type waveaudio alias wave", fileName), null, 0, IntPtr.Zero);
			mciSendString("status wave length", lengthBuf, lengthBuf.Capacity, IntPtr.Zero);
			mciSendString("close wave", null, 0, IntPtr.Zero);

			int length;
			int.TryParse(lengthBuf.ToString(), out length);

			return length;
		}

		/// <summary>
		/// Stops all players and plays the alarm sound.
		/// </summary>
		public void PlayAlarmSound()
		{
			Stop();
			mAlarmSoundPlayer.Play();
		}

		/// <summary>
		/// Stops all players and plays alarm subject and location once.
		/// </summary>
		public void PlayAnnouncement()
		{
			Stop();
			mAnnouncementPlayer.Play();
		}

		/// <summary>
		/// Plays the alarm sound and the announcement. The alarm sound plays immediately (no interval considered).
		/// </summary>
		/// <param name="playAlarmSound">Indicates if the alarm sound should be played.</param>
		/// <param name="intervals">The intervals between sound outputs in milliseconds.</param>
		public void PlaySequence(bool playAlarmSound = true, List<int> intervals = null)
		{
			Stop();
			mIntervalIndex = 0;
			mIntervals = intervals;
			int waitTime = 0;

			// Play alarm sound
			if (playAlarmSound)
			{
				mAlarmSoundPlayer.Play();
				waitTime = mAlarmSoundLength;
			}

			if (mAlarm != null && mIntervals != null && mIntervals.Count > 0)
			{
				// Play announcement
				mStartAnnouncementTimer = true;
				int time = mIntervals[mIntervalIndex] * 1000;
				mAnnouncementTimer.Interval = new TimeSpan(0, 0, 0, 0, waitTime + time);
				if (mAnnouncementPlayer.NaturalDuration.HasTimeSpan)		// Start the timer only if the length of the media is determined
				{
					mAnnouncementTimer.Start();
				}
			}
		}

		/// <summary>
		/// Stops all the players and the timers.
		/// </summary>
		public void Stop()
		{
			mAnnouncementTimer.Stop();
			mAlarmSoundPlayer.Stop();
			mAnnouncementPlayer.Stop();
		}
	}
}
