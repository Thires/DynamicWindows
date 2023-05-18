using DynamicWindows.Properties;
using GeniePlugin.Interfaces;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DynamicWindows
{
  public class SkinnedMDIChild : Form
  {
    private const int WM_NCLBUTTONDOWN = 161;
    private const int HTBORDER = 18;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;
    private const int HTCAPTION = 2;
    private const int HTCLOSE = 20;
    private const int HTGROWBOX = 4;
    private const int HTLEFT = 10;
    private const int HTMAXBUTTON = 9;
    private const int HTMINBUTTON = 8;
    private const int HTRIGHT = 11;
    private const int HTSYSMENU = 3;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private IContainer components;
    public Panel formBody;
    private ContextMenuStrip formMenu;
    private ToolStripMenuItem closeToolStripMenuItem;
    private ToolStripMenuItem floatToolStripMenuItem;
    private ToolStripMenuItem dockLeftToolStripMenuItem;
    private ToolStripMenuItem dockRightToolStripMenuItem;
    private IHost _host;
    private Plugin _plugin;
    public Button CloseCommand;
    private Point _lastLoc;
    private Size _lastSiz;
    private DockStyle _lastDock;
    private Bitmap _topLeft;
    private Bitmap _topRight;
    private Bitmap _topMiddle;
    private Bitmap _leftSide;
    private Bitmap _rightSide;
    private Bitmap _bottomLeft;
    private Bitmap _bottomRight;
    private Bitmap _bottomMiddle;
    private Region _topLeftTrans;
    private Region _topRightTrans;
    private Font _titleFont;
    private int _topMargin;
    private int _topBorder;
    private int _leftMargin;
    private int _rightMargin;
    private int _bottomMargin;

    public int TopMargin
    {
      get
      {
        return this._topMargin;
      }
    }

    public int TopBorder
    {
      get
      {
        return this._topBorder;
      }
    }

    public int BottomMargin
    {
      get
      {
        return this._bottomMargin;
      }
    }

    public int LeftMargin
    {
      get
      {
        return this._leftMargin;
      }
    }

    public int RightMargin
    {
      get
      {
        return this._rightMargin;
      }
    }

    public SkinnedMDIChild(IHost Host, Plugin PlugIn)
    {
      this.InitializeComponent();
      this._host = Host;
      this._plugin = PlugIn;
      this.InitializeSkinning();
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.components != null)
        this.components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      this.components = (IContainer) new Container();
      this.formBody = new Panel();
      this.formMenu = new ContextMenuStrip(this.components);
      this.closeToolStripMenuItem = new ToolStripMenuItem();
      this.floatToolStripMenuItem = new ToolStripMenuItem();
      this.dockLeftToolStripMenuItem = new ToolStripMenuItem();
      this.dockRightToolStripMenuItem = new ToolStripMenuItem();
      this.formMenu.SuspendLayout();
      this.SuspendLayout();
      this.formBody.BackColor = SystemColors.ButtonFace;
      this.formBody.Dock = DockStyle.Fill;
      this.formBody.Location = new Point(0, 0);
      this.formBody.Name = "formBody";
      this.formBody.Size = new Size(292, 266);
      this.formBody.TabIndex = 0;
      this.formBody.MouseClick += new MouseEventHandler(this.formBody_MouseClick);
      this.formBody.MouseDown += new MouseEventHandler(this.formBody_MouseDown);
      this.formBody.MouseEnter += new EventHandler(this.formBody_MouseEnter);
      this.formMenu.Items.AddRange(new ToolStripItem[4]
      {
        (ToolStripItem) this.closeToolStripMenuItem,
        (ToolStripItem) this.floatToolStripMenuItem,
        (ToolStripItem) this.dockLeftToolStripMenuItem,
        (ToolStripItem) this.dockRightToolStripMenuItem
      });
      this.formMenu.Name = "contextMenuStrip1";
      this.formMenu.Size = new Size(137, 92);
      this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
      this.closeToolStripMenuItem.Size = new Size(136, 22);
      this.closeToolStripMenuItem.Text = "Close";
      this.closeToolStripMenuItem.Click += new EventHandler(this.closeToolStripMenuItem_Click);
      this.floatToolStripMenuItem.Checked = true;
      this.floatToolStripMenuItem.CheckState = CheckState.Checked;
      this.floatToolStripMenuItem.Name = "floatToolStripMenuItem";
      this.floatToolStripMenuItem.Size = new Size(136, 22);
      this.floatToolStripMenuItem.Text = "Float";
      this.floatToolStripMenuItem.Click += new EventHandler(this.floatToolStripMenuItem_Click);
      this.dockLeftToolStripMenuItem.Name = "dockLeftToolStripMenuItem";
      this.dockLeftToolStripMenuItem.Size = new Size(136, 22);
      this.dockLeftToolStripMenuItem.Text = "Dock Left";
      this.dockLeftToolStripMenuItem.Click += new EventHandler(this.dockLeftToolStripMenuItem_Click);
      this.dockRightToolStripMenuItem.Name = "dockRightToolStripMenuItem";
      this.dockRightToolStripMenuItem.Size = new Size(136, 22);
      this.dockRightToolStripMenuItem.Text = "Dock Right";
      this.dockRightToolStripMenuItem.Click += new EventHandler(this.dockRightToolStripMenuItem_Click);
      this.ClientSize = new Size(292, 266);
      this.Controls.Add((Control) this.formBody);
      this.FormBorderStyle = FormBorderStyle.None;
      this.Name = "SkinnedMDIChild";
      this.ShowInTaskbar = false;
      this.StartPosition = FormStartPosition.Manual;
      this.Text = "SkinnedMDICHild";
      this.TransparencyKey = Color.Magenta;
      this.Load += new EventHandler(this.SkinnedMDIChild_Load);
      this.MouseClick += new MouseEventHandler(this.SkinnedMDIChild_MouseClick);
      this.MouseDown += new MouseEventHandler(this.SkinnedMDIChild_MouseDown);
      this.FormClosing += new FormClosingEventHandler(this.SkinnedMDIChild_FormClosing);
      this.formMenu.ResumeLayout(false);
      this.ResumeLayout(false);
    }

    public void ShowForm()
    {
      ((Control) this).Show();
      this.SetDockMenu();
    }

    private void SkinnedMDIChild_Load(object sender, EventArgs e)
    {
      this.GetLastPos();
      this.SetDockMenu();
    }

    private void SkinnedMDIChild_FormClosing(object sender, FormClosingEventArgs e)
    {
      this._plugin.forms.Remove(sender);
    }

    private void closeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (this.CloseCommand != null)
            this.CloseCommand.Tag = false;

        if (this.CloseCommand == null)
            this.Hide();
        else
            this._plugin.CbClose((object)this.CloseCommand, e);
    }

    private void SkinnedMDIChild_MouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button != MouseButtons.Right)
        return;
      this.formMenu.Show((Control) this, e.Location);
    }

    private void SkinnedMDIChild_MouseClick(object sender, MouseEventArgs e)
    {
      if (e.Button == MouseButtons.Right)
        return;
      this.formMenu.Hide();
    }

    private void dockLeftToolStripMenuItem_Click(object sender, EventArgs e)
    {
      if (this.Dock == DockStyle.None)
        this.GetLastPos();
      this.Dock = DockStyle.Left;
      this.SetDockMenu();
    }

    private void floatToolStripMenuItem_Click(object sender, EventArgs e)
    {
      this.Dock = DockStyle.None;
      this.SetDockMenu();
      this.GetLastPos();
    }

    private void dockRightToolStripMenuItem_Click(object sender, EventArgs e)
    {
      if (this.Dock == DockStyle.None)
        this.GetLastPos();
      this.Dock = DockStyle.Right;
      this.SetDockMenu();
    }

    private void formBody_MouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button != MouseButtons.Right)
        return;
      this.formMenu.Show((Control) this, e.Location);
    }

    private void formBody_MouseClick(object sender, MouseEventArgs e)
    {
      if (e.Button == MouseButtons.Right)
        return;
      this.formMenu.Hide();
    }

    private void formBody_MouseEnter(object sender, EventArgs e)
    {
      this.Cursor = Cursors.Default;
    }

    private void GetLastPos()
    {
      this._lastLoc = this.Location;
      this._lastSiz = this.Size;
      this._lastDock = this.Dock;
    }

    private void SetLastPos()
    {
      this.Location = this._lastLoc;
      this.Size = this._lastSiz;
      this.Dock = this._lastDock;
    }

    private void SetDockMenu()
    {
      this.dockLeftToolStripMenuItem.Checked = this.Dock == DockStyle.Left;
      this.dockRightToolStripMenuItem.Checked = this.Dock == DockStyle.Right;
      this.floatToolStripMenuItem.Checked = this.Dock == DockStyle.None;
    }

    private void InitializeSkinning()
    {
      this.Paint += new PaintEventHandler(this.Skinning_Paint);
      this.Resize += new EventHandler(this.Skinning_Resize);
      this.TextChanged += new EventHandler(this.Skinning_TextChanged);
      this.MouseMove += new MouseEventHandler(this.Skinning_MouseMove);
      this.MouseDown += new MouseEventHandler(this.Skinning_MouseDown);
      this.Load += new EventHandler(this.Skinning_Load);
    }

    private Region getOpaqueRegion(Bitmap scanBitmap, Color transColor)
    {
      GraphicsPath path = new GraphicsPath();
      Color color = Color.FromArgb((int) transColor.R, (int) transColor.G, (int) transColor.B);
      for (int x = 0; x < scanBitmap.Width; ++x)
      {
        for (int y = 0; y < scanBitmap.Height; ++y)
        {
          if (scanBitmap.GetPixel(x, y) != color)
            path.AddRectangle(new Rectangle(x, y, 1, 1));
        }
      }
      Region region = new Region(path);
      path.Dispose();
      return region;
    }

    private void Skinning_Load(object sender, EventArgs e)
    {
      this._topLeft = Resources.skin_topleft;
      this._topRight = Resources.skin_topright;
      this._topMiddle = Resources.skin_top;
      this._topMargin = this._topMiddle.Height;
      this._leftSide = Resources.skin_left;
      this._leftMargin = this._leftSide.Width;
      this._rightSide = Resources.skin_right;
      this._rightMargin = this._rightSide.Width;
      this._bottomMiddle = Resources.skin_bottom;
      this._bottomMargin = this._bottomMiddle.Height;
      this._topBorder = this._bottomMargin;
      this._bottomLeft = Resources.skin_bottomleft;
      this._bottomRight = Resources.skin_bottomright;
      this._topLeftTrans = this.getOpaqueRegion(this._topLeft, Color.Magenta);
      this._topRightTrans = this.getOpaqueRegion(this._topRight, Color.Magenta);
      this._titleFont = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular, GraphicsUnit.Point, (byte) 0);
      int width = TextRenderer.MeasureText(this.Text, this._titleFont).Width;
      this.Padding = new Padding(this._leftMargin, this._topMargin, this._rightMargin, this._bottomMargin);
      this.MinimumSize = new Size(this._topLeft.Width + this._topRight.Width + width, this._topMargin + this._bottomMargin + 1);
      this.SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    private void Skinning_TextChanged(object sender, EventArgs e)
    {
      if (this._titleFont == null)
        return;
      this.MinimumSize = new Size(this._topLeft.Width + this._topRight.Width + TextRenderer.MeasureText(this.Text, this._titleFont).Width, this._topMargin + this._bottomMargin + 1);
    }

    private void Skinning_MouseDown(object sender, MouseEventArgs e)
    {
      IntPtr wparam = IntPtr.Zero;
      Cursor cursor = Cursors.Default;
      if (e.Button != MouseButtons.Left)
        return;
      this.Capture = false;
      if (e.Y < this._topBorder)
      {
        if (e.X < this._leftMargin)
        {
          cursor = Cursors.SizeNWSE;
          wparam = (IntPtr) 13;
        }
        else if (e.X > this.Width - this._rightMargin)
        {
          cursor = Cursors.SizeNESW;
          wparam = (IntPtr) 14;
        }
        else
        {
          cursor = Cursors.SizeNS;
          wparam = (IntPtr) 12;
        }
      }
      else if (e.Y > this.Height - this._bottomMargin)
      {
        if (e.X < this._leftMargin)
        {
          cursor = Cursors.SizeNESW;
          wparam = (IntPtr) 16;
        }
        else if (e.X > this.Width - this._rightMargin)
        {
          cursor = Cursors.SizeNWSE;
          wparam = (IntPtr) 17;
        }
        else
        {
          cursor = Cursors.SizeNS;
          wparam = (IntPtr) 15;
        }
      }
      else if (e.X < this._leftMargin)
      {
        cursor = Cursors.SizeWE;
        wparam = (IntPtr) 10;
      }
      else if (e.X > this.Width - this._rightMargin)
      {
        cursor = Cursors.SizeWE;
        wparam = (IntPtr) 11;
      }
      else if (e.Y < this._topMargin && e.Y >= this._topBorder)
        wparam = (IntPtr) 2;
      if (this.Dock != DockStyle.None && (this.Dock != DockStyle.Right || !(wparam == (IntPtr) 10)) && (this.Dock != DockStyle.Left || !(wparam == (IntPtr) 11)))
        return;
      this.Cursor = cursor;
      if (!(wparam != IntPtr.Zero))
        return;
      Message m = Message.Create(this.Handle, 161, wparam, IntPtr.Zero);
      this.DefWndProc(ref m);
    }

    private void Skinning_Paint(object sender, PaintEventArgs e)
    {
      if (this._topLeft != null && this.Dock == DockStyle.None)
      {
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.DrawImage((Image) this._topLeft, 0, 0, this._topLeft.Width, this._topLeft.Height);
        int num1 = this.Width - this._topRight.Width;
        int width1 = this._topLeft.Width;
        while (width1 < num1)
        {
          e.Graphics.DrawImage((Image) this._topMiddle, width1, 0, this._topMiddle.Width, this._topMiddle.Height);
          width1 += this._topMiddle.Width;
        }
        e.Graphics.DrawImage((Image) this._topRight, this.Width - this._topRight.Width, 0, this._topRight.Width, this._topRight.Height);
        int num2 = this.Height - this._bottomLeft.Height;
        int height1 = this._topLeft.Height;
        while (height1 < num2)
        {
          e.Graphics.DrawImage((Image) this._leftSide, 0, height1, this._leftSide.Width, this._leftSide.Height);
          height1 += this._leftSide.Height;
        }
        e.Graphics.DrawString(this.Text, this._titleFont, (Brush) new SolidBrush(Color.White), (float) this._leftMargin, (float) (this._bottomMargin / 2));
        e.Graphics.DrawImage((Image) this._bottomLeft, 0, this.Height - this._bottomLeft.Height, this._bottomLeft.Width, this._bottomLeft.Height);
        int num3 = this.Width - this._bottomRight.Width;
        int width2 = this._bottomLeft.Width;
        while (width2 < num3)
        {
          e.Graphics.DrawImage((Image) this._bottomMiddle, width2, this.Height - this._bottomMiddle.Height, this._bottomMiddle.Width, this._bottomMiddle.Height);
          width2 += this._bottomMiddle.Width;
        }
        int num4 = this.Height - this._bottomRight.Height;
        int height2 = this._topRight.Height;
        while (height2 < num4)
        {
          e.Graphics.DrawImage((Image) this._rightSide, this.Width - this._rightSide.Width, height2, this._rightSide.Width, this._rightSide.Height);
          height2 += this._rightSide.Height;
        }
        e.Graphics.DrawImage((Image) this._bottomRight, this.Width - this._bottomRight.Width, this.Height - this._bottomRight.Height, this._bottomRight.Width, this._bottomRight.Height);
        this.Padding = new Padding(this._leftMargin, this._topMargin, this._rightMargin, this._bottomMargin);
      }
      else if (this.Dock == DockStyle.Left)
      {
        int num = this.Width - this._rightSide.Width;
        int x = 0;
        while (x < num)
        {
          e.Graphics.DrawImage((Image) this._topMiddle, x, 0, this._topMiddle.Width, this._topMiddle.Height);
          x += this._topMiddle.Width;
        }
        e.Graphics.DrawString(this.Text, this._titleFont, (Brush) new SolidBrush(Color.White), (float) this._leftMargin, (float) (this._bottomMargin / 2));
        int height = this.Height;
        int y = 0;
        while (y < height)
        {
          e.Graphics.DrawImage((Image) this._rightSide, this.Width - this._rightSide.Width, y, this._rightSide.Width, this._rightSide.Height);
          y += this._rightSide.Height;
        }
        this.Padding = new Padding(0, this._topMargin, this._rightMargin, 0);
      }
      else
      {
        if (this.Dock != DockStyle.Right)
          return;
        int width1 = this.Width;
        int width2 = this._leftSide.Width;
        while (width2 < width1)
        {
          e.Graphics.DrawImage((Image) this._topMiddle, width2, 0, this._topMiddle.Width, this._topMiddle.Height);
          width2 += this._topMiddle.Width;
        }
        e.Graphics.DrawString(this.Text, this._titleFont, (Brush) new SolidBrush(Color.White), (float) this._leftMargin, (float) (this._bottomMargin / 2));
        int height = this.Height;
        int y = 0;
        while (y < height)
        {
          e.Graphics.DrawImage((Image) this._leftSide, 0, y, this._leftSide.Width, this._leftSide.Height);
          y += this._leftSide.Height;
        }
        this.Padding = new Padding(this._leftMargin, this._topMargin, 0, 0);
      }
    }

    private void Skinning_MouseMove(object sender, MouseEventArgs e)
    {
      Cursor cursor = Cursors.Default;
      if (e.Y < this._topBorder)
        cursor = e.X >= this._leftMargin ? (e.X <= this.Width - this._rightMargin ? Cursors.SizeNS : Cursors.SizeNESW) : Cursors.SizeNWSE;
      else if (e.Y > this.Height - this._bottomMargin)
        cursor = e.X >= this._leftMargin ? (e.X <= this.Width - this._rightMargin ? Cursors.SizeNS : Cursors.SizeNWSE) : Cursors.SizeNESW;
      else if (e.X > this.Width - this._rightMargin || e.X < this._leftMargin)
        cursor = Cursors.SizeWE;
      if (this.Dock != DockStyle.None && (this.Dock != DockStyle.Right || e.X >= this._leftMargin) && (this.Dock != DockStyle.Left || e.X <= this.Width - this._rightMargin))
        return;
      this.Cursor = cursor;
    }

    private void Skinning_Resize(object sender, EventArgs e)
    {
      Region region1;
      if (this._topLeft != null && this.Dock == DockStyle.None)
      {
        Region region2 = new Region();
        region1 = this._topLeftTrans.Clone();
        region1.Union(new Rectangle(this._topLeft.Width, 0, this.Width - this._topLeft.Width - this._topRight.Width, this._topLeft.Height));
        Region region3 = this._topRightTrans.Clone();
        region3.Translate(this.Width - this._topRight.Width, 0);
        region1.Union(region3);
        region1.Union(new Rectangle(0, this._topLeft.Height, this.Width, this.Height - this._topLeft.Height));
      }
      else
        region1 = new Region(new Rectangle(0, 0, this.Width, this.Height));
      this.Region = region1;
      this.Invalidate();
      this.formBody.Invalidate();
    }
  }
}
