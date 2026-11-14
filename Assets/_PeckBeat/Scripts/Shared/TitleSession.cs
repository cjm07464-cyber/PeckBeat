using UnityEngine;
using UnityEngine.InputSystem;
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
    private static bool restartInProgress;

    public static InputMode SelectedInputMode { get; set; } = InputMode.None;
    public static Language SelectedLanguage { get; set; } = Language.Korean;
    public static TitleFlowMode FlowMode { get; set; } = TitleFlowMode.NormalTitle;
    public static bool RestartInProgress => restartInProgress;

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

    public static void ResetForNewGame()
    {
        SelectedInputMode = InputMode.None;
        SelectedLanguage = Language.Korean;
        FlowMode = TitleFlowMode.NormalTitle;
    }

    public static void RestartAtTitleForNewGame()
    {
        if (restartInProgress)
        {
            return;
        }

        restartInProgress = true;
        ResetForNewGame();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        StagePauseMenuController.ClearGlobalInputBlockForSceneChange();
        StopAllAudioSources();

        SceneManager.sceneLoaded -= HandleRestartSceneLoaded;
        SceneManager.sceneLoaded += HandleRestartSceneLoaded;
        SceneManager.LoadScene(TitleSceneName);
    }

    private static void StopAllAudioSources()
    {
        AudioSource[] audioSources = Object.FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
    }

    private static void HandleRestartSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (!string.Equals(scene.name, TitleSceneName, System.StringComparison.Ordinal))
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleRestartSceneLoaded;
        restartInProgress = false;
    }
}

[DefaultExecutionOrder(-32000)]
public sealed class PeckBeatEmergencyRestart : MonoBehaviour
{
    private static PeckBeatEmergencyRestart instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject restartObject = new GameObject(nameof(PeckBeatEmergencyRestart));
        instance = restartObject.AddComponent<PeckBeatEmergencyRestart>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (TitleSession.RestartInProgress)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame)
        {
            TitleSession.RestartAtTitleForNewGame();
        }
    }
}
