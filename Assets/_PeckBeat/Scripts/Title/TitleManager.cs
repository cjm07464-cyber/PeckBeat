using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public sealed class TitleManager : MonoBehaviour
{
    private enum TitleState
    {
        InputDeviceSelect,
        LanguageSelect,
        MainTitle,
        StartingGame
    }

    private enum SelectedInputMode
    {
        None,
        Xbox,
        KeyboardMouse
    }

    private enum SelectedLanguage
    {
        Korean,
        English
    }

    [Header("Scene Flow")]
    [SerializeField] private string stage01SceneName = "Stage01_Prototype";
    [SerializeField] private string stage02SceneName = "Stage02";

    [Header("Roots")]
    [SerializeField] private RectTransform titleRoot;
    [SerializeField] private GameObject stage01Title;
    [SerializeField] private GameObject stage02Title;

    [Header("Panels")]
    [SerializeField] private GameObject inputSelectPanel;
    [SerializeField] private GameObject languageSelectPanel;
    [SerializeField] private GameObject mainTitlePanel;

    [Header("Input Select Options")]
    [SerializeField] private RectTransform xboxOption;
    [SerializeField] private RectTransform keyboardMouseOption;
    [SerializeField] private RectTransform inputSelectFocusFrame;

    [Header("Language Select Options")]
    [SerializeField] private RectTransform koreanOption;
    [SerializeField] private RectTransform englishOption;
    [SerializeField] private RectTransform languageSelectFocusFrame;

    [Header("Texts")]
    [SerializeField] private TMP_Text inputSelectGuideText;
    [SerializeField] private TMP_Text languageSelectGuideText;
    [SerializeField] private TMP_Text pressAnyButtonText;

    [Header("Font Assets")]
    [InspectorName("Korean TMP Font Asset")]
    [SerializeField] private TMP_FontAsset koreanTmpFontAsset;
    [InspectorName("English TMP Font Asset")]
    [SerializeField] private TMP_FontAsset englishTmpFontAsset;

    [Header("Input Select Texts")]
    [SerializeField] private TMP_Text inputSelectKoreanGuideText;
    [SerializeField] private TMP_Text inputSelectEnglishGuideText;

    [Header("Language Select Texts")]
    [SerializeField] private TMP_Text languageSelectKoreanGuideText;
    [SerializeField] private TMP_Text languageSelectEnglishGuideText;

    [Header("Title Intro Visuals")]
    [SerializeField] private CanvasGroup blackOverlay;
    [InspectorName("Light Overlay CanvasGroup")]
    [SerializeField] private CanvasGroup lightOverlay;
    [SerializeField] private CanvasGroup inputSelectPanelCanvasGroup;
    [SerializeField] private CanvasGroup languageSelectPanelCanvasGroup;
    [SerializeField] private CanvasGroup mainTitlePanelCanvasGroup;
    [SerializeField] private RectTransform logo;
    [SerializeField] private CanvasGroup logoCanvasGroup;
    [SerializeField] private CanvasGroup backgroundCanvasGroup;
    [InspectorName("Mascot Decoration RectTransform")]
    [SerializeField] private RectTransform mascotDecoration;
    [SerializeField] private CanvasGroup mascotDecorationCanvasGroup;
    [SerializeField] private CanvasGroup pressAnyButtonCanvasGroup;

    [Header("Title Logo")]
    [SerializeField] private Image logoImage;
    [SerializeField] private Sprite koreanLogoSprite;
    [SerializeField] private Sprite englishLogoSprite;

    [Header("Press Any Button Image")]
    [SerializeField] private Image pressAnyButtonImage;
    [SerializeField] private Sprite koreanPressAnyButtonSprite;
    [SerializeField] private Sprite englishPressAnyButtonSprite;

    [Header("Stage01 Title Image")]
    [InspectorName("Stage01 Title Image")]
    [SerializeField] private Image stage01TitleImage;
    [InspectorName("Korean Stage01 Title Sprite")]
    [SerializeField] private Sprite koreanStage01TitleSprite;
    [InspectorName("English Stage01 Title Sprite")]
    [SerializeField] private Sprite englishStage01TitleSprite;

    [Header("Stage02 Title")]
    [SerializeField] private Image stage02BackgroundImage;
    [SerializeField] private Sprite stage02BackgroundSprite;
    [SerializeField] private Image stage02LogoImage;
    [SerializeField] private Sprite koreanStage02LogoSprite;
    [SerializeField] private Sprite englishStage02LogoSprite;
    [SerializeField] private Stage02TitleGuideUI stage02TitleGuideUI;
    [Min(0f)] [SerializeField] private float stage02TitleFadeDuration = 0.4f;
    [Min(0f)] [SerializeField] private float interStageClosedCurtainDelay = 2f;
    [Min(0f)] [SerializeField] private float stage02TitleAfterBgmDelay = 2f;
    [Min(0f)] [SerializeField] private float stage02LoadAfterCurtainCloseDelay = 1f;
    [SerializeField] private bool allowStage02TitleManualSkip = true;

    [Header("Title BGM")]
    [SerializeField] private AudioSource titleBgmSource;
    [SerializeField] private AudioClip titleBgmClip;

    [Header("Title SFX")]
    [InspectorName("Title SFX Source")]
    [SerializeField] private AudioSource titleSfxSource;
    [InspectorName("Move Focus SFX")]
    [SerializeField] private AudioClip moveFocusSfx;
    [InspectorName("Confirm Select SFX")]
    [SerializeField] private AudioClip confirmSelectSfx;
    [InspectorName("Start Press SFX")]
    [SerializeField] private AudioClip startPressSfx;
    [InspectorName("Logo Land SFX")]
    [SerializeField] private AudioClip logoLandSfx;

    [Header("Title Intro Timing")]
    [SerializeField] private float introDelay = 0.35f;
    [SerializeField] private float panelPopDuration = 0.3f;
    [SerializeField] private float panelHideDuration = 0.2f;
    [SerializeField] private float logoDropDuration = 0.65f;
    [SerializeField] private float logoDropHeight = 500f;
    [SerializeField] private float delayAfterLogoDrop = 1f;
    [SerializeField] private float revealDuration = 0.25f;
    [SerializeField] private float flashInDuration = 0.04f;
    [SerializeField] private float flashHoldDuration = 0.05f;
    [SerializeField] private float flashOutDuration = 0.8f;
    [SerializeField] private float delayBeforeMascotRise = 1f;
    [SerializeField] private float mascotRiseDuration = 0.7f;
    [SerializeField] private float mascotRiseDistance = 350f;
    [InspectorName("Press Any Button Reveal Duration")]
    [SerializeField] private float pressAnyButtonRevealDuration = 0.3f;

    [Header("Focus")]
    [SerializeField] private float focusedScale = 1.1f;
    [SerializeField] private float normalScale = 1.0f;
    [SerializeField] private Vector2 focusPadding = new Vector2(40f, 40f);

    [Header("Title Start Tow Transition")]
    [InspectorName("Tow Mouse RectTransform")]
    [SerializeField] private RectTransform towMouse;
    [InspectorName("Tow Mouse CanvasGroup")]
    [SerializeField] private CanvasGroup towMouseCanvasGroup;
    [InspectorName("Tow Mouse AudioSource")]
    [SerializeField] private AudioSource towMouseAudioSource;
    [InspectorName("Tow Mouse Tractor SFX")]
    [SerializeField] private AudioClip towMouseTractorSfx;
    [InspectorName("Tow Push Target RectTransform")]
    [SerializeField] private RectTransform towPushTarget;
    [SerializeField] private float towMoveDuration = 3f;
    [SerializeField] private float towMoveDistanceX = 2600f;
    [SerializeField] private float towMouseStartOffsetX = -700f;

    [Header("Stage01 Title Audio")]
    [InspectorName("Stage01 Title BGM Source")]
    [SerializeField] private AudioSource stage01TitleBgmSource;
    [InspectorName("Stage01 Title BGM Clip")]
    [SerializeField] private AudioClip stage01TitleBgmClip;

    [Header("Stage01 Intro Dialogue")]
    [SerializeField] private TitleIntroDialogueController introDialogueController;
    [SerializeField] private bool playIntroDialogueBeforeStage01Title = true;
    [Min(0f)] [SerializeField] private float delayAfterIntroDialogueBeforeStage01Title = 1f;
    [Min(0f)] [SerializeField] private float stage01TitleFadeDuration = 0.4f;

    [Header("Stage01 Transition Timing")]
    [Min(0f)] [SerializeField] private float delayAfterStage01TitleBgm = 1f;

    [Header("Stage Start Curtain")]
    [InspectorName("Left Curtain RectTransform")]
    [SerializeField] private RectTransform leftCurtain;
    [InspectorName("Right Curtain RectTransform")]
    [SerializeField] private RectTransform rightCurtain;
    [SerializeField] private float curtainCloseDuration = 1f;
    [SerializeField] private float curtainWaitBeforeLoad = 1f;

    private TitleState state = TitleState.InputDeviceSelect;
    private SelectedInputMode selectedInputMode = SelectedInputMode.None;
    private SelectedLanguage selectedLanguage = SelectedLanguage.Korean;

    private int inputFocusIndex;
    private int languageFocusIndex;
    private bool inputLocked;
    private Vector2 logoRestPosition;
    private Vector2 mascotRestPosition;
    private Vector2 towMouseRestPosition;
    private Vector2 leftCurtainOpenPosition;
    private Vector2 rightCurtainOpenPosition;
    private float stage02TitleBgmExpectedDuration;
    private double stage02TitleBgmStartDspTime;
    private Vector3 xboxOptionBaseScale = Vector3.one;
    private Vector3 keyboardMouseOptionBaseScale = Vector3.one;
    private Vector3 koreanOptionBaseScale = Vector3.one;
    private Vector3 englishOptionBaseScale = Vector3.one;
    private TitleSession.TitleFlowMode startupFlowMode;

    private void Awake()
    {
        startupFlowMode = TitleSession.FlowMode;
        CaptureOptionBaseScales();

        SetActive(stage01Title, false);
        SetActive(stage02Title, false);

        if (startupFlowMode == TitleSession.TitleFlowMode.NormalTitle)
        {
            ShowInputSelect();
        }
        else
        {
            RestoreSelectionsFromSession();
            HideNormalTitlePanels();
        }

        PrepareIntroVisuals();
    }

    private void Start()
    {
        switch (startupFlowMode)
        {
            case TitleSession.TitleFlowMode.InterStageToStage02:
                StartCoroutine(ShowInterStageStage02TitleRoutine());
                break;

            case TitleSession.TitleFlowMode.PostStage02Intro:
                StartCoroutine(ShowPostStage02ClosedCurtainRoutine());
                break;

            default:
                StartCoroutine(ShowInitialInputPanelRoutine());
                break;
        }
    }

    private void Update()
    {
        if (inputLocked)
        {
            return;
        }

        switch (state)
        {
            case TitleState.InputDeviceSelect:
                UpdateInputDeviceSelect();
                break;

            case TitleState.LanguageSelect:
                UpdateLanguageSelect();
                break;

            case TitleState.MainTitle:
                UpdateMainTitle();
                break;
        }
    }

    private void UpdateInputDeviceSelect()
    {
        bool move = KeyboardActionA() || GamepadActionA();
        bool confirm = KeyboardActionB() || GamepadActionB();

        if (move)
        {
            inputFocusIndex = 1 - inputFocusIndex;
            RefreshInputFocus();
            PlayTitleSfx(moveFocusSfx);
        }

        if (confirm)
        {
            selectedInputMode = inputFocusIndex == 0
                ? SelectedInputMode.Xbox
                : SelectedInputMode.KeyboardMouse;
            TitleSession.SelectedInputMode = selectedInputMode == SelectedInputMode.Xbox
                ? TitleSession.InputMode.Xbox
                : TitleSession.InputMode.KeyboardMouse;

            StartCoroutine(ShowLanguageSelectRoutine());
        }
    }

    private void UpdateLanguageSelect()
    {
        bool move = SelectedActionA();
        bool confirm = SelectedActionB();

        if (move)
        {
            languageFocusIndex = 1 - languageFocusIndex;
            RefreshLanguageFocus();
            PlayTitleSfx(moveFocusSfx);
        }

        if (confirm)
        {
            selectedLanguage = languageFocusIndex == 0
                ? SelectedLanguage.Korean
                : SelectedLanguage.English;
            TitleSession.SelectedLanguage = selectedLanguage == SelectedLanguage.Korean
                ? TitleSession.Language.Korean
                : TitleSession.Language.English;

            StartCoroutine(ShowMainTitleRoutine());
        }
    }

    private void UpdateMainTitle()
    {
        if (SelectedAnyButton())
        {
            StartCoroutine(StartGameRoutine());
        }
    }

    private void ShowInputSelect()
    {
        state = TitleState.InputDeviceSelect;
        selectedInputMode = SelectedInputMode.None;
        TitleSession.SelectedInputMode = TitleSession.InputMode.None;
        TitleSession.SelectedLanguage = TitleSession.Language.Korean;
        inputFocusIndex = 0;

        SetActive(inputSelectPanel, true);
        SetActive(languageSelectPanel, false);
        SetActive(mainTitlePanel, false);

        RefreshInputSelectText();

        RefreshInputFocus();
    }

    private void RestoreSelectionsFromSession()
    {
        selectedInputMode = TitleSession.SelectedInputMode switch
        {
            TitleSession.InputMode.Xbox => SelectedInputMode.Xbox,
            TitleSession.InputMode.KeyboardMouse => SelectedInputMode.KeyboardMouse,
            _ => SelectedInputMode.None
        };

        selectedLanguage = TitleSession.SelectedLanguage == TitleSession.Language.English
            ? SelectedLanguage.English
            : SelectedLanguage.Korean;
    }

    private void HideNormalTitlePanels()
    {
        inputLocked = true;
        state = TitleState.StartingGame;
        SetActive(inputSelectPanel, false);
        SetActive(languageSelectPanel, false);
        SetActive(mainTitlePanel, false);
        SetActive(stage01Title, false);
        SetActive(stage02Title, false);
    }

    private IEnumerator ShowInterStageStage02TitleRoutine()
    {
        Debug.Log("[Title] InterStageToStage02 started", this);
        HideNormalTitlePanels();
        SetCanvasGroupAlpha(blackOverlay, 1f);
        SetCanvasGroupAlpha(lightOverlay, 0f);

        if (towMouse != null)
        {
            towMouse.gameObject.SetActive(false);
        }

        SetCanvasGroupAlpha(towMouseCanvasGroup, 0f);
        StopTowMouseAudio();
        Debug.Log("[Title] TowMouse skipped for Stage02 interstage", this);

        SetActive(leftCurtain != null ? leftCurtain.gameObject : null, true);
        SetActive(rightCurtain != null ? rightCurtain.gameObject : null, true);
        BringInterStageCurtainsToFront();
        PrepareInterStageAudioSources();

        yield return CloseCurtainsRoutine();
        yield return WaitUnscaled(interStageClosedCurtainDelay);
        yield return RevealStage02TitleRoutine();
        BringInterStageCurtainsToFront();
        yield return OpenCurtainsRoutine();
        Debug.Log("[Title] Stage02_Title shown", this);

        yield return null;
        yield return WaitForStage02TitleBgmOrManualSkip(stage02TitleBgmExpectedDuration);
        yield return WaitUnscaled(stage02TitleAfterBgmDelay);

        BringInterStageCurtainsToFront();
        yield return CloseCurtainsRoutine();
        yield return WaitUnscaled(stage02LoadAfterCurtainCloseDelay);

        TitleSession.FlowMode = TitleSession.TitleFlowMode.NormalTitle;
        Debug.Log("[Title] Loading Stage02", this);
        SceneManager.LoadScene(stage02SceneName);
    }

    private IEnumerator RevealStage02TitleRoutine()
    {
        if (stage02Title == null)
        {
            Debug.LogWarning(
                "[TitleManager] Stage02_Title is not connected. Run PeckBeat/Title/Build Stage02 Title UI.",
                this);
            SetCanvasGroupAlpha(blackOverlay, 0f);
            yield break;
        }

        CanvasGroup canvasGroup = stage02Title.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = stage02Title.AddComponent<CanvasGroup>();
        }

        stage02Title.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        ApplySelectedStage02TitleImages();
        stage02TitleGuideUI?.RefreshGuide();

        float duration = Mathf.Max(0f, stage02TitleFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, NormalizedTime(elapsed, duration));
            SetCanvasGroupAlpha(blackOverlay, 1f - progress);
            yield return null;
        }

        SetCanvasGroupAlpha(blackOverlay, 0f);
        PlayTitleSfx(logoLandSfx);
        stage02TitleBgmExpectedDuration = PlayStageTitleBgmForInterStage();
    }

    private IEnumerator WaitForStage02TitleBgmOrManualSkip(float expectedDuration)
    {
        float duration = Mathf.Max(0f, expectedDuration);
        double expectedEndDspTime = stage02TitleBgmStartDspTime + duration;

        while (AudioSettings.dspTime < expectedEndDspTime)
        {
            if (allowStage02TitleManualSkip && InterStageStartInputPressed())
            {
                Debug.Log("[Title] Stage02 start input detected", this);
                PlayTitleSfx(startPressSfx);
                stage01TitleBgmSource?.Stop();
                yield break;
            }

            yield return null;
        }
    }

    private void PrepareInterStageAudioSources()
    {
        if (titleBgmSource != null)
        {
            // This source is the normal title BGM (MP_Surfing dance in TitleScene).
            titleBgmSource.Stop();
        }

        titleSfxSource?.UnPause();
        stage01TitleBgmSource?.UnPause();
    }

    private void BringInterStageCurtainsToFront()
    {
        if (leftCurtain != null)
        {
            leftCurtain.SetAsLastSibling();
        }

        if (rightCurtain != null)
        {
            rightCurtain.SetAsLastSibling();
        }
    }

    private IEnumerator ShowPostStage02ClosedCurtainRoutine()
    {
        HideNormalTitlePanels();
        SetCanvasGroupAlpha(blackOverlay, 1f);
        SetCanvasGroupAlpha(lightOverlay, 0f);

        if (towMouse != null)
        {
            towMouse.gameObject.SetActive(false);
        }

        SetCanvasGroupAlpha(towMouseCanvasGroup, 0f);
        StopTowMouseAudio();
        PrepareInterStageAudioSources();

        SetActive(leftCurtain != null ? leftCurtain.gameObject : null, true);
        SetActive(rightCurtain != null ? rightCurtain.gameObject : null, true);
        BringInterStageCurtainsToFront();
        yield return CloseCurtainsRoutine();

        if (introDialogueController == null)
        {
            Debug.LogWarning(
                "[Title] PostStage02Intro cannot start because TitleIntroDialogueController is not connected.",
                this);
            yield break;
        }

        yield return introDialogueController.PlayPostStage02Outro();
    }

    private void PrepareIntroVisuals()
    {
        inputLocked = true;

        SetCanvasGroupAlpha(blackOverlay, 1f);
        SetCanvasGroupAlpha(lightOverlay, 0f);
        SetCanvasGroupAlpha(inputSelectPanelCanvasGroup, 0f);
        SetCanvasGroupAlpha(languageSelectPanelCanvasGroup, 0f);
        SetCanvasGroupAlpha(mainTitlePanelCanvasGroup, 1f);
        SetCanvasGroupAlpha(logoCanvasGroup, 0f);
        SetCanvasGroupAlpha(backgroundCanvasGroup, 0f);
        SetCanvasGroupAlpha(mascotDecorationCanvasGroup, 0f);
        SetPressAnyButtonAlpha(0f);

        if (inputSelectPanel != null)
        {
            inputSelectPanel.transform.localScale = Vector3.one * 0.85f;
        }

        if (logo != null)
        {
            logoRestPosition = logo.anchoredPosition;
        }

        if (mascotDecoration != null)
        {
            mascotRestPosition = mascotDecoration.anchoredPosition;
        }

        if (towMouse != null)
        {
            towMouseRestPosition = towMouse.anchoredPosition;
            towMouse.gameObject.SetActive(false);
        }

        if (leftCurtain != null)
        {
            leftCurtainOpenPosition = leftCurtain.anchoredPosition;
        }

        if (rightCurtain != null)
        {
            rightCurtainOpenPosition = rightCurtain.anchoredPosition;
        }

        SetCanvasGroupAlpha(towMouseCanvasGroup, 0f);

        if (lightOverlay != null)
        {
            lightOverlay.interactable = false;
            lightOverlay.blocksRaycasts = false;
        }

        if (titleBgmSource != null)
        {
            titleBgmSource.playOnAwake = false;
            titleBgmSource.Stop();
        }

        if (titleSfxSource != null)
        {
            titleSfxSource.playOnAwake = false;
            titleSfxSource.Stop();
        }

        if (towMouseAudioSource != null)
        {
            towMouseAudioSource.playOnAwake = false;
            towMouseAudioSource.Stop();
            towMouseAudioSource.volume = 1f;
        }

        if (stage01TitleBgmSource != null)
        {
            stage01TitleBgmSource.playOnAwake = false;
            stage01TitleBgmSource.loop = false;
            stage01TitleBgmSource.Stop();
        }
    }

    private IEnumerator ShowInitialInputPanelRoutine()
    {
        yield return WaitUnscaled(introDelay);
        yield return PopPanel(inputSelectPanel, inputSelectPanelCanvasGroup);
        inputLocked = false;
    }

    private IEnumerator ShowLanguageSelectRoutine()
    {
        inputLocked = true;
        float transitionStartTime = Time.unscaledTime;
        float confirmSfxDuration = PlayTitleSfx(confirmSelectSfx);
        yield return HidePanel(inputSelectPanel, inputSelectPanelCanvasGroup);
        yield return WaitForTransitionRemainder(transitionStartTime, confirmSfxDuration);

        ShowLanguageSelect();
        yield return PopPanel(languageSelectPanel, languageSelectPanelCanvasGroup);
        inputLocked = false;
    }

    private IEnumerator ShowMainTitleRoutine()
    {
        inputLocked = true;
        float transitionStartTime = Time.unscaledTime;
        float confirmSfxDuration = PlayTitleSfx(confirmSelectSfx);
        yield return HidePanel(languageSelectPanel, languageSelectPanelCanvasGroup);
        yield return WaitForTransitionRemainder(transitionStartTime, confirmSfxDuration);

        ShowMainTitle();
        PrepareMainTitleVisuals();
        PlayTitleSfx(logoLandSfx);
        yield return DropLogoRoutine();
        yield return WaitUnscaled(delayAfterLogoDrop);

        PlayTitleBgm();
        yield return RevealBackgroundRoutine();
        yield return WaitUnscaled(delayBeforeMascotRise);
        yield return RaiseMascotRoutine();
        yield return RevealPressAnyButtonRoutine();
        inputLocked = false;
    }

    private void ShowLanguageSelect()
    {
        state = TitleState.LanguageSelect;
        languageFocusIndex = 0;

        SetActive(inputSelectPanel, false);
        SetActive(languageSelectPanel, true);
        SetActive(mainTitlePanel, false);

        RefreshLanguageText();
        RefreshLanguageFocus();
    }

    private void ShowMainTitle()
    {
        state = TitleState.MainTitle;
        ApplySelectedLogo();
        ApplySelectedPressAnyButtonImage();
        ApplySelectedStage01TitleImage();

        SetActive(inputSelectPanel, false);
        SetActive(languageSelectPanel, false);
        SetActive(mainTitlePanel, true);

        if (pressAnyButtonImage != null)
        {
            if (pressAnyButtonText != null)
            {
                pressAnyButtonText.enabled = false;
            }
        }
        else if (pressAnyButtonText != null)
        {
            pressAnyButtonText.enabled = true;
            bool koreanSelected = selectedLanguage == SelectedLanguage.Korean;
            pressAnyButtonText.text = koreanSelected
                ? "아무 버튼이나 눌러 시작"
                : "Press Any Button to Start";
            ApplyFont(
                pressAnyButtonText,
                koreanSelected ? koreanTmpFontAsset : englishTmpFontAsset);
        }
    }

    private void PrepareMainTitleVisuals()
    {
        SetCanvasGroupAlpha(mainTitlePanelCanvasGroup, 1f);
        SetCanvasGroupAlpha(blackOverlay, 1f);
        SetCanvasGroupAlpha(lightOverlay, 0f);
        SetCanvasGroupAlpha(logoCanvasGroup, 1f);
        SetCanvasGroupAlpha(backgroundCanvasGroup, 0f);
        SetCanvasGroupAlpha(mascotDecorationCanvasGroup, 0f);
        SetPressAnyButtonAlpha(0f);

        if (logo != null)
        {
            logo.anchoredPosition = logoRestPosition + Vector2.up * logoDropHeight;
        }


        if (mascotDecoration != null)
        {
            mascotDecoration.anchoredPosition = mascotRestPosition + Vector2.down * mascotRiseDistance;
        }
    }

    private IEnumerator DropLogoRoutine()
    {
        if (logo == null)
        {
            yield break;
        }

        Vector2 start = logoRestPosition + Vector2.up * logoDropHeight;
        float duration = Mathf.Max(0f, logoDropDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            logo.anchoredPosition = Vector2.LerpUnclamped(start, logoRestPosition, EaseOutBack(t));
            yield return null;
        }

        logo.anchoredPosition = logoRestPosition;
    }

    private IEnumerator RevealBackgroundRoutine()
    {
        float revealTime = Mathf.Max(0f, revealDuration);
        float flashInTime = Mathf.Max(0f, flashInDuration);
        float flashHoldTime = Mathf.Max(0f, flashHoldDuration);
        float flashOutTime = Mathf.Max(0f, flashOutDuration);
        float flashTime = flashInTime + flashHoldTime + flashOutTime;
        float duration = Mathf.Max(revealTime, flashTime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float revealT = Mathf.SmoothStep(0f, 1f, NormalizedTime(elapsed, revealTime));

            SetCanvasGroupAlpha(blackOverlay, 1f - revealT);
            SetCanvasGroupAlpha(backgroundCanvasGroup, revealT);
            SetCanvasGroupAlpha(
                lightOverlay,
                EvaluateFlashAlpha(elapsed, flashInTime, flashHoldTime, flashOutTime));

            yield return null;
        }

        SetCanvasGroupAlpha(blackOverlay, 0f);
        SetCanvasGroupAlpha(lightOverlay, 0f);
        SetCanvasGroupAlpha(backgroundCanvasGroup, 1f);
    }

    private IEnumerator RaiseMascotRoutine()
    {
        float duration = Mathf.Max(0f, mascotRiseDuration);
        Vector2 startPosition = mascotRestPosition + Vector2.down * mascotRiseDistance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, NormalizedTime(elapsed, duration));
            SetCanvasGroupAlpha(mascotDecorationCanvasGroup, t);

            if (mascotDecoration != null)
            {
                mascotDecoration.anchoredPosition = Vector2.LerpUnclamped(
                    startPosition,
                    mascotRestPosition,
                    t);
            }

            yield return null;
        }

        SetCanvasGroupAlpha(mascotDecorationCanvasGroup, 1f);

        if (mascotDecoration != null)
        {
            mascotDecoration.anchoredPosition = mascotRestPosition;
        }
    }

    private IEnumerator RevealPressAnyButtonRoutine()
    {
        float duration = Mathf.Max(0f, pressAnyButtonRevealDuration);
        float elapsed = 0f;

        SetPressAnyButtonAlpha(0f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, NormalizedTime(elapsed, duration));
            SetPressAnyButtonAlpha(t);
            yield return null;
        }

        SetPressAnyButtonAlpha(1f);
    }

    private void PlayTitleBgm()
    {
        if (titleBgmSource == null)
        {
            return;
        }

        if (titleBgmClip != null)
        {
            titleBgmSource.clip = titleBgmClip;
        }

        if (titleBgmSource.clip != null)
        {
            titleBgmSource.Play();
        }
    }

    private float PlayTitleSfx(AudioClip clip)
    {
        if (clip == null)
        {
            return 0f;
        }

        float playbackSpeed = 1f;
        if (titleSfxSource != null)
        {
            titleSfxSource.Stop();
            titleSfxSource.PlayOneShot(clip);
            playbackSpeed = Mathf.Max(0.01f, Mathf.Abs(titleSfxSource.pitch));
        }

        return clip.length / playbackSpeed;
    }

    private IEnumerator PopPanel(GameObject panel, CanvasGroup canvasGroup)
    {
        SetActive(panel, true);
        SetCanvasGroupAlpha(canvasGroup, 0f);

        Transform panelTransform = panel != null ? panel.transform : null;
        if (panelTransform != null)
        {
            panelTransform.localScale = Vector3.one * 0.85f;
        }

        float duration = Mathf.Max(0f, panelPopDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetCanvasGroupAlpha(canvasGroup, Mathf.SmoothStep(0f, 1f, t));

            if (panelTransform != null)
            {
                panelTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(t));
            }

            yield return null;
        }

        SetCanvasGroupAlpha(canvasGroup, 1f);
        if (panelTransform != null)
        {
            panelTransform.localScale = Vector3.one;
        }
    }

    private IEnumerator HidePanel(GameObject panel, CanvasGroup canvasGroup)
    {
        Transform panelTransform = panel != null ? panel.transform : null;
        Vector3 startScale = panelTransform != null ? panelTransform.localScale : Vector3.one;
        float duration = Mathf.Max(0f, panelHideDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetCanvasGroupAlpha(canvasGroup, 1f - t);

            if (panelTransform != null)
            {
                panelTransform.localScale = Vector3.LerpUnclamped(startScale, Vector3.one * 0.75f, t);
            }

            yield return null;
        }

        SetCanvasGroupAlpha(canvasGroup, 0f);
        SetActive(panel, false);
    }

    private IEnumerator StartGameRoutine()
    {
        bool shouldPlayIntroDialogue =
            playIntroDialogueBeforeStage01Title && introDialogueController != null;

        state = TitleState.StartingGame;
        inputLocked = true;
        float startPressDuration = PlayTitleSfx(startPressSfx);
        yield return WaitUnscaled(startPressDuration);

        SetActive(inputSelectPanel, false);
        SetActive(languageSelectPanel, false);
        SetActive(mainTitlePanel, true);

        yield return TowMouseTransitionRoutine();
        if (titleBgmSource != null)
        {
            titleBgmSource.Stop();
        }

        if (shouldPlayIntroDialogue)
        {
            yield return introDialogueController.PlayIntroDialogue();
            yield return WaitUnscaled(delayAfterIntroDialogueBeforeStage01Title);
        }

        yield return RevealStage01TitleRoutine();

        if (shouldPlayIntroDialogue)
        {
            introDialogueController.ReleaseIntroBackground();
        }
        yield return PlayAndWaitForStage01TitleBgm();
        yield return WaitUnscaled(delayAfterStage01TitleBgm);
        yield return CloseCurtainsRoutine();
        yield return WaitUnscaled(curtainWaitBeforeLoad);
        SceneManager.LoadScene(stage01SceneName);
    }

    private IEnumerator RevealStage01TitleRoutine()
    {
        if (stage01Title == null)
        {
            yield break;
        }

        CanvasGroup canvasGroup = stage01Title.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = stage01Title.AddComponent<CanvasGroup>();
        }

        stage01Title.SetActive(true);
        canvasGroup.alpha = 0f;

        float duration = Mathf.Max(0f, stage01TitleFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, NormalizedTime(elapsed, duration));
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator TowMouseTransitionRoutine()
    {
        RectTransform pushTarget = towPushTarget != null ? towPushTarget : titleRoot;
        Vector2 pushStart = pushTarget != null ? pushTarget.anchoredPosition : Vector2.zero;
        Vector2 pushEnd = pushStart + Vector2.right * towMoveDistanceX;
        Vector2 mouseStart = towMouseRestPosition + Vector2.right * towMouseStartOffsetX;
        Vector2 mouseEnd = mouseStart + Vector2.right * towMoveDistanceX;

        if (towMouse != null)
        {
            towMouse.anchoredPosition = mouseStart;
        if (startupFlowMode == TitleSession.TitleFlowMode.NormalTitle
            && playIntroDialogueBeforeStage01Title
            && introDialogueController != null)
        {
            // This runs after the Start Press SFX wait and immediately before the tow begins.
            introDialogueController.PrepareIntroBackgroundForTow();
        }

        towMouse.gameObject.SetActive(true);
        }

        SetCanvasGroupAlpha(towMouseCanvasGroup, 1f);
        StartTowMouseAudio();

        float duration = Mathf.Max(0f, towMoveDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, NormalizedTime(elapsed, duration));

            if (towMouse != null)
            {
                towMouse.anchoredPosition = Vector2.LerpUnclamped(mouseStart, mouseEnd, progress);
            }

            if (pushTarget != null)
            {
                pushTarget.anchoredPosition = Vector2.LerpUnclamped(pushStart, pushEnd, progress);
            }

            if (towMouseAudioSource != null)
            {
                towMouseAudioSource.volume = 1f - progress;
            }

            yield return null;
        }

        if (towMouse != null)
        {
            towMouse.anchoredPosition = mouseEnd;
        }

        if (pushTarget != null)
        {
            pushTarget.anchoredPosition = pushEnd;
        }

        StopTowMouseAudio();
    }

    private IEnumerator PlayAndWaitForStage01TitleBgm()
    {
        if (stage01TitleBgmSource == null)
        {
            yield break;
        }

        stage01TitleBgmSource.Stop();
        stage01TitleBgmSource.playOnAwake = false;
        stage01TitleBgmSource.loop = false;

        if (stage01TitleBgmClip != null)
        {
            stage01TitleBgmSource.clip = stage01TitleBgmClip;
        }

        if (stage01TitleBgmSource.clip == null)
        {
            yield break;
        }

        stage01TitleBgmSource.Play();
        while (stage01TitleBgmSource.isPlaying)
        {
            yield return null;
        }
    }

    private float PlayStageTitleBgmForInterStage()
    {
        if (stage01TitleBgmSource == null)
        {
            Debug.LogWarning(
                "[TitleManager] Stage01 Title BGM Source is not connected for Stage02_Title.",
                this);
            return 0f;
        }

        stage01TitleBgmSource.Stop();
        stage01TitleBgmSource.playOnAwake = false;
        stage01TitleBgmSource.loop = false;
        stage01TitleBgmSource.UnPause();

        if (stage01TitleBgmClip != null)
        {
            stage01TitleBgmSource.clip = stage01TitleBgmClip;
        }

        if (stage01TitleBgmSource.clip == null)
        {
            Debug.LogWarning(
                "[TitleManager] Stage01 Title BGM Clip is not connected for Stage02_Title.",
                this);
            return 0f;
        }

        stage02TitleBgmStartDspTime = AudioSettings.dspTime;
        stage01TitleBgmSource.Play();
        Debug.Log("[Title] Stage02_Title BGM started", this);
        return GetAudioPlaybackDuration(stage01TitleBgmSource);
    }

    private static float GetAudioPlaybackDuration(AudioSource source)
    {
        if (source == null || source.clip == null)
        {
            return 0f;
        }

        float playbackSpeed = Mathf.Abs(source.pitch);
        if (playbackSpeed <= 0.01f)
        {
            return Mathf.Max(0f, source.clip.length);
        }

        return Mathf.Max(0f, source.clip.length / playbackSpeed);
    }

    private IEnumerator CloseCurtainsRoutine()
    {
        if (leftCurtain == null && rightCurtain == null)
        {
            yield break;
        }

        Vector2 leftClosedPosition = leftCurtainOpenPosition;
        Vector2 rightClosedPosition = rightCurtainOpenPosition;

        if (leftCurtain != null)
        {
            leftCurtain.gameObject.SetActive(true);
            leftCurtain.anchoredPosition = leftCurtainOpenPosition;
            leftClosedPosition += Vector2.right * leftCurtain.rect.width;
        }

        if (rightCurtain != null)
        {
            rightCurtain.gameObject.SetActive(true);
            rightCurtain.anchoredPosition = rightCurtainOpenPosition;
            rightClosedPosition += Vector2.left * rightCurtain.rect.width;
        }

        float duration = Mathf.Max(0f, curtainCloseDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, NormalizedTime(elapsed, duration));

            if (leftCurtain != null)
            {
                leftCurtain.anchoredPosition = Vector2.LerpUnclamped(
                    leftCurtainOpenPosition,
                    leftClosedPosition,
                    progress);
            }

            if (rightCurtain != null)
            {
                rightCurtain.anchoredPosition = Vector2.LerpUnclamped(
                    rightCurtainOpenPosition,
                    rightClosedPosition,
                    progress);
            }

            yield return null;
        }

        if (leftCurtain != null)
        {
            leftCurtain.anchoredPosition = leftClosedPosition;
        }

        if (rightCurtain != null)
        {
            rightCurtain.anchoredPosition = rightClosedPosition;
        }
    }

    private IEnumerator OpenCurtainsRoutine()
    {
        if (leftCurtain == null && rightCurtain == null)
        {
            yield break;
        }

        Vector2 leftClosedPosition = leftCurtainOpenPosition;
        Vector2 rightClosedPosition = rightCurtainOpenPosition;

        if (leftCurtain != null)
        {
            leftCurtain.gameObject.SetActive(true);
            leftClosedPosition += Vector2.right * leftCurtain.rect.width;
            leftCurtain.anchoredPosition = leftClosedPosition;
        }

        if (rightCurtain != null)
        {
            rightCurtain.gameObject.SetActive(true);
            rightClosedPosition += Vector2.left * rightCurtain.rect.width;
            rightCurtain.anchoredPosition = rightClosedPosition;
        }

        float duration = Mathf.Max(0f, curtainCloseDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, NormalizedTime(elapsed, duration));

            if (leftCurtain != null)
            {
                leftCurtain.anchoredPosition = Vector2.LerpUnclamped(
                    leftClosedPosition,
                    leftCurtainOpenPosition,
                    progress);
            }

            if (rightCurtain != null)
            {
                rightCurtain.anchoredPosition = Vector2.LerpUnclamped(
                    rightClosedPosition,
                    rightCurtainOpenPosition,
                    progress);
            }

            yield return null;
        }

        if (leftCurtain != null)
        {
            leftCurtain.anchoredPosition = leftCurtainOpenPosition;
        }

        if (rightCurtain != null)
        {
            rightCurtain.anchoredPosition = rightCurtainOpenPosition;
        }
    }

    private void StartTowMouseAudio()
    {
        if (towMouseAudioSource == null)
        {
            return;
        }

        towMouseAudioSource.Stop();
        towMouseAudioSource.volume = 1f;

        if (towMouseTractorSfx != null)
        {
            towMouseAudioSource.clip = towMouseTractorSfx;
        }

        if (towMouseAudioSource.clip != null)
        {
            towMouseAudioSource.Play();
        }
    }

    private void StopTowMouseAudio()
    {
        if (towMouseAudioSource == null)
        {
            return;
        }

        towMouseAudioSource.volume = 0f;
        towMouseAudioSource.Stop();
        towMouseAudioSource.volume = 1f;
    }

    private void RefreshInputFocus()
    {
        ApplyFocus(xboxOption, inputFocusIndex == 0);
        ApplyFocus(keyboardMouseOption, inputFocusIndex == 1);

        RectTransform target = inputFocusIndex == 0 ? xboxOption : keyboardMouseOption;
        MoveFocusFrame(inputSelectFocusFrame, target);
    }

    private void RefreshLanguageFocus()
    {
        ApplyFocus(koreanOption, languageFocusIndex == 0);
        ApplyFocus(englishOption, languageFocusIndex == 1);

        RectTransform target = languageFocusIndex == 0 ? koreanOption : englishOption;
        MoveFocusFrame(languageSelectFocusFrame, target);
    }

    private void RefreshInputSelectText()
    {
        TMP_Text koreanText = inputSelectKoreanGuideText != null
            ? inputSelectKoreanGuideText
            : inputSelectGuideText;

        SetGuideText(
            koreanText,
            "조작 방식을 선택하세요\nA: 이동 / B: 선택",
            koreanTmpFontAsset);
        SetGuideText(
            inputSelectEnglishGuideText,
            "Select Input Method\nA: Move / B: Select",
            englishTmpFontAsset);
    }

    private void RefreshLanguageText()
    {
        bool keyboardSelected = selectedInputMode == SelectedInputMode.KeyboardMouse;
        string koreanControls = keyboardSelected ? "D: 이동 / J: 선택" : "A: 이동 / B: 선택";
        string englishControls = keyboardSelected ? "D: Move / J: Select" : "A: Move / B: Select";
        TMP_Text koreanText = languageSelectKoreanGuideText != null
            ? languageSelectKoreanGuideText
            : languageSelectGuideText;

        SetGuideText(
            koreanText,
            $"언어를 선택하세요\n{koreanControls}",
            koreanTmpFontAsset);
        SetGuideText(
            languageSelectEnglishGuideText,
            $"Select Language\n{englishControls}",
            englishTmpFontAsset);
    }

    private void CaptureOptionBaseScales()
    {
        xboxOptionBaseScale = GetLocalScaleOrOne(xboxOption);
        keyboardMouseOptionBaseScale = GetLocalScaleOrOne(keyboardMouseOption);
        koreanOptionBaseScale = GetLocalScaleOrOne(koreanOption);
        englishOptionBaseScale = GetLocalScaleOrOne(englishOption);
    }

    private void ApplyFocus(RectTransform target, bool focused)
    {
        if (target == null)
        {
            return;
        }

        float scaleMultiplier = focused ? focusedScale : normalScale;
        target.localScale = GetOptionBaseScale(target) * scaleMultiplier;
    }

    private Vector3 GetOptionBaseScale(RectTransform target)
    {
        if (target == xboxOption)
        {
            return xboxOptionBaseScale;
        }

        if (target == keyboardMouseOption)
        {
            return keyboardMouseOptionBaseScale;
        }

        if (target == koreanOption)
        {
            return koreanOptionBaseScale;
        }

        return target == englishOption ? englishOptionBaseScale : target.localScale;
    }

    private void ApplySelectedLogo()
    {
        if (logoImage == null)
        {
            return;
        }

        Sprite selectedSprite = selectedLanguage == SelectedLanguage.Korean
            ? koreanLogoSprite
            : englishLogoSprite;

        if (selectedSprite == null)
        {
            return;
        }

        logoImage.sprite = selectedSprite;
        logoImage.preserveAspect = true;
    }

    private void ApplySelectedPressAnyButtonImage()
    {
        if (pressAnyButtonImage == null)
        {
            return;
        }

        Sprite selectedSprite = selectedLanguage == SelectedLanguage.Korean
            ? koreanPressAnyButtonSprite
            : englishPressAnyButtonSprite;

        ApplySpriteIfAssigned(pressAnyButtonImage, selectedSprite);
    }

    private void ApplySelectedStage01TitleImage()
    {
        if (stage01TitleImage == null)
        {
            return;
        }

        Sprite selectedSprite = selectedLanguage == SelectedLanguage.Korean
            ? koreanStage01TitleSprite
            : englishStage01TitleSprite;

        ApplySpriteIfAssigned(stage01TitleImage, selectedSprite);
    }

    private void ApplySelectedStage02TitleImages()
    {
        PrepareStage02TitleImage(
            stage02BackgroundImage,
            stage02BackgroundSprite,
            "Stage02Background");

        Sprite selectedLogo = selectedLanguage == SelectedLanguage.Korean
            ? koreanStage02LogoSprite
            : englishStage02LogoSprite;
        PrepareStage02TitleImage(stage02LogoImage, selectedLogo, "Stage02Logo");
    }

    private void PrepareStage02TitleImage(Image target, Sprite configuredSprite, string label)
    {
        if (target == null)
        {
            Debug.LogWarning($"[Title] {label} Image reference is missing.", this);
            return;
        }

        target.gameObject.SetActive(true);
        if (configuredSprite != null)
        {
            target.sprite = configuredSprite;
        }

        if (target.sprite == null)
        {
            target.enabled = false;
            Debug.LogWarning($"[Title] {label} Sprite is not assigned.", this);
            return;
        }

        target.enabled = true;
        target.preserveAspect = true;
        Color color = target.color;
        color.a = 1f;
        target.color = color;
    }

    private void MoveFocusFrame(RectTransform frame, RectTransform target)
    {
        if (frame == null || target == null)
        {
            return;
        }

        frame.gameObject.SetActive(true);
        frame.position = target.position;
        frame.sizeDelta = target.rect.size + focusPadding;
    }

    private bool SelectedActionA()
    {
        return selectedInputMode switch
        {
            SelectedInputMode.Xbox => GamepadActionA(),
            SelectedInputMode.KeyboardMouse => KeyboardActionA(),
            _ => false
        };
    }

    private bool SelectedActionB()
    {
        return selectedInputMode switch
        {
            SelectedInputMode.Xbox => GamepadActionB(),
            SelectedInputMode.KeyboardMouse => KeyboardActionB(),
            _ => false
        };
    }

    private bool SelectedAnyButton()
    {
        return selectedInputMode switch
        {
            SelectedInputMode.Xbox => GamepadAnyButton(),
            SelectedInputMode.KeyboardMouse => KeyboardAnyButton(),
            _ => false
        };
    }

    private static bool InterStageStartInputPressed()
    {
        bool keyboardPressed = InterStageKeyboardStartPressed();
        bool gamepadPressed = InterStageGamepadStartPressed();

        bool preferredInputPressed = TitleSession.SelectedInputMode switch
        {
            TitleSession.InputMode.Xbox => gamepadPressed,
            TitleSession.InputMode.KeyboardMouse => keyboardPressed,
            _ => keyboardPressed || gamepadPressed
        };

        // Inter-stage screens also accept the supported backup device so a stale
        // session selection cannot leave the player stuck on Stage02_Title.
        return preferredInputPressed || keyboardPressed || gamepadPressed;
    }

    private static bool InterStageKeyboardStartPressed()
    {
        Keyboard keyboard = Keyboard.current;
        // Logical mapping: keyboard D = Xbox A, keyboard J = Xbox B.
        return keyboard != null &&
               (keyboard.dKey.wasPressedThisFrame ||
                keyboard.jKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame ||
                keyboard.spaceKey.wasPressedThisFrame);
    }

    private static bool InterStageGamepadStartPressed()
    {
        Gamepad gamepad = Gamepad.current;
        // Xbox A/B and Menu(Start) are all valid Stage02_Title start inputs.
        return gamepad != null &&
               (gamepad.buttonSouth.wasPressedThisFrame ||
                gamepad.buttonEast.wasPressedThisFrame ||
                gamepad.startButton.wasPressedThisFrame);
    }

    private static bool KeyboardActionA()
    {
        return Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame;
    }

    private static bool KeyboardActionB()
    {
        return Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame;
    }

    private static bool KeyboardAnyButton()
    {
        return Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
    }

    private static bool GamepadActionA()
    {
        return Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
    }

    private static bool GamepadActionB()
    {
        return Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
    }

    private static bool GamepadAnyButton()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null)
        {
            return false;
        }

        foreach (InputControl control in gamepad.allControls)
        {
            if (control is ButtonControl button && button.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        float duration = Mathf.Max(0f, seconds);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static IEnumerator WaitForTransitionRemainder(float startTime, float requiredDuration)
    {
        float remaining = requiredDuration - (Time.unscaledTime - startTime);
        if (remaining > 0f)
        {
            yield return WaitUnscaled(remaining);
        }
    }

    private static float NormalizedTime(float elapsed, float duration)
    {
        return duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
    }

    private static float EvaluateFlashAlpha(
        float elapsed,
        float flashInTime,
        float flashHoldTime,
        float flashOutTime)
    {
        if (flashInTime > 0f && elapsed < flashInTime)
        {
            return Mathf.SmoothStep(0f, 1f, elapsed / flashInTime);
        }

        float fadeOutStart = flashInTime + flashHoldTime;
        if (elapsed < fadeOutStart)
        {
            return 1f;
        }

        if (flashOutTime <= 0f)
        {
            return 0f;
        }

        float fadeOutElapsed = Mathf.Max(0f, elapsed - fadeOutStart);
        return Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(fadeOutElapsed / flashOutTime));
    }

    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.70158f;
        float shifted = t - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted
            + overshoot * shifted * shifted;
    }

    private void SetPressAnyButtonAlpha(float alpha)
    {
        float clampedAlpha = Mathf.Clamp01(alpha);

        // Keep the group itself visible and drive the actual Image/TMP graphics.
        // This also works when the assigned CanvasGroup is not their direct parent.
        SetCanvasGroupAlpha(pressAnyButtonCanvasGroup, 1f);
        SetGraphicAlpha(pressAnyButtonImage, clampedAlpha);
        SetGraphicAlpha(pressAnyButtonText, clampedAlpha);
    }

    private static void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }

    private static void SetCanvasGroupAlpha(CanvasGroup canvasGroup, float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }

    private static Vector3 GetLocalScaleOrOne(RectTransform target)
    {
        return target != null ? target.localScale : Vector3.one;
    }

    private static void SetGuideText(TMP_Text target, string value, TMP_FontAsset fontAsset)
    {
        if (target == null)
        {
            return;
        }

        target.gameObject.SetActive(true);
        target.text = value;
        ApplyFont(target, fontAsset);
    }

    private static void ApplyFont(TMP_Text target, TMP_FontAsset fontAsset)
    {
        if (target != null && fontAsset != null)
        {
            target.font = fontAsset;
        }
    }

    private static void ApplySpriteIfAssigned(Image target, Sprite sprite)
    {
        if (target == null || sprite == null)
        {
            return;
        }

        target.sprite = sprite;
        target.preserveAspect = true;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
