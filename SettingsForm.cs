namespace HotkeyClipboard
{
    public class SettingsForm : Form
    {
        private readonly AppSettings settings;
        private HotkeyCombo pendingRecordHotkey;

        private TextBox hotkeyBox = null!;
        private NumericUpDown durationBox = null!;
        private CheckBox visualCheck = null!;
        private CheckBox soundCheck = null!;
        private CheckBox restoreClipboardCheck = null!;
        private CheckBox startWithWindowsCheck = null!;

        public SettingsForm(AppSettings settings)
        {
            this.settings = settings;
            pendingRecordHotkey = new HotkeyCombo
            {
                Ctrl = settings.RecordHotkey.Ctrl,
                Alt = settings.RecordHotkey.Alt,
                Shift = settings.RecordHotkey.Shift,
                Win = settings.RecordHotkey.Win,
                Key = settings.RecordHotkey.Key
            };
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Hotkey Clipboard — Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(440, 300);
            Font = new Font("Segoe UI", 9f);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(16),
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            layout.Controls.Add(new Label { Text = "Start-recording hotkey:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            hotkeyBox = new TextBox { ReadOnly = true, Width = 220, Text = pendingRecordHotkey.ToString() };
            hotkeyBox.KeyDown += HotkeyBox_KeyDown;
            layout.Controls.Add(hotkeyBox, 1, 0);

            layout.Controls.Add(new Label { Text = "Recording window length (seconds):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            durationBox = new NumericUpDown { Minimum = 1, Maximum = 60, Value = settings.RecordingWindowSeconds, Width = 80 };
            layout.Controls.Add(durationBox, 1, 1);

            visualCheck = new CheckBox { Text = "Show an on-screen notification while recording", Checked = settings.ShowVisualNotification, AutoSize = true };
            layout.Controls.Add(visualCheck, 0, 2);
            layout.SetColumnSpan(visualCheck, 2);

            soundCheck = new CheckBox { Text = "Play a sound notification while recording", Checked = settings.PlaySoundNotification, AutoSize = true };
            layout.Controls.Add(soundCheck, 0, 3);
            layout.SetColumnSpan(soundCheck, 2);

            restoreClipboardCheck = new CheckBox { Text = "Restore previous clipboard contents after copy/paste", Checked = settings.RestoreClipboardAfterUse, AutoSize = true };
            layout.Controls.Add(restoreClipboardCheck, 0, 4);
            layout.SetColumnSpan(restoreClipboardCheck, 2);

            startWithWindowsCheck = new CheckBox { Text = "Start automatically when Windows starts", Checked = settings.StartWithWindows, AutoSize = true };
            layout.Controls.Add(startWithWindowsCheck, 0, 5);
            layout.SetColumnSpan(startWithWindowsCheck, 2);

            var hint = new Label
            {
                Text = "Click the hotkey box, then press the combo you want (e.g. Ctrl+Alt+R).",
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic)
            };
            layout.Controls.Add(hint, 0, 6);
            layout.SetColumnSpan(hint, 2);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(8)
            };
            var okBtn = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 90 };
            var cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
            okBtn.Click += (s, e) => ApplyAndClose();
            buttonPanel.Controls.Add(okBtn);
            buttonPanel.Controls.Add(cancelBtn);

            Controls.Add(layout);
            Controls.Add(buttonPanel);
            AcceptButton = okBtn;
            CancelButton = cancelBtn;
        }

        private void HotkeyBox_KeyDown(object? sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;

            // Ignore a lone modifier press; wait for the full combo.
            if (e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey or Keys.LWin or Keys.RWin)
                return;

            if (!e.Control && !e.Alt && !e.Shift)
            {
                MessageBox.Show(this, "Please include at least one modifier (Ctrl, Alt, or Shift) with the key.",
                    "Hotkey Clipboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            pendingRecordHotkey = new HotkeyCombo
            {
                Ctrl = e.Control,
                Alt = e.Alt,
                Shift = e.Shift,
                Win = false,
                Key = e.KeyCode
            };
            hotkeyBox.Text = pendingRecordHotkey.ToString();
        }

        private void ApplyAndClose()
        {
            settings.RecordHotkey = pendingRecordHotkey;
            settings.RecordingWindowSeconds = (int)durationBox.Value;
            settings.ShowVisualNotification = visualCheck.Checked;
            settings.PlaySoundNotification = soundCheck.Checked;
            settings.RestoreClipboardAfterUse = restoreClipboardCheck.Checked;
            settings.StartWithWindows = startWithWindowsCheck.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
