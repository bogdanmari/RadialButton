using System;
using System.Windows.Forms;

namespace RadialButton
{
    internal sealed class RevitWindowOwner : IWin32Window
    {
        public IntPtr Handle { get; private set; }
        public RevitWindowOwner(IntPtr handle) { Handle = handle; }
    }
}
