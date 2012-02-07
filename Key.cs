/*
 * @Author: Cory Travis
 * @Modified: 1/24/2012
 */

using System;
using Extensions;
using Keys.InterOp;

namespace Keys {
    
    /// <summary>
    ///     This class allows for subscription to physical key presses and releases through events.
    /// </summary>
    public sealed class Key {

        private readonly string str;

        private volatile bool down;

        /// <summary>
        ///     Indicates whether this key is currently held. If a key is used in an accelerator keystroke
        ///     this will be false.
        /// </summary>
        public bool Down { 
            get {
                return down;
            }
        }
               
        /// <summary>
        ///     The virtual key code represents a single physical key on the keyboard.
        /// </summary>
        public readonly VirtualKeys VirtualKey;

        /// <summary>
        ///     Will be fired when the key is pressed, unless it was used in an accelerator keystroke.
        /// </summary>
        public event EventHandler Pressed;

        /// <summary>
        ///     Will be fired when the key is released, unless it was used in an accelerator keystroke.
        /// </summary>
        public event EventHandler Released;

        internal Key(VirtualKeys virtualKey) {
            str = virtualKey.ToString();
            VirtualKey = virtualKey;            
        }

        public override string ToString() {
            return str;
        }

        /// <summary>
        ///     Called by the container object when the correspoding physical key is pressed.
        /// </summary>
        internal void OnPressed() {
            down = true;            
            Pressed.Raise(this);
        }
                
        /// <summary>
        ///     Called by the container object when the correspoding physical key is released.
        /// </summary>
        internal void OnReleased() {
            down = false;
            Released.Raise(this);
        }
    }
}
