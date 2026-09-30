public static class TitleSession
{
    public enum InputMode
    {
        None,
        Xbox,
        KeyboardMouse
    }

    public enum Language
    {
        Korean,
        English
    }

    public static InputMode SelectedInputMode { get; set; } = InputMode.None;
    public static Language SelectedLanguage { get; set; } = Language.Korean;
}
