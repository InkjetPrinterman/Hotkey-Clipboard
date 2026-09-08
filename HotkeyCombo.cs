namespace HotkeyClipboard
{
    /// <summary>A modifier + key combination, e.g. Ctrl+Alt+1.</summary>
    public class HotkeyCombo
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }
        public Keys Key { get; set; } = Keys.None;

        public bool IsValid => Key != Keys.None && (Ctrl || Alt || Shift || Win);

        public uint ToModifierFlags()
        {
            uint m = 0;
            if (Alt) m |= NativeMethods.MOD_ALT;
            if (Ctrl) m |= NativeMethods.MOD_CONTROL;
            if (Shift) m |= NativeMethods.MOD_SHIFT;
            if (Win) m |= NativeMethods.MOD_WIN;
            return m;
        }

        public bool Equals(HotkeyCombo? other) =>
            other != null && Ctrl == other.Ctrl && Alt == other.Alt &&
            Shift == other.Shift && Win == other.Win && Key == other.Key;

        public override string ToString()
        {
            var parts = new List<string>();
            if (Ctrl) parts.Add("Ctrl");
            if (Alt) parts.Add("Alt");
            if (Shift) parts.Add("Shift");
            if (Win) parts.Add("Win");
            parts.Add(Key.ToString());
            return string.Join("+", parts);
        }
    }
}
