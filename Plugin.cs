using GeniePlugin.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.Layout;
using System.Xml;

namespace DynamicWindows
{
    public class Plugin : IPlugin
    {
        public ArrayList forms = new ArrayList();
        public Hashtable documents = new Hashtable();
        public Color formback = Color.Black;
        public Color formfore = Color.White;
        public bool bPluginEnabled = true;
        public ArrayList ignorelist = new ArrayList();
        public Dictionary<string, Point> positionList = new Dictionary<string, Point>();
        public bool bStowContainer;
        public Form pForm;
        public IHost ghost;
        private string configPath;
        private InjuriesWindow injuriesWindow;
        private InjuriesOthersWindow injuriesOthersWindow;
        private readonly Dictionary<string, InjuriesOthersWindow> injuryWindows = new Dictionary<string, InjuriesOthersWindow>();
        public bool bDisableOtherInjuries = true;
        public bool bDisableSelfInjuries = true;


        public LoadSave loadSave;
        private string lastConnectionStatus = "";
        public string characterName;

        public bool Enabled
        {
            get
            {
                return this.bPluginEnabled;
            }
            set
            {
                if (value)
                    this.bPluginEnabled = true;
                else
                    this.bPluginEnabled = false;
            }
        }

        public string Name => "Dynamic Windows";

        public string Version => "2.2.3";

        public string Author => "Multiple Developers";

        public string Description => "Displays content windows specified through the XML stream from the game.";

        public void Initialize(IHost Host)
        {
            try
            {
                this.ghost = Host;
                this.pForm = Host.ParentForm;
                this.configPath = Host.get_Variable("PluginPath");
                this.loadSave = new LoadSave(this, this.configPath, this.characterName);

                // Load the config
                this.loadSave.Load();
                this.injuriesWindow = new InjuriesWindow(this); 
                this.injuriesOthersWindow = new InjuriesOthersWindow(this);
            }
            catch (Exception ex)
            {
                Host.EchoText("[Plugin Error] Initialization failed: " + ex.Message);
            }
        }

        public string ParseInput(string Text)
        {
            if (Text.Equals("/debugwindows", StringComparison.OrdinalIgnoreCase))
            {
                this.ghost.EchoText("Form Count: " + this.forms.Count.ToString());
                foreach (Control control in this.forms)
                    this.ghost.EchoText("    Form: " + control.Name);
                foreach (string str in (IEnumerable)this.documents.Keys)
                    this.ghost.EchoText($"Variable: {str} - {this.documents[(object)str]}");
                return "";
            }

            if (Text.Equals("/injurieswindow", StringComparison.OrdinalIgnoreCase))
            {
                this.ghost.EchoText("Re-opening injuries window...");

                string id = "injuries";

                var match = this.ignorelist.Cast<string>()
                    .FirstOrDefault(x => x.Equals(id, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    this.ignorelist.Remove(match);
                }

                this.injuriesWindow.Create(null);
                //this.ghost.SendText("_injury 0 -1");
                return "";
            }

            if (Text.Equals("/toggleInjuries", StringComparison.OrdinalIgnoreCase))
            {
                this.bDisableSelfInjuries = !this.bDisableSelfInjuries;
                this.ghost.EchoText("[Plugin]: Other window is now " +
                    (this.bDisableSelfInjuries ? "disabled" : "enabled"));
                if (this.loadSave != null) this.loadSave.Save();
                return "";
            }

            if (Text.Equals("/toggleOtherInjuries", StringComparison.OrdinalIgnoreCase))
            {
                this.bDisableOtherInjuries = !this.bDisableOtherInjuries;
                this.ghost.EchoText("[Plugin]: Other injuries windows are now " +
                    (this.bDisableOtherInjuries ? "disabled" : "enabled"));
                if (this.loadSave != null) this.loadSave.Save();
                return "";
            }

            if (Text.Equals("/injurieshelp", StringComparison.OrdinalIgnoreCase) || Text.Equals("/injurieswindowshelp", StringComparison.OrdinalIgnoreCase) || Text.Equals("/dynamicwindows help", StringComparison.OrdinalIgnoreCase))
            {
                this.ghost.EchoText("Dynamic Windows Injuries Options Help:");
                this.ghost.EchoText("  /injurieswindow         - Re-opens your self Injuries window if closed or lost.");
                this.ghost.EchoText("  /toggleInjuries         - Enables or disables showing your own Injuries window.");
                this.ghost.EchoText("  /toggleOtherInjuries    - Enables or disables all 'other injuries' windows for other players.");
                this.ghost.EchoText("  /debugwindows           - Displays the number of open windows and their names.");
                this.ghost.EchoText("  (All commands are case-insensitive and can be used at any time.)");
                return "";
            }

            return Text;
        }

        public string ParseText(string Text, string Window)
        {
            if (Window.Trim().ToLower() == "main" || Window.Trim() == string.Empty)
                return this.ParseText(Text);
            else
                return Text;
        }

        public string ParseText(string Text)
        {
            return Text;
        }

        public void ParseXML(string XML)
        {
            if (!this.bPluginEnabled)
                return;

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml("<?xml version='1.0'?><root>" + XML + "</root>");
                foreach (XmlElement xmlElement in xmlDocument.DocumentElement.ChildNodes)
                {
                    //string id = xmlElement.GetAttribute("id")?.Trim();
                    string id = xmlElement.GetAttribute("id");

                    // Skip any ignored windows globally
                    if (!string.IsNullOrEmpty(id) &&
                        this.ignorelist.Cast<string>().Any(x => x.Equals(id, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    switch (xmlElement.Name)
                    {
                    case "exposeStream":
                        this.Parse_xml_exposestream(xmlElement);
                        continue;
                    case "pushStream":
                        this.Parse_xml_pushstream(xmlElement);
                        continue;
                    case "popStream":
                        this.Parse_xml_popStream(xmlElement);
                        continue;
                    case "streamWindow":
                    this.Parse_xml_streamwindow(xmlElement);
                        continue;
                    case "openDialog":
                            if (id.StartsWith("injuries-"))
                            {
                                if (this.bDisableOtherInjuries)
                                    break;
                                string title = xmlElement.GetAttribute("title");
                                injuriesOthersWindow.Create(id, title);
                                break;
                            }

                            if (id == "injuries" && this.injuriesWindow != null)
                            {
                                if (this.bDisableSelfInjuries)
                                    break;
                                this.injuriesWindow.Create(xmlElement);
                                continue;
                            }
                            this.Parse_xml_openwindow(xmlElement);
                        continue;
                        case "dialogData":
                            if (id.StartsWith("injuries-"))
                            {
                                if (this.bDisableOtherInjuries)
                                    break;
                                injuriesOthersWindow.Update(id, xmlElement);
                                break;
                            }
                            
                            if (id == "injuries")
                            {
                                if (this.bDisableSelfInjuries)
                                    break;
                                injuriesWindow.Update(xmlElement);
                                continue;
                            }
                            this.Parse_xml_updatewindow(xmlElement);
                            continue;
                    case "closeDialog":
                        this.Parse_xml_closewindow(xmlElement);
                        continue;
                    case "exposeDialog":
                        this.Parse_xml_exposewindow(xmlElement);
                        continue;
                    case "dynaStream":
                        this.Parse_set_stream(xmlElement);
                        continue;
                    case "clearDynaStream":
                        Parse_clear_stream(xmlElement);
                        continue;
                    case "clearStream":
                        this.Parse_clear_stream(xmlElement);
                        continue;
                    case "clearContainer":
                        this.Parse_container(xmlElement);
                        continue;
                    case "inv":
                        this.Parse_inventory(xmlElement);
                        continue;
                    default:
                        continue;
                }
            }
        }
            catch (Exception ex)
            {
                this.ghost.EchoText("Error parsing XML: " + ex.Message);
            }
        }

        public void Show()
        {
            FormOptionWindow formOptionWindow = new FormOptionWindow(this)
            {
                MdiParent = this.pForm,
                TopMost = true
            };
            ((Control)formOptionWindow).Show();
        }

        public void VariableChanged(string variable)
        {
            string connected = this.ghost.get_Variable("connected");

            if (connected != lastConnectionStatus)
            {
                lastConnectionStatus = connected;

                if (connected == "0") // Disconnected
                {
                    this.loadSave.Save();

                    foreach (SkinnedMDIChild window in this.forms.Cast<SkinnedMDIChild>().ToList())
                        window.Close();

                    this.forms.Clear();
                }
                else if (connected == "1") // Reconnected
                {
                    this.characterName = this.ghost.get_Variable("charactername");
                    this.loadSave = new LoadSave(this, this.configPath, this.characterName);
                    this.loadSave.Load();

                    if (!this.loadSave.IsIgnored("injuries"))
                    {
                        this.ghost.SendText(InjuriesWindow.currentInjuryCommand);
                    }
                }
            }

            // Optional: still respond to charactername specifically if needed
            if (variable.Equals("charactername", StringComparison.OrdinalIgnoreCase))
            {
                // Possibly redundant now
                this.loadSave.Load();
            }
        }

        public void ParentClosing()
        {
            // Update positions of all windows before closing
            foreach (SkinnedMDIChild window in this.forms)
            {
                this.positionList[window.Name] = window.Location;
            }

            // Save character-specific settings
            this.loadSave.Save();

            // Close all open windows (including injuries)
            foreach (SkinnedMDIChild window in this.forms.Cast<SkinnedMDIChild>().ToList())
            {
                window.Close();
            }

            this.forms.Clear();
        }

        private void Parse_xml_streamwindow(XmlElement elem)
        {
            // Check if the id attribute is "profileHelp"
            if (elem.GetAttribute("id") != "profileHelp")
                return;

            // Use default values for the width and height if they are not specified in the element
            int width = elem.HasAttribute("width") ? int.Parse(elem.GetAttribute("width")) : 375;
            int height = elem.HasAttribute("height") ? int.Parse(elem.GetAttribute("height")) : 350;

            // Check if the stream window is already open
            SkinnedMDIChild existingWindow = null;
            foreach (SkinnedMDIChild window in this.forms)
            {
                if (window.Name == elem.GetAttribute("id"))
                {
                    existingWindow = window;
                    break;
                }
            }

            if (existingWindow != null)
            {
                // Close the existing window
                this.forms.Remove(existingWindow);
                existingWindow.Close();
            }

            // Create a new SkinnedMDIChild object for the stream window
            SkinnedMDIChild streamWindow = new SkinnedMDIChild(this.ghost, this)
            {
                MdiParent = this.pForm,
                Text = elem.GetAttribute("title"),
                ForeColor = this.formfore
            };
            streamWindow.formBody.ForeColor = this.formfore;
            streamWindow.Name = elem.GetAttribute("id");
            streamWindow.ClientSize = new Size(width, height + 22);
            streamWindow.FormClosed += (s, e) => this.loadSave.Save();

            if (this.positionList.ContainsKey(elem.GetAttribute("id")))
            {
                streamWindow.StartPosition = FormStartPosition.Manual;
                streamWindow.Location = this.positionList[elem.GetAttribute("id")];
            }
            else
            {
                streamWindow.StartPosition = FormStartPosition.CenterScreen;
            }

            // Show the stream window
            streamWindow.formBody.Visible = true;
            streamWindow.formBody.AutoScroll = true;
            streamWindow.formBody.AutoSize = true;
            if (elem.HasAttribute("resident") && elem.GetAttribute("resident").Equals("false") && !elem.GetAttribute("location").Equals("center"))
                return;

            // Create a new instance of the HelpWindows class
            HelpWindows helpWindows = new HelpWindows();

            // Create a new RichTextBox control
            RichTextBox contentBox = new RichTextBox
            {
                ForeColor = this.formfore,
                BackColor = this.formback,
                ReadOnly = true,
                BorderStyle = BorderStyle.None
            };
            if (streamWindow.Text == "Profile RP Help")
                contentBox.Text = helpWindows.RPHelp;
            if (streamWindow.Text == "Profile PVP Help")
                contentBox.Text = helpWindows.PVPHelp;
            if (streamWindow.Text == "Profile SPOUSE Help")
                contentBox.Text = helpWindows.SPOUSEHelp;
            // this removes the window in genie that these create
            this.ghost.SendText("#window remove profileHelp");
            contentBox.Dock = DockStyle.Fill;

            // Add the RichTextBox control to the formBody property of the streamWindow object
            streamWindow.formBody.Controls.Add(contentBox);
            streamWindow.ShowForm();
        }

        private SkinnedMDIChild FindWindowByName(string name)
        {
            foreach (SkinnedMDIChild window in this.forms)
            {
                if (window.Name == name && !window.IsDisposed)
                    return window;
            }
            return null;
        }

        // Find labels by name
        private SkinnedMDIChild FindLabelByName(string name)
        {
            // Search for the window in the forms list
            foreach (SkinnedMDIChild label in this.forms)
            {
                if (label.Name == name)
                {
                    // Return the window if it is found
                    return label;
                }
            }

            // Return null if the window is not found
            return null;
        }

        private void Parse_xml_exposestream(XmlElement elem)
        {
            var streamWindow = FindWindowByName(elem.GetAttribute("id"));
            streamWindow?.Show();
        }

        private void Parse_xml_pushstream(XmlElement elem)
        {
            var streamWindow = FindWindowByName(elem.GetAttribute("id"));
            if (streamWindow == null) return;

            foreach (Control control in streamWindow.formBody.Controls)
            {
                if (control is RichTextBox rtb && control.Name == elem.GetAttribute("id"))
                {
                    rtb.AppendText(elem.InnerText + Environment.NewLine);
                    return;
                }
            }
        }

        private void Parse_xml_popStream(XmlElement elem)
        {
            var streamWindow = FindWindowByName(elem.GetAttribute("id"));
            if (streamWindow == null) return;

            foreach (Control control in streamWindow.formBody.Controls)
            {
                if (control is RichTextBox rtb && control.Name == elem.GetAttribute("id"))
                {
                    rtb.Clear();
                    return;
                }
            }
        }

        private void Parse_xml_exposewindow(XmlElement elem)
        {
            foreach (SkinnedMDIChild skinnedMdiChild in this.forms)
            {
                if (skinnedMdiChild.Name.Equals(elem.GetAttribute("id")))
                {
                    skinnedMdiChild.TopMost = true;
                    skinnedMdiChild.Update();
                    skinnedMdiChild.ShowForm();
                }
            }
        }

        private void Parse_xml_closewindow(XmlElement elem)
        {
            SkinnedMDIChild skinnedMdiChild1 = (SkinnedMDIChild)null;
            foreach (SkinnedMDIChild skinnedMdiChild2 in this.forms)
            {
                if (skinnedMdiChild2.Name.Equals(elem.GetAttribute("id")))
                    skinnedMdiChild1 = skinnedMdiChild2;
            }
            if (skinnedMdiChild1 == null)
                return;
            this.forms.Remove((object)skinnedMdiChild1);
            skinnedMdiChild1.Close();
        }

        private void Parse_xml_updatewindow(XmlElement xelem)
        {
            SkinnedMDIChild dyndialog = (SkinnedMDIChild)null;
            foreach (SkinnedMDIChild skinnedMdiChild in this.forms)
            {
                if (skinnedMdiChild.Name.Equals(xelem.GetAttribute("id")))
                    dyndialog = skinnedMdiChild;
            }
            if (dyndialog == null)
                return;

            dyndialog.formBody.Visible = false;
            foreach (XmlElement cbx in xelem.ChildNodes)
            {
                switch (cbx.Name)
                {
                    case "label":
                        this.Parse_labels(cbx, dyndialog);
                        continue;
                    case "cmdButton":
                        this.Parse_command_buttons(cbx, dyndialog);
                        continue;
                    case "closeButton":
                        this.Parse_close_button(cbx, dyndialog);
                        continue;
                    case "checkBox":
                        this.Parse_check_box(cbx, dyndialog);
                        continue;
                    case "radio":
                        this.Parse_radio_button(cbx, dyndialog);
                        continue;
                    case "streamBox":
                        this.Parse_stream_box(cbx, dyndialog);
                        continue;
                    case "dropDownBox":
                        this.Parse_drop_down(cbx, dyndialog);
                        continue;
                    case "upDownEditBox":
                        this.Parse_numericupdown(cbx, dyndialog);
                        continue;
                    case "editBox":
                        this.Parse_edit_box(cbx, dyndialog);
                        continue;
                    case "progressBar":
                        this.Parse_progress_bar(cbx, dyndialog);
                        continue;
                    default:
                        continue;
                }
            }
            dyndialog.formBody.Visible = true;
            dyndialog.formBody.AutoScroll = true;
            dyndialog.formBody.AutoSize = true;
            dyndialog.TopMost = true;
            dyndialog.Update();
            dyndialog.ShowForm();
        }

        public void Parse_xml_openwindow(XmlElement xelem)
        {
            if (!xelem.GetAttribute("type").Equals("dynamic") || !xelem.HasAttribute("width") || !xelem.HasAttribute("height"))
                return;
            SkinnedMDIChild skinnedMdiChild1 = (SkinnedMDIChild)null;
            
            if (this.loadSave.IsIgnored(xelem.GetAttribute("id")))
                return;
            
            foreach (SkinnedMDIChild skinnedMdiChild2 in this.forms)
            {
                if (skinnedMdiChild2.Name.Equals(xelem.GetAttribute("id")))
                    skinnedMdiChild1 = skinnedMdiChild2;
            }
            
            if (skinnedMdiChild1 != null)
            {
                this.forms.Remove((object)skinnedMdiChild1);
                skinnedMdiChild1.Close();
            }

            SkinnedMDIChild dyndialog = new SkinnedMDIChild(this.ghost, this)
            {
                MdiParent = this.pForm,
                Text = xelem.GetAttribute("title")
            };
            dyndialog.formBody.ForeColor = this.formfore;
            dyndialog.formBody.BackColor = this.formback;
            dyndialog.formBody.AutoSize = false;
            dyndialog.formBody.BorderStyle = BorderStyle.None;
            this.forms.Add((object)dyndialog);
            dyndialog.Name = xelem.GetAttribute("id");
            
            if (xelem.GetAttribute("id") == "spellChoose")
            {
                dyndialog.ClientSize = new Size(480, int.Parse(xelem.GetAttribute("height")) + 22);
            }
            else
                dyndialog.ClientSize = new Size(int.Parse(xelem.GetAttribute("width")), int.Parse(xelem.GetAttribute("height")) + 22);

            if (this.positionList.ContainsKey(xelem.GetAttribute("id")))
            {
                dyndialog.StartPosition = FormStartPosition.Manual;
                dyndialog.Location = this.positionList[xelem.GetAttribute("id")];
            }
            else
            {
                dyndialog.StartPosition = FormStartPosition.CenterScreen;
            }

            dyndialog.FormClosed += (s, e) => this.loadSave.Save();
            dyndialog.formBody.Visible = false;

            foreach (XmlElement xmlElement in xelem.FirstChild.ChildNodes)
            {
                switch (xmlElement.Name)
                {
                    case "label":
                        this.Parse_labels(xmlElement, dyndialog);
                        continue;
                    case "cmdButton":
                        this.Parse_command_buttons(xmlElement, dyndialog);
                        continue;
                    case "closeButton":
                        this.Parse_close_button(xmlElement, dyndialog);
                        continue;
                    case "radio":
                        this.Parse_radio_button(xmlElement, dyndialog);
                        continue;
                    case "streamBox":
                        this.Parse_stream_box(xmlElement, dyndialog);
                        continue;
                    case "dropDownBox":
                        this.Parse_drop_down(xmlElement, dyndialog);
                        continue;
                    case "editBox":
                        this.Parse_edit_box(xmlElement, dyndialog);
                        continue;
                    case "upDownEditBox":
                        this.Parse_numericupdown(xmlElement, dyndialog);
                        continue;
                    case "clearContainer":
                        this.Parse_container(xmlElement);
                        continue;
                    case "progressBar":
                        this.Parse_progress_bar(xmlElement, dyndialog);
                        continue;
                    default:
                        continue;
                }
            }
            dyndialog.formBody.Visible = true;
            dyndialog.formBody.AutoScroll = true;
            if (xelem.HasAttribute("resident") && xelem.GetAttribute("resident").Equals("false") && !xelem.GetAttribute("location").Equals("detach"))
                return;
            dyndialog.TopMost = true;
            dyndialog.ShowForm();
        }

        private void Parse_container(XmlElement elem)
        {
            if (!this.bStowContainer)
                return;
            this.ghost.SendText("#clear " + elem.GetAttribute("id"));
        }

        private void Parse_inventory(XmlElement elem)
        {
            if (!this.bStowContainer)
                return;
            this.ghost.SendText("#echo >" + elem.GetAttribute("id") + " " + elem.InnerText);
        }

        private void Parse_clear_stream(XmlElement xelem)
        {
            foreach (SkinnedMDIChild skinnedMdiChild in this.forms)
            {
                foreach (Control control in (ArrangedElementCollection)skinnedMdiChild.formBody.Controls)
                {
                    if (control.Name.Equals(xelem.GetAttribute("id")))
                        control.Text = "";
                }
            }
            this.documents.Remove((object)xelem.GetAttribute("id"));
        }

        public void Parse_set_stream(XmlElement xmlElement)
        {
            string id = xmlElement.GetAttribute("id");
            string value = xmlElement.InnerXml;

            this.documents[(object)id] = (object)value;
            foreach (SkinnedMDIChild skinnedMdiChild in this.forms)
            {
                foreach (Control control in (ArrangedElementCollection)skinnedMdiChild.formBody.Controls)
                {
                    if (control.Name.Equals(id))
                    {
                        switch (id)
                        {
                            case "spells":
                                // Create a specific element for the "spells" stream
                                if (control is Panel panel)
                                {
                                    if (panel.Controls.Count == 0)
                                    {
                                        // Only clear the panel and add the title label when it is first called
                                        panel.Controls.Clear();
                                        panel.SuspendLayout();
                                        //panel.Size = this.Build_size(xmlElement, 200, 380);
                                        panel.Height = 380;
                                        panel.Width = 200;
                                        panel.BackColor = this.formback;
                                        Label label = new Label
                                        {
                                            Text = "",
                                            AutoSize = true,
                                            Location = new Point(0, 0)
                                        };
                                        panel.Controls.Add(label);
                                    }

                                    int y = panel.Controls[panel.Controls.Count - 1].Bottom + 5;
                                    if (!xmlElement.HasChildNodes || xmlElement.GetElementsByTagName("d").Count == 0)
                                    {
                                        // Add a label for the spell book name
                                        Label bookLabel = new Label
                                        {
                                            Text = xmlElement.InnerXml,
                                            AutoSize = true,
                                            Location = new Point(0, y)
                                        };
                                        //bookLabel.Font = new Font(bookLabel.Font, FontStyle.Regular | FontStyle.Underline);
                                        bookLabel.Font = new Font(bookLabel.Font.FontFamily, 10, FontStyle.Bold);
                                        bookLabel.ForeColor = this.formfore;
                                        bookLabel.Click -= SpellLabel_Click;
                                        panel.Controls.Add(bookLabel);

                                        y += bookLabel.Height + 5;
                                    }
                                    else
                                    {
                                        // Add labels for the spells
                                        foreach (XmlNode node in xmlElement.ChildNodes)
                                        {
                                            if (node is XmlElement elem && elem.Name == "d")
                                            {
                                                Label spellLabel = new Label
                                                {
                                                    Text = elem.InnerText,
                                                    AutoSize = true,
                                                    Location = new Point(15, y),
                                                    ForeColor = this.formfore
                                                };
                                                spellLabel.Font = new Font(spellLabel.Font.FontFamily, 9, FontStyle.Underline);
                                                spellLabel.Tag = elem.GetAttribute("cmd");
                                                spellLabel.Click += SpellLabel_Click;
                                                panel.Controls.Add(spellLabel);

                                                y += spellLabel.Height + 5;
                                            }
                                        }
                                    }
                                    panel.ResumeLayout();
                                }
                                break;

                            case "spellInfo":
                                // Handle the "spellInfo" stream
                                if (control is RichTextBox spellInfoBox)
                                {
                                    spellInfoBox.AppendText(xmlElement.InnerText + Environment.NewLine);
                                    spellInfoBox.Width = 250;
                                    spellInfoBox.Location = new Point(215, 40);
                                    spellInfoBox.BackColor = this.formback;
                                }
                                break;

                            default:
                                value = Regex.Replace(value, "(<pushBold />|<popBold />|<pushBold/>|<popBold/>)", "");
                                //control.Text = value;

                                string attribute = xmlElement.GetAttribute("id");
                                string innerText = xmlElement.InnerText;
                                this.documents[(object)attribute] = (object)innerText;
                                foreach (Control control1 in (ArrangedElementCollection)skinnedMdiChild.formBody.Controls)
                                {
                                    if (control1.Name.Equals(attribute))
                                        control1.Text = innerText;
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void SpellLabel_Click(object sender, EventArgs e)
        {
            Label label = (Label)sender;
            string cmd = (string)label.Tag;
            ghost.SendText(cmd);

            // Find the form that contains the spell labels
            Form form = label.FindForm();

            // Find the "spells" panel control
            Control spellsPanel = form.Controls.Find("spells", true).FirstOrDefault();

            // Check if the "spells" panel was found
            if (spellsPanel != null)
            {
                // Find all spell labels in the "spells" panel
                var spellLabels = spellsPanel.Controls.OfType<Label>();

                // Reset the ForeColor of all spell labels to their default color
                foreach (var spellLabel in spellLabels)
                {
                    spellLabel.ForeColor = this.formfore;
                }
            }

            // Change the ForeColor of the clicked label to the desired color
            label.ForeColor = Color.Blue;

            // Find the choose button control
            Control chooseButton = form.Controls.Find("chooseSpell", true).FirstOrDefault();

            // Check if the choose button was found
            if (chooseButton != null)
            {
                // Update the text and command of the choose button
                chooseButton.Text = "Choose " + label.Text;
                ((CmdButton)chooseButton).cmd_string = cmd;
            }
        }

        private void Parse_stream_box(XmlElement cbx, SkinnedMDIChild dyndialog)
        {
            string id = cbx.GetAttribute("id");
            switch (id)
            {
                case "spells":
                    // Create a Panel control for the "spells" stream
                    Panel panel = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new Panel() : (Panel)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
                    if (panel == null)
                        return;
                    panel.Name = cbx.GetAttribute("id");
                    panel.Size = Build_size(cbx, int.Parse(cbx.GetAttribute("width")), int.Parse(cbx.GetAttribute("height")));
                    // Spells background color
                    panel.BackColor = this.formback;
                    panel.Location = Set_location(cbx, (Control)panel, dyndialog);
                    panel.AutoScroll = true;
                    //panel.AutoSize = true;
                    dyndialog.formBody.Controls.Add((Control)panel);

                    // Add a Label control for each spell
                    int y = 0;
                    foreach (XmlNode node in cbx.ChildNodes)
                    {
                        if (node is XmlElement elem && elem.Name == "d")
                        {
                            Label spellLabel = new Label
                            {
                                Text = elem.InnerText,
                                AutoSize = true,
                                Location = new Point(0, y),
                                ForeColor = this.formfore
                            };
                            spellLabel.Font = new Font(spellLabel.Font, FontStyle.Underline);
                            spellLabel.Tag = elem.GetAttribute("cmd");
                            spellLabel.Click += SpellLabel_Click;
                            panel.Controls.Add(spellLabel);
                            y += spellLabel.Height + 5;
                        }
                    }
                    break;
                case "spellInfo":
                    // Create a RichTextBox control for other streams
                    RichTextBox spellInfo = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new RichTextBox() : (RichTextBox)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
                    if (spellInfo == null)
                        return;
                    spellInfo.Name = cbx.GetAttribute("id");
                    spellInfo.Rtf = cbx.GetAttribute("value");
                    //spellInfo.Size = Build_size(cbx, int.Parse(cbx.GetAttribute("width")), int.Parse(cbx.GetAttribute("height")));
                    //spellInfo.Size = dyndialog.ClientSize = new Size(300, 380);
                    // spell info colors
                    spellInfo.BackColor = this.formback;
                    spellInfo.ForeColor = this.formfore;
                    //spellInfo.Location = Set_location(cbx, (Control)spellInfo, dyndialog);
                    spellInfo.Width = 300;
                    spellInfo.Height = 380;
                    spellInfo.Location = new Point(215, 40);
                    spellInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                    spellInfo.BorderStyle = BorderStyle.None;
                    spellInfo.Multiline = true;
                    spellInfo.ScrollBars = RichTextBoxScrollBars.Vertical;
                    spellInfo.ReadOnly = true;
                    spellInfo.LinkClicked += Rtb_LinkClicked;
                    spellInfo.DetectUrls = false;
                    dyndialog.formBody.Controls.Add((Control)spellInfo);
                    break;
                default:
                    TextBox textBox = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new TextBox() : (TextBox)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
                    if (textBox == null)
                        return;
                    textBox.Name = cbx.GetAttribute("id");
                    textBox.Text = cbx.GetAttribute("value");
                    textBox.Size = this.Build_size(cbx, 200, 75);
                    textBox.Location = this.Set_location(cbx, (Control)textBox, dyndialog);
                    textBox.Multiline = true;
                    textBox.ScrollBars = ScrollBars.Vertical;
                    dyndialog.formBody.Controls.Add((Control)textBox);
                    break;
            }
        }

        private void Rtb_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            RichTextBox richTextBox = (RichTextBox)sender;
            Point mousePos = richTextBox.PointToClient(Cursor.Position);
            int index = richTextBox.GetCharIndexFromPosition(mousePos);

            int start = richTextBox.Text.LastIndexOf("<d", index);
            int end = richTextBox.Text.IndexOf("</d>", index) + 4;
            string linkText = richTextBox.Text.Substring(start, end - start);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root>" + linkText + "</root>");
            if (doc.DocumentElement.FirstChild is XmlElement elem && elem.Name == "d" && elem.HasAttribute("cmd"))
            {
                ghost.SendText(elem.GetAttribute("cmd"));
            }
        }

        private void Parse_numericupdown(XmlElement cbx, SkinnedMDIChild dyndialog)
        {
            NumericUpDown numericUpDown = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new NumericUpDown() : (NumericUpDown)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
            if (numericUpDown == null)
                return;
            if (cbx.HasAttribute("max"))
                numericUpDown.Maximum = (Decimal)int.Parse(cbx.GetAttribute("max"));
            if (cbx.HasAttribute("min"))
                numericUpDown.Minimum = (Decimal)int.Parse(cbx.GetAttribute("min"));
            numericUpDown.Name = cbx.GetAttribute("id");
            numericUpDown.Text = cbx.GetAttribute("value");
            numericUpDown.Value = (Decimal)int.Parse(cbx.GetAttribute("value"));
            numericUpDown.Size = this.Build_size(cbx, 200, 75);
            numericUpDown.Location = this.Set_location(cbx, (Control)numericUpDown, dyndialog);

            dyndialog.formBody.Controls.Add((Control)numericUpDown);
        }

        private void Parse_edit_box(XmlElement cbx, SkinnedMDIChild dyndialog)
        {
            TextBox textBox = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new TextBox() : (TextBox)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
            if (textBox == null)
                return;
            textBox.Name = cbx.GetAttribute("id");
            textBox.Text = cbx.GetAttribute("value");
            SkinnedMDIChild window = FindWindowByName("bugDialogBox");
            if (window != null)
            {
                textBox.TextChanged += (sender, e) => TextBox_TextChanged(sender, e, dyndialog);
            }
            textBox.Size = this.Build_size(cbx, 200, 75);
            textBox.Location = this.Set_location(cbx, (Control)textBox, dyndialog);
            if (cbx.HasAttribute("maxChars"))
                textBox.MaxLength = int.Parse(cbx.GetAttribute("maxChars"));
            textBox.Multiline = false;
            textBox.WordWrap = true;
            dyndialog.formBody.Controls.Add((Control)textBox);
        }

        private void TextBox_TextChanged(object sender, EventArgs e, SkinnedMDIChild dyndialog)
        {
            if (e is null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            SkinnedMDIChild window = FindWindowByName("bugDialogBox");
            if (window != null)
            {
                // Get a reference to the TextBox control
                TextBox textBox = sender as TextBox;

                // Calculate the current character count
                int charCount = textBox.Text.Length;

                // Find the Label control that corresponds to the TextBox control
                Label label;
                int maxChars;
                string labelText;
                if (textBox.Name == "title")
                {
                    label = dyndialog.formBody.Controls["titleLabel"] as Label;
                    label.Location = new Point(0, 105);
                    maxChars = 128;
                    labelText = "Title";
                }
                else if (textBox.Name == "details")
                {
                    label = dyndialog.formBody.Controls["detailsLabel"] as Label;
                    label.Location = new Point(0, 135);
                    maxChars = 875;
                    labelText = "Details";
                }
                else
                {
                    return;
                }

                // Update the label with the current character count and maximum character count
                label.Text = $"{labelText} {charCount}/{maxChars}";
                label.AutoSize = true;
                // Add the Label control to the form
                dyndialog.formBody.Controls.Add(label);
            }
        }

        private void Parse_check_box(XmlElement cbx, SkinnedMDIChild dyndialog)
        {
            cbCheckBox cbCheckBox = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new cbCheckBox() : (cbCheckBox)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
            if (cbCheckBox == null)
                return;
            cbCheckBox.Name = cbx.GetAttribute("id");
            cbCheckBox.Text = cbx.GetAttribute("text");
            cbCheckBox.checked_value = cbx.GetAttribute("checked_value");
            cbCheckBox.unchecked_value = cbx.GetAttribute("unchecked_value");
            cbCheckBox.Checked = cbx.HasAttribute("checked");
            cbCheckBox.Size = this.Build_size(cbx, 200, 20);
            cbCheckBox.Location = this.Set_location(cbx, (Control)cbCheckBox, dyndialog);
            dyndialog.formBody.Controls.Add((Control)cbCheckBox);
        }

        private void Parse_radio_button(XmlElement cbx, SkinnedMDIChild dyndialog)
        {
            cbRadio cbRadio = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new cbRadio() : (cbRadio)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
            if (cbRadio == null)
                return;
            cbRadio.Name = cbx.GetAttribute("id");
            cbRadio.Text = cbx.GetAttribute("text");
            cbRadio.command = cbx.GetAttribute("cmd");
            cbRadio.group = cbx.GetAttribute("group");
            if (cbx.GetAttribute("value").Contains("0"))
                cbRadio.Checked = false;
            else
                cbRadio.Checked = true;
            cbRadio.CheckedChanged += new EventHandler(this.CbRadioSelect);
            cbRadio.Size = this.Build_size(cbx, 200, 20);
            cbRadio.Location = this.Set_location(cbx, (Control)cbRadio, dyndialog);
            cbRadio.Click += new EventHandler(this.CbRadioSelect);
            dyndialog.formBody.Controls.Add((Control)cbRadio);
        }

        private void Parse_progress_bar(XmlElement cbx, SkinnedMDIChild dyndialog)
        {
            ProgressBar progressBar = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new ProgressBar() : (ProgressBar)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
            if (progressBar == null)
                return;
            progressBar.Name = cbx.GetAttribute("id");
            progressBar.Text = cbx.GetAttribute("text");
            progressBar.Style = ProgressBarStyle.Continuous;
            int.TryParse(cbx.GetAttribute("value"), out int result);
            progressBar.Value = result;
            progressBar.Size = this.Build_size(cbx, 200, 20);
            progressBar.Location = this.Set_location(cbx, (Control)progressBar, dyndialog);
            dyndialog.formBody.Controls.Add((Control)progressBar);
        }

        private void Parse_close_button(XmlElement cbx, SkinnedMDIChild dyndialog)
        {
            // Find the existing choose button control
            Control chooseButton = dyndialog.formBody.Controls.Find("chooseSpell", true).FirstOrDefault();

            // Check if the choose button was found
            if (chooseButton != null)
            {
                // Update the text and command of the choose button
                chooseButton.Text = cbx.GetAttribute("value");
                ((CmdButton)chooseButton).cmd_string = !cbx.HasAttribute("cmd") ? "" : cbx.GetAttribute("cmd");

                // Redraw the choose button
                chooseButton.Invalidate();
            }
            else
            {
                // Create a new closeButton if it doesn't exist
                CmdButton closeButton = new CmdButton
                {
                    Name = cbx.GetAttribute("id"),
                    Text = cbx.GetAttribute("value"), // Set the Text property to the value of the value attribute
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    cmd_string = !cbx.HasAttribute("cmd") ? "" : cbx.GetAttribute("cmd")
                };
                closeButton.Location = this.Set_location(cbx, (Control)closeButton, dyndialog);
                closeButton.Click += new EventHandler(this.CbClose);
                dyndialog.formBody.Controls.Add((Control)closeButton);
                dyndialog.CloseCommand = (Button)closeButton;
            }
        }

        private void Parse_drop_down(XmlElement cbx, SkinnedMDIChild dyndialog)
        {
            cbDropBox cbDropBox = new cbDropBox
            {
                Name = cbx.GetAttribute("id"),
                Text = cbx.GetAttribute("value"),
                content_handler_data = new Hashtable()
            };
            string[] strArray1 = cbx.GetAttribute("content_text").Split(',');
            string[] strArray2 = cbx.GetAttribute("content_value").Split(',');
            for (int index = 0; index < strArray1.Length; ++index)
            {
                cbDropBox.content_handler_data.Add((object)strArray1[index], (object)strArray2[index]);
                cbDropBox.Items.Add((object)strArray1[index]);
            }
            if (cbx.HasAttribute("cmd"))
            {
                cbDropBox.cmd = cbx.GetAttribute("cmd");
                cbDropBox.SelectedIndexChanged += new EventHandler(this.Cb_SelectedIndexChanged);
            }
            cbDropBox.Size = this.Build_size(cbx, 55, 20);
            if (cbDropBox.Name == "locationSettingDD")
            {
                Point currentLocation = this.Set_location(cbx, (Control)cbDropBox, dyndialog);
                cbDropBox.Location = new Point(currentLocation.X + 10, currentLocation.Y);
            }
            else
            cbDropBox.Location = this.Set_location(cbx, (Control)cbDropBox, dyndialog);
            dyndialog.formBody.Controls.Add((Control)cbDropBox);

        }

        private void Parse_command_buttons(XmlElement cbx, SkinnedMDIChild dyndialog)
        {
            CmdButton cmdButton = new CmdButton
            {
                Name = cbx.GetAttribute("id"),
                Text = cbx.GetAttribute("value"),
                cmd_string = cbx.GetAttribute("cmd"),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            if (cbx.GetAttribute("id") == "changeCustom" || cbx.GetAttribute("id") == "changeCustomString")
            {
                Point location = this.Set_location(cbx, (Control)cmdButton, dyndialog);
                if (location.X == 353)
                    location.X += 8;
                cmdButton.Location = location;
            }
            else
            {
                cmdButton.Location = this.Set_location(cbx, (Control)cmdButton, dyndialog);
            }
            cmdButton.Click += new EventHandler(this.CbCommand);
            dyndialog.formBody.Controls.Add((Control)cmdButton);
        }

        private void Parse_labels(XmlElement cbx, SkinnedMDIChild dyndialog)
        {
            Label label = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new Label() : (Label)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
            label.Text = cbx.GetAttribute("value");
            label.Name = cbx.GetAttribute("id");
            label.AutoSize = true;
            label.Size = this.Build_size(cbx, 200, 15);
            //if (!cbx.HasAttribute("width"))
            if (TextRenderer.MeasureText(label.Text, label.Font).Width > 0)
                label.Width = TextRenderer.MeasureText(label.Text, label.Font).Width;
            SkinnedMDIChild windowbug = FindWindowByName("bugDialogBox");
            if (windowbug != null)
            {
                switch (cbx.GetAttribute("id"))
                {
                    case "categoryLabel":
                        label.Location = new Point(30, 75);
                        break;
                    case "titleLabel":
                        label.Location = new Point(30, 105);
                        break;
                    case "detailsLabel":
                        label.Location = new Point(30, 135);
                        break;
                }
            }
            else
                label.Location = this.Set_location(cbx, (Control)label, dyndialog);
            if (dyndialog.formBody.Controls.Contains((Control)label))
                return;
            dyndialog.formBody.Controls.Add((Control)label);
        }

        public void CbCommand(object sender, EventArgs e)
        {
            CmdButton cmdButton = (CmdButton)sender;
            Panel panel = (Panel)cmdButton.Parent;
            string str1 = cmdButton.cmd_string;
            string str2 = "";
            if (cmdButton.cmd_string.Contains("%"))
            {
                foreach (Control control in (ArrangedElementCollection)panel.Controls)
                {
                    switch (control)
                    {
                        case cbRadio _:
                            if (((RadioButton)control).Checked)
                            {
                                str1 = str1.Replace("%" + ((cbRadio)control).group + "%", ((cbRadio)control).command + " ");
                                continue;
                            }
                            continue;
                        case cbCheckBox _:
                            str1 = str1.Replace("%" + control.Name + "%", ((cbCheckBox)control).value + " ");
                            continue;
                        case cbDropBox _:
                            cbDropBox cbDropBox = (cbDropBox)control;
                            if (cbDropBox.SelectedIndex > -1)
                                str2 = (string)cbDropBox.content_handler_data[cbDropBox.Items[cbDropBox.SelectedIndex]];
                            if (control.Name == "province1")
                                str1 = str1.Replace("%province1%", str2);
                            if (control.Name == "bank1")
                                str1 = str1.Replace("%bank1%", str2);
                            else if (control.Name == "bank2")
                                str1 = str1.Replace("%bank2%", str2);
                            if (control.Name == "category")
                            {
                                str1 = str1.Replace("%category%", cbDropBox.Text.Remove(cbDropBox.Text.IndexOf(" ")));
                                str1 = str1.Replace("%title%", ";%title%");
                                str1 = str1.Replace("%details%", ";%details%");
                            }
                            continue;
                        default:
                            str1 = str1.Replace("%" + control.Name + "%", control.Text + " ");
                            continue;
                    }
                }
                if (cmdButton.Text.Equals("Clear"))
                {
                    this.forms.Remove((object)(Form)((Control)sender).Parent);
                    ((Form)cmdButton.Parent).Close();
                }
            }

            if (cmdButton.Text == "Update Toggles")
            {
                ghost.SendText(str1);
                ghost.SendText("profile /edit");
            }
            else if (str1.Contains("profile /set"))
            {
                ghost.SendText(str1);
                ghost.SendText("profile /edit");
            }
            else if (str1.Contains("profile /toggle im"))
            {
                ghost.SendText("profile /edit");
            }
            else
            {
                this.ghost.SendText(str1.Replace(";", "\\;"));
            }
        }

        public void Cb_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbDropBox cbDropBox = (cbDropBox)sender;
            string str = "";

            if (cbDropBox.cmd.Contains("%"))
            {
                string newValue = "";
                if (cbDropBox.SelectedIndex > -1)
                    newValue = (string)cbDropBox.content_handler_data[cbDropBox.Items[cbDropBox.SelectedIndex]];

                str = cbDropBox.cmd.Replace("%" + cbDropBox.Name + "%", newValue);
            }
            else
            {
                str = cbDropBox.cmd;
            }

            this.ghost.SendText(str.Replace(";", "\\;"));

            // reload after dropdown profile change
            if (str.StartsWith("profile /set"))
            {
                this.ghost.SendText("profile /edit");
            }
        }

        public void CloseCommand(Button cb)
        {
            if (cb is null)
            {
                throw new ArgumentNullException(nameof(cb));
            }
        }

        public void CbClose(object sender, EventArgs e)
        {
            CmdButton cmdButton = sender as CmdButton;
            Panel panel = (Panel)cmdButton.Parent;
            SkinnedMDIChild skinnedMdiChild = (SkinnedMDIChild)cmdButton.FindForm();
            string str2 = "";
            if (cmdButton.cmd_string.Length > 2)
            {
                string str1 = cmdButton.cmd_string;
                if (cmdButton.cmd_string.Contains("%"))
                {
                    foreach (Control control in (ArrangedElementCollection)panel.Controls)
                    {
                        switch (control)
                        {
                            case cbRadio _:
                                if (((RadioButton)control).Checked)
                                {
                                    str1 = str1.Replace("%" + ((cbRadio)control).group + "%", ((cbRadio)control).command + " ");
                                    continue;
                                }
                                continue;
                            case cbDropBox _:
                                cbDropBox cbDropBox = (cbDropBox)control;
                                if (cbDropBox.SelectedIndex > -1)
                                    str2 = (string)cbDropBox.content_handler_data[cbDropBox.Items[cbDropBox.SelectedIndex]];
                                if (control.Name == "province1")
                                    str1 = str1.Replace("%province1%", str2);
                                if (control.Name == "bank1")
                                    str1 = str1.Replace("%bank1%", str2);
                                else if (control.Name == "bank2")
                                    str1 = str1.Replace("%bank2%", str2);
                                if (control.Name == "category")
                                {
                                    str1 = str1.Replace("%category%", cbDropBox.Text.Remove(cbDropBox.Text.IndexOf(" ")));
                                    str1 = str1.Replace("%title%", ";%title%");
                                    str1 = str1.Replace("%details%", ";%details%");
                                }
                                continue;
                            default:
                                str1 = str1.Replace("%" + control.Name + "%", control.Text);
                                continue;
                        }
                    }
                }
                if (cmdButton.Name == "chooseSpell")
                    ghost.SendText(cmdButton.Text + " Spell");
                else if (cmdButton.Name == "confirmOK")
                    ghost.SendText(str1);
                else if (str2 == "")
                {
                    this.forms.Remove((object)skinnedMdiChild);
                    skinnedMdiChild.Close();
                }
                else
                    this.ghost.SendText(str1.Replace(";", "\\;"));
            }

            this.forms.Remove((object)skinnedMdiChild);
            skinnedMdiChild.Close();
        }

        public void CbRadioSelect(object sender, EventArgs e)
        {
            cbRadio clickedRadio = (cbRadio)sender;

            // Only proceed if the radio was just checked and the event is from the user
            if (!clickedRadio.Checked || !clickedRadio.Focused)
                return;

            // Uncheck all radios in the same group
            foreach (Control control in clickedRadio.Parent.Controls)
            {
                if (control is cbRadio radio && radio.group == clickedRadio.group)
                {
                    radio.Checked = (radio == clickedRadio);
                }
            }

            // Send command
            if (!string.IsNullOrEmpty(clickedRadio.command))
            {
                InjuriesWindow.currentInjuryCommand = clickedRadio.command;
                ghost.SendText(clickedRadio.command);
            }
        }

        private Size Build_size(XmlElement cbx, int width, int height)
        {
            int w = cbx.HasAttribute("width") ? int.Parse(cbx.GetAttribute("width")) : width;
            int h = cbx.HasAttribute("height") ? int.Parse(cbx.GetAttribute("height")) : height;
            return new Size(w, h);
        }

        private Point Set_location(XmlElement cbx, Control cont, SkinnedMDIChild p_form)
        {
            int.TryParse(cbx.GetAttribute("top"), out int result2);
            int.TryParse(cbx.GetAttribute("left"), out int result1);

            if (cbx.HasAttribute("align"))
            {
                if (cbx.GetAttribute("align").Equals("center"))
                {
                    result2 = p_form.formBody.Height / 2 - cont.ClientSize.Height / 2 + result2;
                    result1 = p_form.formBody.Width / 2 - cont.ClientSize.Width / 2 + result1;
                }
                else if (cbx.GetAttribute("align").Equals("s"))
                {
                    cont.Anchor = AnchorStyles.Bottom;
                    result1 = p_form.formBody.Width / 2 - cont.ClientSize.Width / 2 + result1;
                }
                else if (cbx.GetAttribute("align").Equals("se"))
                    cont.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                else if (cbx.GetAttribute("align").Equals("sw"))
                    cont.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                else if (cbx.GetAttribute("align").Equals("n"))
                {
                    cont.Anchor = AnchorStyles.Top;
                    result1 = p_form.formBody.Width / 2 - cont.ClientSize.Width / 2 + result1;
                }
                else if (cbx.GetAttribute("align").Equals("ne"))
                    cont.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                else if (cbx.GetAttribute("align").Equals("nw"))
                    cont.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }
            else if (cbx.HasAttribute("anchor_left"))
            {
                Control control = p_form.formBody.Controls[cbx.GetAttribute("anchor_left")];
                int num1 = result2;
                int num2 = result1;
                result1 = control.Left + control.Width + num2 + 5;
                result2 = num1;
                if (result2 == 0)
                    result2 = control.Top;
            }
            else if (cbx.HasAttribute("anchor_right"))
            {
                Control control = p_form.formBody.Controls[cbx.GetAttribute("anchor_right")];
                int num1 = result2;
                int num2 = result1;
                result1 = control.Left - num2 - cont.Width - 5;
                result2 = num1;
                if (result2 == 0)
                    result2 = control.Top;
            }
            if (cbx.HasAttribute("anchor_top"))
            {
                Control control = p_form.formBody.Controls[cbx.GetAttribute("anchor_top")];
                result2 = result2 + control.Bottom + 2;
                if (result2 == 0)
                    result2 = control.Top;
            }
            if (result2 < 0)
                result2 = p_form.formBody.Height - cont.Height + result2;
            if (result1 < 0)
                result1 = p_form.formBody.Width - cont.Width + result1;
            if (result2 + cont.Height > p_form.formBody.Height)
            {
                int num = result2 + cont.Height - p_form.formBody.Height;
                p_form.ClientSize = new Size(p_form.ClientSize.Width, p_form.ClientSize.Height + num + 2);
            }
            if (result1 + cont.Width > p_form.formBody.Width)
            {
                int num = result1 + cont.Width - p_form.formBody.Width;
                p_form.ClientSize = new Size(p_form.ClientSize.Width + num + 2, p_form.ClientSize.Height);
            }
            return new Point(result1, result2);
        }
    }
}
