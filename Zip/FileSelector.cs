// FileSelector.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2006, 2007, 2008, 2009 Microsoft Corporation.  All rights reserved.
//
// This code is released under the Microsoft Public License . 
// See the License.txt for details.  
//
// ------------------------------------------------------------------
//
// This module implements a "file selector" that finds files based on a set of inclusion criteria,
// including filename, size, file time, and potentially file attributes.
// The criteria are given in a string with a simple expression language. Examples: 
// 
// find all .txt files: 
//     name = *.txt 
//
// shorthand for the above
//     *.txt
//
// all files modified after January 1st, 2009
//     mtime > 2009-01-01
//
// All .txt files modified after the first of the year
//     name = *.txt  AND  mtime > 2009-01-01
//
// All .txt files modified after the first of the year, or any file with the archive bit set
//     (name = *.txt  AND  mtime > 2009-01-01) or (attribtues = A)
//
// All .txt files or any file greater than 1mb in size
//     (name = *.txt  or  size > 1mb)
//
// and so on.
// ------------------------------------------------------------------


using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Ionic
{

    /// <summary>
    /// Enumerates the options for a logical conjunction. This enum is intended for use 
    /// internally by the FileSelector class.
    /// </summary>
    internal enum LogicalConjunction
    {
        NONE,
        AND,
        OR,
        XOR,
    }

    internal enum WhichTime
    {
        atime,
        mtime,
        ctime,
    }


    internal enum ComparisonOperator
    {
        [Description(">")]
        GreaterThan,
        [Description(">=")]
        GreaterThanOrEqualTo,
        [Description("<")]
        LesserThan,
        [Description("<=")]
        LesserThanOrEqualTo,
        [Description("=")]
        EqualTo,
        [Description("!=")]
        NotEqualTo
    }


    internal abstract partial class SelectionCriterion
    {
        internal abstract bool Evaluate(string filename);
    }


    internal partial class SizeCriterion : SelectionCriterion
    {
        internal ComparisonOperator Operator;
        internal Int64 Size;

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("size ").Append(EnumUtil.GetDescription(Operator)).Append(" ").Append(Size.ToString());
            return sb.ToString();
        }

        internal override bool Evaluate(string filename)
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(filename);
            return _Evaluate(fi.Length);
        }

        private bool _Evaluate(Int64 Length)
        {
            bool result = false;
            switch (Operator)
            {
                case ComparisonOperator.GreaterThanOrEqualTo:
                    result = Length >= Size;
                    break;
                case ComparisonOperator.GreaterThan:
                    result = Length > Size;
                    break;
                case ComparisonOperator.LesserThanOrEqualTo:
                    result = Length <= Size;
                    break;
                case ComparisonOperator.LesserThan:
                    result = Length < Size;
                    break;
                case ComparisonOperator.EqualTo:
                    result = Length == Size;
                    break;
                case ComparisonOperator.NotEqualTo:
                    result = Length != Size;
                    break;
                default:
                    throw new ArgumentException("Operator");
            }
            return result;
        }

    }



    internal partial class TimeCriterion : SelectionCriterion
    {
        internal ComparisonOperator Operator;
        internal WhichTime Which;
        internal DateTime Time;

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Which.ToString()).Append(" ").Append(EnumUtil.GetDescription(Operator)).Append(" ").Append(Time.ToString("yyyy-MM-dd-HH:mm:ss"));
            return sb.ToString();
        }

        internal override bool Evaluate(string filename)
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(filename);
            DateTime x;
            switch (Which)
            {
                case WhichTime.atime:
                    x = System.IO.File.GetLastAccessTime(filename);
                    break;
                case WhichTime.mtime:
                    x = System.IO.File.GetLastWriteTime(filename);
                    break;
                case WhichTime.ctime:
                    x = System.IO.File.GetCreationTime(filename);
                    break;
                default:
                    throw new ArgumentException("Operator");
            }
            return _Evaluate(x);
        }


        private bool _Evaluate(DateTime x)
        {

            bool result = false;
            switch (Operator)
            {
                case ComparisonOperator.GreaterThanOrEqualTo:
                    result = (x >= Time);
                    break;
                case ComparisonOperator.GreaterThan:
                    result = (x > Time);
                    break;
                case ComparisonOperator.LesserThanOrEqualTo:
                    result = (x <= Time);
                    break;
                case ComparisonOperator.LesserThan:
                    result = (x < Time);
                    break;
                case ComparisonOperator.EqualTo:
                    result = (x == Time);
                    break;
                case ComparisonOperator.NotEqualTo:
                    result = (x != Time);
                    break;
                default:
                    throw new ArgumentException("Operator");
            }

            //Console.WriteLine("TimeCriterion[{2}]({0})= {1}", filename, result, Which.ToString());
            return result;
        }
    }



    internal partial class NameCriterion : SelectionCriterion
    {
        private Regex _re;
        private String _regexString;
        internal ComparisonOperator Operator;
        private string _MatchingFileSpec;
        internal virtual string MatchingFileSpec
        {
            set
            {
                _MatchingFileSpec = value;
                _regexString = "^" +
                Regex.Escape(value)
                .Replace(@"\*\.\*", @"([^\.]+|.*\.[^\\\.]*)")
                .Replace(@"\.\*", @"\.[^\\\.]*")
                .Replace(@"\*", @".*")
                .Replace(@"\?", @"[^\\\.]")
                + "$";

                // neither of these is correct
                //if (!_regexString.StartsWith(@"\\")) _regexString = @"\\" + _regexString;
                //if (_regexString.IndexOf("\\") == -1)  _regexString = @"\\" + _regexString;
                _re = new Regex(_regexString, RegexOptions.IgnoreCase);
            }
        }


        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("name = ").Append(_MatchingFileSpec);
            return sb.ToString();
        }


        internal override bool Evaluate(string filename)
        {
            return _Evaluate(filename);
        }

        private bool _Evaluate(string fullpath)
        {
            // No slash in the pattern implicitly means recurse, which means compare to 
            // filename only, not full path.
            String f = (_MatchingFileSpec.IndexOf('\\') == -1)
                ? System.IO.Path.GetFileName(fullpath)
                : fullpath; // compare to fullpath

            bool result = _re.IsMatch(f);
            if (Operator != ComparisonOperator.EqualTo)
                result = !result;
            return result;
        }
    }



    internal partial class AttributesCriterion : SelectionCriterion
    {
        private FileAttributes _Attributes;
        internal ComparisonOperator Operator;
        internal string AttributeString
        {
            get
            {
                string result = "";
                if ((_Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                    result += "H";
                if ((_Attributes & FileAttributes.System) == FileAttributes.System)
                    result += "S";
                if ((_Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    result += "R";
                if ((_Attributes & FileAttributes.Archive) == FileAttributes.Archive)
                    result += "A";
                if ((_Attributes & FileAttributes.NotContentIndexed) == FileAttributes.NotContentIndexed)
                    result += "I";
                return result;
            }

            set
            {
                _Attributes = FileAttributes.Normal;
                foreach (char c in value.ToUpper())
                {
                    switch (c)
                    {
                        case 'H':
                            if ((_Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                                throw new ArgumentException(String.Format("Repeated flag. ({0})", c), "value");
                            _Attributes |= FileAttributes.Hidden;
                            break;

                        case 'R':
                            if ((_Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                throw new ArgumentException(String.Format("Repeated flag. ({0})", c), "value");
                            _Attributes |= FileAttributes.ReadOnly;
                            break;

                        case 'S':
                            if ((_Attributes & FileAttributes.System) == FileAttributes.System)
                                throw new ArgumentException(String.Format("Repeated flag. ({0})", c), "value");
                            _Attributes |= FileAttributes.System;
                            break;

                        case 'A':
                            if ((_Attributes & FileAttributes.Archive) == FileAttributes.Archive)
                                throw new ArgumentException(String.Format("Repeated flag. ({0})", c), "value");
                            _Attributes |= FileAttributes.Archive;
                            break;

                        case 'I':
                            if ((_Attributes & FileAttributes.NotContentIndexed) == FileAttributes.NotContentIndexed)
                                throw new ArgumentException(String.Format("Repeated flag. ({0})", c), "value");
                            _Attributes |= FileAttributes.NotContentIndexed;
                            break;
                        default:
                            throw new ArgumentException(value);
                    }
                }
            }
        }


        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("attributes ").Append(EnumUtil.GetDescription(Operator)).Append(" ").Append(AttributeString);
            return sb.ToString();
        }

        private bool _EvaluateOne(FileAttributes fileAttrs, FileAttributes criterionAttrs)
        {
            bool result = false;
            if ((_Attributes & criterionAttrs) == criterionAttrs)
                result = ((fileAttrs & criterionAttrs) == criterionAttrs);
            else
                result = true;
            return result;
        }



        internal override bool Evaluate(string filename)
        {
#if NETCF
		FileAttributes fileAttrs = NetCfFile.GetAttributes(filename);
#else
            FileAttributes fileAttrs = System.IO.File.GetAttributes(filename);
#endif

            return _Evaluate(fileAttrs);
        }

        private bool _Evaluate(FileAttributes fileAttrs)
        {
            //Console.WriteLine("fileattrs[{0}]={1}", filename, fileAttrs.ToString());

            bool result = _EvaluateOne(fileAttrs, FileAttributes.Hidden);
            if (result)
                result = _EvaluateOne(fileAttrs, FileAttributes.System);
            if (result)
                result = _EvaluateOne(fileAttrs, FileAttributes.ReadOnly);
            if (result)
                result = _EvaluateOne(fileAttrs, FileAttributes.Archive);

            if (Operator != ComparisonOperator.EqualTo)
                result = !result;

            //Console.WriteLine("AttributesCriterion[{2}]({0})= {1}", filename, result, AttributeString);

            return result;
        }
    }



    internal partial class CompoundCriterion : SelectionCriterion
    {
        internal LogicalConjunction Conjunction;
        internal SelectionCriterion Left;

        private SelectionCriterion _Right;
        internal SelectionCriterion Right
        {
            get { return _Right; }
            set
            {
                _Right = value;
                if (value == null)
                    Conjunction = LogicalConjunction.NONE;
                else if (Conjunction == LogicalConjunction.NONE)
                    Conjunction = LogicalConjunction.AND;
            }
        }


        internal override bool Evaluate(string filename)
        {
            bool result = Left.Evaluate(filename);
            switch (Conjunction)
            {
                case LogicalConjunction.AND:
                    if (result)
                        result = Right.Evaluate(filename);
                    break;
                case LogicalConjunction.OR:
                    if (!result)
                        result = Right.Evaluate(filename);
                    break;
                case LogicalConjunction.XOR:
                    result ^= Right.Evaluate(filename);
                    break;
                default:
                    throw new ArgumentException("Conjunction");
            }
            return result;
        }


        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(")
            .Append((Left != null) ? Left.ToString() : "null")
            .Append(" ")
            .Append(Conjunction.ToString())
            .Append(" ")
            .Append((Right != null) ? Right.ToString() : "null")
            .Append(")");
            return sb.ToString();
        }
    }



    /// <summary>
    /// FileSelector encapsulates logic that selects files from a source based on a set
    /// of criteria.  
    /// </summary>
    /// <remarks>
    ///
    /// <para>
    /// Typically, an application that creates or manipulates Zip archives will not directly
    /// interact with the FileSelector class.  The FileSelector class is used internally by the
    /// ZipFile class for selecting files for inclusion into the ZipFile, when the <see
    /// cref="Ionic.Zip.ZipFile.AddSelectedFiles(String,String)"/> method is called.
    /// </para>
    ///
    /// <para>
    /// But, some applications may wish to use the FileSelector class directly, to select
    /// files from disk volumes based on a set of criteria, without creating or querying Zip
    /// archives.  The file selection criteria include: a pattern to match the filename; the
    /// last modified, created, or last accessed time of the file; the size of the file; and
    /// the attributes of the file.
    /// </para>
    /// </remarks>
    public partial class FileSelector
    {
        internal SelectionCriterion _Criterion;

        /// <summary>
        /// The default constructor.  
        /// </summary>
        /// <remarks>
        /// Typically, applications won't use this constructor.  Instead they'll call the
        /// constructor that accepts a selectionCriteria string.  If you use this constructor,
        /// you'll want to set the SelectionCriteria property on the instance before calling
        /// SelectFiles().
        /// </remarks>
        protected FileSelector() { }


        /// <summary>
        /// Constructor that allows the caller to specify file selection criteria.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This constructor allows the caller to specify a set of criteria for selection of files.
        /// </para>
        /// 
        /// <para>
        /// See <see cref="FileSelector.SelectionCriteria"/> for a description of the syntax of 
        /// the selectionCriteria string.
        /// </para>
        /// </remarks>
        /// 
        /// <param name="selectionCriteria">The criteria for file selection.</param>
        public FileSelector(String selectionCriteria)
        {
            if (!String.IsNullOrEmpty(selectionCriteria))
                _Criterion = _ParseCriterion(selectionCriteria);
        }



        /// <summary>
        /// The string specifying which files to include when retrieving.
        /// </summary>
        /// <remarks>
        ///         
        /// <para>
        /// Specify the criteria in statements of 3 elements: a noun, an operator, and a value.
        /// Consider the string "name != *.doc" .  The noun is "name".  The operator is "!=",
        /// implying "Not Equal".  The value is "*.doc".  That criterion, in English, says "all
        /// files with a name that does not end in the .doc extension."
        /// </para> 
        ///
        /// <para>
        /// Supported nouns include "name" for the filename; "atime", "mtime", and "ctime" for
        /// last access time, last modfied time, and created time of the file, respectively;
        /// "attributes" for the file attributes; and "size" for the file length (uncompressed).
        /// The "attributes" and "name" nouns both support = and != as operators.  The "size",
        /// "atime", "mtime", and "ctime" nouns support = and !=, and &gt;, &gt;=, &lt;, &lt;=
        /// as well.
        /// </para> 
        ///
        /// <para>
        /// Specify values for the file attributes as a string with one or more of the
        /// characters H,R,S,A,I in any order, implying Hidden, ReadOnly, System, Archive,
        /// and NotContextIndexed, 
        /// respectively.  To specify a time, use YYYY-MM-DD-HH:mm:ss as the format.  If you
        /// omit the HH:mm:ss portion, it is assumed to be 00:00:00 (midnight). The value for a
        /// size criterion is expressed in integer quantities of bytes, kilobytes (use k or kb
        /// after the number), megabytes (m or mb), or gigabytes (g or gb).  The value for a
        /// name is a pattern to match against the filename, potentially including wildcards.
        /// The pattern follows CMD.exe glob rules: * implies one or more of any character,
        /// while ? implies one character.  If the name pattern contains any slashes, it is
        /// matched to the entire filename, including the path; otherwise, it is matched
        /// against only the filename without the path.  This means a pattern of "*\*.*" matches 
        /// all files one directory level deep, while a pattern of "*.*" matches all files in 
        /// all directories.    
        /// </para> 
        ///
        /// <para>
        /// To specify a name pattern that includes spaces, use single quotes around the pattern.
        /// A pattern of "'* *.*'" will match all files that have spaces in the filename.  The full 
        /// criteria string for that would be "name = '* *.*'" . 
        /// </para> 
        ///
        /// <para>
        /// Some examples: a string like "attributes != H" retrieves all entries whose
        /// attributes do not include the Hidden bit.  A string like "mtime > 2009-01-01"
        /// retrieves all entries with a last modified time after January 1st, 2009.  For
        /// example "size &gt; 2gb" retrieves all entries whose uncompressed size is greater
        /// than 2gb.
        /// </para> 
        ///
        /// <para>
        /// You can combine criteria with the conjunctions AND, OR, and XOR. Using a string like
        /// "name = *.txt AND size &gt;= 100k" for the selectionCriteria retrieves entries whose
        /// names end in .txt, and whose uncompressed size is greater than or equal to 100
        /// kilobytes.
        /// </para>
        ///
        /// <para>
        /// For more complex combinations of criteria, you can use parenthesis to group clauses
        /// in the boolean logic.  Absent parenthesis, the precedence of the criterion atoms is
        /// determined by order of appearance.  Unlike the C# language, the AND conjunction does
        /// not take precendence over the logical OR.  This is important only in strings that
        /// contain 3 or more criterion atoms.  In other words, "name = *.txt and size &gt; 1000
        /// or attributes = H" implies "((name = *.txt AND size &gt; 1000) OR attributes = H)"
        /// while "attributes = H OR name = *.txt and size &gt; 1000" evaluates to "((attributes
        /// = H OR name = *.txt) AND size &gt; 1000)".  When in doubt, use parenthesis.
        /// </para>
        ///
        /// <para>
        /// Using time properties requires some extra care. If you want to retrieve all entries
        /// that were last updated on 2009 February 14, specify "mtime &gt;= 2009-02-14 AND
        /// mtime &lt; 2009-02-15".  Read this to say: all files updated after 12:00am on
        /// February 14th, until 12:00am on February 15th.  You can use the same bracketing
        /// approach to specify any time period - a year, a month, a week, and so on.
        /// </para>
        ///
        /// <para>
        /// The syntax allows one special case: if you provide a string with no spaces, it is treated as
        /// a pattern to match for the filename.  Therefore a string like "*.xls" will be equivalent to 
        /// specifying "name = *.xls".  
        /// </para>
        /// 
        /// <para>
        /// There is no logic in this class that insures that the inclusion criteria
        /// are internally consistent.  For example, it's possible to specify criteria that
        /// says the file must have a size of less than 100 bytes, as well as a size that
        /// is greater than 1000 bytes.  Obviously no file will ever satisfy such criteria,
        /// but this class does not check and find such inconsistencies.
        /// </para>
        /// 
        /// </remarks>
        ///
        /// <exception cref="System.Exception">
        /// Thrown in the setter if the value has an invalid syntax.
        /// </exception>
        public String SelectionCriteria
        {
            get
            {
                if (_Criterion == null) return null;
                return _Criterion.ToString();
            }
            set
            {
                if (value == null) _Criterion = null;
                else if (value.Trim() == "") _Criterion = null;
                else
                    _Criterion = _ParseCriterion(value);
            }
        }


        private enum ParseState
        {
            Start,
            OpenParen,
            CriterionDone,
            ConjunctionPending,
            Whitespace,
        }



        private SelectionCriterion _ParseCriterion(String s)
        {
            if (s == null) return null;

            // shorthand for filename glob
            if (s.IndexOf(" ") == -1)
                s = "name = " + s;

            // inject spaces after open paren and before close paren
            string[] prPairs = { @"\((\S)", "( $1", @"(\S)\)", "$1 )", };
            for (int i = 0; i + 1 < prPairs.Length; i += 2)
            {
                Regex rgx = new Regex(prPairs[i]);
                s = rgx.Replace(s, prPairs[i + 1]);
            }

            // split the expression into tokens 
            string[] tokens = s.Trim().Split(' ', '\t');

            if (tokens.Length < 3) throw new ArgumentException(s);

            SelectionCriterion current = null;

            LogicalConjunction pendingConjunction = LogicalConjunction.NONE;

            ParseState state;
            var stateStack = new System.Collections.Generic.Stack<ParseState>();
            var critStack = new System.Collections.Generic.Stack<SelectionCriterion>();
            stateStack.Push(ParseState.Start);

            for (int i = 0; i < tokens.Length; i++)
            {
                switch (tokens[i].ToLower())
                {
                    case "and":
                    case "xor":
                    case "or":
                        state = stateStack.Peek();
                        if (state != ParseState.CriterionDone)
                            throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));

                        if (tokens.Length <= i + 3)
                            throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));

                        pendingConjunction = (LogicalConjunction)Enum.Parse(typeof(LogicalConjunction), tokens[i].ToUpper());
                        current = new CompoundCriterion { Left = current, Right = null, Conjunction = pendingConjunction };
                        stateStack.Push(state);
                        stateStack.Push(ParseState.ConjunctionPending);
                        critStack.Push(current);
                        break;

                    case "(":
                        state = stateStack.Peek();
                        if (state != ParseState.Start && state != ParseState.ConjunctionPending && state != ParseState.OpenParen)
                            throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));

                        if (tokens.Length <= i + 4)
                            throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));

                        stateStack.Push(ParseState.OpenParen);
                        break;

                    case ")":
                        state = stateStack.Pop();
                        if (stateStack.Peek() != ParseState.OpenParen)
                            throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));

                        stateStack.Pop();
                        stateStack.Push(ParseState.CriterionDone);
                        break;

                    case "atime":
                    case "ctime":
                    case "mtime":
                        if (tokens.Length <= i + 2)
                            throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));

                        DateTime t;
                        try
                        {
                            t = DateTime.ParseExact(tokens[i + 2], "yyyy-MM-dd-HH:mm:ss", null);
                        }
                        catch
                        {
                            t = DateTime.ParseExact(tokens[i + 2], "yyyy-MM-dd", null);
                        }
                        current = new TimeCriterion
                        {
                            Which = (WhichTime)Enum.Parse(typeof(WhichTime), tokens[i]),
                            Operator = (ComparisonOperator)EnumUtil.Parse(typeof(ComparisonOperator), tokens[i + 1]),
                            Time = t
                        };
                        i += 2;
                        stateStack.Push(ParseState.CriterionDone);
                        break;


                    case "length":
                    case "size":
                        if (tokens.Length <= i + 2)
                            throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));

                        Int64 sz = 0;
                        string v = tokens[i + 2];
                        if (v.ToUpper().EndsWith("K"))
                            sz = Int64.Parse(v.Substring(0, v.Length - 1)) * 1024;
                        else if (v.ToUpper().EndsWith("KB"))
                            sz = Int64.Parse(v.Substring(0, v.Length - 2)) * 1024;
                        else if (v.ToUpper().EndsWith("M"))
                            sz = Int64.Parse(v.Substring(0, v.Length - 1)) * 1024 * 1024;
                        else if (v.ToUpper().EndsWith("MB"))
                            sz = Int64.Parse(v.Substring(0, v.Length - 2)) * 1024 * 1024;
                        else if (v.ToUpper().EndsWith("G"))
                            sz = Int64.Parse(v.Substring(0, v.Length - 1)) * 1024 * 1024 * 1024;
                        else if (v.ToUpper().EndsWith("GB"))
                            sz = Int64.Parse(v.Substring(0, v.Length - 2)) * 1024 * 1024 * 1024;
                        else sz = Int64.Parse(tokens[i + 2]);

                        current = new SizeCriterion
                        {
                            Size = sz,
                            Operator = (ComparisonOperator)EnumUtil.Parse(typeof(ComparisonOperator), tokens[i + 1])
                        };
                        i += 2;
                        stateStack.Push(ParseState.CriterionDone);
                        break;

                    case "filename":
                    case "name":
                        {
                            if (tokens.Length <= i + 2)
                                throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));

                            ComparisonOperator c =
                                (ComparisonOperator)EnumUtil.Parse(typeof(ComparisonOperator), tokens[i + 1]);

                            if (c != ComparisonOperator.NotEqualTo && c != ComparisonOperator.EqualTo)
                                throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));

                            string m = tokens[i + 2];
                            // handle single-quoted filespecs (used to include spaces in filename patterns)
                            if (m.StartsWith("'"))
                            {
                                int ix = i;
                                if (!m.EndsWith("'"))
                                {
                                    do
                                    {
                                        i++;
                                        if (tokens.Length <= i + 2)
                                            throw new ArgumentException(String.Join(" ", tokens, ix, tokens.Length - ix));
                                        m += " " + tokens[i + 2];
                                    } while (!tokens[i + 2].EndsWith("'"));
                                }
                                // trim off leading and trailing single quotes
                                m = m.Substring(1, m.Length - 2);
                            }

                            current = new NameCriterion
                            {
                                MatchingFileSpec = m,
                                Operator = c
                            };
                            i += 2;
                            stateStack.Push(ParseState.CriterionDone);
                        }
                        break;

                    case "attributes":
                        {
                            if (tokens.Length <= i + 2)
                                throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));

                            ComparisonOperator c =
                                (ComparisonOperator)EnumUtil.Parse(typeof(ComparisonOperator), tokens[i + 1]);

                            if (c != ComparisonOperator.NotEqualTo && c != ComparisonOperator.EqualTo)
                                throw new ArgumentException(String.Join(" ", tokens, i, tokens.Length - i));

                            current = new AttributesCriterion
                            {
                                AttributeString = tokens[i + 2],
                                Operator = c
                            };
                            i += 2;
                            stateStack.Push(ParseState.CriterionDone);
                        }
                        break;

                    case "":
                        // NOP
                        stateStack.Push(ParseState.Whitespace);
                        break;

                    default:
                        throw new ArgumentException("'" + tokens[i] + "'");
                }

                state = stateStack.Peek();
                if (state == ParseState.CriterionDone)
                {
                    stateStack.Pop();
                    if (stateStack.Peek() == ParseState.ConjunctionPending)
                    {
                        while (stateStack.Peek() == ParseState.ConjunctionPending)
                        {
                            var cc = critStack.Pop() as CompoundCriterion;
                            cc.Right = current;
                            current = cc; // mark the parent as current (walk up the tree)
                            stateStack.Pop();   // the conjunction is no longer pending 

                            state = stateStack.Pop();
                            if (state != ParseState.CriterionDone)
                                throw new ArgumentException();
                        }
                    }
                    else stateStack.Push(ParseState.CriterionDone);  // not sure?
                }

                if (state == ParseState.Whitespace)
                    stateStack.Pop();
            }

            return current;
        }


        /// <summary>
        /// Returns a string representation of the FileSelector object.
        /// </summary>
        /// <returns>The string representation of the boolean logic statement of the file
        /// selection criteria for this instance. </returns>
        public override String ToString()
        {
            return _Criterion.ToString();
        }


        private bool Evaluate(string filename)
        {
            bool result = _Criterion.Evaluate(filename);
            return result;
        }


        /// <summary>
        /// Returns the names of the files in the specified directory
        /// that fit the selection criteria specified in the FileSelector.
        /// </summary>
        ///
        /// <remarks>
        /// This is equivalent to calling <see cref="SelectFiles(String, bool)"/> 
        /// with recurseDirectories = false.
        /// </remarks>
        ///
        /// <param name="directory">
        /// The name of the directory over which to apply the FileSelector criteria.
        /// </param>
        ///
        /// <returns>
        /// A collection of strings containing fully-qualified pathnames of files
        /// that match the criteria specified in the FileSelector instance.
        /// </returns>
        public System.Collections.Generic.ICollection<String> SelectFiles(String directory)
        {
            return SelectFiles(directory, false);
        }


        /// <summary>
        /// Returns the names of the files in the specified directory that fit the selection
        /// criteria specified in the FileSelector, optionally recursing through subdirectories.
        /// </summary>
        ///
        /// <remarks>
        /// This method applies the file selection criteria contained in the FileSelector to the 
        /// files contained in the given directory, and returns the names of files that 
        /// conform to the criteria. 
        /// </remarks>
        ///
        /// <param name="directory">
        /// The name of the directory over which to apply the FileSelector criteria.
        /// </param>
        ///
        /// <param name="recurseDirectories">
        /// Whether to recurse through subdirectories when applying the file selection criteria.
        /// </param>
        ///
        /// <returns>
        /// An collection of strings containing fully-qualified pathnames of files
        /// that match the criteria specified in the FileSelector instance.
        /// </returns>
        public System.Collections.Generic.ICollection<String> SelectFiles(String directory, bool recurseDirectories)
        {
            if (_Criterion == null)
                throw new ArgumentException("SelectionCriteria has not been set");

            var list = new System.Collections.Generic.List<String>();
            try
            {
                String[] filenames = System.IO.Directory.GetFiles(directory);

                // add the files: 
                foreach (String filename in filenames)
                {
                    if (Evaluate(filename))
                        list.Add(filename);
                }

                if (recurseDirectories)
                {
                    // add the subdirectories:
                    String[] dirnames = System.IO.Directory.GetDirectories(directory);
                    foreach (String dir in dirnames)
                    {
                        list.AddRange(this.SelectFiles(dir, recurseDirectories));
                    }
                }
            }
            // can get System.UnauthorizedAccessException here 
            catch { }

            return list;
        }
    }




    /// <summary>
    /// Summary description for EnumUtil.
    /// </summary>
    internal sealed class EnumUtil
    {
        /// <summary>
        /// Returns the value of the DescriptionAttribute if the specified Enum value has one.
        /// If not, returns the ToString() representation of the Enum value.
        /// </summary>
        /// <param name="value">The Enum to get the description for</param>
        /// <returns></returns>
        internal static string GetDescription(System.Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more 
        /// enumerated constants to an equivalent enumerated object.
        /// Note: use the DescriptionAttribute on enum values to enable this.
        /// </summary>
        /// <param name="enumType">The System.Type of the enumeration.</param>
        /// <param name="stringRepresentation">A string containing the name or value to convert.</param>
        /// <returns></returns>
        internal static object Parse(Type enumType, string stringRepresentation)
        {
            return Parse(enumType, stringRepresentation, false);
        }

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more 
        /// enumerated constants to an equivalent enumerated object.
        /// A parameter specified whether the operation is case-sensitive.
        /// Note: use the DescriptionAttribute on enum values to enable this.
        /// </summary>
        /// <param name="enumType">The System.Type of the enumeration.</param>
        /// <param name="stringRepresentation">A string containing the name or value to convert.</param>
        /// <param name="ignoreCase">Whether the operation is case-sensitive or not.</param>
        /// <returns></returns>
        internal static object Parse(Type enumType, string stringRepresentation, bool ignoreCase)
        {
            if (ignoreCase)
                stringRepresentation = stringRepresentation.ToLower();

            foreach (System.Enum enumVal in System.Enum.GetValues(enumType))
            {
                string description = GetDescription(enumVal);
                if (ignoreCase)
                    description = description.ToLower();
                if (description == stringRepresentation)
                    return enumVal;
            }

            return System.Enum.Parse(enumType, stringRepresentation, ignoreCase);
        }
    }


#if DEMO
    public class DemonstrateFileSelector
    {
	// Fields
	private string _directory;
	private bool _recurse;
	private string _selectionCriteria;
	private FileSelector f;

	// Methods
	public DemonstrateFileSelector()
	{
	    this._directory = ".";
	    this._recurse = true;
	}

	public DemonstrateFileSelector(string[] args)
	{
	    this._directory = ".";
	    this._recurse = true;
	    for (int i = 0; i < args.Length; i++)
	    {
		switch(args[i])
		{
		case"-?":
		    Usage();
		    Environment.Exit(0);
		    break;
		case "-directory":
		    i++;
		    if (args.Length <= i)
		    {
			throw new ArgumentException("-directory");
		    }
		    this._directory = args[i];
		    break;
		case "-norecurse":
		    this._recurse = false;
		    break;

		default:
		    if (this._selectionCriteria != null)
		    {
			throw new ArgumentException(args[i]);
		    }
		    this._selectionCriteria = args[i];
		    break;
		}


		if (this._selectionCriteria != null)
		{
		    this.f = new FileSelector(this._selectionCriteria);
		}
	    }
	}

	public static void Main(string[] args)
	{
	    try
	    {
		new DemonstrateFileSelector(args).Run();
	    }
	    catch (Exception exc1)
	    {
		Console.WriteLine("Exception: {0}", exc1.ToString());
		Usage();
	    }
	}


	public void Run()
	{
	    if (this.f == null)
	    {
		this.f = new FileSelector("name = *.jpg AND (size > 1000 OR atime < 2009-02-14-00:00:00)");
	    }
	    Console.WriteLine("\nSelecting files:\n" + this.f.ToString());
	    var files = this.f.SelectFiles(this._directory, this._recurse);
	    if (files.Count == 0)
	    {
		Console.WriteLine("no files.");
	    }
	    else
	    {
		Console.WriteLine("files: {0}", files.Count);
		foreach (string file in files)
		{
		    Console.WriteLine("  " + file);
		}
	    }
	}

	public static void Usage()
	{
	    Console.WriteLine("FileSelector: select files based on selection criteria.\n");
	    Console.WriteLine("Usage:\n  FileSelector <selectionCriteria>  [-directory <dir>] [-norecurse]");
	}
    }

#endif
 





}


