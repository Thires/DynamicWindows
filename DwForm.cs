using GeniePlugin.Interfaces;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DynamicWindows
{
    public class DwForm : Form
    {
        // ── Win32 P/Invoke for no-activate show ───────────────────────────────
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, uint uFlags);

        private const int SW_SHOWNOACTIVATE = 4;
        private static readonly IntPtr HWND_TOP = new(0);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        // ── Win32 P/Invoke for title-bar (caption) recoloring via DWM ─────────
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hWnd, int attr, ref int attrValue, int attrSize);

        // Newer builds use 20; Win10 1809 (build 17763) used 19.
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        private const int DWMWA_BORDER_COLOR = 34;   // Win11 22000+
        private const int DWMWA_CAPTION_COLOR = 35;  // Win11 22000+
        private const int DWMWA_TEXT_COLOR = 36;     // Win11 22000+

        // ── Public API (mirrors SkinnedMDIChild) ─────────────────────────────

        public Panel FormBody { get; private set; } = null!;

        public Button? CloseCommand { get; set; }

        public bool NoActivate { get; set; }

        protected override bool ShowWithoutActivation => NoActivate;

        // ── Private state ─────────────────────────────────────────────────────
        private readonly IHost _host;
        private readonly Plugin _plugin;

        private ContextMenuStrip _contextMenu = null!;
        private ToolStripMenuItem _closeMenuItem = null!;
        private ToolStripMenuItem _floatMenuItem = null!;
        private ToolStripMenuItem _dockLeftMenuItem = null!;
        private ToolStripMenuItem _dockRightMenuItem = null!;

        private Point _floatLocation;
        private Size _floatSize;

        // ── Constructor ────────────────────────────────────────────────────────

        public DwForm(IHost host, Plugin plugin)
        {
            _host = host;
            _plugin = plugin;

            SuspendLayout();
            BuildFormBody();
            BuildContextMenu();
            ApplyFormDefaults();
            ResumeLayout(false);
        }

        // ── Initialisation helpers ─────────────────────────────────────────────

        private void BuildFormBody()
        {
            FormBody = new Panel
            {
                Name = "formBody",
                Dock = DockStyle.Fill,
                BackColor = SystemColors.ControlDark,
                Padding = new Padding(1),   // reveals the 1px border for docked content
                TabIndex = 0,
            };
            FormBody.MouseClick += FormBody_MouseClick;
            FormBody.Paint += FormBody_Paint;
            FormBody.Resize += (s, e) => FormBody.Invalidate();
            Controls.Add(FormBody);
        }

        private void BuildContextMenu()
        {
            _closeMenuItem = new ToolStripMenuItem("Close", null, CloseMenuItem_Click);
            _floatMenuItem = new ToolStripMenuItem("Float", null, FloatMenuItem_Click) { Checked = true };
            _dockLeftMenuItem = new ToolStripMenuItem("Dock Left", null, DockLeftMenuItem_Click);
            _dockRightMenuItem = new ToolStripMenuItem("Dock Right", null, DockRightMenuItem_Click);

            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.AddRange(new ToolStripItem[]
            {
                _closeMenuItem, _floatMenuItem, _dockLeftMenuItem, _dockRightMenuItem,
            });
        }

        private void ApplyFormDefaults()
        {
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "DwForm";
            Name = "DwForm";

            Load += DwForm_Load;
            FormClosing += DwForm_FormClosing;
            MouseClick += Form_MouseClick;
        }

        // ── Public show API ────────────────────────────────────────────────────

        // ── Title-bar coloring ───────────────────────────────────────────────
        // The OS draws the caption (title bar) in its default theme, which can be
        // unreadable against the plugin's theme. Recolor it via DWM to match.
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyTitleBarColors();
        }

        public void ApplyTitleBarColors()
        {
            if (!IsHandleCreated) return;
            try
            {
                // Dark/light caption mode (Win10 1809+). Covers builds without
                // explicit color support so the title bar at least matches light/dark.
                int dark = IsDark(_plugin.formback) ? 1 : 0;
                if (DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int)) != 0)
                    DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref dark, sizeof(int));

                // Explicit caption background, text, and border colors (Win11 22000+).
                int caption = ToColorRef(_plugin.formback);
                DwmSetWindowAttribute(Handle, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
                int text = ToColorRef(_plugin.formfore);
                DwmSetWindowAttribute(Handle, DWMWA_TEXT_COLOR, ref text, sizeof(int));
                int border = ToColorRef(BorderColor());
                DwmSetWindowAttribute(Handle, DWMWA_BORDER_COLOR, ref border, sizeof(int));
            }
            catch { /* DWM unavailable (pre-Win10) — leave the default title bar */ }
        }

        // COLORREF is 0x00BBGGRR.
        private static int ToColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

        private static bool IsDark(Color c) => (c.R * 0.299 + c.G * 0.587 + c.B * 0.114) < 128.0;

        // A thin frame so windows don't blend into a dark background. Derived from the
        // theme (45% of the way from the body color toward the text color) so it stays
        // visible on both dark and light themes.
        private Color BorderColor()
        {
            Color b = _plugin.formback, f = _plugin.formfore;
            static int Mix(int x, int y) => x + (int)((y - x) * 0.45);
            return Color.FromArgb(Mix(b.R, f.R), Mix(b.G, f.G), Mix(b.B, f.B));
        }

        private void FormBody_Paint(object? sender, PaintEventArgs e)
        {
            var r = FormBody.ClientRectangle;
            using var pen = new Pen(BorderColor());
            e.Graphics.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
        }

        public void ShowForm()
        {
            Show();
            UpdateDockMenu();
            BringToFront();
            try { Activate(); } catch { /* non-critical */ }
        }

        public void ShowNoActivate()
        {
            if (!IsHandleCreated)
            {
                // Create the handle without activating by using SW_SHOWNOACTIVATE
                SetVisibleCore(true);
            }

            ShowWindow(Handle, SW_SHOWNOACTIVATE);
            SetWindowPos(Handle, HWND_TOP, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

            UpdateDockMenu();
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        private void DwForm_Load(object? sender, EventArgs e) => UpdateDockMenu();

        private void DwForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _plugin.forms.Remove(this);
        }

        private void Form_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                _contextMenu.Show(this, e.Location);
        }

        private void FormBody_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                _contextMenu.Show(FormBody, e.Location);
        }

        // ── Context menu handlers ──────────────────────────────────────────────

        private void CloseMenuItem_Click(object? sender, EventArgs e)
        {
            if (CloseCommand != null)
                _plugin.CbClose(CloseCommand, e);
            else
                Hide();
        }

        private void FloatMenuItem_Click(object? sender, EventArgs e)
        {
            if (Dock == DockStyle.None) return;
            Dock = DockStyle.None;
            Location = _floatLocation;
            ClientSize = _floatSize;
            UpdateDockMenu();
        }

        private void DockLeftMenuItem_Click(object? sender, EventArgs e)
        {
            SaveFloatState();
            Dock = DockStyle.Left;
            UpdateDockMenu();
        }

        private void DockRightMenuItem_Click(object? sender, EventArgs e)
        {
            SaveFloatState();
            Dock = DockStyle.Right;
            UpdateDockMenu();
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void SaveFloatState()
        {
            if (Dock == DockStyle.None)
            {
                _floatLocation = Location;
                _floatSize = ClientSize;
            }
        }

        private void UpdateDockMenu()
        {
            _floatMenuItem.Checked = Dock == DockStyle.None;
            _dockLeftMenuItem.Checked = Dock == DockStyle.Left;
            _dockRightMenuItem.Checked = Dock == DockStyle.Right;
        }

        // ── Dispose ────────────────────────────────────────────────────────────

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _contextMenu?.Dispose();
            base.Dispose(disposing);
        }
    }
}
