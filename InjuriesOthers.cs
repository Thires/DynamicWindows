using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace DynamicWindows
{
    public class InjuriesOthersWindow
    {
        private readonly Plugin plugin;
        private readonly Dictionary<string, Panel> otherPanels = new Dictionary<string, Panel>();
        private readonly Dictionary<string, string> windowInjuryCommands = new Dictionary<string, string>();
        private readonly ToolTip woundTip = new ToolTip();

        private readonly Dictionary<string, Point> injuryMarkerPositions = new Dictionary<string, Point>
        {
            { "head",      new Point(52,  10) }, { "neck",      new Point(52,  28) },
            { "chest",     new Point(52,  48) }, { "abdomen",   new Point(52,  68) },
            { "back",      new Point(102, 48) }, { "leftArm",   new Point(25,  48) },
            { "rightArm",  new Point(80,  48) }, { "leftHand",  new Point(8,   92) },
            { "rightHand", new Point(92,  92) }, { "leftLeg",   new Point(34, 125) },
            { "rightLeg",  new Point(70, 125) }, { "leftEye",   new Point(4,    2) },
            { "rightEye",  new Point(92,   2) }, { "nsys",      new Point(4,   48) },
            { "rightFoot", new Point(92, 150) }
        };

        private readonly Dictionary<string, Point> scarMarkerPositions;

        public InjuriesOthersWindow(Plugin plugin)
        {
            this.plugin = plugin;
            scarMarkerPositions = new Dictionary<string, Point>(injuryMarkerPositions);
        }

        private static Image? GetBodyImage(string? command)
        {
            bool isInternal = command != null &&
                (command.Contains("_injury 3") || command.Contains("_injury 4") || command.Contains("_injury 5"));
            string resourceName = isInternal ? "body_image_int" : "body_image_ext";
            return (Image?)Properties.Resources.ResourceManager.GetObject(resourceName);
        }

        public void Create(string id, string title)
        {
            // Close all existing other-injuries windows before opening a new one
            if (id.StartsWith("injuries-"))
            {
                foreach (var form in plugin.forms.Where(f => f.Name.StartsWith("injuries-")).ToList())
                {
                    form.Close();
                    plugin.forms.Remove(form);
                }
            }

            windowInjuryCommands.TryGetValue(id, out string? checkedCmd);

            var window = new DwForm(plugin.ghost, plugin)
            {
                Owner = plugin.pForm,
                Text = title,
                Name = id,
                ClientSize = new Size(230, 290),
                StartPosition = FormStartPosition.Manual,
                Location = plugin.positionList.TryGetValue(id, out Point savedPos)
                    ? savedPos
                    : new Point(100, 100)
            };

            window.LocationChanged += delegate { plugin.positionList[id] = window.Location; };
            window.FormBody.BackColor = plugin.formback;
            window.FormBody.ForeColor = plugin.formfore;
            window.FormBody.AutoScroll = false;

            var panel = new Panel
            {
                Name = "injurySilhouette",
                Size = new Size(120, 200),
                Location = new Point(10, 10),
                BackColor = Color.Transparent,
                BackgroundImage = GetBodyImage(checkedCmd),
                BackgroundImageLayout = ImageLayout.Zoom
            };
            window.FormBody.Controls.Add(panel);
            otherPanels[id] = panel;

            string characterSuffix = id.Replace("injuries-", "");
            string[] labels = { "E Wound", "I Wound", "E Scar", "I Scar", "E Both", "I Both" };
            string[] cmds =
            {
                "_injury 0 -" + characterSuffix,
                "_injury 3 -" + characterSuffix,
                "_injury 1 -" + characterSuffix,
                "_injury 4 -" + characterSuffix,
                "_injury 2 -" + characterSuffix,
                "_injury 5 -" + characterSuffix
            };
            int y = 10;

            for (int i = 0; i < labels.Length; i++)
            {
                var radio = new CbRadio
                {
                    Name = "injr_" + i,
                    Text = labels[i],
                    group = "injureMode",
                    command = cmds[i],
                    Checked = checkedCmd == null ? i == 0 : cmds[i] == checkedCmd,
                    Location = new Point(140, y),
                    Size = new Size(100, 20)
                };

                radio.CheckedChanged += (sender, e) =>
                {
                    var r = (CbRadio)sender!;
                    if (r.Checked)
                    {
                        windowInjuryCommands[id] = r.command;
                        if (otherPanels.TryGetValue(id, out Panel? p))
                            p.BackgroundImage = GetBodyImage(r.command);
                        plugin.ghost.SendText(r.command);
                        window.BringToFront();
                    }
                };

                window.FormBody.Controls.Add(radio);
                y += 20;
            }

            plugin.forms.Add(window);
            window.ShowNoActivate();
        }

        public void Update(string id, XmlElement elem)
        {
            DwForm? window = plugin.forms.FirstOrDefault(f => f.Name == id);
            if (window == null || window.IsDisposed) return;

            Panel? panel = null;
            if (!otherPanels.TryGetValue(id, out panel) &&
                window.FormBody.Controls.ContainsKey("injurySilhouette"))
            {
                panel = (Panel)window.FormBody.Controls["injurySilhouette"]!;
                otherPanels[id] = panel;
            }
            if (panel == null) return;

            // ── Radio buttons ─────────────────────────────────────────────────
            XmlNodeList radios = elem.GetElementsByTagName("radio");
            if (radios.Count > 0)
            {
                for (int i = window.FormBody.Controls.Count - 1; i >= 0; i--)
                    if (window.FormBody.Controls[i] is CbRadio)
                        window.FormBody.Controls.RemoveAt(i);

                int ry = 10;
                string? lastCheckedCmd = null;

                foreach (XmlElement radioElem in radios)
                {
                    var radio = new CbRadio
                    {
                        Text = radioElem.GetAttribute("text"),
                        command = radioElem.GetAttribute("cmd"),
                        group = "injureMode",
                        Checked = radioElem.GetAttribute("value") != "0",
                        Location = new Point(140, ry),
                        Size = new Size(100, 20)
                    };

                    if (radio.Checked) lastCheckedCmd = radio.command;

                    radio.CheckedChanged += (sender, e) =>
                    {
                        var r = (CbRadio)sender!;
                        if (r.Checked)
                        {
                            windowInjuryCommands[id] = r.command;
                            if (otherPanels.TryGetValue(id, out Panel? p))
                                p.BackgroundImage = GetBodyImage(r.command);
                            plugin.ghost.SendText(r.command);
                            window.BringToFront();
                        }
                    };

                    window.FormBody.Controls.Add(radio);
                    ry += 20;
                }

                if (lastCheckedCmd != null)
                    windowInjuryCommands[id] = lastCheckedCmd;
            }

            // ── Command buttons ───────────────────────────────────────────────
            XmlNodeList buttons = elem.GetElementsByTagName("cmdButton");
            if (buttons.Count > 0)
            {
                for (int i = window.FormBody.Controls.Count - 1; i >= 0; i--)
                    if (window.FormBody.Controls[i] is CmdButton)
                        window.FormBody.Controls.RemoveAt(i);

                const int btnW = 80, btnH = 22, spacing = 6;
                int cmdY = 240;

                for (int i = 0; i < buttons.Count; i++)
                {
                    var btnElem = (XmlElement)buttons[i]!;
                    var btn = new CmdButton
                    {
                        Name = btnElem.GetAttribute("id"),
                        Text = btnElem.GetAttribute("value"),
                        cmd_string = btnElem.GetAttribute("cmd"),
                        Size = new Size(btnW, btnH),
                        Location = new Point(10 + (i % 2) * (btnW + spacing), cmdY)
                    };
                    btn.Click += (sender, e) => plugin.ghost.SendText(((CmdButton)sender!).cmd_string);
                    window.FormBody.Controls.Add(btn);
                    if (i % 2 == 1) cmdY += btnH + 4;
                }
            }

            // ── Wound markers ─────────────────────────────────────────────────
            XmlNodeList images = elem.GetElementsByTagName("image");
            if (images.Count > 0)
            {
                List<string> activeWoundTypes = GetActiveWoundTypes(id);
                panel.SuspendLayout();
                panel.Controls.Clear();

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

                    panel.Controls.Add(marker);
                }

                panel.ResumeLayout();
                panel.Invalidate();
                panel.Refresh();
            }

            // ── Health bar ────────────────────────────────────────────────────
            try
            {
                XmlNodeList bars = elem.GetElementsByTagName("progressBar");
                if (bars.Count > 0)
                {
                    for (int i = window.FormBody.Controls.Count - 1; i >= 0; i--)
                        if (window.FormBody.Controls[i] is ProgressBar)
                            window.FormBody.Controls.RemoveAt(i);

                    var barElem = (XmlElement)bars[0]!;
                    int.TryParse(barElem.GetAttribute("value"), out int value);

                    var bar = new OthersHealthBar
                    {
                        Name = "health_" + id,
                        Location = new Point(10, 215),
                        Size = new Size(180, 18),
                        Value = Math.Min(100, Math.Max(0, value))
                    };
                    window.FormBody.Controls.Add(bar);
                }
            }
            catch (Exception ex)
            {
                plugin.ghost.EchoText($"[InjuriesOthers] Update error for {id}: {ex.Message}");
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

        private List<string> GetActiveWoundTypes(string id)
        {
            windowInjuryCommands.TryGetValue(id, out string? command);
            command ??= "_injury 0 -1";

            if (command.Contains("_injury 0")) return new List<string> { "Injury1", "Injury2", "Injury3", "Nsys1", "Nsys2", "Nsys3" };
            if (command.Contains("_injury 1")) return new List<string> { "Scar1", "Scar2", "Scar3", "Nsys1", "Nsys2", "Nsys3" };
            if (command.Contains("_injury 2")) return new List<string> { "Injury1", "Injury2", "Injury3", "Injury4", "Injury5", "Scar1", "Scar2", "Scar3", "Nsys1", "Nsys2", "Nsys3" };
            if (command.Contains("_injury 3")) return new List<string> { "Injury1", "Injury2", "Injury3", "Injury4", "Injury5", "Nsys1", "Nsys2", "Nsys3" };
            if (command.Contains("_injury 4")) return new List<string> { "Scar1", "Scar2", "Scar3", "Nsys1", "Nsys2", "Nsys3" };
            if (command.Contains("_injury 5")) return new List<string> { "Injury1", "Injury2", "Injury3", "Scar1", "Scar2", "Scar3", "Nsys1", "Nsys2", "Nsys3" };
            return new List<string>();
        }
    }

    public class OthersHealthBar : ProgressBar
    {
        public OthersHealthBar()
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
