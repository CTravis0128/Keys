/* The following is an example usage of the Keys class */

using Keys;
using Keys.InterOp;

namespace Example {
    
    class KeysExample : IDisposable {   

        Keys.Keys keys;
    
        public KeysExample(IntPtr windowHandle) {

            /* windowHandle should be a handle to a compatible window. 
               Xna windows work; Windows Forms have some issues. */

            keys = new Keys.Keys(windowHandle);

            keys[VirtualKeys.SPACE].Pressed += (sender, args) => {
                //Do stuff when the spacebar is pressed.
            };

            keys[VirtualKeys.RETURN].Released += (sender, args) => {
                //Do stuff when the return key is released;
            };
        }      
    
        public void Dispose() {

            //Keys uses unmanaged resources that must be disposed.

            keys.Dispose();
        }
    }
}

