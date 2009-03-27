using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace wyDay.Controls
{
    public class AnimationControl : Control
    {
        private Image m_BaseImage;
        private int m_Rows = 1;
        private int m_Columns = 1;
        private bool m_SkipFirstFrame;

        private readonly Timer aniTimer = new Timer();
        private int m_AnimationInterval = 1000;

        //used in animation
        private int columnOn = 1;
        private int rowOn = 1;

        private int frameWidth;
        private int frameHeight;

        //for static images
        private bool staticImage;

        readonly float[][] ptsArray ={ 
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 1, 0, 0, 0},
            new float[] {0, 0, 1, 0, 0},
            new float[] {0, 0, 0, 0, 0}, 
            new float[] {0, 0, 0, 0, 1}};

        readonly ImageAttributes imgAttributes = new ImageAttributes();

        #region Properties
        public int AnimationInterval
        {
            get { return m_AnimationInterval; }
            set
            {
                m_AnimationInterval = value;
                aniTimer.Interval = m_AnimationInterval;
            }
        }

        public Image BaseImage
        {
            get { return m_BaseImage; }
            set
            {
                m_BaseImage = value;
                if (m_BaseImage != null)
                {
                    if (staticImage)
                    {
                        Width = frameWidth = m_BaseImage.Width;
                        Height = frameHeight = m_BaseImage.Height;
                    }
                    else
                    {
                        Width = frameWidth = m_BaseImage.Width / m_Columns;
                        Height = frameHeight = m_BaseImage.Height / m_Rows;
                    }
                }
                else
                {
                    Width = frameWidth = 0;
                    Height = frameHeight = 0;
                }
            }
        }

        public int Columns
        {
            get { return m_Columns; }
            set { m_Columns = value; }
        }

        public int Rows
        {
            get { return m_Rows; }
            set { m_Rows = value; }
        }

        public bool StaticImage
        {
            get { return staticImage; }
            set { staticImage = value; }
        }

        public bool CurrentlyAnimating
        {
            get
            {
                return aniTimer.Enabled;
            }
        }

        public bool SkipFirstFrame
        {
            get { return m_SkipFirstFrame; }
            set { m_SkipFirstFrame = value; }
        }

        #endregion

        //Constructor
        public AnimationControl()
        {
            SetupObjects();
        }

        public AnimationControl(Image newBaseImage, int newColumns, int newRows, int newInterval)
        {
            staticImage = false;
            m_Columns = newColumns;
            m_Rows = newRows;
            BaseImage = newBaseImage;
            AnimationInterval = newInterval;

            SetupObjects();
        }

        public AnimationControl(Image newBaseImage, int newInterval)
        {
            staticImage = true;
            m_Columns = 1;
            m_Rows = 1;
            BaseImage = newBaseImage;
            AnimationInterval = newInterval;

            SetupObjects();
        }

        private void SetupObjects()
        {
            //Set Defaults
            aniTimer.Enabled = false;
            aniTimer.Tick += aniTimer_Tick;

            //This turns off internal double buffering of all custom GDI+ drawing
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.FixedHeight | ControlStyles.FixedWidth, true);
            SetStyle(ControlStyles.Selectable, false);
        }

        //methods
        void aniTimer_Tick(object sender, EventArgs e)
        {
            if (staticImage)
            {
                //if no transparency at all, stop the timer.
                if (ptsArray[3][3] >= 1f)
                {
                    StopAnimation();
                    ptsArray[3][3] = 1f;
                }
                else
                {
                    ptsArray[3][3] += .05f;
                }
            }
            else
            {
                if (columnOn == m_Columns)
                {
                    if (rowOn == m_Rows)
                    {
                        columnOn = m_SkipFirstFrame ? 2 : 1;

                        rowOn = 1;
                    }
                    else
                    {
                        columnOn = 1;
                        rowOn++;
                    }
                }
                else
                {
                    columnOn++;
                }
            }

            Refresh();
        }

        public void StartAnimation()
        {
            //if the timer isn't already running
            if (aniTimer.Enabled == false)
            {
                aniTimer.Start();
                if (staticImage)
                {
                    ptsArray[3][3] = .05f;
                }
                else
                {
                    columnOn++;
                }

                Refresh();
            }
        }

        public void StopAnimation()
        {
            aniTimer.Stop();
            columnOn = 1;
            rowOn = 1;
            Refresh();
            ptsArray[3][3] = 0; //reset to complete transparency
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
            e.Graphics.InterpolationMode = InterpolationMode.Low;

            if (m_BaseImage != null)
            {
                if (staticImage)
                {
                    imgAttributes.SetColorMatrix(new ColorMatrix(ptsArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    e.Graphics.DrawImage(m_BaseImage, new Rectangle(0, 0, frameWidth, frameHeight), 0, 0, frameWidth, frameHeight, GraphicsUnit.Pixel, imgAttributes);
                }
                else
                {
                    e.Graphics.DrawImage(m_BaseImage, new Rectangle(0, 0, frameWidth, frameHeight), new Rectangle((columnOn - 1) * frameWidth, (rowOn - 1) * frameHeight, frameWidth, frameHeight), GraphicsUnit.Pixel);
                }
            }
        }
    }

}
