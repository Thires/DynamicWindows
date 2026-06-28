using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace DynamicWindows
{
    public class LoadSave
    {
        private readonly Plugin plugin;
        private readonly string configPath;

        // ── Defaults ─────────────────────────────────────────────────────────
        private static readonly Color DefaultFore = Color.White;
        private static readonly Color DefaultBack = Color.Black;
        private const bool DefaultStow = false;
        private const bool DefaultEnabled = true;
        private const bool DefaultDisableOther = true;
        private const bool DefaultDisableSelf = true;
        private const float DefaultScale = 1.0f;
        private static readonly string DefaultFontFamily = SystemFonts.DefaultFont.Name;
        private const FontStyle DefaultFontStyle = FontStyle.Regular;
        private static readonly Color DefaultLinkColor = Color.Blue;
        private static readonly Color DefaultTimerColor = Color.RoyalBlue;
        private static readonly Color DefaultTimerBackColor = Color.Black;
        private static readonly Color DefaultTimerTextColor = Color.White;

        public LoadSave(Plugin plugin, string configPath, string characterName)
        {
            this.plugin = plugin;
            this.configPath = configPath;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string CharName =>
            NormaliseName(plugin.ghost?.get_Variable("charactername") ?? "Default");

        private static string NormaliseName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Default";
            return char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant();
        }

        // ── Load ──────────────────────────────────────────────────────────────

        public void Load()
        {
            string filePath = Path.Combine(configPath, "DynamicWindows.xml");
            if (!File.Exists(filePath)) return;

            var xml = new XmlDocument();
            xml.Load(filePath);

            // 1. Start from hard-coded defaults
            plugin.formfore = DefaultFore;
            plugin.formback = DefaultBack;
            plugin.bStowContainer = DefaultStow;
            plugin.bPluginEnabled = DefaultEnabled;
            plugin.bDisableOtherInjuries = DefaultDisableOther;
            plugin.bDisableSelfInjuries = DefaultDisableSelf;
            plugin.Scale = DefaultScale;
            plugin.FontFamilyName = DefaultFontFamily;
            plugin.FontStyleChoice = DefaultFontStyle;
            plugin.linkColor = DefaultLinkColor;
            plugin.timerBarColor = DefaultTimerColor;
            plugin.timerBarBackColor = DefaultTimerBackColor;
            plugin.timerBarTextColor = DefaultTimerTextColor;
            plugin.ignorelist.Clear();
            plugin.positionList.Clear();

            // 2. Apply global defaults from XML (id has no dot)
            ApplyConfigs(xml, "");

            // 3. Apply per-character overrides (id prefixed with "CharName.")
            string prefix = CharName + ".";
            ApplyConfigs(xml, prefix);

            // Load ignore list
            foreach (XmlElement ignore in xml.GetElementsByTagName("Ignore"))
            {
                string id = ignore.GetAttribute("id");
                string? cleanId = StripPrefix(id, prefix);
                if (cleanId != null)
                    plugin.ignorelist.Add(cleanId);
            }

            // Load window positions — preserve any already-open windows first
            foreach (DwForm window in plugin.forms)
                plugin.positionList[window.Name] = window.Location;

            foreach (XmlElement pos in xml.GetElementsByTagName("Position"))
            {
                string id = pos.GetAttribute("id");
                string? cleanId = StripPrefix(id, prefix);
                if (cleanId == null) continue;

                if (int.TryParse(pos.GetAttribute("X"), out int x) &&
                    int.TryParse(pos.GetAttribute("Y"), out int y))
                {
                    plugin.positionList[cleanId] = new Point(x, y);
                }
            }
        }

        private void ApplyConfigs(XmlDocument xml, string prefix)
        {
            foreach (XmlElement element in xml.GetElementsByTagName("Config"))
            {
                string id = element.GetAttribute("id");
                string? cleanId = StripPrefix(id, prefix);
                if (cleanId == null) continue;

                switch (cleanId)
                {
                    case "foreground":
                        plugin.formfore = ColorTranslator.FromHtml(element.GetAttribute("color"));
                        break;
                    case "background":
                        plugin.formback = ColorTranslator.FromHtml(element.GetAttribute("color"));
                        break;
                    case "stowcontainer":
                        bool.TryParse(element.GetAttribute("enabled"), out plugin.bStowContainer);
                        break;
                    case "plugin":
                        bool.TryParse(element.GetAttribute("pluginenabled"), out plugin.bPluginEnabled);
                        break;
                    case "disableOtherInjuries":
                        bool.TryParse(element.GetAttribute("otherenabled"), out plugin.bDisableOtherInjuries);
                        break;
                    case "disableSelfInjuries":
                        bool.TryParse(element.GetAttribute("selfenabled"), out plugin.bDisableSelfInjuries);
                        break;
                    case "uiScale":
                        if (float.TryParse(element.GetAttribute("value"),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float scale))
                            plugin.Scale = Math.Max(1.0f, Math.Min(1.5f, scale));
                        break;
                    case "fontFamily":
                        {
                            string fam = element.GetAttribute("name");
                            if (!string.IsNullOrWhiteSpace(fam)) plugin.FontFamilyName = fam;
                            string st = element.GetAttribute("style");
                            if (!string.IsNullOrWhiteSpace(st) &&
                                Enum.TryParse(st, out FontStyle fs))
                                plugin.FontStyleChoice = fs;
                        }
                        break;
                    case "linkColor":
                        plugin.linkColor = ColorTranslator.FromHtml(element.GetAttribute("color"));
                        break;
                    case "timerColor":
                        plugin.timerBarColor = ColorTranslator.FromHtml(element.GetAttribute("color"));
                        break;
                    case "timerBackColor":
                        plugin.timerBarBackColor = ColorTranslator.FromHtml(element.GetAttribute("color"));
                        break;
                    case "timerTextColor":
                        plugin.timerBarTextColor = ColorTranslator.FromHtml(element.GetAttribute("color"));
                        break;
                }
            }
        }

        private static string? StripPrefix(string id, string prefix)
        {
            if (prefix == "")
            {
                // Global entry: id must contain no dot
                return id.Contains('.') ? null : id;
            }
            if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return id.Substring(prefix.Length);
            return null;
        }

        // ── Save ──────────────────────────────────────────────────────────────

        public void Save()
        {
            string filePath = Path.Combine(configPath, "DynamicWindows.xml");
            var xml = new XmlDocument();
            XmlElement root;

            if (File.Exists(filePath))
            {
                xml.Load(filePath);
                root = xml.DocumentElement!;
            }
            else
            {
                root = xml.CreateElement("DynamicWindows");
                xml.AppendChild(root);
            }

            string prefix = CharName + ".";

            // Remove only this character's existing entries
            RemoveByPrefix(root, prefix);

            // Collect current window positions
            foreach (DwForm window in plugin.forms)
                plugin.positionList[window.Name] = window.Location;

            // Save per-character overrides — only values that differ from defaults
            if (plugin.formfore != DefaultFore)
                AddConfig(xml, root, prefix, "foreground", "color", ColorTranslator.ToHtml(plugin.formfore));
            if (plugin.formback != DefaultBack)
                AddConfig(xml, root, prefix, "background", "color", ColorTranslator.ToHtml(plugin.formback));
            if (plugin.bStowContainer != DefaultStow)
                AddConfig(xml, root, prefix, "stowcontainer", "enabled", plugin.bStowContainer.ToString());
            if (plugin.bPluginEnabled != DefaultEnabled)
                AddConfig(xml, root, prefix, "plugin", "pluginenabled", plugin.bPluginEnabled.ToString());
            if (plugin.bDisableOtherInjuries != DefaultDisableOther)
                AddConfig(xml, root, prefix, "disableOtherInjuries", "otherenabled", plugin.bDisableOtherInjuries.ToString());
            if (plugin.bDisableSelfInjuries != DefaultDisableSelf)
                AddConfig(xml, root, prefix, "disableSelfInjuries", "selfenabled", plugin.bDisableSelfInjuries.ToString());
            if (Math.Abs(plugin.Scale - DefaultScale) > 0.001f)
                AddConfig(xml, root, prefix, "uiScale", "value",
                    plugin.Scale.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            if (!string.Equals(plugin.FontFamilyName, DefaultFontFamily, StringComparison.Ordinal)
                || plugin.FontStyleChoice != DefaultFontStyle)
            {
                XmlElement cfg = xml.CreateElement("Config");
                cfg.SetAttribute("id", prefix + "fontFamily");
                cfg.SetAttribute("name", plugin.FontFamilyName);
                cfg.SetAttribute("style", plugin.FontStyleChoice.ToString());
                root.AppendChild(cfg);
            }
            if (plugin.linkColor != DefaultLinkColor)
                AddConfig(xml, root, prefix, "linkColor", "color", ColorTranslator.ToHtml(plugin.linkColor));
            if (plugin.timerBarColor != DefaultTimerColor)
                AddConfig(xml, root, prefix, "timerColor", "color", ColorTranslator.ToHtml(plugin.timerBarColor));
            if (plugin.timerBarBackColor != DefaultTimerBackColor)
                AddConfig(xml, root, prefix, "timerBackColor", "color", ColorTranslator.ToHtml(plugin.timerBarBackColor));
            if (plugin.timerBarTextColor != DefaultTimerTextColor)
                AddConfig(xml, root, prefix, "timerTextColor", "color", ColorTranslator.ToHtml(plugin.timerBarTextColor));

            // Ignore list
            foreach (string id in plugin.ignorelist)
            {
                XmlElement ign = xml.CreateElement("Ignore");
                ign.SetAttribute("id", prefix + id);
                root.AppendChild(ign);
            }

            // Window positions — skip injuries-NNN entries
            foreach (var pair in plugin.positionList)
            {
                if (pair.Key.StartsWith("injuries-")) continue;

                XmlElement pos = xml.CreateElement("Position");
                pos.SetAttribute("id", prefix + pair.Key);
                pos.SetAttribute("X", pair.Value.X.ToString());
                pos.SetAttribute("Y", pair.Value.Y.ToString());
                root.AppendChild(pos);
            }

            xml.Save(filePath);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void RemoveByPrefix(XmlElement root, string prefix)
        {
            var toRemove = new List<XmlNode>();
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node is XmlElement el && el.HasAttribute("id") &&
                    el.GetAttribute("id").StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    toRemove.Add(el);
            }
            foreach (XmlNode node in toRemove)
                root.RemoveChild(node);
        }

        private static void AddConfig(XmlDocument xml, XmlElement root,
                                      string prefix, string id, string attr, string val)
        {
            XmlElement cfg = xml.CreateElement("Config");
            cfg.SetAttribute("id", prefix + id);
            cfg.SetAttribute(attr, val);
            root.AppendChild(cfg);
        }

        public bool IsIgnored(string fullId)
        {
            string prefix = CharName + ".";
            string cleanId = fullId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? fullId.Substring(prefix.Length)
                : fullId;
            return plugin.ignorelist.Any(x => x.Equals(cleanId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
