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
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AlarmInfoCenter.Base
{
    /// <summary>
    /// This class provides the whole WHITE-functionality.
    /// </summary>
    public static class WhiteUtilities
    {
        #region Constants

        /// <summary>
        /// To identify a street, the street name must at least contain this pattern.
        /// </summary>
        private const string StreetIdentifier = "str";

        /// <summary>
        /// To identify a alley, the alley name must at least contain this pattern.
        /// </summary>
        private const string AlleyIdentifier = "ga";

        /// <summary>
        /// To identify a way, the way name must at least contain this pattern.
        /// </summary>
        private const string WayIdentifier = "weg";

        /// <summary>
        /// If the alarm's subject is equal to this identifier, it will get parsed with a different logic / alogrithm.
        /// </summary>
        private const string BMAIdentifier = "brandmeldealarm";

		/// <summary>
		/// German word for street in lower case.
		/// </summary>
		private const string StreetTerm = "straﬂe";

		/// <summary>
		/// German word for alley in lower case.
		/// </summary>
		private const string AlleyTerm = "gasse";

		/// <summary>
		/// German word for way in lower case.
		/// </summary>
		private const string WayTerm = "weg";

		private const char TypeForceCharacter = '!';

        #endregion

        /// <summary>
        /// Uses the WHITE algorithm / process to get a concrete location out of the alarm's additional information.
        /// </summary>
        /// <param name="alarm">The alarm which should get WHITE parsed.</param>
        /// <returns>A concrete location (with uppercase letters only) or NULL if no location could be found.</returns>
        public static string FindPlace(Alarm alarm)
        {
            return null;
            /*string locationProposition = null;

            // No additional information in the extra text
            if (string.IsNullOrWhiteSpace(alarm.AdditionalInformation))
            {
                return null;
            }

            // Use BMA specific logic
            else if (alarm.Subject.ToLower().Contains(BMAIdentifier))
            {
                locationProposition = FindBMAPlace(alarm.AdditionalInformation.ToLower(), AicServerSettings.DefaultCity);

                if (locationProposition != null)
                {
					return locationProposition;
                }
            }

            // Use non specific logic
			// BMA: Second chance by searching with non specific logic
			locationProposition = FindNormalPlace(alarm.AdditionalInformation.ToLower(), AicServerSettings.DefaultCity);

            if (locationProposition != null)
            {
                return locationProposition.ToUpper();
            }

            return null;*/
        }
        /*
        /// <summary>
        /// WHITE-process for non specific alarm subjects (such as 'Brandmeldealarm').
        /// </summary
        /// <param name="additionalInformation">The alarm's additional information.</param>
        /// <param name="defaultCity">The firebrigade's default city (in the AIC-Settings).</param>
        /// <returns>The concrete location with the default city at the end of the string or NULL if no location could be found.</returns>
        private static string FindNormalPlace(string additionalInformation, string defaultCity)
        {
            // Compare each possible street naming
			foreach (KeyValuePair<string, List<string>> pair in AicSettings.Global.WhiteNormalEntries)
            {
                CompareMode mode = CompareMode.Normal;

				// Actual street (from the dictionary) doesn't contain any type pattern (street, way, alley)
				// Use special search method
				if (!pair.Key.Contains(StreetTerm) && !pair.Key.Contains(WayTerm) && !pair.Key.Contains(AlleyTerm))
                {
                    mode = CompareMode.WithoutStreetTypePattern;
                }

				// Try to find the current WHITE-Entry in the additional text
				// Core WHITE functionality
                int entryResult = CompareEntry(pair.Value, additionalInformation, mode);

                // No hit
                if (entryResult == -1)
                {
                    continue;
                }

                // Hit without exact house number
                else if (entryResult == 0)
                {
                    return pair.Key + " " + defaultCity;
                }

                // Hit with exact house number
                else
                {
                    return pair.Key + " " + entryResult + " " + defaultCity;
                }
            }

            return null;
        }

        /// <summary>
        /// WHITE-process for specific 'BMA'-alarms.
        /// </summary
        /// <param name="additionalInformation">The alarm's additional information.</param>
        /// <param name="defaultCity">The firebrigade's default city (in the AIC-Settings).</param>
        /// <returns>The concrete location of the company with the default city at the end of the string 
        /// or NULL if no location / company could be found.</returns>
        private static string FindBMAPlace(string additionalInformation, string defaultCity)
        {
			foreach (KeyValuePair<string, List<string>> pair in AicServerSettings.WhiteBmaEntries)
            {
				// Try to find the current WHITE-Entry in the additional text
				// Core WHITE functionality
                int result = CompareEntry(pair.Value, additionalInformation, CompareMode.BMA);

                if(result != -1)
                {
                    return pair.Key + " " + defaultCity;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Compares a set of entries (possible street names) if it fits with the additional information of the alarm.
        /// </summary
        /// <param name="streetEntries">All possible street names regarding to one street.</param>
        /// <param name="additionalInformation">The alarm's additional information.</param>
        /// <param name="mode">The mode the entries will be compared.</param>
        /// <returns>
        /// If the search / comparasion succeded the method will return either 0 if no house number couldn't be computed
        /// or a number greater 0 representing the possible / exact house number. If it fails the method will return -1.
        /// </returns>
        private static int CompareEntry(
            IEnumerable<string> streetEntries, 
            string additionalInformation, 
            CompareMode mode)
        {
            int result = -1;

            string additionalInformationReplaced = additionalInformation.
                Replace("-", string.Empty).
                Replace(" ", string.Empty);

            // Compare each possible street naming
            foreach (string streetNaming in streetEntries)
            {
                string streetNamingReplaced = streetNaming.
                    Replace(" ", string.Empty).
                    Replace("-", string.Empty);

				// Check if the additional text contains the current search value
                bool foundNormalStreetNaming = additionalInformationReplaced.Contains(streetNamingReplaced);

				// 1. Check if current search value can't be treated with any other street type (street, way, alley) than specified
				// 2. Check if the additional text contains the current search value
                bool foundForcedStreetNaming = 
					streetNamingReplaced.IndexOf(TypeForceCharacter) != -1 && // 1
					additionalInformationReplaced.Contains(streetNamingReplaced.Substring(0, streetNamingReplaced.IndexOf(TypeForceCharacter))); // 2

                // Search mode: normal
                // 1. Check if at least the whole search value from the dictionary could be found in the alarm's additional information.
                // 2. Verify it's correcteness
                if (mode == CompareMode.Normal && (foundNormalStreetNaming || foundForcedStreetNaming))
                {
                    // Used to force a check of ONE! pattern ('weg', 'straﬂe' or 'gasse') at the end
                    bool forceWay = false;
                    bool forceAlley = false;
                    bool forceStreet = false;
                    bool forceAnything = false;

                    // Check if the algorithm has to use 'weg', 'straﬂe' or 'gasse'

                    if (streetNamingReplaced.Contains(TypeForceCharacter + StreetTerm))
                    {
                        forceAnything = true;
                        forceStreet = true;
						streetNamingReplaced = streetNamingReplaced.Replace(TypeForceCharacter + StreetTerm, string.Empty);
                    }

					else if (streetNamingReplaced.Contains(TypeForceCharacter + WayTerm))
                    {
                        forceAnything = true;
                        forceWay = true;
						streetNamingReplaced = streetNamingReplaced.Replace(TypeForceCharacter + WayTerm, string.Empty);
                    }

					else if (streetNamingReplaced.Contains(TypeForceCharacter + AlleyTerm))
                    {
                        forceAnything = true;
                        forceAlley = true;
						streetNamingReplaced = streetNamingReplaced.Replace(TypeForceCharacter + AlleyTerm, string.Empty);
                    }

					// Now it's time to choose the right type identifier for searching
					// Search for those types in the additional information

                    Func<string, string ,int ,int, int> thirdStep = null;

                    string streetTypePattern = null;

                    int streetIndex = additionalInformationReplaced.IndexOf(StreetIdentifier, StringComparison.Ordinal);
                    int wayIndex = additionalInformationReplaced.IndexOf(WayIdentifier, StringComparison.Ordinal);
                    int alleyIndex = additionalInformationReplaced.IndexOf(AlleyIdentifier, StringComparison.Ordinal);

                    if (forceStreet || (!forceAnything && streetIndex != -1))
                    {
                        thirdStep = FindHousenumberStreetSpecific;
                        streetTypePattern = StreetIdentifier;
                    }

                    else if (forceWay || (!forceAnything && wayIndex != -1))
                    {
                        thirdStep = FindHousenumberWaySpecific;
                        streetTypePattern = WayIdentifier;
                    }

                    else if (forceAlley || (!forceAnything && alleyIndex != -1))
                    {
                        thirdStep = FindHousenumberAlleySpecific;
                        streetTypePattern = AlleyIdentifier;
                    }

					// No type in the additional information found
                    else
                    {
                        break;
                    }

                    int verifyResult = VerifyAnyStreetType(additionalInformation, streetNamingReplaced, streetTypePattern, thirdStep);

                    // Run through more possible combinations
                    // Maybe there is one where the algorithm can find a exact housenumber

                    // Override initial invalid result with at least a valid result
                    if (verifyResult != -1 && result == -1)
                    {
                        result = verifyResult;
                    }

                    // Override a valid result without housenumber with the result with housenumber - job done
                    else if (verifyResult > 0 && result == 0)
                    {
                        result = verifyResult;
                        break;
                    }
                }

                else if (mode == CompareMode.WithoutStreetTypePattern && foundNormalStreetNaming)
                {
                    Func<string, string, int, int, int> thirdStep = FindHousenumberNonSpecific;

                    int verifyResult = VerifyAnyStreetType(additionalInformation, streetNamingReplaced, null, thirdStep);

                    // Run through more possible combinations
                    // Maybe there is one where the algorithm can find a exact housenumber

                    // Override invalid result with at least a valid result
                    if (verifyResult != -1 && result == -1)
                    {
                        result = verifyResult;
                    }

                    // Override a valid result without housenumber with the result with housenumber - job done
                    else if (verifyResult > 0 && result == 0)
                    {
                        result = verifyResult;
                        break;
                    }
                }

                // Search mode: bma
                // No need for verifying
                else if (foundNormalStreetNaming && mode == CompareMode.BMA)
                {
                    return 0;
                }
            }

            return result;
        }

        /// <summary>
        /// Delegate function providing an algorithm for the third step of the WHITE-Verify process (street type specialized). 
        /// (see documentation for more detail)
        /// </summary>
        /// <param name="additionalInformation">The alarm's additional information.</param>
        /// <param name="streetNamingReplaced">The street's naming with '-' and ' ' replaced with ''.</param>
        /// <param name="replacementCharCount">The number of '-' and ' ' which have already been parsed.</param>
        /// <param name="streetNameStartIndex">The start index of the street's pattern in the alarm's additional information.</param>
        /// <returns>Positive number: the exact housenumber, -1: no exact housenumber found</returns>
        private static int FindHousenumberStreetSpecific(
            string additionalInformation, 
            string streetNamingReplaced, 
            int replacementCharCount, 
            int streetNameStartIndex)
        {
            bool digitFound = false;
            bool pointFound = false;
            bool lastCharIsReplacement = false;

            StringBuilder builder = new StringBuilder();

            for (int l = streetNameStartIndex + streetNamingReplaced.Length + StreetIdentifier.Length + replacementCharCount; 
                l < additionalInformation.Length; l++)
            {
                // Current character is a digit
                if (char.IsDigit(additionalInformation[l]))
                {
                    lastCharIsReplacement = false;
                    digitFound = true;

                    builder.Append(additionalInformation[l]);
                }

                // Current character isn't a digit
                else
                {
                    // Digits have already been found - The search is over
                    if (digitFound)
                    {
                        break;
                    }

                    // The current character is not one of these allowed 'replacement'-characters and a point has already been found
                    else if (pointFound && additionalInformation[l] != '-' && additionalInformation[l] != ' ')
                    {
                        break;
                    }

                    // Point hasn't been found yet and the current character is a possible character in the street's type pattern
                    else if (!pointFound && (additionalInformation[l] == 'a' || additionalInformation[l] == 's' ||
                                        additionalInformation[l] == 'ﬂ' || additionalInformation[l] == 'e'))
                    {
                        lastCharIsReplacement = false;

                        continue;
                    }

                    // Current character is a point
                    else if (additionalInformation[l] == '.')
                    {
                        pointFound = true;
                    }

                    // The current character is one of these allowed 'replacement'-characters
                    else
                    {
                        // The last character was not one of these 'replacement'-characters
                        if (!lastCharIsReplacement)
                        {
                            lastCharIsReplacement = true;
                        }

                        // Two times in a row a 'replacement'-character found - search for housenumber is over
                        else
                        {
                            break;
                        }
                    }
                }
            }

            if (builder.ToString() == string.Empty)
                return -1;

            int houseNumber = -1;

            bool succeded = int.TryParse(builder.ToString(), out houseNumber);

            if (!succeded)
                return -1;

            return houseNumber;
        }

        /// <summary>
        /// Delegate function providing an algorithm for the third step of the WHITE-Verify process (way type specialized). 
        /// (see documentation for more detail)
        /// </summary>
        /// <param name="additionalInformation">The alarm's additional information.</param>
        /// <param name="streetNamingReplaced">The street's naming with '-' and ' ' replaced with ''.</param>
        /// <param name="replacementCharCount">The number of '-' and ' ' which have already been parsed.</param>
        /// <param name="streetNameStartIndex">The start index of the street's pattern in the alarm's additional information.</param>
        /// <returns>Positive number: the exact housenumber, -1: no exact housenumber found</returns>
        private static int FindHousenumberWaySpecific(
            string additionalInformation,
            string streetNamingReplaced,
            int replacementCharCount,
            int streetNameStartIndex)
        {
            bool digitFound = false;

            bool lastCharIsReplacement = false;
            StringBuilder builder = new StringBuilder();

            for (int l = streetNameStartIndex + streetNamingReplaced.Length + WayIdentifier.Length + replacementCharCount; 
                l < additionalInformation.Length; l++)
            {
                // Current character is a digit
                if (char.IsDigit(additionalInformation[l]))
                {
                    lastCharIsReplacement = false;
                    digitFound = true;

                    builder.Append(additionalInformation[l]);
                }

                // Current character isn't a digit
                else
                {
                    // Digits have already been found - The search is over
                    if (digitFound)
                    {
                        break;
                    }

                    // The current character is not one of these allowed 'replacement'-characters
                    else if (additionalInformation[l] != '-' && additionalInformation[l] != ' ')
                    {
                        break;
                    }

                    // The current character is one of these allowed 'replacement'-characters
                    else
                    {
                        // The last character was not one of these 'replacement'-characters
                        if (!lastCharIsReplacement)
                        {
                            lastCharIsReplacement = true;
                        }

                        // Two times in a row a 'replacement'-character found - search for housenumber is over
                        else
                        {
                            break;
                        }
                    }
                }
            }

            int houseNumber = -1;

            int.TryParse(builder.ToString(), out houseNumber);

            return houseNumber;
        }

        /// <summary>
        /// Delegate function providing an algorithm for the third step of the WHITE-Verify process (alley type specialized). 
        /// (see documentation for more detail)
        /// </summary>
        /// <param name="additionalInformation">The alarm's additional information.</param>
        /// <param name="streetNamingReplaced">The street's naming with '-' and ' ' replaced with ''.</param>
        /// <param name="replacementCharCount">The number of '-' and ' ' which have already been parsed.</param>
        /// <param name="streetNameStartIndex">The start index of the street's pattern in the alarm's additional information.</param>
        /// <returns>Positive number: the exact housenumber, -1: no exact housenumber found</returns>
        private static int FindHousenumberAlleySpecific(
            string additionalInformation,
            string streetNamingReplaced,
            int replacementCharCount,
            int streetNameStartIndex)
        {
            bool digitFound = false;
            bool pointFound = false;

            bool lastCharIsReplacement = false;
            StringBuilder builder = new StringBuilder();

            for (int l = streetNameStartIndex + streetNamingReplaced.Length + AlleyIdentifier.Length + replacementCharCount;
                l < additionalInformation.Length; l++)
            {
                // Current character is a digit
                if (char.IsDigit(additionalInformation[l]))
                {
                    lastCharIsReplacement = false;
                    digitFound = true;

                    builder.Append(additionalInformation[l]);
                }

                // Current character isn't a digit
                else
                {
                    // Digits have already been found - The search is over
                    if (digitFound)
                    {
                        break;
                    }

                    // The current character is not one of these allowed 'replacement'-characters and a point has already been found
                    else if (pointFound && additionalInformation[l] != '-' && additionalInformation[l] != ' ')
                    {
                        break;
                    }

                    // Point hasn't been found yet and the current character is a possible character in the alley's type pattern
                    else if (!pointFound && (additionalInformation[l] == 's' || additionalInformation[l] == 'ﬂ' || additionalInformation[l] == 'e'))
                    {
                        lastCharIsReplacement = false;

                        continue;
                    }

                    // Current character is a point
                    else if (additionalInformation[l] == '.')
                    {
                        pointFound = true;
                    }

                    // The current character is one of these allowed 'replacement'-characters
                    else
                    {
                        // The last character was not one of these 'replacement'-characters
                        if (!lastCharIsReplacement)
                        {
                            lastCharIsReplacement = true;
                        }

                        // Two times in a row a 'replacement'-character found - search for housenumber is over
                        else
                        {
                            break;
                        }
                    }
                }
            }

            int houseNumber = -1;

            int.TryParse(builder.ToString(), out houseNumber);

            return houseNumber;
        }

        /// <summary>
        /// Delegate function providing an algorithm for the third step of the WHITE-Verify process (no street type pattern specialized). 
        /// (see documentation for more detail)
        /// </summary>
        /// <param name="additionalInformation">The alarm's additional information.</param>
        /// <param name="streetNamingReplaced">The street's naming with '-' and ' ' replaced with ''.</param>
        /// <param name="replacementCharCount">The number of '-' and ' ' which have already been parsed.</param>
        /// <param name="streetNameStartIndex">The start index of the street's pattern in the alarm's additional information.</param>
        /// <returns>Positive number: the exact housenumber, -1: no exact housenumber found</returns>
        private static int FindHousenumberNonSpecific(
            string additionalInformation,
            string streetNamingReplaced,
            int replacementCharCount,
            int streetNameStartIndex)
        {
            bool digitFound = false;
            bool lastCharIsReplacement = false;
            StringBuilder builder = new StringBuilder();

            for (int l = streetNameStartIndex + streetNamingReplaced.Length + replacementCharCount;
                l < additionalInformation.Length; l++)
            {
                // Current character is a digit
                if(char.IsDigit(additionalInformation[l]))
                {
                    lastCharIsReplacement = false;
                    digitFound = true;

                    builder.Append(additionalInformation[l]);
                }

                // Current character isn't a digit
                else
                {
                    // Digits have already been found - The search is over
                    if (digitFound)
                    {
                        break;
                    }

                    // The current character is not one of these allowed 'replacement'-characters
                    else if (additionalInformation[l] != '-' && additionalInformation[l] != ' ')
                    {
                        break;
                    }

                    // The current character is one of these allowed 'replacement'-characters
                    else
                    {
                        // The last character was not one of these 'replacement'-characters
                        if (!lastCharIsReplacement)
                        {
                            lastCharIsReplacement = true;
                        }

                        // Two times in a row a 'replacement'-character found - search for housenumber is over
                        else
                        {
                            break;
                        }
                    }
                }
            }

            int houseNumber = -1;

            int.TryParse(builder.ToString(), out houseNumber);

            return houseNumber;
        }

        /// <summary>
        /// Performs the first two stages of the WHITE-Verify process with street names. (see documents for more information)
        /// </summary>
        /// <param name="additionalInformation">The alarm's additional information.</param>
        /// <param name="streetNamingReplaced">The street name to search for with already replaced '-' and ' ' with ''.</param>
        /// <param name="streetTypePattern">The street-type's pattern like 'weg', 'str' or 'ga' for the second step of the process 
        /// or NULL if the street name has no type pattern like 'Kappern 73'.</param>
        /// <param name="thirdStep">The third step as a function delegate with parameters: additionalInformation, streetNamingReplaced, 
        /// ,replacementCharacterCount and streetNameStartIndex. If this step succeded it must return the housenumber otherwise -1.</param>
        /// <returns>Positive number > 0: 3 stages succeded (correct housenumber) - 0: 2 stages succeded (correct street name)
        /// - (-1): Verification failed.</returns>
        private static int VerifyAnyStreetType(
            string additionalInformation, 
            string streetNamingReplaced, 
            string streetTypePattern,
            Func<string, string, int, int, int> thirdStep)
        {
            bool lastCharIsReplacement = false;
            // Needed for second and third step (to find the start position)
            int replacementCharCount = 0;
            // >0: housenumber, 0: success without housenumber, -1: fail
            int result = -1;

            // FIRST STEP
            // Run through all characters of the additional alarm information
            for (int i = 0; i < additionalInformation.Length; i++)
            {
                // First character matches with the possible street name
                if (additionalInformation[i] == streetNamingReplaced[0])
                {
                    bool namingFound = false;
                    int run = 0;

                    for (int j = i; j - i < streetNamingReplaced.Length; j++)
                    {
                        // Pattern matches with one of two replacement characters (second chance - see document)
                        if (additionalInformation[j] == streetNamingReplaced[run])
                        {
                            run++;
                            lastCharIsReplacement = false;

                            // Step one done (succeded)
                            if (j - i == streetNamingReplaced.Length - 1)
                            {
                                namingFound = true;
                                break;
                            }

                            continue;
                        }

                        // Pattern matches with one of two replacement characters (second chance - see document)
                        else if (additionalInformation[j] == '-' || additionalInformation[j].ToString() == " ")
                        {
                            replacementCharCount++;

                            if (!lastCharIsReplacement)
                            {
                                lastCharIsReplacement = true;
                            }

                            else
                            {
                                break;
                            }
                        }

                        else
                        {
                            break;
                        }
                    }

                    // SECOND STEP
                    // Find street type pattern (like 'str', 'ga' or 'weg') right after the street naming
                    // or skip second stage if there is no type pattern
                    if (namingFound)
                    {
                        bool secondStageSucceded = false;

                        // There is no type pattern in the street's name
                        // Skip second stage
                        if (streetTypePattern == null)
                        {
                            secondStageSucceded = true;
                        }

                        // There's a type pattern to check
                        else
                        {
                            lastCharIsReplacement = false;
                            run = 0;

                            // Start right after the street's name and check if the type pattern
                            for (int k = i + streetNamingReplaced.Length; k < additionalInformation.Length; k++)
                            {
                                // Pattern matches with the text
                                if (additionalInformation[k] == streetTypePattern[run])
                                {
                                    run++;
                                    lastCharIsReplacement = false;

                                    // Step two done (succeded)
                                    if (k - i - streetNamingReplaced.Length == streetTypePattern.Length - 1)
                                    {
                                        secondStageSucceded = true;

                                        break;
                                    }

                                    continue;
                                }

                                // Pattern matches with one of two replacement characters (second chance - see document)
                                else if (additionalInformation[k] == '-' || additionalInformation[k].ToString() == " ")
                                {
                                    if (!lastCharIsReplacement)
                                    {
                                        lastCharIsReplacement = true;
                                    }


                                    else
                                    {
                                        break;
                                    }
                                }

                                // Something else found (not valid)
                                else
                                {
                                    break;
                                }
                            }
                        }

                        // THIRD STEP
                        if (secondStageSucceded)
                        {
                            int thirdStepResult = thirdStep(additionalInformation, streetNamingReplaced, replacementCharCount, i);

                            // Third step succeded
                            if (thirdStepResult != -1)
                            {
                                return thirdStepResult;
                            }

                            // Two stages succeded
                            else
                            {
                                return 0;
                            }
                        }
                    }
                }
            }

            return result;
        }*/
    }

    internal enum CompareMode
    {
        Normal = 0,
        WithoutStreetTypePattern = 1,
        BMA = 2
    }
}
