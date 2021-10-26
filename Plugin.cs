using GeniePlugin.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.Layout;
using System.Xml;

namespace DynamicWindows
{
	public class Plugin : IPlugin
	{
		public ArrayList forms = new ArrayList();
		public Hashtable documents = new Hashtable();
		public Color formback = Color.Black;
		public Color formfore = Color.White;
		public bool bPluginEnabled = true;
		public ArrayList ignorelist = new ArrayList();
		public Dictionary<string, Point> positionList = new Dictionary<string, Point>();
		public bool bStowContainer;
		public Form pForm;
		public IHost ghost;
		private string configPath;

		public bool Enabled
		{
			get
			{
				return this.bPluginEnabled;
			}
			set
			{
				if(value)
					this.bPluginEnabled = true;
				else
					this.bPluginEnabled = false;
			}
		}

		public string Name => "Dynamic Windows";

		public string Version => "1.2.5";

		public string Author => "Multiple Developers";

		public string Description => "Displays content windows specified through the XML stream from the game.";

		public void Initialize(IHost Host)
		{
			try
			{
				this.ghost = Host;
				this.pForm = Host.ParentForm;
				this.configPath = Host.get_Variable("PluginPath");
				this.LoadConfig();
			}
			catch
			{
			}
		}

		public void SaveConfig()
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.CreateXmlDeclaration("1.0", "utf-8", (string)null);
			XmlElement element1 = xmlDocument.CreateElement("DynamicWindows");
			xmlDocument.AppendChild((XmlNode)element1);
			XmlElement element2 = xmlDocument.CreateElement("Config");
			element2.SetAttribute("id", "foreground");
			element2.SetAttribute("color", ColorTranslator.ToHtml(this.formfore));
			element1.AppendChild((XmlNode)element2);
			XmlElement element3 = xmlDocument.CreateElement("Config");
			element3.SetAttribute("id", "background");
			element3.SetAttribute("color", ColorTranslator.ToHtml(this.formback));
			element1.AppendChild((XmlNode)element3);
			XmlElement element4 = xmlDocument.CreateElement("Config");
			element4.SetAttribute("id", "stowcontainer");
			element4.SetAttribute("enabled", this.bStowContainer.ToString());
			element1.AppendChild((XmlNode)element4);
			XmlElement element5 = xmlDocument.CreateElement("Config");
			element5.SetAttribute("id", "plugin");
			element5.SetAttribute("pluginenabled", this.bPluginEnabled.ToString());
			element1.AppendChild((XmlNode)element5);
			foreach(string str in this.ignorelist)
			{
				XmlElement element6 = xmlDocument.CreateElement("Ignore");
				element6.SetAttribute("id", str);
				element1.AppendChild((XmlNode)element6);
			}
			foreach(SkinnedMDIChild skinnedMdiChild in this.forms)
			{
				if(this.positionList.ContainsKey(skinnedMdiChild.Name))
					this.positionList.Remove(skinnedMdiChild.Name);
				this.positionList.Add(skinnedMdiChild.Name, skinnedMdiChild.Location);
			}
			foreach(KeyValuePair<string, Point> keyValuePair in this.positionList)
			{
				XmlElement element6 = xmlDocument.CreateElement("Position");
				element6.SetAttribute("id", keyValuePair.Key);
				element6.SetAttribute("X", keyValuePair.Value.X.ToString());
				element6.SetAttribute("Y", keyValuePair.Value.Y.ToString());
				element1.AppendChild((XmlNode)element6);
			}
			xmlDocument.Save(this.configPath + "/DynamicWindows.xml");
		}

		private void LoadConfig()
		{
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.Load(this.configPath + "/DynamicWindows.xml");
			}
			catch(Exception ex)
			{
				this.ghost.EchoText("Could not load Dynamic Windows Config File, It will be created when you change your options and hit OK: " + ex.Message);
				return;
			}
			foreach(XmlElement xmlElement in xmlDocument.GetElementsByTagName("Config"))
			{
				if(xmlElement.GetAttribute("id") == "foreground")
					this.formfore = ColorTranslator.FromHtml(xmlElement.GetAttribute("color"));
				else if(xmlElement.GetAttribute("id") == "background")
					this.formback = ColorTranslator.FromHtml(xmlElement.GetAttribute("color"));
				else if(xmlElement.GetAttribute("id") == "stowcontainer")
				{
					bool result = false;
					bool.TryParse(xmlElement.GetAttribute("enabled"), out result);
					this.bStowContainer = result;
				}
				else if(xmlElement.GetAttribute("id") == "plugin")
				{
					bool result = false;
					bool.TryParse(xmlElement.GetAttribute("pluginenabled"), out result);
					this.bPluginEnabled = result;
				}
			}
			foreach(XmlElement xmlElement in xmlDocument.GetElementsByTagName("Ignore"))
				this.ignorelist.Add((object)xmlElement.GetAttribute("id"));
			foreach(XmlElement xmlElement in xmlDocument.GetElementsByTagName("Position"))
				this.positionList.Add(xmlElement.GetAttribute("id"), new Point(int.Parse(xmlElement.GetAttribute("X")), int.Parse(xmlElement.GetAttribute("Y"))));
		}

		public string ParseInput(string Text)
		{
			if(!Text.StartsWith("/debugwindows"))
				return Text;
			this.ghost.EchoText("Form Count: " + this.forms.Count.ToString());
			foreach(Control control in this.forms)
				this.ghost.EchoText("    Form: " + control.Name);
			foreach(string str in (IEnumerable)this.documents.Keys)
				this.ghost.EchoText(string.Concat(new object[4]
				{
		  (object) "Variable: ",
		  (object) str,
		  (object) " - ",
		  this.documents[(object) str]
				}));
			return "";
		}

		public string ParseText(string Text, string Window)
		{
			if(Window.Trim().ToLower() == "main" || Window.Trim() == string.Empty)
				return this.ParseText(Text);
			else
				return Text;
		}

		public string ParseText(string Text)
		{
			return Text;
		}

		public void ParseXML(string XML)
		{
			if(!this.bPluginEnabled)
				return;
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml("<?xml version='1.0'?><root>" + XML + "</root>");
			foreach(XmlElement xmlElement in xmlDocument.DocumentElement.ChildNodes)
			{
				switch(xmlElement.Name)
				{
					case "openDialog":
						this.parse_xml_openwindow(xmlElement);
						continue;
					case "dialogData":
						this.parse_xml_updatewindow(xmlElement);
						continue;
					case "closeDialog":
						this.parse_xml_closewindow(xmlElement);
						continue;
					case "exposeDialog":
						this.parse_xml_exposewindow(xmlElement);
						continue;
					case "dynaStream":
						this.parse_set_stream(xmlElement);
						continue;
					case "clearStream":
						this.parse_clear_stream(xmlElement);
						continue;
					case "clearContainer":
						this.parse_container(xmlElement);
						continue;
					case "inv":
						this.parse_inventory(xmlElement);
						continue;
					default:
						continue;
				}
			}
		}

		public void Show()
		{
			FormOptionWindow formOptionWindow = new FormOptionWindow(this);
			formOptionWindow.MdiParent = this.pForm;
			formOptionWindow.TopMost = true;
			((Control)formOptionWindow).Show();
		}

		public void VariableChanged(string Variable)
		{
		}

		public void ParentClosing()
		{
			this.SaveConfig();
		}

		private void parse_xml_exposewindow(XmlElement elem)
		{
			foreach(SkinnedMDIChild skinnedMdiChild in this.forms)
			{
				if(skinnedMdiChild.Name.Equals(elem.GetAttribute("id")))
				{
					skinnedMdiChild.TopMost = true;
					skinnedMdiChild.Update();
					skinnedMdiChild.ShowForm();
				}
			}
		}

		private void parse_xml_closewindow(XmlElement elem)
		{
			SkinnedMDIChild skinnedMdiChild1 = (SkinnedMDIChild)null;
			foreach(SkinnedMDIChild skinnedMdiChild2 in this.forms)
			{
				if(skinnedMdiChild2.Name.Equals(elem.GetAttribute("id")))
					skinnedMdiChild1 = skinnedMdiChild2;
			}
			if(skinnedMdiChild1 == null)
				return;
			this.forms.Remove((object)skinnedMdiChild1);
			skinnedMdiChild1.Close();
		}

		private void parse_xml_updatewindow(XmlElement xelem)
		{
			SkinnedMDIChild dyndialog = (SkinnedMDIChild)null;
			foreach(SkinnedMDIChild skinnedMdiChild in this.forms)
			{
				if(skinnedMdiChild.Name.Equals(xelem.GetAttribute("id")))
					dyndialog = skinnedMdiChild;
			}
			if(dyndialog == null)
				return;
			dyndialog.formBody.Visible = false;
			foreach(XmlElement cbx in xelem.ChildNodes)
			{
				switch(cbx.Name)
				{
					case "label":
						this.parse_labels(cbx, dyndialog);
						continue;
					case "cmdButton":
						this.parse_command_buttons(cbx, dyndialog);
						continue;
					case "closeButton":
						this.parse_close_button(cbx, dyndialog);
						continue;
					case "checkBox":
						this.parse_check_box(cbx, dyndialog);
						continue;
					case "radio":
						this.parse_radio_button(cbx, dyndialog);
						continue;
					case "streamBox":
						this.parse_stream_box(cbx, dyndialog);
						continue;
					case "dropDownBox":
						this.parse_drop_down(cbx, dyndialog);
						continue;
					case "upDownEditBox":
						this.parse_numericupdown(cbx, dyndialog);
						continue;
					case "editBox":
						this.parse_edit_box(cbx, dyndialog);
						continue;
					case "progressBar":
						this.parse_progress_bar(cbx, dyndialog);
						continue;
					default:
						continue;
				}
			}
			dyndialog.formBody.Visible = true;
			dyndialog.formBody.AutoScroll = true;
			dyndialog.TopMost = true;
			dyndialog.Update();
			dyndialog.ShowForm();
		}

		public void parse_xml_openwindow(XmlElement xelem)
		{
			if(!xelem.GetAttribute("type").Equals("dynamic") || !xelem.HasAttribute("width") || !xelem.HasAttribute("height"))
				return;
			SkinnedMDIChild skinnedMdiChild1 = (SkinnedMDIChild)null;
			if(this.ignorelist.Contains((object)xelem.GetAttribute("id")))
				return;
			foreach(SkinnedMDIChild skinnedMdiChild2 in this.forms)
			{
				if(skinnedMdiChild2.Name.Equals(xelem.GetAttribute("id")))
					skinnedMdiChild1 = skinnedMdiChild2;
			}
			if(skinnedMdiChild1 != null)
			{
				this.forms.Remove((object)skinnedMdiChild1);
				skinnedMdiChild1.Close();
			}
			SkinnedMDIChild dyndialog = new SkinnedMDIChild(this.ghost, this);
			dyndialog.MdiParent = this.pForm;
			dyndialog.Text = xelem.GetAttribute("title");
			dyndialog.formBody.ForeColor = this.formfore;
			dyndialog.formBody.BackColor = this.formback;
			this.forms.Add((object)dyndialog);
			dyndialog.Name = xelem.GetAttribute("id");
			dyndialog.ClientSize = new Size(int.Parse(xelem.GetAttribute("width")), int.Parse(xelem.GetAttribute("height")) + 22);
			if(this.positionList.ContainsKey(xelem.GetAttribute("id")))
				dyndialog.Location = this.positionList[xelem.GetAttribute("id")];
			dyndialog.formBody.Visible = false;
			foreach(XmlElement xmlElement in xelem.FirstChild.ChildNodes)
			{
				switch(xmlElement.Name)
				{
					case "label":
						this.parse_labels(xmlElement, dyndialog);
						continue;
					case "cmdButton":
						this.parse_command_buttons(xmlElement, dyndialog);
						continue;
					case "closeButton":
						this.parse_close_button(xmlElement, dyndialog);
						continue;
					case "radio":
						this.parse_radio_button(xmlElement, dyndialog);
						continue;
					case "streamBox":
						this.parse_stream_box(xmlElement, dyndialog);
						continue;
					case "dropDownBox":
						this.parse_drop_down(xmlElement, dyndialog);
						continue;
					case "editBox":
						this.parse_edit_box(xmlElement, dyndialog);
						continue;
					case "upDownEditBox":
						this.parse_numericupdown(xmlElement, dyndialog);
						continue;
					case "clearContainer":
						this.parse_container(xmlElement);
						continue;
					case "progressBar":
						this.parse_progress_bar(xmlElement, dyndialog);
						continue;
					default:
						continue;
				}
			}
			dyndialog.formBody.Visible = true;
			dyndialog.formBody.AutoScroll = true;
			if(xelem.HasAttribute("resident") && xelem.GetAttribute("resident").Equals("false") && !xelem.GetAttribute("location").Equals("detach"))
				return;
			dyndialog.TopMost = true;
			dyndialog.ShowForm();
		}

		private void parse_container(XmlElement elem)
		{
			if(!this.bStowContainer)
				return;
			this.ghost.SendText("#clear " + elem.GetAttribute("id"));
		}

		private void parse_inventory(XmlElement elem)
		{
			if(!this.bStowContainer)
				return;
			this.ghost.SendText("#echo >" + elem.GetAttribute("id") + " " + elem.InnerText);
		}

		private void parse_clear_stream(XmlElement xelem)
		{
			foreach(SkinnedMDIChild skinnedMdiChild in this.forms)
			{
				foreach(Control control in (ArrangedElementCollection)skinnedMdiChild.formBody.Controls)
				{
					if(control.Name.Equals(xelem.GetAttribute("id")))
						control.Text = "";
				}
			}
			this.documents.Remove((object)xelem.GetAttribute("id"));
		}

		private void parse_set_stream(XmlElement xelem)
		{
			string attribute = xelem.GetAttribute("id");
			string innerText = xelem.InnerText;
			this.documents[(object)attribute] = (object)innerText;
			foreach(SkinnedMDIChild skinnedMdiChild in this.forms)
			{
				foreach(Control control in (ArrangedElementCollection)skinnedMdiChild.formBody.Controls)
				{
					if(control.Name.Equals(attribute))
						control.Text = innerText;
				}
			}
		}

		private void parse_stream_box(XmlElement cbx, SkinnedMDIChild dyndialog)
		{
			TextBox textBox = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new TextBox() : (TextBox)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
			if(textBox == null)
				return;
			textBox.Name = cbx.GetAttribute("id");
			textBox.Text = cbx.GetAttribute("value");
			textBox.Size = this.build_size(cbx, 200, 75);
			textBox.Location = this.set_location(cbx, (Control)textBox, dyndialog);
			textBox.Multiline = true;
			textBox.ScrollBars = ScrollBars.Vertical;
			dyndialog.formBody.Controls.Add((Control)textBox);
		}

		private void parse_numericupdown(XmlElement cbx, SkinnedMDIChild dyndialog)
		{
			NumericUpDown numericUpDown = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new NumericUpDown() : (NumericUpDown)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
			if(numericUpDown == null)
				return;
			if(cbx.HasAttribute("max"))
				numericUpDown.Maximum = (Decimal)int.Parse(cbx.GetAttribute("max"));
			if(cbx.HasAttribute("min"))
				numericUpDown.Minimum = (Decimal)int.Parse(cbx.GetAttribute("min"));
			numericUpDown.Name = cbx.GetAttribute("id");
			numericUpDown.Text = cbx.GetAttribute("value");
			numericUpDown.Value = (Decimal)int.Parse(cbx.GetAttribute("value"));
			numericUpDown.Size = this.build_size(cbx, 200, 75);
			numericUpDown.Location = this.set_location(cbx, (Control)numericUpDown, dyndialog);

			dyndialog.formBody.Controls.Add((Control)numericUpDown);
		}

		private void parse_edit_box(XmlElement cbx, SkinnedMDIChild dyndialog)
		{
			TextBox textBox = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new TextBox() : (TextBox)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
			if(textBox == null)
				return;
			textBox.Name = cbx.GetAttribute("id");
			textBox.Text = cbx.GetAttribute("value");
			textBox.Size = this.build_size(cbx, 200, 75);
			textBox.Location = this.set_location(cbx, (Control)textBox, dyndialog);
			if(cbx.HasAttribute("MaxChars"))
				textBox.MaxLength = int.Parse(cbx.GetAttribute("MaxChars"));
			textBox.Multiline = false;
			dyndialog.formBody.Controls.Add((Control)textBox);
		}

		private void parse_check_box(XmlElement cbx, SkinnedMDIChild dyndialog)
		{
			cbCheckBox cbCheckBox = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new cbCheckBox() : (cbCheckBox)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
			if(cbCheckBox == null)
				return;
			cbCheckBox.Name = cbx.GetAttribute("id");
			cbCheckBox.Text = cbx.GetAttribute("text");
			cbCheckBox.checked_value = cbx.GetAttribute("checked_value");
			cbCheckBox.unchecked_value = cbx.GetAttribute("unchecked_value");
			cbCheckBox.Checked = cbx.HasAttribute("checked");
			cbCheckBox.Size = this.build_size(cbx, 200, 20);
			cbCheckBox.Location = this.set_location(cbx, (Control)cbCheckBox, dyndialog);
			dyndialog.formBody.Controls.Add((Control)cbCheckBox);
		}

		private void parse_radio_button(XmlElement cbx, SkinnedMDIChild dyndialog)
		{
			cbRadio cbRadio = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new cbRadio() : (cbRadio)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
			if(cbRadio == null)
				return;
			cbRadio.Name = cbx.GetAttribute("id");
			cbRadio.Text = cbx.GetAttribute("text");
			cbRadio.command = cbx.GetAttribute("cmd");
			cbRadio.group = cbx.GetAttribute("group");
			if(cbx.GetAttribute("value").Contains("0"))
				cbRadio.Checked = false;
			else
				cbRadio.Checked = true;
			cbRadio.Size = this.build_size(cbx, 200, 20);
			cbRadio.Location = this.set_location(cbx, (Control)cbRadio, dyndialog);
			cbRadio.Click += new EventHandler(this.cbRadioSelect);
			dyndialog.formBody.Controls.Add((Control)cbRadio);
		}

		private void parse_progress_bar(XmlElement cbx, SkinnedMDIChild dyndialog)
		{
			ProgressBar progressBar = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new ProgressBar() : (ProgressBar)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
			if(progressBar == null)
				return;
			progressBar.Name = cbx.GetAttribute("id");
			progressBar.Text = cbx.GetAttribute("text");
			progressBar.Style = ProgressBarStyle.Continuous;
			int result = 0;
			int.TryParse(cbx.GetAttribute("value"), out result);
			progressBar.Value = result;
			progressBar.Size = this.build_size(cbx, 200, 20);
			progressBar.Location = this.set_location(cbx, (Control)progressBar, dyndialog);
			dyndialog.formBody.Controls.Add((Control)progressBar);
		}

		private void parse_close_button(XmlElement cbx, SkinnedMDIChild dyndialog)
		{
			CmdButton cmdButton = new CmdButton();
			cmdButton.Name = cbx.GetAttribute("id");
			cmdButton.Text = cbx.GetAttribute("value");
			cmdButton.Size = this.build_size(cbx, 55, 20);
			cmdButton.Size = new Size(cmdButton.Size.Width + 10, cmdButton.Size.Height);
			cmdButton.cmd_string = !cbx.HasAttribute("cmd") ? "" : cbx.GetAttribute("cmd");
			cmdButton.Location = this.set_location(cbx, (Control)cmdButton, dyndialog);
			cmdButton.Click += new EventHandler(this.cbClose);
			dyndialog.formBody.Controls.Add((Control)cmdButton);
			dyndialog.CloseCommand = (Button)cmdButton;
		}

		private void parse_drop_down(XmlElement cbx, SkinnedMDIChild dyndialog)
		{
			cbDropBox cbDropBox = new cbDropBox();
			cbDropBox.Name = cbx.GetAttribute("id");
			cbDropBox.Text = cbx.GetAttribute("value");
			cbDropBox.content_handler_data = new Hashtable();
			string[] strArray1 = cbx.GetAttribute("content_text").Split(',');
			string[] strArray2 = cbx.GetAttribute("content_value").Split(',');
			for(int index = 0;index < strArray1.Length;++index)
			{
				cbDropBox.content_handler_data.Add((object)strArray1[index], (object)strArray2[index]);
				cbDropBox.Items.Add((object)strArray1[index]);
			}
			if(cbx.HasAttribute("cmd"))
			{
				cbDropBox.cmd = cbx.GetAttribute("cmd");
				cbDropBox.SelectedIndexChanged += new EventHandler(this.cb_SelectedIndexChanged);
			}
			cbDropBox.Size = this.build_size(cbx, 55, 20);
			cbDropBox.Location = this.set_location(cbx, (Control)cbDropBox, dyndialog);
			dyndialog.formBody.Controls.Add((Control)cbDropBox);
		}

		private void parse_command_buttons(XmlElement cbx, SkinnedMDIChild dyndialog)
		{
			CmdButton cmdButton = new CmdButton();
			cmdButton.Name = cbx.GetAttribute("id");
			cmdButton.Text = cbx.GetAttribute("value");
			cmdButton.cmd_string = cbx.GetAttribute("cmd");
			cmdButton.Size = this.build_size(cbx, 50, 20);
			cmdButton.Size = new Size(cmdButton.Size.Width + 10, cmdButton.Size.Height);
			cmdButton.Location = this.set_location(cbx, (Control)cmdButton, dyndialog);
			cmdButton.Click += new EventHandler(this.cbCommand);
			dyndialog.formBody.Controls.Add((Control)cmdButton);
		}

		private void parse_labels(XmlElement cbx, SkinnedMDIChild dyndialog)
		{
			Label label = !dyndialog.formBody.Controls.ContainsKey(cbx.GetAttribute("id")) ? new Label() : (Label)dyndialog.formBody.Controls[cbx.GetAttribute("id")];
			label.Text = cbx.GetAttribute("value");
			label.Name = cbx.GetAttribute("id");
			label.Size = this.build_size(cbx, 200, 15);
            if (!cbx.HasAttribute("width"))
                    label.AutoSize = true;
			label.Location = this.set_location(cbx, (Control)label, dyndialog);
			if(dyndialog.formBody.Controls.Contains((Control)label))
				return;
			dyndialog.formBody.Controls.Add((Control)label);
		}

		public void cbCommand(object sender, EventArgs e)
		{
			CmdButton cmdButton = (CmdButton)sender;
			Panel panel = (Panel)cmdButton.Parent;
			string str1 = cmdButton.cmd_string;
			string str2 = "";
			if(cmdButton.cmd_string.Contains("%"))
			{
				foreach(Control control in (ArrangedElementCollection)panel.Controls)
				{
					if(control is cbRadio)
					{
						if(((RadioButton)control).Checked)
							str1 = str1.Replace("%" + ((cbRadio)control).group + "%", ((cbRadio)control).command + " ");
					}
					else if(control is cbCheckBox)
					{
						str1 = str1.Replace("%" + control.Name + "%", ((cbCheckBox)control).value + " ");
					}
					else if (control is cbDropBox)
					{
						cbDropBox cbDropBox = (cbDropBox)control;
						if (cbDropBox.SelectedIndex > -1)
							str2 = (string)cbDropBox.content_handler_data[cbDropBox.Items[cbDropBox.SelectedIndex]];
						if (str1.Contains("%province1%"))
							str1 = str1.Replace("%province1%", str2);
						if (str1.Contains("%bank1%"))
							str1 = str1.Replace("%bank1%", str2);
						else if (str1.Contains("%bank2%"))
							str1 = str1.Replace("%bank2%", str2);
						if (str1.Contains("%category%"))
						{
							str1 = str1.Replace("%category%", cbDropBox.Text.Remove(cbDropBox.Text.IndexOf(" ")) + ";");
							if (str1.Contains("%title%"))
								str1 = str1.Replace("%details%", ";%details%");
						}
						else
							str1 = str1.Replace("%" + cmdButton.Name + "%", str2);
					}
					else
						str1 = str1.Replace("%" + control.Name + "%", control.Text + " ");
				}
				if (cmdButton.Text.Equals("Clear"))
				{
					this.forms.Remove((object)(Form)((Control)sender).Parent);
					((Form)cmdButton.Parent).Close();
				}
			}
			this.ghost.SendText(str1.Replace(";", "\\;"));
		}

		public void cb_SelectedIndexChanged(object sender, EventArgs e)
		{
			cbDropBox cbDropBox = (cbDropBox)sender;
			string str = "";
			if(cbDropBox.cmd.Contains("%"))
			{
				string newValue = "";
				if(cbDropBox.SelectedIndex > -1)
					newValue = (string)cbDropBox.content_handler_data[cbDropBox.Items[cbDropBox.SelectedIndex]];
				str = cbDropBox.cmd.Replace("%" + cbDropBox.Name + "%", newValue);
			}
			this.ghost.SendText(str.Replace(";", "\\;"));
		}

		public void CloseCommand(Button cb)
		{
		}

		public void cbClose(object sender, EventArgs e)
		{
			CmdButton cmdButton = (CmdButton)sender;
			Panel panel = (Panel)cmdButton.Parent;
			SkinnedMDIChild skinnedMdiChild = (SkinnedMDIChild)cmdButton.FindForm();
			string str2 = "";
			if(cmdButton.cmd_string.Length > 2)
			{
				string str1 = cmdButton.cmd_string;
				if(cmdButton.cmd_string.Contains("%"))
				{
					foreach(Control control in (ArrangedElementCollection)panel.Controls)
					{
						if(control is cbRadio)
						{
							if(((RadioButton)control).Checked)
								str1 = str1.Replace("%" + ((cbRadio)control).group + "%", ((cbRadio)control).command + " ");
						}
						else if(control is cbDropBox)
						{
							cbDropBox cbDropBox = (cbDropBox)control;
							if (cbDropBox.SelectedIndex > -1)
								str2 = (string)cbDropBox.content_handler_data[cbDropBox.Items[cbDropBox.SelectedIndex]];
							if (str1.Contains("%province1%"))
								str1 = str1.Replace("%province1%", str2);
							if (str1.Contains("%bank1%"))
								str1 = str1.Replace("%bank1%", str2);
							else if (str1.Contains("%bank2%"))
								str1 = str1.Replace("%bank2%", str2);
							if (str1.Contains("%category%"))
							{
								str1 = str1.Replace("%category%", cbDropBox.Text.Remove(cbDropBox.Text.IndexOf(" ")) + ";");
								if (str1.Contains("%title%"))
									str1 = str1.Replace("%details%", ";%details%");
							}
							else
								str1 = str1.Replace("%" + cmdButton.Name + "%", str2);
						}
						else
							str1 = str1.Replace("%" + control.Name + "%", control.Text);
					}			
				}
					this.ghost.SendText(str1.Replace(";", "\\;"));
			}
			this.forms.Remove((object)skinnedMdiChild);
			skinnedMdiChild.Close();
		}

	public void cbRadioSelect(object sender, EventArgs e)
    {
      cbRadio cbRadio = (cbRadio) sender;
      SkinnedMDIChild skinnedMdiChild = (SkinnedMDIChild) cbRadio.FindForm();
      foreach (Control control in (ArrangedElementCollection) cbRadio.Parent.Controls)
      {
        if (control is cbRadio)
        {
          if (cbRadio.group.Equals(((cbRadio) control).group) && !cbRadio.Name.Equals(control.Name))
            ((RadioButton) control).Checked = false;
          else
            ((RadioButton) control).Checked = true;
        }
      }
    }

    private Size build_size(XmlElement cbx, int width, int height)
    {
      int width1 = width;
      int height1 = height;
      if (cbx.HasAttribute("width"))
        width1 = int.Parse(cbx.GetAttribute("width"));
      if (cbx.HasAttribute("height"))
        height1 = int.Parse(cbx.GetAttribute("height"));
      return new Size(width1, height1);
    }

    private Point set_location(XmlElement cbx, Control cont, SkinnedMDIChild p_form)
    {
      int result1 = 0;
      int result2 = 0;
      int.TryParse(cbx.GetAttribute("top"), out result2);
      int.TryParse(cbx.GetAttribute("left"), out result1);
      if (cbx.HasAttribute("align"))
      {
        if (cbx.GetAttribute("align").Equals("center"))
        {
          result2 = p_form.formBody.Height / 2 - cont.ClientSize.Height / 2 + result2;
          result1 = p_form.formBody.Width / 2 - cont.ClientSize.Width / 2 + result1;
        }
        else if (cbx.GetAttribute("align").Equals("s"))
        {
          cont.Anchor = AnchorStyles.Bottom;
          result1 = p_form.formBody.Width / 2 - cont.ClientSize.Width / 2 + result1;
        }
        else if (cbx.GetAttribute("align").Equals("se"))
          cont.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        else if (cbx.GetAttribute("align").Equals("sw"))
          cont.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        else if (cbx.GetAttribute("align").Equals("n"))
        {
          cont.Anchor = AnchorStyles.Top;
          result1 = p_form.formBody.Width / 2 - cont.ClientSize.Width / 2 + result1;
        }
        else if (cbx.GetAttribute("align").Equals("ne"))
          cont.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        else if (cbx.GetAttribute("align").Equals("nw"))
          cont.Anchor = AnchorStyles.Top | AnchorStyles.Left;
      }
      else if (cbx.HasAttribute("anchor_left"))
      {
        Control control = p_form.formBody.Controls[cbx.GetAttribute("anchor_left")];
        int num1 = result2;
        int num2 = result1;
        result1 = control.Left + control.Width + num2 + 5;
        result2 = num1;
        if (result2 == 0)
          result2 = control.Top;
      }
      else if (cbx.HasAttribute("anchor_right"))
      {
        Control control = p_form.formBody.Controls[cbx.GetAttribute("anchor_right")];
        int num1 = result2;
        int num2 = result1;
        result1 = control.Left - num2 - cont.Width - 5;
        result2 = num1;
        if (result2 == 0)
          result2 = control.Top;
      }
      if (cbx.HasAttribute("anchor_top"))
      {
        Control control = p_form.formBody.Controls[cbx.GetAttribute("anchor_top")];
        result2 = result2 + control.Bottom + 2;
        if (result2 == 0)
          result2 = control.Top;
      }
      if (result2 < 0)
        result2 = p_form.formBody.Height - cont.Height + result2;
      if (result1 < 0)
        result1 = p_form.formBody.Width - cont.Width + result1;
      if (result2 + cont.Height > p_form.formBody.Height)
      {
        int num = result2 + cont.Height - p_form.formBody.Height;
        p_form.ClientSize = new Size(p_form.ClientSize.Width, p_form.ClientSize.Height + num + 2);
      }
      if (result1 + cont.Width > p_form.formBody.Width)
      {
        int num = result1 + cont.Width - p_form.formBody.Width;
        p_form.ClientSize = new Size(p_form.ClientSize.Width + num + 2, p_form.ClientSize.Height);
      }
      return new Point(result1, result2);
    }
  }
}
