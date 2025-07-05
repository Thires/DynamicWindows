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
        private Dictionary<string, HashSet<string>> woundData = new Dictionary<string, HashSet<string>>();

        private readonly Dictionary<string, Point> injuryMarkerPositions = new Dictionary<string, Point>
        {
            { "head",     new Point(50,  10) },
            { "neck",     new Point(50,  28) },
            { "chest",    new Point(50,  48) },
            { "abdomen",  new Point(50,  68) },
            { "back",     new Point(50,  88) },

            { "leftArm",  new Point( 4,  48) },
            { "rightArm", new Point(84,  48) },
            { "leftHand", new Point(0,   78) },
            { "rightHand",new Point(92,  78) },

            { "leftLeg",  new Point(30, 118) },
            { "rightLeg", new Point(70, 118) },
            { "leftFoot", new Point(28, 158) },
            { "rightFoot",new Point(72, 158) },

            { "leftEye",  new Point(36,  2) },
            { "rightEye", new Point(64,  2) },
            { "nsys",     new Point(50, 100) }
        };


        private readonly Dictionary<string, Point> scarMarkerPositions = new Dictionary<string, Point>
        {
            { "head",     new Point(50,  10) },
            { "neck",     new Point(50,  28) },
            { "chest",    new Point(50,  48) },
            { "abdomen",  new Point(50,  68) },
            { "back",     new Point(50,  88) },

            { "leftArm",  new Point( 4,  48) },
            { "rightArm", new Point(84,  48) },
            { "leftHand", new Point(0,   78) },
            { "rightHand",new Point(92,  78) },

            { "leftLeg",  new Point(30, 118) },
            { "rightLeg", new Point(70, 118) },
            { "leftFoot", new Point(28, 158) },
            { "rightFoot",new Point(72, 158) },

            { "leftEye",  new Point(36,  2) },
            { "rightEye", new Point(64,  2) },
            { "nsys",     new Point(50, 100) }
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
            //window.ClientSize = new Size(190, 210);
            window.ClientSize = new Size(230, 260);
            window.StartPosition = FormStartPosition.Manual;
            window.Location = plugin.positionList.ContainsKey("injuries")
                ? plugin.positionList["injuries"]
                : new Point(100, 100);
            window.TopMost = true;

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
            if (currentInjuryCommand == "_injury 0 -1") return new List<string> { "Injury1", "Injury2" };
            if (currentInjuryCommand == "_injury 1 -1") return new List<string> { "Scar1", "Scar2" };
            if (currentInjuryCommand == "_injury 2 -1") return new List<string> { "Injury1", "Injury2", "Scar1", "Scar2" };
            if (currentInjuryCommand == "_injury 3 -1") return new List<string> { "Injury1", "Injury2" };
            if (currentInjuryCommand == "_injury 4 -1") return new List<string> { "Scar1", "Scar2" };
            if (currentInjuryCommand == "_injury 5 -1") return new List<string> { "Injury1", "Injury2", "Scar1", "Scar2" };

            return new List<string>();
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

            // Clear all wound data unless using "Both" modes
            if (elem.HasAttribute("clear") && elem.GetAttribute("clear") == "t")
            {
                if (currentInjuryCommand != "_injury 2 -1" && currentInjuryCommand != "_injury 5 -1")
                {
                    woundData.Clear();
                }
            }

            // Store all wound info
            foreach (XmlElement image in elem.GetElementsByTagName("image"))
            {
                string part = image.GetAttribute("id");
                string name = image.GetAttribute("name");

                if (!woundData.ContainsKey(part))
                {
                    woundData[part] = new HashSet<string>();
                }

                woundData[part].Add(name);
            }

            // Redraw markers only for current mode
            List<string> activeWoundTypes = GetActiveWoundTypes();
            injurySilhouettePanel.Controls.Clear();

            foreach (var kvp in woundData)
            {
                string part = kvp.Key;
                HashSet<string> wounds = kvp.Value;

                foreach (string wound in wounds)
                {
                    if (!activeWoundTypes.Contains(wound))
                        continue;

                    Point location = new Point(0, 0);
                    Color backColor = Color.Transparent;
                    string text = "";
                    bool valid = false;

                    if (wound == "Injury1" && injuryMarkerPositions.TryGetValue(part, out location))
                    {
                        text = "1"; backColor = Color.Coral; valid = true;
                    }
                    else if (wound == "Injury2" && injuryMarkerPositions.TryGetValue(part, out location))
                    {
                        text = "2"; backColor = Color.Red; valid = true;
                    }
                    else if (wound == "Scar1" && scarMarkerPositions.TryGetValue(part, out location))
                    {
                        text = "1"; backColor = Color.Orange; valid = true;
                    }
                    else if (wound == "Scar2" && scarMarkerPositions.TryGetValue(part, out location))
                    {
                        text = "2"; backColor = Color.DarkOrange; valid = true;
                    }

                    if (valid)
                    {
                        // Offset scars to right
                        if (wound.StartsWith("Scar"))
                            location.X += 16;

                        Label marker = new Label();
                        marker.Size = new Size(15, 15);
                        marker.TextAlign = ContentAlignment.MiddleCenter;
                        marker.ForeColor = Color.White;
                        marker.BackColor = backColor;
                        marker.BorderStyle = BorderStyle.FixedSingle;
                        marker.Text = text;
                        marker.Location = location;

                        injurySilhouettePanel.Controls.Add(marker);
                    }
                }
            }

            // Health bar updates
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

            injurySilhouettePanel.Invalidate();
            injurySilhouettePanel.Refresh();
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
