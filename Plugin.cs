using GeniePlugin.Interfaces;
using System;
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
        // ── Public state (accessed by other classes) ─────────────────────────new
        public List<DwForm> forms = new List<DwForm>();
        public Dictionary<string, string> documents = new Dictionary<string, string>();
        public Color formback = Color.Black;
        public Color formfore = Color.White;

        // User-selectable font family for the whole plugin (size still driven by Scale).
        public string FontFamilyName = SystemFonts.DefaultFont.Name;
        // User-selectable font style (Bold/Italic/Underline) applied to dialog controls.
        public FontStyle FontStyleChoice = FontStyle.Regular;
        // Highlight colour for the selected spell/feat clickable link (default blue).
        public Color linkColor = Color.Blue;
        // Fill colour for the AimTimer countdown bar (default royal blue).
        public Color timerBarColor = Color.RoyalBlue;
        public bool bPluginEnabled = true;
        public List<string> ignorelist = new List<string>();
        public Dictionary<string, Point> positionList = new Dictionary<string, Point>();
        public bool bStowContainer;
        public Form pForm = null!;
        public IHost ghost = null!;
        public bool bDisableOtherInjuries = true;
        public bool bDisableSelfInjuries = true;
        public LoadSave loadSave = null!;
        public string characterName = string.Empty;

        // ── UI Scale ─────────────────────────────────────────────────────────
        // Scale = 1.0 → original server pixel coords unchanged (default).
        // Scale = 1.25 → 25 % larger across all window dimensions, positions,
        //                 sizes, and fonts. Adjustable in the options window.
        public float Scale = 1.0f;

        public int S(int v) => (int)Math.Round(v * Scale);

        public float SF(float size) => (float)Math.Round(size * Scale, 1);

        // ── Fonts ────────────────────────────────────────────────────────────
        // LayoutFont: used by the re-layout engine on ALL controls in re-flowed dialogs.
        // At Scale=1.0 this matches SystemFonts.DefaultFont so the layout looks
        // identical to the original. Only grows when Scale is increased.
        public Font LayoutFont
        {
            get
            {
                // Identical to before only when nothing has been customised.
                if (FontFamilyName == SystemFonts.DefaultFont.Name
                    && Scale <= 1.0f && FontStyleChoice == FontStyle.Regular)
                    return SystemFonts.DefaultFont;

                float size = SF(SystemFonts.DefaultFont.Size);
                try { return new Font(FontFamilyName, size, FontStyleChoice); }
                catch { return new Font(FontFamilyName, size); }   // family can't render the style
            }
        }

        // Plugin-managed list/info pane fonts (spell/feat panels, RTBs)
        public Font LabelFont => new Font(FontFamilyName, SF(9f), FontStyle.Underline);
        public Font HeaderFont => new Font(FontFamilyName, SF(10f), FontStyle.Bold);
        public Font InfoFont => new Font(FontFamilyName, SF(9f), FontStyle.Regular);

        // ── Layout engine ────────────────────────────────────────────────────
        private DialogLayout _dialogLayout = null!;

        // ── Private state ────────────────────────────────────────────────────
        private string configPath = string.Empty;
        private InjuriesWindow injuriesWindow = null!;
        private InjuriesOthersWindow injuriesOthersWindow = null!;
        private ShopWindow shopWindow = null!;
        private string lastConnectionStatus = string.Empty;
        private AimTimer _aimTimer = null!;
        private string? _lastXml;

        private readonly HashSet<string> _pendingLayout = new HashSet<string>();

        // ── IPlugin metadata ─────────────────────────────────────────────────
        public string Name => "Dynamic Windows";
        public string Version => "3.0.0";
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
                shopWindow = new ShopWindow(this);
                _aimTimer = new AimTimer(this);
                _dialogLayout = new DialogLayout(this);
            }
            catch (Exception ex)
            {
                host.EchoText("[Plugin Error] Initialization failed: " + ex.Message);
            }
        }

        public void Show()
        {
            new FormOptionWindow(this) { Owner = pForm }.Show();
        }

        public void ParentClosing()
        {
            foreach (DwForm window in forms)
                positionList[window.Name] = window.Location;

            loadSave.Save();

            foreach (DwForm window in forms.ToList())
                window.Close();

            forms.Clear();
            _aimTimer.Dispose();
        }

        public void VariableChanged(string variable)
        {
            string connected = ghost.get_Variable("connected");

            if (connected != lastConnectionStatus)
            {
                lastConnectionStatus = connected;

                if (connected == "0")
                {
                    loadSave.Save();
                    foreach (DwForm w in forms.ToList())
                        w.Close();
                    forms.Clear();
                }
                else if (connected == "1")
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
            // Shop browser: captures item-detail lines. No-ops unless an item scan is active.
            shopWindow?.HandleText(text, window);

            return (window.Trim().ToLower() == "main" || window.Trim() == string.Empty)
                ? ParseText(text)
                : text;
        }

        public string ParseText(string text) => text;

        public string ParseInput(string text)
        {
            switch (text.ToLower())
            {
                case "shop window":
                case "/shop":
                case "/shopwindow":
                    shopWindow.Open();
                    return "";

                case "/debugwindows":
                    ghost.EchoText("Form Count: " + forms.Count);
                    foreach (DwForm c in forms)
                        ghost.EchoText("    Form: " + c.Name);
                    foreach (var kvp in documents)
                        ghost.EchoText($"Variable: {kvp.Key} - {kvp.Value}");
                    return "";

                case "/dumpxml":
                    if (_lastXml != null)
                        ghost.EchoText("[DumpXML]\n" + _lastXml);
                    else
                        ghost.EchoText("[DumpXML] No XML captured yet.");
                    return "";

                case "/injurieswindow":
                    if (!bDisableSelfInjuries)
                    {
                        ghost.EchoText("Re-opening injuries window...");
                        string? match = ignorelist.FirstOrDefault(x => x.Equals("injuries", StringComparison.OrdinalIgnoreCase));
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

                foreach (XmlElement elem in doc.DocumentElement!.ChildNodes)
                {
                    string id = elem.GetAttribute("id");

                    if (!string.IsNullOrEmpty(id) &&
                        ignorelist.Any(x => x.Equals(id, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    // Shop browser: captures <d cmd='shop ...'> links and the closing <prompt>.
                    // No-ops unless a shop scan is active.
                    shopWindow?.HandleXml(elem);

                    switch (elem.Name)
                    {
                        case "exposeStream": Parse_xml_exposestream(elem); continue;
                        case "pushStream": Parse_xml_pushstream(elem); continue;
                        case "popStream": Parse_xml_popStream(elem); continue;
                        case "streamWindow": Parse_xml_streamwindow(elem); continue;
                        case "closeDialog": Parse_xml_closewindow(elem); continue;
                        case "exposeDialog": Parse_xml_exposewindow(elem); continue;
                        case "dynaStream": Parse_set_stream(elem); continue;
                        case "clearDynaStream":
                        case "clearStream": Parse_clear_stream(elem); continue;
                        case "clearContainer": Parse_container(elem); continue;
                        case "inv": Parse_inventory(elem); continue;

                        case "openDialog":
                            _lastXml = elem.OuterXml; // capture for /dumpxml debug
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
                            else if (id == "AimTimerDialog")
                            {
                                _aimTimer.OnOpen();
                            }
                            else
                            {
                                Parse_xml_openwindow(elem);
                            }
                            continue;

                        case "dialogData":
                            _lastXml = elem.OuterXml; // capture for /dumpxml debug
                            if (id.StartsWith("injuries-"))
                            {
                                if (!bDisableOtherInjuries)
                                    injuriesOthersWindow.Update(id, elem);
                            }
                            else if (id == "injuries")
                            {
                                if (!bDisableSelfInjuries)
                                    injuriesWindow?.Update(elem);
                            }
                            else if (id == "AimTimerDialog")
                            {
                                var timerNode = elem.SelectSingleNode("timer[@id='firingTimer']") as XmlElement;
                                if (timerNode != null && long.TryParse(timerNode.GetAttribute("value"), out long epoch))
                                    _aimTimer.OnTimerValue(epoch);
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
            if (elem.GetAttribute("id") != "profileHelp") return;

            string id = elem.GetAttribute("id");
            int width = elem.HasAttribute("width") ? S(int.Parse(elem.GetAttribute("width"))) : S(375);
            int height = elem.HasAttribute("height") ? S(int.Parse(elem.GetAttribute("height"))) : S(350);

            CloseWindowIfOpen(id);

            var win = CreateWindow(id, elem.GetAttribute("title"), width, height + 22);
            win.FormBody.Visible = true;
            win.FormBody.AutoScroll = true;
            win.FormBody.AutoSize = true;

            if (elem.HasAttribute("resident") &&
                elem.GetAttribute("resident").Equals("false") &&
                !elem.GetAttribute("location").Equals("center"))
                return;

            var help = new HelpWindows();
            var contentBox = new RichTextBox
            {
                ForeColor = formfore,
                BackColor = formback,
                Font = InfoFont,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill
            };

            if (win.Text == "Profile RP Help") contentBox.Text = help.RPHelp;
            if (win.Text == "Profile PVP Help") contentBox.Text = help.PVPHelp;
            if (win.Text == "Profile SPOUSE Help") contentBox.Text = help.SPOUSEHelp;

            ghost.SendText("#window remove profileHelp");
            win.FormBody.Controls.Add(contentBox);
            AddHelpCloseRow(win);   // Close button + Esc, same as the command-help window
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
            _pendingLayout.Remove(id);

            int width = S(int.Parse(xelem.GetAttribute("width")));
            int height = S(int.Parse(xelem.GetAttribute("height")));

            var dialog = CreateWindow(id, xelem.GetAttribute("title"), width, height + 22);
            dialog.FormBody.ForeColor = formfore;
            dialog.FormBody.BackColor = formback;
            dialog.FormBody.AutoSize = false;
            dialog.FormBody.BorderStyle = BorderStyle.None;
            dialog.FormBody.Visible = false;

            bool isStreamDialog = id == "spellChoose" || id == "featChoose" || id == "featRemove";
            var firstChild = xelem.FirstChild as XmlElement;
            BuildDialogControls(firstChild, dialog, isStreamDialog);

            bool isEmpty = firstChild == null ||
                           !firstChild.HasChildNodes ||
                           firstChild.GetAttribute("clear") == "t";

            if (!isStreamDialog && !isEmpty && firstChild != null)
            {
                _dialogLayout.Apply(dialog, firstChild);
                // Retain a standalone copy of the laid-out container so later partial
                // dialogData updates can be merged and re-laid-out by the same engine
                // instead of falling back to raw server coords (see Parse_xml_updatewindow).
                dialog.Tag = CloneContainer(firstChild);
            }
            else if (!isStreamDialog && isEmpty)
                // Empty container — content arrives via dialogData; layout deferred to exposeDialog
                _pendingLayout.Add(id);
            else if (!isStreamDialog)
                AutoFitDialog(dialog);

            dialog.FormBody.Visible = true;
            dialog.FormBody.AutoScroll = true;

            bool isResident = xelem.HasAttribute("resident") && xelem.GetAttribute("resident").Equals("false");
            string location = xelem.GetAttribute("location");
            if (isResident && !location.Equals("center") && !location.Equals("detach")) return;

            // Don't show pending-layout windows yet — exposeDialog will show them
            if (!_pendingLayout.Contains(id))
                dialog.ShowForm();

            // Confirm dialogs need a deferred bring-to-front so they surface above
            // whatever profile/other windows triggered them
            if (id == "confirm")
            {
                var t = new System.Windows.Forms.Timer { Interval = 10 };
                t.Tick += (s, e) => { t.Stop(); t.Dispose(); dialog.BringToFront(); dialog.Focus(); };
                t.Start();
            }
        }

        private void Parse_xml_updatewindow(XmlElement xelem)
        {
            var dialog = FindWindowByName(xelem.GetAttribute("id"));
            if (dialog == null) return;

            string id = xelem.GetAttribute("id");
            bool isPending = _pendingLayout.Contains(id);
            bool isStreamDialog = id == "spellChoose" || id == "featChoose" || id == "featRemove";

            // Does this update bring in a control we haven't placed yet? Only then do we
            // need the layout engine. A pure value update (text only) must NOT re-layout:
            // re-running the content-sensitive grid churns column widths and shifts/expands
            // the whole window when a long value is set. Captured before BuildDialogControls
            // creates the new controls. (Store window.)
            bool introducesNewControl = xelem.ChildNodes.OfType<XmlElement>()
                .Select(e => e.GetAttribute("id"))
                .Where(cid => !string.IsNullOrEmpty(cid))
                .Any(cid => !dialog.FormBody.Controls.ContainsKey(cid));

            dialog.FormBody.Visible = false;
            BuildDialogControls(xelem, dialog);

            if (isPending)
            {
                // Accumulate this dialogData's child elements into a merged XmlElement
                // stored on dialog.Tag so exposeDialog can run layout over all of them.
                XmlElement merged;
                if (dialog.Tag is XmlElement existing)
                {
                    merged = existing;
                }
                else
                {
                    var doc = new XmlDocument();
                    merged = doc.CreateElement("dialogData");
                    doc.AppendChild(merged);
                    dialog.Tag = merged;
                }

                // Append a copy of each child element into the merged container
                foreach (XmlElement child in xelem.ChildNodes.OfType<XmlElement>())
                {
                    var imported = (XmlElement)merged.OwnerDocument.ImportNode(child, true);
                    merged.AppendChild(imported);
                }
            }
            else
            {
                // A grid-laid-out window (e.g. the store window) receiving a partial
                // dialogData update. Merge the update into the stored open container and
                // re-run the layout engine so align/anchor controls resolve to their real
                // positions instead of raw server coords — otherwise the Change/Clear
                // buttons drop into the upper-left corner and the labels overlap.
                if (!isStreamDialog && xelem.GetAttribute("clear") != "t"
                    && dialog.Tag is XmlElement storedContainer)
                {
                    // Keep the retained container current either way, so a later structural
                    // update re-lays out over the full, up-to-date control set.
                    MergeContainer(storedContainer, xelem);

                    // Re-run the grid only when the set of controls actually changed.
                    // Value-only updates keep the positions assigned on open and just
                    // refresh their text — no churn, no window growth, no left-shift.
                    if (introducesNewControl)
                        _dialogLayout.Apply(dialog, storedContainer);
                    else
                        AutoFitDialog(dialog);
                }
                else
                {
                    AutoFitDialog(dialog);
                }

                dialog.FormBody.Visible = true;
                dialog.FormBody.AutoScroll = true;
                dialog.FormBody.AutoSize = true;
                dialog.Update();
                dialog.ShowForm();
                return;
            }

            dialog.FormBody.Visible = true;
            dialog.FormBody.AutoScroll = true;
            dialog.FormBody.AutoSize = true;
        }

        private void Parse_xml_exposewindow(XmlElement elem)
        {
            string id = elem.GetAttribute("id");
            var win = FindWindowByName(id);
            if (win == null) return;

            if (_pendingLayout.Remove(id))
            {
                // All dialogData updates have arrived — run layout over the merged container
                if (win.Tag is XmlElement mergedContainer)
                {
                    _dialogLayout.Apply(win, mergedContainer);
                    win.FormBody.AutoScroll = true;
                }
                else
                {
                    AutoFitDialog(win);
                }
            }

            win.Update();
            win.ShowForm();
        }

        private void Parse_xml_closewindow(XmlElement elem)
        {
            string id = elem.GetAttribute("id");
            if (id == "AimTimerDialog")
                _aimTimer.OnClose();
            else
                CloseWindowIfOpen(id);
        }

        private void Parse_xml_exposestream(XmlElement elem)
        {
            FindWindowByName(elem.GetAttribute("id"))?.ShowForm();
        }

        private void Parse_xml_pushstream(XmlElement elem)
        {
            var win = FindWindowByName(elem.GetAttribute("id"));
            if (win == null) return;
            var rtb = win.FormBody.Controls[elem.GetAttribute("id")] as RichTextBox;
            rtb?.AppendText(elem.InnerText + Environment.NewLine);
        }

        private void Parse_xml_popStream(XmlElement elem)
        {
            var win = FindWindowByName(elem.GetAttribute("id"));
            if (win == null) return;
            var rtb = win.FormBody.Controls[elem.GetAttribute("id")] as RichTextBox;
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
            foreach (DwForm win in forms)
                foreach (Control ctrl in win.FormBody.Controls)
                    if (ctrl.Name.Equals(id))
                        ctrl.Text = "";
            documents.Remove(id);
        }

        public void Parse_set_stream(XmlElement xmlElement)
        {
            string id = xmlElement.GetAttribute("id");
            string value = xmlElement.InnerXml;
            documents[id] = value;

            foreach (DwForm win in forms)
            {
                foreach (Control ctrl in win.FormBody.Controls)
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
                                const int spellListW = 200;
                                spellRtb.Location = new Point(S(spellListW) + S(10), S(40));
                                spellRtb.Width = win.ClientSize.Width - S(spellListW) - S(25);
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
                                const int featListW = 250;
                                featRtb.Location = new Point(S(featListW) + S(10), S(60));
                                featRtb.Width = win.ClientSize.Width - S(featListW) - S(25);
                                featRtb.BackColor = formback;
                            }
                            break;

                        default:
                            value = Regex.Replace(value, @"(<pushBold\s*/>|<popBold\s*/>)", "");
                            string innerText = xmlElement.InnerText;
                            documents[id] = innerText;
                            foreach (Control ctrl2 in win.FormBody.Controls)
                                if (ctrl2.Name.Equals(id))
                                    ctrl2.Text = innerText;
                            break;
                    }
                }
            }
        }

        // =====================================================================
        // Stream panel population helpers
        // =====================================================================

        private void Stream_AppendSpellItems(Panel? panel, XmlElement xmlElement)
        {
            if (panel == null) return;

            panel.SuspendLayout();

            if (panel.Controls.Count == 0)
            {
                panel.Height = S(380);
                panel.Width = S(200);
                panel.BackColor = formback;
                panel.Controls.Add(new Label { Text = "", AutoSize = true, Location = new Point(0, 0) });
            }

            int y = panel.Controls[panel.Controls.Count - 1].Bottom + S(5);

            bool hasSpells = xmlElement.HasChildNodes &&
                             xmlElement.GetElementsByTagName("d").Count > 0;

            if (!hasSpells)
            {
                var header = new Label
                {
                    Text = xmlElement.InnerXml,
                    AutoSize = true,
                    Location = new Point(0, y),
                    ForeColor = formfore,
                    Font = HeaderFont
                };
                header.Click -= SpellLabel_Click;
                panel.Controls.Add(header);
            }
            else
            {
                foreach (XmlNode node in xmlElement.ChildNodes)
                {
                    if (node is not XmlElement elem || elem.Name != "d") continue;

                    var lbl = new Label
                    {
                        Text = elem.InnerText,
                        AutoSize = true,
                        Location = new Point(S(15), y),
                        ForeColor = formfore,
                        Font = LabelFont,
                        Tag = elem.GetAttribute("cmd")
                    };
                    lbl.Click += SpellLabel_Click;
                    panel.Controls.Add(lbl);
                    y += lbl.Height + S(5);
                }
            }

            panel.ResumeLayout(false);
            panel.PerformLayout();
        }

        private void Stream_AppendFeatItems(Panel? panel, XmlElement xmlElement)
        {
            if (panel == null) return;

            bool hasContent = xmlElement.ChildNodes.Cast<XmlNode>()
                .Any(n => n is XmlElement e && e.Name == "d" && !string.IsNullOrWhiteSpace(e.InnerText));
            if (!hasContent) return;

            panel.SuspendLayout();

            if (panel.Controls.Count == 0)
            {
                panel.Height = S(380);
                panel.Width = S(250);
                panel.BackColor = formback;
                panel.Controls.Add(new Label { Text = "", AutoSize = true, Location = new Point(0, 0) });
            }

            int y = panel.Controls[panel.Controls.Count - 1].Bottom + S(5);

            foreach (XmlNode node in xmlElement.ChildNodes)
            {
                if (node is not XmlElement elem || elem.Name != "d" ||
                    string.IsNullOrWhiteSpace(elem.InnerText)) continue;

                var lbl = MakeClickableLabel(elem.InnerText, elem.GetAttribute("cmd"), new Point(S(5), y), FeatLabel_Click);
                panel.Controls.Add(lbl);
                y += lbl.Height + S(5);
            }

            panel.ResumeLayout(false);
            panel.PerformLayout();
        }

        // =====================================================================
        // Dialog control builders
        // =====================================================================

        private void BuildDialogControls(XmlElement? container, DwForm dialog, bool isStreamDialog = false)
        {
            if (container == null) return;

            foreach (XmlElement cbx in container.ChildNodes)
            {
                switch (cbx.Name)
                {
                    case "label": Parse_labels(cbx, dialog, isStreamDialog); break;
                    case "cmdButton": Parse_command_buttons(cbx, dialog, isStreamDialog); break;
                    case "closeButton": Parse_close_button(cbx, dialog, isStreamDialog); break;
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

        private void Parse_stream_box(XmlElement cbx, DwForm dialog)
        {
            string id = cbx.GetAttribute("id");

            switch (id)
            {
                case "spells":
                    {
                        var panel = GetOrCreateControl<Panel>(cbx, dialog);
                        panel.Size = new Size(S(int.Parse(cbx.GetAttribute("width"))),
                                                  S(int.Parse(cbx.GetAttribute("height"))));
                        panel.BackColor = formback;
                        panel.Location = SetLocation(cbx, panel, dialog);
                        panel.AutoScroll = true;
                        dialog.FormBody.Controls.Add(panel);

                        int y = 0;
                        foreach (XmlNode node in cbx.ChildNodes)
                        {
                            if (node is not XmlElement elem || elem.Name != "d") continue;
                            var lbl = MakeClickableLabel(elem.InnerText, elem.GetAttribute("cmd"), new Point(0, y), SpellLabel_Click);
                            panel.Controls.Add(lbl);
                            y += lbl.Height + S(5);
                        }
                        break;
                    }

                case "spellInfo":
                    {
                        var rtb = GetOrCreateControl<RichTextBox>(cbx, dialog);
                        rtb.BackColor = formback;
                        rtb.ForeColor = formfore;
                        rtb.Font = InfoFont;
                        const int spellListW = 200;
                        rtb.Location = new Point(S(spellListW) + S(10), S(40));
                        rtb.Width = dialog.ClientSize.Width - S(spellListW) - S(25);
                        rtb.Height = S(380);
                        rtb.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                        rtb.BorderStyle = BorderStyle.None;
                        rtb.Multiline = true;
                        rtb.ScrollBars = RichTextBoxScrollBars.Vertical;
                        rtb.ReadOnly = true;
                        rtb.DetectUrls = false;
                        rtb.LinkClicked += Rtb_LinkClicked;
                        dialog.FormBody.Controls.Add(rtb);
                        break;
                    }

                case "featList":
                    {
                        var panel = GetOrCreateControl<Panel>(cbx, dialog);
                        panel.Size = new Size(S(int.Parse(cbx.GetAttribute("width"))),
                                                  S(int.Parse(cbx.GetAttribute("height"))));
                        panel.BackColor = formback;
                        panel.Location = SetLocation(cbx, panel, dialog);
                        panel.AutoScroll = true;
                        dialog.FormBody.Controls.Add(panel);

                        int y = 0;
                        foreach (XmlNode node in cbx.ChildNodes)
                        {
                            if (node is not XmlElement elem || elem.Name != "d" ||
                                string.IsNullOrWhiteSpace(elem.InnerText)) continue;
                            var lbl = MakeClickableLabel(elem.InnerText, elem.GetAttribute("cmd"), new Point(0, y), FeatLabel_Click);
                            panel.Controls.Add(lbl);
                            y += lbl.Height + S(5);
                        }
                        break;
                    }

                case "featInfo":
                    {
                        var rtb = GetOrCreateControl<RichTextBox>(cbx, dialog);
                        rtb.BackColor = formback;
                        rtb.ForeColor = formfore;
                        rtb.Font = InfoFont;
                        const int featListW = 250;
                        rtb.Location = new Point(S(featListW) + S(10), S(60));
                        rtb.Width = dialog.ClientSize.Width - S(featListW) - S(25);
                        rtb.Height = S(380);
                        rtb.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                        rtb.BorderStyle = BorderStyle.None;
                        rtb.Multiline = true;
                        rtb.ScrollBars = RichTextBoxScrollBars.Vertical;
                        rtb.ReadOnly = true;
                        rtb.DetectUrls = false;
                        dialog.FormBody.Controls.Add(rtb);
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
                        dialog.FormBody.Controls.Add(tb);
                        break;
                    }
            }
        }

        private void Parse_close_button(XmlElement cbx, DwForm dialog, bool useServerLayout = false)
        {
            Control? existing = dialog.FormBody.Controls.Find("chooseSpell", true).FirstOrDefault()
                             ?? dialog.FormBody.Controls.Find("chooseFeat", true).FirstOrDefault()
                             ?? dialog.FormBody.Controls.Find("unlearnFeat", true).FirstOrDefault();

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

            if (useServerLayout)
            {
                btn.Font = SystemFonts.DefaultFont;
                btn.Size = BuildSize(cbx, 75, 23);
                btn.Location = SetLocation(cbx, btn, dialog);
            }

            btn.Click += CbClose;
            dialog.FormBody.Controls.Add(btn);
            dialog.CloseCommand = btn;
        }

        private void Parse_command_buttons(XmlElement cbx, DwForm dialog, bool useServerLayout = false)
        {
            string id = cbx.GetAttribute("id");

            // Reuse an existing button: partial dialogData updates (e.g. the store window
            // after clearing the custom string) re-send the same cmdButtons. Creating a
            // fresh CmdButton each time stacked duplicate instances at (0,0) in the
            // upper-left corner, since only the original got repositioned by the layout
            // engine. Update the existing button in place instead.
            if (dialog.FormBody.Controls[id] is CmdButton existingBtn)
            {
                existingBtn.Text = cbx.GetAttribute("value");
                existingBtn.cmd_string = cbx.GetAttribute("cmd");
                if (useServerLayout)
                {
                    existingBtn.Size = BuildSize(cbx, 75, 23);
                    existingBtn.Location = SetLocation(cbx, existingBtn, dialog);
                }
                existingBtn.Invalidate();
                return;
            }

            var btn = new CmdButton
            {
                Name = id,
                Text = cbx.GetAttribute("value"),
                cmd_string = cbx.GetAttribute("cmd"),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            if (useServerLayout)
            {
                btn.Font = SystemFonts.DefaultFont;
                btn.Size = BuildSize(cbx, 75, 23);
                btn.Location = SetLocation(cbx, btn, dialog);
            }

            btn.Click += CbCommand;
            dialog.FormBody.Controls.Add(btn);
        }

        private void Parse_labels(XmlElement cbx, DwForm dialog, bool useServerLayout = false)
        {
            var lbl = dialog.FormBody.Controls.ContainsKey(cbx.GetAttribute("id"))
                ? (Label)dialog.FormBody.Controls[cbx.GetAttribute("id")]
                : new Label();

            lbl.Text = cbx.GetAttribute("value");
            lbl.Name = cbx.GetAttribute("id");
            lbl.AutoSize = true;
            lbl.ForeColor = formfore;

            if (useServerLayout)
            {
                lbl.Font = SystemFonts.DefaultFont;
                lbl.Size = BuildSize(cbx, 200, 15);
                lbl.Location = SetLocation(cbx, lbl, dialog);
            }

            if (!dialog.FormBody.Controls.Contains(lbl))
                dialog.FormBody.Controls.Add(lbl);
        }

        private void Parse_check_box(XmlElement cbx, DwForm dialog)
        {
            var cb = GetOrCreateControl<CbCheckBox>(cbx, dialog);
            cb.Text = cbx.GetAttribute("text");
            cb.checked_value = cbx.GetAttribute("checked_value");
            cb.unchecked_value = cbx.GetAttribute("unchecked_value");
            cb.Checked = cbx.HasAttribute("checked");
            cb.ForeColor = formfore;
            dialog.FormBody.Controls.Add(cb);
        }

        private void Parse_radio_button(XmlElement cbx, DwForm dialog)
        {
            var rb = GetOrCreateControl<CbRadio>(cbx, dialog);
            rb.Text = cbx.GetAttribute("text");
            rb.command = cbx.GetAttribute("cmd");
            rb.group = cbx.GetAttribute("group");
            rb.Checked = !cbx.GetAttribute("value").Contains("0");
            rb.ForeColor = formfore;
            rb.CheckedChanged += CbRadioSelect;
            rb.Click += CbRadioSelect;
            dialog.FormBody.Controls.Add(rb);
        }

        private void Parse_numericupdown(XmlElement cbx, DwForm dialog)
        {
            var nud = GetOrCreateControl<NumericUpDown>(cbx, dialog);
            if (cbx.HasAttribute("max")) nud.Maximum = int.Parse(cbx.GetAttribute("max"));
            if (cbx.HasAttribute("min")) nud.Minimum = int.Parse(cbx.GetAttribute("min"));
            nud.Text = cbx.GetAttribute("value");
            nud.Value = int.Parse(cbx.GetAttribute("value"));
            dialog.FormBody.Controls.Add(nud);
        }

        private void Parse_edit_box(XmlElement cbx, DwForm dialog)
        {
            var tb = GetOrCreateControl<TextBox>(cbx, dialog);
            tb.Text = cbx.GetAttribute("value");
            tb.Multiline = false;
            tb.WordWrap = true;
            // Preserve server width so the layout engine can use it during measurement
            if (cbx.HasAttribute("width")) tb.Width = int.Parse(cbx.GetAttribute("width"));
            if (cbx.HasAttribute("maxChars"))
                tb.MaxLength = int.Parse(cbx.GetAttribute("maxChars"));
            if (FindWindowByName("bugDialogBox") != null)
                tb.TextChanged += (s, e) => TextBox_TextChanged(s, e, dialog);
            dialog.FormBody.Controls.Add(tb);
        }

        private void Parse_progress_bar(XmlElement cbx, DwForm dialog)
        {
            var pb = GetOrCreateControl<ProgressBar>(cbx, dialog);
            pb.Style = ProgressBarStyle.Continuous;
            int.TryParse(cbx.GetAttribute("value"), out int val);
            pb.Value = val;
            if (cbx.HasAttribute("width")) pb.Width = int.Parse(cbx.GetAttribute("width"));
            if (cbx.HasAttribute("height")) pb.Height = int.Parse(cbx.GetAttribute("height"));
            dialog.FormBody.Controls.Add(pb);
        }

        private void Parse_drop_down(XmlElement cbx, DwForm dialog)
        {
            var dd = new CbDropBox
            {
                Name = cbx.GetAttribute("id"),
                Text = cbx.GetAttribute("value"),
                content_handler_data = new System.Collections.Hashtable()
            };

            if (cbx.HasAttribute("width")) dd.Width = int.Parse(cbx.GetAttribute("width"));

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

            dialog.FormBody.Controls.Add(dd);
        }

        // =====================================================================
        // Event handlers – label clicks
        // =====================================================================

        private void SpellLabel_Click(object? sender, EventArgs e)
        {
            if (sender is not Label label) return;
            string cmd = (string)label.Tag!;
            ghost.SendText(cmd);

            Form? form = label.FindForm();
            if (form == null) return;
            ResetLabelColors(form, "spells");
            label.ForeColor = linkColor;
            UpdateActionButton(form, "chooseSpell", "Choose " + label.Text, cmd);
        }

        private void FeatLabel_Click(object? sender, EventArgs e)
        {
            if (sender is not Label label) return;
            string cmd = (string)label.Tag!;

            Form? form = label.FindForm();
            if (form == null) return;
            ResetLabelColors(form, "featList");
            label.ForeColor = linkColor;

            if (form.Controls.Find("featInfo", true).FirstOrDefault() is RichTextBox rtb)
                rtb.Clear();

            Control? actionBtn = form.Controls.Find("chooseFeat", true).FirstOrDefault()
                              ?? form.Controls.Find("unlearnFeat", true).FirstOrDefault();
            if (actionBtn is CmdButton btn)
            {
                btn.Text = (btn.Name == "unlearnFeat" ? "Unlearn " : "Choose ") + label.Text;
                btn.cmd_string = cmd;
            }

            ghost.SendText(cmd);
        }

        private void Rtb_LinkClicked(object? sender, LinkClickedEventArgs e)
        {
            if (sender is not RichTextBox rtb) return;
            Point mouse = rtb.PointToClient(Cursor.Position);
            int index = rtb.GetCharIndexFromPosition(mouse);
            int start = rtb.Text.LastIndexOf("<d", index);
            int end = rtb.Text.IndexOf("</d>", index) + 4;
            string chunk = rtb.Text.Substring(start, end - start);

            var doc = new XmlDocument();
            doc.LoadXml("<root>" + chunk + "</root>");
            if (doc.DocumentElement!.FirstChild is XmlElement elem &&
                elem.Name == "d" && elem.HasAttribute("cmd"))
                ghost.SendText(elem.GetAttribute("cmd"));
        }

        // =====================================================================
        // Event handlers – buttons
        // =====================================================================

        public void CbClose(object? sender, EventArgs e)
        {
            if (sender is not CmdButton btn) return;
            var panel = btn.Parent as Panel;
            var dialog = btn.FindForm() as DwForm;
            string cmd = btn.cmd_string ?? string.Empty;
            string ddValue = "";

            if (cmd.Length > 2)
            {
                if (cmd.Contains("%") && panel != null)
                {
                    foreach (Control ctrl in panel.Controls)
                    {
                        switch (ctrl)
                        {
                            case CbRadio rb when rb.Checked:
                                cmd = cmd.Replace("%" + rb.group + "%", rb.command + " ");
                                break;
                            case CbDropBox dd:
                                if (dd.SelectedIndex > -1)
                                    ddValue = (string)dd.content_handler_data[dd.Items[dd.SelectedIndex]]!;
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

                if (btn.Name == "chooseSpell") ghost.SendText(btn.Text + " Spell");
                else if (btn.Name == "chooseFeat" || btn.Name == "unlearnFeat") ghost.SendText(btn.cmd_string!);
                else if (btn.Name == "confirmOK") ghost.SendText(cmd);
                else if (ddValue == "")
                {
                    if (dialog != null) { forms.Remove(dialog); dialog.Close(); }
                    return;
                }
                else ghost.SendText(cmd.Replace(";", "\\;"));
            }

            if (dialog != null) { forms.Remove(dialog); dialog.Close(); }
        }

        public void CbCommand(object? sender, EventArgs e)
        {
            if (sender is not CmdButton btn) return;
            var panel = btn.Parent as Panel;
            string cmd = btn.cmd_string ?? string.Empty;
            string ddValue = "";

            if (cmd.Contains("%") && panel != null)
            {
                foreach (Control ctrl in panel.Controls)
                {
                    switch (ctrl)
                    {
                        case CbRadio rb when rb.Checked:
                            cmd = cmd.Replace("%" + rb.group + "%", rb.command + " ");
                            break;
                        case CbCheckBox cb:
                            cmd = cmd.Replace("%" + ctrl.Name + "%", cb.Value + " ");
                            break;
                        case CbDropBox dd:
                            if (dd.SelectedIndex > -1)
                                ddValue = (string)dd.content_handler_data[dd.Items[dd.SelectedIndex]]!;
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
                    if (btn.Parent is Form f) { forms.Remove((DwForm)f); f.Close(); }
                    return;
                }

                // A token is still unresolved here — nothing was selected to fill it, so
                // there is no valid command to send (the literal %token% just yields
                // "Please rephrase that command"). If the unresolved token is a radio group
                // with nothing checked (e.g. the Available Containers OK, cmd="%containers%"),
                // the plugin closes the window itself, since the game never sends a
                // closeDialog for a command it never received. Any other unresolved token
                // just suppresses the send and leaves the window open for a selection.
                if (cmd.Contains('%'))
                {
                    bool unselectedRadioGroup =
                        panel.Controls.OfType<CbRadio>().Any(r => cmd.Contains("%" + r.group + "%"))
                        && !panel.Controls.OfType<CbRadio>().Any(r => r.Checked);

                    if (unselectedRadioGroup && btn.FindForm() is DwForm uf)
                    {
                        forms.Remove(uf);
                        uf.Close();
                    }
                    return;
                }
            }

            if (cmd.TrimEnd() == "store _set -1")
            {
                cmd = "store _clear 0";
                if (btn.FindForm() is DwForm cf) { forms.Remove(cf); cf.Close(); }
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

        public void CbRadioSelect(object? sender, EventArgs e)
        {
            if (sender is not CbRadio rb) return;
            if (!rb.Checked || !rb.Focused) return;

            foreach (Control ctrl in rb.Parent!.Controls)
                if (ctrl is CbRadio other && other.group == rb.group)
                    other.Checked = (other == rb);

            // If a button in this window confirms this radio group (its cmd references
            // %group%, e.g. the Available Containers OK uses %containers%), selecting the
            // radio is only a choice — it must NOT fire the command, and the bag is set
            // only when OK is clicked. (This also stops the selection from double-sending.)
            // Groups with no such confirm button keep auto-send on select (e.g. injuries).
            bool confirmedByButton = rb.Parent!.Controls.OfType<CmdButton>()
                .Any(b => (b.cmd_string ?? string.Empty).Contains("%" + rb.group + "%"));
            if (confirmedByButton) return;

            if (!string.IsNullOrEmpty(rb.command))
            {
                InjuriesWindow.currentInjuryCommand = rb.command;
                ghost.SendText(rb.command);
            }
        }

        public void Cb_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is not CbDropBox dd) return;
            string cmd = dd.cmd ?? string.Empty;

            if (cmd.Contains("%") && dd.SelectedIndex > -1)
            {
                string val = (string)dd.content_handler_data[dd.Items[dd.SelectedIndex]]!;
                cmd = cmd.Replace("%" + dd.Name + "%", val);
            }

            ghost.SendText(cmd.Replace(";", "\\;"));
            if (cmd.StartsWith("profile /set"))
                ghost.SendText("profile /edit");
        }

        // =====================================================================
        // TextBox change tracker (bug report dialog character counter)
        // =====================================================================

        private void TextBox_TextChanged(object? sender, EventArgs e, DwForm dialog)
        {
            if (FindWindowByName("bugDialogBox") == null) return;
            if (sender is not TextBox tb) return;

            int count = tb.Text.Length;
            // Only show the character counter when the user has actually typed something
            if (count == 0) return;

            Label? lbl;
            int maxChars;
            string labelName;

            switch (tb.Name)
            {
                case "title":
                    lbl = dialog.FormBody.Controls["titleLabel"] as Label;
                    maxChars = 128;
                    labelName = "Title";
                    break;
                case "details":
                    lbl = dialog.FormBody.Controls["detailsLabel"] as Label;
                    maxChars = 875;
                    labelName = "Details";
                    break;
                default: return;
            }

            if (lbl == null) return;
            lbl.Text = $"{labelName} {count}/{maxChars}";
            lbl.AutoSize = true;
            if (!dialog.FormBody.Controls.Contains(lbl))
                dialog.FormBody.Controls.Add(lbl);
        }

        // =====================================================================
        // CloseCommand stub
        // =====================================================================

        public void CloseCommand(Button cb)
        {
            if (cb is null) throw new ArgumentNullException(nameof(cb));
        }

        // =====================================================================
        // Public window utilities
        // =====================================================================

        public DwForm CreateWindow(string id, string title, int width, int height)
        {
            var win = new DwForm(ghost, this)
            {
                Owner = pForm,
                Text = title,
                ForeColor = formfore,
                Name = id,
                ClientSize = new Size(width, height)
            };
            // formBody.Font inherits SystemFonts.DefaultFont from the form — server-positioned
            // controls keep their coordinates. Plugin-managed controls (spell/feat lists, RTBs)
            // set InfoFont / LabelFont / HeaderFont explicitly, which respect the Scale factor.
            win.FormBody.ForeColor = formfore;
            // Render controls in the selected plugin font. The layout engine measures with the
            // same LayoutFont, so measurement and rendering stay consistent. At the default
            // family + Scale 1.0 this is SystemFonts.DefaultFont, identical to before.
            win.FormBody.Font = LayoutFont;
            win.FormClosing += (s, e) =>
            {
                positionList[id] = win.WindowState == FormWindowState.Normal
                    ? win.Location
                    : win.RestoreBounds.Location;
            };
            win.FormClosed += (s, e) => loadSave.Save();

            if (positionList.TryGetValue(id, out Point savedPos))
            {
                win.StartPosition = FormStartPosition.Manual;
                win.Location = savedPos;
            }
            else
            {
                win.StartPosition = FormStartPosition.CenterScreen;
            }

            forms.Add(win);
            return win;
        }

        //public DwForm CreateSkinnedWindow(string id, string title, int width, int height)
        //    => CreateWindow(id, title, width, height);

        public DwForm? FindWindowByName(string name)
        {
            foreach (DwForm win in forms)
                if (win.Name == name && !win.IsDisposed)
                    return win;
            return null;
        }

        public void CloseWindowIfOpen(string name)
        {
            var win = FindWindowByName(name);
            if (win == null) return;
            forms.Remove(win);
            win.Close();
        }

        public Button AddHelpCloseRow(DwForm win)
        {
            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = S(40),
                Padding = new Padding(S(6)),
                BackColor = formback,
            };
            var close = new Button { Text = "Close", AutoSize = true };
            close.Click += (s, e) => win.Close();
            bottom.Controls.Add(close);
            win.FormBody.Controls.Add(bottom);
            win.CancelButton = close;   // Esc closes
            return close;
        }

        // =====================================================================
        // Private utility methods
        // =====================================================================

        private T GetOrCreateControl<T>(XmlElement cbx, DwForm dialog) where T : Control, new()
        {
            string id = cbx.GetAttribute("id");
            if (dialog.FormBody.Controls.ContainsKey(id) && dialog.FormBody.Controls[id] is T existing)
                return existing;
            return new T { Name = id };
        }

        private Label MakeClickableLabel(string text, string cmd, Point location, EventHandler clickHandler)
        {
            var lbl = new Label
            {
                Text = text,
                AutoSize = true,
                Location = location,
                ForeColor = formfore,
                Font = LabelFont,
                Tag = cmd
            };
            lbl.Click += clickHandler;
            return lbl;
        }

        private void ResetLabelColors(Form form, string panelName)
        {
            var panel = form.Controls.Find(panelName, true).FirstOrDefault();
            if (panel == null) return;
            foreach (var lbl in panel.Controls.OfType<Label>())
                lbl.ForeColor = formfore;
        }

        private void UpdateActionButton(Form form, string buttonName, string text, string cmd)
        {
            if (form.Controls.Find(buttonName, true).FirstOrDefault() is CmdButton btn)
            {
                btn.Text = text;
                btn.cmd_string = cmd;
            }
        }

        private void AutoFitDialog(DwForm dialog, int padRight = 12, int padBottom = 12)
        {
            int maxRight = 0;
            int maxBottom = 0;

            foreach (Control ctrl in dialog.FormBody.Controls)
            {
                if (ctrl.Right > maxRight) maxRight = ctrl.Right;
                if (ctrl.Bottom > maxBottom) maxBottom = ctrl.Bottom;
            }

            int neededWidth = maxRight + padRight;
            int neededHeight = maxBottom + padBottom;

            // Only grow the window if controls genuinely overflow the current size.
            // Never shrink below what the server specified (already scaled in Parse_xml_openwindow).
            // The clamp in SetLocation means controls shouldn't overflow, but guard anyway.
            if (neededWidth > dialog.FormBody.Width)
                dialog.ClientSize = new Size(neededWidth, dialog.ClientSize.Height);
            if (neededHeight > dialog.FormBody.Height)
                dialog.ClientSize = new Size(dialog.ClientSize.Width, neededHeight + 22);
        }

        // Standalone deep copy of a dialogData container, detached from the parse document
        // so it can be retained on dialog.Tag and merged with later partial updates.
        private static XmlElement CloneContainer(XmlElement source)
        {
            var doc = new XmlDocument();
            var copy = (XmlElement)doc.ImportNode(source, true);
            doc.AppendChild(copy);
            return copy;
        }

        // Merge a partial dialogData update into the retained container: a child replaces
        // an existing element with the same id, and a brand-new id is appended. This lets
        // the layout engine re-run over the full control set on a partial update.
        private static void MergeContainer(XmlElement target, XmlElement update)
        {
            foreach (XmlElement child in update.ChildNodes.OfType<XmlElement>())
            {
                var imported = (XmlElement)target.OwnerDocument.ImportNode(child, true);
                string cid = child.GetAttribute("id");

                XmlElement? existing = string.IsNullOrEmpty(cid) ? null
                    : target.ChildNodes.OfType<XmlElement>()
                        .FirstOrDefault(e => e.GetAttribute("id") == cid);

                if (existing != null)
                    target.ReplaceChild(imported, existing);
                else
                    target.AppendChild(imported);
            }
        }

        // =====================================================================
        // Layout helpers — all server coords run through S() here
        // =====================================================================

        private Size BuildSize(XmlElement cbx, int defaultWidth, int defaultHeight)
        {
            int w = cbx.HasAttribute("width") ? int.Parse(cbx.GetAttribute("width")) : defaultWidth;
            int h = cbx.HasAttribute("height") ? int.Parse(cbx.GetAttribute("height")) : defaultHeight;
            return new Size(S(w), S(h));
        }

        private Point SetLocation(XmlElement cbx, Control ctrl, DwForm parent)
        {
            int.TryParse(cbx.GetAttribute("top"), out int rawTop);
            int.TryParse(cbx.GetAttribute("left"), out int rawLeft);

            int top;
            int left;

            if (cbx.HasAttribute("align"))
            {
                // align= uses top/left as small signed offsets from the alignment point.
                // Scale those offsets so they stay proportional.
                top = S(rawTop);
                left = S(rawLeft);

                switch (cbx.GetAttribute("align"))
                {
                    case "center":
                        top = parent.FormBody.Height / 2 - ctrl.ClientSize.Height / 2 + top;
                        left = parent.FormBody.Width / 2 - ctrl.ClientSize.Width / 2 + left;
                        break;
                    case "s":
                        ctrl.Anchor = AnchorStyles.Bottom;
                        left = parent.FormBody.Width / 2 - ctrl.ClientSize.Width / 2 + left;
                        break;
                    case "se": ctrl.Anchor = AnchorStyles.Bottom | AnchorStyles.Right; break;
                    case "sw": ctrl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left; break;
                    case "n":
                        ctrl.Anchor = AnchorStyles.Top;
                        left = parent.FormBody.Width / 2 - ctrl.ClientSize.Width / 2 + left;
                        break;
                    case "ne": ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Right; break;
                    case "nw": ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Left; break;
                }
            }
            else if (cbx.HasAttribute("anchor_left"))
            {
                // anchor_left: position relative to the right edge of another control.
                // The anchor control's position is already correctly scaled (it was placed
                // by SetLocation). rawLeft/rawTop are small pixel gaps — scale them.
                Control anchor = parent.FormBody.Controls[cbx.GetAttribute("anchor_left")]!;
                left = anchor.Left + anchor.Width + S(rawLeft) + S(5);
                top = rawTop == 0 ? anchor.Top : anchor.Top + S(rawTop);
            }
            else if (cbx.HasAttribute("anchor_right"))
            {
                Control anchor = parent.FormBody.Controls[cbx.GetAttribute("anchor_right")]!;
                left = anchor.Left - S(rawLeft) - ctrl.Width - S(5);
                top = rawTop == 0 ? anchor.Top : anchor.Top + S(rawTop);
            }
            else
            {
                // Plain absolute coordinates — scale them directly.
                top = S(rawTop);
                left = S(rawLeft);
            }

            if (cbx.HasAttribute("anchor_top"))
            {
                Control anchor = parent.FormBody.Controls[cbx.GetAttribute("anchor_top")]!;
                top = anchor.Bottom + S(2) + (rawTop != 0 ? S(rawTop) : 0);
            }

            // Negative values = offset from the far edge of the scaled window
            if (top < 0) top = parent.FormBody.Height - ctrl.Height + top;
            if (left < 0) left = parent.FormBody.Width - ctrl.Width + left;

            // Clamp to window bounds — do NOT expand the window for overflow.
            // The server sized the window for its layout; we scaled it; it should fit.
            top = Math.Max(0, Math.Min(top, parent.FormBody.Height - ctrl.Height));
            left = Math.Max(0, Math.Min(left, parent.FormBody.Width - ctrl.Width));

            return new Point(left, top);
        }
    }
}
