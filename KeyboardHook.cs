using static HotkeyClipboard.NativeMethods;

namespace HotkeyClipboard
{
    /// <summary>
    /// A process-wide low-level keyboard hook. This is only installed while a
    /// "recording window" is open, since it's the only way to detect an
    /// arbitrary, not-yet-registered modifier+key combo as the user types it.
    /// It is uninstalled again the instant a combo is captured or the window
    /// times out, so it does not sit in the keyboard path during normal use.
    ///
    /// While installed it also SWALLOWS every keystroke (returns non-zero
    /// instead of calling CallNextHookEx) so that whatever key the user
    /// presses to define a combo never reaches the focused app — otherwise,
    /// e.g. pressing "1" while defining Ctrl+1 in Notepad would type "1"
    /// into the document and replace any highlighted text.
    /// </summary>
    public class LowLevelKeyboardHook : IDisposable
    private SynchronizationContext? marshal;
    {
        public event Action<int, bool>? KeyEvent;

        private IntPtr hookId = IntPtr.Zero;
        private readonly LowLevelKeyboardProc proc;

        public LowLevelKeyboardHook()
        {
            proc = HookCallback;
        }

        public void Install()
        {
            if (hookId != IntPtr.Zero) return;
            marshal = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            hookId = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule?.ModuleName), 0);
        }

        public void Uninstall()
        {
            if (hookId == IntPtr.Zero) return;
            UnhookWindowsHookEx(hookId);
            hookId = IntPtr.Zero;
        }

        private const uint LLKHF_INJECTED = 0x00000010;

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            IntPtr thisHook = hookId;
            if (nCode >= 0)
            {
                uint flags = unchecked((uint)Marshal.ReadInt32(lParam, 8));
                bool injected = (flags & LLKHF_INJECTED) != 0;

                if (!injected)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    int msg = wParam.ToInt32();
                    if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                        KeyEvent?.Invoke(vkCode, true);
                    else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                        KeyEvent?.Invoke(vkCode, false);

                    // Block this real keystroke from reaching the rest of
                    // the hook chain / the focused window. This hook only
                    // ever runs during the short recording window, so
                    // nothing the user types during that window should
                    // land in any application.
                    return (IntPtr)1;
                }
                // Never swallow synthetic/injected input (e.g. our own
                // failsafe modifier release) — always let it flow through.
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        public void Dispose() => Uninstall();
    }
}
