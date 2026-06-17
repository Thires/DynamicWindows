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

        private System.Windows.Forms.Timer _tick;
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
                else
                {
                    var existing = _plugin.FindWindowByName("AimTimerDialog");
                    try { existing.Activate(); }
                    catch { existing.BringToFront(); }
                }

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
            if (existing != null)
            {
                existing.BringToFront();
                existing.Select();
                return;
            }

            var win = _plugin.CreateSkinnedWindow("AimTimerDialog", "Aim Timer", 200, 58);
            win.formBody.BackColor = _plugin.formback;
            win.formBody.Visible = false;

            var timerBar = new TimerBarPanel
            {
                Name = "bar",
                Left = 5,
                Top = 5,
                Width = 180,
                Height = 26,
                BackColor = _plugin.formback,
                ForeColor = _plugin.formfore,
            };

            win.formBody.Controls.Add(timerBar);
            win.formBody.Visible = true;

            // When the window is closed, just stop the tick — do NOT call UpdateDisplay
            win.FormClosed += (s, e) =>
            {
                // Capture final position before the save fires
                _plugin.positionList["AimTimerDialog"] = win.Location;
                _tick?.Stop();
            };

            win.TopMost = false;
            win.ShowForm();

            // Clear any bad saved position so it centers on first use
            if (!_plugin.positionList.ContainsKey("AimTimerDialog"))
                win.Location = new Point(
                    (_plugin.pForm.ClientSize.Width - win.Width) / 2,
                    (_plugin.pForm.ClientSize.Height - win.Height) / 2);

            // MDI children need Activate() not BringToFront/Select
            var t = new System.Windows.Forms.Timer { Interval = 10 };
            t.Tick += (ts, te) =>
            {
                t.Stop();
                t.Dispose();
                try { win.Activate(); }
                catch { win.BringToFront(); }
            };
            t.Start();
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
            // Update display to show zero — only if window is still alive
            UpdateDisplay(0, 0.0);
        }

        private void OnTick(object sender, EventArgs e)
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

            Action update = () =>
            {
                if (win.formBody.Controls["bar"] is TimerBarPanel bar)
                {
                    bar.Fraction = pct;
                    bar.CountText = secondsLeft.ToString();
                    bar.Invalidate();
                }

                // Re-raise on every update so it stays on top within the MDI container
                win.BringToFront();
                win.Focus();
            };

            if (win.InvokeRequired) win.Invoke(update);
            else update();
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        public void Dispose()
        {
            _tick?.Stop();
            _tick?.Dispose();
            _tick = null;
            _plugin.CloseWindowIfOpen("AimTimerDialog");
        }
    }

    /// <summary>Custom panel that owner-draws a progress bar with a centered countdown number.</summary>
    internal class TimerBarPanel : Panel
    {
        public double Fraction { get; set; } = 0.0;
        public string CountText { get; set; } = "0";

        public TimerBarPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
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
                g.FillRectangle(Brushes.RoyalBlue, 0, 0, fillW, r.Height);

            Font font = new Font("Arial", 11, FontStyle.Bold);
            SizeF textSize = g.MeasureString(CountText, font);
            float tx = (r.Width - textSize.Width) / 2f;
            float ty = (r.Height - textSize.Height) / 2f;
            g.DrawString(CountText, font, new SolidBrush(ForeColor), tx, ty);
            font.Dispose();
        }
    }
}
