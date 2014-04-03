using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using MetroFramework.Components;
using MetroFramework.Drawing;
using MetroFramework.Interfaces;

namespace MetroFramework.Controls
{
    public enum LineDividerPositions : int
    {
        /// <summary>
        /// Poistion the Divider to the Right of the Text
        /// </summary>
        Right,
        /// <summary>
        /// Position the Divider the the Left of the Text
        /// </summary>
        Below
    }

    public partial class MetroLineDivider : MetroLabel
    {

        /// <summary>
        /// Divider Position
        /// </summary>
        private LineDividerPositions m_DividerPosition = LineDividerPositions.Right;

        public MetroLineDivider()
        {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            SizeF sf = GetPreferredSize(Size.Empty);
            SolidBrush m_oLineDividerColor = new SolidBrush(this.DefaultForecolor());

            if (this.DividerPosition == LineDividerPositions.Right)
            {
                Rectangle rect = new Rectangle(Convert.ToInt32(sf.Width), Convert.ToInt32((Convert.ToInt32(sf.Height) / 2) + 1), this.Width - Convert.ToInt32(sf.Width), 1);
                e.Graphics.FillRectangle(m_oLineDividerColor, rect);
            }
            else if (this.DividerPosition == LineDividerPositions.Below)
            {
                Rectangle rect = new Rectangle(1, Convert.ToInt32(sf.Height), this.Width, 1);
                e.Graphics.FillRectangle(m_oLineDividerColor, rect);
            }
                      
        }

        /// <summary>
        /// Returns the label's default color
        /// </summary>
        /// <returns></returns>
        private Color DefaultForecolor()
        {
              if (UseCustomForeColor)
            {
                return ForeColor;
            }
            else
            {
                if (!Enabled)
                {
                    if (Parent != null)
                    {
                        if (Parent is MetroTile)
                        {
                            return MetroPaint.ForeColor.Tile.Disabled(Theme);
                        }
                        else
                        {
                            return MetroPaint.ForeColor.Label.Normal(Theme);
                        }
                    }
                    else
                    {
                        return MetroPaint.ForeColor.Label.Disabled(Theme);
                    }
                }
                else
                {
                    if (Parent != null)
                    {
                        if (Parent is MetroTile)
                        {
                            return MetroPaint.ForeColor.Tile.Normal(Theme);
                        }
                        else
                        {
                            if (this.UseStyleColors)
                            {
                                return MetroPaint.GetStyleColor(Style);
                            }
                            else
                            {
                                return MetroPaint.ForeColor.Label.Normal(Theme);
                            }
                        }
                    }
                    else
                    {
                        if (UseStyleColors)
                        {
                            return MetroPaint.GetStyleColor(Style);
                        }
                        else
                        {
                            return MetroPaint.ForeColor.Label.Normal(Theme);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Sets/Gets the position of the line divider
        /// </summary>
        [Description("The location of the line divider"),
        Category("Appearance"), DefaultValue(LineDividerPositions.Right)]
        public LineDividerPositions DividerPosition
        {
            get { return this.m_DividerPosition; }
            set
            {
                this.m_DividerPosition = value;
                this.Invalidate();
            }
        }

        /// <summary>
        /// Set the Auto Size to False by default and make it not browsable
        /// </summary>
        [Browsable(false)]
        public override bool AutoSize
        {
            get
            {
                return base.AutoSize;
            }
            set
            {
                base.AutoSize = false;
            }
        }
    }
}
