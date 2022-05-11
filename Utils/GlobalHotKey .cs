using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TTS_Chan.Utils
{
    public class GlobalHotKey : IDisposable
    {
        public static int RegisterHotKey(string aKeyGestureString, Action aAction)
        {
            var c = new KeyGestureConverter();
            var aKeyGesture = (KeyGesture)c.ConvertFrom(aKeyGestureString);
            return RegisterHotKey(aKeyGesture!.Modifiers, aKeyGesture.Key, aAction);
        }

        public static int RegisterHotKey(ModifierKeys aModifier, Key aKey, Action aAction)
        {
            if (aModifier == ModifierKeys.None)
            {
                throw new ArgumentException("Modifier must not be ModifierKeys.None");
            }
            if (aAction is null)
            {
                throw new ArgumentNullException(nameof(aAction));
            }

            var aVirtualKeyCode = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(aKey);
            _currentId += 1;
            var aRegistered = RegisterHotKey(Window.Handle, _currentId,
                                        (uint)aModifier | MOD_NOREPEAT,
                                        (uint)aVirtualKeyCode);

            if (aRegistered)
            {
                RegisteredHotKeys.Add(new HotKeyWithAction(aModifier, aKey, aAction, _currentId));
            }
            else
            {
                return -1;
            }
            return _currentId;
        }

        public static bool UnregisterHotKey(int hotkeyId)
        {
            return UnregisterHotKey(Window.Handle, hotkeyId);
        }

        public void Dispose()
        {
            // unregister all the registered hot keys.
            for (var i = _currentId; i > 0; i--)
            {
                UnregisterHotKey(Window.Handle, i);
            }

            // dispose the inner native window.
            Window.Dispose();
        }

        static GlobalHotKey()
        {
            Window.KeyPressed += (s, e) =>
            {
                RegisteredHotKeys.ForEach(x =>
                {
                    if (e.Modifier == x.Modifier && e.Key == x.Key)
                    {
                        x.Action();
                    }
                });
            };
        }

        private static readonly InvisibleWindowForMessages Window = new();
        private static int _currentId;
        private static readonly uint MOD_NOREPEAT = 0x4000;
        private static readonly List<HotKeyWithAction> RegisteredHotKeys = new();

        private class HotKeyWithAction
        {

            public HotKeyWithAction(ModifierKeys modifier, Key key, Action action, int id)
            {
                Modifier = modifier;
                Key = key;
                Action = action;
                Id = id;
            }

            public ModifierKeys Modifier { get; }
            public Key Key { get; }
            public Action Action { get; }
            public int Id { get; }
        }

        // Registers a hot key with Windows.
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        // Unregisters the hot key with Windows.
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private sealed class InvisibleWindowForMessages : System.Windows.Forms.NativeWindow, IDisposable
        {
            public InvisibleWindowForMessages()
            {
                CreateHandle(new System.Windows.Forms.CreateParams());
            }

            private static readonly int WM_HOTKEY = 0x0312;
            protected override void WndProc(ref System.Windows.Forms.Message m)
            {
                base.WndProc(ref m);

                if (m.Msg != WM_HOTKEY) return;
                var aWpfKey = KeyInterop.KeyFromVirtualKey(((int)m.LParam >> 16) & 0xFFFF);
                var modifier = (ModifierKeys)((int)m.LParam & 0xFFFF);
                KeyPressed?.Invoke(this, new HotKeyPressedEventArgs(modifier, aWpfKey));
            }

            public class HotKeyPressedEventArgs : EventArgs
            {
                internal HotKeyPressedEventArgs(ModifierKeys modifier, Key key)
                {
                    Modifier = modifier;
                    Key = key;
                }

                public ModifierKeys Modifier { get; }

                public Key Key { get; }
            }


            public event EventHandler<HotKeyPressedEventArgs> KeyPressed;

            #region IDisposable Members

            public void Dispose()
            {
                DestroyHandle();
            }

            #endregion
        }
    }
}
