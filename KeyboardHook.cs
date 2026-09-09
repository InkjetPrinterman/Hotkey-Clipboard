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
public class LowLevelKeyboardHook : IDisposable {
public event Action<int, bool>? KeyEvent;

private IntPtr hookId = IntPtr.Zero;
private readonly LowLevelKeyboardProc proc;
private SynchronizationContext? marshal;

public LowLevelKeyboardHook()
{
// Held in a field so the delegate is not collected while the OS
// still holds a pointer to it.
proc = HookCallback;
}

public void Install()
{
if (hookId != IntPtr.Zero) return;

// The callback is delivered via this thread's message queue, so a
// thread that can receive it at all is by definition one that
// pumps messages — which is what makes it a valid target to post
// back to. The fallback covers a caller that pumps but has no
// context installed; in this app Application.Run has always
// installed one long before Install() is first reached.
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
// marshal is deliberately left set: events posted just before this
// point are still sitting in the queue, and clearing the context
// would strand them. Handlers already ignore events that arrive
// after the recording window has closed.
}

private const uint LLKHF_INJECTED = 0x00000010;

private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
{
// Snapshot the handle. Uninstall() now runs from the message loop
// rather than nested inside this callback, so it can zero the
// field while this call is on the stack — and CallNextHookEx below
// must not be handed IntPtr.Zero.
IntPtr thisHook = hookId;

if (nCode >= 0)
{
// lParam points at a KBDLLHOOKSTRUCT valid only for the
// duration of this call, so read everything we need now: the
// posted continuation runs long after it is gone.
uint flags = unchecked((uint)Marshal.ReadInt32(lParam, 8));
bool injected = (flags & LLKHF_INJECTED) != 0;

if (!injected)
{
int vkCode = Marshal.ReadInt32(lParam);
int msg = wParam.ToInt32();
bool isDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
bool isUp = msg == WM_KEYUP || msg == WM_SYSKEYUP;

if (isDown || isUp)
{
// Post, never Send: Send would block this callback on
// the subscriber exactly like a direct call does.
var handler = KeyEvent;
if (handler != null)
marshal?.Post(_ => handler(vkCode, isDown), null);
}

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
return CallNextHookEx(thisHook, nCode, wParam, lParam);
}

public void Dispose() => Uninstall();
}
}
