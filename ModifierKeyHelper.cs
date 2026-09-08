namespace HotkeyClipboard
{
    public enum ModifierKey { Ctrl, Alt, Shift, Win }

    /// <summary>Maps raw virtual-key codes (as seen by the low-level keyboard hook) to a ModifierKey.</summary>
    public static class ModifierKeyHelper
    {
        public static ModifierKey? FromVirtualKey(int vk)
        {
            return vk switch
            {
                0x11 or 0xA2 or 0xA3 => ModifierKey.Ctrl,   // VK_CONTROL, LCONTROL, RCONTROL
                0x12 or 0xA4 or 0xA5 => ModifierKey.Alt,    // VK_MENU, LMENU, RMENU
                0x10 or 0xA0 or 0xA1 => ModifierKey.Shift,  // VK_SHIFT, LSHIFT, RSHIFT
                0x5B or 0x5C => ModifierKey.Win,            // VK_LWIN, VK_RWIN
                _ => null
            };
        }
    }
}
