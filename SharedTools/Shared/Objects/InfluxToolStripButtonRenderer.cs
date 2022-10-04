using System.Drawing;
using System.Windows.Forms;

namespace Influx.Shared.Objects
{
    public class InfluxToolStripButtonRenderer : ToolStripProfessionalRenderer
    {
        Brush CheckedBackBrush = Brushes.Black;
        Brush HoverBackBrush = Brushes.Black;
        Pen BorderPen = Pens.Black;

        public InfluxToolStripButtonRenderer(Pen BorderColor, Brush HoverBack, Brush CheckedBack)
        {
            CheckedBackBrush = CheckedBack;
            HoverBackBrush = HoverBack;
            BorderPen = BorderColor;
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is ToolStripButton tsItem)
            {
                if (e.Item.Selected && tsItem.Enabled)
                {
                    base.OnRenderButtonBackground(e);
                    Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
                    bounds.Width -= 1;
                    bounds.Height -= 1;
                    e.Graphics.DrawRectangle(BorderPen, bounds);
                    bounds.X += 1;
                    bounds.Y += 1;
                    bounds.Width -= 1;
                    bounds.Height -= 1;
                    e.Graphics.FillRectangle(HoverBackBrush, bounds);
                }
                else if (/*btn.CheckOnClick && */ (tsItem as ToolStripButton).Checked)
                {
                    base.OnRenderButtonBackground(e);
                    Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
                    bounds.Width -= 1;
                    bounds.Height -= 1;
                    e.Graphics.DrawRectangle(BorderPen, bounds);
                    bounds.X += 1;
                    bounds.Y += 1;
                    bounds.Width -= 1;
                    bounds.Height -= 1;
                    e.Graphics.FillRectangle(CheckedBackBrush, bounds);
                }
                else
                    return;
            }
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is ToolStripDropDownButton tsItem)
            {
                if (e.Item.Selected && tsItem.Enabled)
                {
                    base.OnRenderDropDownButtonBackground(e);
                    Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
                    bounds.Width -= 1;
                    bounds.Height -= 1;
                    e.Graphics.DrawRectangle(BorderPen, bounds);
                    bounds.X += 1;
                    bounds.Y += 1;
                    bounds.Width -= 1;
                    bounds.Height -= 1;
                    e.Graphics.FillRectangle(HoverBackBrush, bounds);
                }
                else
                    return;
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is ToolStripMenuItem tsItem)
            {
                if (e.Item.Selected && tsItem.Enabled)
                {
                    base.OnRenderMenuItemBackground(e);
                    Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
                    bounds.Width -= 1;
                    bounds.Height -= 1;
                    e.Graphics.DrawRectangle(BorderPen, bounds);
                    bounds.X += 1;
                    bounds.Y += 1;
                    bounds.Width -= 1;
                    bounds.Height -= 1;
                    e.Graphics.FillRectangle(HoverBackBrush, bounds);
                }
                else
                    return;
            }

        }
    }
}
