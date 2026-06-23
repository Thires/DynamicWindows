using GeniePlugin.Interfaces;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicWindows
{
    public enum AimTimerMode { Off, CloseOnExpire, StayOpenOnExpire }

    public class AimTimer : IDisposable
    {
        private readonly Plugin _plugin;

        public AimTimerMode Mode { get; set; } = AimTimerMode.CloseOnExpire;

        private System.Windows.Forms.Timer? _tick;
        private long _expireEpoch;
        private long _startEpoch;

        public AimTimer(Plugin plugin)
        {
            _plugin = plugin;
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void OnOpen()
        {
            if (Mode == AimTimerMode.Off) return;
            ShowWindow();
        }

        public void OnClose()
        {
            if (Mode == AimTimerMode.CloseOnExpire)
                CloseWindow();
        }

        public void OnTimerValue(long epochValue)
        {
            if (Mode == AimTimerMode.Off) return;

            if (epochValue == 0)
            {
                StopTick();
            }
            else
            {
                _expireEpoch = epochValue;
                _startEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (_plugin.FindWindowByName("AimTimerDialog") == null)
                {
                    ShowWindow();
                }
                // If the window already exists just let it sit — no focus steal

                StartTick();

                // Paint immediately — don't wait for first tick
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long remaining = _expireEpoch - now;
                long total = _expireEpoch - _startEpoch;
                double pct = total > 0 ? (double)remaining / total : 1.0;
                UpdateDisplay(remaining, pct);
            }
        }

        // ── Window ───────────────────────────────────────────────────────────

        private void ShowWindow()
        {
            var existing = _plugin.FindWindowByName("AimTimerDialog");
            if (existing != null) return;  // already visible, don't re-raise or steal focus

            // Bar height scales with the plugin scale factor; window height wraps it exactly.
            int barH = _plugin.S(18);
            int winW = _plugin.S(180);
            int winH = barH + 4;  // 2px padding top + 2px bottom, no dead space

            var win = _plugin.CreateWindow("AimTimerDialog", "Aim Timer", winW, winH);
            win.FormBody.BackColor = _plugin.formback;
            win.FormBody.Visible = false;

            var timerBar = new TimerBarPanel
            {
                Name = "bar",
                Dock = DockStyle.Fill,   // fills formBody exactly — no gap
                BackColor = _plugin.formback,
                ForeColor = _plugin.formfore,
                FillColor = _plugin.timerBarColor,
            };

            win.FormBody.Controls.Add(timerBar);
            win.FormBody.Visible = true;

            // When the window is closed, stop the tick and save its position
            win.FormClosed += (s, e) =>
            {
                _plugin.positionList["AimTimerDialog"] = win.Location;
                _tick?.Stop();
            };

            win.TopMost = false;
            win.NoActivate = true;   // never steal focus from Genie's input bar on open/reopen
            win.ShowNoActivate();

            // Default to centered over Genie's window on first use
            if (!_plugin.positionList.ContainsKey("AimTimerDialog"))
            {
                var owner = _plugin.pForm;
                win.Location = new Point(
                    owner.Left + (owner.Width - win.Width) / 2,
                    owner.Top + (owner.Height - win.Height) / 2);
            }
        }

        private void CloseWindow()
        {
            _tick?.Stop();
            _plugin.CloseWindowIfOpen("AimTimerDialog");
        }

        // ── Tick ─────────────────────────────────────────────────────────────

        private void StartTick()
        {
            if (_tick == null)
            {
                _tick = new System.Windows.Forms.Timer { Interval = 250 };
                _tick.Tick += OnTick;
            }
            _tick.Start();
        }

        private void StopTick()
        {
            _tick?.Stop();
            UpdateDisplay(0, 0.0);
        }

        private void OnTick(object? sender, EventArgs e)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long remaining = _expireEpoch - now;
            long total = _expireEpoch - _startEpoch;

            if (remaining <= 0) { StopTick(); return; }

            double pct = total > 0 ? (double)remaining / total : 0.0;
            UpdateDisplay(remaining, pct);
        }

        private void UpdateDisplay(long secondsLeft, double pct)
        {
            var win = _plugin.FindWindowByName("AimTimerDialog");
            if (win == null || win.IsDisposed) return;

            void update()
            {
                if (win.FormBody.Controls["bar"] is TimerBarPanel bar)
                {
                    bar.Fraction = pct;
                    bar.CountText = secondsLeft.ToString();
                    bar.Invalidate();
                }
                // No BringToFront here — the aim timer must not steal focus from
                // Genie's input bar while the player is trying to type commands.
            }

            if (win.InvokeRequired) win.Invoke((Action)update);
            else update();
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        public void Dispose()
        {
            _tick?.Stop();
            _tick?.Dispose();
            _tick = null;
            _plugin.CloseWindowIfOpen("AimTimerDialog");
            GC.SuppressFinalize(this);
        }
    }

    internal class TimerBarPanel : Panel
    {
        public double Fraction { get; set; } = 0.0;
        public string CountText { get; set; } = "0";
        public Color FillColor { get; set; } = Color.RoyalBlue;

        public TimerBarPanel()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle r = ClientRectangle;

            g.FillRectangle(new SolidBrush(BackColor), r);

            int fillW = (int)(r.Width * Fraction);
            if (fillW > 0)
                using (var fillBrush = new SolidBrush(FillColor))
                    g.FillRectangle(fillBrush, 0, 0, fillW, r.Height);

            using var font = new Font("Arial", 10, FontStyle.Bold);
            SizeF textSize = g.MeasureString(CountText, font);
            float tx = (r.Width - textSize.Width) / 2f;
            float ty = (r.Height - textSize.Height) / 2f;
            g.DrawString(CountText, font, new SolidBrush(ForeColor), tx, ty);
        }
    }
}
