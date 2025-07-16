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
        private Panel injurySilhouettePanel;
        internal static string currentInjuryCommand = "_injury 0 -1";

        private ToolTip woundTip = new ToolTip();
        //private Dictionary<string, HashSet<string>> woundData = new Dictionary<string, HashSet<string>>();

        private readonly Dictionary<string, Point> injuryMarkerPositions = new Dictionary<string, Point>
        {
            { "head",     new Point(52,  10) },
            { "neck",     new Point(52,  28) },
            { "chest",    new Point(52,  48) },
            { "abdomen",  new Point(52,  68) },
            { "back",     new Point(102,  48) },

            { "leftArm",  new Point(25,  48) },
            { "rightArm", new Point(80,  48) },
            { "leftHand", new Point(8,   92) },
            { "rightHand",new Point(92,  92) },

            { "leftLeg",  new Point(34, 125) },
            { "rightLeg", new Point(70, 125) },

            { "leftEye",  new Point(4,  2) },
            { "rightEye", new Point(92,  2) },
            { "nsys",     new Point(4, 48) }
        };


        private readonly Dictionary<string, Point> scarMarkerPositions = new Dictionary<string, Point>
        {
            { "head",     new Point(52,  10) },
            { "neck",     new Point(52,  28) },
            { "chest",    new Point(52,  48) },
            { "abdomen",  new Point(52,  68) },
            { "back",     new Point(102,  48) },

            { "leftArm",  new Point(25,  48) },
            { "rightArm", new Point(80,  48) },
            { "leftHand", new Point(8,   92) },
            { "rightHand",new Point(92,  92) },

            { "leftLeg",  new Point(34, 125) },
            { "rightLeg", new Point(70, 125) },

            { "leftEye",  new Point(4,  2) },
            { "rightEye", new Point(92,  2) },
            { "nsys",     new Point(4, 48) }
        };

        public InjuriesWindow(Plugin plugin)
        {
            this.plugin = plugin;
        }

        public void Create(XmlElement elem)
        {
            SkinnedMDIChild existing = null;
            foreach (SkinnedMDIChild f in plugin.forms)
            {
                if (f.Name == "injuries")
                {
                    existing = f;
                    break;
                }
            }

            if (existing != null)
            {
                plugin.forms.Remove(existing);
                existing.Close();
            }

            SkinnedMDIChild window = new SkinnedMDIChild(plugin.ghost, plugin);
            window.MdiParent = plugin.pForm;
            window.Text = "Injuries";
            window.Name = "injuries";
            window.ClientSize = new Size(230, 260);
            window.StartPosition = FormStartPosition.Manual;
            window.Location = plugin.positionList.ContainsKey("injuries")
                ? plugin.positionList["injuries"]
                : new Point(100, 100);

            window.LocationChanged += delegate
            {
                plugin.positionList["injuries"] = window.Location;
            };

            window.formBody.BackColor = plugin.formback;
            window.formBody.ForeColor = plugin.formfore;
            window.formBody.AutoScroll = false;

            injurySilhouettePanel = new Panel();
            injurySilhouettePanel.Name = "injurySilhouette";
            injurySilhouettePanel.Size = new Size(120, 200); // was 90x150
            injurySilhouettePanel.Location = new Point(10, 10); // more padding
            injurySilhouettePanel.BackColor = Color.Transparent;
            injurySilhouettePanel.BackgroundImage = Properties.Resources.body_image;
            injurySilhouettePanel.BackgroundImageLayout = ImageLayout.Zoom;
            window.formBody.Controls.Add(injurySilhouettePanel);

            string[] radioLabels = { "E Wound", "I Wound", "E Scar", "I Scar", "E Both", "I Both" };
            string[] radioCommands = { "_injury 0 -1", "_injury 3 -1", "_injury 1 -1", "_injury 4 -1", "_injury 2 -1", "_injury 5 -1" };
            int y = 10;
            for (int i = 0; i < radioLabels.Length; i++)
            {
                cbRadio radio = new cbRadio();
                radio.Name = "injr_" + i;
                radio.Text = radioLabels[i];
                radio.group = "injureMode";
                radio.command = radioCommands[i];
                radio.Checked = (radioCommands[i] == currentInjuryCommand);
                radio.Location = new Point(140, y);
                radio.Size = new Size(100, 20);

                radio.CheckedChanged += delegate (object sender, EventArgs e)
                {
                    cbRadio r = (cbRadio)sender;
                    if (r.Checked)
                    {
                        currentInjuryCommand = r.command;
                        plugin.ghost.SendText(currentInjuryCommand);
                        window.BringToFront();
                    }
                };

                window.formBody.Controls.Add(radio);
                y += 20;
            }

            HealthBar healthBar = new HealthBar();
            healthBar.Name = "health2";
            healthBar.Value = 100;
            healthBar.Location = new Point(10, 215);
            healthBar.Size = new Size(180, 18);
            window.formBody.Controls.Add(healthBar);

            plugin.forms.Add(window);
            window.ShowForm();
        }

        private List<string> GetActiveWoundTypes()
        {
            switch (currentInjuryCommand)
            {
                case "_injury 0 -1": return new List<string> { "Injury1", "Injury2", "Injury3" }; // E Wound
                case "_injury 1 -1": return new List<string> { "Scar1", "Scar2", "Scar3" };     // E Scar
                case "_injury 2 -1": return new List<string> { "Injury1", "Injury2", "Injury3", "Injury4", "Injury5", "Scar1", "Scar2", "Scar3" }; // E Both
                case "_injury 3 -1": return new List<string> { "Injury1", "Injury2", "Injury3", "Injury4", "Injury5", "Nsys1", "Nsys2", "Nsys3" }; // I Wound
                case "_injury 4 -1": return new List<string> { "Scar1", "Scar2", "Scar3", "Nsys1", "Nsys2", "Nsys3" };      // I Scar
                case "_injury 5 -1": return new List<string> { "Injury1", "Injury2", "Injury3", "Scar1", "Scar2", "Scar3", "Nsys1", "Nsys2", "Nsys3" }; // I Both
            }
            return new List<string>(); // Default to nothing
        }

        public void Update(XmlElement elem)
        {
            SkinnedMDIChild window = null;
            foreach (SkinnedMDIChild f in plugin.forms)
            {
                if (f.Name == "injuries")
                {
                    window = f;
                    break;
                }
            }
            if (window == null) return;

            if (injurySilhouettePanel == null && window.formBody.Controls.ContainsKey("injurySilhouette"))
                injurySilhouettePanel = (Panel)window.formBody.Controls["injurySilhouette"];

            injurySilhouettePanel.GetType()
    .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
    ?.SetValue(injurySilhouettePanel, true, null);

            List<string> activeWoundTypes = GetActiveWoundTypes();
            XmlNodeList images = elem.GetElementsByTagName("image");

            if (images.Count == 0)
            {
                //just here
            }
            else
            {
                bool foundMatch = false;

                foreach (XmlElement image in images)
                {
                    string woundName = image.GetAttribute("name");
                    string part = image.GetAttribute("id");
                    //string severity = "0";
                    string prefix = GetInjuryPrefix();

                    if (activeWoundTypes.Contains(woundName))
                    {
                        foundMatch = true;
                    }

                    // to make vars
                    //if (part == woundName)
                    //    severity = "0";
                    //else
                    //    severity = woundName.Substring(woundName.Length - 1);

                    //plugin.ghost.set_Variable($"Injuries.{prefix}.{part}", severity);
                    //plugin.ghost.EchoText($"Injuries.{prefix}.{part} " + severity);

                }

                injurySilhouettePanel.SuspendLayout();

                if (!foundMatch)
                {
                    injurySilhouettePanel.Controls.Clear();
                    injurySilhouettePanel.Invalidate();
                    injurySilhouettePanel.Refresh();
                }
                else
                {
                    // Remove old wound markers
                    List<Control> toRemove = new List<Control>();
                    foreach (Control ctrl in injurySilhouettePanel.Controls)
                    {
                        if (!(ctrl is Label)) continue;

                        string text = ctrl.Text;

                        // Remove any marker that matches a wound type
                        if ((text == "1" && (activeWoundTypes.Contains("Injury1") || activeWoundTypes.Contains("Scar1") || activeWoundTypes.Contains("Nsys1"))) ||
                            (text == "2" && (activeWoundTypes.Contains("Injury2") || activeWoundTypes.Contains("Scar2") || activeWoundTypes.Contains("Nsys2"))) ||
                            (text == "3" && (activeWoundTypes.Contains("Injury3") || activeWoundTypes.Contains("Scar3") || activeWoundTypes.Contains("Nsys3"))))
                        {
                            toRemove.Add(ctrl);
                        }
                    }

                    foreach (Control ctrl in toRemove)
                    {
                        injurySilhouettePanel.Controls.Remove(ctrl);
                    }

                    foreach (XmlElement image in images)
                    {
                        string part = image.GetAttribute("id");
                        string name = image.GetAttribute("name");

                        if (!activeWoundTypes.Contains(name))
                            continue;

                        Point location = new Point(0, 0);
                        string text = "";
                        Color backColor = Color.Transparent;
                        bool valid = false;

                        // Injuries
                        if (name == "Injury1" && injuryMarkerPositions.TryGetValue(part, out location))
                        {
                            text = "1";
                            backColor = Color.Yellow;
                            valid = true;
                        }
                        else if (name == "Injury2" && injuryMarkerPositions.TryGetValue(part, out location))
                        {
                            text = "2";
                            backColor = Color.Orange;
                            valid = true;
                        }
                        else if (name == "Injury3" && injuryMarkerPositions.TryGetValue(part, out location))
                        {
                            text = "3";
                            backColor = Color.Red;
                            valid = true;
                        }

                        // Scars
                        else if (name == "Scar1" && scarMarkerPositions.TryGetValue(part, out location))
                        {
                            text = "1";
                            backColor = Color.Yellow;
                            valid = true;
                        }
                        else if (name == "Scar2" && scarMarkerPositions.TryGetValue(part, out location))
                        {
                            text = "2";
                            backColor = Color.Orange;
                            valid = true;
                        }
                        else if (name == "Scar3" && scarMarkerPositions.TryGetValue(part, out location))
                        {
                            text = "3";
                            backColor = Color.Red;
                            valid = true;
                        }

                        // Nerves
                        else if (name == "Nsys1" && injuryMarkerPositions.TryGetValue(part, out location))
                        {
                            text = "1";
                            backColor = Color.Yellow;
                            valid = true;
                        }
                        else if (name == "Nsys2" && injuryMarkerPositions.TryGetValue(part, out location))
                        {
                            text = "2";
                            backColor = Color.Orange;
                            valid = true;
                        }
                        else if (name == "Nsys3" && injuryMarkerPositions.TryGetValue(part, out location))
                        {
                            text = "3";
                            backColor = Color.Red;
                            valid = true;
                        }

                        if (valid)
                        {
                            Label marker = new Label();
                            marker.Size = new Size(15, 15);
                            marker.TextAlign = ContentAlignment.MiddleCenter;
                            marker.ForeColor = Color.White;
                            //marker.Font = new Font(marker.Font, FontStyle.Bold);
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

                            // Set text color based on severity
                            if (text == "1")
                                marker.ForeColor = Color.Black;
                            else if (text == "2")
                                marker.ForeColor = Color.Black;
                            else if (text == "3")
                                marker.ForeColor = Color.White;


                            injurySilhouettePanel.Controls.Add(marker);
                        }
                    }

                    injurySilhouettePanel.Invalidate();
                    injurySilhouettePanel.Refresh();
                }
            }

            injurySilhouettePanel.ResumeLayout();
            injurySilhouettePanel.Invalidate();

            // Health bar update — always runs
            foreach (XmlElement progress in elem.GetElementsByTagName("progressBar"))
            {
                string id = progress.GetAttribute("id");
                foreach (ProgressBar bar in window.formBody.Controls.OfType<ProgressBar>())
                {
                    if (bar.Name == id)
                    {
                        int value;
                        if (int.TryParse(progress.GetAttribute("value"), out value))
                        {
                            bar.Value = Math.Min(bar.Maximum, Math.Max(bar.Minimum, value));
                        }
                    }
                }
            }
        }

        private string GetInjuryPrefix()
        {
            switch (currentInjuryCommand)
            {
                case "_injury 0 -1": return "EWound";
                case "_injury 1 -1": return "EScar";
                case "_injury 2 -1": return "EBoth";
                case "_injury 3 -1": return "IWound";
                case "_injury 4 -1": return "IScar";
                case "_injury 5 -1": return "IBoth";
                default: return "Unknown";
            }
        }

        private class HealthBar : ProgressBar
        {
            public HealthBar()
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
}
