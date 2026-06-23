using System.Collections;
using System.Windows.Forms;

namespace DynamicWindows
{
    internal class CbDropBox : ComboBox
    {
        public string cmd = string.Empty;
        public Hashtable content_handler_data = new();

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
