using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Stage01TitleGuideUI : MonoBehaviour
{
    private enum HoldInputSource
    {
        None,
        KeyboardD,
        KeyboardJ,
        GamepadA,
        GamepadB
    }

    [Header("Guide References")]
    [SerializeField] private GameObject guideRoot;
    [SerializeField] private Image guidePanelImage;
    [SerializeField] private TMP_Text guideTitleText;
    [SerializeField] private Image leftTargetImage;
    [SerializeField] private TMP_Text leftHeadingText;
    [SerializeField] private Image leftButtonImage;
    [SerializeField] private TMP_Text leftGuideText;
    [SerializeField] private Image rightTargetImage;
    [SerializeField] private TMP_Text rightHeadingText;
    [SerializeField] private Image rightButtonImage;
    [SerializeField] private TMP_Text rightGuideText;
    [SerializeField] private TMP_Text startPromptText;
    [SerializeField] private Image startHoldProgressImage;
    [Header("English Start Row")]
    [SerializeField] private GameObject englishStartRow;
    [SerializeField] private TMP_Text englishStartPromptText;
    [SerializeField] private Image englishStartHoldProgressImage;
    [SerializeField] private GameObject stageTitleLogo;

    [Header("Stage01 Guide Sprites")]
    [SerializeField] private Sprite chickGuideSprite;
    [SerializeField] private Sprite cuckooGuideSprite;

    [Header("Keyboard Button Sprites")]
    [SerializeField] private Sprite keyboardDButtonSprite;
    [SerializeField] private Sprite keyboardJButtonSprite;

    [Header("Gamepad Button Sprites")]
    [SerializeField] private Sprite gamepadAButtonSprite;
    [SerializeField] private Sprite gamepadBButtonSprite;

    [Header("Font Assets")]
    [SerializeField] private TMP_FontAsset koreanTmpFontAsset;
    [SerializeField] private TMP_FontAsset englishTmpFontAsset;

    [Header("Paper Unfold Animation")]
    [Min(0f)] [SerializeField] private float unfoldDuration = 0.65f;
    [SerializeField] private CanvasGroup guideCanvasGroup;

    [Header("Hold To Start")]
    [Min(0.1f)] [SerializeField] private float startHoldDuration = 2f;

    [Header("English Fixed Layout")]
    [SerializeField] private Vector2 englishGuideTitlePosition = new(0f, 428f);
    [SerializeField] private Vector2 englishButtonPosition = new(-250f, 115f);
    [SerializeField] private Vector2 englishHeadingPosition = new(75f, 115f);
    [SerializeField] private Vector2 englishTargetPosition = new(-240f, -45f);
    [SerializeField] private Vector2 englishDescriptionPosition = new(90f, -45f);

    private bool guideVisible;
    private bool startControlsVisible;
    private RectTransform animatedRect;
    private Vector2 originalPivot;
    private Vector2 originalAnchoredPosition;
    private Vector3 originalLocalScale;
    private float originalCanvasAlpha = 1f;
    private bool animationStateCaptured;
    private bool guideLayoutCaptured;
    private Vector2 guideTitleBasePosition;
    private Vector2 leftTargetBasePosition;
    private Vector2 rightTargetBasePosition;
    private Vector2 leftHeadingBasePosition;
    private Vector2 rightHeadingBasePosition;
    private Vector2 leftButtonBasePosition;
    private Vector2 rightButtonBasePosition;
    private Vector2 leftGuideBasePosition;
    private Vector2 rightGuideBasePosition;
    private TextAlignmentOptions guideTitleBaseAlignment;
    private TextAlignmentOptions leftHeadingBaseAlignment;
    private TextAlignmentOptions rightHeadingBaseAlignment;
    private TextAlignmentOptions leftGuideBaseAlignment;
    private TextAlignmentOptions rightGuideBaseAlignment;
    private TextWrappingModes leftHeadingBaseWrapping;
    private TextWrappingModes rightHeadingBaseWrapping;
    private TextWrappingModes leftGuideBaseWrapping;
    private TextWrappingModes rightGuideBaseWrapping;

    private void OnEnable()
    {
        CaptureGuideLayoutIfNeeded();
        RefreshGuide();
        SetGuideRootActive(guideVisible);
    }

    public void RefreshGuide()
    {
        bool useGamepad = TitleSession.SelectedInputMode == TitleSession.InputMode.Xbox;
        bool useEnglish = TitleSession.SelectedLanguage == TitleSession.Language.English;

        ApplyButtonSprite(
            leftButtonImage,
            useGamepad ? gamepadAButtonSprite : keyboardDButtonSprite);
        ApplyButtonSprite(
            rightButtonImage,
            useGamepad ? gamepadBButtonSprite : keyboardJButtonSprite);
        ApplyOptionalSprite(leftTargetImage, chickGuideSprite);
        ApplyOptionalSprite(rightTargetImage, cuckooGuideSprite);

        TMP_FontAsset selectedFont = useEnglish ? englishTmpFontAsset : koreanTmpFontAsset;
        ApplyText(guideTitleText, useEnglish ? "How to Play" : "게임 방법", selectedFont);
        ApplyText(leftHeadingText, useEnglish ? "Feed the Chicks" : "아기새 먹이주기", selectedFont);
        ApplyText(
            leftGuideText,
            useEnglish
                ? "Press the left button\nonce to feed!"
                : "왼쪽 버튼을 한 번 눌러 먹이를 주세요!",
            selectedFont);
        ApplyText(
            rightHeadingText,
            useEnglish ? "Chase Away the Cuckoo" : "뻐꾸기 쫓아내기",
            selectedFont);
        ApplyText(
            rightGuideText,
            useEnglish
                ? "Press the right button twice\nto chase it away!"
                : "오른쪽 버튼을 두 번 눌러 쫓아내세요!",
            selectedFont);
        bool hasEnglishStartRow = englishStartRow != null &&
            englishStartPromptText != null && englishStartHoldProgressImage != null;
        ApplyText(
            startPromptText,
            useEnglish && !hasEnglishStartRow ? "Hold to Start!" : "꾹 눌러서 시작!",
            useEnglish && !hasEnglishStartRow ? englishTmpFontAsset : koreanTmpFontAsset);
        ApplyText(englishStartPromptText, "Hold to Start!", englishTmpFontAsset);
        ApplyLanguageLayout(useEnglish);
        if (startControlsVisible)
        {
            SetStartControlsVisible(true);
        }
    }

    private void CaptureGuideLayoutIfNeeded()
    {
        if (guideLayoutCaptured)
        {
            return;
        }

        guideTitleBasePosition = GetAnchoredPosition(guideTitleText);
        leftTargetBasePosition = GetAnchoredPosition(leftTargetImage);
        rightTargetBasePosition = GetAnchoredPosition(rightTargetImage);
        leftHeadingBasePosition = GetAnchoredPosition(leftHeadingText);
        rightHeadingBasePosition = GetAnchoredPosition(rightHeadingText);
        leftButtonBasePosition = GetAnchoredPosition(leftButtonImage);
        rightButtonBasePosition = GetAnchoredPosition(rightButtonImage);
        leftGuideBasePosition = GetAnchoredPosition(leftGuideText);
        rightGuideBasePosition = GetAnchoredPosition(rightGuideText);
        guideTitleBaseAlignment = GetAlignment(guideTitleText);
        leftHeadingBaseAlignment = GetAlignment(leftHeadingText);
        rightHeadingBaseAlignment = GetAlignment(rightHeadingText);
        leftGuideBaseAlignment = GetAlignment(leftGuideText);
        rightGuideBaseAlignment = GetAlignment(rightGuideText);
        leftHeadingBaseWrapping = GetWrapping(leftHeadingText);
        rightHeadingBaseWrapping = GetWrapping(rightHeadingText);
        leftGuideBaseWrapping = GetWrapping(leftGuideText);
        rightGuideBaseWrapping = GetWrapping(rightGuideText);
        guideLayoutCaptured = true;
    }

    private void ApplyLanguageLayout(bool useEnglish)
    {
        CaptureGuideLayoutIfNeeded();
        RestoreGuideLayoutBaseline();
        if (!useEnglish)
        {
            return;
        }

        SetAnchoredPosition(guideTitleText, englishGuideTitlePosition);
        SetAnchoredPosition(leftButtonImage, englishButtonPosition);
        SetAnchoredPosition(rightButtonImage, englishButtonPosition);
        SetAnchoredPosition(leftHeadingText, englishHeadingPosition);
        SetAnchoredPosition(rightHeadingText, englishHeadingPosition);
        SetAnchoredPosition(leftTargetImage, englishTargetPosition);
        SetAnchoredPosition(rightTargetImage, englishTargetPosition);
        SetAnchoredPosition(leftGuideText, englishDescriptionPosition);
        SetAnchoredPosition(rightGuideText, englishDescriptionPosition);

        SetCentered(guideTitleText);
        SetCentered(leftHeadingText);
        SetCentered(rightHeadingText);
        SetCentered(leftGuideText, true);
        SetCentered(rightGuideText, true);
    }

    private void RestoreGuideLayoutBaseline()
    {
        SetAnchoredPosition(guideTitleText, guideTitleBasePosition);
        SetAnchoredPosition(leftTargetImage, leftTargetBasePosition);
        SetAnchoredPosition(rightTargetImage, rightTargetBasePosition);
        SetAnchoredPosition(leftHeadingText, leftHeadingBasePosition);
        SetAnchoredPosition(rightHeadingText, rightHeadingBasePosition);
        SetAnchoredPosition(leftButtonImage, leftButtonBasePosition);
        SetAnchoredPosition(rightButtonImage, rightButtonBasePosition);
        SetAnchoredPosition(leftGuideText, leftGuideBasePosition);
        SetAnchoredPosition(rightGuideText, rightGuideBasePosition);
        if (guideTitleText != null)
        {
            guideTitleText.alignment = guideTitleBaseAlignment;
        }
        RestoreTextLayout(leftHeadingText, leftHeadingBaseAlignment, leftHeadingBaseWrapping);
        RestoreTextLayout(rightHeadingText, rightHeadingBaseAlignment, rightHeadingBaseWrapping);
        RestoreTextLayout(leftGuideText, leftGuideBaseAlignment, leftGuideBaseWrapping);
        RestoreTextLayout(rightGuideText, rightGuideBaseAlignment, rightGuideBaseWrapping);
    }

    private static Vector2 GetAnchoredPosition(Component component)
    {
        RectTransform rect = GetRect(component);
        return rect != null ? rect.anchoredPosition : Vector2.zero;
    }

    private static RectTransform GetRect(Component component)
    {
        return component != null ? component.transform as RectTransform : null;
    }

    private static void SetAnchoredPosition(Component component, Vector2 position)
    {
        RectTransform rect = GetRect(component);
        if (rect != null)
        {
            rect.anchoredPosition = position;
        }
    }

    private static TextAlignmentOptions GetAlignment(TMP_Text target)
    {
        return target != null ? target.alignment : TextAlignmentOptions.Center;
    }

    private static TextWrappingModes GetWrapping(TMP_Text target)
    {
        return target != null ? target.textWrappingMode : TextWrappingModes.Normal;
    }

    private static void SetCentered(TMP_Text target, bool wrap = false)
    {
        if (target == null)
        {
            return;
        }

        target.alignment = TextAlignmentOptions.Center;
        if (wrap)
        {
            target.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    private static void RestoreTextLayout(
        TMP_Text target,
        TextAlignmentOptions alignment,
        TextWrappingModes wrapping)
    {
        if (target == null)
        {
            return;
        }

        target.alignment = alignment;
        target.textWrappingMode = wrapping;
    }

    public void ShowGuide()
    {
        RestoreAnimationState();
        guideVisible = true;
        RefreshGuide();
        SetStageTitleLogoActive(false);
        SetGuideRootActive(true);
        SetStartControlsVisible(true);
    }

    public IEnumerator ShowGuideUnfolded()
    {
        RestoreAnimationState();
        guideVisible = true;
        RefreshGuide();
        SetStageTitleLogoActive(false);
        SetGuideRootActive(true);
        SetStartControlsVisible(false);

        if (!TryCaptureAnimationState())
        {
            SetStartControlsVisible(true);
            yield break;
        }

        SetTemporaryCenterPivot(animatedRect);
        Vector3 startScale = new(
            originalLocalScale.x * 0.9f,
            originalLocalScale.y * 0.03f,
            originalLocalScale.z);
        animatedRect.localScale = startScale;
        guideCanvasGroup.alpha = 0f;

        float duration = Mathf.Max(0f, unfoldDuration);
        if (duration <= 0f)
        {
            RestoreAnimationState();
            SetStartControlsVisible(true);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float unfoldProgress = EaseOutBack(normalized);
            animatedRect.localScale = Vector3.LerpUnclamped(
                startScale,
                originalLocalScale,
                unfoldProgress);
            guideCanvasGroup.alpha = Mathf.SmoothStep(0f, originalCanvasAlpha, normalized);
            yield return null;
        }

        RestoreAnimationState();
        SetStartControlsVisible(true);
    }

    public IEnumerator WaitForHoldStartInput()
    {
        SetStartControlsVisible(true);
        ResetHoldProgress();

        while (GetPressedHoldInput() != HoldInputSource.None)
        {
            yield return null;
        }

        yield return null;

        HoldInputSource activeInput = HoldInputSource.None;
        float heldDuration = 0f;
        float requiredDuration = Mathf.Max(0.1f, startHoldDuration);
        while (heldDuration < requiredDuration)
        {
            HoldInputSource pressedInput = GetPressedHoldInput();
            if (activeInput == HoldInputSource.None)
            {
                if (pressedInput != HoldInputSource.None)
                {
                    activeInput = pressedInput;
                }

                yield return null;
                continue;
            }

            if (!IsHoldInputPressed(activeInput) || IsDifferentHoldInputPressed(activeInput))
            {
                activeInput = HoldInputSource.None;
                heldDuration = 0f;
                SetHoldProgress(0f);
                yield return null;
                continue;
            }

            heldDuration += Time.unscaledDeltaTime;
            SetHoldProgress(heldDuration / requiredDuration);
            yield return null;
        }

        SetHoldProgress(1f);
    }

    public void HideGuide()
    {
        RestoreAnimationState();
        ResetHoldProgress();
        SetStartControlsVisible(false);
        guideVisible = false;
        SetGuideRootActive(false);
    }

    private void OnDisable()
    {
        RestoreAnimationState();
        ResetHoldProgress();
    }

    private void SetGuideRootActive(bool active)
    {
        if (guideRoot != null && guideRoot.activeSelf != active)
        {
            guideRoot.SetActive(active);
        }
    }

    private void SetStageTitleLogoActive(bool active)
    {
        if (stageTitleLogo == null)
        {
            Transform titleLogo = transform.Find("Title_01");
            if (titleLogo == null)
            {
                titleLogo = transform.Find("BackGround/Title_01");
            }
            if (titleLogo != null)
            {
                stageTitleLogo = titleLogo.gameObject;
            }
        }

        if (stageTitleLogo != null && stageTitleLogo.activeSelf != active)
        {
            stageTitleLogo.SetActive(active);
        }
    }

    private bool TryCaptureAnimationState()
    {
        if (guideRoot == null)
        {
            return false;
        }

        animatedRect = guideRoot.transform as RectTransform;
        if (animatedRect == null)
        {
            return false;
        }

        if (guideCanvasGroup == null)
        {
            guideCanvasGroup = guideRoot.GetComponent<CanvasGroup>();
        }

        if (guideCanvasGroup == null)
        {
            guideCanvasGroup = guideRoot.AddComponent<CanvasGroup>();
        }

        originalPivot = animatedRect.pivot;
        originalAnchoredPosition = animatedRect.anchoredPosition;
        originalLocalScale = animatedRect.localScale;
        originalCanvasAlpha = guideCanvasGroup.alpha > 0f
            ? guideCanvasGroup.alpha
            : 1f;
        animationStateCaptured = true;
        return true;
    }

    private static void SetTemporaryCenterPivot(RectTransform rect)
    {
        Vector2 centerPivot = new(0.5f, 0.5f);
        Vector2 pivotDelta = centerPivot - rect.pivot;
        Vector2 rectSize = rect.rect.size;
        rect.pivot = centerPivot;
        rect.anchoredPosition += Vector2.Scale(pivotDelta, rectSize);
    }

    private void RestoreAnimationState()
    {
        if (!animationStateCaptured || animatedRect == null)
        {
            return;
        }

        animatedRect.localScale = originalLocalScale;
        animatedRect.pivot = originalPivot;
        animatedRect.anchoredPosition = originalAnchoredPosition;
        if (guideCanvasGroup != null)
        {
            guideCanvasGroup.alpha = originalCanvasAlpha;
        }

        animationStateCaptured = false;
        animatedRect = null;
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.35f;
        float shifted = value - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted
            + overshoot * shifted * shifted;
    }

    private void SetStartControlsVisible(bool visible)
    {
        startControlsVisible = visible;
        bool useEnglishRow = TitleSession.SelectedLanguage == TitleSession.Language.English &&
            englishStartRow != null && englishStartPromptText != null &&
            englishStartHoldProgressImage != null;
        bool showLegacyControls = visible && !useEnglishRow;

        if (startPromptText != null &&
            startPromptText.gameObject.activeSelf != showLegacyControls)
        {
            startPromptText.gameObject.SetActive(showLegacyControls);
        }

        if (startHoldProgressImage != null &&
            startHoldProgressImage.gameObject.activeSelf != showLegacyControls)
        {
            startHoldProgressImage.gameObject.SetActive(showLegacyControls);
        }

        if (englishStartRow != null && englishStartRow.activeSelf != (visible && useEnglishRow))
        {
            englishStartRow.SetActive(visible && useEnglishRow);
        }

        if (visible)
        {
            ConfigureHoldProgressImage(
                useEnglishRow ? englishStartHoldProgressImage : startHoldProgressImage);
        }
    }

    private static void ConfigureHoldProgressImage(Image progressImage)
    {
        if (progressImage == null)
        {
            return;
        }

        progressImage.type = Image.Type.Filled;
        progressImage.fillMethod = Image.FillMethod.Radial360;
        progressImage.fillOrigin = (int)Image.Origin360.Top;
        progressImage.fillClockwise = true;
    }

    private void ResetHoldProgress()
    {
        SetHoldProgress(0f);
    }

    private void SetHoldProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        if (startHoldProgressImage != null)
        {
            startHoldProgressImage.fillAmount = clampedProgress;
        }

        if (englishStartHoldProgressImage != null)
        {
            englishStartHoldProgressImage.fillAmount = clampedProgress;
        }
    }

    private static HoldInputSource GetPressedHoldInput()
    {
        bool allowKeyboard = TitleSession.SelectedInputMode != TitleSession.InputMode.Xbox;
        bool allowGamepad = TitleSession.SelectedInputMode != TitleSession.InputMode.KeyboardMouse;

        if (allowKeyboard && Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed)
            {
                return HoldInputSource.KeyboardD;
            }

            if (Keyboard.current.jKey.isPressed)
            {
                return HoldInputSource.KeyboardJ;
            }
        }

        if (allowGamepad && Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.isPressed)
            {
                return HoldInputSource.GamepadA;
            }

            if (Gamepad.current.buttonEast.isPressed)
            {
                return HoldInputSource.GamepadB;
            }
        }

        return HoldInputSource.None;
    }

    private static bool IsHoldInputPressed(HoldInputSource input)
    {
        return input switch
        {
            HoldInputSource.KeyboardD =>
                Keyboard.current != null && Keyboard.current.dKey.isPressed,
            HoldInputSource.KeyboardJ =>
                Keyboard.current != null && Keyboard.current.jKey.isPressed,
            HoldInputSource.GamepadA =>
                Gamepad.current != null && Gamepad.current.buttonSouth.isPressed,
            HoldInputSource.GamepadB =>
                Gamepad.current != null && Gamepad.current.buttonEast.isPressed,
            _ => false
        };
    }

    private static bool IsDifferentHoldInputPressed(HoldInputSource activeInput)
    {
        bool allowKeyboard = TitleSession.SelectedInputMode != TitleSession.InputMode.Xbox;
        bool allowGamepad = TitleSession.SelectedInputMode != TitleSession.InputMode.KeyboardMouse;

        if (allowKeyboard && Keyboard.current != null)
        {
            if (activeInput != HoldInputSource.KeyboardD && Keyboard.current.dKey.isPressed)
            {
                return true;
            }

            if (activeInput != HoldInputSource.KeyboardJ && Keyboard.current.jKey.isPressed)
            {
                return true;
            }
        }

        if (allowGamepad && Gamepad.current != null)
        {
            if (activeInput != HoldInputSource.GamepadA && Gamepad.current.buttonSouth.isPressed)
            {
                return true;
            }

            if (activeInput != HoldInputSource.GamepadB && Gamepad.current.buttonEast.isPressed)
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplyButtonSprite(Image target, Sprite sprite)
    {
        if (target == null)
        {
            return;
        }

        target.sprite = sprite;
        target.enabled = sprite != null;
    }

    private static void ApplyOptionalSprite(Image target, Sprite sprite)
    {
        if (target == null || sprite == null)
        {
            return;
        }

        target.sprite = sprite;
        target.enabled = true;
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
        else if (target.font == null)
        {
            target.font = TMP_Settings.defaultFontAsset;
        }
    }
}
