namespace HotkeyClipboard
{
    public class AppSettings
    {
        /// <summary>
        /// Hard ceiling on the recording window, regardless of what the user
        /// configures. Every key is swallowed system-wide while recording is
        /// active, so this window is intentionally kept short no matter what.
        /// </summary>
        public const int MaxRecordingWindowSeconds = 10;

        /// <summary>Hotkey that opens the "recording window" to define a new snippet.</summary>
        public HotkeyCombo RecordHotkey { get; set; } = new HotkeyCombo { Ctrl = true, Shift = true, Key = Keys.C };

        /// <summary>How many seconds the recording window stays open waiting for a combo.</summary>
        public int RecordingWindowSeconds { get; set; } = 5;

        /// <summary>Show an on-screen toast while the recording window is active.</summary>
        public bool ShowVisualNotification { get; set; } = true;

        /// <summary>Play a system sound when the recording window opens/succeeds/fails.</summary>
        public bool PlaySoundNotification { get; set; } = true;

        /// <summary>Put the clipboard back the way it was after a copy/paste operation.</summary>
        public bool RestoreClipboardAfterUse { get; set; } = true;

        /// <summary>Launch automatically at Windows sign-in.</summary>
        public bool StartWithWindows { get; set; } = false;
    }
}
