using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace DynamicWindows
{
    public class InjuriesWindow
    {
        private readonly Plugin plugin;
        private Panel? injurySilhouettePanel;
        internal static string currentInjuryCommand = "_injury 0 -1";

        private readonly ToolTip woundTip = new ToolTip();

        private readonly Dictionary<string, Point> injuryMarkerPositions = new Dictionary<string, Point>
        {
            { "head",      new Point(52,  10) },
            { "neck",      new Point(52,  28) },
            { "chest",     new Point(52,  48) },
            { "abdomen",   new Point(52,  68) },
            { "back",      new Point(102, 48) },
            { "leftArm",   new Point(25,  48) },
            { "rightArm",  new Point(80,  48) },
            { "leftHand",  new Point(8,   92) },
            { "rightHand", new Point(92,  92) },
            { "leftLeg",   new Point(34, 125) },
            { "rightLeg",  new Point(70, 125) },
            { "leftEye",   new Point(4,    2) },
            { "rightEye",  new Point(92,   2) },
            { "nsys",      new Point(4,   48) },
            { "rightFoot", new Point(92, 150) }
        };

        private readonly Dictionary<string, Point> scarMarkerPositions = new Dictionary<string, Point>
        {
            { "head",      new Point(52,  10) },
            { "neck",      new Point(52,  28) },
            { "chest",     new Point(52,  48) },
            { "abdomen",   new Point(52,  68) },
            { "back",      new Point(102, 48) },
            { "leftArm",   new Point(25,  48) },
            { "rightArm",  new Point(80,  48) },
            { "leftHand",  new Point(8,   92) },
            { "rightHand", new Point(92,  92) },
            { "leftLeg",   new Point(34, 125) },
            { "rightLeg",  new Point(70, 125) },
            { "leftEye",   new Point(4,    2) },
            { "rightEye",  new Point(92,   2) },
            { "nsys",      new Point(4,   48) },
            { "rightFoot", new Point(92, 150) }
        };

        public InjuriesWindow(Plugin plugin)
        {
            this.plugin = plugin;
        }

        private static Image? GetBodyImage(string command)
        {
            bool isInternal = command == "_injury 3 -1" || command == "_injury 4 -1" || command == "_injury 5 -1";
            string resourceName = isInternal ? "body_image_int" : "body_image_ext";
            return (Image?)Properties.Resources.ResourceManager.GetObject(resourceName);
        }

        public void Create(XmlElement? elem)
        {
            // Close any existing injuries window first
            DwForm? existing = plugin.forms.FirstOrDefault(f => f.Name == "injuries");
            if (existing != null)
            {
                plugin.forms.Remove(existing);
                existing.Close();
            }

            var window = new DwForm(plugin.ghost, plugin)
            {
                Owner = plugin.pForm,
                Text = "Injuries",
                Name = "injuries",
                ClientSize = new Size(230, 260),
                StartPosition = FormStartPosition.Manual,
                Location = plugin.positionList.TryGetValue("injuries", out Point savedPos)
                    ? savedPos
                    : new Point(100, 100)
            };

            window.LocationChanged += delegate
            {
                plugin.positionList["injuries"] = window.Location;
            };

            window.FormBody.BackColor = plugin.formback;
            window.FormBody.ForeColor = plugin.formfore;
            window.FormBody.AutoScroll = false;

            injurySilhouettePanel = new Panel
            {
                Name = "injurySilhouette",
                Size = new Size(120, 200),
                Location = new Point(10, 10),
                BackColor = Color.Transparent,
                BackgroundImage = GetBodyImage(currentInjuryCommand),
                BackgroundImageLayout = ImageLayout.Zoom
            };
            window.FormBody.Controls.Add(injurySilhouettePanel);

            string[] radioLabels = { "E Wound", "I Wound", "E Scar", "I Scar", "E Both", "I Both" };
            string[] radioCommands = { "_injury 0 -1", "_injury 3 -1", "_injury 1 -1", "_injury 4 -1", "_injury 2 -1", "_injury 5 -1" };
            int y = 10;

            for (int i = 0; i < radioLabels.Length; i++)
            {
                var radio = new CbRadio
                {
                    Name = "injr_" + i,
                    Text = radioLabels[i],
                    group = "injureMode",
                    command = radioCommands[i],
                    Checked = radioCommands[i] == currentInjuryCommand,
                    Location = new Point(140, y),
                    Size = new Size(100, 20)
                };

                radio.CheckedChanged += (sender, e) =>
                {
                    var r = (CbRadio)sender!;
                    if (r.Checked)
                    {
                        currentInjuryCommand = r.command;
                        injurySilhouettePanel!.BackgroundImage = GetBodyImage(currentInjuryCommand);
                        plugin.ghost.SendText(currentInjuryCommand);
                        window.BringToFront();
                    }
                };

                window.FormBody.Controls.Add(radio);
                y += 20;
            }

            var healthBar = new HealthBar
            {
                Name = "health2",
                Value = 100,
                Location = new Point(10, 215),
                Size = new Size(180, 18)
            };
            window.FormBody.Controls.Add(healthBar);

            plugin.forms.Add(window);
            window.ShowNoActivate();
        }

        private static List<string> GetActiveWoundTypes()
        {
            return currentInjuryCommand switch
            {
                "_injury 0 -1" => new List<string> { "Injury1", "Injury2", "Injury3", "Nsys1", "Nsys2", "Nsys3" },
                "_injury 1 -1" => new List<string> { "Scar1", "Scar2", "Scar3", "Nsys1", "Nsys2", "Nsys3" },
                "_injury 2 -1" => new List<string> { "Injury1", "Injury2", "Injury3", "Injury4", "Injury5", "Scar1", "Scar2", "Scar3", "Nsys1", "Nsys2", "Nsys3" },
                "_injury 3 -1" => new List<string> { "Injury1", "Injury2", "Injury3", "Injury4", "Injury5", "Nsys1", "Nsys2", "Nsys3" },
                "_injury 4 -1" => new List<string> { "Scar1", "Scar2", "Scar3", "Nsys1", "Nsys2", "Nsys3" },
                "_injury 5 -1" => new List<string> { "Injury1", "Injury2", "Injury3", "Scar1", "Scar2", "Scar3", "Nsys1", "Nsys2", "Nsys3" },
                _ => new List<string>()
            };
        }

        public void Update(XmlElement elem)
        {
            DwForm? window = plugin.forms.FirstOrDefault(f => f.Name == "injuries");
            if (window == null) return;

            if (injurySilhouettePanel == null && window.FormBody.Controls.ContainsKey("injurySilhouette"))
                injurySilhouettePanel = (Panel)window.FormBody.Controls["injurySilhouette"]!;

            if (injurySilhouettePanel == null) return;

            // Enable double-buffering on the panel via reflection (Panel doesn't expose it publicly)
            injurySilhouettePanel.GetType()
                .GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(injurySilhouettePanel, true, null);

            List<string> activeWoundTypes = GetActiveWoundTypes();
            XmlNodeList images = elem.GetElementsByTagName("image");

            if (images.Count > 0)
            {
                bool foundMatch = images.Cast<XmlElement>()
                    .Any(img => activeWoundTypes.Contains(img.GetAttribute("name")));

                injurySilhouettePanel.SuspendLayout();

                if (!foundMatch)
                {
                    injurySilhouettePanel.Controls.Clear();
                    injurySilhouettePanel.Invalidate();
                    injurySilhouettePanel.Refresh();
                }
                else
                {
                    // Remove old markers that match the active wound types
                    var toRemove = injurySilhouettePanel.Controls.OfType<Label>()
                        .Where(lbl =>
                            (lbl.Text == "1" && (activeWoundTypes.Contains("Injury1") || activeWoundTypes.Contains("Scar1") || activeWoundTypes.Contains("Nsys1"))) ||
                            (lbl.Text == "2" && (activeWoundTypes.Contains("Injury2") || activeWoundTypes.Contains("Scar2") || activeWoundTypes.Contains("Nsys2"))) ||
                            (lbl.Text == "3" && (activeWoundTypes.Contains("Injury3") || activeWoundTypes.Contains("Scar3") || activeWoundTypes.Contains("Nsys3"))))
                        .ToList();

                    foreach (var ctrl in toRemove)
                        injurySilhouettePanel.Controls.Remove(ctrl);

                    foreach (XmlElement image in images)
                    {
                        string part = image.GetAttribute("id");
                        string name = image.GetAttribute("name");
                        if (!activeWoundTypes.Contains(name)) continue;

                        if (!TryGetMarkerParams(name, part, out Point location, out string text, out Color backColor))
                            continue;

                        var marker = new Label
                        {
                            Size = new Size(15, 15),
                            TextAlign = ContentAlignment.MiddleCenter,
                            ForeColor = text == "3" ? Color.White : Color.Black,
                            BackColor = backColor,
                            BorderStyle = BorderStyle.FixedSingle,
                            Text = text,
                            Location = location
                        };

                        string? tooltipText = image.HasAttribute("tooltip") ? image.GetAttribute("tooltip") : null;
                        string? cmdText = image.HasAttribute("cmd") ? image.GetAttribute("cmd") : null;

                        if (!string.IsNullOrEmpty(tooltipText))
                            woundTip.SetToolTip(marker, tooltipText);
                        if (!string.IsNullOrEmpty(cmdText))
                        {
                            marker.Cursor = Cursors.Hand;
                            marker.Click += (s, e) => plugin.ghost.SendText(cmdText);
                        }

                        injurySilhouettePanel.Controls.Add(marker);
                    }

                    injurySilhouettePanel.Invalidate();
                    injurySilhouettePanel.Refresh();
                }

                injurySilhouettePanel.ResumeLayout();
                injurySilhouettePanel.Invalidate();
            }

            // Health bar update — always runs
            foreach (XmlElement progress in elem.GetElementsByTagName("progressBar"))
            {
                string id = progress.GetAttribute("id");
                foreach (ProgressBar bar in window.FormBody.Controls.OfType<ProgressBar>())
                {
                    if (bar.Name == id && int.TryParse(progress.GetAttribute("value"), out int value))
                        bar.Value = Math.Min(bar.Maximum, Math.Max(bar.Minimum, value));
                }
            }
        }

        private bool TryGetMarkerParams(string name, string part,
            out Point location, out string text, out Color backColor)
        {
            location = Point.Empty;
            text = "";
            backColor = Color.Transparent;

            var positions = name.StartsWith("Scar") ? scarMarkerPositions : injuryMarkerPositions;

            if (!positions.TryGetValue(part, out location)) return false;

            if (name.EndsWith("1")) { text = "1"; backColor = Color.Yellow; return true; }
            if (name.EndsWith("2")) { text = "2"; backColor = Color.Orange; return true; }
            if (name.EndsWith("3")) { text = "3"; backColor = Color.Red; return true; }

            return false;
        }

        private static string GetInjuryPrefix()
        {
            return currentInjuryCommand switch
            {
                "_injury 0 -1" => "EWound",
                "_injury 1 -1" => "EScar",
                "_injury 2 -1" => "EBoth",
                "_injury 3 -1" => "IWound",
                "_injury 4 -1" => "IScar",
                "_injury 5 -1" => "IBoth",
                _ => "Unknown"
            };
        }

        // ── Inner class ───────────────────────────────────────────────────────

        private class HealthBar : ProgressBar
        {
            public HealthBar()
            {
                SetStyle(ControlStyles.UserPaint, true);
                ForeColor = Color.White;
                BackColor = Color.DarkRed;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Rectangle rec = e.ClipRectangle;
                rec.Width = (int)(rec.Width * ((double)Value / Maximum));
                if (rec.Width == 0) rec.Width = 1;

                using (var brush = new SolidBrush(Color.Red))
                    e.Graphics.FillRectangle(brush, rec);

                string text = "HEALTH " + Value + "%";
                SizeF len = e.Graphics.MeasureString(text, Font);
                using (var textBrush = new SolidBrush(Color.White))
                    e.Graphics.DrawString(text, Font, textBrush,
                        new PointF((Width - len.Width) / 2, (Height - len.Height) / 2));

                base.OnPaint(e);
            }
        }
    }
}
