/*
 * @Author: Cory Travis
 * @Modified: 1/24/2012
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using Keys.InterOp;

namespace Keys {
   
    /// <summary>
    ///     Manages key presses and events attached to them including accelerator keystrokes. 
    /// </summary>
    public sealed class Keys : IDisposable {
                    
        private sealed class AcceleratorCollection : KeyedCollection<ushort, Accelerator> {

            protected override ushort GetKeyForItem(Accelerator item) {
                return item.Command;
            }
        }

        private static readonly VirtualKeys[] virtualKeys = (VirtualKeys[])Enum.GetValues(typeof(VirtualKeys));

        private volatile bool disposed;
        
        private Key[] keys;     

        private AcceleratorCollection accelerators = new AcceleratorCollection();

        private ReaderWriterLockSlim acceleratorsLock = new ReaderWriterLockSlim();

        private volatile bool recording;

        private StringBuilder buffer = new StringBuilder();

        private object bufferLock = new object();

        private IntPtr hWnd;

        private Delegates.GetMsgProc getMsgProc;

        private Delegates.CallWndProc callWndProc;

        private uint dwThreadId;

        private IntPtr getMsgHHook;

        private IntPtr callWndHHook;
                
        private ACCEL[] accels;
               
        private IntPtr hAccel;

        public bool Disposed {
            get {
                return disposed;
            }
        }

        /// <summary>
        ///     If true, character codes corresponding to keystrokes will be recorded in a buffer.
        /// </summary>
        public bool Recording { 
            get {
                return recording;
            }
            set {
                recording = value;
            }
        }
        
        /// <summary>
        ///     Retrieve a representation of a single phyiscal key to which press and release events can be mapped.
        /// </summary>
        /// <param name="virtualKey">
        ///     The virtual key code of the physical keys representation to retrieve.
        /// </param>
        public Key this[VirtualKeys virtualKey] {
            get {
                return keys[(int)virtualKey];
            }
        }

        /// <summary>
        ///     Create a new instance of Keys. 
        /// </summary>
        /// <param name="hWnd">
        ///     A handle to the window whose keystrokes will be monitored and subscribed to through events.
        /// </param>
        /// <param name="record">
        ///     If true, keystrokes will be recorded in a buffer for later retrieval. Defaults to false.
        /// </param>
        public Keys(IntPtr hWnd, bool record = false) {       
          
            if(hWnd != IntPtr.Zero) {

                keys = new Key[virtualKeys.Max(virtualKey => (int)virtualKey) + 1];
                foreach(var virtualKey in virtualKeys) {
                    keys[(int)virtualKey] = new Key(virtualKey);
                }

                this.hWnd = hWnd;            

                recording = record;

                dwThreadId = Functions.GetWindowThreadProcessId(hWnd, IntPtr.Zero);                
                getMsgProc = GetMessageProcedure;
                getMsgHHook = Functions.SetWindowsHookEx(WindowsHooks.GETMESSAGE, getMsgProc, IntPtr.Zero, dwThreadId);                    
                callWndProc = CallWindowProcedure;     
                callWndHHook = Functions.SetWindowsHookEx(WindowsHooks.CALLWNDPROC, callWndProc, IntPtr.Zero, dwThreadId);

            } else {
                throw new ArgumentNullException("hWnd");
            }
        }
        
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        ///     Gets the size of the recorded buffered keystrokes.
        /// </summary>
        public int GetBufferLength() {
            lock(bufferLock) {
                return buffer.Length;
            }
        }

        /// <summary>
        ///     Clear the character buffer without taking its contents.
        /// </summary>
        public void ClearBuffer() {
            lock(bufferLock) {
                buffer.Clear();
            }
        }
                
        /// <summary>
        ///     Gets the character buffer as a string and clears the buffer.
        /// </summary>
        /// <returns>
        ///     A string consisting of the former contents of the character buffer.
        /// </returns>
        public string TakeBuffer() {
            string result;
            lock(bufferLock) {
                result = buffer.ToString();
                buffer.Clear();        
            }
            return result;
        }

        /// <summary>
        ///     Take the buffer, clearing its contents.
        /// </summary>
        /// <param name="stringBuilder">
        ///     A StringBuilder to which the contents of the buffer will be copied.
        /// </param>
        /// <returns>
        ///     An integer representing the number of characters taken.
        /// </returns>
        public int TakeBuffer(StringBuilder stringBuilder) {
            int appended;
            lock(bufferLock) {
                appended = buffer.Length;
                try {
                    stringBuilder.Append(buffer.ToString());
                } catch(ArgumentOutOfRangeException) {
                    ThrowOverflowException();
                }
                buffer.Clear();
            }
            return appended;
        }        
        
        /// <summary>
        ///     Insert the buffer into the given container at the given index.
        /// </summary>        
        /// <param name="stringBuilder">
        ///     A StringBuilder to which the contents of the buffer will be copied.
        /// </param>
        /// <param name="index">
        ///     The index at which insertion will begin.
        /// </param>
        /// <returns>
        ///     An integer representing the number of characters inserted.
        /// </returns>
        public int TakeBuffer(StringBuilder stringBuilder, int index) {
            int inserted;
            lock(bufferLock) {
                inserted = buffer.Length; 
                try {
                    stringBuilder.Insert(index, buffer.ToString());
                } catch(ArgumentOutOfRangeException) {
                    ThrowOverflowException();
                }
                buffer.Clear();
            }
            return inserted;
        }

        /// <summary>
        ///     Register a single accelerator. Its events will be fired when the associated keystroke is detected.
        /// </summary>
        /// <param name="accelerator">
        ///     The accelerator to be registered.
        /// </param>
        /// <returns>
        ///     A boolean value indicating whether or not the registration was successful.
        /// </returns>
        public bool RegisterAccelerator(Accelerator accelerator) {            
            bool result = false;
            acceleratorsLock.EnterUpgradeableReadLock();  
            if(!accelerators.Contains(accelerator.Command)) {
                acceleratorsLock.EnterWriteLock();
                accelerators.Add(accelerator);
                BuildAcceleratorTable();
                acceleratorsLock.ExitWriteLock();
                result = true;
            }
            acceleratorsLock.ExitUpgradeableReadLock();
            return result;
        }

        /// <summary>
        ///     Register several accelerators. Their events will be fired when the associated keystrokes are detected.
        /// </summary>
        /// <param name="accelerators">
        ///     An enumeration of accelerators to register.
        /// </param>
        /// <returns>
        ///     An integer representing the number of accelerators that were successfully registered.
        /// </returns>
        public int RegisterAccelerators(IEnumerable<Accelerator> accelerators) {            
            int result = 0;
            acceleratorsLock.EnterWriteLock();
            foreach(var accelerator in accelerators) {
                if(!this.accelerators.Contains(accelerator.Command)) {
                    this.accelerators.Add(accelerator);
                    result++;
                }
            }
            if(result > 0) {
                BuildAcceleratorTable();
            }
            acceleratorsLock.ExitWriteLock();
            return result;
        }

        /// <summary>
        ///     Unregister a single accelerator so that its events will not be fired when the associated keystroke is
        ///     detected.
        /// </summary>
        /// <param name="accelerator">
        ///     The accelerator to unregister.
        /// </param>
        /// <returns>
        ///     A boolean indicating whether or not the unregistration was successful.
        /// </returns>
        public bool UnregisterAccelerator(Accelerator accelerator) {
            acceleratorsLock.EnterWriteLock();
            bool result = accelerators.Remove(accelerator);
            if(result) {
                BuildAcceleratorTable();
            }
            acceleratorsLock.ExitWriteLock();
            return result;
        }

        /// <summary>
        ///     Unregister several accelerators so that their events will not be fired when the associated keystrokes 
        ///     are detected.
        /// </summary>
        /// <param name="accelerators">
        ///     An enumeration of accelerators to unregister.
        /// </param>
        /// <returns>
        ///     An integer representing the number of accelerators that were successfully unregistered.
        /// </returns>
        public int UnregisterAccelerators(IEnumerable<Accelerator> accelerators) {            
            int result = 0;
            acceleratorsLock.EnterWriteLock();
            foreach(var accelerator in accelerators) {
                if(this.accelerators.Remove(accelerator)) {
                    result++;
                }
            }
            if(result > 0) {
                BuildAcceleratorTable();
            }
            acceleratorsLock.ExitWriteLock();
            return result;
        }

        /// <summary>
        ///     Clear the accelerator table. 
        /// </summary>
        public void ClearAccelerators() {
            acceleratorsLock.EnterWriteLock();
            accelerators.Clear();
            BuildAcceleratorTable();
            acceleratorsLock.ExitWriteLock();
        }

        private void Dispose(bool disposing) {

            if(!disposed) {

                if(hAccel != IntPtr.Zero) {
                    Functions.DestroyAcceleratorTable(hAccel);
                    hAccel = IntPtr.Zero;
                }

                if(getMsgHHook != IntPtr.Zero) {
                    Functions.UnhookWindowsHookEx(getMsgHHook);
                    getMsgHHook = IntPtr.Zero;
                }
                                
                if(callWndHHook != IntPtr.Zero) {
                    Functions.UnhookWindowsHookEx(callWndHHook);
                    callWndHHook = IntPtr.Zero;
                }               

                if(disposing) {
                    keys = null;       
                    accels = null;
                    accelerators = null;
                    acceleratorsLock.Dispose();
                    acceleratorsLock = null;
                    recording = default(bool);
                    buffer = null;                             
                    bufferLock = null;
                    hWnd = IntPtr.Zero;
                    getMsgProc = null;
                    callWndProc = null;
                    dwThreadId = default(uint);
                    disposed = true;
                }
            }
        }

        private void ThrowOverflowException() {
            throw new OverflowException("The given StringBuilder did not have space for the buffers contents.");
        }

        private IntPtr GetMessageProcedure(HookCodes nCode, IntPtr wParam, ref MSG lParam) {
            if(nCode == HookCodes.ACTION && (PeekMessageOptions)wParam == PeekMessageOptions.REMOVE) {
                if(lParam.message == WindowsMessages.KEYDOWN || lParam.message == WindowsMessages.SYSKEYDOWN) {
                    KeyDown(ref lParam);
                } else if(lParam.message == WindowsMessages.KEYUP || lParam.message == WindowsMessages.SYSKEYUP) {
                    KeyUp(ref lParam);    
                } else if(lParam.message == WindowsMessages.CHAR) {
                    CharReceived(ref lParam);
                }
            }
            return Functions.CallNextHookEx(getMsgHHook, nCode, wParam, ref lParam);          
        }

        private void KeyDown(ref MSG msg) {
            if(hAccel == IntPtr.Zero || Functions.TranslateAccelerator(hWnd, hAccel, ref msg) == 0) {
                if(recording) {
                    Functions.TranslateMessage(ref msg);
                }
                keys[(int)msg.wParam].OnPressed();
            } 
        }

        private void KeyUp(ref MSG msg) {
            Key key = keys[(int)msg.wParam];
            if(key.Down) /* if the key was used in an accelerator keystroke, the key down was never fired */ {
                key.OnReleased();
            }
        }

        private void CharReceived(ref MSG msg) {
            char ch = (char)msg.wParam;
            if(ch >= ' ' && ch <= '~') {
                lock(bufferLock) {
                    try {
                        buffer.Append(ch);
                    } catch(ArgumentOutOfRangeException) {
                        throw new OverflowException("The character buffer has overflown.");
                    }
                }
            }   
        }
        
        private IntPtr CallWindowProcedure(HookCodes nCode, IntPtr wParam, ref CWPSTRUCT lParam) {
            if(lParam.message == WindowsMessages.COMMAND || lParam.message == WindowsMessages.SYSCOMMAND) {                                                
                uint parameters = (uint)lParam.wParam;
                if((MessageSources)Macros.HIWORD(parameters) == MessageSources.Accelerator) {
                    ushort command = Macros.LOWORD(parameters);
                    acceleratorsLock.EnterReadLock();                    
                    Accelerator accelerator = accelerators.Contains(command) ? accelerators[command] : null;
                    acceleratorsLock.ExitReadLock();                    
                    if(accelerator != null) /* in case the accelerator was deleted in another thread */ {
                        accelerator.Accelerate();
                    }
                }
            }
            return Functions.CallNextHookEx(callWndHHook, nCode, wParam, ref lParam);
        }

        private void BuildAcceleratorTable() {

            if(hAccel != IntPtr.Zero) {
                Functions.DestroyAcceleratorTable(hAccel);                
                hAccel = IntPtr.Zero;
            }

            if(accelerators.Count > 0) {
                accels = new ACCEL[accelerators.Count];
                int i = 0;
                foreach(var accelerator in accelerators) {
                    accels[i++] = accelerator.Accel;
                }
                hAccel = Functions.CreateAcceleratorTable(accels, accels.Length);
            } else {                
                accels = null;
            }
        }

        ~Keys() {
            Dispose(false);
        }
    }
}
