using System.Windows.Forms;

namespace DynamicWindows
{
  internal class cbCheckBox : CheckBox
  {
    public string cmd;
    public string unchecked_value;
    public string checked_value;

    public string value
    {
      get
      {
        if (this.Checked)
          return this.checked_value;
        else
          return this.unchecked_value;
      }
    }

    public cbCheckBox()
    {
      this.cmd = string.Empty;
      this.unchecked_value = "0";
      this.checked_value = "1";
    }
  }
}
