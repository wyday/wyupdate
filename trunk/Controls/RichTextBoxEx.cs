using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace wyDay.Controls
{
    internal delegate void LinkHandler(object sender, string linkTarget);

	internal class RichTextBoxEx : RichTextBox
	{
		#region Interop-Defines
		[StructLayout(LayoutKind.Sequential)]
		struct CHARFORMAT2_STRUCT
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
		static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

		const int WM_USER			 = 0x0400;
		const int EM_GETCHARFORMAT	 = WM_USER+58;
		const int EM_SETCHARFORMAT	 = WM_USER+68;

		const int SCF_SELECTION	= 0x0001;

		#region CHARFORMAT2 Flags

		const UInt32 CFE_LINK		= 0x0020;
        const UInt32 CFM_LINK		= 0x00000020;

		#endregion

		#endregion

		public RichTextBoxEx()
		{
			// Otherwise, non-standard links get lost when user starts typing
			// next to a non-standard link
			DetectUrls = false;
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
        string defaultRTFHeader;

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


            string defaultFontTable = defaultRTFHeader.Substring(startIndex, indexOn - startIndex);

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


            indexOn = 0;

            do
            {
                // remove all other font sizes other than "\fs<N>" other than the default "fsM"

                // number of slashes preceding "\fs<N>"
                int numSlashesPreceding = 0;

                // find a "\fs" element
                int slashIndex = text.IndexOf("\\fs", indexOn);
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

		void SetSelectionStyle(UInt32 mask, UInt32 effect)
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

		int GetSelectionStyle(UInt32 mask, UInt32 effect)
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
			    state = (cf.dwEffects & effect) == effect ? 1 : 0;
			}
			else
			{
				state = -1;
			}
			
			Marshal.FreeCoTaskMem(lpar);
			return state;
		}
	}
}
