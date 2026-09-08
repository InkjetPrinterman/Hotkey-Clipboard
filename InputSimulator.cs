using static HotkeyClipboard.NativeMethods;

namespace HotkeyClipboard
{
    public static class InputSimulator
    {
        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_C = 0x43;
        private const ushort VK_V = 0x56;

        // Every virtual-key code that represents a modifier, including the
        // left/right specific variants. Used by the failsafe below.
        private static readonly ushort[] AllModifierVks =
        {
            0x11, 0xA2, 0xA3, // VK_CONTROL, VK_LCONTROL, VK_RCONTROL
            0x12, 0xA4, 0xA5, // VK_MENU (Alt), VK_LMENU, VK_RMENU
            0x10, 0xA0, 0xA1, // VK_SHIFT, VK_LSHIFT, VK_RSHIFT
            0x5B, 0x5C        // VK_LWIN, VK_RWIN
        };

        public static void SendCopy() => SendCombo(VK_CONTROL, VK_C);
        public static void SendPaste() => SendCombo(VK_CONTROL, VK_V);

        private static void SendCombo(ushort modifierVk, ushort keyVk)
        {
            var inputs = new[]
            {
                MakeKeyInput(modifierVk, keyUp: false),
                MakeKeyInput(keyVk, keyUp: false),
                MakeKeyInput(keyVk, keyUp: true),
                MakeKeyInput(modifierVk, keyUp: true),
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());

            // FAILSAFE: force every modifier back to "up" immediately after.
            // If the sequence above was ever only partially delivered (e.g.
            // blocked mid-way by UIPI when the focused window runs elevated,
            // or interrupted by an exception), this guarantees we never
            // leave a modifier logically stuck "down" at the OS level —
            // which is what breaks normal typing until reboot otherwise.
            ReleaseAllModifiers();
        }

        /// <summary>
        /// Forces every modifier key to the "up" state. Safe to call at any
        /// time, including when nothing is actually held — releasing a key
        /// that isn't down has no effect. This is the core failsafe: it is
        /// called after every simulated copy/paste, when a recording window
        /// closes for any reason, on unexpected errors, and once at startup
        /// in case a previous run left the keyboard in a bad state.
        /// </summary>
        public static void ReleaseAllModifiers()
        {
            try
            {
                var inputs = new INPUT[AllModifierVks.Length];
                for (int i = 0; i < AllModifierVks.Length; i++)
                    inputs[i] = MakeKeyInput(AllModifierVks[i], keyUp: true);
                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            }
            catch
            {
                // The failsafe itself must never throw.
            }
        }

        private static INPUT MakeKeyInput(ushort vk, bool keyUp)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vk,
                        wScan = 0,
                        dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }
    }
}
