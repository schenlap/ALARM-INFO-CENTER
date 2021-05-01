﻿/*
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

namespace AlarmInfoCenter.Base
{
	/// <summary>
	/// The kind of a water supply point.
	/// </summary>
	public enum WaterSupplyPointKind
	{
		Undefined,

		/// <summary>
		/// Oberflurhydrant
		/// </summary>
		PillarHydrant,

		/// <summary>
		/// Unterflurhydrant
		/// </summary>
		UndergroundHydrant,

		/// <summary>
		/// Löschbecken
		/// </summary>
		FireExtinguishingPool,

		/// <summary>
		/// Saugstelle
		/// </summary>
		SuctionPoint
	}
}
