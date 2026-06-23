using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DynamicWindows
{
    public class FontPickerDialog : Form
    {
        private readonly ComboBox _family = new();
        private readonly CheckBox _bold = new();
        private readonly CheckBox _italic = new();
        private readonly CheckBox _underline = new();
        private readonly Label _preview = new();
        private readonly Button _ok = new();
        private readonly Button _cancel = new();

        public string SelectedFamily { get; private set; }
        public FontStyle SelectedStyle { get; private set; }

        public FontPickerDialog(string family, FontStyle style)
        {
            SelectedFamily = family;
            SelectedStyle = style;

            Text = "Select Font";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(300, 175);

            var lbl = new Label { Text = "Font:", AutoSize = true, Location = new Point(12, 13) };

            _family.DropDownStyle = ComboBoxStyle.DropDownList;
            _family.Location = new Point(12, 32);
            _family.Width = 276;
            foreach (FontFamily ff in FontFamily.Families.OrderBy(f => f.Name))
                _family.Items.Add(ff.Name);
            int idx = _family.Items.IndexOf(family);
            if (idx < 0) idx = _family.Items.IndexOf(SystemFonts.DefaultFont.Name);
            _family.SelectedIndex = Math.Max(0, idx);
            _family.SelectedIndexChanged += (s, e) => UpdatePreview();

            _bold.Text = "Bold";            _bold.AutoSize = true;      _bold.Location = new Point(12, 66);
            _italic.Text = "Italic";        _italic.AutoSize = true;    _italic.Location = new Point(78, 66);
            _underline.Text = "Underline";  _underline.AutoSize = true; _underline.Location = new Point(146, 66);
            _bold.Checked = style.HasFlag(FontStyle.Bold);
            _italic.Checked = style.HasFlag(FontStyle.Italic);
            _underline.Checked = style.HasFlag(FontStyle.Underline);
            _bold.CheckedChanged += (s, e) => UpdatePreview();
            _italic.CheckedChanged += (s, e) => UpdatePreview();
            _underline.CheckedChanged += (s, e) => UpdatePreview();

            _preview.Text = "AaBbYyZz  0123";
            _preview.TextAlign = ContentAlignment.MiddleCenter;
            _preview.BorderStyle = BorderStyle.FixedSingle;
            _preview.Location = new Point(12, 92);
            _preview.Size = new Size(276, 32);

            _ok.Text = "OK";
            _ok.DialogResult = DialogResult.OK;
            _ok.Location = new Point(132, 140);
            _ok.Size = new Size(75, 23);
            _ok.Click += (s, e) =>
            {
                SelectedFamily = (string)_family.SelectedItem!;
                SelectedStyle = CurrentStyle();
            };

            _cancel.Text = "Cancel";
            _cancel.DialogResult = DialogResult.Cancel;
            _cancel.Location = new Point(213, 140);
            _cancel.Size = new Size(75, 23);

            AcceptButton = _ok;
            CancelButton = _cancel;

            Controls.AddRange(new Control[]
            {
                lbl, _family, _bold, _italic, _underline, _preview, _ok, _cancel
            });

            UpdatePreview();
        }

        private FontStyle CurrentStyle()
        {
            FontStyle s = FontStyle.Regular;
            if (_bold.Checked) s |= FontStyle.Bold;
            if (_italic.Checked) s |= FontStyle.Italic;
            if (_underline.Checked) s |= FontStyle.Underline;
            return s;
        }

        private void UpdatePreview()
        {
            if (_family.SelectedItem is not string fam) return;
            try { _preview.Font = new Font(fam, 12f, CurrentStyle()); }
            catch
            {
                try { _preview.Font = new Font(fam, 12f); } catch { /* leave previous */ }
            }
        }
    }
}
