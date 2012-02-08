using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace wyUpdate.Common
{
    public static class VersionTools
    {
        static readonly Hashtable greek_ltrs = new Hashtable
	        {
		        {"alpha", 0}, {"beta", 1}, {"gamma", 2},
                {"delta", 3}, {"epsilon", 4}, {"zeta", 5},
                {"eta", 6}, {"theta", 7}, {"iota", 8},
                {"kappa", 9}, {"lambda", 10}, {"mu", 11},
                {"nu", 12}, {"xi", 13}, {"omicron", 14},
                {"pi", 15}, {"rho", 16}, {"sigma", 17},
                {"tau", 18}, {"upsilon", 19}, {"phi", 20},
                {"chi", 21}, {"psi", 22}, {"omega", 23},
                {"rc", 24} // RC = release candidate
	        };

        /// <summary>Compares two versions and returns an integer that indicates their relationship in the sort order.</summary>
        /// <param name="versionA">The first verison to compare.</param>
        /// <param name="versionB">The second version to compare.</param>
        /// <returns>Return a negative number if versionA is less than versionB, 0 if they're equal, a positive number if versionA is greater than versionB.</returns>
        public static int Compare(string versionA, string versionB)
        {
            if (versionA == null) return -1;
            if (versionB == null) return 1;

            // Convert version to lowercase, and
            // replace all instances of "release candidate" with "rc"
            versionA = Regex.Replace(versionA.ToLowerInvariant(), @"release[\s]+candidate", "rc");
            versionB = Regex.Replace(versionB.ToLowerInvariant(), @"release[\s]+candidate", "rc");

            //compare indices
            int iVerA = 0, iVerB = 0;

            bool lastAWasLetter = true, lastBWasLetter = true;

            for (;;)
            {
                //store index before GetNextObject just in case we need to rollback
                int greekIndA = iVerA;
                int greekIndB = iVerB;

                string objA = GetNextObject(versionA, ref iVerA, ref lastAWasLetter);
                string objB = GetNextObject(versionB, ref iVerB, ref lastBWasLetter);


                //normalize versions so comparing integer against integer, 
                //(i.e. "1 a" is expanded to "1.0.0 a" when compared with "1.0.0 XXX")
                //also, rollback the index on the version modified
                if ((!lastBWasLetter && objB != null) && (objA == null || lastAWasLetter))
                {
                    objA = "0";
                    iVerA = greekIndA;
                }
                else if ((!lastAWasLetter && objA != null) && (objB == null || lastBWasLetter))
                {
                    objB = "0";
                    iVerB = greekIndB;
                }


                // find greek index for A and B
                greekIndA = lastAWasLetter ? GetGreekIndex(objA) : -1;
                greekIndB = lastBWasLetter ? GetGreekIndex(objB) : -1;


                if (objA == null && objB == null)
                    return 0; //versions are equal

                if (objA == null) // objB != null
                {
                    //if versionB has a greek word, then A is greater
                    if (greekIndB != -1)
                        return 1;

                    return -1;
                }

                if (objB == null) // objA != null
                {
                    //if versionA has a greek word, then B is greater
                    if (greekIndA != -1)
                        return -1;

                    return 1;
                }

                if (char.IsDigit(objA[0]) == char.IsDigit(objB[0]))
                {
                    int strComp;
                    if (char.IsDigit(objA[0]))
                    {
                        //compare integers
                        strComp = IntCompare(objA, objB);

                        if (strComp != 0)
                            return strComp;
                    }
                    else
                    {
                        if (greekIndA == -1 && greekIndB == -1)
                        {
                            //compare non-greek strings
                            strComp = string.Compare(objA, objB, StringComparison.Ordinal);

                            if (strComp != 0)
                                return strComp;
                        }
                        else if (greekIndA == -1)
                            return 1; //versionB has a greek word, thus A is newer
                        else if (greekIndB == -1)
                            return -1; //versionA has a greek word, thus B is newer
                        else
                        {
                            //compare greek words
                            if (greekIndA > greekIndB)
                                return 1;

                            if (greekIndB > greekIndA)
                                return -1;
                        }
                    }
                }
                else if (char.IsDigit(objA[0]))
                    return 1; //versionA is newer than versionB
                else
                    return -1; //verisonB is newer than versionA
            }
        }

        static string GetNextObject(string version, ref int index, ref bool lastWasLetter)
        {
            //1 == string, 2 == int, -1 == neither
            int StringOrInt = -1;

            int startIndex = index;

            while (version.Length != index)
            {
                if (StringOrInt == -1)
                {
                    if (char.IsLetter(version[index]))
                    {
                        startIndex = index;
                        StringOrInt = 1;
                    }
                    else if (char.IsDigit(version[index]))
                    {
                        startIndex = index;
                        StringOrInt = 2;
                    }
                    else if (lastWasLetter && !char.IsWhiteSpace(version[index]))
                    {
                        index++;
                        lastWasLetter = false;
                        return "0";
                    }
                }
                else if (StringOrInt == 1 && !char.IsLetter(version[index]))
                    break;
                else if (StringOrInt == 2 && !char.IsDigit(version[index]))
                    break;

                index++;
            }

            // set the last "type" retrieved
            lastWasLetter = StringOrInt == 1;

            // return the retitrved sub-string
            if (StringOrInt == 1 || StringOrInt == 2)
                return version.Substring(startIndex, index - startIndex);

            // was neither a string nor and int
            return null;
        }

        static int GetGreekIndex(object version)
        {
            object val = greek_ltrs[version];

            if (val == null)
                return -1;

            return (int)val;
        }


        static int IntCompare(string a, string b)
        {
            int lastZero = -1;

            // Clear any preceding zeros

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != '0')
                    break;

                lastZero = i;
            }

            if (lastZero != -1)
                a = a.Substring(lastZero + 1, a.Length - (lastZero + 1));

            lastZero = -1;

            for (int i = 0; i < b.Length; i++)
            {
                if (b[i] != '0')
                    break;

                lastZero = i;
            }

            if (lastZero != -1)
                b = b.Substring(lastZero + 1, b.Length - (lastZero + 1));


            if (a.Length > b.Length)
                return 1;

            if (a.Length < b.Length)
                return -1;

            return string.Compare(a, b, StringComparison.Ordinal);
        }

#if DESIGNER

        /// <summary>Increments the version number.</summary>
        /// <param name="version">The version to increment.</param>
        /// <returns>Returns the next logical verison (e.g. 1.0.2 -> 1.0.3, or 1.1 beta -> 1.1 beta 2)</returns>
        public static string VerisonPlusPlus(string version)
        {
            int previ, i = 0;
            object prevObj, obj = null;

            bool junkBool = false;

            do
            {
                previ = i;
                prevObj = obj;
                obj = GetNextObject(version, ref i, ref junkBool);

            } while (obj != null);

            if (prevObj != null)
            {
                // try to increment the final digit (e.g. 1.0.2 -> 1.0.3)
                if (char.IsDigit(((string)prevObj)[0]))
                    return version.Substring(0, previ - ((string)prevObj).Length) + NumberPlusOne((string)prevObj);

                // otherwise just tack on a 2 (e.g. 1.0 beta -> 1.0 beta 2)
                return version + " 2";
            }

            return version;
        }

        static string NumberPlusOne(string number)
        {
            StringBuilder sb = new StringBuilder(number.Length + 2);

            int i = number.Length - 1;
            int tempInt = 1;

            // process the number
            for (; i >= 0; i--)
            {
                tempInt += number[i] - '0';

                if (tempInt == 10)
                {
                    sb.Insert(0, '0');
                    tempInt = 1;
                }
                else
                {
                    sb.Insert(0, (char)(tempInt + '0'));
                    tempInt = 0;
                    break;
                }
            }

            if (tempInt != 0)
                // e.g. 99 + 1
                sb.Insert(0, '1');
            else if (i > 0)
                // insert the higher digits that didn't need process
                // e.g. 573 + 1 = 574, the leading '57' is copied over
                sb.Insert(0, number.Substring(0, i));

            return sb.ToString();
        }

#endif

        static string thisVersion;

        /// <summary>Gets the file version of the currently executing assembly.</summary>
        /// <returns>The version of the currently executing assembly.</returns>
        public static string FromExecutingAssembly()
        {
            return thisVersion ??
                   (thisVersion =
                    FileVersionInfo.GetVersionInfo(System.Windows.Forms.Application.ExecutablePath).FileVersion);
        }
    }
}
