/*
 * @Author: Cory Travis
 * @Modified: 1/24/2012
 */

using System.Runtime.InteropServices;

namespace Keys.InterOp {
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct ACCEL {

        internal byte fVirt;

        internal ushort key;

        internal ushort cmd;
    }
}
