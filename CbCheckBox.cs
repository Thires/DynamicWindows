using System.Windows.Forms;

namespace DynamicWindows
{
  internal class CbCheckBox : CheckBox
  {
    public string cmd;
    public string unchecked_value;
    public string checked_value;

    public string Value
    {
      get
      {
        if (this.Checked)
          return this.checked_value;
        else
          return this.unchecked_value;
      }
    }

    public CbCheckBox()
    {
      this.cmd = string.Empty;
      this.unchecked_value = "0";
      this.checked_value = "1";
    }
  }
}
