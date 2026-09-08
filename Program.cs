namespace HotkeyClipboard
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Failsafe: no matter how this process ends — clean exit, crash,
            // or an unhandled exception — never leave a modifier key
            // logically stuck down for the rest of the Windows session.
            AppDomain.CurrentDomain.ProcessExit += (s, e) => InputSimulator.ReleaseAllModifiers();
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                InputSimulator.ReleaseAllModifiers();
            };
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                InputSimulator.ReleaseAllModifiers();
            };

            // Also clear anything left over from a previous run before we
            // do anything else.
            InputSimulator.ReleaseAllModifiers();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var mutex = new Mutex(true, "HotkeyClipboard_SingleInstance_9F3B1A", out bool isNew);
            if (!isNew)
            {
                MessageBox.Show("Hotkey Clipboard is already running — check your system tray.",
                    "Hotkey Clipboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.Run(new TrayAppContext());
        }
    }
}
