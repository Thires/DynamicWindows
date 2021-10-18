using System.Collections;
using System.Windows.Forms;

namespace DynamicWindows
{
  internal class cbDropBox : ComboBox
  {
    public string cmd;
    public Hashtable content_handler_data;

		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// cbDropBox
			// 
			this.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.FormattingEnabled = true;
			this.ResumeLayout(false);

		}
	}
}