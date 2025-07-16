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
        private readonly string characterName;

        public LoadSave(Plugin plugin, string configPath, string characterName)
        {
            this.plugin = plugin;
            this.configPath = configPath;
            this.characterName = characterName;
        }

        public void Load()
        {
            string filePath = Path.Combine(configPath, "DynamicWindows.xml");
            if (!File.Exists(filePath))
                return;

            XmlDocument xml = new XmlDocument();
            xml.Load(filePath);

            string characterName = plugin.ghost.get_Variable("charactername") ?? "Default";
            string prefix = characterName + ".";

            // Reset global state so old character settings don't carry over
            plugin.formfore = Color.White;
            plugin.formback = Color.Black;
            plugin.bStowContainer = false;
            plugin.bPluginEnabled = true;
            plugin.bDisableOtherInjuries = true;
            plugin.bDisableSelfInjuries = true;
            plugin.ignorelist.Clear();
            plugin.positionList.Clear();

            // Load configs
            foreach (XmlElement element in xml.GetElementsByTagName("Config"))
            {
                string id = element.GetAttribute("id");
                if (!id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string key = id.Substring(prefix.Length);

                switch (key)
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
                }
            }

            // Load ignore list
            foreach (XmlElement ignore in xml.GetElementsByTagName("Ignore"))
            {
                string id = ignore.GetAttribute("id");
                if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    plugin.ignorelist.Add(id.Substring(prefix.Length));
                }
            }

            foreach (SkinnedMDIChild window in this.plugin.forms)
            {
                this.plugin.positionList[window.Name] = window.Location;
            }

            // Load window positions
            foreach (XmlElement pos in xml.GetElementsByTagName("Position"))
            {
                string id = pos.GetAttribute("id");
                if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string cleanId = id.Substring(prefix.Length);
                    if (int.TryParse(pos.GetAttribute("X"), out int x) &&
                        int.TryParse(pos.GetAttribute("Y"), out int y))
                    {
                        plugin.positionList[cleanId] = new Point(x, y);
                    }
                }
            }
        }

        public void Save()
        {
            string filePath = Path.Combine(configPath, "DynamicWindows.xml");
            XmlDocument xml = new XmlDocument();
            XmlElement root;

            if (File.Exists(filePath))
            {
                xml.Load(filePath);
                root = xml.DocumentElement;
            }
            else
            {
                root = xml.CreateElement("DynamicWindows");
                xml.AppendChild(root);
            }

            // Get current character prefix
            string characterName = plugin.ghost.get_Variable("charactername") ?? "Default";
            string prefix = characterName + ".";

            // Only remove nodes for current character
            List<XmlNode> toRemove = new List<XmlNode>();
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node is XmlElement el && el.HasAttribute("id") && el.GetAttribute("id").StartsWith(prefix))
                {
                    toRemove.Add(el);
                }
            }
            foreach (XmlNode node in toRemove)
            {
                root.RemoveChild(node);
            }

            // Add new character-specific settings
            void AddConfig(string id, string attr, string val)
            {
                XmlElement cfg = xml.CreateElement("Config");
                cfg.SetAttribute("id", prefix + id);
                cfg.SetAttribute(attr, val);
                root.AppendChild(cfg);
            }

            AddConfig("foreground", "color", ColorTranslator.ToHtml(plugin.formfore));
            AddConfig("background", "color", ColorTranslator.ToHtml(plugin.formback));
            AddConfig("stowcontainer", "enabled", plugin.bStowContainer.ToString());
            AddConfig("plugin", "pluginenabled", plugin.bPluginEnabled.ToString());
            AddConfig("disableOtherInjuries", "otherenabled", plugin.bDisableOtherInjuries.ToString());
            AddConfig("disableSelfInjuries", "selfenabled", plugin.bDisableSelfInjuries.ToString());

            foreach (string id in plugin.ignorelist)
            {
                XmlElement ign = xml.CreateElement("Ignore");
                ign.SetAttribute("id", prefix + id);
                root.AppendChild(ign);
            }

            foreach (Form f in plugin.forms)
            {
                if (!plugin.positionList.ContainsKey(f.Name))
                    plugin.positionList[f.Name] = f.Location;
            }

            foreach (SkinnedMDIChild window in this.plugin.forms)
            {
                this.plugin.positionList[window.Name] = window.Location;
            }

            foreach (var pair in plugin.positionList)
            {
                // don't other injuries-numbers positions
                if (!pair.Key.StartsWith("injuries-"))
                {
                    XmlElement pos = xml.CreateElement("Position");
                    pos.SetAttribute("id", prefix + pair.Key);
                    pos.SetAttribute("X", pair.Value.X.ToString());
                    pos.SetAttribute("Y", pair.Value.Y.ToString());
                    root.AppendChild(pos);
                }
            }

            xml.Save(Path.Combine(configPath, "DynamicWindows.xml"));
        }

        public bool IsIgnored(string fullId)
        {
            string characterName = plugin.ghost.get_Variable("charactername") ?? "Default";
            string expectedPrefix = characterName + ".";

            // Only strip if it's prefixed — fallback to raw ID otherwise
            string cleanId = fullId.StartsWith(expectedPrefix)
                ? fullId.Substring(expectedPrefix.Length)
                : fullId;

            return plugin.ignorelist.Cast<string>().Any(x => x.Equals(cleanId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
