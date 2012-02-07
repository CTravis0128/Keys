/*
 * @Author: Cory Travis
 * @Modified: 1/24/2012
 */

using System;

namespace Keys.InterOp {

    /// <summary>
    ///     Holds delegates representing signatures for  function pointers.
    /// </summary>
    internal static class Delegates {

        internal delegate IntPtr GetMsgProc(HookCodes nCode, IntPtr wParam, ref MSG lParam);

        internal delegate IntPtr CallWndProc(HookCodes nCode, IntPtr wParam, ref CWPSTRUCT lParam);

    }
}
