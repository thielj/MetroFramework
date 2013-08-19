#region Copyright (c) 2013 Jens Thiel, http://thielj.github.io/MetroFramework
/*
 
MetroFramework - Windows Modern UI for .NET WinForms applications

Copyright (c) 2013 Jens Thiel, http://thielj.github.io/MetroFramework

Permission is hereby granted, free of charge, to any person obtaining a copy of 
this software and associated documentation files (the "Software"), to deal in the 
Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
and to permit persons to whom the Software is furnished to do so, subject to the 
following conditions:

The above copyright notice and this permission notice shall be included in 
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 
Portions of this software are (c) 2011 Sven Walter, http://github.com/viperneo

 */
#endregion

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

using MetroFramework.Components;
using MetroFramework.Drawing;
using MetroFramework.Interfaces;
using MetroFramework.Native;

namespace MetroFramework.Controls
{
    [Designer("MetroFramework.Design.MetroProgressBarDesigner, " + AssemblyRef.MetroFrameworkDesignSN)]
    [ToolboxBitmap(typeof(ProgressBar))]
    public class MetroProgressBar : MetroProgressBarBase
    {
        // TODO: Transparency?

        #region Properties

        private ContentAlignment textAlign = ContentAlignment.MiddleRight;
        [DefaultValue(ContentAlignment.MiddleRight)]
        [Category(MetroDefaults.CatAppearance)]
        public ContentAlignment TextAlign
        {
            get { return textAlign; }
            set { textAlign = value; Invalidate(); }
        }

        private bool hideProgressText = true;
        [DefaultValue(true)]
        [Category(MetroDefaults.CatAppearance)]
        public bool HideProgressText
        {
            get { return hideProgressText; }
            set { hideProgressText = value; Invalidate(); }
        }

        private ProgressBarStyle progressBarStyle = ProgressBarStyle.Continuous;
        [DefaultValue(ProgressBarStyle.Continuous)]
        [Category(MetroDefaults.CatAppearance)]
        public ProgressBarStyle ProgressBarStyle
        {
            get { return progressBarStyle; }
            set { progressBarStyle = value; Invalidate(); }
        }

        public new int Value
        {
            get { return base.Value; }
            set { if (value > Maximum) return; base.Value = value; Invalidate(); }
        }

        [Browsable(false)]
        public double ProgressTotalPercent
        {
            get { return ((1 - (double)(Maximum - Value) / Maximum) * 100); }
        }

        [Browsable(false)]
        public double ProgressTotalValue
        {
            get { return (1 - (double)(Maximum - Value) / Maximum); }
        }

        [Browsable(false)]
        public string ProgressPercentText
        {
            get { return (string.Format("{0}%", Math.Round(ProgressTotalPercent))); }
        }

        private double ProgressBarWidth
        {
            get { return (((double)Value / Maximum) * ClientRectangle.Width); }
        }

        private int ProgressBarMarqueeWidth
        {
            get { return (ClientRectangle.Width / 3); }
        }

        #endregion

        public MetroProgressBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
        }

        #region Paint Methods

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WinApi.Messages.WM_NCCALCSIZE && (int)m.WParam != 0)
            {
                m.Result = (IntPtr)0;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            try
            {
                e.Graphics.Clear(EffectiveBackColor);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                Invalidate();
            }
        }

        protected override void OnPaintForeground(PaintEventArgs e)
        {
            if (progressBarStyle == ProgressBarStyle.Continuous)
            {
                if (!DesignMode) StopTimer();

                DrawProgressContinuous(e.Graphics);
            }
            else if (progressBarStyle == ProgressBarStyle.Blocks)
            {
                if (!DesignMode) StopTimer();

                DrawProgressContinuous(e.Graphics);
            }
            else if (progressBarStyle == ProgressBarStyle.Marquee)
            {
                if (!DesignMode && Enabled) StartTimer();
                if (!Enabled) StopTimer();

                if (Value == Maximum)
                {
                    StopTimer();
                    DrawProgressContinuous(e.Graphics);
                }
                else
                {
                    DrawProgressMarquee(e.Graphics);
                }
            }

            DrawProgressText(e.Graphics);

            using (Pen p = new Pen(GetThemeColor("BorderColor")))
            {
                Rectangle borderRect = new Rectangle(0, 0, Width - 1, Height - 1);
                e.Graphics.DrawRectangle(p, borderRect);
            }
        }

        private void DrawProgressContinuous(Graphics graphics)
        {
            graphics.FillRectangle(new SolidBrush(GetStyleColor()), 0, 0, (int)ProgressBarWidth, ClientRectangle.Height);
        }

        private int marqueeX = 0;

        private void DrawProgressMarquee(Graphics graphics)
        {
            graphics.FillRectangle(new SolidBrush(GetStyleColor()), marqueeX, 0, ProgressBarMarqueeWidth, ClientRectangle.Height);
        }

        private void DrawProgressText(Graphics graphics)
        {
            if (HideProgressText) return;
            TextRenderer.DrawText(graphics, ProgressPercentText, EffectiveFont, ClientRectangle, EffectiveForeColor, TextAlign.AsTextFormatFlags() | TextFormatFlags.EndEllipsis);
        }

        #endregion

        #region Private Methods

        private Timer marqueeTimer;
        private bool marqueeTimerEnabled
        {
            get
            {
                return marqueeTimer != null && marqueeTimer.Enabled;
            }
        }

        private void StartTimer()
        {
            if (marqueeTimerEnabled) return;

            if (marqueeTimer == null)
            {
                marqueeTimer = new Timer {Interval = 10};
                marqueeTimer.Tick += marqueeTimer_Tick;
            }

            marqueeX = -ProgressBarMarqueeWidth;
    
            marqueeTimer.Stop();
            marqueeTimer.Start();

            marqueeTimer.Enabled = true;

            Invalidate();
        }

        private void StopTimer()
        {
            if (marqueeTimer == null) return;

            marqueeTimer.Stop();

            Invalidate();
        }

        private void marqueeTimer_Tick(object sender, EventArgs e)
        {
            marqueeX++;

            if (marqueeX > ClientRectangle.Width)
            {
                marqueeX = -ProgressBarMarqueeWidth;
            }

            Invalidate();
        }

        #endregion
    }
}
