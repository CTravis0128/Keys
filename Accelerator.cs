/*
 * @Author: Cory Travis
 * @Modified: 1/24/2012
 */

using System;
using System.Collections.Generic;
using System.Text;
using Extensions;
using Keys.InterOp;

namespace Keys {
    
    /// <summary>
    ///     Represents a keystroke such as Ctrl + F or Ctrl + Alt + Space.
    /// </summary>
    public sealed class Accelerator {
                
        private const byte VIRTKEY = 0x01;

        private const string SEPARATOR = " + ";

        private static readonly Dictionary<AcceleratorKeys, string> acceleratorKeys = new Dictionary<AcceleratorKeys, string>();

        private static readonly Dictionary<VirtualKeys, string> virtualKeys = new Dictionary<VirtualKeys, string>();

        private static readonly StringBuilder stringBuilder = new StringBuilder();

        private static readonly object builderLock = new object();

        private readonly string str;

        internal readonly ACCEL Accel;

        internal readonly ushort Command;

        public readonly AcceleratorKeys AcceleratorKey;

        public readonly VirtualKeys VirtualKey;

        public event EventHandler Handler;

        static Accelerator() {

            foreach(var acceleratorKey in Enum.GetValues(typeof(AcceleratorKeys))) {
                acceleratorKeys.Add((AcceleratorKeys)acceleratorKey, acceleratorKey.ToString());
            }

            foreach(var virtualKey in Enum.GetValues(typeof(VirtualKeys))) {
                virtualKeys.Add((VirtualKeys)virtualKey, virtualKey.ToString());
            }
        }

        /// <summary>
        ///     Create a new accelerator from the given accelerator key and virtual key combination.
        /// </summary>
        /// <param name="acceleratorKey">
        ///     The accelerator key (Ctrl, Alt, or Shift). Can be combined with | to form more complicated keystrokes
        ///     such as Ctrl + Alt + Space.
        /// </param>
        /// <param name="virtualKey">
        ///     The virtual key code (may not be Ctrl, Alt, or Shift).
        /// </param>
        public Accelerator(AcceleratorKeys acceleratorKey, VirtualKeys virtualKey) {
            
            AcceleratorKey = acceleratorKey;
            VirtualKey = virtualKey;
            Command = Macros.MAKEWORD((byte)AcceleratorKey, (byte)VirtualKey); 

            lock(builderLock) {

                //create a string representation of this accelerators keystroke such as "CONTROL + SPACE"
                foreach(var key in acceleratorKeys.Keys) {
                    if(AcceleratorKey.Any(key)) {
                        stringBuilder.Append(acceleratorKeys[key])
                                     .Append(SEPARATOR);
                    }
                }
                stringBuilder.Append(virtualKeys[VirtualKey]);
                str = stringBuilder.ToString();
                stringBuilder.Clear();
            }

            Accel = new ACCEL() { 
                fVirt = (byte)(AcceleratorKey | (AcceleratorKeys)VIRTKEY),
                key = (ushort)VirtualKey,
                cmd = Command            
            };          
        } 

        public override string ToString() {
            return str;
        }              

        /// <summary>
        ///     Called by a hook procedure when the relevant keystroke is detected.
        /// </summary>
        internal void Accelerate() {
            Handler.Raise(this);
        }
    }
}
