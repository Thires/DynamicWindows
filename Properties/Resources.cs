using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace DynamicWindows.Properties
{
    [DebuggerNonUserCode]
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
    [CompilerGenerated]
    internal class Resources
    {
        private static ResourceManager? resourceMan;
        private static CultureInfo? resourceCulture;

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
        {
            get
            {
                resourceMan ??= new ResourceManager("DynamicWindows.Properties.Resources", typeof(Resources).Assembly);
                return resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo? Culture
        {
            get { return resourceCulture; }
            set { resourceCulture = value; }
        }

        internal static Icon Taleweaver
            => (Icon)ResourceManager.GetObject("Taleweaver", resourceCulture)!;

        internal static Bitmap body_image_ext
            => (Bitmap)ResourceManager.GetObject("body_image_ext", resourceCulture)!;

        internal static Bitmap body_image_int
            => (Bitmap)ResourceManager.GetObject("body_image_int", resourceCulture)!;

        internal Resources()
        {
        }
    }
}
