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
            { "chest",    new Point(45,  48) },
            { "abdomen",  new Point(50,  68) },
            { "back",     new Point(45,  88) },

            { "leftArm",  new Point( 4,  48) },
            { "rightArm", new Point(84,  48) },
            { "leftHand", new Point(0,   82) },
            { "rightHand",new Point(92,  82) },

            { "leftLeg",  new Point(30, 118) },
            { "rightLeg", new Point(70, 118) },
            { "leftFoot", new Point(28, 158) },
            { "rightFoot",new Point(72, 158) },

            { "leftEye",  new Point(4,  2) },
            { "rightEye", new Point(84,  2) },
            { "nsys",     new Point(50, 100) }
        };


        private readonly Dictionary<string, Point> scarMarkerPositions = new Dictionary<string, Point>
        {
            { "head",     new Point(54,  10) },
            { "neck",     new Point(54,  28) },
            { "chest",    new Point(49,  48) },
            { "abdomen",  new Point(54,  68) },
            { "back",     new Point(49,  88) },

            { "leftArm",  new Point(8,  48) },
            { "rightArm", new Point(88,  48) },
            { "leftHand", new Point(4,   78) },
            { "rightHand",new Point(96,  78) },

            { "leftLeg",  new Point(34, 118) },
            { "rightLeg", new Point(74, 118) },
            { "leftFoot", new Point(32, 158) },
            { "rightFoot",new Point(76, 158) },

            { "leftEye",  new Point(8,  2) },
            { "rightEye", new Point(88,  2) },
            { "nsys",     new Point(54, 100) }
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
            switch (currentInjuryCommand)
            {
                case "_injury 0 -1": return new List<string> { "Injury1", "Injury2" }; // E Wound
                case "_injury 1 -1": return new List<string> { "Scar1", "Scar2" };     // E Scar
                case "_injury 2 -1": return new List<string> { "Injury1", "Injury2", "Scar1", "Scar2" }; // E Both
                case "_injury 3 -1": return new List<string> { "Injury1", "Injury2" }; // I Wound
                case "_injury 4 -1": return new List<string> { "Scar1", "Scar2" };     // I Scar
                case "_injury 5 -1": return new List<string> { "Injury1", "Injury2", "Scar1", "Scar2" }; // I Both
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
                    if (activeWoundTypes.Contains(woundName))
                    {
                        foundMatch = true;
                    }
                }

                if (!foundMatch)
                {
                    injurySilhouettePanel.Controls.Clear();
                    injurySilhouettePanel.Invalidate();
                    injurySilhouettePanel.Refresh();
                }
                else
                {
                    // Remove existing matching markers
                    List<Control> toRemove = new List<Control>();
                    foreach (Control ctrl in injurySilhouettePanel.Controls)
                    {
                        if (!(ctrl is Label)) continue;

                        string text = ctrl.Text;
                        if ((text == "1" && activeWoundTypes.Contains("Injury1")) ||
                            (text == "2" && activeWoundTypes.Contains("Injury2")) ||
                            (text == "1" && activeWoundTypes.Contains("Scar1")) ||
                            (text == "2" && activeWoundTypes.Contains("Scar2")))
                        {
                            toRemove.Add(ctrl);
                        }
                    }
                    foreach (Control ctrl in toRemove)
                        injurySilhouettePanel.Controls.Remove(ctrl);

                    // Add new wound markers
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

                        if ((name == "Injury1" || name == "Injury2") && injuryMarkerPositions.TryGetValue(part, out location))
                        {
                            text = name == "Injury1" ? "1" : "2";
                            backColor = name == "Injury1" ? Color.Coral : Color.Red;
                            valid = true;
                        }
                        else if ((name == "Scar1" || name == "Scar2") && scarMarkerPositions.TryGetValue(part, out location))
                        {
                            location.X += 16;
                            text = name == "Scar1" ? "1" : "2";
                            backColor = name == "Scar1" ? Color.Orange : Color.DarkOrange;
                            valid = true;
                        }

                        if (valid)
                        {
                            Label marker = new Label
                            {
                                Size = new Size(15, 15),
                                TextAlign = ContentAlignment.MiddleCenter,
                                ForeColor = Color.White,
                                BackColor = backColor,
                                BorderStyle = BorderStyle.FixedSingle,
                                Text = text,
                                Location = location
                            };
                            injurySilhouettePanel.Controls.Add(marker);
                        }
                    }

                    injurySilhouettePanel.Invalidate();
                    injurySilhouettePanel.Refresh();
                }
            }

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
