using System.Drawing;
using System.Windows.Forms;

namespace GeniePLugin.DynamicWindows
{
    internal class TimerBarPanel : Control
    {
        public string Name { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
        public double Fraction { get; internal set; }
        public string CountText { get; internal set; }
    }
}