namespace HotkeyClipboard
{
    /// <summary>A single saved piece of text and the combo that copies/pastes it.</summary>
    public class Snippet
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public HotkeyCombo Combo { get; set; } = new HotkeyCombo();
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The numeric id this combo is registered under with RegisterHotKey
        /// for THIS run of the app. Reassigned on every startup — not persisted.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public int RuntimeHotkeyId { get; set; }
    }
}
