using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace wyDay.Controls
{
    public class RichTextBoxLink
    {
        public int StartIndex;
        public int Length;
        public string LinkTarget;

        public RichTextBoxLink(string linktarget)
        {
            LinkTarget = linktarget;
        }

        public RichTextBoxLink(int startIndex)
        {
            StartIndex = startIndex;
        }
    }

    public delegate void LinkHandler(object sender, string linkTarget);

	public class RichTextBoxEx : RichTextBox
	{
		#region Interop-Defines
		[ StructLayout( LayoutKind.Sequential )]
		private struct CHARFORMAT2_STRUCT
		{
			public UInt32	cbSize; 
			public UInt32   dwMask; 
			public UInt32   dwEffects; 
			public Int32    yHeight; 
			public Int32    yOffset; 
			public Int32	crTextColor; 
			public byte     bCharSet; 
			public byte     bPitchAndFamily; 
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=32)]
			public char[]   szFaceName; 
			public UInt16	wWeight;
			public UInt16	sSpacing;
			public int		crBackColor; // Color.ToArgb() -> int
			public int		lcid;
			public int		dwReserved;
			public Int16	sStyle;
			public Int16	wKerning;
			public byte		bUnderlineType;
			public byte		bAnimation;
			public byte		bRevAuthor;
			public byte		bReserved1;
		}

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		private const int WM_USER			 = 0x0400;
		private const int EM_GETCHARFORMAT	 = WM_USER+58;
		private const int EM_SETCHARFORMAT	 = WM_USER+68;

		private const int SCF_SELECTION	= 0x0001;
		private const int SCF_WORD		= 0x0002;
		private const int SCF_ALL		= 0x0004;

		#region CHARFORMAT2 Flags
		private const UInt32 CFE_BOLD		= 0x0001;
		private const UInt32 CFE_ITALIC		= 0x0002;
		private const UInt32 CFE_UNDERLINE	= 0x0004;
		private const UInt32 CFE_STRIKEOUT	= 0x0008;
		private const UInt32 CFE_PROTECTED	= 0x0010;
		private const UInt32 CFE_LINK		= 0x0020;
		private const UInt32 CFE_AUTOCOLOR	= 0x40000000;
		private const UInt32 CFE_SUBSCRIPT	= 0x00010000;		/* Superscript and subscript are */
		private const UInt32 CFE_SUPERSCRIPT= 0x00020000;		/*  mutually exclusive			 */

		private const int CFM_SMALLCAPS		= 0x0040;			/* (*)	*/
		private const int CFM_ALLCAPS		= 0x0080;			/* Displayed by 3.0	*/
		private const int CFM_HIDDEN		= 0x0100;			/* Hidden by 3.0 */
		private const int CFM_OUTLINE		= 0x0200;			/* (*)	*/
		private const int CFM_SHADOW		= 0x0400;			/* (*)	*/
		private const int CFM_EMBOSS		= 0x0800;			/* (*)	*/
		private const int CFM_IMPRINT		= 0x1000;			/* (*)	*/
		private const int CFM_DISABLED		= 0x2000;
		private const int CFM_REVISED		= 0x4000;

		private const int CFM_BACKCOLOR		= 0x04000000;
		private const int CFM_LCID			= 0x02000000;
		private const int CFM_UNDERLINETYPE	= 0x00800000;		/* Many displayed by 3.0 */
		private const int CFM_WEIGHT		= 0x00400000;
		private const int CFM_SPACING		= 0x00200000;		/* Displayed by 3.0	*/
		private const int CFM_KERNING		= 0x00100000;		/* (*)	*/
		private const int CFM_STYLE			= 0x00080000;		/* (*)	*/
		private const int CFM_ANIMATION		= 0x00040000;		/* (*)	*/
		private const int CFM_REVAUTHOR		= 0x00008000;


		private const UInt32 CFM_BOLD		= 0x00000001;
		private const UInt32 CFM_ITALIC		= 0x00000002;
		private const UInt32 CFM_UNDERLINE	= 0x00000004;
		private const UInt32 CFM_STRIKEOUT	= 0x00000008;
		private const UInt32 CFM_PROTECTED	= 0x00000010;
		private const UInt32 CFM_LINK		= 0x00000020;
		private const UInt32 CFM_SIZE		= 0x80000000;
		private const UInt32 CFM_COLOR		= 0x40000000;
		private const UInt32 CFM_FACE		= 0x20000000;
		private const UInt32 CFM_OFFSET		= 0x10000000;
		private const UInt32 CFM_CHARSET	= 0x08000000;
		private const UInt32 CFM_SUBSCRIPT	= CFE_SUBSCRIPT | CFE_SUPERSCRIPT;
		private const UInt32 CFM_SUPERSCRIPT= CFM_SUBSCRIPT;

		private const byte CFU_UNDERLINENONE		= 0x00000000;
		private const byte CFU_UNDERLINE			= 0x00000001;
		private const byte CFU_UNDERLINEWORD		= 0x00000002; /* (*) displayed as ordinary underline	*/
		private const byte CFU_UNDERLINEDOUBLE		= 0x00000003; /* (*) displayed as ordinary underline	*/
		private const byte CFU_UNDERLINEDOTTED		= 0x00000004;
		private const byte CFU_UNDERLINEDASH		= 0x00000005;
		private const byte CFU_UNDERLINEDASHDOT		= 0x00000006;
		private const byte CFU_UNDERLINEDASHDOTDOT	= 0x00000007;
		private const byte CFU_UNDERLINEWAVE		= 0x00000008;
		private const byte CFU_UNDERLINETHICK		= 0x00000009;
		private const byte CFU_UNDERLINEHAIRLINE	= 0x0000000A; /* (*) displayed as ordinary underline	*/

		#endregion

		#endregion


        List<RichTextBoxLink> linkCollection = new List<RichTextBoxLink>();

		public RichTextBoxEx()
		{
			// Otherwise, non-standard links get lost when user starts typing
			// next to a non-standard link
			this.DetectUrls = false;
            BorderStyle = BorderStyle.None;
		}

		[DefaultValue(false)]
		public new bool DetectUrls
		{
			get { return base.DetectUrls; }
			set { base.DetectUrls = value; }
		}

        //create the RichTextBox with a thin border
        protected override CreateParams CreateParams
        {
            get
            {
                //const int WS_BORDER = 0x00800000;
                //const int WS_EX_STATICEDGE = 0x00020000;

                CreateParams cp = base.CreateParams;

                //add the thin 'Static Edge"
                cp.ExStyle |= 0x00020000;

                return cp;
            }
        }

        // 
        string defaultRTFHeader = null;

        public void LoadAndSterilizeRTF(string filename)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(filename);

            // load the file into a string
            string loadedText = file.ReadToEnd();

            Rtf = SterilizeRTF(loadedText);

            file.Close();
        }

        public string SterilizeRTF(string text)
        {
            // if defaultRTF header = null, load it
            if (defaultRTFHeader == null)
                defaultRTFHeader = Rtf;

            StringBuilder sb = new StringBuilder();

            string defaultFontTable;

            // get the font table of the default header

            int startIndex = defaultRTFHeader.IndexOf("\\fonttbl") + 8;
            int indexOn = startIndex;
            int numOpenBraces = 0;



            do
            {
                if (defaultRTFHeader[indexOn] == '{')
                    numOpenBraces++;
                else if (defaultRTFHeader[indexOn] == '}')
                    numOpenBraces--;

                indexOn++;

            } while (numOpenBraces > -1);


            defaultFontTable = defaultRTFHeader.Substring(startIndex, indexOn - startIndex);



            // find the font table of the input text
            indexOn = startIndex = text.IndexOf("\\fonttbl") + 8;

            // add the header from the source text
            sb.Append(text.Substring(0, startIndex));

            // add the font table from the default header
            sb.Append(defaultFontTable);


            // find the end of the source text's fontTable
            numOpenBraces = 0;

            do
            {
                if (text[indexOn] == '{')
                    numOpenBraces++;
                else if (text[indexOn] == '}')
                    numOpenBraces--;

                indexOn++;

            } while (numOpenBraces > -1);


            text = text.Substring(indexOn, text.Length - indexOn);


            // ----- Remove other font sizes

            string defaultFontSize;
            bool atLeaseOneDigit = false;

            startIndex = 0;

            do
            {
                indexOn = startIndex = defaultRTFHeader.IndexOf("\\fs", startIndex) + 3;

                while (char.IsDigit(defaultRTFHeader, indexOn))
                {
                    indexOn++;
                    atLeaseOneDigit = true;
                }

            } while (!atLeaseOneDigit);

            defaultFontSize = defaultRTFHeader.Substring(startIndex, indexOn - startIndex);


            // remove all other font sizes other than "\fs<N>" other than the default "fsM"

            // number of slashes preceding "\fs<N>"
            int numSlashesPreceding;


            indexOn = 0;
            int slashIndex;

            do
            {
                numSlashesPreceding = 0;

                // find a "\fs" element
                slashIndex = text.IndexOf("\\fs", indexOn);
                indexOn = startIndex = slashIndex + 3;

                // count how many slashes precede the "\fs<N>" element
                while (slashIndex > 1 && text[--slashIndex] == '\\')
                {
                    numSlashesPreceding++;
                }
                
                // if it's an even number of slashes (0,2,...) then we're dealing with a "REAL"
                // "\fs<N>" element instead of just escaped slashes followed by the text "fs"

                if (startIndex > 2 && numSlashesPreceding % 2 == 0)
                {
                    sb.Append(text.Substring(0, startIndex));
                    sb.Append(defaultFontSize);

                    while (char.IsDigit(text, startIndex))
                        startIndex++;

                    text = text.Substring(startIndex, text.Length - startIndex);

                    indexOn = 0;
                }

            } while (startIndex > 2); // -1 + 3 = 2 (that is, no more "\fs" found)


            // add the left over text
            sb.Append(text);

            // set the RTF text
            return sb.ToString();
        }


		/// <summary>
		/// Insert a given text at a given position as a link. The link text is followed by
		/// a hash (#) and the given hyperlink text, both of them invisible.
		/// When clicked on, the whole link text and hyperlink string are given in the
		/// LinkClickedEventArgs.
		/// </summary>
		/// <param name="text">Text to be inserted</param>
		/// <param name="hyperlink">Invisible hyperlink string to be inserted</param>
		/// <param name="position">Insert position</param>
		public void InsertLink(string text, string linkText)
		{
            RichTextBoxLink link = new RichTextBoxLink(linkText);

            linkCollection.Add(link);

            link.StartIndex = SelectionStart;
            link.Length = text.Length;


            CreateLink(link, text, linkCollection.Count - 1);
		}

        private void CreateLink(RichTextBoxLink link, string text, int linkOn)
        {
            string hyperlink = linkOn.ToString();
            SelectedRtf = @"{\rtf1\ansi " + FormatStringForRTF(text) + @"\v #" + hyperlink + @"\v0}";
            Select(link.StartIndex, link.Length + hyperlink.Length + 1);
			SetSelectionLink(true);
            Select(link.StartIndex + link.Length + hyperlink.Length + 1, 0);
        }

        public void CreateExistingLinks(List<RichTextBoxLink> links)
        {
            if (links == null)
                return;

            linkCollection = links;

            for (int i = 0; i < links.Count; i++)
            {
                Select(links[i].StartIndex, links[i].Length);

                //TODO: don't overwrite the style (bold, italics, and underline)
                CreateLink(links[i], this.Text.Substring(links[i].StartIndex, links[i].Length), i);  
            }
        }

        private static string FormatStringForRTF(string inString)
        {
            string tmpStr = inString.Replace("\n", "$n$");
            tmpStr = tmpStr.Replace(@"\", @"\\");
            tmpStr = tmpStr.Replace("{", @"\{");
            tmpStr = tmpStr.Replace("}", @"\}");
            tmpStr = tmpStr.Replace("$n$", @"\par ");
            return tmpStr;
        }

		/// <summary>
		/// Set the current selection's link style
		/// </summary>
		/// <param name="link">true: set link style, false: clear link style</param>
		public void SetSelectionLink(bool link)
		{
			SetSelectionStyle(CFM_LINK, link ? CFE_LINK : 0);
		}

		/// <summary>
		/// Get the link style for the current selection
		/// </summary>
		/// <returns>0: link style not set, 1: link style set, -1: mixed</returns>
		public int GetSelectionLink()
		{
			return GetSelectionStyle(CFM_LINK, CFE_LINK);
		}

		private void SetSelectionStyle(UInt32 mask, UInt32 effect)
		{
			CHARFORMAT2_STRUCT cf = new CHARFORMAT2_STRUCT();
			cf.cbSize = (UInt32)Marshal.SizeOf(cf);
			cf.dwMask = mask;
			cf.dwEffects = effect;

			IntPtr wpar = new IntPtr(SCF_SELECTION);
			IntPtr lpar = Marshal.AllocCoTaskMem( Marshal.SizeOf( cf ) ); 
			Marshal.StructureToPtr(cf, lpar, false);

			IntPtr res = SendMessage(Handle, EM_SETCHARFORMAT, wpar, lpar);

			Marshal.FreeCoTaskMem(lpar);
		}

		private int GetSelectionStyle(UInt32 mask, UInt32 effect)
		{
			CHARFORMAT2_STRUCT cf = new CHARFORMAT2_STRUCT();
			cf.cbSize = (UInt32)Marshal.SizeOf(cf);
			cf.szFaceName = new char[32];

			IntPtr wpar = new IntPtr(SCF_SELECTION);
			IntPtr lpar = 	Marshal.AllocCoTaskMem( Marshal.SizeOf( cf ) ); 
			Marshal.StructureToPtr(cf, lpar, false);

			IntPtr res = SendMessage(Handle, EM_GETCHARFORMAT, wpar, lpar);

			cf = (CHARFORMAT2_STRUCT)Marshal.PtrToStructure(lpar, typeof(CHARFORMAT2_STRUCT));

			int state;
			// dwMask holds the information which properties are consistent throughout the selection:
			if ((cf.dwMask & mask) == mask) 
			{
				if ((cf.dwEffects & effect) == effect)
					state = 1;
				else
					state = 0;
			}
			else
			{
				state = -1;
			}
			
			Marshal.FreeCoTaskMem(lpar);
			return state;
		}


        public new event LinkHandler LinkClicked;


        protected override void OnLinkClicked(LinkClickedEventArgs e)
        {
            int linkIndex = int.Parse(e.LinkText.Substring(linkIndex = e.LinkText.LastIndexOf('#') + 1, e.LinkText.Length - linkIndex));


            if (LinkClicked != null)
                LinkClicked(this, linkCollection[linkIndex].LinkTarget);


            base.OnLinkClicked(e);
        }

	}
}
