using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace DynamicWindows
{
    public class InjuriesOthersWindow
    {
        private readonly Plugin plugin;
        private readonly Dictionary<string, Panel> otherPanels = new Dictionary<string, Panel>();
        private readonly Dictionary<string, string> windowInjuryCommands = new Dictionary<string, string>();

        private ToolTip woundTip = new ToolTip();

        private readonly Dictionary<string, Point> injuryMarkerPositions = new Dictionary<string, Point>
        {
            { "head", new Point(52, 10) }, { "neck", new Point(52, 28) }, { "chest", new Point(52, 48) },
            { "abdomen", new Point(52, 68) }, { "back", new Point(102, 48) },
            { "leftArm", new Point(25, 48) }, { "rightArm", new Point(80, 48) },
            { "leftHand", new Point(8, 92) }, { "rightHand", new Point(92, 92) },
            { "leftLeg", new Point(34, 125) }, { "rightLeg", new Point(70, 125) },
            { "leftEye", new Point(4, 2) }, { "rightEye", new Point(92, 2) }, { "nsys", new Point(4, 48) }
        };

        private readonly Dictionary<string, Point> scarMarkerPositions;

        public InjuriesOthersWindow(Plugin plugin)
        {
            this.plugin = plugin;
            this.scarMarkerPositions = new Dictionary<string, Point>(injuryMarkerPositions);
        }

        public void Create(string id, string title)
        {
            if (id.StartsWith("injuries-"))
            {
                SkinnedMDIChild existing = null;
                var formsToRemove = new List<SkinnedMDIChild>();
                for (int i = 0; i < plugin.forms.Count; i++)
                {
                    var form = plugin.forms[i] as SkinnedMDIChild;
                    if (form != null && form.Name.StartsWith("injuries-"))
                    {
                        formsToRemove.Add(form);
                    }
                }
                foreach (var form in formsToRemove)
                {
                    form.Close();
                    plugin.forms.Remove(form);
                }
            }

                SkinnedMDIChild window = new SkinnedMDIChild(plugin.ghost, plugin);
                window.MdiParent = plugin.pForm;
                window.Text = title;
                window.Name = id;
                window.ClientSize = new Size(230, 290);
                window.StartPosition = FormStartPosition.Manual;
                window.Location = plugin.positionList.ContainsKey(id) ? plugin.positionList[id] : new Point(100, 100);

                window.LocationChanged += delegate { plugin.positionList[id] = window.Location; };
                window.formBody.BackColor = plugin.formback;
                window.formBody.ForeColor = plugin.formfore;
                window.formBody.AutoScroll = false;

                Panel panel = new Panel();
                panel.Name = "injurySilhouette";
                panel.Size = new Size(120, 200);
                panel.Location = new Point(10, 10);
                panel.BackColor = Color.Transparent;

                panel.BackgroundImage = Properties.Resources.body_image;
                panel.BackgroundImageLayout = ImageLayout.Zoom;
                window.formBody.Controls.Add(panel);
                otherPanels[id] = panel;

                string[] labels = { "E Wound", "I Wound", "E Scar", "I Scar", "E Both", "I Both" };
                string characterSuffix = id.Replace("injuries-", "");

                string[] cmds = {
                "_injury 0 -" + characterSuffix,
                "_injury 3 -" + characterSuffix,
                "_injury 1 -" + characterSuffix,
                "_injury 4 -" + characterSuffix,
                "_injury 2 -" + characterSuffix,
                "_injury 5 -" + characterSuffix
            };
                int y = 10;

            string checkedCmd = null;
            windowInjuryCommands.TryGetValue(id, out checkedCmd);

            for (int i = 0; i < labels.Length; i++)
                {
                    CbRadio radio = new CbRadio();
                    radio.Name = "injr_" + i;
                    radio.Text = labels[i];
                    radio.group = "injureMode";
                    radio.command = cmds[i];
                    radio.Checked = (checkedCmd == null ? i == 0 : cmds[i] == checkedCmd);
                    radio.Location = new Point(140, y);
                    radio.Size = new Size(100, 20);

                    radio.CheckedChanged += delegate (object sender, EventArgs e)
                    {
                        CbRadio r = (CbRadio)sender;
                        if (r.Checked)
                        {
                            windowInjuryCommands[id] = r.command;
                            plugin.ghost.SendText(r.command);
                            window.BringToFront();
                        }
                    };
                    window.formBody.Controls.Add(radio);
                    y += 20;
                }
                plugin.forms.Add(window);
                window.ShowForm();
            }
        

        public void Update(string id, XmlElement elem)
        {
            SkinnedMDIChild window = null;
            foreach (SkinnedMDIChild f in plugin.forms)
                if (f.Name == id) { window = f; break; }
            if (window == null || window.IsDisposed)
                return;

            Panel panel = null;
            if (!otherPanels.TryGetValue(id, out panel) && window.formBody.Controls.ContainsKey("injurySilhouette"))
            {
                panel = (Panel)window.formBody.Controls["injurySilhouette"];
                otherPanels[id] = panel;
            }
            if (panel == null)
                return;

            // -- RADIO BUTTONS --
            XmlNodeList radios = elem.GetElementsByTagName("radio");
            if (radios != null && radios.Count > 0)
            {
                for (int i = window.formBody.Controls.Count - 1; i >= 0; i--)
                    if (window.formBody.Controls[i] is CbRadio)
                        window.formBody.Controls.RemoveAt(i);

                int y = 10;
                string lastCheckedCmd = null;
                foreach (XmlElement radioElem in radios)
                {
                    CbRadio radio = new CbRadio();
                    radio.Text = radioElem.GetAttribute("text");
                    radio.command = radioElem.GetAttribute("cmd");
                    radio.group = radioElem.GetAttribute("group");
                    radio.Checked = radioElem.GetAttribute("value") == "1";
                    radio.Location = new Point(140, y);
                    radio.Size = new Size(100, 20);
                    y += 20;

                    string thisId = id;
                    radio.CheckedChanged += delegate (object sender, EventArgs e)
                    {
                        CbRadio r = (CbRadio)sender;
                        if (r.Checked)
                        {
                            windowInjuryCommands[thisId] = r.command;
                            plugin.ghost.SendText(r.command);
                            window.BringToFront();
                        }
                    };
                    window.formBody.Controls.Add(radio);

                    if (radio.Checked)
                        lastCheckedCmd = radio.command;
                }
                if (lastCheckedCmd != null)
                    windowInjuryCommands[id] = lastCheckedCmd;
            }

            // -- COMMAND BUTTONS --
            XmlNodeList buttons = elem.GetElementsByTagName("cmdButton");
            if (buttons != null && buttons.Count > 0)
            {
                for (int i = window.formBody.Controls.Count - 1; i >= 0; i--)
                    if (window.formBody.Controls[i] is CmdButton)
                        window.formBody.Controls.RemoveAt(i);

                int cmdY = 240;
                int btnW = 80, btnH = 22, spacing = 6;
                for (int i = 0; i < buttons.Count; i++)
                {
                    XmlElement btnElem = (XmlElement)buttons[i];
                    CmdButton btn = new CmdButton();
                    btn.Name = btnElem.GetAttribute("id");
                    btn.Text = btnElem.GetAttribute("value");
                    btn.cmd_string = btnElem.GetAttribute("cmd");
                    btn.Size = new Size(btnW, btnH);

                    int col = i % 2;
                    btn.Location = new Point(10 + col * (btnW + spacing), cmdY);
                    btn.Click += delegate (object sender, EventArgs e)
                    {
                        CmdButton b = (CmdButton)sender;
                        plugin.ghost.SendText(b.cmd_string);
                    };
                    window.formBody.Controls.Add(btn);
                    if (col == 1) cmdY += btnH + 4;
                }
            }

            // -- WOUND MARKERS --
            XmlNodeList images = elem.GetElementsByTagName("image");
            if (images != null && images.Count > 0)
            {
                List<string> activeWoundTypes = GetActiveWoundTypes(id);
                panel.SuspendLayout();
                panel.Controls.Clear();
                foreach (XmlElement image in images)
                {
                    string part = image.GetAttribute("id");
                    string name = image.GetAttribute("name");
                    if (!activeWoundTypes.Contains(name)) continue;

                    Point location = Point.Empty;
                    string text = "";
                    Color backColor = Color.Transparent;
                    bool valid = false;

                    if (name == "Injury1" && injuryMarkerPositions.TryGetValue(part, out location)) { text = "1"; backColor = Color.Yellow; valid = true; }
                    else if (name == "Injury2" && injuryMarkerPositions.TryGetValue(part, out location)) { text = "2"; backColor = Color.Orange; valid = true; }
                    else if (name == "Injury3" && injuryMarkerPositions.TryGetValue(part, out location)) { text = "3"; backColor = Color.Red; valid = true; }
                    else if (name == "Scar1" && scarMarkerPositions.TryGetValue(part, out location)) { text = "1"; backColor = Color.Yellow; valid = true; }
                    else if (name == "Scar2" && scarMarkerPositions.TryGetValue(part, out location)) { text = "2"; backColor = Color.Orange; valid = true; }
                    else if (name == "Scar3" && scarMarkerPositions.TryGetValue(part, out location)) { text = "3"; backColor = Color.Red; valid = true; }
                    else if (name == "Nsys1" && injuryMarkerPositions.TryGetValue(part, out location)) { text = "1"; backColor = Color.Yellow; valid = true; }
                    else if (name == "Nsys2" && injuryMarkerPositions.TryGetValue(part, out location)) { text = "2"; backColor = Color.Orange; valid = true; }
                    else if (name == "Nsys3" && injuryMarkerPositions.TryGetValue(part, out location)) { text = "3"; backColor = Color.Red; valid = true; }

                    if (valid)
                    {
                        Label marker = new Label();
                        marker.Size = new Size(15, 15);
                        marker.TextAlign = ContentAlignment.MiddleCenter;
                        marker.ForeColor = text == "3" ? Color.White : Color.Black;
                        marker.BackColor = backColor;
                        marker.BorderStyle = BorderStyle.FixedSingle;
                        marker.Text = text;
                        marker.Location = location;
                        string tooltipText = image.HasAttribute("tooltip") ? image.GetAttribute("tooltip") : null;
                        string cmdText = image.HasAttribute("cmd") ? image.GetAttribute("cmd") : null;

                        if (!string.IsNullOrEmpty(tooltipText))
                            woundTip.SetToolTip(marker, tooltipText);
                        if (!string.IsNullOrEmpty(cmdText))
                        {
                            marker.Cursor = Cursors.Hand;
                            marker.Click += delegate (object sender, EventArgs e)
                            {
                                plugin.ghost.SendText(cmdText);
                            };
                        }
                        panel.Controls.Add(marker);
                    }
                }
                panel.ResumeLayout();
                panel.Invalidate();
                panel.Refresh();
            }
            try
            {
                // -- HEALTH BAR --
                XmlNodeList bars = elem.GetElementsByTagName("progressBar");
                if (bars != null && bars.Count > 0)
                {
                    for (int i = window.formBody.Controls.Count - 1; i >= 0; i--)
                        if (window.formBody.Controls[i] is ProgressBar || window.formBody.Controls[i] is OthersHealthBar)
                            window.formBody.Controls.RemoveAt(i);

                    XmlElement barElem = (XmlElement)bars[0];
                    int value = 0;
                    int.TryParse(barElem.GetAttribute("value"), out value);
                    OthersHealthBar bar = new OthersHealthBar();
                    bar.Name = "health_" + id;
                    bar.Location = new Point(10, 215);
                    bar.Size = new Size(180, 18);
                    bar.Value = Math.Min(bar.Maximum, Math.Max(bar.Minimum, value));
                    window.formBody.Controls.Add(bar);
                }
            }
            catch (Exception ex)
            {
                plugin.ghost.EchoText("FATAL: Exception in Update for " + id + " : " + ex);
            }
        }



        private List<string> GetActiveWoundTypes(string id)
        {
            string command;
            if (!windowInjuryCommands.TryGetValue(id, out command))
                command = "_injury 0 -1";
            if (command.Contains("_injury 0")) return new List<string> { "Injury1", "Injury2", "Injury3" };
            if (command.Contains("_injury 1")) return new List<string> { "Scar1", "Scar2", "Scar3" };
            if (command.Contains("_injury 2")) return new List<string> { "Injury1", "Injury2", "Injury3", "Injury4", "Injury5", "Scar1", "Scar2", "Scar3" };
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
            this.SetStyle(ControlStyles.UserPaint, true);
            this.ForeColor = Color.White;
            this.BackColor = Color.DarkRed;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rec = e.ClipRectangle;
            rec.Width = (int)(rec.Width * ((double)this.Value / this.Maximum));
            if (rec.Width == 0) rec.Width = 1;

            using (SolidBrush brush = new SolidBrush(Color.Red))
            {
                e.Graphics.FillRectangle(brush, rec);
            }

            string text = "HEALTH " + this.Value + "%";
            SizeF len = e.Graphics.MeasureString(text, this.Font);
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                e.Graphics.DrawString(text, this.Font, textBrush,
                    new PointF((this.Width - len.Width) / 2, (this.Height - len.Height) / 2));
            }

            base.OnPaint(e);
        }
    }
}
