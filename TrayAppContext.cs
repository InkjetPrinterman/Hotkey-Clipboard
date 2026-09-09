namespace HotkeyClipboard
{
    /// <summary>
    /// Runs the whole app with no main window — just a tray icon plus a
    /// hidden message-only window for receiving hotkeys. Settings and the
    /// snippet manager are opened on demand from the tray menu only; they
    /// are never required for the hotkeys themselves to work.
    /// </summary>
    public class TrayAppContext : ApplicationContext
    {
        private const int RECORD_HOTKEY_ID = 1;
        private const int SNIPPET_ID_BASE = 100;

        private readonly NotifyIcon trayIcon;
        private readonly HotkeyWindow hotkeyWindow;
        private readonly SettingsStore settingsStore;
        private readonly SnippetStore snippetStore;
        private readonly LowLevelKeyboardHook recordingHook;
        private readonly System.Windows.Forms.Timer recordingTimer;
        private readonly RecordingToast toast;
        private readonly HashSet<ModifierKey> heldModifiers = new();

        private bool isRecording;
        private int recordingSecondsLeft;
        private int nextSnippetId = SNIPPET_ID_BASE;

        private AppSettings Settings => settingsStore.Settings;

        public TrayAppContext()
        {
            // Failsafe: if a previous run of this app (or a crash) ever left
            // a modifier key logically stuck down, clear that the moment
            // we start, before doing anything else.
            InputSimulator.ReleaseAllModifiers();

            settingsStore = new SettingsStore();
            snippetStore = new SnippetStore();

            hotkeyWindow = new HotkeyWindow();
            hotkeyWindow.HotkeyPressed += OnHotkeyPressed;

            recordingHook = new LowLevelKeyboardHook();
            recordingHook.KeyEvent += OnRecordingKeyEvent;

            recordingTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            recordingTimer.Tick += RecordingTimer_Tick;

            toast = new RecordingToast();

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "Hotkey Clipboard"
            };
            BuildTrayMenu();

            RegisterRecordHotkey();
            RegisterAllSnippetHotkeys();
            ApplyStartWithWindows(Settings.StartWithWindows);
        }

        private void BuildTrayMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Start Recording Now", null, (s, e) => BeginRecording());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Manage Saved Shortcuts...", null, (s, e) => OpenManageSnippets());
            menu.Items.Add("Settings...", null, (s, e) => OpenSettings());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Fix Stuck Modifier Keys", null, (s, e) => InputSimulator.ReleaseAllModifiers());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => ExitApp());
            trayIcon.ContextMenuStrip = menu;
            trayIcon.DoubleClick += (s, e) => OpenManageSnippets();
        }

        // ==================== Hotkey registration ====================

        private void RegisterRecordHotkey()
        {
            NativeMethods.UnregisterHotKey(hotkeyWindow.Handle, RECORD_HOTKEY_ID);
            var combo = Settings.RecordHotkey;
            if (!combo.IsValid) return;
            NativeMethods.RegisterHotKey(hotkeyWindow.Handle, RECORD_HOTKEY_ID,
                combo.ToModifierFlags() | NativeMethods.MOD_NOREPEAT, (uint)combo.Key);
        }

        private void RegisterAllSnippetHotkeys()
        {
            foreach (var snip in snippetStore.Snippets)
                RegisterSnippetHotkey(snip);
        }

        private bool RegisterSnippetHotkey(Snippet snip)
        {
            int id = nextSnippetId++;
            bool ok = NativeMethods.RegisterHotKey(hotkeyWindow.Handle, id,
                snip.Combo.ToModifierFlags() | NativeMethods.MOD_NOREPEAT, (uint)snip.Combo.Key);
            if (ok) snip.RuntimeHotkeyId = id;
            return ok;
        }

        private void UnregisterSnippetHotkey(Snippet snip)
        {
            if (snip.RuntimeHotkeyId != 0)
            {
                NativeMethods.UnregisterHotKey(hotkeyWindow.Handle, snip.RuntimeHotkeyId);
                snip.RuntimeHotkeyId = 0;
            }
        }

        private void OnHotkeyPressed(int id)
        {
            if (id == RECORD_HOTKEY_ID) { BeginRecording(); return; }
            var snip = snippetStore.Snippets.FirstOrDefault(s => s.RuntimeHotkeyId == id);
            if (snip != null) PasteSnippet(snip);
        }

        // ==================== Recording flow ====================

        private void BeginRecording()
        {
            if (isRecording) return;
            isRecording = true;
            SeedHeldModifiersFromPhysicalState();
            recordingSecondsLeft = Math.Max(1, Settings.RecordingWindowSeconds);

            if (Settings.PlaySoundNotification) SystemSounds.Asterisk.Play();
            if (Settings.ShowVisualNotification)
            {
                toast.SetText(BuildCountdownText());
                toast.ShowToast();
            }

            recordingHook.Install();
            recordingTimer.Start();
        }

        private string BuildCountdownText() =>
            $"Recording… hold a modifier + key to save your selection · Esc to cancel ({recordingSecondsLeft}s)";

        /// <summary>
        /// If the user is still physically holding down the modifiers that
        /// triggered the record-hotkey (e.g. still holding Ctrl+Alt from
        /// Ctrl+Alt+R), the hook won't see a fresh key-down for them. Seed
        /// the held set from the real keyboard state so that case still works.
        /// </summary>
        private void SeedHeldModifiersFromPhysicalState()
        {
            heldModifiers.Clear();
            if (IsPhysicallyDown(0x11)) heldModifiers.Add(ModifierKey.Ctrl);   // VK_CONTROL
            if (IsPhysicallyDown(0x12)) heldModifiers.Add(ModifierKey.Alt);    // VK_MENU
            if (IsPhysicallyDown(0x10)) heldModifiers.Add(ModifierKey.Shift);  // VK_SHIFT
            if (IsPhysicallyDown(0x5B) || IsPhysicallyDown(0x5C)) heldModifiers.Add(ModifierKey.Win); // LWIN/RWIN
        }

        private static bool IsPhysicallyDown(int vk) => (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;

        private void RecordingTimer_Tick(object? sender, EventArgs e)
        {
            recordingSecondsLeft--;
            if (recordingSecondsLeft <= 0)
            {
                StopRecordingInternals();
                PlayTone(success: false);
                ShowTransientMessage("Recording timed out — nothing saved.");
                return;
            }
            if (Settings.ShowVisualNotification) toast.SetText(BuildCountdownText());
        }

        private void StopRecordingInternals()
        {
            isRecording = false;
            recordingTimer.Stop();
            recordingHook.Uninstall();
            heldModifiers.Clear();

            // Failsafe: whatever was or wasn't physically held during the
            // recording window, force everything back to "up" now that
            // we're done listening for it.
            InputSimulator.ReleaseAllModifiers();
        }

        private const int VK_ESCAPE = 0x1B;

        private void OnRecordingKeyEvent(int vk, bool down)
        {
            if (!isRecording) return;

            if (down && vk == VK_ESCAPE)
            {
                StopRecordingInternals();
                PlayTone(false);
                ShowTransientMessage("Recording canceled.");
                return;
            }

            var mod = ModifierKeyHelper.FromVirtualKey(vk);
            if (mod != null)
            {
                if (down) heldModifiers.Add(mod.Value);
                else heldModifiers.Remove(mod.Value);
                return;
            }

            if (down && heldModifiers.Count > 0)
            {
                var combo = new HotkeyCombo
                {
                    Ctrl = heldModifiers.Contains(ModifierKey.Ctrl),
                    Alt = heldModifiers.Contains(ModifierKey.Alt),
                    Shift = heldModifiers.Contains(ModifierKey.Shift),
                    Win = heldModifiers.Contains(ModifierKey.Win),
                    Key = (Keys)vk
                };

                StopRecordingInternals();
                if (Settings.ShowVisualNotification)
                {
                    toast.SetText("Capturing your selected text…");
                    toast.ShowToast();
                }
                _ = HandleCapturedComboAsync(combo);
            }
        }

        private async Task HandleCapturedComboAsync(HotkeyCombo combo)
        {
            try
            {
                if (combo.Equals(Settings.RecordHotkey))
                {
                    PlayTone(false);
                    ShowTransientMessage($"{combo} starts a recording — pick a different combination.");
                    return;
                }

                string? previousClipboard = ClipboardHelper.TryGetTextAsync();

                InputSimulator.SendCopy();
                await Task.Delay(150);
                string? capturedText = ClipboardHelper.TryGetTextAsync();

                if (Settings.RestoreClipboardAfterUse && previousClipboard != null)
                    ClipboardHelper.TryGetTextAsync(previousClipboard);

                if (string.IsNullOrEmpty(capturedText))
                {
                    PlayTone(false);
                    ShowTransientMessage("No text appeared to be selected — nothing was saved.");
                    return;
                }

                var existing = snippetStore.FindByCombo(combo);
                if (existing != null) UnregisterSnippetHotkey(existing);

                var snippet = new Snippet { Combo = combo, Text = capturedText };
                bool ok = RegisterSnippetHotkey(snippet);
                if (!ok)
                {
                    PlayTone(false);
                    ShowTransientMessage($"{combo} is already used elsewhere on your system — try a different combo.");
                    return;
                }

                snippetStore.AddOrUpdate(snippet);
                PlayTone(true);
                ShowTransientMessage($"Saved! Press {combo} anywhere to paste it.");
            }
            catch
            {
                // Never let an unexpected error here leave the user stuck.
                ShowTransientMessage("Something went wrong — nothing was saved.");
            }
            finally
            {
                // Failsafe: guaranteed to run no matter which path above was
                // taken, or whether anything threw.
                InputSimulator.ReleaseAllModifiers();
            }
        }

        // ==================== Paste flow ====================

        private void PasteSnippet(Snippet snip)
        {
            try
            {
                string? previous = Settings.RestoreClipboardAfterUse ? ClipboardHelper.TryGetText() : null;
                ClipboardHelper.TrySetText(snip.Text);
                InputSimulator.SendPaste();

                if (Settings.RestoreClipboardAfterUse)
                {
                    var t = new System.Windows.Forms.Timer { Interval = 250 };
                    t.Tick += (s, e) =>
                    {
                        t.Stop();
                        t.Dispose();
                        if (previous != null) ClipboardHelper.TrySetText(previous);
                    };
                    t.Start();
                }
            }
            finally
            {
                InputSimulator.ReleaseAllModifiers();
            }
        }

        // ==================== Notifications ====================

        private void PlayTone(bool success)
        {
            if (!Settings.PlaySoundNotification) return;
            (success ? SystemSounds.Asterisk : SystemSounds.Hand).Play();
        }

        private void ShowTransientMessage(string text, int hideAfterMs = 1600)
        {
            if (!Settings.ShowVisualNotification)
            {
                toast.HideToast();
                return;
            }
            toast.SetText(text);
            toast.ShowToast();
            ScheduleToastHide(hideAfterMs);
        }

        private void ScheduleToastHide(int ms)
        {
            var t = new System.Windows.Forms.Timer { Interval = ms };
            t.Tick += (s, e) => { t.Stop(); t.Dispose(); toast.HideToast(); };
            t.Start();
        }

        // ==================== Settings / management windows ====================

        private void OpenSettings()
        {
            using var form = new SettingsForm(Settings);
            if (form.ShowDialog() == DialogResult.OK)
            {
                settingsStore.Save();
                RegisterRecordHotkey();
                ApplyStartWithWindows(Settings.StartWithWindows);
            }
        }

        private void OpenManageSnippets()
        {
            using var form = new ManageSnippetsForm(snippetStore);
            form.SnippetDeleted += UnregisterSnippetHotkey;
            form.ShowDialog();
        }

        private void ApplyStartWithWindows(bool enabled)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
                if (key == null) return;
                if (enabled)
                    key.SetValue("HotkeyClipboard", Application.ExecutablePath);
                else
                    key.DeleteValue("HotkeyClipboard", throwOnMissingValue: false);
            }
            catch
            {
                // Non-fatal: registry access can fail in locked-down environments.
            }
        }

        private void ExitApp()
        {
            foreach (var snip in snippetStore.Snippets) UnregisterSnippetHotkey(snip);
            NativeMethods.UnregisterHotKey(hotkeyWindow.Handle, RECORD_HOTKEY_ID);
            recordingHook.Dispose();
            InputSimulator.ReleaseAllModifiers();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            toast.Dispose();
            hotkeyWindow.Dispose();
            Application.Exit();
        }
    }
}
