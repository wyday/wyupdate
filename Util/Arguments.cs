/*
* Arguments class: application arguments interpreter
*
* Authors: R. LOPES
* Contributors: R. LOPES, BillyZKid, Hastarin, E. Marcon (VB version)
* Created: 25 October 2002
* Modified: 29 September 2003
* URL: http://www.codeproject.com/csharp/command_line.asp
*
* Version: 1.1
*/

using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace wyUpdate.Common
{
    public sealed class Arguments
    {
        // Variables
        private StringDictionary Parameters;

        // Constructors
        public Arguments(string Args)
        {
            Regex Extractor = new Regex(@"(['""][^""]+['""])\s*|([^\s]+)\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            MatchCollection Matches;
            // Get matches (first string ignored because Environment.CommandLine starts with program filename)
            Matches = Extractor.Matches(Args);
            string[] Parts = new string[Matches.Count - 1];
            for (int i = 1; i < Matches.Count; i++) Parts[i - 1] = Matches[i].Value.Trim();
            Extract(Parts);
        }

        public Arguments(string[] Args)
        {
            Extract(Args);
        }
        // Extract command line parameters and values stored in a string array
        private void Extract(string[] Args)
        {
            Parameters = new StringDictionary();
            Regex Spliter = new Regex(@"^([/-]|--){1}(?<name>\w+)([:=])?(?<value>.+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            char[] TrimChars ={ '"', '\'' };
            string Parameter = null;
            Match Part;
            // Valid parameters forms:
            // {-,/,--}param{ ,=,:}((",')value(",'))
            // Examples: -param1 value1 --param2 /param3:"Test-:-work" /param4=happy -param5 '--=nice=--'

            foreach (string Arg in Args)
            {
                Part = Spliter.Match(Arg);
                if (!Part.Success)
                {
                    // Found a value (for the last parameter found (space separator))
                    if (Parameter != null) Parameters[Parameter] = Arg.Trim(TrimChars);
                }
                else
                {
                    // Matched a name, optionally with inline value
                    Parameter = Part.Groups["name"].Value;
                    Parameters.Add(Parameter, Part.Groups["value"].Value.Trim(TrimChars));
                }
            }
        }
        // Retrieve a parameter value if it exists
        public string this[string Param]
        {
            get
            {
                return (Parameters[Param]);
            }
        }
    }
}