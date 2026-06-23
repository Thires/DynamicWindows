using GeniePlugin.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace DynamicWindows
{
    public class ShopWindow
    {
        public enum ScanStatusEnum { None, Shop, Surface, Item, Read }

        private readonly Plugin plugin;

        private DwForm? window;
        private TreeView tree = null!;
        private Label statusLabel = null!;
        private TextBox detailBox = null!;
        private Button buyButton = null!;
        private Button appraiseButton = null!;
        private Button wikiButton = null!;
        private Button shopButton = null!;

        private TreeNode? currentSurface;
        private TreeNode? currentItem;

        // Surface-note capture: a note (e.g. "A note on the table reads: ...") sometimes
        // follows the item list, before the "[Type SHOP [GOOD]...]" marker. Keyed by surface
        // command so each surface keeps its own; shown in the detail pane.
        private readonly Dictionary<string, string> surfaceNotes = new();
        private readonly List<string> currentSurfaceItems = new();  // for filtering item lines
        private readonly List<string> noteBuffer = new();
        private string pendingSurfaceCmd = "";

        public ScanStatusEnum ScanStatus { get; private set; } = ScanStatusEnum.None;

        public ShopWindow(Plugin plugin) => this.plugin = plugin;

        // ── Trigger ──────────────────────────────────────────────────────────

        public void Open()
        {
            EnsureWindow();
            window!.ShowForm();
            Reset();
            ScanStatus = ScanStatusEnum.Shop;
            plugin.ghost.SendText("shop");
        }

        private void Reset()
        {
            currentItem = null;
            currentSurface = null;
            surfaceNotes.Clear();
            currentSurfaceItems.Clear();
            noteBuffer.Clear();
            pendingSurfaceCmd = "";
            RunOnUi(() =>
            {
                tree.Nodes.Clear();
                statusLabel.Text = "";
                detailBox.Clear();
            });
        }

        // ── Window build ─────────────────────────────────────────────────────

        private void EnsureWindow()
        {
            if (window != null && !window.IsDisposed) return;

            window = new DwForm(plugin.ghost, plugin)
            {
                Owner = plugin.pForm,
                Text = "Shop Window",
                Name = "shopWindow",
                ClientSize = new Size(plugin.S(640), plugin.S(440)),
                StartPosition = FormStartPosition.Manual,
                Location = plugin.positionList.TryGetValue("shopWindow", out Point saved)
                    ? saved : new Point(150, 150),
            };
            window.FormBody.BackColor = plugin.formback;
            window.FormBody.ForeColor = plugin.formfore;
            window.FormBody.Font = plugin.LayoutFont;
            window.LocationChanged += delegate
            {
                if (window!.WindowState == FormWindowState.Normal)
                    plugin.positionList["shopWindow"] = window.Location;
            };
            window.FormClosed += delegate
            {
                ScanStatus = ScanStatusEnum.None;
                window = null;
            };

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = plugin.formback,
            };

            statusLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = plugin.S(16),
                AutoSize = false,
                BackColor = plugin.formback,
                ForeColor = plugin.formfore,
                Text = "",
            };
            tree = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = plugin.formback,
                ForeColor = plugin.formfore,
                HideSelection = false,
                Font = plugin.LayoutFont,
            };
            tree.BeforeExpand += Tree_BeforeExpand;
            tree.AfterSelect += Tree_AfterSelect;
            // Right-click selects the node under the cursor, then shows the copy menu.
            tree.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var n = tree.GetNodeAt(e.Location);
                    if (n != null) tree.SelectedNode = n;
                }
            };
            tree.ContextMenuStrip = BuildCopyMenu();
            split.Panel1.Controls.Add(tree);
            split.Panel1.Controls.Add(statusLabel);

            var buttonRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = plugin.S(32),
                BackColor = plugin.formback,
            };
            buyButton = MakeButton("Buy", Buy_Click);
            appraiseButton = MakeButton("Appraise", Appraise_Click);
            wikiButton = MakeButton("Wiki Lookup", Wiki_Click);
            shopButton = MakeButton("Shop Again", Shop_Click);
            buttonRow.Controls.AddRange(new Control[] { buyButton, appraiseButton, wikiButton, shopButton });

            detailBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = plugin.formback,
                ForeColor = plugin.formfore,
                BorderStyle = BorderStyle.None,
                Font = plugin.LayoutFont,
            };
            split.Panel2.Controls.Add(detailBox);
            split.Panel2.Controls.Add(buttonRow);

            window.FormBody.Controls.Add(split);
            try { split.SplitterDistance = plugin.S(260); } catch { /* min-size guard */ }

            plugin.forms.Add(window);
        }

        private Button MakeButton(string text, EventHandler onClick)
        {
            var b = new Button { Text = text, AutoSize = true, Margin = new Padding(plugin.S(3)) };
            b.Click += onClick;
            return b;
        }

        // ── Tree population ───────────────────────────────────────────────────

        public void AddSurface(string command, string text)
        {
            RunOnUi(() =>
            {
                var node = new TreeNode(text) { Tag = command };
                node.Nodes.Add("");   // placeholder makes the node expandable -> BeforeExpand
                tree.Nodes.Add(node);
            });
        }

        public void AddItem(string command, string text)
        {
            currentSurfaceItems.Add(text);   // remembered so note capture can skip item lines
            RunOnUi(() =>
            {
                if (currentSurface == null) return;

                // Remove the "" placeholder (Tag == null) now that real items are arriving.
                for (int i = currentSurface.Nodes.Count - 1; i >= 0; i--)
                    if (currentSurface.Nodes[i].Tag == null)
                        currentSurface.Nodes.RemoveAt(i);

                currentSurface.Nodes.Add(new TreeNode(text) { Tag = command });
                currentSurface.Expand();   // real children now -> BeforeExpand won't refetch
            });
        }

        // ── Parse hooks (called from Plugin.ParseXML / ParseText) ─────────────

        public void HandleXml(XmlElement elem)
        {
            if (elem.Name == "prompt")
            {
                // End of a listing block — stop adding surface/item nodes.
                if (ScanStatus == ScanStatusEnum.Shop || ScanStatus == ScanStatusEnum.Surface)
                    ScanStatus = ScanStatusEnum.None;
                return;
            }

            if (ScanStatus != ScanStatusEnum.Shop && ScanStatus != ScanStatusEnum.Surface) return;
            if (elem.Name != "d") return;

            string cmd = elem.GetAttribute("cmd");
            if (!cmd.StartsWith("shop") || cmd == "shop") return;

            if (ScanStatus == ScanStatusEnum.Shop) AddSurface(cmd, elem.InnerText);
            else AddItem(cmd, elem.InnerText);
        }

        public void HandleText(string text, string window)
        {
            if (ScanStatus == ScanStatusEnum.Item && (window == "main" || window == "shopwindow"))
            {
                string t = text.Trim('\n', '\r', ' ');
                if (t.EndsWith(">"))
                    ScanStatus = ScanStatusEnum.None;   // hit the prompt — detail block done
                else if (!string.IsNullOrEmpty(text) &&
                         !text.StartsWith("You can buy this item if you like"))
                    AddDetail(t);
            }
            else if (ScanStatus == ScanStatusEnum.Shop)
            {
                if (text.Trim('\n', '\r', ' ') == "There is nothing to buy here.")
                {
                    ScanStatus = ScanStatusEnum.None;
                    RunOnUi(() => statusLabel.Text = "There is nothing to buy here.");
                }
            }
            else if (ScanStatus == ScanStatusEnum.Surface)
            {
                string t = text.Trim('\n', '\r', ' ');
                if (t.StartsWith("[Type SHOP [GOOD]")) { FinalizeSurfaceNote(); return; }
                if (t.Length == 0 || t.StartsWith("[")) return;             // blanks / other markers
                if (Regex.IsMatch(t, @"^(On|In) the .*you see:")) return;        // surface header
                if (currentSurfaceItems.Any(it => t.StartsWith(it))) return;     // item label line
                noteBuffer.Add(t);                                              // -> the note/message
            }
        }

        // Defined positionally: the note is whatever non-item, non-marker text sits between the
        // item list and the "[Type SHOP [GOOD]...]" marker — so it works even when the message
        // doesn't begin with "A note on the ...".
        private void FinalizeSurfaceNote()
        {
            if (noteBuffer.Count == 0) return;
            string note = string.Join(Environment.NewLine, noteBuffer);
            noteBuffer.Clear();
            if (!string.IsNullOrWhiteSpace(pendingSurfaceCmd))
                surfaceNotes[pendingSurfaceCmd] = note;
            RunOnUi(() => detailBox.Text = note);   // show it now — the surface was just expanded
        }

        private void AddDetail(string text) => RunOnUi(() => detailBox.AppendText(text + Environment.NewLine));

        // ── Tree events ───────────────────────────────────────────────────────

        private void Tree_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            // Only act on un-loaded surfaces (their single child is the "" placeholder).
            if (e.Node?.FirstNode == null || e.Node.FirstNode.Text != "") return;

            // Cancel the visual expand and KEEP the placeholder. The node re-expands itself
            // when items arrive (AddItem). If the command is blocked (roundtime), the
            // placeholder stays so the branch keeps its [+] and is simply retryable — the
            // original cleared it here, which is what left a roundtimed branch un-expandable.
            e.Cancel = true;
            currentSurface = e.Node;
            pendingSurfaceCmd = e.Node.Tag?.ToString() ?? "";
            currentSurfaceItems.Clear();
            noteBuffer.Clear();
            ScanStatus = ScanStatusEnum.Surface;
            plugin.ghost.SendText(pendingSurfaceCmd);
        }

        private void Tree_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            string tag = e.Node?.Tag?.ToString() ?? "";
            // Items carry a second '#': "shop #item in #surface". Surfaces have only one.
            bool isItem = tag.IndexOf("#", Math.Min(10, tag.Length)) > 0;
            if (isItem)
            {
                RunOnUi(() => detailBox.Clear());
                currentItem = e.Node;
                ScanStatus = ScanStatusEnum.Item;
                plugin.ghost.SendText(tag);
            }
            else
            {
                // Surface node selected — show its captured note (or clear if it has none).
                currentItem = null;
                string note = surfaceNotes.TryGetValue(tag, out var n) ? n : "";
                RunOnUi(() => detailBox.Text = note);
            }
        }

        // ── Buttons ───────────────────────────────────────────────────────────

        private void Buy_Click(object? sender, EventArgs e)
        {
            Match? m = ItemMatch();
            if (m == null) return;
            plugin.ghost.SendText($"buy {m.Groups[1]} from {m.Groups[3]}");
            plugin.ghost.EchoText("Purchased " + (GetTap() ?? ""));
        }

        private void Appraise_Click(object? sender, EventArgs e)
        {
            Match? m = ItemMatch();
            if (m == null) return;
            plugin.ghost.SendText($"appraise {m.Groups[1]} from {m.Groups[3]} careful");
        }

        private void Wiki_Click(object? sender, EventArgs e)
        {
            string? tap = GetTap();
            if (tap == null) return;
            tap = Regex.Replace(tap, @"^(an|a|some|several)\s", "");

            const string format = "https://elanthipedia.play.net/index.php?title=Special:Ask&q={0}" +
                "&p=format%3Dbroadtable%2Flink%3Dall%2Fheaders%3Dshow%2Fsearchlabel%3D...-20further-20results" +
                "%2Fclass%3Dsortable-20wikitable-20smwtable&eq=no#";

            var categories = new List<string> { "Weapon", "Armor", "Item", "Shield" };
            string query = string.Join("+OR+",
                categories.Select(c => $"[[{c}%3A{tap.Replace(" ", "+")}]]"));

            try
            {
                Process.Start(new ProcessStartInfo(string.Format(format, query)) { UseShellExecute = true });
            }
            catch { /* no browser / blocked — ignore */ }
        }

        private void Shop_Click(object? sender, EventArgs e)
        {
            plugin.ghost.SendText($"shop window");
        }

        private Match? ItemMatch()
        {
            if (currentItem == null) return null;
            var m = Regex.Match(currentItem.Tag?.ToString() ?? "", @"^shop (#\d*) (\w*) (#\d*)$");
            return m.Success ? m : null;
        }

        private string? GetTap()
        {
            if (currentItem == null) return null;
            var m = Regex.Match(currentItem.Text,
                "^(.*) for (\\d*\\.?\\d*)(k)? ([A-z0-9'\\-`\" ]+)$");
            return m.Success ? m.Groups[1].Value : null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        // ── Copy support ──────────────────────────────────────────────────────

        private ContextMenuStrip BuildCopyMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Copy selected item", null, (s, e) => CopySelectedItem(tree.SelectedNode));
            menu.Items.Add("Copy surface", null, (s, e) => CopySelectedSurface(tree.SelectedNode));
            menu.Items.Add("Copy all surfaces", null, (s, e) => CopyAll());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Copy detail pane", null, (s, e) => CopyToClipboard(detailBox.Text));
            return menu;
        }

        private static void CopySelectedItem(TreeNode? node)
        {
            if (node != null) CopyToClipboard(node.Text);
        }

        // Copy a surface and its items. If an item is selected, copy its parent surface branch.
        private static void CopySelectedSurface(TreeNode? node)
        {
            if (node == null) return;
            TreeNode surface = node.Parent ?? node;   // a top-level node is the surface itself
            var sb = new StringBuilder();
            AppendBranch(sb, surface);
            CopyToClipboard(sb.ToString().TrimEnd());
        }

        private void CopyAll()
        {
            var sb = new StringBuilder();
            foreach (TreeNode surface in tree.Nodes)
            {
                AppendBranch(sb, surface);
                sb.AppendLine();
            }
            CopyToClipboard(sb.ToString().TrimEnd());
        }

        private static void AppendBranch(StringBuilder sb, TreeNode surface)
        {
            sb.AppendLine(surface.Text);
            foreach (TreeNode item in surface.Nodes)
                if (item.Tag != null)   // skip the "" placeholder on un-expanded surfaces
                    sb.AppendLine("    " + item.Text);
        }

        private static void CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try { Clipboard.SetText(text); } catch { /* clipboard busy/unavailable */ }
        }

        private void RunOnUi(Action action)
        {
            if (window == null || window.IsDisposed) return;
            if (window.InvokeRequired) window.Invoke(action);
            else action();
        }
    }
}
