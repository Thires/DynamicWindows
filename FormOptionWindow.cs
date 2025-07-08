using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicWindows
{
  public class FormOptionWindow : Form
  {
    private IContainer components;
    private CheckBox CheckBoxStowContainer;
    private Button ButtonClose;
    private CheckBox checkBoxDisablePlugin;
    private TextBox textBox_Color;
    private Button buttonForeground;
    private Button buttonBackground;
    private ListBox listbox_openwindows;
    private ListBox listBox_ignores;
    private Button button_ignore;
    private Button button_clear;
    private Button button_clearall;
    private Label label1;
    private Label label2;
    private Button button_closewindow;
    private readonly Plugin _plugin;
	public IContainer Components { get => components; set => components = value; }
	
	public FormOptionWindow(Plugin PlugIn)
    {
      this._plugin = PlugIn;
      this.InitializeComponent();
      foreach (Control control in this._plugin.forms)
        this.listbox_openwindows.Items.Add((object) control.Name);
      foreach (string str in this._plugin.ignorelist)
        this.listBox_ignores.Items.Add((object) str);
      this.textBox_Color.ForeColor = this._plugin.formfore;
      this.textBox_Color.BackColor = this._plugin.formback;
      this.checkBoxDisablePlugin.Checked = !this._plugin.bPluginEnabled;
      this.CheckBoxStowContainer.Checked = this._plugin.bStowContainer;
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.Components != null)
        this.Components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      this.CheckBoxStowContainer = new CheckBox();
      this.ButtonClose = new Button();
      this.checkBoxDisablePlugin = new CheckBox();
      this.textBox_Color = new TextBox();
      this.buttonForeground = new Button();
      this.buttonBackground = new Button();
      this.listbox_openwindows = new ListBox();
      this.listBox_ignores = new ListBox();
      this.button_ignore = new Button();
      this.button_clear = new Button();
      this.button_clearall = new Button();
      this.label1 = new Label();
      this.label2 = new Label();
      this.button_closewindow = new Button();
      this.SuspendLayout();
      this.CheckBoxStowContainer.AutoSize = true;
      this.CheckBoxStowContainer.Location = new Point(13, 13);
      this.CheckBoxStowContainer.Name = "CheckBoxStowContainer";
      this.CheckBoxStowContainer.Size = new Size(140, 17);
      this.CheckBoxStowContainer.TabIndex = 0;
      this.CheckBoxStowContainer.Text = "Stow Container Window";
      this.CheckBoxStowContainer.UseVisualStyleBackColor = true;
      this.CheckBoxStowContainer.CheckedChanged += new EventHandler(this.CheckBoxStowContainer_CheckedChanged);
      this.ButtonClose.DialogResult = DialogResult.Cancel;
      this.ButtonClose.Location = new Point(12, 310);
      this.ButtonClose.Name = "ButtonClose";
      this.ButtonClose.Size = new Size(75, 23);
      this.ButtonClose.TabIndex = 1;
      this.ButtonClose.Text = "OK";
      this.ButtonClose.UseVisualStyleBackColor = true;
      this.ButtonClose.Click += new EventHandler(this.ButtonClose_Click);
      this.checkBoxDisablePlugin.AutoSize = true;
      this.checkBoxDisablePlugin.Location = new Point(272, 316);
      this.checkBoxDisablePlugin.Name = "checkBoxDisablePlugin";
      this.checkBoxDisablePlugin.Size = new Size(123, 17);
      this.checkBoxDisablePlugin.TabIndex = 2;
      this.checkBoxDisablePlugin.Text = "Disable Entire Plugin";
      this.checkBoxDisablePlugin.UseVisualStyleBackColor = true;
      this.checkBoxDisablePlugin.CheckedChanged += new EventHandler(this.CheckBoxDisablePlugin_CheckedChanged);
      this.textBox_Color.Location = new Point(194, 42);
      this.textBox_Color.Name = "textBox_Color";
      this.textBox_Color.Size = new Size(100, 20);
      this.textBox_Color.TabIndex = 3;
      this.textBox_Color.Text = "Example";
      this.textBox_Color.TextAlign = HorizontalAlignment.Center;
      this.buttonForeground.Location = new Point(168, 13);
      this.buttonForeground.Name = "buttonForeground";
      this.buttonForeground.Size = new Size(75, 23);
      this.buttonForeground.TabIndex = 4;
      this.buttonForeground.Text = "Foreground";
      this.buttonForeground.UseVisualStyleBackColor = true;
      this.buttonForeground.Click += new EventHandler(this.ButtonForeground_Click);
      this.buttonBackground.Location = new Point(249, 13);
      this.buttonBackground.Name = "buttonBackground";
      this.buttonBackground.Size = new Size(75, 23);
      this.buttonBackground.TabIndex = 5;
      this.buttonBackground.Text = "Background";
      this.buttonBackground.UseVisualStyleBackColor = true;
      this.buttonBackground.Click += new EventHandler(this.ButtonBackground_Click);
      this.listbox_openwindows.FormattingEnabled = true;
      this.listbox_openwindows.Location = new Point(13, 107);
      this.listbox_openwindows.Name = "listbox_openwindows";
      this.listbox_openwindows.Size = new Size(158, 147);
      this.listbox_openwindows.TabIndex = 6;
      this.listBox_ignores.FormattingEnabled = true;
      this.listBox_ignores.Location = new Point(238, 107);
      this.listBox_ignores.Name = "listBox_ignores";
      this.listBox_ignores.Size = new Size(157, 147);
      this.listBox_ignores.TabIndex = 7;
      this.button_ignore.Location = new Point(13, 260);
      this.button_ignore.Name = "button_ignore";
      this.button_ignore.Size = new Size(54, 23);
      this.button_ignore.TabIndex = 8;
      this.button_ignore.Text = "Ignore";
      this.button_ignore.UseVisualStyleBackColor = true;
      this.button_ignore.Click += new EventHandler(this.Button_ignore_Click);
      this.button_clear.Location = new Point(340, 260);
      this.button_clear.Name = "button_clear";
      this.button_clear.Size = new Size(55, 23);
      this.button_clear.TabIndex = 9;
      this.button_clear.Text = "Clear";
      this.button_clear.UseVisualStyleBackColor = true;
      this.button_clear.Click += new EventHandler(this.Button_clear_Click);
      this.button_clearall.Location = new Point(249, 260);
      this.button_clearall.Name = "button_clearall";
      this.button_clearall.Size = new Size(75, 23);
      this.button_clearall.TabIndex = 10;
      this.button_clearall.Text = "Clear All";
      this.button_clearall.UseVisualStyleBackColor = true;
      this.button_clearall.Click += new EventHandler(this.Button_clearall_Click);
      this.label1.AutoSize = true;
      this.label1.Location = new Point(238, 88);
      this.label1.Name = "label1";
      this.label1.Size = new Size(56, 13);
      this.label1.TabIndex = 11;
      this.label1.Text = "Ignore List";
      this.label2.AutoSize = true;
      this.label2.Location = new Point(13, 87);
      this.label2.Name = "label2";
      this.label2.Size = new Size(80, 13);
      this.label2.TabIndex = 12;
      this.label2.Text = "Open Windows";
      this.button_closewindow.Location = new Point(78, 260);
      this.button_closewindow.Name = "button_closewindow";
      this.button_closewindow.Size = new Size(75, 23);
      this.button_closewindow.TabIndex = 13;
      this.button_closewindow.Text = "Close";
      this.button_closewindow.UseVisualStyleBackColor = true;
      this.button_closewindow.Click += new EventHandler(this.Button_closewindow_Click);
      this.AutoScaleDimensions = new SizeF(6f, 13f);
      this.AutoScaleMode = AutoScaleMode.Font;
      this.CancelButton = (IButtonControl) this.ButtonClose;
      this.ClientSize = new Size(407, 345);
      this.ControlBox = false;
      this.Controls.Add((Control) this.button_closewindow);
      this.Controls.Add((Control) this.label2);
      this.Controls.Add((Control) this.label1);
      this.Controls.Add((Control) this.button_clearall);
      this.Controls.Add((Control) this.button_clear);
      this.Controls.Add((Control) this.button_ignore);
      this.Controls.Add((Control) this.listBox_ignores);
      this.Controls.Add((Control) this.listbox_openwindows);
      this.Controls.Add((Control) this.buttonBackground);
      this.Controls.Add((Control) this.buttonForeground);
      this.Controls.Add((Control) this.textBox_Color);
      this.Controls.Add((Control) this.checkBoxDisablePlugin);
      this.Controls.Add((Control) this.ButtonClose);
      this.Controls.Add((Control) this.CheckBoxStowContainer);
      this.Name = "FormOptionWindow";
      this.Text = "Dynamic Window Options";
      this.ResumeLayout(false);
      this.PerformLayout();
    }

    private void CheckBoxStowContainer_CheckedChanged(object sender, EventArgs e)
    {
      if (this.CheckBoxStowContainer.Checked)
        this._plugin.bStowContainer = true;
      else
        this._plugin.bStowContainer = false;
    }

    private void CheckBoxDisablePlugin_CheckedChanged(object sender, EventArgs e)
    {
      if (this.checkBoxDisablePlugin.Checked)
      {
        this._plugin.bPluginEnabled = false;
        this._plugin.documents.Clear();
        foreach (Form form in this._plugin.forms)
          form.Close();
        this._plugin.forms.Clear();
      }
      else
        this._plugin.bPluginEnabled = true;
    }

    private void ButtonForeground_Click(object sender, EventArgs e)
    {
            ColorDialog colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                Color = this._plugin.formfore
            };
            if (colorDialog.ShowDialog() != DialogResult.Cancel)
      {
        this.textBox_Color.ForeColor = colorDialog.Color;
        this._plugin.formfore = colorDialog.Color;
      }
      this.Update();
    }

    private void ButtonBackground_Click(object sender, EventArgs e)
    {
            ColorDialog colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                Color = this._plugin.formback
            };
            if (colorDialog.ShowDialog() != DialogResult.Cancel)
      {
        this.textBox_Color.BackColor = colorDialog.Color;
        this._plugin.formback = colorDialog.Color;
      }
      this.Update();
    }

    private void ButtonClose_Click(object sender, EventArgs e)
    {
      this._plugin.SaveConfig();
      this.Close();
    }

        private void Button_ignore_Click(object sender, EventArgs e)
        {
            if (listbox_openwindows.SelectedIndex == -1)
                return;

            string selectedName = listbox_openwindows.SelectedItem.ToString();
            Form form1 = null;

            foreach (Form form2 in this._plugin.forms)
            {
                if (form2.Name == selectedName)
                {
                    form1 = form2;
                    break;
                }
            }

            if (form1 == null)
                return;

            if (!this._plugin.ignorelist.Contains(form1.Name))
            {
                this.listBox_ignores.Items.Add(form1.Name);
                this._plugin.ignorelist.Add(form1.Name);
            }

            if (form1 is SkinnedMDIChild skinnedForm)
                this._plugin.forms.Remove(skinnedForm);

            form1.Close();
            this.listbox_openwindows.Items.Remove(form1.Name);

            // Optional: save immediately
            this._plugin.SaveConfig();
        }


        private void Button_closewindow_Click(object sender, EventArgs e)
        {
            Form form1 = null;
            foreach (Form form2 in this._plugin.forms)
            {
                if (form2.Name.Equals(this.listbox_openwindows.Items[this.listbox_openwindows.SelectedIndex].ToString()))
                {
                    form1 = form2;
                    break;
                }
            }
            if (form1 == null)
                return;

            this.listbox_openwindows.Items.Remove(form1.Name);
            this._plugin.forms.Remove(form1);
            form1.Close();
        }


        private void Button_clearall_Click(object sender, EventArgs e)
    {
      this._plugin.ignorelist.Clear();
      this.listBox_ignores.Items.Clear();
    }

    private void Button_clear_Click(object sender, EventArgs e)
    {
        if (listBox_ignores.SelectedIndex >= 0)
        {
            string str = (string)listBox_ignores.Items[listBox_ignores.SelectedIndex];
            this._plugin.ignorelist.Remove(str);
            listBox_ignores.Items.Remove(str);
        }
    }
    }
}
