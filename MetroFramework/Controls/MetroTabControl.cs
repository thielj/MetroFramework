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

// Most likely origins / copyright owner
// (c) Mick Doherty
// http://dotnetrix.co.uk/tabcontrol.htm
// http://www.pcreview.co.uk/forums/adding-custom-tabpages-design-time-t2904262.html
// http://www.codeproject.com/Articles/12185/A-NET-Flat-TabControl-CustomDraw
// http://www.codeproject.com/Articles/278/Fully-owner-drawn-tab-control

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Design;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;

using MetroFramework.Drawing;
using MetroFramework.Native;

namespace MetroFramework.Controls
{

    #region MetroTabPageCollection

    [ToolboxItem(false)]
    [Editor("MetroFramework.Design.MetroTabPageCollectionEditor, " + AssemblyRef.MetroFrameworkDesignSN, typeof(UITypeEditor))]
    public class MetroTabPageCollection : TabControl.TabPageCollection
    {
        public MetroTabPageCollection(MetroTabControl owner)
            : base(owner)
        { }
    }

    #endregion

    [Designer("MetroFramework.Design.MetroTabControlDesigner, " + AssemblyRef.MetroFrameworkDesignSN)]
    [ToolboxBitmap(typeof(TabControl))]
    public partial class MetroTabControl : MetroTabControlBase
    {

        #region Fields

        private SubClass scUpDown = null;
        private bool bUpDown = false;

        private const int TAB_BOTTOM_BORDER_HEIGHT = 3;

        private ContentAlignment textAlign = ContentAlignment.MiddleLeft;
        [DefaultValue(ContentAlignment.MiddleLeft)]
        [Category(MetroDefaults.CatAppearance)]
        public ContentAlignment TextAlign
        {
            get { return textAlign; }
            set { textAlign = value; }
        }

        [Editor("MetroFramework.Design.MetroTabPageCollectionEditor, " + AssemblyRef.MetroFrameworkDesignSN, typeof(UITypeEditor))]
        public new TabPageCollection TabPages
        {
            get { return base.TabPages; }
        }


        private bool isMirrored;
        [DefaultValue(false)]
        [Category(MetroDefaults.CatAppearance)]
        public new bool IsMirrored
        {
            get { return isMirrored; }
            set
            {
                if (isMirrored == value)
                {
                    return;
                }
                isMirrored = value;
                UpdateStyles();
            }
        }

        #endregion

        public MetroTabControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            // ControlStyles.AllPaintingInWmPaint |
            // ControlStyles.ResizeRedraw |
            UseStyleColor();
            UseFontStyle();
            //Padding = new Point(6, 8); 
        }

        protected override Padding DefaultPadding
        {
            get { return new Padding(6, 8, 6, 8); }
        }

        #region Paint Methods

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            try
            {
                if (TabPages == null || TabPages.Count == 0 || !(TabPages[SelectedIndex] is MetroTabPage))
                {
                    e.Graphics.Clear(EffectiveBackColor);
                    return;
                }
                var tb = (MetroTabPage)TabPages[SelectedIndex];
                if (tb.EffectiveBackColor.A < 255 && tb.BackgroundImage == null)
                {
                    e.Graphics.Clear(EffectiveBackColor);
                    return;
                }
                using (Brush bgBrush = new SolidBrush(EffectiveBackColor))
                {
                    Region r = new Region(ClientRectangle);
                    r.Exclude(DisplayRectangle);
                    e.Graphics.FillRegion(bgBrush, r);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                Invalidate();
            }
        }

        protected override void OnPaintForeground(PaintEventArgs e)
        {
            for (var index = 0; index < TabPages.Count; index++)
            {
                if (index == SelectedIndex) continue;
                DrawTab(index, e.Graphics);
            }
            if (SelectedIndex <= -1) return;

            DrawTabBottomBorder(SelectedIndex, e.Graphics);
            DrawTab(SelectedIndex, e.Graphics);
            DrawTabSelected(SelectedIndex, e.Graphics);
        }

        private void DrawTabBottomBorder(int index, Graphics graphics)
        {
            using (Brush bgBrush = new SolidBrush(GetThemeColor("BorderColor")))
            {
                Rectangle borderRectangle = new Rectangle(DisplayRectangle.X, GetTabRect(index).Bottom + 2 - TAB_BOTTOM_BORDER_HEIGHT,
                    DisplayRectangle.Width, TAB_BOTTOM_BORDER_HEIGHT);

                graphics.FillRectangle(bgBrush, borderRectangle);
            }
        }

        private void DrawTabSelected(int index, Graphics graphics)
        {
            using (Brush selectionBrush = new SolidBrush(GetStyleColor()))
            {
                Rectangle selectedTabRect = GetTabRect(index);
                //Size textAreaRect = MeasureText(TabPages[index].Text);
                Rectangle borderRectangle = new Rectangle(
                    selectedTabRect.X + ((index == 0) ? 2 : 0),
                    selectedTabRect.Bottom + 2 - TAB_BOTTOM_BORDER_HEIGHT,
                    selectedTabRect.Width + ((index == 0) ? 0 : 2),
                    TAB_BOTTOM_BORDER_HEIGHT);
                graphics.FillRectangle(selectionBrush, borderRectangle);
            }
        }

        //private Size MeasureText(string text)
        //{
        //    Size preferredSize;
        //    using (Graphics g = CreateGraphics())
        //    {
        //        Size proposedSize = new Size(int.MaxValue, int.MaxValue);
        //        preferredSize = TextRenderer.MeasureText(g, text, MetroFonts.TabControl(metroLabelSize, metroLabelWeight), proposedSize,
        //                                                 TextAlign.AsTextFormatFlags() | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        //    }
        //    return preferredSize;
        //}

        private void DrawTab(int index, Graphics graphics)
        {
            TabPage tabPage = TabPages[index];
            Rectangle tabRect = GetTabRect(index);

            if (index == 0)
            {
                tabRect.X = DisplayRectangle.X;
            }

            //Rectangle bgRect = tabRect;

            tabRect.Width += 20; // TODO: Why???

            //using (Brush bgBrush = new SolidBrush(backColor))
            //{
            //    graphics.FillRectangle(bgBrush, bgRect);
            //}

            TextRenderer.DrawText(graphics, tabPage.Text, EffectiveFont, tabRect, EffectiveForeColor, EffectiveBackColor, TextAlign.AsTextFormatFlags() | TextFormatFlags.EndEllipsis);
        }

        [SecuritySafeCritical]
        private void DrawUpDown(Graphics graphics)
        {
            RECT borderRect = new RECT();
            WinApi.GetClientRect(scUpDown.Handle, ref borderRect);

            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            graphics.Clear(EffectiveBackColor);

            using (Brush b = new SolidBrush(GetThemeColor("BorderColor")))
            using (GraphicsPath gp = new GraphicsPath(FillMode.Winding))
            {
                PointF[] pts = { new PointF(6, 6), new PointF(16, 0), new PointF(16, 12) };
                gp.AddLines(pts);
                graphics.FillPath(b, gp);

                gp.Reset();
                PointF[] pts2 = { new PointF(borderRect.Width - 15, 0), new PointF(borderRect.Width - 5, 6), new PointF(borderRect.Width - 15, 12) };
                gp.AddLines(pts2);
                graphics.FillPath(b, gp);
            }
        }

        #endregion

        #region Overridden Methods

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        // TODO: ???
        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            Invalidate();
        }

        // TODO: ???
        [SecuritySafeCritical]
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (!DesignMode)
            {
                // TODO: We must make sure we don't call this while receiving a scrollbar message !!!
                WinApi.ShowScrollBar(Handle, WinApi.ScrollBar.Both, 0);
            }
        }

        protected override CreateParams CreateParams
        {
            // TODO: Why the below????
            [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                const int WS_EX_LAYOUTRTL = 0x400000;
                const int WS_EX_NOINHERITLAYOUT = 0x100000;
                var cp = base.CreateParams;
                if (isMirrored)
                {
                    cp.ExStyle = cp.ExStyle | WS_EX_LAYOUTRTL | WS_EX_NOINHERITLAYOUT;
                }
                return cp;
            }
        }

        private new Rectangle GetTabRect(int index)
        {
            return index < 0 ? new Rectangle() : base.GetTabRect(index);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (SelectedIndex != -1)
            {
                if (!TabPages[SelectedIndex].Focused)
                {
                    bool subControlFocused = false;
                    foreach (Control ctrl in TabPages[SelectedIndex].Controls)
                    {
                        if (ctrl.Focused)
                        {
                            subControlFocused = true;
                            return;
                        }
                    }

                    if (!subControlFocused)
                    {
                        TabPages[SelectedIndex].Select();
                        TabPages[SelectedIndex].Focus();
                    }
                }
            }

            base.OnMouseWheel(e);
        }

        #endregion





        // The below is possibly (c) Oscar Londono
        // http://www.codeproject.com/Articles/12185/A-NET-Flat-TabControl-CustomDraw

        #region

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            this.OnFontChanged(EventArgs.Empty);
            FindUpDown();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            FindUpDown();
            UpdateUpDown();
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            FindUpDown();
            UpdateUpDown();
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            UpdateUpDown();
            //Invalidate();
        }

        [SecuritySafeCritical]
        private void FindUpDown()
        {
            bool bFound = false;
            IntPtr pWnd = WinApi.GetWindow(Handle, WinApi.GW_CHILD);
            while (pWnd != IntPtr.Zero)
            {
                char[] className = new char[33];
                int length = WinApi.GetClassName(pWnd, className, 32);
                string s = new string(className, 0, length);
                if (s == "msctls_updown32")
                {
                    bFound = true;
                    if (!bUpDown)
                    {
                        scUpDown = new SubClass(pWnd, true);
                        scUpDown.SubClassedWndProc += scUpDown_SubClassedWndProc;
                        bUpDown = true;
                    }
                    break;
                }
                pWnd = WinApi.GetWindow(pWnd, WinApi.GW_HWNDNEXT);
            }

            if ((!bFound) && (bUpDown))
                bUpDown = false;
        }

        [SecuritySafeCritical]
        private void UpdateUpDown()
        {
            if (bUpDown)
            {
                if (WinApi.IsWindowVisible(scUpDown.Handle))
                {
                    RECT rect = new RECT();
                    WinApi.GetClientRect(scUpDown.Handle, ref rect);
                    WinApi.InvalidateRect(scUpDown.Handle, ref rect, true);
                }
            }
        }

        [SecuritySafeCritical]
        private int scUpDown_SubClassedWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WinApi.Messages.WM_PAINT:
                    IntPtr hDC = WinApi.GetWindowDC(scUpDown.Handle);
                    try
                    {
                        using (Graphics g = Graphics.FromHdc(hDC))
                            DrawUpDown(g);
                    }
                    finally
                    {
                        WinApi.ReleaseDC(scUpDown.Handle, hDC);
                    }
                    m.Result = IntPtr.Zero;
                    RECT rect = new RECT();
                    WinApi.GetClientRect(scUpDown.Handle, ref rect);
                    WinApi.ValidateRect(scUpDown.Handle, ref rect);
                    return 1;
            }

            return 0;
        }


        //send font change to properly resize tab page header rects
        //http://www.codeproject.com/Articles/13305/Painting-Your-Own-Tabs?msg=2707590#xx2707590xx
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_SETFONT = 0x30;
        private const int WM_FONTCHANGE = 0x1d;

        [SecuritySafeCritical]
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            IntPtr hFont = this.EffectiveFont.ToHfont();
            SendMessage(this.Handle, WM_SETFONT, hFont, (IntPtr)(-1));
            SendMessage(this.Handle, WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero);
            this.UpdateStyles();
        }

        #endregion
    }
}
