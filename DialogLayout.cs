using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace DynamicWindows
{
    internal class DialogLayout
    {
        private const int PAD = 8;
        private const int COL_GAP = 8;
        private const int ROW_GAP = 6;
        private const int BTN_PAD = 14;
        private const int ROW_SNAP = 10;

        private readonly Plugin _plugin;

        public DialogLayout(Plugin plugin)
        {
            _plugin = plugin;
        }

        // ── Entry point ───────────────────────────────────────────────────────

        public void Apply(DwForm dialog, XmlElement container)
        {
            Font font = _plugin.LayoutFont;
            bool isStore = dialog.Name == "storeWindow";

            var elems = container.ChildNodes.OfType<XmlElement>()
                .Where(e => e.Name != "clearContainer")
                .ToList();

            if (!elems.Any()) return;

            var cells = AssignGrid(elems);
            if (!cells.Any()) return;

            // Measure all controls
            foreach (var c in cells)
                Measure(dialog.FormBody.Controls[c.Id], font);

            // Split: bottom-anchored (s/sw/se), centre-body (align=center — placed after
            // all regular body rows but before bottom buttons), and regular body
            var bottom = cells.Where(c => c.BottomAnchored).ToList();
            var centreBody = cells.Where(c => c.HCentre && !c.BottomAnchored).ToList();
            var body = cells.Where(c => !c.BottomAnchored && !c.HCentre).ToList();

            if (!body.Any() && !centreBody.Any() && !bottom.Any()) return;

            // Column widths and row heights
            int cols = body.Any() ? body.Max(c => c.Col) + 1 : 0;
            int rows = body.Any() ? body.Max(c => c.Row) + 1 : 0;

            var colW = cols > 0 ? new int[cols] : new int[1];
            var rowH = rows > 0 ? new int[rows] : new int[1];

            foreach (var c in body)
            {
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (ctrl == null || c.Col >= cols || c.Row >= rows) continue;
                // Exclude from column width: right-pinned controls and FullWidth controls.
                // FullWidth controls stretch to the window width — they don't drive it.
                if (c.RightAlign || c.Xml.HasAttribute("anchor_right") || c.FullWidth) continue;

                // anchor_left controls are positioned off their anchor in pass 3, NOT by the
                // grid column — so they must not inflate the columns the absolute controls use.
                // (profileEdit: the dropdowns / IM boxes were widening the checkbox grid and
                //  shoving the Circle/Url toggles out to the far right.) Keep their row height.
                if (c.Xml.HasAttribute("anchor_left"))
                {
                    rowH[c.Row] = Math.Max(rowH[c.Row], ctrl.Height);
                    continue;
                }

                // streamBox values (profile features/quote) overflow to the right edge instead
                // of driving the value column — regardless of whether they happen to be solo on
                // their row. Otherwise the 250px box inflates col1 and pushes Guild far right.
                if (c.Xml.Name == "streamBox")
                {
                    rowH[c.Row] = Math.Max(rowH[c.Row], ctrl.Height);
                    continue;
                }

                // Store window: the custom value labels are re-pinned to the main value
                // column by AlignStoreCustomFields, so their (possibly long) text must not
                // drive the grid width here. Otherwise a container name of ~18+ characters
                // grows the window past its server width on first open, kicking in a
                // horizontal scroll and a paint glitch. Keep their row height.
                if (isStore && (c.Id == "customString" || c.Id == "customContainer" || c.Id == "stringContainer"))
                {
                    rowH[c.Row] = Math.Max(rowH[c.Row], ctrl.Height);
                    continue;
                }

                colW[c.Col] = Math.Max(colW[c.Col], ctrl.Width);
                rowH[c.Row] = Math.Max(rowH[c.Row], ctrl.Height);
            }

            // Row heights still need FullWidth control heights
            foreach (var c in body.Where(c => c.FullWidth))
            {
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (ctrl == null || c.Row >= rows) continue;
                rowH[c.Row] = Math.Max(rowH[c.Row], ctrl.Height);
            }

            for (int i = 0; i < cols; i++) if (colW[i] == 0) colW[i] = 20;
            for (int i = 0; i < rows; i++) if (rowH[i] == 0) rowH[i] = 8;

            // Column X and row Y origins
            var colX = new int[Math.Max(cols, 1)];
            var rowY = new int[Math.Max(rows, 1)];
            colX[0] = PAD;
            for (int i = 1; i < cols; i++) colX[i] = colX[i - 1] + colW[i - 1] + COL_GAP;
            rowY[0] = PAD;
            for (int i = 1; i < rows; i++) rowY[i] = rowY[i - 1] + rowH[i - 1] + ROW_GAP;

            // Right-gutter for ne/anchor_right controls
            int rightGutter = 0;
            var rightPinned = body.Where(c => c.RightAlign || c.Xml.HasAttribute("anchor_right")).ToList();
            if (rightPinned.Any())
            {
                foreach (var rg in rightPinned.GroupBy(c => c.Row))
                {
                    int w = 0;
                    foreach (var rc in rg)
                    {
                        var rc2 = dialog.FormBody.Controls[rc.Id];
                        if (rc2 != null) w += rc2.Width + COL_GAP;
                    }
                    rightGutter = Math.Max(rightGutter, w + PAD);
                }
            }

            int contentW = PAD + (cols > 0 ? colW.Sum() + Math.Max(0, cols - 1) * COL_GAP : 0)
                         + rightGutter + PAD;
            contentW = Math.Max(contentW, 120);

            // centreBody controls (align="center") are excluded from colW but must
            // still contribute to the window width so centred controls don't overflow
            foreach (var c in centreBody)
            {
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (ctrl != null) contentW = Math.Max(contentW, ctrl.Width + PAD * 2);
            }

            // FullWidth streamBox controls (e.g. confirm RTB) keep their server width
            // as a minimum window width since they're excluded from colW
            foreach (var c in body.Where(b => b.FullWidth))
            {
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (ctrl is TextBox tb && tb.Multiline)
                    contentW = Math.Max(contentW, ctrl.Width + PAD * 2);
            }

            // In-column streamBoxes (profile features/quote) are excluded from colW, so the
            // window must still be wide enough for them: column origin + the box's server width.
            foreach (var c in body.Where(b => b.Xml.Name == "streamBox" && !b.FullWidth))
            {
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (ctrl != null && c.Col < cols)
                    contentW = Math.Max(contentW, colX[c.Col] + ctrl.Width + PAD);
            }

            // Resolve final window width — content-driven, not floored by server width
            int finalW = contentW;

            // Place body controls
            foreach (var c in body)
            {
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (ctrl == null || c.Col >= cols || c.Row >= rows) continue;

                int y = rowY[c.Row] + (rowH[c.Row] - ctrl.Height) / 2;
                int x = c.HCentre ? (finalW - ctrl.Width) / 2
                      : c.RightAlign ? finalW - PAD - ctrl.Width
                      : colX[c.Col];

                ctrl.Location = new Point(x, y);

                // Expand TextBox controls to fill their column width ONLY when the
                // column width wasn't inflated by a ComboBox (e.g. Bug Report Title/Details
                // should match the category dropdown width). For IM service/screen name
                // textboxes alongside dropdowns, keep measured width.
                if (ctrl is TextBox && c.Xml.Name != "streamBox" && !c.FullWidth && c.Col < cols &&
                    !c.Xml.HasAttribute("anchor_left")) // anchor_left textboxes placed by pass 3
                    ctrl.Width = colW[c.Col];
            }

            // Re-snap controls whose fixed server-left drops them in a far column band when
            // they really belong right next to their same-row left neighbour:
            //   • tiny help buttons (server width <= 15, e.g. profile "?")
            //   • inline button groups (e.g. bank Pay/ALL, Withdraw/ALL) — a button whose
            //     left neighbour on the row is also a button packs against it instead of
            //     riding the value column's (dropdown-inflated) width.
            foreach (var c in body)
            {
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (ctrl == null) continue;
                // Never re-snap right-pinned controls (align="ne") or anchored controls —
                // they have their own placement and must not be packed left.
                if (c.RightAlign || c.Xml.HasAttribute("anchor_left") || c.Xml.HasAttribute("anchor_right")) continue;

                // nearest body control on the same row that sits to its left
                Control? leftN = null;
                foreach (var o in body)
                {
                    if (o.Id == c.Id || o.Row != c.Row) continue;
                    var oc = dialog.FormBody.Controls[o.Id];
                    if (oc == null || oc.Left >= ctrl.Left) continue;
                    if (leftN == null || oc.Right > leftN.Right) leftN = oc;
                }
                if (leftN == null) continue;

                int.TryParse(c.Xml.GetAttribute("width"), out int sw);
                bool tinyHelp = sw > 0 && sw <= 15;

                // Inline button group: pack a button against the button on its left — but ONLY
                // if the row isn't already using right-pinned buttons (align="ne"/anchor_right)
                // to handle alignment, otherwise we'd fight that and stagger them (store window).
                bool rowHasRightPinned = body.Any(o => o.Row == c.Row &&
                    (o.RightAlign || o.Xml.HasAttribute("anchor_right")));
                bool buttonGroup = ctrl is Button && leftN is Button && !rowHasRightPinned;

                if (!tinyHelp && !buttonGroup) continue;

                ctrl.Location = new Point(leftN.Right + COL_GAP,
                                          leftN.Top + (leftN.Height - ctrl.Height) / 2);
            }

            // If a re-snap vacated the rightmost column, tighten the window to the actual
            // content so there's no dead space where the button used to sit. Only for simple
            // windows with no centre/bottom controls (whose centring relies on finalW);
            // anchored controls re-grow finalW in their own passes.
            if (!centreBody.Any() && !bottom.Any())
            {
                int maxRight = PAD;
                foreach (var c in body)
                {
                    var ctrl = dialog.FormBody.Controls[c.Id];
                    if (ctrl != null) maxRight = Math.Max(maxRight, ctrl.Right);
                }
                finalW = Math.Max(120, maxRight + PAD);
            }

            // Centre-body controls (align="center") — placed after all regular body rows
            int bottomY = rows > 0 ? rowY[rows - 1] + rowH[rows - 1] + ROW_GAP : PAD;

            foreach (var c in centreBody)
            {
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (ctrl == null) continue;
                ctrl.Location = new Point((finalW - ctrl.Width) / 2, bottomY);
                bottomY += ctrl.Height + ROW_GAP;
            }

            if (centreBody.Any()) bottomY += BTN_PAD - ROW_GAP; // extra gap before buttons

            // Bottom controls (s/sw/se) — centred/edged below everything
            if (!centreBody.Any())
                bottomY = rows > 0 ? rowY[rows - 1] + rowH[rows - 1] + BTN_PAD : PAD;

            foreach (var grp in bottom.GroupBy(c => c.OriginalTop).OrderBy(g => g.Key))
            {
                var grpList = grp.ToList();
                int grpH = grpList.Select(c => dialog.FormBody.Controls[c.Id]?.Height ?? 0)
                                  .DefaultIfEmpty(0).Max();

                var swBtns = grpList.Where(c => c.Xml.GetAttribute("align") == "sw").ToList();
                var seBtns = grpList.Where(c => c.Xml.GetAttribute("align") == "se").ToList();
                var sBtns = grpList.Where(c => c.Xml.GetAttribute("align") == "s").ToList();

                foreach (var bc in swBtns)
                {
                    var ctrl = dialog.FormBody.Controls[bc.Id];
                    if (ctrl != null) ctrl.Location = new Point(PAD, bottomY + (grpH - ctrl.Height) / 2);
                }
                foreach (var bc in seBtns)
                {
                    var ctrl = dialog.FormBody.Controls[bc.Id];
                    if (ctrl != null) ctrl.Location = new Point(finalW - PAD - ctrl.Width, bottomY + (grpH - ctrl.Height) / 2);
                }

                if (sBtns.Count == 1)
                {
                    var ctrl = dialog.FormBody.Controls[sBtns[0].Id];
                    if (ctrl != null) ctrl.Location = new Point((finalW - ctrl.Width) / 2, bottomY + (grpH - ctrl.Height) / 2);
                }
                else if (sBtns.Count > 1)
                {
                    int totalW = sBtns.Sum(c => dialog.FormBody.Controls[c.Id]?.Width ?? 0)
                               + (sBtns.Count - 1) * COL_GAP;
                    int bx = (finalW - totalW) / 2;
                    foreach (var bc in sBtns)
                    {
                        var ctrl = dialog.FormBody.Controls[bc.Id];
                        if (ctrl == null) continue;
                        ctrl.Location = new Point(bx, bottomY + (grpH - ctrl.Height) / 2);
                        bx += ctrl.Width + COL_GAP;
                    }
                }

                bottomY += grpH + ROW_GAP;
            }

            // Second pass: anchor_right controls — place left of their anchor
            foreach (var c in body.Where(c => c.Xml.HasAttribute("anchor_right")))
            {
                string aId = c.Xml.GetAttribute("anchor_right");
                var anc = dialog.FormBody.Controls[aId];
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (anc == null || ctrl == null) continue;
                ctrl.Location = new Point(anc.Left - COL_GAP - ctrl.Width,
                                          anc.Top + (anc.Height - ctrl.Height) / 2);
            }

            // Third pass: anchor_left controls — X position from anchor's right edge,
            // Y position from the element's own row (computed in grid assignment).
            // This correctly places chained controls both horizontally (beside anchor)
            // and vertically (in their own row based on their top value).
            foreach (var c in body.Where(c => c.Xml.HasAttribute("anchor_left")))
            {
                string aId = c.Xml.GetAttribute("anchor_left");
                var anc = dialog.FormBody.Controls[aId];
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (anc == null || ctrl == null) continue;

                // X: immediately to the right of the anchor
                int ax = anc.Left + anc.Width + COL_GAP;

                // The dropdowns share one column by all anchoring to a single label
                // (editSpouseLabel). When this row's own leading label is wider than that
                // anchor (e.g. "Preferred Location:" vs "Spouse Setting:"), the control would
                // sit underneath its own label — so push just this one right to clear it.
                // Rows whose label fits the shared column are left untouched (stay aligned).
                foreach (var other in body)
                {
                    if (other == c || other.Row != c.Row) continue;
                    if (other.Xml.HasAttribute("anchor_left") ||
                        other.Xml.HasAttribute("anchor_right") || other.RightAlign) continue;
                    var oc = dialog.FormBody.Controls[other.Id];
                    if (oc != null && oc.Left + oc.Width > ax)
                        ax = oc.Left + oc.Width + COL_GAP;
                }

                // Y: use element's own row if it has one, otherwise match anchor's row
                int ay;
                if (c.Row >= 0 && c.Row < rows)
                    ay = rowY[c.Row] + (rowH[c.Row] - ctrl.Height) / 2;
                else
                    ay = anc.Top + (anc.Height - ctrl.Height) / 2;

                ctrl.Location = new Point(ax, ay);
                // If this row carries right-pinned buttons (align="ne"/anchor_right), reserve
                // the gutter for them so a long anchor_left value can't grow the window right
                // up to its own edge and overlap the buttons (store window Change/Clear).
                finalW = Math.Max(finalW,
                    ax + ctrl.Width + (rightGutter > 0 ? COL_GAP + rightGutter : PAD));
            }

            // finalW may have grown during the anchor_left pass after the right-pinned controls
            // were first placed in the body loop. Re-pin RightAlign (align="ne") controls to the
            // final width, then slide each anchor_right control back against them. (Store window.)
            foreach (var c in body.Where(c => c.RightAlign))
            {
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (ctrl != null) ctrl.Left = finalW - PAD - ctrl.Width;
            }
            foreach (var c in body.Where(c => c.Xml.HasAttribute("anchor_right")))
            {
                string aId = c.Xml.GetAttribute("anchor_right");
                var anc = dialog.FormBody.Controls[aId];
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (anc != null && ctrl != null) ctrl.Left = anc.Left - COL_GAP - ctrl.Width;
            }

            // Size is purely content-driven — set after all passes so finalW is accurate
            dialog.ClientSize = new Size(finalW, bottomY + PAD);

            // Stretch full-width controls to final window width
            foreach (var c in body.Where(c => c.FullWidth))
            {
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (ctrl != null)
                {
                    ctrl.Location = new Point(PAD, ctrl.Top);
                    ctrl.Width = finalW - PAD * 2;
                }
            }

            // Stretch in-column streamBoxes (profile features/quote) to the right edge,
            // keeping their X beside the label (do NOT reset X to PAD like FullWidth does).
            foreach (var c in body.Where(c => c.Xml.Name == "streamBox" && !c.FullWidth))
            {
                var ctrl = dialog.FormBody.Controls[c.Id];
                if (ctrl != null)
                    ctrl.Width = Math.Max(ctrl.Width, finalW - ctrl.Left - PAD);
            }

            // Bug Report window only: widen the category dropdown to fit its longest option
            // and align category/title/details to a shared right edge (start points unchanged).
            if (dialog.Name == "bugDialogBox")
                AlignBugReportInputs(dialog, font);
            if (dialog.Name == "storeWindow")
                AlignStoreCustomFields(dialog);
        }

        // Store window: the custom-section value labels (customString / customContainer, plus
        // the server's duplicate stringContainer) are sent at left=100, but the grid places
        // them off the wide "Items containing the string:" column and lets them drift right
        // whenever the server re-sends a longer value. Pin them to the main value column so
        // they line up with the other rows and stay put when their text is set. Scoped here.
        private static void AlignStoreCustomFields(DwForm dialog)
        {
            var c = dialog.FormBody.Controls;
            if (c["ammunitionContainer"] is not Control mainValue) return;
            int leftX = mainValue.Left;

            foreach (var id in new[] { "customString", "customContainer", "stringContainer" })
                if (c[id] is Control v) v.Left = leftX;

            // stringContainer is the server's duplicate of customContainer — sit it exactly
            // on top so the pair reads as a single line rather than two offset labels.
            if (c["customContainer"] is Control cc && c["stringContainer"] is Control sc)
                sc.Top = cc.Top;
        }

        // Bug Report: give the category combo enough width for its longest option, then snap
        // category/title/details AND the instructions box to one shared right edge, size the
        // window to that edge, and move the right-pinned Cancel button to match. Scoped to that
        // window; start (left) positions are left exactly as the layout placed them.
        private static void AlignBugReportInputs(DwForm dialog, Font font)
        {
            var category = dialog.FormBody.Controls["category"] as ComboBox;
            var instructions = dialog.FormBody.Controls["instructions"];
            var cancel = dialog.FormBody.Controls["close"];

            var inputs = new[] { "category", "title", "details" }
                .Select(n => dialog.FormBody.Controls[n])
                .OfType<Control>()
                .ToList();
            if (inputs.Count == 0) return;

            // Shared right edge: the widest input, but at least enough for the category's longest option.
            int rightEdge = inputs.Max(c => c.Right);
            if (category != null)
            {
                int longest = 0;
                foreach (var item in category.Items)
                {
                    int w = TextRenderer.MeasureText(item?.ToString() ?? string.Empty, font).Width;
                    if (w > longest) longest = w;
                }
                longest += SystemInformation.VerticalScrollBarWidth + 8;   // dropdown arrow + padding
                rightEdge = Math.Max(rightEdge, category.Left + longest);
                category.DropDownWidth = Math.Max(category.DropDownWidth, longest);  // open list never clips
            }

            // Align the three inputs and the instructions box to that edge.
            foreach (var c in inputs)
                c.Width = Math.Max(20, rightEdge - c.Left);
            if (instructions != null)
                instructions.Width = Math.Max(20, rightEdge - instructions.Left);

            // Size the window to the shared edge and move the right-pinned Cancel button to it.
            int newWidth = rightEdge + PAD;
            if (newWidth != dialog.ClientSize.Width)
                dialog.ClientSize = new Size(newWidth, dialog.ClientSize.Height);
            if (cancel != null)
                cancel.Left = rightEdge - cancel.Width;
        }

        // ── Grid assignment ───────────────────────────────────────────────────
        // Core principle: ROW from absolute top value (snapped); COLUMN from anchors.
        // anchor_top overrides row. anchor_left/right only set column.

        private List<GridCell> AssignGrid(List<XmlElement> elems)
        {
            var placed = new Dictionary<string, GridCell>();

            // Step 1: Build the top-value → row-index map from ALL elements
            // (not just non-anchored ones). Every element with an explicit top > 0
            // contributes to the row map regardless of anchors.
            var snapGroups = new List<(int SnapTop, List<XmlElement> Elems)>();
            foreach (var elem in elems)
            {
                if (IsBottomAnchored(elem)) continue;
                int.TryParse(elem.GetAttribute("top"), out int top);
                if (top < 0) top = 100000 + Math.Abs(top);
                if (top == 0 && !elem.HasAttribute("top")) continue;

                bool isCentre = elem.GetAttribute("align") == "center";

                // align="center" elements must not merge with non-centre elements —
                // they have their own row even if top values are close.
                // (e.g. customStringDialog: editBox top=15 must not merge with label top=10)
                var hit = snapGroups.FirstOrDefault(g =>
                    Math.Abs(g.SnapTop - top) <= ROW_SNAP &&
                    // Only merge if both are centre-aligned or both are not
                    g.Elems.All(e => (e.GetAttribute("align") == "center") == isCentre));

                if (hit.Elems != null)
                    hit.Elems.Add(elem);
                else
                    snapGroups.Add((top, new List<XmlElement> { elem }));
            }
            snapGroups = snapGroups.OrderBy(g => g.SnapTop).ToList();

            // Map from snap-top → sequential row index
            var topToRow = new Dictionary<int, int>();
            int rowIdx = 0;
            foreach (var (snapTop, _) in snapGroups)
                topToRow[snapTop] = rowIdx++;

            // Helper: find row index for a given element's top value
            int GetRowForTop(int top)
            {
                if (top < 0) top = 100000 + Math.Abs(top);
                foreach (var (snapTop, _) in snapGroups)
                    if (Math.Abs(snapTop - top) <= ROW_SNAP)
                        return topToRow[snapTop];
                return 0;
            }

            // Step 2: Build a global left-value → column index mapping.
            // All absolute-positioned elements across all rows are sorted by left value.
            // Elements within LEFT_SNAP pixels of each other share the same column.
            // This ensures "Goal" (left=105) aligns with NUDs (left=100) across rows.
            // EXCLUDED: solo label rows with no width (e.g. "Show These In My Profile:")
            // — these are FullWidth spans that don't participate in the column grid.
            const int LEFT_SNAP = 20;

            // Pre-identify FullWidth candidates: sole label in their snap group, no width attr
            var fullWidthIds = new HashSet<string>(
                snapGroups
                    .Where(g =>
                    {
                        var abs = g.Elems.Where(e =>
                            !e.HasAttribute("anchor_left") &&
                            !e.HasAttribute("anchor_right") &&
                            !e.HasAttribute("anchor_top") &&
                            !IsBottomAnchored(e)).ToList();
                        return abs.Count == 1 &&
                               abs[0].Name == "label" &&
                               !abs[0].HasAttribute("width");
                    })
                    .Select(g => g.Elems
                        .First(e => !e.HasAttribute("anchor_left") &&
                                    !e.HasAttribute("anchor_right") &&
                                    !e.HasAttribute("anchor_top") &&
                                    !IsBottomAnchored(e))
                        .GetAttribute("id")));

            var allAbsElems = snapGroups
                .SelectMany(g => g.Elems)
                .Where(e => !e.HasAttribute("anchor_left") &&
                            !e.HasAttribute("anchor_right") &&
                            !e.HasAttribute("anchor_top") &&
                            !IsBottomAnchored(e) &&
                            !fullWidthIds.Contains(e.GetAttribute("id"))) // exclude FullWidth
                .Select(e => {
                    int.TryParse(e.GetAttribute("left"), out int l);
                    return (Left: l < 0 ? int.MaxValue + l : l, Elem: e);
                })
                .OrderBy(t => t.Left)
                .ToList();

            // Build column bands: group left values within LEFT_SNAP of each other
            var colBands = new List<int>(); // representative left value for each column
            foreach (var (left, _) in allAbsElems)
            {
                bool found = false;
                for (int i = 0; i < colBands.Count; i++)
                {
                    if (Math.Abs(colBands[i] - left) <= LEFT_SNAP) { found = true; break; }
                }
                if (!found) colBands.Add(left);
            }
            colBands.Sort();

            int GetColForLeft(int left)
            {
                int sortKey = left < 0 ? int.MaxValue + left : left;
                for (int i = 0; i < colBands.Count; i++)
                    if (Math.Abs(colBands[i] - sortKey) <= LEFT_SNAP) return i;
                return 0;
            }

            // Place all absolute-positioned body elements using global column mapping
            foreach (var (_, groupElems) in snapGroups)
            {
                var absInGroup = groupElems
                    .Where(e => !e.HasAttribute("anchor_left") &&
                                !e.HasAttribute("anchor_right") &&
                                !e.HasAttribute("anchor_top"))
                    .ToList();

                int.TryParse(groupElems[0].GetAttribute("top"), out int groupTop);
                if (groupTop < 0) groupTop = 100000 + Math.Abs(groupTop);
                int row = GetRowForTop(groupTop);

                foreach (var e in absInGroup)
                {
                    string id = e.GetAttribute("id");
                    if (placed.ContainsKey(id)) continue;
                    int.TryParse(e.GetAttribute("left"), out int leftVal);
                    // FullWidth controls: pre-identified solo-label-no-width rows,
                    // or solo streamBox rows
                    bool fw = fullWidthIds.Contains(id) ||
                              (e.Name == "streamBox" && absInGroup.Count == 1);
                    int col = fw ? 0 : GetColForLeft(leftVal);
                    bool hc = e.GetAttribute("align") == "center";
                    bool ra = e.GetAttribute("align") == "ne";
                    placed[id] = new GridCell(id, e, col, row,
                        groupTop < 100000 ? groupTop : -(groupTop - 100000), fw, hc, ra, false);
                }
            }

            // Step 3: Place bottom-anchored elements
            foreach (var elem in elems.Where(IsBottomAnchored))
            {
                string id = elem.GetAttribute("id");
                int.TryParse(elem.GetAttribute("top"), out int bt);
                int origTop = bt < 0 ? 100000 + Math.Abs(bt) : bt;
                placed[id] = new GridCell(id, elem, 0, 0, origTop, false, false, false, true);
            }

            // Step 4: Place anchored body elements via multi-pass queue
            var anchored = elems.Where(e =>
                (e.HasAttribute("anchor_left") || e.HasAttribute("anchor_right") || e.HasAttribute("anchor_top"))
                && !IsBottomAnchored(e)).ToList();

            var queue = new Queue<XmlElement>(anchored);
            int maxPasses = (anchored.Count + 1) * (anchored.Count + 1) + 10;
            int pass = 0;

            while (queue.Count > 0 && pass++ < maxPasses)
            {
                var elem = queue.Dequeue();
                string id = elem.GetAttribute("id");
                if (placed.ContainsKey(id)) continue;

                // Row: anchor_top overrides; otherwise use element's own top value
                int? resolvedRow = null;
                int? resolvedCol = null;
                bool missing = false;

                if (elem.HasAttribute("anchor_top"))
                {
                    string aId = elem.GetAttribute("anchor_top");
                    if (!placed.TryGetValue(aId, out var a)) { missing = true; }
                    else resolvedRow = a.Row + 1;
                }
                else
                {
                    // Use element's own top value for row
                    int.TryParse(elem.GetAttribute("top"), out int elemTop);
                    if (elemTop != 0)
                        resolvedRow = GetRowForTop(elemTop < 0 ? 100000 + Math.Abs(elemTop) : elemTop);
                    // If top=0 or missing, fall back to anchor's row (handled below)
                }

                if (elem.HasAttribute("anchor_left"))
                {
                    string aId = elem.GetAttribute("anchor_left");
                    if (!placed.TryGetValue(aId, out var a)) { missing = true; }
                    else
                    {
                        resolvedCol = a.Col + 1;
                        resolvedRow ??= a.Row; // fallback: same row as anchor
                    }
                }
                else if (elem.HasAttribute("anchor_right"))
                {
                    string aId = elem.GetAttribute("anchor_right");
                    if (!placed.TryGetValue(aId, out var a)) { missing = true; }
                    else
                    {
                        resolvedCol = Math.Max(0, a.Col - 1);
                        resolvedRow ??= a.Row;
                    }
                }

                if (missing || resolvedRow == null || resolvedCol == null && !elem.HasAttribute("anchor_top"))
                {
                    queue.Enqueue(elem);
                    continue;
                }

                int finalCol = resolvedCol ?? 0;
                int finalRow = resolvedRow.Value;

                bool fw = elem.Name == "streamBox";
                bool hc = elem.GetAttribute("align") == "center";
                bool ra = elem.GetAttribute("align") == "ne";
                placed[id] = new GridCell(id, elem, finalCol, finalRow, finalRow, fw, hc, ra, false);
            }

            // Re-number rows sequentially (anchor_top may create gaps)
            var rowNums = placed.Values.Where(c => !c.BottomAnchored)
                .Select(c => c.Row).Distinct().OrderBy(r => r).ToList();
            var rowMap = rowNums.Select((r, i) => (r, i)).ToDictionary(t => t.r, t => t.i);
            foreach (var c in placed.Values.Where(c => !c.BottomAnchored))
                if (rowMap.TryGetValue(c.Row, out int nr)) c.Row = nr;

            return placed.Values.ToList();
        }

        // ── Measurement ───────────────────────────────────────────────────────

        private static void Measure(Control? ctrl, Font font)
        {
            if (ctrl == null) return;
            ctrl.Font = font;

            switch (ctrl)
            {
                case Label lbl:
                    lbl.AutoSize = false;
                    // Pre-size counter labels (titleLabel, detailsLabel) at their
                    // maximum possible text width so the column is wide enough when
                    // the counter updates during typing
                    string measureText = lbl.Name switch
                    {
                        "titleLabel" => "Title 128/128",
                        "detailsLabel" => "Details 875/875",
                        _ => string.IsNullOrEmpty(lbl.Text) ? "M" : lbl.Text
                    };
                    var lsz = TextRenderer.MeasureText(measureText, font);
                    lbl.Size = new Size(lsz.Width + 4, lsz.Height + 4);
                    break;

                case Button btn:
                    var bsz = TextRenderer.MeasureText(btn.Text, font);
                    btn.Size = new Size(bsz.Width + 16, bsz.Height + 8);
                    break;

                case NumericUpDown:
                    var nsz = TextRenderer.MeasureText("0000", font);
                    ctrl.Size = new Size(nsz.Width + 28, nsz.Height + 6);
                    break;

                case TextBox tb:
                    var tsz = TextRenderer.MeasureText("M", font);
                    int th = tb.Multiline && ctrl.Height > 30 ? ctrl.Height : tsz.Height + 6;
                    // Use server-specified width if set (e.g. editBox width="380"),
                    // otherwise default to 160px
                    int tw = ctrl.Width > 20 ? ctrl.Width : 160;
                    tb.Size = new Size(tw, th);
                    break;

                case ComboBox cb:
                    // Use server-specified width if set (e.g. dropDownBox width="300"),
                    // otherwise measure from longest item text, capped at 200px
                    if (ctrl.Width > 20)
                    {
                        var fh = TextRenderer.MeasureText("M", font);
                        cb.Size = new Size(ctrl.Width, fh.Height + 6);
                    }
                    else
                    {
                        string sample = "MMMMMMMM";
                        if (cb.Items.Count > 0)
                        {
                            int mx = 0;
                            foreach (var item in cb.Items)
                            {
                                int l = item?.ToString()?.Length ?? 0;
                                if (l > mx) { mx = l; sample = item?.ToString() ?? sample; }
                            }
                        }
                        var csz = TextRenderer.MeasureText(sample, font);
                        cb.Size = new Size(Math.Min(csz.Width + 30, 200), csz.Height + 6);
                    }
                    break;

                case CheckBox:
                case RadioButton:
                    ctrl.AutoSize = false;
                    var xsz = TextRenderer.MeasureText(ctrl.Text, font);
                    ctrl.Size = new Size(xsz.Width + 22, xsz.Height + 4);
                    break;

                case RichTextBox:
                    break; // keep server dimensions

                case ProgressBar:
                    if (ctrl.Height < 16) ctrl.Height = 20;
                    break;
            }
        }

        private static bool IsBottomAnchored(XmlElement e)
        {
            string a = e.GetAttribute("align");
            return a == "s" || a == "se" || a == "sw";
        }
    }

    // ── Supporting type ───────────────────────────────────────────────────────

    internal class GridCell
    {
        public string Id { get; }
        public XmlElement Xml { get; }
        public int Col { get; }
        public int Row { get; set; }
        public int OriginalTop { get; }
        public bool FullWidth { get; }
        public bool HCentre { get; }
        public bool RightAlign { get; }
        public bool BottomAnchored { get; }

        public GridCell(string id, XmlElement xml, int col, int row,
                        int originalTop, bool fullWidth, bool hCentre,
                        bool rightAlign, bool bottomAnchored)
        {
            Id = id; Xml = xml; Col = col; Row = row;
            OriginalTop = originalTop; FullWidth = fullWidth;
            HCentre = hCentre; RightAlign = rightAlign; BottomAnchored = bottomAnchored;
        }
    }
}