using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicWindows
{
    public class CbStreamBox : Panel
    {
        private readonly Plugin _plugin;

        public CbStreamBox(Plugin plugin)
        {
            _plugin = plugin;
        }

        public CbStreamBox()
        {
            // Set default properties for the stream box
            this.BackColor = _plugin.formback;
            this.AutoScroll = true;
        }

        public void AddContent(string content)
        {
            // Add content to the stream box
            Label label = new Label
            {
                Text = content,
                AutoSize = true,
                ForeColor = _plugin.formfore,
                Name = content
            };
            this.Controls.Add(label);
        }
    }

}
