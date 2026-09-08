namespace HotkeyClipboard
{
    /// <summary>
    /// A small always-on-top popup used purely as a visual notification.
    /// It is created with WS_EX_NOACTIVATE so showing it never steals focus
    /// away from the window/selection the user is working with — critical,
    /// since focus-stealing here would break the "copy the highlighted text"
    /// step entirely.
    /// </summary>
    public class RecordingToast : Form
    {
        private readonly Label label;

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                const int WS_EX_TOPMOST = 0x00000008;
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
                return cp;
            }
        }

        public RecordingToast()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(32, 32, 36);
            Size = new Size(360, 64);
            Opacity = 0.95;

            label = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(8),
                Text = "Recording…"
            };
            Controls.Add(label);

            var area = (Screen.PrimaryScreen ?? Screen.AllScreens[0]).WorkingArea;
            Location = new Point(area.Right - Width - 20, area.Bottom - Height - 20);
        }

        public void SetText(string text) => label.Text = text;

        public void ShowToast()
        {
            if (!Visible) Show();
        }

        public void HideToast()
        {
            if (Visible) Hide();
        }
    }
}
