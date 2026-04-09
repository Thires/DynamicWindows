using GeniePlugin.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace DynamicWindows
{
    public class Plugin : IPlugin
    {
        // ── Public state (accessed by other classes) ─────────────────────────
        public ArrayList forms = new ArrayList();
        public Hashtable documents = new Hashtable();
        public Color formback = Color.Black;
        public Color formfore = Color.White;
        public bool bPluginEnabled = true;
        public float fontSize = 9f;
        public string fontFamily = SystemFonts.DefaultFont.FontFamily.Name;

        /// <summary>Resolved FontFamily used by all dynamic labels. Falls back to the system default.</summary>
        private FontFamily ResolvedFontFamily =>
            FontFamily.Families.FirstOrDefault(f => f.Name.Equals(fontFamily, StringComparison.OrdinalIgnoreCase))
            ?? SystemFonts.DefaultFont.FontFamily;

        /// <summary>Linear scale factor relative to the default font size of 9pt.</summary>
        private float FontScale => fontSize / 9f;
        public ArrayList ignorelist = new ArrayList();
        public Dictionary<string, Point> positionList = new Dictionary<string, Point>();
        public bool bStowContainer;
        public Form pForm;
        public IHost ghost;
        public bool bDisableOtherInjuries = true;
        public bool bDisableSelfInjuries = true;
        public LoadSave loadSave;
        public string characterName;

        // ── Private state ────────────────────────────────────────────────────
        private string configPath;
        private InjuriesWindow injuriesWindow;
        private InjuriesOthersWindow injuriesOthersWindow;
        private readonly Dictionary<string, InjuriesOthersWindow> injuryWindows = new Dictionary<string, InjuriesOthersWindow>();
        private string lastConnectionStatus = "";

        // ── IPlugin metadata ─────────────────────────────────────────────────
        public string Name => "Dynamic Windows";
        public string Version => "2.2.6";
        public string Author => "Multiple Developers";
        public string Description => "Displays content windows specified through the XML stream from the game.";

        public bool Enabled
        {
            get => bPluginEnabled;
            set => bPluginEnabled = value;
        }

        // =====================================================================
        // IPlugin lifecycle
        // =====================================================================

        public void Initialize(IHost host)
        {
            try
            {
                ghost = host;
                pForm = host.ParentForm;
                configPath = host.get_Variable("PluginPath");
                loadSave = new LoadSave(this, configPath, characterName);
                loadSave.Load();
                injuriesWindow = new InjuriesWindow(this);
                injuriesOthersWindow = new InjuriesOthersWindow(this);
            }
            catch (Exception ex)
            {
                host.EchoText("[Plugin Error] Initialization failed: " + ex.Message);
            }
        }

        public void Show()
        {
            new FormOptionWindow(this) { MdiParent = pForm, TopMost = true }.Show();
        }

        public void ParentClosing()
        {
            foreach (SkinnedMDIChild window in forms)
                positionList[window.Name] = window.Location;

            loadSave.Save();

            foreach (SkinnedMDIChild window in forms.Cast<SkinnedMDIChild>().ToList())
                window.Close();

            forms.Clear();
        }

        public void VariableChanged(string variable)
        {
            string connected = ghost.get_Variable("connected");

            if (connected != lastConnectionStatus)
            {
                lastConnectionStatus = connected;

                if (connected == "0")           // disconnected
                {
                    loadSave.Save();
                    foreach (SkinnedMDIChild w in forms.Cast<SkinnedMDIChild>().ToList())
                        w.Close();
                    forms.Clear();
                }
                else if (connected == "1")      // reconnected
                {
                    characterName = ghost.get_Variable("charactername");
                    loadSave = new LoadSave(this, configPath, characterName);
                    loadSave.Load();
                }
            }

            if (variable.Equals("charactername", StringComparison.OrdinalIgnoreCase))
                loadSave.Load();
        }

        // =====================================================================
        // Text / input parsing
        // =====================================================================

        public string ParseText(string text, string window)
        {
            return (window.Trim().ToLower() == "main" || window.Trim() == string.Empty)
                ? ParseText(text)
                : text;
        }

        public string ParseText(string text) => text;

        public string ParseInput(string text)
        {
            switch (text.ToLower())
            {
                case "/debugwindows":
                    ghost.EchoText("Form Count: " + forms.Count);
                    foreach (Control c in forms)
                        ghost.EchoText("    Form: " + c.Name);
                    foreach (string key in (IEnumerable)documents.Keys)
                        ghost.EchoText($"Variable: {key} - {documents[key]}");
                    return "";

                case "/injurieswindow":
                    if (!bDisableSelfInjuries)
                    {
                        ghost.EchoText("Re-opening injuries window...");
                        var match = ignorelist.Cast<string>()
                            .FirstOrDefault(x => x.Equals("injuries", StringComparison.OrdinalIgnoreCase));
                        if (match != null) ignorelist.Remove(match);
                        injuriesWindow.Create(null);
                    }
                    return "";

                case "/toggleinjuries":
                    bDisableSelfInjuries = !bDisableSelfInjuries;
                    ghost.EchoText("[Plugin]: Self injuries window is now " + (bDisableSelfInjuries ? "disabled" : "enabled"));
                    loadSave?.Save();
                    return "";

                case "/toggleotherinjuries":
                    bDisableOtherInjuries = !bDisableOtherInjuries;
                    ghost.EchoText("[Plugin]: Other injuries windows are now " + (bDisableOtherInjuries ? "disabled" : "enabled"));
                    loadSave?.Save();
                    return "";

                case "/injurieshelp":
                case "/injurieswindowshelp":
                case "/dynamicwindows help":
                    ghost.EchoText("Dynamic Windows Help:");
                    ghost.EchoText("  /injurieswindow         - Re-opens your self Injuries window if closed or lost.");
                    ghost.EchoText("  /toggleInjuries         - Enables or disables showing your own Injuries window.");
                    ghost.EchoText("  /toggleOtherInjuries    - Enables or disables all 'other injuries' windows for other players.");
                    ghost.EchoText("  /debugwindows           - Displays the number of open windows and their names.");
                    ghost.EchoText("  (All commands are case-insensitive and can be used at any time.)");
                    return "";
            }

            return text;
        }

        // =====================================================================
        // XML parsing – main dispatch
        // =====================================================================

        public void ParseXML(string xml)
        {
            if (!bPluginEnabled) return;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml("<?xml version='1.0'?><root>" + xml + "</root>");

                foreach (XmlElement elem in doc.DocumentElement.ChildNodes)
                {
                    string id = elem.GetAttribute("id");

                    if (!string.IsNullOrEmpty(id) &&
                        ignorelist.Cast<string>().Any(x => x.Equals(id, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    switch (elem.Name)
                    {
                        case "exposeStream": Parse_xml_exposestream(elem); continue;
                        case "pushStream": Parse_xml_pushstream(elem); continue;
                        case "popStream": Parse_xml_popStream(elem); continue;
                        case "streamWindow": Parse_xml_streamwindow(elem); continue;
                        case "closeDialog": Parse_xml_closewindow(elem); continue;
                        case "exposeDialog": Parse_xml_exposewindow(elem); continue;
                        case "dynaStream": Parse_set_stream(elem); continue;
                        case "clearDynaStream": Parse_clear_stream(elem); continue;
                        case "clearStream": Parse_clear_stream(elem); continue;
                        case "clearContainer": Parse_container(elem); continue;
                        case "inv": Parse_inventory(elem); continue;

                        case "openDialog":
                            if (id.StartsWith("injuries-"))
                            {
                                if (!bDisableOtherInjuries)
                                    injuriesOthersWindow.Create(id, elem.GetAttribute("title"));
                            }
                            else if (id == "injuries" && injuriesWindow != null)
                            {
                                if (!bDisableSelfInjuries)
                                    injuriesWindow.Create(elem);
                            }
                            else
                            {
                                Parse_xml_openwindow(elem);
                            }
                            continue;

                        case "dialogData":
                            if (id.StartsWith("injuries-"))
                            {
                                if (!bDisableOtherInjuries)
                                    injuriesOthersWindow.Update(id, elem);
                            }
                            else if (id == "injuries")
                            {
                                if (!bDisableSelfInjuries)
                                    injuriesWindow.Update(elem);
                            }
                            else
                            {
                                Parse_xml_updatewindow(elem);
                            }
                            continue;

                        default: continue;
                    }
                }
            }
            catch (Exception ex)
            {
                ghost.EchoText("Error parsing XML: " + ex.Message);
            }
        }

        // =====================================================================
        // Window open / close / expose / update
        // =====================================================================

        private void Parse_xml_streamwindow(XmlElement elem)
        {
            // Only handles the profile help popup
            if (elem.GetAttribute("id") != "profileHelp") return;

            string id = elem.GetAttribute("id");
            int width = elem.HasAttribute("width") ? int.Parse(elem.GetAttribute("width")) : 375;
            int height = elem.HasAttribute("height") ? int.Parse(elem.GetAttribute("height")) : 350;

            CloseWindowIfOpen(id);

            var win = CreateSkinnedWindow(id, elem.GetAttribute("title"), width, height + 22);
            win.formBody.Visible = true;
            win.formBody.AutoScroll = true;
            win.formBody.AutoSize = true;

            if (elem.HasAttribute("resident") &&
                elem.GetAttribute("resident").Equals("false") &&
                !elem.GetAttribute("location").Equals("center"))
                return;

            var help = new HelpWindows();
            var contentBox = new RichTextBox
            {
                ForeColor = formfore,
                BackColor = formback,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill
            };

            if (win.Text == "Profile RP Help") contentBox.Text = help.RPHelp;
            if (win.Text == "Profile PVP Help") contentBox.Text = help.PVPHelp;
            if (win.Text == "Profile SPOUSE Help") contentBox.Text = help.SPOUSEHelp;

            ghost.SendText("#window remove profileHelp");
            win.formBody.Controls.Add(contentBox);
            win.ShowForm();
        }

        public void Parse_xml_openwindow(XmlElement xelem)
        {
            if (!xelem.GetAttribute("type").Equals("dynamic") ||
                !xelem.HasAttribute("width") || !xelem.HasAttribute("height"))
                return;

            string id = xelem.GetAttribute("id");

            if (loadSave.IsIgnored(id)) return;

            CloseWindowIfOpen(id);

            // Spell/feat choose dialogs have custom layout — scale their window width with font size
            int xmlWidth = int.Parse(xelem.GetAttribute("width"));
            int xmlHeight = int.Parse(xelem.GetAttribute("height"));
            int width, height;

            if (id == "spellChoose" || id == "featChoose" || id == "featRemove")
            {
                width = (int)(xmlWidth * FontScale);
                height = (int)(xmlHeight * FontScale);
            }
            else
            {
                width = xmlWidth;
                height = xmlHeight;
            }

            var dialog = CreateSkinnedWindow(id, xelem.GetAttribute("title"), width, height + 22);
            dialog.formBody.ForeColor = formfore;
            dialog.formBody.BackColor = formback;
            dialog.formBody.AutoSize = false;
            dialog.formBody.BorderStyle = BorderStyle.None;
            dialog.formBody.Visible = false;

            BuildDialogControls(xelem.FirstChild as XmlElement, dialog);

            // For dialogs whose window size comes from XML (not scaled), auto-expand
            // to fit content if the larger font pushes controls outside the original bounds.
            if (id != "spellChoose" && id != "featChoose" && id != "featRemove")
                AutoFitDialog(dialog);

            dialog.formBody.Visible = true;
            dialog.formBody.AutoScroll = true;

            bool isResident = xelem.HasAttribute("resident") && xelem.GetAttribute("resident").Equals("false");
            string location = xelem.GetAttribute("location");
            // Only suppress showing if the dialog is non-resident and neither centered nor detached
            if (isResident && !location.Equals("center") && !location.Equals("detach")) return;

            dialog.TopMost = true;
            dialog.ShowForm();

            // "confirm" dialogs must steal focus immediately
            if (id == "confirm")
            {
                var t = new Timer { Interval = 10 };
                t.Tick += (s, e) => { t.Stop(); t.Dispose(); dialog.BringToFront(); dialog.Focus(); };
                t.Start();
            }
        }

        private void Parse_xml_updatewindow(XmlElement xelem)
        {
            var dialog = FindWindowByName(xelem.GetAttribute("id"));
            if (dialog == null) return;

            dialog.formBody.Visible = false;
            BuildDialogControls(xelem, dialog);
            AutoFitDialog(dialog);
            dialog.formBody.Visible = true;
            dialog.formBody.AutoScroll = true;
            dialog.formBody.AutoSize = true;
            dialog.TopMost = true;
            dialog.Update();
            dialog.ShowForm();
        }

        private void Parse_xml_exposewindow(XmlElement elem)
        {
            var win = FindWindowByName(elem.GetAttribute("id"));
            if (win == null) return;
            win.TopMost = true;
            win.Update();
            win.ShowForm();
        }

        private void Parse_xml_closewindow(XmlElement elem)
        {
            CloseWindowIfOpen(elem.GetAttribute("id"));
        }

        private void Parse_xml_exposestream(XmlElement elem)
        {
            FindWindowByName(elem.GetAttribute("id"))?.Show();
        }

        private void Parse_xml_pushstream(XmlElement elem)
        {
            var win = FindWindowByName(elem.GetAttribute("id"));
            if (win == null) return;

            var rtb = win.formBody.Controls[elem.GetAttribute("id")] as RichTextBox;
            rtb?.AppendText(elem.InnerText + Environment.NewLine);
        }

        private void Parse_xml_popStream(XmlElement elem)
        {
            var win = FindWindowByName(elem.GetAttribute("id"));
            if (win == null) return;

            var rtb = win.formBody.Controls[elem.GetAttribute("id")] as RichTextBox;
            rtb?.Clear();
        }

        // =====================================================================
        // Stream / inventory helpers
        // =====================================================================

        private void Parse_container(XmlElement elem)
        {
            if (bStowContainer)
                ghost.SendText("#clear " + elem.GetAttribute("id"));
        }

        private void Parse_inventory(XmlElement elem)
        {
            if (bStowContainer)
                ghost.SendText("#echo >" + elem.GetAttribute("id") + " " + elem.InnerText);
        }

        private void Parse_clear_stream(XmlElement xelem)
        {
            string id = xelem.GetAttribute("id");
            foreach (SkinnedMDIChild win in forms)
            {
                foreach (Control ctrl in win.formBody.Controls)
                {
                    if (ctrl.Name.Equals(id))
                        ctrl.Text = "";
                }
            }
            documents.Remove(id);
        }

        public void Parse_set_stream(XmlElement xmlElement)
        {
            string id = xmlElement.GetAttribute("id");
            string value = xmlElement.InnerXml;
            documents[id] = value;

            foreach (SkinnedMDIChild win in forms)
            {
                foreach (Control ctrl in win.formBody.Controls)
                {
                    if (!ctrl.Name.Equals(id)) continue;

                    switch (id)
                    {
                        case "spells":
                            Stream_AppendSpellItems(ctrl as Panel, xmlElement);
                            break;

                        case "spellInfo":
                            if (ctrl is RichTextBox spellRtb)
                            {
                                spellRtb.AppendText(xmlElement.InnerText + Environment.NewLine);
                                int spellListW = (int)(200 * FontScale);
                                spellRtb.Width = win.formBody.Width - spellListW - 15;
                                spellRtb.Location = new Point(spellListW + 10, 40);
                                spellRtb.BackColor = formback;
                            }
                            break;

                        case "featList":
                            Stream_AppendFeatItems(ctrl as Panel, xmlElement);
                            break;

                        case "featInfo":
                            if (ctrl is RichTextBox featRtb)
                            {
                                featRtb.AppendText(xmlElement.InnerText + Environment.NewLine);
                                int featListW = (int)(250 * FontScale);
                                featRtb.Width = win.formBody.Width - featListW - 15;
                                featRtb.Location = new Point(featListW + 10, 60);
                                featRtb.BackColor = formback;
                            }
                            break;

                        default:
                            value = Regex.Replace(value, @"(<pushBold\s*/>|<popBold\s*/>)", "");
                            string innerText = xmlElement.InnerText;
                            documents[id] = innerText;
                            foreach (Control ctrl2 in win.formBody.Controls)
                            {
                                if (ctrl2.Name.Equals(id))
                                    ctrl2.Text = innerText;
                            }
                            break;
                    }
                }
            }
        }

        // =====================================================================
        // Stream panel population helpers
        // =====================================================================

        /// <summary>Appends spell book headers and clickable spell labels to the spells panel.</summary>
        private void Stream_AppendSpellItems(Panel panel, XmlElement xmlElement)
        {
            if (panel == null) return;

            panel.SuspendLayout();

            if (panel.Controls.Count == 0)
            {
                panel.Height = 380;
                panel.Width = (int)(200 * FontScale);
                panel.BackColor = formback;
                panel.Controls.Add(new Label { Text = "", AutoSize = true, Location = new Point(0, 0) });
            }

            int y = panel.Controls[panel.Controls.Count - 1].Bottom + 5;

            bool hasSpells = xmlElement.HasChildNodes &&
                             xmlElement.GetElementsByTagName("d").Count > 0;

            if (!hasSpells)
            {
                // Section header (book name)
                var header = new Label
                {
                    Text = xmlElement.InnerXml,
                    AutoSize = true,
                    Location = new Point(0, y),
                    ForeColor = formfore,
                    Font = new Font(ResolvedFontFamily, fontSize + 1, FontStyle.Bold)
                };
                header.Click -= SpellLabel_Click;
                panel.Controls.Add(header);
            }
            else
            {
                foreach (XmlNode node in xmlElement.ChildNodes)
                {
                    if (!(node is XmlElement elem) || elem.Name != "d") continue;

                    var lbl = new Label
                    {
                        Text = elem.InnerText,
                        AutoSize = true,
                        Location = new Point(15, y),
                        ForeColor = formfore,
                        Font = new Font(ResolvedFontFamily, fontSize, FontStyle.Underline),
                        Tag = elem.GetAttribute("cmd")
                    };
                    lbl.Click += SpellLabel_Click;
                    panel.Controls.Add(lbl);
                    y += lbl.Height + 5;
                }
            }

            panel.ResumeLayout(false);
            panel.PerformLayout();
        }

        /// <summary>Appends clickable feat labels to the featList panel.</summary>
        private void Stream_AppendFeatItems(Panel panel, XmlElement xmlElement)
        {
            if (panel == null) return;

            // Skip dynaStream calls that carry no real content (the game pads with many empty entries)
            bool hasContent = xmlElement.ChildNodes.Cast<XmlNode>()
                .Any(n => n is XmlElement e && e.Name == "d" && !string.IsNullOrWhiteSpace(e.InnerText));
            if (!hasContent) return;

            panel.SuspendLayout();

            if (panel.Controls.Count == 0)
            {
                panel.Height = 380;
                panel.Width = (int)(250 * FontScale);
                panel.BackColor = formback;
                panel.Controls.Add(new Label { Text = "", AutoSize = true, Location = new Point(0, 0) });
            }

            int y = panel.Controls[panel.Controls.Count - 1].Bottom + 5;

            foreach (XmlNode node in xmlElement.ChildNodes)
            {
                if (!(node is XmlElement elem) || elem.Name != "d" ||
                    string.IsNullOrWhiteSpace(elem.InnerText)) continue;

                var lbl = MakeClickableLabel(elem.InnerText, elem.GetAttribute("cmd"), new Point(5, y), FeatLabel_Click);
                panel.Controls.Add(lbl);
                y += lbl.Height + 5;
            }

            panel.ResumeLayout(false);
            panel.PerformLayout();
        }

        // =====================================================================
        // Dialog control builders
        // =====================================================================

        /// <summary>Iterates child XML elements and builds the corresponding WinForms controls.</summary>
        private void BuildDialogControls(XmlElement container, SkinnedMDIChild dialog)
        {
            if (container == null) return;

            foreach (XmlElement cbx in container.ChildNodes)
            {
                switch (cbx.Name)
                {
                    case "label": Parse_labels(cbx, dialog); break;
                    case "cmdButton": Parse_command_buttons(cbx, dialog); break;
                    case "closeButton": Parse_close_button(cbx, dialog); break;
                    case "checkBox": Parse_check_box(cbx, dialog); break;
                    case "radio": Parse_radio_button(cbx, dialog); break;
                    case "streamBox": Parse_stream_box(cbx, dialog); break;
                    case "dropDownBox": Parse_drop_down(cbx, dialog); break;
                    case "editBox": Parse_edit_box(cbx, dialog); break;
                    case "upDownEditBox": Parse_numericupdown(cbx, dialog); break;
                    case "progressBar": Parse_progress_bar(cbx, dialog); break;
                    case "clearContainer": Parse_container(cbx); break;
                }
            }
        }

        private void Parse_stream_box(XmlElement cbx, SkinnedMDIChild dialog)
        {
            string id = cbx.GetAttribute("id");

            switch (id)
            {
                case "spells":
                    {
                        var panel = GetOrCreateControl<Panel>(cbx, dialog);
                        int spellPanelW = (int)(int.Parse(cbx.GetAttribute("width")) * FontScale);
                        int spellPanelH = (int)(int.Parse(cbx.GetAttribute("height")) * FontScale);
                        panel.Size = new Size(spellPanelW, spellPanelH);
                        panel.BackColor = formback;
                        panel.Location = SetLocation(cbx, panel, dialog);
                        panel.AutoScroll = true;
                        dialog.formBody.Controls.Add(panel);

                        int y = 0;
                        foreach (XmlNode node in cbx.ChildNodes)
                        {
                            if (!(node is XmlElement elem) || elem.Name != "d") continue;
                            var lbl = MakeClickableLabel(elem.InnerText, elem.GetAttribute("cmd"), new Point(0, y), SpellLabel_Click);
                            panel.Controls.Add(lbl);
                            y += lbl.Height + 5;
                        }
                        break;
                    }

                case "spellInfo":
                    {
                        var rtb = GetOrCreateControl<RichTextBox>(cbx, dialog);
                        rtb.BackColor = formback;
                        rtb.ForeColor = formfore;
                        int spellListW = (int)(200 * FontScale);  // matches spells panel base width
                        int infoLeft = spellListW + 10;
                        rtb.Location = new Point(infoLeft, 40);
                        rtb.Width = dialog.formBody.Width - infoLeft - 5;
                        rtb.Height = (int)(380 * FontScale);
                        rtb.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                        rtb.BorderStyle = BorderStyle.None;
                        rtb.Multiline = true;
                        rtb.ScrollBars = RichTextBoxScrollBars.Vertical;
                        rtb.ReadOnly = true;
                        rtb.DetectUrls = false;
                        rtb.LinkClicked += Rtb_LinkClicked;
                        dialog.formBody.Controls.Add(rtb);
                        break;
                    }

                case "featList":
                    {
                        var panel = GetOrCreateControl<Panel>(cbx, dialog);
                        int featPanelW = (int)(int.Parse(cbx.GetAttribute("width")) * FontScale);
                        int featPanelH = (int)(int.Parse(cbx.GetAttribute("height")) * FontScale);
                        panel.Size = new Size(featPanelW, featPanelH);
                        panel.BackColor = formback;
                        panel.Location = SetLocation(cbx, panel, dialog);
                        panel.AutoScroll = true;
                        dialog.formBody.Controls.Add(panel);

                        int y = 0;
                        foreach (XmlNode node in cbx.ChildNodes)
                        {
                            if (!(node is XmlElement elem) || elem.Name != "d" ||
                                string.IsNullOrWhiteSpace(elem.InnerText)) continue;
                            var lbl = MakeClickableLabel(elem.InnerText, elem.GetAttribute("cmd"), new Point(0, y), FeatLabel_Click);
                            panel.Controls.Add(lbl);
                            y += lbl.Height + 5;
                        }
                        break;
                    }

                case "featInfo":
                    {
                        var rtb = GetOrCreateControl<RichTextBox>(cbx, dialog);
                        rtb.BackColor = formback;
                        rtb.ForeColor = formfore;
                        int featListW = (int)(250 * FontScale);  // matches featList panel base width
                        int infoLeft = featListW + 10;
                        rtb.Location = new Point(infoLeft, 60);
                        rtb.Width = dialog.formBody.Width - infoLeft - 5;
                        rtb.Height = (int)(380 * FontScale);
                        rtb.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                        rtb.BorderStyle = BorderStyle.None;
                        rtb.Multiline = true;
                        rtb.ScrollBars = RichTextBoxScrollBars.Vertical;
                        rtb.ReadOnly = true;
                        rtb.DetectUrls = false;
                        dialog.formBody.Controls.Add(rtb);
                        break;
                    }

                default:
                    {
                        var tb = GetOrCreateControl<TextBox>(cbx, dialog);
                        tb.Text = cbx.GetAttribute("value");
                        tb.Size = BuildSize(cbx, 200, 75);
                        tb.Location = SetLocation(cbx, tb, dialog);
                        tb.Multiline = true;
                        tb.ScrollBars = ScrollBars.Vertical;
                        dialog.formBody.Controls.Add(tb);
                        break;
                    }
            }
        }

        private void Parse_close_button(XmlElement cbx, SkinnedMDIChild dialog)
        {
            // Reuse any existing action button (spell choose, feat choose/unlearn)
            Control existing = dialog.formBody.Controls.Find("chooseSpell", true).FirstOrDefault()
                            ?? dialog.formBody.Controls.Find("chooseFeat", true).FirstOrDefault()
                            ?? dialog.formBody.Controls.Find("unlearnFeat", true).FirstOrDefault();

            if (existing is CmdButton existingBtn)
            {
                existingBtn.Text = cbx.GetAttribute("value");
                existingBtn.cmd_string = cbx.HasAttribute("cmd") ? cbx.GetAttribute("cmd") : "";
                existingBtn.Invalidate();
                return;
            }

            var btn = new CmdButton
            {
                Name = cbx.GetAttribute("id"),
                Text = cbx.GetAttribute("value"),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                cmd_string = cbx.HasAttribute("cmd") ? cbx.GetAttribute("cmd") : ""
            };
            btn.Location = SetLocation(cbx, btn, dialog);
            btn.Click += CbClose;
            dialog.formBody.Controls.Add(btn);
            dialog.CloseCommand = btn;
        }

        private void Parse_command_buttons(XmlElement cbx, SkinnedMDIChild dialog)
        {
            var btn = new CmdButton
            {
                Name = cbx.GetAttribute("id"),
                Text = cbx.GetAttribute("value"),
                cmd_string = cbx.GetAttribute("cmd"),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            // Two specific buttons need a small X offset correction
            Point loc = SetLocation(cbx, btn, dialog);
            if ((cbx.GetAttribute("id") == "changeCustom" || cbx.GetAttribute("id") == "changeCustomString") && loc.X == 353)
                loc.X += 8;
            btn.Location = loc;

            btn.Click += CbCommand;
            dialog.formBody.Controls.Add(btn);
        }

        private void Parse_labels(XmlElement cbx, SkinnedMDIChild dialog)
        {
            var lbl = dialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id"))
                ? (Label)dialog.formBody.Controls[cbx.GetAttribute("id")]
                : new Label();

            lbl.Text = cbx.GetAttribute("value");
            lbl.Name = cbx.GetAttribute("id");
            lbl.AutoSize = true;
            lbl.Size = BuildSize(cbx, 200, 15);

            int measuredWidth = TextRenderer.MeasureText(lbl.Text, lbl.Font).Width;
            if (measuredWidth > 0) lbl.Width = measuredWidth;

            // Bug dialog has fixed label positions
            var bugWin = FindWindowByName("bugDialogBox");
            if (bugWin != null)
            {
                switch (cbx.GetAttribute("id"))
                {
                    case "categoryLabel": lbl.Location = new Point(30, 75); break;
                    case "titleLabel": lbl.Location = new Point(30, 105); break;
                    case "detailsLabel": lbl.Location = new Point(30, 135); break;
                    default: lbl.Location = SetLocation(cbx, lbl, dialog); break;
                }
            }
            else
            {
                lbl.Location = SetLocation(cbx, lbl, dialog);
            }

            if (!dialog.formBody.Controls.Contains(lbl))
                dialog.formBody.Controls.Add(lbl);
        }

        private void Parse_check_box(XmlElement cbx, SkinnedMDIChild dialog)
        {
            var cb = GetOrCreateControl<cbCheckBox>(cbx, dialog);
            cb.Text = cbx.GetAttribute("text");
            cb.checked_value = cbx.GetAttribute("checked_value");
            cb.unchecked_value = cbx.GetAttribute("unchecked_value");
            cb.Checked = cbx.HasAttribute("checked");
            cb.Size = BuildSize(cbx, 200, 20);
            cb.Location = SetLocation(cbx, cb, dialog);
            dialog.formBody.Controls.Add(cb);
        }

        private void Parse_radio_button(XmlElement cbx, SkinnedMDIChild dialog)
        {
            var rb = GetOrCreateControl<CbRadio>(cbx, dialog);
            rb.Text = cbx.GetAttribute("text");
            rb.command = cbx.GetAttribute("cmd");
            rb.group = cbx.GetAttribute("group");
            rb.Checked = !cbx.GetAttribute("value").Contains("0");
            rb.Size = BuildSize(cbx, 200, 20);
            rb.Location = SetLocation(cbx, rb, dialog);
            rb.CheckedChanged += CbRadioSelect;
            rb.Click += CbRadioSelect;
            dialog.formBody.Controls.Add(rb);
        }

        private void Parse_numericupdown(XmlElement cbx, SkinnedMDIChild dialog)
        {
            var nud = GetOrCreateControl<NumericUpDown>(cbx, dialog);
            if (cbx.HasAttribute("max")) nud.Maximum = int.Parse(cbx.GetAttribute("max"));
            if (cbx.HasAttribute("min")) nud.Minimum = int.Parse(cbx.GetAttribute("min"));
            nud.Text = cbx.GetAttribute("value");
            nud.Value = int.Parse(cbx.GetAttribute("value"));
            nud.Size = BuildSize(cbx, 200, 75);
            nud.Location = SetLocation(cbx, nud, dialog);
            dialog.formBody.Controls.Add(nud);
        }

        private void Parse_edit_box(XmlElement cbx, SkinnedMDIChild dialog)
        {
            var tb = GetOrCreateControl<TextBox>(cbx, dialog);
            tb.Text = cbx.GetAttribute("value");
            tb.Size = BuildSize(cbx, 200, 75);
            tb.Location = SetLocation(cbx, tb, dialog);
            tb.Multiline = false;
            tb.WordWrap = true;
            if (cbx.HasAttribute("maxChars"))
                tb.MaxLength = int.Parse(cbx.GetAttribute("maxChars"));
            if (FindWindowByName("bugDialogBox") != null)
                tb.TextChanged += (s, e) => TextBox_TextChanged(s, e, dialog);
            dialog.formBody.Controls.Add(tb);
        }

        private void Parse_progress_bar(XmlElement cbx, SkinnedMDIChild dialog)
        {
            var pb = GetOrCreateControl<ProgressBar>(cbx, dialog);
            pb.Style = ProgressBarStyle.Continuous;
            int.TryParse(cbx.GetAttribute("value"), out int val);
            pb.Value = val;
            pb.Size = BuildSize(cbx, 200, 20);
            pb.Location = SetLocation(cbx, pb, dialog);
            dialog.formBody.Controls.Add(pb);
        }

        private void Parse_drop_down(XmlElement cbx, SkinnedMDIChild dialog)
        {
            var dd = new cbDropBox
            {
                Name = cbx.GetAttribute("id"),
                Text = cbx.GetAttribute("value"),
                content_handler_data = new Hashtable()
            };

            string[] labels = cbx.GetAttribute("content_text").Split(',');
            string[] values = cbx.GetAttribute("content_value").Split(',');
            for (int i = 0; i < labels.Length; i++)
            {
                dd.content_handler_data.Add(labels[i], values[i]);
                dd.Items.Add(labels[i]);
            }

            if (cbx.HasAttribute("cmd"))
            {
                dd.cmd = cbx.GetAttribute("cmd");
                dd.SelectedIndexChanged += Cb_SelectedIndexChanged;
            }

            dd.Size = BuildSize(cbx, 55, 20);
            Point loc = SetLocation(cbx, dd, dialog);
            dd.Location = dd.Name == "locationSettingDD" ? new Point(loc.X + 10, loc.Y) : loc;

            dialog.formBody.Controls.Add(dd);
        }

        // =====================================================================
        // Event handlers – label clicks
        // =====================================================================

        private void SpellLabel_Click(object sender, EventArgs e)
        {
            var label = (Label)sender;
            string cmd = (string)label.Tag;
            ghost.SendText(cmd);

            Form form = label.FindForm();
            ResetLabelColors(form, "spells");
            label.ForeColor = Color.Blue;

            UpdateActionButton(form, "chooseSpell", "Choose " + label.Text, cmd);
        }

        private void FeatLabel_Click(object sender, EventArgs e)
        {
            var label = (Label)sender;
            string cmd = (string)label.Tag;

            Form form = label.FindForm();
            ResetLabelColors(form, "featList");
            label.ForeColor = Color.Blue;

            // Clear info pane for fresh content
            if (form.Controls.Find("featInfo", true).FirstOrDefault() is RichTextBox rtb)
                rtb.Clear();

            // Works for both choose and unlearn dialogs
            Control actionBtn = form.Controls.Find("chooseFeat", true).FirstOrDefault()
                             ?? form.Controls.Find("unlearnFeat", true).FirstOrDefault();
            if (actionBtn is CmdButton btn)
            {
                btn.Text = (btn.Name == "unlearnFeat" ? "Unlearn " : "Choose ") + label.Text;
                btn.cmd_string = cmd;
            }

            ghost.SendText(cmd);
        }

        private void Rtb_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            var rtb = (RichTextBox)sender;
            Point mouse = rtb.PointToClient(Cursor.Position);
            int index = rtb.GetCharIndexFromPosition(mouse);
            int start = rtb.Text.LastIndexOf("<d", index);
            int end = rtb.Text.IndexOf("</d>", index) + 4;
            string chunk = rtb.Text.Substring(start, end - start);

            var doc = new XmlDocument();
            doc.LoadXml("<root>" + chunk + "</root>");
            if (doc.DocumentElement.FirstChild is XmlElement elem &&
                elem.Name == "d" && elem.HasAttribute("cmd"))
                ghost.SendText(elem.GetAttribute("cmd"));
        }

        // =====================================================================
        // Event handlers – buttons
        // =====================================================================

        public void CbClose(object sender, EventArgs e)
        {
            var btn = (CmdButton)sender;
            var panel = (Panel)btn.Parent;
            var dialog = (SkinnedMDIChild)btn.FindForm();
            string cmd = btn.cmd_string;
            string ddValue = "";

            if (cmd.Length > 2)
            {
                if (cmd.Contains("%"))
                {
                    foreach (Control ctrl in panel.Controls)
                    {
                        switch (ctrl)
                        {
                            case CbRadio rb when rb.Checked:
                                cmd = cmd.Replace("%" + rb.group + "%", rb.command + " ");
                                break;
                            case cbDropBox dd:
                                if (dd.SelectedIndex > -1)
                                    ddValue = (string)dd.content_handler_data[dd.Items[dd.SelectedIndex]];
                                if (ctrl.Name == "province1") cmd = cmd.Replace("%province1%", ddValue);
                                if (ctrl.Name == "bank1") cmd = cmd.Replace("%bank1%", ddValue);
                                else if (ctrl.Name == "bank2") cmd = cmd.Replace("%bank2%", ddValue);
                                if (ctrl.Name == "category")
                                {
                                    cmd = cmd.Replace("%category%", dd.Text.Remove(dd.Text.IndexOf(" ")));
                                    cmd = cmd.Replace("%title%", ";%title%");
                                    cmd = cmd.Replace("%details%", ";%details%");
                                }
                                break;
                            default:
                                cmd = cmd.Replace("%" + ctrl.Name + "%", ctrl.Text);
                                break;
                        }
                    }
                }

                if (btn.Name == "chooseSpell")
                    ghost.SendText(btn.Text + " Spell");
                else if (btn.Name == "chooseFeat" || btn.Name == "unlearnFeat")
                    ghost.SendText(btn.cmd_string);
                else if (btn.Name == "confirmOK")
                    ghost.SendText(cmd);
                else if (ddValue == "")
                {
                    forms.Remove(dialog);
                    dialog.Close();
                    return;
                }
                else
                    ghost.SendText(cmd.Replace(";", "\\;"));
            }

            forms.Remove(dialog);
            dialog.Close();
        }

        public void CbCommand(object sender, EventArgs e)
        {
            var btn = (CmdButton)sender;
            var panel = (Panel)btn.Parent;
            string cmd = btn.cmd_string;
            string ddValue = "";

            if (cmd.Contains("%"))
            {
                foreach (Control ctrl in panel.Controls)
                {
                    switch (ctrl)
                    {
                        case CbRadio rb when rb.Checked:
                            cmd = cmd.Replace("%" + rb.group + "%", rb.command + " ");
                            break;
                        case cbCheckBox cb:
                            cmd = cmd.Replace("%" + ctrl.Name + "%", cb.value + " ");
                            break;
                        case cbDropBox dd:
                            if (dd.SelectedIndex > -1)
                                ddValue = (string)dd.content_handler_data[dd.Items[dd.SelectedIndex]];
                            if (ctrl.Name == "province1") cmd = cmd.Replace("%province1%", ddValue);
                            if (ctrl.Name == "bank1") cmd = cmd.Replace("%bank1%", ddValue);
                            else if (ctrl.Name == "bank2") cmd = cmd.Replace("%bank2%", ddValue);
                            if (ctrl.Name == "category")
                            {
                                cmd = cmd.Replace("%category%", dd.Text.Remove(dd.Text.IndexOf(" ")));
                                cmd = cmd.Replace("%title%", ";%title%");
                                cmd = cmd.Replace("%details%", ";%details%");
                            }
                            break;
                        default:
                            cmd = cmd.Replace("%" + ctrl.Name + "%", ctrl.Text + " ");
                            break;
                    }
                }

                if (btn.Text.Equals("Clear"))
                {
                    forms.Remove((Form)btn.Parent);
                    ((Form)btn.Parent).Close();
                    return;
                }
            }

            if (btn.Text == "Update Toggles" || cmd.Contains("profile /set"))
            {
                ghost.SendText(cmd);
                ghost.SendText("profile /edit");
            }
            else if (cmd.Contains("profile /toggle im"))
            {
                ghost.SendText("profile /edit");
            }
            else
            {
                ghost.SendText(cmd.Replace(";", "\\;"));
            }
        }

        public void CbRadioSelect(object sender, EventArgs e)
        {
            var rb = (CbRadio)sender;
            if (!rb.Checked || !rb.Focused) return;

            foreach (Control ctrl in rb.Parent.Controls)
            {
                if (ctrl is CbRadio other && other.group == rb.group)
                    other.Checked = (other == rb);
            }

            if (!string.IsNullOrEmpty(rb.command))
            {
                InjuriesWindow.currentInjuryCommand = rb.command;
                ghost.SendText(rb.command);
            }
        }

        public void Cb_SelectedIndexChanged(object sender, EventArgs e)
        {
            var dd = (cbDropBox)sender;
            string cmd = dd.cmd;

            if (cmd.Contains("%") && dd.SelectedIndex > -1)
            {
                string val = (string)dd.content_handler_data[dd.Items[dd.SelectedIndex]];
                cmd = cmd.Replace("%" + dd.Name + "%", val);
            }

            ghost.SendText(cmd.Replace(";", "\\;"));

            if (cmd.StartsWith("profile /set"))
                ghost.SendText("profile /edit");
        }

        // =====================================================================
        // TextBox change tracker (bug report dialog character counter)
        // =====================================================================

        private void TextBox_TextChanged(object sender, EventArgs e, SkinnedMDIChild dialog)
        {
            if (FindWindowByName("bugDialogBox") == null) return;

            var tb = (TextBox)sender;
            int count = tb.Text.Length;

            Label lbl;
            int maxChars;
            string labelName;

            switch (tb.Name)
            {
                case "title":
                    lbl = dialog.formBody.Controls["titleLabel"] as Label;
                    maxChars = 128;
                    labelName = "Title";
                    if (lbl != null) lbl.Location = new Point(0, 105);
                    break;
                case "details":
                    lbl = dialog.formBody.Controls["detailsLabel"] as Label;
                    maxChars = 875;
                    labelName = "Details";
                    if (lbl != null) lbl.Location = new Point(0, 135);
                    break;
                default: return;
            }

            if (lbl == null) return;
            lbl.Text = $"{labelName} {count}/{maxChars}";
            lbl.AutoSize = true;
            if (!dialog.formBody.Controls.Contains(lbl))
                dialog.formBody.Controls.Add(lbl);
        }

        // =====================================================================
        // CloseCommand stub (required by SkinnedMDIChild)
        // =====================================================================

        public void CloseCommand(Button cb)
        {
            if (cb is null) throw new ArgumentNullException(nameof(cb));
        }

        // =====================================================================
        // Layout helpers
        // =====================================================================

        private Size BuildSize(XmlElement cbx, int defaultWidth, int defaultHeight)
        {
            int w = cbx.HasAttribute("width") ? int.Parse(cbx.GetAttribute("width")) : defaultWidth;
            int h = cbx.HasAttribute("height") ? int.Parse(cbx.GetAttribute("height")) : defaultHeight;
            return new Size(w, h);
        }

        private Point SetLocation(XmlElement cbx, Control ctrl, SkinnedMDIChild parent)
        {
            int.TryParse(cbx.GetAttribute("top"), out int top);
            int.TryParse(cbx.GetAttribute("left"), out int left);

            if (cbx.HasAttribute("align"))
            {
                switch (cbx.GetAttribute("align"))
                {
                    case "center":
                        top = parent.formBody.Height / 2 - ctrl.ClientSize.Height / 2 + top;
                        left = parent.formBody.Width / 2 - ctrl.ClientSize.Width / 2 + left;
                        break;
                    case "s":
                        ctrl.Anchor = AnchorStyles.Bottom;
                        left = parent.formBody.Width / 2 - ctrl.ClientSize.Width / 2 + left;
                        break;
                    case "se": ctrl.Anchor = AnchorStyles.Bottom | AnchorStyles.Right; break;
                    case "sw": ctrl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left; break;
                    case "n":
                        ctrl.Anchor = AnchorStyles.Top;
                        left = parent.formBody.Width / 2 - ctrl.ClientSize.Width / 2 + left;
                        break;
                    case "ne": ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Right; break;
                    case "nw": ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Left; break;
                }
            }
            else if (cbx.HasAttribute("anchor_left"))
            {
                Control anchor = parent.formBody.Controls[cbx.GetAttribute("anchor_left")];
                left = anchor.Left + anchor.Width + left + 5;
                if (top == 0) top = anchor.Top;
            }
            else if (cbx.HasAttribute("anchor_right"))
            {
                Control anchor = parent.formBody.Controls[cbx.GetAttribute("anchor_right")];
                left = anchor.Left - left - ctrl.Width - 5;
                if (top == 0) top = anchor.Top;
            }

            if (cbx.HasAttribute("anchor_top"))
            {
                Control anchor = parent.formBody.Controls[cbx.GetAttribute("anchor_top")];
                top += anchor.Bottom + 2;
                if (top == 0) top = anchor.Top;
            }

            // Negative values mean "offset from the far edge"
            if (top < 0) top = parent.formBody.Height - ctrl.Height + top;
            if (left < 0) left = parent.formBody.Width - ctrl.Width + left;

            // Expand the parent if the control would overflow
            if (top + ctrl.Height > parent.formBody.Height)
                parent.ClientSize = new Size(parent.ClientSize.Width, parent.ClientSize.Height + (top + ctrl.Height - parent.formBody.Height) + 2);
            if (left + ctrl.Width > parent.formBody.Width)
                parent.ClientSize = new Size(parent.ClientSize.Width + (left + ctrl.Width - parent.formBody.Width) + 2, parent.ClientSize.Height);

            return new Point(left, top);
        }

        // =====================================================================
        // Private utility methods
        // =====================================================================

        /// <summary>
        /// Expands a dialog's client size so all controls fit without clipping.
        /// Measures the furthest right and bottom edge across all controls in formBody,
        /// then grows the window if needed. A small padding is added on each edge.
        /// </summary>
        private void AutoFitDialog(SkinnedMDIChild dialog, int padRight = 12, int padBottom = 12)
        {
            int maxRight = 0;
            int maxBottom = 0;

            foreach (Control ctrl in dialog.formBody.Controls)
            {
                int r = ctrl.Right;
                int b = ctrl.Bottom;
                if (r > maxRight) maxRight = r;
                if (b > maxBottom) maxBottom = b;
            }

            int neededWidth = maxRight + padRight;
            int neededHeight = maxBottom + padBottom;

            if (neededWidth > dialog.formBody.Width || neededHeight > dialog.formBody.Height)
            {
                int newClientW = Math.Max(dialog.ClientSize.Width, neededWidth);
                int newClientH = Math.Max(dialog.ClientSize.Height, neededHeight + 22); // +22 for title bar
                dialog.ClientSize = new Size(newClientW, newClientH);
            }
        }

        /// <summary>Creates a themed, positioned SkinnedMDIChild and adds it to the forms list.</summary>
        private SkinnedMDIChild CreateSkinnedWindow(string id, string title, int width, int height)
        {
            var win = new SkinnedMDIChild(ghost, this)
            {
                MdiParent = pForm,
                Text = title,
                ForeColor = formfore,
                Name = id,
                ClientSize = new Size(width, height)
            };
            win.formBody.ForeColor = formfore;
            win.formBody.Font = new Font(ResolvedFontFamily, fontSize, FontStyle.Regular);
            win.FormClosed += (s, e) => loadSave.Save();

            if (positionList.ContainsKey(id))
            {
                win.StartPosition = FormStartPosition.Manual;
                win.Location = positionList[id];
            }
            else
            {
                win.StartPosition = FormStartPosition.CenterScreen;
            }

            forms.Add(win);
            return win;
        }

        /// <summary>Finds an open window by name, or returns null.</summary>
        private SkinnedMDIChild FindWindowByName(string name)
        {
            foreach (SkinnedMDIChild win in forms)
            {
                if (win.Name == name && !win.IsDisposed)
                    return win;
            }
            return null;
        }

        /// <summary>Closes and removes a window from the forms list if it is open.</summary>
        private void CloseWindowIfOpen(string name)
        {
            var win = FindWindowByName(name);
            if (win == null) return;
            forms.Remove(win);
            win.Close();
        }

        /// <summary>Gets an existing typed control by id from the dialog, or creates a new one.</summary>
        private T GetOrCreateControl<T>(XmlElement cbx, SkinnedMDIChild dialog) where T : Control, new()
        {
            string id = cbx.GetAttribute("id");
            if (dialog.formBody.Controls.ContainsKey(id) && dialog.formBody.Controls[id] is T existing)
                return existing;
            var ctrl = new T { Name = id };
            return ctrl;
        }

        /// <summary>Creates a styled, clickable underlined label used in spell/feat lists.</summary>
        private Label MakeClickableLabel(string text, string cmd, Point location, EventHandler clickHandler)
        {
            var lbl = new Label
            {
                Text = text,
                AutoSize = true,
                Location = location,
                ForeColor = formfore,
                Font = new Font(ResolvedFontFamily, fontSize, FontStyle.Underline),
                Tag = cmd
            };
            lbl.Click += clickHandler;
            return lbl;
        }

        /// <summary>Resets all label ForeColors inside a named panel back to the default foreground color.</summary>
        private void ResetLabelColors(Form form, string panelName)
        {
            var panel = form.Controls.Find(panelName, true).FirstOrDefault();
            if (panel == null) return;
            foreach (var lbl in panel.Controls.OfType<Label>())
                lbl.ForeColor = formfore;
        }

        /// <summary>Finds a CmdButton by name and updates its text and command string.</summary>
        private void UpdateActionButton(Form form, string buttonName, string text, string cmd)
        {
            if (form.Controls.Find(buttonName, true).FirstOrDefault() is CmdButton btn)
            {
                btn.Text = text;
                btn.cmd_string = cmd;
            }
        }
    }
}
