using GeniePlugin.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Layout;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Microsoft.VisualBasic;

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
        private bool userClosing;
        private string chooseSpell;

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

        public string Version => "2.1.0";

        public string Author => "Multiple Developers";

        public string Description => "Displays content windows specified through the XML stream from the game.";

        public void Initialize(IHost Host)
        {
            try
            {
                this.ghost = Host;
                this.pForm = Host.ParentForm;
                this.configPath = Host.get_Variable("PluginPath");
                this.LoadConfig();
            }
            catch
            {
            }
        }

        public void SaveConfig()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.CreateXmlDeclaration("1.0", "utf-8", (string)null);
            XmlElement element1 = xmlDocument.CreateElement("DynamicWindows");
            xmlDocument.AppendChild((XmlNode)element1);
            XmlElement element2 = xmlDocument.CreateElement("Config");
            element2.SetAttribute("id", "foreground");
            element2.SetAttribute("color", ColorTranslator.ToHtml(this.formfore));
            element1.AppendChild((XmlNode)element2);
            XmlElement element3 = xmlDocument.CreateElement("Config");
            element3.SetAttribute("id", "background");
            element3.SetAttribute("color", ColorTranslator.ToHtml(this.formback));
            element1.AppendChild((XmlNode)element3);
            XmlElement element4 = xmlDocument.CreateElement("Config");
            element4.SetAttribute("id", "stowcontainer");
            element4.SetAttribute("enabled", this.bStowContainer.ToString());
            element1.AppendChild((XmlNode)element4);
            XmlElement element5 = xmlDocument.CreateElement("Config");
            element5.SetAttribute("id", "plugin");
            element5.SetAttribute("pluginenabled", this.bPluginEnabled.ToString());
            element1.AppendChild((XmlNode)element5);
            foreach (string str in this.ignorelist)
            {
                XmlElement element6 = xmlDocument.CreateElement("Ignore");
                element6.SetAttribute("id", str);
                element1.AppendChild((XmlNode)element6);
            }
            foreach (SkinnedMDIChild skinnedMdiChild in this.forms)
            {
                if (this.positionList.ContainsKey(skinnedMdiChild.Name))
                    this.positionList.Remove(skinnedMdiChild.Name);
                this.positionList.Add(skinnedMdiChild.Name, skinnedMdiChild.Location);
            }
            foreach (KeyValuePair<string, Point> keyValuePair in this.positionList)
            {
                XmlElement element6 = xmlDocument.CreateElement("Position");
                element6.SetAttribute("id", keyValuePair.Key);
                element6.SetAttribute("X", keyValuePair.Value.X.ToString());
                element6.SetAttribute("Y", keyValuePair.Value.Y.ToString());
                element1.AppendChild((XmlNode)element6);
            }
            xmlDocument.Save(this.configPath + "/DynamicWindows.xml");
        }

        private void LoadConfig()
        {
            XmlDocument xmlDocument = new XmlDocument();
            try
            {
                xmlDocument.Load(this.configPath + "/DynamicWindows.xml");
            }
            catch (Exception ex)
            {
                this.ghost.EchoText("Could not load Dynamic Windows Config File, It will be created when you change your options and hit OK: " + ex.Message);
                return;
            }
            foreach (XmlElement xmlElement in xmlDocument.GetElementsByTagName("Config"))
            {
                if (xmlElement.GetAttribute("id") == "foreground")
                    this.formfore = ColorTranslator.FromHtml(xmlElement.GetAttribute("color"));
                else if (xmlElement.GetAttribute("id") == "background")
                    this.formback = ColorTranslator.FromHtml(xmlElement.GetAttribute("color"));
                else if (xmlElement.GetAttribute("id") == "stowcontainer")
                {
                    bool result = false;
                    bool.TryParse(xmlElement.GetAttribute("enabled"), out result);
                    this.bStowContainer = result;
                }
                else if (xmlElement.GetAttribute("id") == "plugin")
                {
                    bool result = false;
                    bool.TryParse(xmlElement.GetAttribute("pluginenabled"), out result);
                    this.bPluginEnabled = result;
                }
            }
            foreach (XmlElement xmlElement in xmlDocument.GetElementsByTagName("Ignore"))
                this.ignorelist.Add((object)xmlElement.GetAttribute("id"));
            foreach (XmlElement xmlElement in xmlDocument.GetElementsByTagName("Position"))
                this.positionList.Add(xmlElement.GetAttribute("id"), new Point(int.Parse(xmlElement.GetAttribute("X")), int.Parse(xmlElement.GetAttribute("Y"))));
        }

        public string ParseInput(string Text)
        {
            if (!Text.StartsWith("/debugwindows"))
                return Text;
            this.ghost.EchoText("Form Count: " + this.forms.Count.ToString());
            foreach (Control control in this.forms)
                this.ghost.EchoText("    Form: " + control.Name);
            foreach (string str in (IEnumerable)this.documents.Keys)
                this.ghost.EchoText(string.Concat(new object[4]
                {
          (object) "Variable: ",
          (object) str,
          (object) " - ",
          this.documents[(object) str]
                }));
            return "";
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
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<?xml version='1.0'?><root>" + XML + "</root>");

            foreach (XmlElement xmlElement in xmlDocument.DocumentElement.ChildNodes)
            {
                switch (xmlElement.Name)
                {
                    //case "exposeStream":
                    //    this.Parse_xml_exposestream(xmlElement);
                    //    continue;
                    //case "pushStream":
                    //    this.Parse_xml_pushstream(xmlElement);
                    //    continue;
                    case "streamWindow":
                        this.Parse_xml_streamwindow(xmlElement);
                        continue;
                    case "openDialog":
                        this.Parse_xml_openwindow(xmlElement);
                        continue;
                    case "dialogData":
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

        public void Show()
        {
            FormOptionWindow formOptionWindow = new FormOptionWindow(this);
            formOptionWindow.MdiParent = this.pForm;
            formOptionWindow.TopMost = true;
            ((Control)formOptionWindow).Show();
        }

        public void VariableChanged(string Variable)
        {
        }

        public void ParentClosing()
        {
            this.SaveConfig();
        }

        private void Parse_xml_streamwindow(XmlElement elem)
        {

            // Check if the id attribute is "profileHelp"
            if (elem.GetAttribute("id") != "profileHelp")
            {
                return;
            }

            // Use default values for the width and height if they are not specified in the element
            int width = elem.HasAttribute("width") ? int.Parse(elem.GetAttribute("width")) : 600;
            int height = elem.HasAttribute("height") ? int.Parse(elem.GetAttribute("height")) : 300;

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
            SkinnedMDIChild streamWindow = new SkinnedMDIChild(this.ghost, this);
            streamWindow.MdiParent = this.pForm;
            streamWindow.Text = elem.GetAttribute("title");
            streamWindow.FormBorderStyle = FormBorderStyle.FixedSingle;
            streamWindow.ForeColor = this.formfore;
            streamWindow.formBody.ForeColor = this.formfore;
            streamWindow.Name = elem.GetAttribute("id");
            streamWindow.ClientSize = new Size(width, height + 22);
            if (this.positionList.ContainsKey(elem.GetAttribute("id")))
                streamWindow.Location = this.positionList[elem.GetAttribute("id")];
            streamWindow.StartPosition = FormStartPosition.CenterScreen;
            streamWindow.AutoSize = true;
            streamWindow.ControlBox = true; // Show the control box
            streamWindow.MinimizeBox = false; // Hide the minimize button
            streamWindow.MaximizeBox = false; // Hide the maximize button


            // Show the stream window
            streamWindow.formBody.Visible = true;
            streamWindow.formBody.AutoScroll = true;
            streamWindow.formBody.AutoSize = true;
            if (elem.HasAttribute("resident") && elem.GetAttribute("resident").Equals("false") && !elem.GetAttribute("location").Equals("center"))
                return;
            //streamWindow.TopMost = true;
            this.forms.Add(streamWindow);

            // Create a new instance of the HelpWindows class
            helpWindows helpWindows = new helpWindows();

            // Create a new RichTextBox control
            RichTextBox contentBox = new RichTextBox();
            contentBox.ForeColor = Color.White;
            contentBox.BackColor = Color.Black;
            contentBox.ReadOnly = true;
            contentBox.BorderStyle = BorderStyle.FixedSingle;
            if (streamWindow.Text == "Profile RP Help")
                contentBox.Text = helpWindows.RPHelp;
            if (streamWindow.Text == "Profile PVP Help")
                contentBox.Text = helpWindows.PVPHelp;
            if (streamWindow.Text == "Profile SPOUSE Help")
                contentBox.Text = helpWindows.SPOUSEHelp;
            contentBox.Dock = DockStyle.Fill;

            // Add the RichTextBox control to the formBody property of the streamWindow object
            streamWindow.formBody.Controls.Add(contentBox);
            streamWindow.ShowForm();
        }


        // Define a new method to find a window with a specific name
        private SkinnedMDIChild FindWindowByName(string name)
        {
            // Search for the window in the forms list
            foreach (SkinnedMDIChild window in this.forms)
            {
                if (window.Name == name)
                {
                    // Return the window if it is found
                    return window;
                }
            }

            // Return null if the window is not found
            return null;
        }


        private void Parse_xml_exposestream(XmlElement elem)
        {
            // Find the stream window with the specified id
            SkinnedMDIChild streamWindow = null;
            foreach (SkinnedMDIChild window in this.forms)
            {
                if (window.Name == elem.GetAttribute("id"))
                {
                    streamWindow = window;
                    break;
                }
            }

            // Check if the stream window was found
            if (streamWindow != null)
            {
                // Show the stream window
                streamWindow.Show();
            }
        }

        private void Parse_xml_pushstream(XmlElement elem)
        {
            // Find the stream window with the specified id
            SkinnedMDIChild streamWindow = null;
            foreach (SkinnedMDIChild window in this.forms)
            {
                if (window.Name == elem.GetAttribute("id"))
                {
                    streamWindow = window;
                    break;
                }
            }

            // Check if the stream window was found
            if (streamWindow != null)
            {
                // Update the content of the stream window
                string content = elem.InnerText;
                streamWindow.Text = content;
            }
        }


        private void Parse_xml_exposewindow(XmlElement elem)
        {
            foreach (SkinnedMDIChild skinnedMdiChild in this.forms)
            {
                string attribute = elem.GetAttribute("id");
                string innerText = elem.InnerText;
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
                        break;
                    case "cmdButton":
                        this.Parse_command_buttons(cbx, dyndialog);
                        break;
                    case "closeButton":
                        this.Parse_close_button(cbx, dyndialog);
                        break;
                    case "checkBox":
                        this.Parse_check_box(cbx, dyndialog);
                        break;
                    case "radio":
                        this.Parse_radio_button(cbx, dyndialog);
                        break;
                    case "streamBox":
                        this.Parse_stream_box(cbx, dyndialog);
                        break;
                    case "dropDownBox":
                        this.Parse_drop_down(cbx, dyndialog);
                        break;
                    case "upDownEditBox":
                        this.Parse_numericupdown(cbx, dyndialog);
                        break;
                    case "editBox":
                        this.Parse_edit_box(cbx, dyndialog);
                        break;
                    case "progressBar":
                        this.Parse_progress_bar(cbx, dyndialog);
                        break;
                    default:
                        break;
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
            if (this.ignorelist.Contains((object)xelem.GetAttribute("id")))
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
            SkinnedMDIChild dyndialog = new SkinnedMDIChild(this.ghost, this);
            dyndialog.MdiParent = this.pForm;
            dyndialog.Text = xelem.GetAttribute("title");
            dyndialog.formBody.ForeColor = this.formfore;
            dyndialog.formBody.BackColor = this.formback;
            dyndialog.formBody.AutoSize = true;
            dyndialog.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.forms.Add((object)dyndialog);
            dyndialog.Name = xelem.GetAttribute("id");
            dyndialog.ClientSize = new Size(int.Parse(xelem.GetAttribute("width")), int.Parse(xelem.GetAttribute("height")) + 22);
            if (this.positionList.ContainsKey(xelem.GetAttribute("id")))
                dyndialog.Location = this.positionList[xelem.GetAttribute("id")];
            dyndialog.formBody.Visible = false;
            dyndialog.StartPosition = FormStartPosition.CenterScreen;
            // Add a control box to the window
            dyndialog.ControlBox = true; // Show the control box
            dyndialog.MinimizeBox = false; // Hide the minimize button
            dyndialog.MaximizeBox = false; // Hide the maximize button


            // Add a FormClosing event handler
            //dyndialog.FormClosing += Dyndialog_FormClosing;
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

                                        Label label = new Label();
                                        label.Text = "";
                                        label.AutoSize = true;
                                        label.Location = new Point(0, 0);
                                        panel.Controls.Add(label);
                                    }

                                    int y = panel.Controls[panel.Controls.Count - 1].Bottom + 5;
                                    if (!xmlElement.HasChildNodes || xmlElement.GetElementsByTagName("d").Count == 0)
                                    {
                                        // Add a label for the spell book name
                                        Label bookLabel = new Label();
                                        bookLabel.Text = xmlElement.InnerXml;
                                        bookLabel.AutoSize = true;
                                        bookLabel.Location = new Point(0, y);
                                        //bookLabel.Font = new Font(bookLabel.Font, FontStyle.Regular | FontStyle.Underline);
                                        bookLabel.Font = new Font(bookLabel.Font.FontFamily, 10, FontStyle.Bold);
                                        bookLabel.ForeColor = Color.White;
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
                                                Label spellLabel = new Label();
                                                spellLabel.Text = elem.InnerText;
                                                spellLabel.AutoSize = true;
                                                spellLabel.Location = new Point(15, y);
                                                spellLabel.ForeColor = Color.White;
                                                //spellLabel.Font = new Font(spellLabel.Font, FontStyle.Regular | FontStyle.Underline);
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
                                if (control is RichTextBox richTextBox)
                                {
                                    richTextBox.AppendText(xmlElement.InnerText + Environment.NewLine);
                                    // Update the location of the spellInfo control
                                    //richTextBox.Location = new Point(255, 40);
                                }
                                break;


                            default:
                                value = Regex.Replace(value, "(<pushBold />|<popBold />)", "");
                                control.Text = value;
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
                    spellLabel.ForeColor = Color.White;
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
            if (cbx.GetAttribute("id") == "spells")
            {
                // Create a Panel control for the "spells" stream
                Panel panel = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new Panel() : (Panel)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
                if (panel == null)
                    return;
                panel.Name = cbx.GetAttribute("id");
                panel.Size = this.Build_size(cbx, 200, 75);
                // Spells background color
                panel.BackColor = Color.Black;
                panel.Location = this.Set_location(cbx, (Control)panel, dyndialog);
                panel.AutoScroll = true;
                panel.AutoSize = true;
                dyndialog.formBody.Controls.Add((Control)panel);

                // Add a Label control for each spell
                int y = 0;
                foreach (XmlNode node in cbx.ChildNodes)
                {
                    if (node is XmlElement elem && elem.Name == "d")
                    {
                        Label spellLabel = new Label();
                        spellLabel.Text = elem.InnerText;
                        spellLabel.AutoSize = true;
                        spellLabel.Location = new Point(0, y);
                        spellLabel.ForeColor = Color.White;
                        spellLabel.Font = new Font(spellLabel.Font, FontStyle.Underline);
                        spellLabel.Tag = elem.GetAttribute("cmd");
                        spellLabel.Click += SpellLabel_Click;
                        panel.Controls.Add(spellLabel);
                        y += spellLabel.Height + 5;
                    }
                }
            }
            else
            {
                // Create a RichTextBox control for other streams
                RichTextBox spellInfo = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new RichTextBox() : (RichTextBox)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
                if (spellInfo == null)
                    return;
                spellInfo.Name = cbx.GetAttribute("id");
                spellInfo.Rtf = cbx.GetAttribute("value");
                spellInfo.Size = this.Build_size(cbx, 200, 75);
                spellInfo.BackColor = Color.Black;
                spellInfo.ForeColor = Color.White;
                spellInfo.Location = this.Set_location(cbx, (Control)spellInfo, dyndialog);
                spellInfo.Multiline = true;
                spellInfo.ScrollBars = RichTextBoxScrollBars.Vertical;
                spellInfo.ReadOnly = true;
                spellInfo.LinkClicked += Rtb_LinkClicked;
                spellInfo.DetectUrls = false;
                dyndialog.formBody.Controls.Add((Control)spellInfo);
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
            XmlElement elem = doc.DocumentElement.FirstChild as XmlElement;
            if (elem != null && elem.Name == "d" && elem.HasAttribute("cmd"))
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
            textBox.Size = this.Build_size(cbx, 200, 75);
            textBox.Location = this.Set_location(cbx, (Control)textBox, dyndialog);
            if (cbx.HasAttribute("MaxChars"))
                textBox.MaxLength = int.Parse(cbx.GetAttribute("MaxChars"));
            textBox.Multiline = false;
            dyndialog.formBody.Controls.Add((Control)textBox);
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
            int result = 0;
            int.TryParse(cbx.GetAttribute("value"), out result);
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
                CmdButton closeButton = new CmdButton();
                closeButton.Name = cbx.GetAttribute("id");
                closeButton.Text = cbx.GetAttribute("value"); // Set the Text property to the value of the value attribute
                closeButton.AutoSize = true;
                closeButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                closeButton.cmd_string = !cbx.HasAttribute("cmd") ? "" : cbx.GetAttribute("cmd");
                closeButton.Location = this.Set_location(cbx, (Control)closeButton, dyndialog);
                closeButton.Click += new EventHandler(this.CbClose);
                dyndialog.formBody.Controls.Add((Control)closeButton);
                dyndialog.CloseCommand = (Button)closeButton;
            }
        }


        private void Parse_drop_down(XmlElement cbx, SkinnedMDIChild dyndialog)
        {
            cbDropBox cbDropBox = new cbDropBox();
            cbDropBox.Name = cbx.GetAttribute("id");
            cbDropBox.Text = cbx.GetAttribute("value");
            cbDropBox.content_handler_data = new Hashtable();
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
            CmdButton cmdButton = new CmdButton();
            cmdButton.Name = cbx.GetAttribute("id");
            cmdButton.Text = cbx.GetAttribute("value");
            cmdButton.cmd_string = cbx.GetAttribute("cmd");
            cmdButton.AutoSize = true;
            cmdButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            cmdButton.Location = this.Set_location(cbx, (Control)cmdButton, dyndialog);
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
            }
            if (str1.StartsWith("profile"))
                ghost.SendText(str1);
            else if (cmdButton.Text.Equals("Update Toggles"))
                this.ghost.SendText(str1);
            else if (cmdButton.Text.Equals("Update"))
                this.ghost.SendText(str1);
            else if (cmdButton.Text.Equals("Clear"))
            {
                this.forms.Remove((object)(Form)((Control)sender).Parent);
                ((Form)cmdButton.Parent).Close();
            }
            else
                this.ghost.SendText(str1.Replace(";", "\\;"));
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
            this.ghost.SendText(str.Replace(";", "\\;"));
        }

        public void CloseCommand(Button cb)
        {
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
                else if (str1.StartsWith("profile"))
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
            cbRadio cbRadio = (cbRadio)sender;
            SkinnedMDIChild skinnedMdiChild = (SkinnedMDIChild)cbRadio.FindForm();
            foreach (Control control in (ArrangedElementCollection)cbRadio.Parent.Controls)
            {
                if (control is cbRadio)
                {
                    if (cbRadio.group.Equals(((cbRadio)control).group) && !cbRadio.Name.Equals(control.Name))
                        ((RadioButton)control).Checked = false;
                    else
                        ((RadioButton)control).Checked = true;
                }
            }
        }

        private Size Build_size(XmlElement cbx, int width, int height)
        {
            int width1 = width;
            int height1 = height;
            if (cbx.HasAttribute("width"))
                width1 = int.Parse(cbx.GetAttribute("width"));
            if (cbx.HasAttribute("height"))
                height1 = int.Parse(cbx.GetAttribute("height"));
            return new Size(width1, height1);
        }

        private Point Set_location(XmlElement cbx, Control cont, SkinnedMDIChild p_form)
        {
            int result1 = 0;
            int result2 = 0;
            int.TryParse(cbx.GetAttribute("top"), out result2);
            int.TryParse(cbx.GetAttribute("left"), out result1);

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
