namespace HotkeyClipboard
{
    /// <summary>
    /// A window with no UI at all (parented to HWND_MESSAGE) that exists only
    /// so RegisterHotKey has a handle to deliver WM_HOTKEY messages to. This
    /// is what lets hotkeys work without any visible application window.
    /// </summary>
    public class HotkeyWindow : NativeWindow, IDisposable
    {
        public event Action<int>? HotkeyPressed;

        private const int HWND_MESSAGE = -3;

        public HotkeyWindow()
        {
            var cp = new CreateParams { Parent = (IntPtr)HWND_MESSAGE };
            CreateHandle(cp);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
            {
                HotkeyPressed?.Invoke(m.WParam.ToInt32());
            }
            base.WndProc(ref m);
        }

        public void Dispose() => DestroyHandle();
    }
}
