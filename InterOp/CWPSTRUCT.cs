/*
 * @Author: Cory Travis
 * @Modified: 1/24/2012
 */

using System;
using System.Runtime.InteropServices;

namespace Keys.InterOp {
        
    [StructLayout(LayoutKind.Sequential)]
    internal struct CWPSTRUCT {

        internal IntPtr lParam;
    
        internal IntPtr wParam;
    
        internal WindowsMessages message;   

        internal IntPtr hwnd;
    }
}
