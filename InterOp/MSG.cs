/*
 * @Author: Cory Travis
 * @Modified: 1/24/2012
 */

using System;
using System.Runtime.InteropServices;

namespace Keys.InterOp {

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSG {

        internal IntPtr hWnd;

        internal WindowsMessages message;

        internal IntPtr wParam;

        internal IntPtr lParam;

        internal uint time;

        internal IntPtr pt;
    }
}
