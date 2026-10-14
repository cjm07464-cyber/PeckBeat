using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StagePauseMenuController : MonoBehaviour
{
    private const int ContinueIndex = 0;
    private const int RestartIndex = 1;
    private const int QuitIndex = 2;

    [Header("UI References")]
    [SerializeField] private Canvas pauseCanvas;
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private RectTransform curtainRoot;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject countdownRoot;
    [SerializeField] private Image pauseRatImage;
    [SerializeField] private Image focusRatImage;
    [SerializeField] private Image countdownImage;
    [SerializeField] private Image dimOverlay;
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text continueText;
    [SerializeField] private TMP_Text restartText;
    [SerializeField] private TMP_Text quitText;

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset koreanFont;
    [SerializeField] private TMP_FontAsset englishFont;

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip pauseCurtainSfx;
    [SerializeField] private AudioClip countdownSfx;

    [Header("Sprites")]
    [SerializeField] private Sprite pauseRatSprite;
    [SerializeField] private Sprite focusRatSprite;
    [SerializeField] private Sprite countdown3Sprite;
    [SerializeField] private Sprite countdown2Sprite;
    [SerializeField] private Sprite countdown1Sprite;

    [Header("Pause Input")]
    [SerializeField] private KeyCode pauseKeyboardKey = KeyCode.Escape;
    [SerializeField] private KeyCode pauseGamepadButton = KeyCode.JoystickButton7;
    [SerializeField] private bool requireStageBgmPlaying = true;

    [Header("Timing")]
    [Min(0f)] [SerializeField] private float curtainSlideDuration = 0.3f;
    [Min(0f)] [SerializeField] private float countdownNumberDuration = 0.65f;
    [Min(0f)] [SerializeField] private float fadeOutDuration = 0.6f;
    [SerializeField] private float curtainHiddenOffsetY = 1200f;

    [Header("Focus Layout")]
    [SerializeField] private float continueFocusX = -340f;
    [SerializeField] private float restartFocusX;
    [SerializeField] private float quitFocusX = 340f;
    [SerializeField] private float focusRatFixedY = -105f;

    private PeckBeatRhythmManager rhythmManager;
    private PeckBeatInput gameplayInput;
    private Vector2 curtainShownPosition;
    private Vector2 curtainHiddenPosition;
    private bool gameplayInputWasEnabled;
    private bool isPaused;
    private bool transitionRunning;
    private bool previousStickLeftHeld;
    private bool warnedMissingKoreanFont;
    private bool warnedMissingEnglishFont;
    private int selectionIndex;

    private void Awake()
    {
        rhythmManager = Object.FindFirstObjectByType<PeckBeatRhythmManager>(FindObjectsInactive.Include);
        gameplayInput = Object.FindFirstObjectByType<PeckBeatInput>(FindObjectsInactive.Include);

        if (curtainRoot != null)
        {
            curtainShownPosition = curtainRoot.anchoredPosition;
            curtainHiddenPosition = curtainShownPosition + Vector2.up * curtainHiddenOffsetY;
            curtainRoot.anchoredPosition = curtainHiddenPosition;
        }

        ApplyAssignedAssets();
        HidePauseUiImmediately();
    }

    private void Update()
    {
        if (!isPaused)
        {
            if (!transitionRunning && PausePressedThisFrame() && CanPauseNow())
            {
                StartCoroutine(EnterPauseRoutine());
            }

            return;
        }

        if (transitionRunning)
        {
            return;
        }

        if (PausePressedThisFrame())
        {
            StartCoroutine(ContinueRoutine());
            return;
        }

        if (NextSelectionPressed())
        {
            selectionIndex = (selectionIndex + 1) % 3;
            RefreshFocusPosition();
        }
        else if (PreviousSelectionPressed())
        {
            selectionIndex = (selectionIndex + 2) % 3;
            RefreshFocusPosition();
        }

        if (ConfirmSelectionPressed())
        {
            switch (selectionIndex)
            {
                case ContinueIndex:
                    StartCoroutine(ContinueRoutine());
                    break;
                case RestartIndex:
                    RestartStage();
                    break;
                case QuitIndex:
                    StartCoroutine(QuitRoutine());
                    break;
            }
        }
    }

    private bool CanPauseNow()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Stage01_Prototype" && sceneName != "Stage02")
        {
            return false;
        }

        if (rhythmManager == null)
        {
            return false;
        }

        return !requireStageBgmPlaying || rhythmManager.IsSongPlaying;
    }

    private IEnumerator EnterPauseRoutine()
    {
        transitionRunning = true;

        if (rhythmManager == null || !rhythmManager.PausePlayback())
        {
            transitionRunning = false;
            yield break;
        }

        isPaused = true;
        gameplayInputWasEnabled = gameplayInput != null && gameplayInput.enabled;
        if (gameplayInputWasEnabled)
        {
            gameplayInput.enabled = false;
        }

        Time.timeScale = 0f;
        ShowPauseCanvas();
        ApplyLanguageAndFont();
        selectionIndex = ContinueIndex;
        RefreshFocusPosition();

        SetActive(countdownRoot, false);
        SetActive(menuRoot, false);
        SetImageAlpha(fadeOverlay, 0f);
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(false);
        }

        if (curtainRoot != null)
        {
            curtainRoot.anchoredPosition = curtainHiddenPosition;
        }

        PlayUiSfx(pauseCurtainSfx);
        yield return SlideCurtain(curtainHiddenPosition, curtainShownPosition);
        SetActive(menuRoot, true);
        transitionRunning = false;
    }

    private IEnumerator ContinueRoutine()
    {
        if (transitionRunning)
        {
            yield break;
        }

        transitionRunning = true;
        SetActive(menuRoot, false);
        PlayUiSfx(pauseCurtainSfx);
        yield return SlideCurtain(curtainShownPosition, curtainHiddenPosition);
        yield return PlayCountdownRoutine();

        rhythmManager?.ResumePlayback();
        if (gameplayInput != null && gameplayInputWasEnabled)
        {
            gameplayInput.enabled = true;
        }

        Time.timeScale = 1f;
        isPaused = false;
        transitionRunning = false;
        HidePauseUiImmediately();
    }

    private IEnumerator PlayCountdownRoutine()
    {
        SetActive(countdownRoot, true);
        Sprite[] countdownSprites = { countdown3Sprite, countdown2Sprite, countdown1Sprite };
        foreach (Sprite sprite in countdownSprites)
        {
            if (countdownImage != null)
            {
                countdownImage.sprite = sprite;
                countdownImage.enabled = sprite != null;
            }

            PlayUiSfx(countdownSfx);
            yield return WaitUnscaled(countdownNumberDuration);
        }

        SetActive(countdownRoot, false);
    }

    private void RestartStage()
    {
        RestoreSimulationBeforeSceneChange();
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    private IEnumerator QuitRoutine()
    {
        if (transitionRunning)
        {
            yield break;
        }

        transitionRunning = true;
        SetActive(menuRoot, false);

        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            float duration = Mathf.Max(0f, fadeOutDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetImageAlpha(fadeOverlay, Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration)));
                yield return null;
            }

            SetImageAlpha(fadeOverlay, 1f);
        }

        RestoreSimulationBeforeSceneChange();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RestoreSimulationBeforeSceneChange()
    {
        rhythmManager?.ResumePlayback();
        if (gameplayInput != null && gameplayInputWasEnabled)
        {
            gameplayInput.enabled = true;
        }

        Time.timeScale = 1f;
        isPaused = false;
    }

    private IEnumerator SlideCurtain(Vector2 from, Vector2 to)
    {
        if (curtainRoot == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0f, curtainSlideDuration);
        float elapsed = 0f;
        curtainRoot.anchoredPosition = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.0001f, duration));
            curtainRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, progress);
            yield return null;
        }

        curtainRoot.anchoredPosition = to;
    }

    private IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0f, duration);
        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void ShowPauseCanvas()
    {
        if (pauseCanvas != null)
        {
            pauseCanvas.enabled = true;
        }

        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 1f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = true;
        }
    }

    private void HidePauseUiImmediately()
    {
        SetActive(menuRoot, false);
        SetActive(countdownRoot, false);

        if (fadeOverlay != null)
        {
            SetImageAlpha(fadeOverlay, 0f);
            fadeOverlay.gameObject.SetActive(false);
        }

        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }

        if (pauseCanvas != null)
        {
            pauseCanvas.enabled = false;
        }
    }

    private void ApplyAssignedAssets()
    {
        ApplyOptionalSprite(pauseRatImage, pauseRatSprite);
        ApplyOptionalSprite(focusRatImage, focusRatSprite);
    }

    private void ApplyLanguageAndFont()
    {
        bool english = TitleSession.SelectedLanguage == TitleSession.Language.English;
        TMP_FontAsset selectedFont = english ? englishFont : koreanFont;

        if (selectedFont == null && HasAnyTextReference())
        {
            WarnMissingFontOnce(english);
        }

        SetTextAndFont(titleText, english ? "Pause" : "일시 정지", selectedFont);
        SetTextAndFont(continueText, english ? "Continue" : "계속하기", selectedFont);
        SetTextAndFont(restartText, english ? "Restart Stage" : "스테이지 다시하기", selectedFont);
        SetTextAndFont(quitText, english ? "Quit" : "그만하기", selectedFont);
    }

    private bool HasAnyTextReference()
    {
        return titleText != null || continueText != null || restartText != null || quitText != null;
    }

    private void WarnMissingFontOnce(bool english)
    {
        if (english)
        {
            if (warnedMissingEnglishFont)
            {
                return;
            }

            warnedMissingEnglishFont = true;
            Debug.LogWarning(
                "[StagePauseMenuController] English Font is not assigned. Existing TMP fonts will be kept.",
                this);
            return;
        }

        if (warnedMissingKoreanFont)
        {
            return;
        }

        warnedMissingKoreanFont = true;
        Debug.LogWarning(
            "[StagePauseMenuController] Korean Font is not assigned. Existing TMP fonts will be kept.",
            this);
    }

    private void RefreshFocusPosition()
    {
        if (focusRatImage == null)
        {
            return;
        }

        float x = selectionIndex switch
        {
            RestartIndex => restartFocusX,
            QuitIndex => quitFocusX,
            _ => continueFocusX
        };
        focusRatImage.rectTransform.anchoredPosition = new Vector2(x, focusRatFixedY);
    }

    private bool PausePressedThisFrame()
    {
        return KeyboardKeyPressedThisFrame(pauseKeyboardKey)
            || GamepadButtonPressedThisFrame(pauseGamepadButton);
    }

    private static bool NextSelectionPressed()
    {
        return (Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
    }

    private bool PreviousSelectionPressed()
    {
        bool keyboardPressed = Keyboard.current != null
            && (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame);
        bool dpadPressed = Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame;
        bool stickHeld = Gamepad.current != null && Gamepad.current.leftStick.ReadValue().x < -0.5f;
        bool stickPressed = stickHeld && !previousStickLeftHeld;
        previousStickLeftHeld = stickHeld;
        return keyboardPressed || dpadPressed || stickPressed;
    }

    private static bool ConfirmSelectionPressed()
    {
        bool keyboardPressed = Keyboard.current != null
            && (Keyboard.current.jKey.wasPressedThisFrame
                || Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                || Keyboard.current.spaceKey.wasPressedThisFrame);
        bool gamepadPressed = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        return keyboardPressed || gamepadPressed;
    }

    private static bool KeyboardKeyPressedThisFrame(KeyCode keyCode)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        bool specialKeyPressed = keyCode switch
        {
            KeyCode.Escape => keyboard.escapeKey.wasPressedThisFrame,
            KeyCode.Tab => keyboard.tabKey.wasPressedThisFrame,
            KeyCode.Space => keyboard.spaceKey.wasPressedThisFrame,
            KeyCode.Return => keyboard.enterKey.wasPressedThisFrame,
            KeyCode.P => keyboard.pKey.wasPressedThisFrame,
            KeyCode.BackQuote => keyboard.backquoteKey.wasPressedThisFrame,
            _ => false
        };

        if (specialKeyPressed)
        {
            return true;
        }

        if (System.Enum.TryParse(keyCode.ToString(), true, out Key inputSystemKey)
            && inputSystemKey != Key.None)
        {
            return keyboard[inputSystemKey].wasPressedThisFrame;
        }

        return false;
    }

    private static bool GamepadButtonPressedThisFrame(KeyCode keyCode)
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null)
        {
            return false;
        }

        return keyCode switch
        {
            KeyCode.JoystickButton0 => gamepad.buttonSouth.wasPressedThisFrame,
            KeyCode.JoystickButton1 => gamepad.buttonEast.wasPressedThisFrame,
            KeyCode.JoystickButton2 => gamepad.buttonWest.wasPressedThisFrame,
            KeyCode.JoystickButton3 => gamepad.buttonNorth.wasPressedThisFrame,
            KeyCode.JoystickButton4 => gamepad.leftShoulder.wasPressedThisFrame,
            KeyCode.JoystickButton5 => gamepad.rightShoulder.wasPressedThisFrame,
            KeyCode.JoystickButton6 => gamepad.selectButton.wasPressedThisFrame,
            KeyCode.JoystickButton7 => gamepad.startButton.wasPressedThisFrame,
            KeyCode.JoystickButton8 => gamepad.leftStickButton.wasPressedThisFrame,
            KeyCode.JoystickButton9 => gamepad.rightStickButton.wasPressedThisFrame,
            _ => gamepad.startButton.wasPressedThisFrame
        };
    }

    private void PlayUiSfx(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }

    private static void ApplyOptionalSprite(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        if (sprite != null)
        {
            image.sprite = sprite;
        }

        image.enabled = image.sprite != null;
        image.preserveAspect = true;
    }

    private static void SetTextAndFont(TMP_Text text, string value, TMP_FontAsset font)
    {
        if (text == null)
        {
            return;
        }

        text.text = value;
        if (font != null)
        {
            text.font = font;
        }
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void OnDisable()
    {
        if (isPaused)
        {
            RestoreSimulationBeforeSceneChange();
        }
    }
}
