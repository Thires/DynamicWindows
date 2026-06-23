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
        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        // ── Public API ───────────────────────────────────────────────────────

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
                TabIndex = 0,
            };
            FormBody.MouseClick += FormBody_MouseClick;
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
