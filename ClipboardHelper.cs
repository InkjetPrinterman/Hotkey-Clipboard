namespace HotkeyClipboard
{
    public static class ClipboardHelper
    {
        public static string? TryGetText()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return Clipboard.ContainsText() ? Clipboard.GetText() : null;
                }
                catch (ExternalException)
                {
                    Thread.Sleep(40);
                }
            }
            return null;
        }

        public static bool TrySetText(string text)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Clipboard.SetText(text);
                    return true;
                }
                catch (ExternalException)
                {
                    Thread.Sleep(40);
                }
            }
            return false;
        }
    public static async Task<string?> TryGetTextAsync()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return Clipboard.ContainsText() ? Clipboard.GetText() : null;
                }
                    catch (ExternalException)
                {
                await Task.Delay(40);
            }
        }
                return null;
    }

    public static async Task<bool> TrySetTextAsync(string text)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Clipboard.SetText(text);
                    return true;
                }
                    catch (ExternalException)
            {
                await Task.Delay(40);
        }
    }
                return false;
    }
 }
}
