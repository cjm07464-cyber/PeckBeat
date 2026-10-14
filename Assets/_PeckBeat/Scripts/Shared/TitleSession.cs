using UnityEngine.SceneManagement;

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

    public enum TitleFlowMode
    {
        NormalTitle,
        InterStageToStage02,
        PostStage02Intro
    }

    private const string TitleSceneName = "TitleScene";

    public static InputMode SelectedInputMode { get; set; } = InputMode.None;
    public static Language SelectedLanguage { get; set; } = Language.Korean;
    public static TitleFlowMode FlowMode { get; set; } = TitleFlowMode.NormalTitle;

    public static void RequestStage01ClearToStage02()
    {
        FlowMode = TitleFlowMode.InterStageToStage02;
        SceneManager.LoadScene(TitleSceneName);
    }

    public static void RequestStage02ClearToPostIntro()
    {
        FlowMode = TitleFlowMode.PostStage02Intro;
        SceneManager.LoadScene(TitleSceneName);
    }
}
