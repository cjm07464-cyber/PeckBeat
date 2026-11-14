using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Stage02InstructionUI : MonoBehaviour
{
    [Header("Guide Texts")]
    [SerializeField] private TMP_Text leftGuideText;
    [SerializeField] private TMP_Text middleGuideText;
    [SerializeField] private TMP_Text rightGuideText;

    [Header("Button Images")]
    [SerializeField] private Image leftButtonImage;
    [SerializeField] private Image middleButtonImage;
    [SerializeField] private Image rightButtonImage;

    [Header("Log Images")]
    [SerializeField] private Image leftLogImage;
    [SerializeField] private Image middleLogImage;
    [SerializeField] private Image rightLogImage;

    [Header("Action A Button Sprites")]
    [SerializeField] private Sprite keyboardDButtonSprite;
    [SerializeField] private Sprite gamepadAButtonSprite;

    [Header("Action B Button Sprites")]
    [SerializeField] private Sprite keyboardJButtonSprite;
    [SerializeField] private Sprite gamepadBButtonSprite;

    [Header("Stage 02 Log Sprites")]
    [SerializeField] private Sprite oneHitLogSprite;
    [SerializeField] private Sprite twoHitLogSprite;
    [SerializeField] private Sprite holdLongLogSprite;

    [Header("Font Assets")]
    [SerializeField] private TMP_FontAsset koreanTmpFontAsset;
    [SerializeField] private TMP_FontAsset englishTmpFontAsset;

    [Header("Timed Hide")]
    [Min(0f)] [SerializeField] private float instructionVisibleDuration = 28f;
    [Min(0f)] [SerializeField] private float instructionSlideUpDuration = 0.6f;
    [Min(0f)] [SerializeField] private float instructionHideMargin = 100f;

    private PeckBeatRhythmManager rhythmManager;
    private RectTransform instructionRoot;
    private Vector2 initialAnchoredPosition;
    private Coroutine slideUpRoutine;
    private bool initialPositionCached;
    private bool hideStarted;

    private void Awake()
    {
        ResolveRuntimeReferences();
        CacheInitialPosition();
        RefreshGuide();
    }

    private void OnEnable()
    {
        ResolveRuntimeReferences();
        CacheInitialPosition();
        ResetInstructionPresentation();
        RefreshGuide();
    }

    private void Update()
    {
        if (hideStarted)
        {
            return;
        }

        if (rhythmManager == null)
        {
            ResolveRuntimeReferences();
        }

        if (rhythmManager == null || !rhythmManager.IsSongPlaying)
        {
            return;
        }

        if (rhythmManager.PlaybackSongTime >= Mathf.Max(0f, instructionVisibleDuration))
        {
            hideStarted = true;
            slideUpRoutine = StartCoroutine(SlideUpAndHideRoutine());
        }
    }

    private void OnDisable()
    {
        slideUpRoutine = null;
    }

    public void RefreshGuide()
    {
        bool useGamepad = TitleSession.SelectedInputMode == TitleSession.InputMode.Xbox;
        bool useEnglish = TitleSession.SelectedLanguage == TitleSession.Language.English;

        Sprite actionASprite = useGamepad ? gamepadAButtonSprite : keyboardDButtonSprite;
        ApplySpriteIfAssigned(leftButtonImage, actionASprite);
        ApplySpriteIfAssigned(middleButtonImage, actionASprite);
        ApplySpriteIfAssigned(
            rightButtonImage,
            useGamepad ? gamepadBButtonSprite : keyboardJButtonSprite);

        ApplySpriteIfAssigned(leftLogImage, oneHitLogSprite);
        ApplySpriteIfAssigned(middleLogImage, twoHitLogSprite);
        ApplySpriteIfAssigned(rightLogImage, holdLongLogSprite);

        TMP_FontAsset selectedFont = useEnglish
            ? englishTmpFontAsset
            : koreanTmpFontAsset;
        ApplyText(leftGuideText, useEnglish ? "Once!" : "1번!", selectedFont);
        ApplyText(middleGuideText, useEnglish ? "Twice!" : "2번!", selectedFont);
        ApplyText(rightGuideText, useEnglish ? "Hold!" : "꾹~ 눌러!", selectedFont);
    }

    private IEnumerator SlideUpAndHideRoutine()
    {
        if (instructionRoot == null)
        {
            yield break;
        }

        Vector2 startPosition = instructionRoot.anchoredPosition;
        float hideDistance = CalculateRequiredHideDistance();
        Vector2 endPosition = startPosition + Vector2.up * hideDistance;
        float duration = Mathf.Max(0f, instructionSlideUpDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (IsPresentationPaused())
            {
                yield return null;
                continue;
            }

            elapsed += Time.unscaledDeltaTime;
            float progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            instructionRoot.anchoredPosition = Vector2.LerpUnclamped(
                startPosition,
                endPosition,
                easedProgress);
            yield return null;
        }

        instructionRoot.anchoredPosition = endPosition;
        slideUpRoutine = null;
        instructionRoot.gameObject.SetActive(false);
    }

    private float CalculateRequiredHideDistance()
    {
        float margin = Mathf.Max(0f, instructionHideMargin);
        Canvas canvas = instructionRoot != null
            ? instructionRoot.GetComponentInParent<Canvas>()
            : null;
        RectTransform canvasRect = canvas != null
            ? canvas.rootCanvas.transform as RectTransform
            : null;

        if (canvasRect != null)
        {
            Bounds contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                canvasRect,
                instructionRoot);
            float distanceToTop = canvasRect.rect.yMax - contentBounds.min.y;
            return Mathf.Max(margin, distanceToTop + margin);
        }

        Bounds localBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
            instructionRoot);
        return Mathf.Max(instructionRoot.rect.height, localBounds.size.y) + margin;
    }

    private bool IsPresentationPaused()
    {
        return StagePauseMenuController.IsGameplayInputBlocked
            || (rhythmManager != null && rhythmManager.IsPlaybackPaused);
    }

    private void ResolveRuntimeReferences()
    {
        instructionRoot ??= transform as RectTransform;
        if (rhythmManager == null)
        {
            rhythmManager = Object.FindFirstObjectByType<PeckBeatRhythmManager>(
                FindObjectsInactive.Include);
        }
    }

    private void CacheInitialPosition()
    {
        if (instructionRoot == null || initialPositionCached)
        {
            return;
        }

        initialAnchoredPosition = instructionRoot.anchoredPosition;
        initialPositionCached = true;
    }

    private void ResetInstructionPresentation()
    {
        if (slideUpRoutine != null)
        {
            StopCoroutine(slideUpRoutine);
            slideUpRoutine = null;
        }

        hideStarted = false;
        if (instructionRoot != null && initialPositionCached)
        {
            instructionRoot.anchoredPosition = initialAnchoredPosition;
        }
    }

    private static void ApplySpriteIfAssigned(Image target, Sprite sprite)
    {
        if (target == null || sprite == null)
        {
            return;
        }

        target.sprite = sprite;
        target.enabled = true;
        target.preserveAspect = true;
    }

    private static void ApplyText(TMP_Text target, string value, TMP_FontAsset fontAsset)
    {
        if (target == null)
        {
            return;
        }

        target.text = value;
        if (fontAsset != null)
        {
            target.font = fontAsset;
        }
    }
}
