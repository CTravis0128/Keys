/*
 * @Author: Cory Travis
 * @Modified: 1/24/2012
 */
using System;
using System.Runtime.InteropServices;

namespace Keys.InterOp {
    
    /// <summary>
    ///     Contains functions imported from native binaries. 
    /// </summary>
    internal static class Functions {

        [DllImport("user32.dll")] 
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

        [DllImport("user32.dll")]
        internal static extern IntPtr CreateAcceleratorTable(ACCEL[] lpaccl, int cEntries);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetWindowsHookEx(WindowsHooks hook, Delegates.GetMsgProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetWindowsHookEx(WindowsHooks hook, Delegates.CallWndProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        internal static extern int TranslateAccelerator(IntPtr hWnd, IntPtr hAccTable, ref MSG lpMsg);
               
        [DllImport("user32.dll")] 
        internal static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")] 
        internal static extern IntPtr CallNextHookEx(IntPtr hhk, HookCodes nCode, IntPtr wParam, ref MSG lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr CallNextHookEx(IntPtr hhk, HookCodes nCode, IntPtr wParam, ref CWPSTRUCT lParam);

        [DllImport("user32.dll")] 
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        internal static extern uint DestroyAcceleratorTable(IntPtr hAccel);
    }
}
