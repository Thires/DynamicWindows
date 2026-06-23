using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DynamicWindows
{
    public class FormOptionWindow : Form
    {

        private CheckBox CheckBoxStowContainer = null!;
        private Button ButtonClose = null!;
        private Button ButtonCancel = null!;
        private Button buttonHelp = null!;
        private CheckBox checkBoxDisablePlugin = null!;
        private TextBox textBox_Color = null!;
        private Button buttonForeground = null!;
        private Button buttonBackground = null!;
        private Button buttonFont = null!;
        private Button buttonLinkColor = null!;
        private Button buttonTimerColor = null!;
        private Button buttonDefaults = null!;
        private ListBox listbox_openwindows = null!;
        private ListBox listBox_ignores = null!;
        private Button button_ignore = null!;
        private Button button_clear = null!;
        private Button button_clearall = null!;
        private Label label1 = null!;
        private Label label2 = null!;
        private Button button_closewindow = null!;
        private CheckBox cbDisableOtherInjuries = null!;
        private CheckBox cbDisableSelfInjuries = null!;
        private TrackBar trackBarScale = null!;
        private Label labelScaleValue = null!;
        private Label labelScaleTitle = null!;

        private readonly Plugin _plugin;

        // Snapshot of every setting this window can change, captured when it opens, so Cancel
        // can revert without writing the XML. OK is the only path that persists.
        private readonly Color _origFore, _origBack, _origLink, _origTimer;
        private readonly string _origFontFamily;
        private readonly FontStyle _origFontStyle;
        private readonly float _origScale;
        private readonly bool _origStow, _origEnabled, _origDisableOther, _origDisableSelf;
        private readonly List<string> _origIgnore;

        public FormOptionWindow(Plugin plugin)
        {
            _plugin = plugin;

            _origFore = plugin.formfore;
            _origBack = plugin.formback;
            _origLink = plugin.linkColor;
            _origTimer = plugin.timerBarColor;
            _origFontFamily = plugin.FontFamilyName;
            _origFontStyle = plugin.FontStyleChoice;
            _origScale = plugin.Scale;
            _origStow = plugin.bStowContainer;
            _origEnabled = plugin.bPluginEnabled;
            _origDisableOther = plugin.bDisableOtherInjuries;
            _origDisableSelf = plugin.bDisableSelfInjuries;
            _origIgnore = new List<string>(plugin.ignorelist);

            InitializeComponent();

            foreach (DwForm form in _plugin.forms)
                listbox_openwindows.Items.Add(form.Name);

            foreach (string str in _plugin.ignorelist)
                listBox_ignores.Items.Add(str);

            textBox_Color.ForeColor = _plugin.formfore;
            textBox_Color.BackColor = _plugin.formback;
            ApplyFontPreview();
            buttonLinkColor.ForeColor = _plugin.linkColor;
            buttonTimerColor.ForeColor = _plugin.timerBarColor;
            checkBoxDisablePlugin.Checked = !_plugin.bPluginEnabled;
            CheckBoxStowContainer.Checked = _plugin.bStowContainer;
            cbDisableOtherInjuries.Checked = _plugin.bDisableOtherInjuries;
            cbDisableSelfInjuries.Checked = _plugin.bDisableSelfInjuries;

            // TrackBar range: 100–150 representing 1.00–1.50 in steps of 5 (i.e. 0.05)
            // Beyond 1.5x row spacing and control heights diverge visibly in tightly-packed
            // server dialogs like TDP Planning — 1.25x is the sweet spot for most displays.
            trackBarScale.Minimum = 100;
            trackBarScale.Maximum = 150;
            trackBarScale.TickFrequency = 5;
            trackBarScale.SmallChange = 5;
            trackBarScale.LargeChange = 10;
            trackBarScale.Value = (int)Math.Round(_plugin.Scale * 100);
            labelScaleValue.Text = _plugin.Scale.ToString("F2") + "x";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            CheckBoxStowContainer = new CheckBox();
            ButtonClose = new Button();
            ButtonCancel = new Button();
            buttonHelp = new Button();
            checkBoxDisablePlugin = new CheckBox();
            textBox_Color = new TextBox();
            buttonForeground = new Button();
            buttonBackground = new Button();
            buttonFont = new Button();
            buttonLinkColor = new Button();
            buttonTimerColor = new Button();
            buttonDefaults = new Button();
            listbox_openwindows = new ListBox();
            listBox_ignores = new ListBox();
            button_ignore = new Button();
            button_clear = new Button();
            button_clearall = new Button();
            label1 = new Label();
            label2 = new Label();
            button_closewindow = new Button();
            cbDisableOtherInjuries = new CheckBox();
            cbDisableSelfInjuries = new CheckBox();
            trackBarScale = new TrackBar();
            labelScaleValue = new Label();
            labelScaleTitle = new Label();
            ((ISupportInitialize)trackBarScale).BeginInit();
            SuspendLayout();
            // 
            // CheckBoxStowContainer
            // 
            CheckBoxStowContainer.AutoSize = true;
            CheckBoxStowContainer.Location = new Point(15, 15);
            CheckBoxStowContainer.Margin = new Padding(4, 3, 4, 3);
            CheckBoxStowContainer.Name = "CheckBoxStowContainer";
            CheckBoxStowContainer.Size = new Size(154, 19);
            CheckBoxStowContainer.TabIndex = 21;
            CheckBoxStowContainer.Text = "Stow Container Window";
            CheckBoxStowContainer.UseVisualStyleBackColor = true;
            CheckBoxStowContainer.CheckedChanged += CheckBoxStowContainer_CheckedChanged;
            // 
            // ButtonClose
            // 
            ButtonClose.DialogResult = DialogResult.Cancel;
            ButtonClose.Location = new Point(14, 421);
            ButtonClose.Margin = new Padding(4, 3, 4, 3);
            ButtonClose.Name = "ButtonClose";
            ButtonClose.Size = new Size(88, 27);
            ButtonClose.TabIndex = 18;
            ButtonClose.Text = "OK";
            ButtonClose.UseVisualStyleBackColor = true;
            ButtonClose.Click += ButtonClose_Click;
            // 
            // ButtonCancel
            // 
            ButtonCancel.Location = new Point(108, 421);
            ButtonCancel.Margin = new Padding(4, 3, 4, 3);
            ButtonCancel.Name = "ButtonCancel";
            ButtonCancel.Size = new Size(88, 27);
            ButtonCancel.TabIndex = 19;
            ButtonCancel.Text = "Cancel";
            ButtonCancel.UseVisualStyleBackColor = true;
            ButtonCancel.Click += ButtonCancel_Click;
            // 
            // buttonHelp
            // 
            buttonHelp.Location = new Point(203, 421);
            buttonHelp.Margin = new Padding(4, 3, 4, 3);
            buttonHelp.Name = "buttonHelp";
            buttonHelp.Size = new Size(88, 27);
            buttonHelp.TabIndex = 20;
            buttonHelp.Text = "Help";
            buttonHelp.UseVisualStyleBackColor = true;
            buttonHelp.Click += ButtonHelp_Click;
            // 
            // checkBoxDisablePlugin
            // 
            checkBoxDisablePlugin.AutoSize = true;
            checkBoxDisablePlugin.Location = new Point(317, 428);
            checkBoxDisablePlugin.Margin = new Padding(4, 3, 4, 3);
            checkBoxDisablePlugin.Name = "checkBoxDisablePlugin";
            checkBoxDisablePlugin.Size = new Size(134, 19);
            checkBoxDisablePlugin.TabIndex = 17;
            checkBoxDisablePlugin.Text = "Disable Entire Plugin";
            checkBoxDisablePlugin.UseVisualStyleBackColor = true;
            checkBoxDisablePlugin.CheckedChanged += CheckBoxDisablePlugin_CheckedChanged;
            // 
            // textBox_Color
            // 
            textBox_Color.Location = new Point(226, 46);
            textBox_Color.Margin = new Padding(4, 3, 4, 3);
            textBox_Color.Multiline = true;
            textBox_Color.Name = "textBox_Color";
            textBox_Color.Size = new Size(116, 28);
            textBox_Color.TabIndex = 16;
            textBox_Color.Text = "Example";
            textBox_Color.TextAlign = HorizontalAlignment.Center;
            // 
            // buttonForeground
            // 
            buttonForeground.Location = new Point(196, 15);
            buttonForeground.Margin = new Padding(4, 3, 4, 3);
            buttonForeground.Name = "buttonForeground";
            buttonForeground.Size = new Size(88, 27);
            buttonForeground.TabIndex = 11;
            buttonForeground.Text = "Foreground";
            buttonForeground.UseVisualStyleBackColor = true;
            buttonForeground.Click += ButtonForeground_Click;
            // 
            // buttonBackground
            // 
            buttonBackground.Location = new Point(290, 15);
            buttonBackground.Margin = new Padding(4, 3, 4, 3);
            buttonBackground.Name = "buttonBackground";
            buttonBackground.Size = new Size(88, 27);
            buttonBackground.TabIndex = 10;
            buttonBackground.Text = "Background";
            buttonBackground.UseVisualStyleBackColor = true;
            buttonBackground.Click += ButtonBackground_Click;
            // 
            // buttonFont
            // 
            buttonFont.Location = new Point(385, 15);
            buttonFont.Margin = new Padding(4, 3, 4, 3);
            buttonFont.Name = "buttonFont";
            buttonFont.Size = new Size(88, 27);
            buttonFont.TabIndex = 12;
            buttonFont.Text = "Font…";
            buttonFont.UseVisualStyleBackColor = true;
            buttonFont.Click += ButtonFont_Click;
            // 
            // buttonLinkColor
            // 
            buttonLinkColor.Location = new Point(385, 48);
            buttonLinkColor.Margin = new Padding(4, 3, 4, 3);
            buttonLinkColor.Name = "buttonLinkColor";
            buttonLinkColor.Size = new Size(88, 27);
            buttonLinkColor.TabIndex = 13;
            buttonLinkColor.Text = "Link Color";
            buttonLinkColor.UseVisualStyleBackColor = true;
            buttonLinkColor.Click += ButtonLinkColor_Click;
            // 
            // buttonTimerColor
            // 
            buttonTimerColor.Location = new Point(385, 82);
            buttonTimerColor.Margin = new Padding(4, 3, 4, 3);
            buttonTimerColor.Name = "buttonTimerColor";
            buttonTimerColor.Size = new Size(88, 27);
            buttonTimerColor.TabIndex = 14;
            buttonTimerColor.Text = "Timer Color";
            buttonTimerColor.UseVisualStyleBackColor = true;
            buttonTimerColor.Click += ButtonTimerColor_Click;
            // 
            // buttonDefaults
            // 
            buttonDefaults.Location = new Point(226, 77);
            buttonDefaults.Margin = new Padding(4, 3, 4, 3);
            buttonDefaults.Name = "buttonDefaults";
            buttonDefaults.Size = new Size(117, 25);
            buttonDefaults.TabIndex = 15;
            buttonDefaults.Text = "Reset Defaults";
            buttonDefaults.UseVisualStyleBackColor = true;
            buttonDefaults.Click += ButtonDefaults_Click;
            // 
            // listbox_openwindows
            // 
            listbox_openwindows.FormattingEnabled = true;
            listbox_openwindows.ItemHeight = 15;
            listbox_openwindows.Location = new Point(15, 123);
            listbox_openwindows.Margin = new Padding(4, 3, 4, 3);
            listbox_openwindows.Name = "listbox_openwindows";
            listbox_openwindows.Size = new Size(184, 169);
            listbox_openwindows.TabIndex = 9;
            // 
            // listBox_ignores
            // 
            listBox_ignores.FormattingEnabled = true;
            listBox_ignores.ItemHeight = 15;
            listBox_ignores.Location = new Point(278, 123);
            listBox_ignores.Margin = new Padding(4, 3, 4, 3);
            listBox_ignores.Name = "listBox_ignores";
            listBox_ignores.Size = new Size(182, 169);
            listBox_ignores.TabIndex = 8;
            // 
            // button_ignore
            // 
            button_ignore.Location = new Point(15, 300);
            button_ignore.Margin = new Padding(4, 3, 4, 3);
            button_ignore.Name = "button_ignore";
            button_ignore.Size = new Size(63, 27);
            button_ignore.TabIndex = 7;
            button_ignore.Text = "Ignore";
            button_ignore.UseVisualStyleBackColor = true;
            button_ignore.Click += Button_ignore_Click;
            // 
            // button_clear
            // 
            button_clear.Location = new Point(397, 300);
            button_clear.Margin = new Padding(4, 3, 4, 3);
            button_clear.Name = "button_clear";
            button_clear.Size = new Size(64, 27);
            button_clear.TabIndex = 6;
            button_clear.Text = "Clear";
            button_clear.UseVisualStyleBackColor = true;
            button_clear.Click += Button_clear_Click;
            // 
            // button_clearall
            // 
            button_clearall.Location = new Point(290, 300);
            button_clearall.Margin = new Padding(4, 3, 4, 3);
            button_clearall.Name = "button_clearall";
            button_clearall.Size = new Size(88, 27);
            button_clearall.TabIndex = 5;
            button_clearall.Text = "Clear All";
            button_clearall.UseVisualStyleBackColor = true;
            button_clearall.Click += Button_clearall_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(278, 102);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(62, 15);
            label1.TabIndex = 4;
            label1.Text = "Ignore List";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(15, 102);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(88, 15);
            label2.TabIndex = 3;
            label2.Text = "Open Windows";
            // 
            // button_closewindow
            // 
            button_closewindow.Location = new Point(88, 300);
            button_closewindow.Margin = new Padding(4, 3, 4, 3);
            button_closewindow.Name = "button_closewindow";
            button_closewindow.Size = new Size(88, 27);
            button_closewindow.TabIndex = 2;
            button_closewindow.Text = "Close Win";
            button_closewindow.UseVisualStyleBackColor = true;
            button_closewindow.Click += Button_closewindow_Click;
            // 
            // cbDisableOtherInjuries
            // 
            cbDisableOtherInjuries.AutoSize = true;
            cbDisableOtherInjuries.Location = new Point(15, 52);
            cbDisableOtherInjuries.Margin = new Padding(4, 3, 4, 3);
            cbDisableOtherInjuries.Name = "cbDisableOtherInjuries";
            cbDisableOtherInjuries.Size = new Size(190, 19);
            cbDisableOtherInjuries.TabIndex = 1;
            cbDisableOtherInjuries.Text = "Disable Other Injuries Windows";
            cbDisableOtherInjuries.UseVisualStyleBackColor = true;
            cbDisableOtherInjuries.CheckedChanged += CbDisableOtherInjuries_CheckedChanged;
            // 
            // cbDisableSelfInjuries
            // 
            cbDisableSelfInjuries.AutoSize = true;
            cbDisableSelfInjuries.Location = new Point(15, 75);
            cbDisableSelfInjuries.Margin = new Padding(4, 3, 4, 3);
            cbDisableSelfInjuries.Name = "cbDisableSelfInjuries";
            cbDisableSelfInjuries.Size = new Size(174, 19);
            cbDisableSelfInjuries.TabIndex = 0;
            cbDisableSelfInjuries.Text = "Disable Self Injuries Window";
            cbDisableSelfInjuries.UseVisualStyleBackColor = true;
            cbDisableSelfInjuries.CheckedChanged += CbDisableSelfInjuries_CheckedChanged;
            // 
            // trackBarScale
            // 
            trackBarScale.Location = new Point(15, 355);
            trackBarScale.Margin = new Padding(4, 3, 4, 3);
            trackBarScale.Name = "trackBarScale";
            trackBarScale.Size = new Size(385, 45);
            trackBarScale.TabIndex = 23;
            trackBarScale.ValueChanged += TrackBarScale_ValueChanged;
            // 
            // labelScaleValue
            // 
            labelScaleValue.AutoSize = true;
            labelScaleValue.Location = new Point(408, 361);
            labelScaleValue.Margin = new Padding(4, 0, 4, 0);
            labelScaleValue.Name = "labelScaleValue";
            labelScaleValue.Size = new Size(33, 15);
            labelScaleValue.TabIndex = 24;
            labelScaleValue.Text = "1.00x";
            // 
            // labelScaleTitle
            // 
            labelScaleTitle.AutoSize = true;
            labelScaleTitle.Location = new Point(15, 335);
            labelScaleTitle.Margin = new Padding(4, 0, 4, 0);
            labelScaleTitle.Name = "labelScaleTitle";
            labelScaleTitle.Size = new Size(259, 15);
            labelScaleTitle.TabIndex = 22;
            labelScaleTitle.Text = "UI Scale — 1.00x to 1.50x  (1.25x recommended):";
            // 
            // FormOptionWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(478, 462);
            ControlBox = false;
            Controls.Add(cbDisableSelfInjuries);
            Controls.Add(cbDisableOtherInjuries);
            Controls.Add(button_closewindow);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(button_clearall);
            Controls.Add(button_clear);
            Controls.Add(button_ignore);
            Controls.Add(listBox_ignores);
            Controls.Add(listbox_openwindows);
            Controls.Add(buttonBackground);
            Controls.Add(buttonForeground);
            Controls.Add(buttonFont);
            Controls.Add(buttonLinkColor);
            Controls.Add(buttonTimerColor);
            Controls.Add(buttonDefaults);
            Controls.Add(textBox_Color);
            Controls.Add(checkBoxDisablePlugin);
            Controls.Add(ButtonClose);
            Controls.Add(ButtonCancel);
            Controls.Add(buttonHelp);
            Controls.Add(CheckBoxStowContainer);
            Controls.Add(labelScaleTitle);
            Controls.Add(trackBarScale);
            Controls.Add(labelScaleValue);
            Margin = new Padding(4, 3, 4, 3);
            Name = "FormOptionWindow";
            Text = "Dynamic Window Options";
            ((ISupportInitialize)trackBarScale).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        private void CheckBoxStowContainer_CheckedChanged(object? sender, EventArgs e)
        {
            _plugin.bStowContainer = CheckBoxStowContainer.Checked;
        }

        private void CheckBoxDisablePlugin_CheckedChanged(object? sender, EventArgs e)
        {
            if (checkBoxDisablePlugin.Checked)
            {
                _plugin.bPluginEnabled = false;
                _plugin.documents.Clear();

                foreach (DwForm form in _plugin.forms.ToList())
                    form.Close();

                _plugin.forms.Clear();
            }
            else
            {
                _plugin.bPluginEnabled = true;
            }
        }

        private void ButtonForeground_Click(object? sender, EventArgs e)
        {
            using var colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                Color = _plugin.formfore
            };
            if (colorDialog.ShowDialog() != DialogResult.Cancel)
            {
                textBox_Color.ForeColor = colorDialog.Color;
                _plugin.formfore = colorDialog.Color;
            }
            Update();
        }

        private void ButtonBackground_Click(object? sender, EventArgs e)
        {
            using var colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                Color = _plugin.formback
            };
            if (colorDialog.ShowDialog() != DialogResult.Cancel)
            {
                textBox_Color.BackColor = colorDialog.Color;
                _plugin.formback = colorDialog.Color;
            }
            Update();
        }

        private void ButtonFont_Click(object? sender, EventArgs e)
        {
            using var picker = new FontPickerDialog(_plugin.FontFamilyName, _plugin.FontStyleChoice)
            {
                Owner = this
            };
            if (picker.ShowDialog() == DialogResult.OK)
            {
                _plugin.FontFamilyName = picker.SelectedFamily;
                _plugin.FontStyleChoice = picker.SelectedStyle;
                ApplyFontPreview();
            }
        }

        private void ApplyFontPreview()
        {
            float size = textBox_Color.Font.Size;
            try { textBox_Color.Font = new Font(_plugin.FontFamilyName, size, _plugin.FontStyleChoice); }
            catch
            {
                try { textBox_Color.Font = new Font(_plugin.FontFamilyName, size); } catch { /* keep current */ }
            }
        }

        private void ButtonLinkColor_Click(object? sender, EventArgs e)
        {
            using var colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                Color = _plugin.linkColor
            };
            if (colorDialog.ShowDialog() != DialogResult.Cancel)
            {
                _plugin.linkColor = colorDialog.Color;
                buttonLinkColor.ForeColor = colorDialog.Color;
            }
        }

        private void ButtonTimerColor_Click(object? sender, EventArgs e)
        {
            using var colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                Color = _plugin.timerBarColor
            };
            if (colorDialog.ShowDialog() != DialogResult.Cancel)
            {
                _plugin.timerBarColor = colorDialog.Color;
                buttonTimerColor.ForeColor = colorDialog.Color;
            }
        }

        private void ButtonDefaults_Click(object? sender, EventArgs e)
        {
            // Reset appearance settings to their defaults (these mirror the defaults in LoadSave).
            _plugin.formfore = Color.White;
            _plugin.formback = Color.Black;
            _plugin.linkColor = Color.Blue;
            _plugin.timerBarColor = Color.RoyalBlue;
            _plugin.FontFamilyName = SystemFonts.DefaultFont.Name;
            _plugin.FontStyleChoice = FontStyle.Regular;
            _plugin.Scale = 1.0f;
            trackBarScale.Value = 100;
            labelScaleValue.Text = _plugin.Scale.ToString("F2") + "x";

            // Refresh the previews so the reset is visible immediately (OK saves, Cancel reverts).
            textBox_Color.ForeColor = _plugin.formfore;
            textBox_Color.BackColor = _plugin.formback;
            buttonLinkColor.ForeColor = _plugin.linkColor;
            buttonTimerColor.ForeColor = _plugin.timerBarColor;
            ApplyFontPreview();
        }

        private void ButtonClose_Click(object? sender, EventArgs e)
        {
            CloseHelpWindow();
            _plugin.loadSave.Save();
            Close();
        }

        private void CloseHelpWindow()
        {
            const string id = "commandHelp";

            foreach (var f in _plugin.forms.ToList())
            {
                if (!f.IsDisposed && f.Name == id)
                    f.Close();
            }
        }

        private void ButtonHelp_Click(object? sender, EventArgs e)
        {
            const string id = "commandHelp";

            // Already open? Just surface it rather than stacking duplicates.
            foreach (var f in _plugin.forms)
                if (!f.IsDisposed && f.Name == id) { f.BringToFront(); f.Focus(); return; }

            // Same themed, read-only (selectable/copyable) RichTextBox window the profile help
            // uses. Monospaced so the dot-leader columns in HelpWindows.CommandHelp line up.
            var win = _plugin.CreateWindow(id, "Window & Plugin Commands", _plugin.S(520), _plugin.S(440));
            win.FormBody.Visible = true;
            win.FormBody.AutoScroll = true;

            var box = new RichTextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                WordWrap = false,
                BackColor = _plugin.formback,
                ForeColor = _plugin.formfore,
                Font = new Font(FontFamily.GenericMonospace, _plugin.InfoFont.Size, FontStyle.Regular),
                Text = new HelpWindows().CommandHelp,
            };

            // Close button row along the bottom (the form's top-right X still works too).
            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = _plugin.S(40),
                Padding = new Padding(_plugin.S(6)),
                BackColor = _plugin.formback,
            };
            var closeButton = new Button { Text = "Close", AutoSize = true };
            closeButton.Click += delegate { win.Close(); };
            bottom.Controls.Add(closeButton);

            win.FormBody.Controls.Add(box);      // Fill added first so the bottom row sits beneath it
            win.FormBody.Controls.Add(bottom);
            win.CancelButton = closeButton;      // Esc also closes
            win.ShowForm();
        }

        private void ButtonCancel_Click(object? sender, EventArgs e)
        {
            // Revert every setting to its state when the window opened, then close WITHOUT
            // saving — so nothing this session touched is persisted to the XML.
            _plugin.formfore = _origFore;
            _plugin.formback = _origBack;
            _plugin.linkColor = _origLink;
            _plugin.timerBarColor = _origTimer;
            _plugin.FontFamilyName = _origFontFamily;
            _plugin.FontStyleChoice = _origFontStyle;
            _plugin.Scale = _origScale;
            _plugin.bStowContainer = _origStow;
            _plugin.bPluginEnabled = _origEnabled;
            _plugin.bDisableOtherInjuries = _origDisableOther;
            _plugin.bDisableSelfInjuries = _origDisableSelf;
            _plugin.ignorelist.Clear();
            _plugin.ignorelist.AddRange(_origIgnore);
            CloseHelpWindow();
            Close();
        }

        private void Button_ignore_Click(object? sender, EventArgs e)
        {
            if (listbox_openwindows.SelectedIndex == -1) return;

            string selectedName = listbox_openwindows.SelectedItem!.ToString()!;
            DwForm? form1 = _plugin.forms.FirstOrDefault(f => f.Name == selectedName);
            if (form1 == null) return;

            string ignoreId = form1.Name;
            if (!_plugin.ignorelist.Contains(ignoreId))
            {
                listBox_ignores.Items.Add(ignoreId);
                _plugin.ignorelist.Add(ignoreId);
            }

            _plugin.forms.Remove(form1);
            form1.Close();
            listbox_openwindows.Items.Remove(form1.Name);
        }

        private void Button_closewindow_Click(object? sender, EventArgs e)
        {
            if (listbox_openwindows.SelectedIndex == -1) return;

            string selectedName = listbox_openwindows.SelectedItem!.ToString()!;
            DwForm? form1 = _plugin.forms.FirstOrDefault(f => f.Name == selectedName);
            if (form1 == null) return;

            listbox_openwindows.Items.Remove(form1.Name);
            _plugin.forms.Remove(form1);
            form1.Close();
        }

        private void Button_clearall_Click(object? sender, EventArgs e)
        {
            // Remove only current character's ignores from both lists
            string prefix = _plugin.characterName + ".";
            _plugin.ignorelist.RemoveAll(id =>
                id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            for (int i = listBox_ignores.Items.Count - 1; i >= 0; i--)
            {
                string item = (string)listBox_ignores.Items[i];
                if (item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    listBox_ignores.Items.RemoveAt(i);
            }
        }

        private void Button_clear_Click(object? sender, EventArgs e)
        {
            if (listBox_ignores.SelectedIndex < 0) return;

            string selected = (string)listBox_ignores.SelectedItem!;
            _plugin.ignorelist.Remove(selected);
            listBox_ignores.Items.Remove(selected);
        }

        private void CbDisableOtherInjuries_CheckedChanged(object? sender, EventArgs e)
        {
            _plugin.bDisableOtherInjuries = cbDisableOtherInjuries.Checked;
        }

        private void CbDisableSelfInjuries_CheckedChanged(object? sender, EventArgs e)
        {
            _plugin.bDisableSelfInjuries = cbDisableSelfInjuries.Checked;
        }

        private void TrackBarScale_ValueChanged(object? sender, EventArgs e)
        {
            // Snap to nearest 5 so values are always clean multiples of 0.05
            int snapped = (int)Math.Round(trackBarScale.Value / 5.0) * 5;
            if (trackBarScale.Value != snapped)
            {
                trackBarScale.Value = snapped;
                return;
            }

            float newScale = snapped / 100f;
            _plugin.Scale = newScale;
            labelScaleValue.Text = newScale.ToString("F2") + "x";
        }
    }
}
