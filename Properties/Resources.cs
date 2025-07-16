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
        private static ResourceManager resourceMan;
        private static CultureInfo resourceCulture;

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals((object)Resources.resourceMan, (object)null))
                    Resources.resourceMan = new ResourceManager("DynamicWindows.Properties.Resources", typeof(Resources).Assembly);
                return Resources.resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture
        {
            get
            {
                return Resources.resourceCulture;
            }
            set
            {
                Resources.resourceCulture = value;
            }
        }

        internal static Bitmap skin_bottom
        {
            get
            {
                return (Bitmap)Resources.ResourceManager.GetObject("skin_bottom", Resources.resourceCulture);
            }
        }

        internal static Bitmap skin_bottomleft
        {
            get
            {
                return (Bitmap)Resources.ResourceManager.GetObject("skin_bottomleft", Resources.resourceCulture);
            }
        }

        internal static Bitmap skin_bottomright
        {
            get
            {
                return (Bitmap)Resources.ResourceManager.GetObject("skin_bottomright", Resources.resourceCulture);
            }
        }

        internal static Bitmap skin_left
        {
            get
            {
                return (Bitmap)Resources.ResourceManager.GetObject("skin_left", Resources.resourceCulture);
            }
        }

        internal static Bitmap skin_right
        {
            get
            {
                return (Bitmap)Resources.ResourceManager.GetObject("skin_right", Resources.resourceCulture);
            }
        }

        internal static Bitmap skin_top
        {
            get
            {
                return (Bitmap)Resources.ResourceManager.GetObject("skin_top", Resources.resourceCulture);
            }
        }

        internal static Bitmap skin_topleft
        {
            get
            {
                return (Bitmap)Resources.ResourceManager.GetObject("skin_topleft", Resources.resourceCulture);
            }
        }

        internal static Bitmap skin_topright
        {
            get
            {
                return (Bitmap)Resources.ResourceManager.GetObject("skin_topright", Resources.resourceCulture);
            }
        }

        internal static Icon Taleweaver
        {
            get
            {
                return (Icon)Resources.ResourceManager.GetObject("Taleweaver", Resources.resourceCulture);
            }
        }

        internal static Bitmap body_image
        {
            get
            {
                return (Bitmap)Resources.ResourceManager.GetObject("body_image", Resources.resourceCulture);
            }
        }

        internal static Bitmap skra
        {
            get
            {
                return (Bitmap)Resources.ResourceManager.GetObject("skra", Resources.resourceCulture);
            }
        }

        internal Resources()
        {
        }
    }
}