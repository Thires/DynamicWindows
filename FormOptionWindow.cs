using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
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
    private CheckBox cbDisableOtherInjuries;
    private CheckBox cbDisableSelfInjuries;
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
      this.cbDisableOtherInjuries.Checked = this._plugin.bDisableOtherInjuries;
      this.cbDisableSelfInjuries.Checked = this._plugin.bDisableSelfInjuries;
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.Components != null)
        this.Components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
            this.CheckBoxStowContainer = new System.Windows.Forms.CheckBox();
            this.ButtonClose = new System.Windows.Forms.Button();
            this.checkBoxDisablePlugin = new System.Windows.Forms.CheckBox();
            this.textBox_Color = new System.Windows.Forms.TextBox();
            this.buttonForeground = new System.Windows.Forms.Button();
            this.buttonBackground = new System.Windows.Forms.Button();
            this.listbox_openwindows = new System.Windows.Forms.ListBox();
            this.listBox_ignores = new System.Windows.Forms.ListBox();
            this.button_ignore = new System.Windows.Forms.Button();
            this.button_clear = new System.Windows.Forms.Button();
            this.button_clearall = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.button_closewindow = new System.Windows.Forms.Button();
            this.cbDisableOtherInjuries = new System.Windows.Forms.CheckBox();
            this.cbDisableSelfInjuries = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // CheckBoxStowContainer
            // 
            this.CheckBoxStowContainer.AutoSize = true;
            this.CheckBoxStowContainer.Location = new System.Drawing.Point(13, 13);
            this.CheckBoxStowContainer.Name = "CheckBoxStowContainer";
            this.CheckBoxStowContainer.Size = new System.Drawing.Size(140, 17);
            this.CheckBoxStowContainer.TabIndex = 0;
            this.CheckBoxStowContainer.Text = "Stow Container Window";
            this.CheckBoxStowContainer.UseVisualStyleBackColor = true;
            this.CheckBoxStowContainer.CheckedChanged += new System.EventHandler(this.CheckBoxStowContainer_CheckedChanged);
            // 
            // ButtonClose
            // 
            this.ButtonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonClose.Location = new System.Drawing.Point(12, 310);
            this.ButtonClose.Name = "ButtonClose";
            this.ButtonClose.Size = new System.Drawing.Size(75, 23);
            this.ButtonClose.TabIndex = 1;
            this.ButtonClose.Text = "OK";
            this.ButtonClose.UseVisualStyleBackColor = true;
            this.ButtonClose.Click += new System.EventHandler(this.ButtonClose_Click);
            // 
            // checkBoxDisablePlugin
            // 
            this.checkBoxDisablePlugin.AutoSize = true;
            this.checkBoxDisablePlugin.Location = new System.Drawing.Point(272, 316);
            this.checkBoxDisablePlugin.Name = "checkBoxDisablePlugin";
            this.checkBoxDisablePlugin.Size = new System.Drawing.Size(123, 17);
            this.checkBoxDisablePlugin.TabIndex = 2;
            this.checkBoxDisablePlugin.Text = "Disable Entire Plugin";
            this.checkBoxDisablePlugin.UseVisualStyleBackColor = true;
            this.checkBoxDisablePlugin.CheckedChanged += new System.EventHandler(this.CheckBoxDisablePlugin_CheckedChanged);
            // 
            // textBox_Color
            // 
            this.textBox_Color.Location = new System.Drawing.Point(194, 42);
            this.textBox_Color.Name = "textBox_Color";
            this.textBox_Color.Size = new System.Drawing.Size(100, 20);
            this.textBox_Color.TabIndex = 3;
            this.textBox_Color.Text = "Example";
            this.textBox_Color.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // buttonForeground
            // 
            this.buttonForeground.Location = new System.Drawing.Point(168, 13);
            this.buttonForeground.Name = "buttonForeground";
            this.buttonForeground.Size = new System.Drawing.Size(75, 23);
            this.buttonForeground.TabIndex = 4;
            this.buttonForeground.Text = "Foreground";
            this.buttonForeground.UseVisualStyleBackColor = true;
            this.buttonForeground.Click += new System.EventHandler(this.ButtonForeground_Click);
            // 
            // buttonBackground
            // 
            this.buttonBackground.Location = new System.Drawing.Point(249, 13);
            this.buttonBackground.Name = "buttonBackground";
            this.buttonBackground.Size = new System.Drawing.Size(75, 23);
            this.buttonBackground.TabIndex = 5;
            this.buttonBackground.Text = "Background";
            this.buttonBackground.UseVisualStyleBackColor = true;
            this.buttonBackground.Click += new System.EventHandler(this.ButtonBackground_Click);
            // 
            // listbox_openwindows
            // 
            this.listbox_openwindows.FormattingEnabled = true;
            this.listbox_openwindows.Location = new System.Drawing.Point(13, 107);
            this.listbox_openwindows.Name = "listbox_openwindows";
            this.listbox_openwindows.Size = new System.Drawing.Size(158, 147);
            this.listbox_openwindows.TabIndex = 6;
            // 
            // listBox_ignores
            // 
            this.listBox_ignores.FormattingEnabled = true;
            this.listBox_ignores.Location = new System.Drawing.Point(238, 107);
            this.listBox_ignores.Name = "listBox_ignores";
            this.listBox_ignores.Size = new System.Drawing.Size(157, 147);
            this.listBox_ignores.TabIndex = 7;
            // 
            // button_ignore
            // 
            this.button_ignore.Location = new System.Drawing.Point(13, 260);
            this.button_ignore.Name = "button_ignore";
            this.button_ignore.Size = new System.Drawing.Size(54, 23);
            this.button_ignore.TabIndex = 8;
            this.button_ignore.Text = "Ignore";
            this.button_ignore.UseVisualStyleBackColor = true;
            this.button_ignore.Click += new System.EventHandler(this.Button_ignore_Click);
            // 
            // button_clear
            // 
            this.button_clear.Location = new System.Drawing.Point(340, 260);
            this.button_clear.Name = "button_clear";
            this.button_clear.Size = new System.Drawing.Size(55, 23);
            this.button_clear.TabIndex = 9;
            this.button_clear.Text = "Clear";
            this.button_clear.UseVisualStyleBackColor = true;
            this.button_clear.Click += new System.EventHandler(this.Button_clear_Click);
            // 
            // button_clearall
            // 
            this.button_clearall.Location = new System.Drawing.Point(249, 260);
            this.button_clearall.Name = "button_clearall";
            this.button_clearall.Size = new System.Drawing.Size(75, 23);
            this.button_clearall.TabIndex = 10;
            this.button_clearall.Text = "Clear All";
            this.button_clearall.UseVisualStyleBackColor = true;
            this.button_clearall.Click += new System.EventHandler(this.Button_clearall_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(238, 88);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Ignore List";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 87);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Open Windows";
            // 
            // button_closewindow
            // 
            this.button_closewindow.Location = new System.Drawing.Point(78, 260);
            this.button_closewindow.Name = "button_closewindow";
            this.button_closewindow.Size = new System.Drawing.Size(75, 23);
            this.button_closewindow.TabIndex = 13;
            this.button_closewindow.Text = "Close";
            this.button_closewindow.UseVisualStyleBackColor = true;
            this.button_closewindow.Click += new System.EventHandler(this.Button_closewindow_Click);
            // 
            // cbDisableOtherInjuries
            // 
            this.cbDisableOtherInjuries.AutoSize = true;
            this.cbDisableOtherInjuries.Checked = true;
            this.cbDisableOtherInjuries.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbDisableOtherInjuries.Location = new System.Drawing.Point(12, 36);
            this.cbDisableOtherInjuries.Name = "cbDisableOtherInjuries";
            this.cbDisableOtherInjuries.Size = new System.Drawing.Size(139, 17);
            this.cbDisableOtherInjuries.TabIndex = 14;
            this.cbDisableOtherInjuries.Text = "Disable Empath Healing";
            this.cbDisableOtherInjuries.UseVisualStyleBackColor = true;
            this.cbDisableOtherInjuries.CheckedChanged += new System.EventHandler(this.cbDisableOtherInjuries_CheckedChanged);
            // 
            // cbDisableSelfInjuries
            // 
            this.cbDisableSelfInjuries.AutoSize = true;
            this.cbDisableSelfInjuries.Checked = true;
            this.cbDisableSelfInjuries.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbDisableSelfInjuries.Location = new System.Drawing.Point(12, 59);
            this.cbDisableSelfInjuries.Name = "cbDisableSelfInjuries";
            this.cbDisableSelfInjuries.Size = new System.Drawing.Size(118, 17);
            this.cbDisableSelfInjuries.TabIndex = 15;
            this.cbDisableSelfInjuries.Text = "Disable Self Injuries";
            this.cbDisableSelfInjuries.UseVisualStyleBackColor = true;
            this.cbDisableSelfInjuries.CheckedChanged += new System.EventHandler(this.cbDisableSelfInjuries_CheckedChanged);
            // 
            // FormOptionWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.ButtonClose;
            this.ClientSize = new System.Drawing.Size(407, 345);
            this.ControlBox = false;
            this.Controls.Add(this.cbDisableSelfInjuries);
            this.Controls.Add(this.cbDisableOtherInjuries);
            this.Controls.Add(this.button_closewindow);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button_clearall);
            this.Controls.Add(this.button_clear);
            this.Controls.Add(this.button_ignore);
            this.Controls.Add(this.listBox_ignores);
            this.Controls.Add(this.listbox_openwindows);
            this.Controls.Add(this.buttonBackground);
            this.Controls.Add(this.buttonForeground);
            this.Controls.Add(this.textBox_Color);
            this.Controls.Add(this.checkBoxDisablePlugin);
            this.Controls.Add(this.ButtonClose);
            this.Controls.Add(this.CheckBoxStowContainer);
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

            foreach (Form form in this._plugin.forms.Cast<Form>().ToList())
                form.Close();

            this._plugin.forms.Clear();
        }
        else
        {
            this._plugin.bPluginEnabled = true;
        }
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
        this._plugin.loadSave.Save();
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

            string ignoreId = form1.Name;

            if (!this._plugin.ignorelist.Contains(ignoreId))
            {
                this.listBox_ignores.Items.Add(ignoreId);
                this._plugin.ignorelist.Add(ignoreId);
            }

            if (form1 is SkinnedMDIChild skinnedForm)
                this._plugin.forms.Remove(skinnedForm);

            form1.Close();
            this.listbox_openwindows.Items.Remove(form1.Name);

            // Optional: save immediately
            this._plugin.loadSave.Save();
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
            // Remove only current character's ignores
            string prefix = this._plugin.characterName + ".";
            for (int i = this._plugin.ignorelist.Count - 1; i >= 0; i--)
            {
                if (this._plugin.ignorelist[i] is string id && id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    this._plugin.ignorelist.RemoveAt(i);
                }
            }

            // Remove only those from UI
            for (int i = listBox_ignores.Items.Count - 1; i >= 0; i--)
            {
                string item = (string)listBox_ignores.Items[i];
                if (item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    listBox_ignores.Items.RemoveAt(i);
                }
            }
        }


        private void Button_clear_Click(object sender, EventArgs e)
        {
            if (listBox_ignores.SelectedIndex >= 0)
            {
                string selected = (string)listBox_ignores.Items[listBox_ignores.SelectedIndex];
                this._plugin.ignorelist.Remove(selected);
                listBox_ignores.Items.Remove(selected);
            }
        }


        private void cbDisableOtherInjuries_CheckedChanged(object sender, EventArgs e)
        {
            this._plugin.bDisableOtherInjuries = cbDisableOtherInjuries.Checked;
            this._plugin.loadSave.Save();
        }


        private void cbDisableSelfInjuries_CheckedChanged(object sender, EventArgs e)
        {
            this._plugin.bDisableSelfInjuries = cbDisableSelfInjuries.Checked;
            this._plugin.loadSave.Save();
        }

    }
}
