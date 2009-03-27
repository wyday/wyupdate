using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.ComponentModel;

// For the latest version visit: http://wyday.com/linklabel2/

// Bugs or suggestions: http://wyday.com/forum/

namespace wyDay.Controls
{
    public class LinkLabel2 : Control
    {
        private Font hoverFont;

        Rectangle textRect;
        
        bool isHovered;
        bool keyAlreadyProcessed;

        Image image;
        int imageRightPad = 8;



        [DefaultValue(8)]
        public int ImageRightPad
        {
            get { return imageRightPad; }
            set 
            { 
                imageRightPad = value;

                RefreshTextRect();
                Invalidate();
            }
        }

        [DefaultValue(null)]
        public Image Image
        {
            get { return image; }
            set 
            { 
                image = value;

                RefreshTextRect();
                Invalidate();
            }
        }

        [DefaultValue(true)]
        public bool HoverUnderline { get; set; }

        [DefaultValue(true)]
        public bool UseSystemColor { get; set; }


        public Color RegularColor { get; set; }
        public Color HoverColor { get; set; }


        [DllImport("user32.dll")]
        public static extern int LoadCursor(int hInstance, int lpCursorName);

        [DllImport("user32.dll")]
        public static extern int SetCursor(int hCursor);

        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                base.Text = value;

                RefreshTextRect();

                Invalidate();
            }
        }

        public LinkLabel2()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.FixedHeight | ControlStyles.FixedWidth, true);
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);

            hoverFont = new Font(Font, FontStyle.Underline);

            ForeColor = SystemColors.HotTrack;

            UseSystemColor = true;
            HoverUnderline = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                Focus();

            base.OnMouseDown(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            Invalidate();

            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs mevent)
        {
            base.OnMouseMove(mevent);
            if (mevent.Button != MouseButtons.None)
            {
                if (!ClientRectangle.Contains(mevent.Location))
                {
                    if (isHovered)
                    {
                        isHovered = false;
                        Invalidate();
                    }
                }
                else if (!isHovered)
                {
                    isHovered = true;
                    Invalidate();
                }
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();

            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            keyAlreadyProcessed = false;
            Invalidate();

            base.OnLostFocus(e);
        }



        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!keyAlreadyProcessed && e.KeyCode == Keys.Enter)
            {
                keyAlreadyProcessed = true;
                OnClick(e);
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            keyAlreadyProcessed = false;

            base.OnKeyUp(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (isHovered && e.Clicks == 1 && (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle))
                OnClick(e);

            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
            e.Graphics.InterpolationMode = InterpolationMode.Low;

            // image
            if (image != null)
                e.Graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

            //text
            TextRenderer.DrawText(e.Graphics, Text,
                isHovered && HoverUnderline ? hoverFont : Font,
                textRect,
                UseSystemColor ? ForeColor : (isHovered ? HoverColor : RegularColor),
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            // draw the focus rectangle.
            if (Focused && ShowFocusCues)
                ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            hoverFont = new Font(Font, FontStyle.Underline);
            RefreshTextRect();

            base.OnFontChanged(e);
        }

        private void RefreshTextRect()
        {
            textRect = new Rectangle(Point.Empty, TextRenderer.MeasureText(Text, Font, Size, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix));
            int width = textRect.Width + 1, 
                height = textRect.Height + 1;

            if (image != null)
            {
                width = textRect.Width + 1 + image.Width + imageRightPad;

                //adjust the x position of the text
                textRect.X += image.Width + imageRightPad;

                if (image.Height > textRect.Height)
                {
                    height = image.Height + 1;

                    // adjust the y-position of the text
                    textRect.Y += (image.Height - textRect.Height) / 2;
                }
            }

            Size = new Size(width, height);
        }

        protected override void WndProc(ref Message m)
        {
            //WM_SETCURSOR == 32
            if (m.Msg == 32)
            {
                //IDC_HAND == 32649
                SetCursor(LoadCursor(0, 32649));

                //the message has been handled
                m.Result = IntPtr.Zero;
                return;
            }

            base.WndProc(ref m);
        }
    }



}
