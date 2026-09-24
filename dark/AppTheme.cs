using System.Drawing;
using System.Drawing.Drawing2D;

namespace MusicIdTrainer;

internal static class AppTheme
{
    public static readonly Color Background = Color.FromArgb(12, 8, 11);
    public static readonly Color Panel = Color.FromArgb(24, 13, 18);
    public static readonly Color Input = Color.FromArgb(15, 10, 14);
    public static readonly Color Button = Color.FromArgb(61, 28, 39);
    public static readonly Color Border = Color.FromArgb(115, 57, 70);
    public static readonly Color Text = Color.FromArgb(247, 233, 237);
    public static readonly Color Muted = Color.FromArgb(211, 181, 189);
    public static readonly Color Accent = Color.FromArgb(144, 55, 74);

    public static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = Accent;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(173, 77, 97);
        button.UseVisualStyleBackColor = false;
        button.BackColor = Button;
        button.ForeColor = Text;
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(7, 3, 7, 3);
        button.MinimumSize = new Size(0, 33);
    }

    public static void StyleControls(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            control.ForeColor = Text;
            switch (control)
            {
                case Button button:
                    StyleButton(button);
                    break;
                case GroupBox box:
                    box.BackColor = Panel;
                    box.ForeColor = Muted;
                    break;
                case ListBox or TextBox or NumericUpDown or ComboBox:
                    control.BackColor = Input;
                    break;
                case PictureBox:
                    control.BackColor = Background;
                    break;
                case TableLayoutPanel or FlowLayoutPanel or Label:
                    control.BackColor = Color.Transparent;
                    break;
                case SplitContainer:
                    control.BackColor = Background;
                    break;
                case Panel panel when panel is not GradientSurface:
                    panel.BackColor = Background;
                    break;
            }
            StyleControls(control);
        }
    }
}

internal sealed class GradientSurface : Panel
{
    public GradientSurface() => DoubleBuffered = true;

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (ClientSize.Width == 0 || ClientSize.Height == 0) return;
        using var brush = new LinearGradientBrush(ClientRectangle,
            Color.FromArgb(43, 18, 27), AppTheme.Background, LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }
}

internal sealed class FramedGroupBox : GroupBox
{
    public FramedGroupBox() => DoubleBuffered = true;

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        using var pen = new Pen(AppTheme.Border);
        var textSize = TextRenderer.MeasureText(Text, Font);
        var top = Math.Max(1, textSize.Height / 2);
        e.Graphics.DrawLine(pen, 0, top, 9, top);
        e.Graphics.DrawLine(pen, textSize.Width + 18, top, Width - 1, top);
        e.Graphics.DrawLine(pen, 0, top, 0, Height - 1);
        e.Graphics.DrawLine(pen, Width - 1, top, Width - 1, Height - 1);
        e.Graphics.DrawLine(pen, 0, Height - 1, Width - 1, Height - 1);
        TextRenderer.DrawText(e.Graphics, Text, Font, new Point(11, 0), ForeColor, BackColor);
    }
}
