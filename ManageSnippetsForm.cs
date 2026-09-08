namespace HotkeyClipboard
{
    public class ManageSnippetsForm : Form
    {
        private readonly SnippetStore store;
        private ListView listView = null!;

        /// <summary>Raised right before a snippet is removed, so the caller can unregister its hotkey.</summary>
        public event Action<Snippet>? SnippetDeleted;

        public ManageSnippetsForm(SnippetStore store)
        {
            this.store = store;
            BuildUi();
            Populate();
        }

        private void BuildUi()
        {
            Text = "Hotkey Clipboard — Saved Shortcuts";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(540, 380);
            Font = new Font("Segoe UI", 9f);

            listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            listView.Columns.Add("Shortcut", 120);
            listView.Columns.Add("Preview", 280);
            listView.Columns.Add("Saved", 120);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(8)
            };
            var deleteBtn = new Button { Text = "Delete Selected", Width = 130 };
            var closeBtn = new Button { Text = "Close", Width = 90, DialogResult = DialogResult.OK };
            deleteBtn.Click += DeleteBtn_Click;
            buttonPanel.Controls.Add(closeBtn);
            buttonPanel.Controls.Add(deleteBtn);

            var emptyHint = new Label
            {
                Dock = DockStyle.Top,
                Text = "Snippets you record will appear here. Select one and click Delete to remove it.",
                AutoSize = false,
                Height = 24,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };

            Controls.Add(listView);
            Controls.Add(emptyHint);
            Controls.Add(buttonPanel);
        }

        private void Populate()
        {
            listView.Items.Clear();
            foreach (var snip in store.Snippets.OrderBy(s => s.CreatedUtc))
            {
                var preview = snip.Text.Length > 60 ? snip.Text[..60] + "…" : snip.Text;
                preview = preview.Replace('\r', ' ').Replace('\n', ' ');
                var item = new ListViewItem(new[]
                {
                    snip.Combo.ToString(),
                    preview,
                    snip.CreatedUtc.ToLocalTime().ToString("g")
                })
                {
                    Tag = snip
                };
                listView.Items.Add(item);
            }
        }

    private void DeleteBtn_Click(object? sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0) return;

    // Snapshot the selected snippets first — we're about to mutate
    // the store and repopulate the list, so don't rely on
    // SelectedItems staying valid mid-loop.
            var selected = listView.SelectedItems
                .Cast<ListViewItem>()
                .Select(item => (Snippet)item.Tag!)
                .ToList();

            string message = selected.Count == 1
                ? $"Delete the shortcut {selected[0].Combo}?"
                : $"Delete these {selected.Count} shortcuts?\n\n" +
                  string.Join("\n", selected.Select(s => s.Combo.ToString()));

            var confirm = MessageBox.Show(this, message, "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            foreach (var snip in selected)
            {
                SnippetDeleted?.Invoke(snip);
                store.Remove(snip);
            }

               Populate();
        }
    }
}
