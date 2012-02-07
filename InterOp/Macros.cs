/*
 * @Author: Cory Travis
 * @Modified: 1/24/2012
 */

namespace Keys.InterOp {
    
    /// <summary>
    ///  Contains (effective) reproductions of useful macros from native headers.
    /// </summary>
    internal static class Macros {
        
        internal static ushort MAKEWORD(byte lo, byte hi) {
            return (ushort)(lo + (hi << 8));
        }

        internal static uint MAKEDWORD(ushort lo, ushort hi) {
            return (uint)(lo + (hi << 16));
        }

        internal static ushort LOWORD(uint dw) {
            return (ushort)(dw & 0xFFFF);
        }

        internal static ushort HIWORD(uint dw) {
            return (ushort)((dw >> 16) & 0xFFFF);
        }
        
        internal static byte LOBYTE(ushort w) {           
            return (byte)(w & 0xFF);
        }

        internal static byte HIBYTE(ushort w) {
            return (byte)((w >> 8) & 0xFF);
        }
    }
}
