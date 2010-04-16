using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using wyDay.Controls;
using wyUpdate.Common;

namespace wyUpdate
{
    // The frame type to draw in the paint events.
    public enum FrameType
    {
        WelcomeFinish = 0, License = 1, TextInfo = 2, Update = 3
    }

    public enum UpdateItemStatus
    {
        Error = -1, Nothing = 0, Working = 1, Success = 2
    }

    internal class UpdateItem
    {
        static readonly Image ErrorImage = new Bitmap(typeof(UpdateItem), "cross.png");
        static readonly Image SuccessImage = new Bitmap(typeof(UpdateItem), "tick.png");
        static readonly Image ProgressImage = new Bitmap(typeof(UpdateItem), "process-working.png");

        Label m_Label = new Label { AutoSize = true };

        public Label Label
        {
            get { return m_Label; }
            set { m_Label = value; }
        }
        UpdateItemStatus m_Status;
        AnimationControl m_Animation = new AnimationControl(null, 1);

        //Position
        int m_Left, m_Top;

        //Animation width

        public int AnimationWidth { get; set; }

        public AnimationControl Animation
        {
            get { return m_Animation; }
            set { m_Animation = value; }
        }

        public int Left
        {
            get { return m_Left; }
            set
            {
                m_Left = value;
                Animation.Left = m_Left;
                Label.Left = m_Left + AnimationWidth + 5;
            }
        }

        public int Top
        {
            get { return m_Top; }
            set
            {
                m_Top = value;
                Animation.Top = m_Top;
                Label.Top = m_Top;
            }
        }

        public string Text
        {
            get
            {
                return m_Label.Text;
            }
            set
            {
                m_Label.Text = value;
            }
        }

        public bool Visible
        {
            get
            {
                return m_Animation.Visible;
            }
            set
            {
                m_Animation.Visible = value;
                m_Label.Visible = value;
            }
        }

        public void Show()
        {
            Visible = true;
        }

        public void Hide()
        {
            Visible = false;
        }

        public void Clear()
        {
            m_Status = UpdateItemStatus.Nothing;
            m_Label.Text = "";
        }

        public UpdateItemStatus Status
        {
            get { return m_Status; }
            set
            {
                if (m_Status != value)//only set m_Status if it's a different status
                {
                    m_Status = value;

                    switch (m_Status)
                    {
                        case UpdateItemStatus.Error:
                            m_Animation.StopAnimation();
                            m_Animation.StaticImage = true;
                            m_Animation.Rows = 4;
                            m_Animation.Columns = 8;
                            m_Animation.AnimationInterval = 25;
                            m_Animation.BaseImage = ErrorImage;
                            m_Animation.StartAnimation();
                            m_Label.Font = new Font(Label.Font, FontStyle.Regular);
                            break;
                        case UpdateItemStatus.Nothing:
                            m_Animation.BaseImage = null;
                            m_Animation.StartAnimation();
                            break;
                        case UpdateItemStatus.Working:
                            m_Animation.StopAnimation();
                            m_Animation.StaticImage = false;
                            m_Animation.Rows = 5;
                            m_Animation.Columns = 10;
                            m_Animation.AnimationInterval = 25;
                            m_Animation.BaseImage = ProgressImage;
                            Animation.StartAnimation();
                            m_Label.Font = new Font(Label.Font, FontStyle.Bold);
                            break;
                        case UpdateItemStatus.Success:
                            m_Animation.StopAnimation();
                            m_Animation.StaticImage = true;
                            m_Animation.Rows = 4;
                            m_Animation.Columns = 8;
                            m_Animation.AnimationInterval = 25;
                            m_Animation.BaseImage = SuccessImage;
                            m_Animation.StartAnimation();
                            m_Label.Font = new Font(Label.Font, FontStyle.Regular);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }

    internal class PanelDisplay : ContainerControl
    {
        #region Private Variables

        //Images
        Image m_SideImage;
        Image m_TopImage;

        ImageAlign m_HeaderImageAlign = ImageAlign.Left;
        int m_HeaderIndent = 14;
        Color m_HeaderTextColor = Color.Black;

        bool m_HideHeaderDivider;

        //Text
        string m_Title;
        Font m_TitleFont;
        Rectangle m_TitleRect;

        string m_Description;
        Rectangle m_DescriptionRect;

        string m_Body;
        Rectangle m_BodyRect;

        string m_BottomText;
        Rectangle m_BottomRect;

        //message (and/or license)
        readonly RichTextBoxEx messageBox = new RichTextBoxEx();

        //downloading and installing
        readonly Windows7ProgressBar progressBar = new Windows7ProgressBar();
        int m_Progress;

        string m_ProgressStatus;
        Rectangle m_ProgressStatusRect;

        //padding for the text
        int m_LeftPad = 14;
        int m_RightPad = 14;
        int m_TopPad = 14;

        //offset for the Top description
        int m_DescriptionOffset = 10;

        //the total HeaderHeight (including 3d line)
        const int m_HeaderHeight = 59;

        FrameType m_TypeofFrame;

        //"working" animation
        AnimationControl aniWorking;

        LinkLabel2 noUpdateAvailableLink;
        string noUpdateAvailableURL;

        Button errorDetailsButton;

        #endregion Private Variables

        //Update Items
        bool m_ShowChecklist;
        public UpdateItem[] UpdateItems = new UpdateItem[4];

        #region Properties

        /// <summary>
        /// The progress of the install or download.
        /// </summary>
        public int Progress
        {
            get { return m_Progress; }
            set
            {
                m_Progress = value;
                if (progressBar != null)
                {
                    if (m_Progress > 100)
                        m_Progress = 100;
                    else if (m_Progress < 0)
                        m_Progress = 0;

                    progressBar.Value = m_Progress;
                }
            }
        }

        public void AppendText(string plaintext)
        {
            messageBox.Select(messageBox.Text.Length, 0);
            messageBox.SelectedText = plaintext;
        }

        public void AppendRichText(string rtfText)
        {
            messageBox.Select(messageBox.Text.Length, 0);
            messageBox.SelectedRtf = messageBox.SterilizeRTF(rtfText);
        }

        public void AppendAndBoldText(string plaintext)
        {
            // set the cursor to the end of the file
            messageBox.Select(messageBox.Text.Length, 0);
            
            // store the current font, and change to bold
            Font prevSelectionFont = messageBox.SelectionFont;
            messageBox.SelectionFont = new Font(messageBox.SelectionFont, FontStyle.Bold);

            // append the text
            messageBox.SelectedText = plaintext;

            //revert the selection font back to the old one
            messageBox.SelectionFont = prevSelectionFont;
        }

        public string GetChangesRTF()
        {
            return messageBox.Rtf;
        }

        /// <summary>
        /// The status shown right below the progress bar.
        /// </summary>
        public string ProgressStatus
        {
            get { return m_ProgressStatus; }
            set
            {
                m_ProgressStatus = value;
                if (progressBar != null && progressBar.Visible)
                {
                    m_ProgressStatusRect = UpdateTextSize(m_ProgressStatus, 
                        new Padding(m_LeftPad, progressBar.Bottom + 4, m_RightPad, 0), 
                        TextFormatFlags.SingleLine | TextFormatFlags.WordEllipsis, Font);

                    //Invalidate the appropriate region
                    Invalidate(new Rectangle(m_LeftPad, progressBar.Bottom, Width - m_LeftPad - m_RightPad, Height - progressBar.Bottom));
                }
            }
        }

        /// <summary>
        /// The number of pixels the description is offset from the title in the 'x' direction.
        /// </summary>
        public int DescriptionOffset
        {
            get { return m_DescriptionOffset; }
            set { m_DescriptionOffset = value; }
        }

        /// <summary>
        /// The type of frame.
        /// </summary>
        public FrameType TypeofFrame
        {
            get { return m_TypeofFrame; }
            set { m_TypeofFrame = value; }
        }

        /// <summary>
        /// The space between the left side of the panel and the text.
        /// </summary>
        public int LeftPad
        {
            get { return m_LeftPad; }
            set { m_LeftPad = value; }
        }

        /// <summary>
        /// The space between the right side of the panel and the text.
        /// </summary>
        public int RightPad
        {
            get { return m_RightPad; }
            set { m_RightPad = value; }
        }

        /// <summary>
        /// The space between the top of the panel and the text.
        /// </summary>
        public int TopPad
        {
            get { return m_TopPad; }
            set { m_TopPad = value; }
        }

        public string Title
        {
            get { return m_Title; }
            set
            {
                m_Title = value;
                Invalidate();
            }
        }

        public string Description
        {
            get { return m_Description; }
            set
            {
                m_Description = value;
                Invalidate();
            }
        }

        public string Body
        {
            get { return m_Body; }
            set
            {
                m_Body = value;
                Invalidate();
            }
        }

        public string BottomText
        {
            get { return m_BottomText; }
            set
            {
                m_BottomText = value;
                Invalidate();
            }
        }

        public Image SideImage
        {
            get { return m_SideImage; }
            set { m_SideImage = value; }
        }

        public Image TopImage
        {
            get { return m_TopImage; }
            set { m_TopImage = value; }
        }

        public ImageAlign HeaderImageAlign
        {
            get { return m_HeaderImageAlign; }
            set { m_HeaderImageAlign = value; }
        }

        public int HeaderIndent
        {
            get { return m_HeaderIndent; }
            set { m_HeaderIndent = value; }
        }

        public Color HeaderTextColor
        {
            get { return m_HeaderTextColor; }
            set { m_HeaderTextColor = value; }
        }

        public bool HideHeaderDivider
        {
            get { return m_HideHeaderDivider; }
            set { m_HideHeaderDivider = value; }
        }

        public bool ShowChecklist
        {
            get { return m_ShowChecklist; }
            set { m_ShowChecklist = value; }
        }

        public string ErrorDetails { get; set; }

        #endregion

        #region Constructors

        public PanelDisplay(int width, int height)
        {
            Width = width;
            Height = height;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Opaque, true);

            //setup the progressbar
            progressBar.Location = new Point(m_LeftPad, Height - 60);
            progressBar.Size = new Size(Width - m_LeftPad - m_RightPad, 20);
            progressBar.Maximum = 100;
            progressBar.Value = 0;
            progressBar.Visible = false;
            Controls.Add(progressBar);

            //setup the messageBox
            messageBox.Location = new Point(m_LeftPad, m_HeaderHeight + m_TopPad + 20);
            messageBox.Multiline = true;
            messageBox.Size = new Size(Width - m_LeftPad - m_RightPad, 0);
            messageBox.ReadOnly = true;
            messageBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            messageBox.BackColor = SystemColors.Window; //white
            messageBox.Visible = false;
            messageBox.LinkClicked += messageBox_LinkClicked;
            Controls.Add(messageBox);

            //setup the animation list
            for (int i = 0; i < UpdateItems.Length; i++)
            {
                UpdateItems[i] = new UpdateItem
                                     {
                                         AnimationWidth = 16, 
                                         Visible = false, 
                                         Left = 45
                                     };
                Controls.Add(UpdateItems[i].Animation);
                Controls.Add(UpdateItems[i].Label);
            }

            //the single centered animation
            aniWorking = new AnimationControl(new Bitmap(typeof(PanelDisplay), "process-working.png"), 10, 5, 25)
                             {
                                 Visible = false,
                                 Location = new Point((Width/2) - 25, (Height/2))
                             };

            Controls.Add(aniWorking);
        }

        #endregion Constructors

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int SendMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        public void ChangePanel(FrameType panType, string title, string description, string body, string bottom)
        {
            //WM_SETREDRAW = 0xB;
            SendMessage(Handle, 0xB, 0, 0); //disable drawing

            m_TypeofFrame = panType;

            m_Title = title;
            m_Description = description;
            m_Body = body;
            m_BottomText = bottom;

            if (panType == FrameType.WelcomeFinish)
            {
                if (ErrorDetails == null)
                    messageBox.Hide();
                else
                {
                    messageBox.Clear();
                    AppendText(ErrorDetails);
                }

                progressBar.Hide();
                HideAnimations();

                m_TitleFont = new Font(Font.FontFamily, 12, FontStyle.Bold);
            }
            else
            {
                m_TitleFont = new Font(Font.FontFamily, Font.Size + 1, FontStyle.Bold);
            }

            //calculate the text sizes and positions
            UpdateTextRectangles();

            progressBar.ShowInTaskbar = false;

            //handle specifics
            switch (panType)
            {
                case FrameType.License:
                //TODO: show checkbox (I agree) box
                //TODO: add an event handler for checked state
                //fall through to add the messageBox
                case FrameType.TextInfo:
                    messageBox.Show();
                    progressBar.Hide();
                    HideAnimations();
                    break;
                case FrameType.Update:
                    messageBox.Hide();
                    progressBar.Show();

                    progressBar.ContainerControl = (Form)TopLevelControl;
                    progressBar.ShowInTaskbar = m_ShowChecklist;

                    if (m_ShowChecklist)
                    {
                        //set the defaults for the UpdateItems
                        for (int i = 0; i < UpdateItems.Length; i++)
                        {
                            //clear any previous state, and show
                            UpdateItems[i].Clear();
                            UpdateItems[i].Show();
                        }
                    }
                    else
                    {
                        //show a single centered animation
                        aniWorking.StartAnimation();
                        aniWorking.Show();
                    }
                    break;
            }

            //re-enable drawing and Refresh
            SendMessage(Handle, 0xB, 1, 0);
            Refresh();
        }

        public void SetNoUpdateAvailableLink(string text, string link)
        {
            noUpdateAvailableLink = new LinkLabel2
                                        {
                                            Text = text, Visible = false,
                                            BackColor = Color.White
                                        };
            noUpdateAvailableLink.Click += NoUpdateAvailableLink_Click;
            noUpdateAvailableURL = link;

            Controls.Add(noUpdateAvailableLink);
        }

        void NoUpdateAvailableLink_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(noUpdateAvailableURL);
        }

        public void SetUpErrorDetails(string detailsButton)
        {
            errorDetailsButton = new Button
                                     {
                                         Text = detailsButton,
                                         FlatStyle = FlatStyle.System,
                                         Visible = false,
                                         AutoSize = true,
                                         Padding = new Padding(6, 0, 6, 0)
                                     };

            errorDetailsButton.Click += errorDetailsButton_Click;

            Controls.Add(errorDetailsButton);
        }

        void errorDetailsButton_Click(object sender, EventArgs e)
        {
            errorDetailsButton.Visible = false;
            messageBox.Show();
        }

        static void messageBox_LinkClicked(object sender, string link)
        {
            System.Diagnostics.Process.Start(link);
        }

        void UpdateTextRectangles()
        {
            int lPad = m_LeftPad;
            int lHeaderPad = m_HeaderIndent, rHeaderPad = m_RightPad;

            //calculate left and right padding
            if (m_TypeofFrame == FrameType.WelcomeFinish && m_SideImage != null)
                lPad += m_SideImage.Width;
            else if (m_TypeofFrame != FrameType.WelcomeFinish)
            {
                //calculate header padding
                if (m_TopImage != null)
                {
                    switch (m_HeaderImageAlign)
                    {
                        case ImageAlign.Left:
                            lHeaderPad += m_TopImage.Width;
                            break;
                        case ImageAlign.Right:
                            rHeaderPad += m_TopImage.Width;
                            break;
                        case ImageAlign.Fill:
                            break;
                    }
                }
            }

            if (m_TypeofFrame == FrameType.WelcomeFinish)
            {
                //calculate title rectangle
                m_TitleRect = UpdateTextSize(m_Title,
                    new Padding(lPad, m_TopPad, m_RightPad, 0),
                    TextFormatFlags.WordBreak, m_TitleFont);

                //calculate description rectangle
                m_DescriptionRect = UpdateTextSize(m_Description,
                    new Padding(lPad, m_TitleRect.Bottom + m_TopPad, m_RightPad, 0),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoClipping, Font);
            }
            else //header Title & description
            {
                //calculate title rectangle
                m_TitleRect = UpdateTextSize(m_Title,
                    new Padding(lHeaderPad, m_TopPad, rHeaderPad, 0),
                    TextFormatFlags.WordEllipsis, m_TitleFont);

                //calculate description rectangle
                m_DescriptionRect = UpdateTextSize(m_Description,
                    new Padding(lHeaderPad + m_DescriptionOffset, m_TitleRect.Bottom, rHeaderPad, 0),
                    TextFormatFlags.WordEllipsis, Font);

                //Vertically center the header text
                m_TitleRect.Location = new Point(lHeaderPad, (m_HeaderHeight / 2) - ((m_TitleRect.Height + m_DescriptionRect.Height) / 2));
                m_DescriptionRect.Location = new Point(lHeaderPad + m_DescriptionOffset, m_TitleRect.Bottom);
            }

            
            //calculate bottom rectangle
            m_BottomRect = UpdateTextSize(m_BottomText,
                new Padding(lPad, 0, m_RightPad, 9),
                TextFormatFlags.WordBreak, Font, ContentAlignment.BottomRight);
            
            if (m_TypeofFrame != FrameType.WelcomeFinish)
            {
                //calculate body rectangle
                m_BodyRect = UpdateTextSize(m_Body,
                    new Padding(lPad, m_HeaderHeight + TopPad, m_RightPad, 0), 
                    TextFormatFlags.WordBreak, Font);

                //Resize the messageBox
                if (m_TypeofFrame == FrameType.TextInfo)
                {
                    messageBox.Top = m_BodyRect.Bottom + 5;
                    messageBox.Height = Height - messageBox.Top - 5 - (Bottom - m_BottomRect.Top);
                }

                //Reposition the m_UpdateItems
                if (m_ShowChecklist)
                {
                    for (int i = 0; i < UpdateItems.Length; i++)
                    {
                        UpdateItems[i].Top = m_BodyRect.Bottom + 25 + (30 * i);
                    }
                }
            }
            else if (noUpdateAvailableLink != null) // AND m_TypeOfFrame == WelcomeFinish
            {
                noUpdateAvailableLink.Location = new Point(m_DescriptionRect.Left, m_DescriptionRect.Bottom + 20);
                noUpdateAvailableLink.Visible = true;
            }
            else if (ErrorDetails != null) // m_TypeofFrame == FrameType.WelcomeFinish
            {
                errorDetailsButton.Location = new Point(Width - m_RightPad - errorDetailsButton.Width, m_DescriptionRect.Bottom + 5);
                errorDetailsButton.Visible = true;

                //Resize the messageBox
                messageBox.Location = new Point(lPad, m_DescriptionRect.Bottom + 5);
                messageBox.Size = new Size(Width - lPad - m_RightPad,
                           Height - messageBox.Top - 5 - (Bottom - m_BottomRect.Top));
            }
        }

        Rectangle UpdateTextSize(string text, Padding padding, TextFormatFlags flags, Font font)
        {
            return UpdateTextSize(text, padding, flags, font, ContentAlignment.TopLeft);
        }

        Rectangle UpdateTextSize(string text, Padding padding, TextFormatFlags flags, Font font, ContentAlignment alignment)
        {
            if (font == null)
                font = Font;

            Size tabTextSize = TextRenderer.MeasureText(text,
                font,
                new Size(Width - padding.Left - padding.Right, 1),
                flags | TextFormatFlags.NoPrefix);

            if (alignment == ContentAlignment.BottomRight)
                return new Rectangle(new Point(Width - tabTextSize.Width - padding.Right, Height - tabTextSize.Height - padding.Bottom), tabTextSize);
            
            return new Rectangle(new Point(padding.Left, padding.Top), tabTextSize);
        }

        void HideAnimations()
        {
            //set the defaults for the UpdateItems
            for (int i = 0; i < UpdateItems.Length; i++)
            {
                UpdateItems[i].Hide();
            }
            aniWorking.Hide();
            aniWorking.StopAnimation();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
            e.Graphics.InterpolationMode = InterpolationMode.Low;

            if (m_TypeofFrame == FrameType.WelcomeFinish)
            {
                //background 
                e.Graphics.FillRectangle(SystemBrushes.Control, 0, 316, Width, Height - 316);
                //Side Image, and white background
                DrawSide(e.Graphics);
            }
            else
            {
                //background 
                e.Graphics.FillRectangle(SystemBrushes.Control, 0, m_HideHeaderDivider ? 57 : 59, Width, Height - (m_HideHeaderDivider ? 57 : 59));
                //Top image, and white background
                DrawTop(e.Graphics);
            }

            DrawMain(e.Graphics);
        }

        void DrawSide(Graphics gr)
        {
            try
            {
                Rectangle imageLocation = new Rectangle(0, 0, m_SideImage.Width, m_SideImage.Height);
                gr.DrawImage(m_SideImage, imageLocation);
                gr.ExcludeClip(imageLocation);
            }
            catch { }

            //Draw the white background
            gr.FillRectangle(Brushes.White, 0, 0, Width, 314);
        }

        void DrawTop(Graphics gr)
        {
            //draw the topImage
            try
            {
                Rectangle imageLocation = m_HeaderImageAlign == ImageAlign.Right 
                            ? new Rectangle(Width - m_TopImage.Width, 0, m_TopImage.Width, m_TopImage.Height) 
                            : new Rectangle(0, 0, m_TopImage.Width, m_TopImage.Height);

                gr.DrawImage(m_TopImage, imageLocation);
                gr.ExcludeClip(imageLocation);
            }
            catch { }

            //Draw white background
            gr.FillRectangle(Brushes.White, 0, 0, Width, m_HeaderHeight - 2);

            gr.ResetClip();

            //draw m_Title 
            TextRenderer.DrawText(gr, m_Title, m_TitleFont, m_TitleRect, m_HeaderTextColor, TextFormatFlags.WordEllipsis | TextFormatFlags.NoPrefix);

            //draw m_Description
            TextRenderer.DrawText(gr, m_Description, Font, m_DescriptionRect, m_HeaderTextColor, TextFormatFlags.WordEllipsis | TextFormatFlags.NoPrefix);

            //draw divider line
            if (!m_HideHeaderDivider)
                Draw3DLine(gr, 0, Width, m_HeaderHeight - 2);
        }

        void DrawMain(Graphics gr)
        {
            if (m_TypeofFrame == FrameType.WelcomeFinish)
            {
                //Draw m_Title and m_Description
                TextRenderer.DrawText(gr, m_Title, m_TitleFont, m_TitleRect, ForeColor, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

                //Draw m_Description
                TextRenderer.DrawText(gr, m_Description, Font, m_DescriptionRect, ForeColor, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            }
            else
            {
                //Draw m_Body
                TextRenderer.DrawText(gr, m_Body, Font, m_BodyRect, ForeColor, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            }

            //Draw m_BottomText
            if (!string.IsNullOrEmpty(m_BottomText))
                TextRenderer.DrawText(gr, m_BottomText, Font, m_BottomRect, ForeColor, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);


            //Draw m_ProgressStatus
            if (!string.IsNullOrEmpty(m_ProgressStatus) && progressBar.Visible)
                TextRenderer.DrawText(gr, m_ProgressStatus, Font,
                    m_ProgressStatusRect, ForeColor, 
                    TextFormatFlags.SingleLine | TextFormatFlags.WordEllipsis | TextFormatFlags.NoPrefix);


            // draw bottom divider & branding
            int brandingWidth = 0;
            if (m_TypeofFrame != FrameType.WelcomeFinish)
                brandingWidth = DrawBranding(gr, 3, 314);

            Draw3DLine(gr, brandingWidth, Width, 314);
        }

        protected static void Draw3DLine(Graphics gr, int x1, int x2, int y1)
        {
            gr.DrawLine(SystemPens.ControlDark, x1, y1, x2, y1);
            gr.DrawLine(SystemPens.ControlLightLight, x1, y1 + 1, x2, y1 + 1);
        }

        int DrawBranding(Graphics gr, int x, int midPointY)
        {
            SizeF textSize = gr.MeasureString("wyUpdate", Font);
            midPointY -= (int)(textSize.Height / 2.0);

            //draw the text
            gr.DrawString("wyUpdate", Font, SystemBrushes.ControlLightLight, new PointF(x, midPointY));
            gr.DrawString("wyUpdate", Font, SystemBrushes.ControlDark, new PointF(x - 1, midPointY - 1));

            return (int)textSize.Width + x;
        }
    }
}
