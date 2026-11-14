using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Stage02TitleGuideUI : MonoBehaviour
{
    private enum HoldInputSource { None, KeyboardD, KeyboardJ, GamepadA, GamepadB }

    [Header("Guide References")]
    [SerializeField] private GameObject guideRoot;
    [SerializeField] private Image guidePanelImage;
    [SerializeField] private TMP_Text guideTitleText;
    [SerializeField] private Image leftTargetImage;
    [SerializeField] private Image leftButtonImage;
    [SerializeField] private TMP_Text leftGuideText;
    [SerializeField] private Image middleTargetImage;
    [SerializeField] private Image middleButtonImage;
    [SerializeField] private TMP_Text middleGuideText;
    [SerializeField] private Image rightTargetImage;
    [SerializeField] private Image rightButtonImage;
    [SerializeField] private TMP_Text rightGuideText;
    [SerializeField] private TMP_Text startPromptText;
    [SerializeField] private Image startHoldProgressImage;

    [Header("Stage02 Guide Sprites")]
    [SerializeField] private Sprite oneHitLogSprite;
    [SerializeField] private Sprite twoHitLogSprite;
    [SerializeField] private Sprite holdLogSprite;

    [Header("Action A Button Sprites")]
    [SerializeField] private Sprite keyboardDButtonSprite;
    [SerializeField] private Sprite gamepadAButtonSprite;

    [Header("Action B Button Sprites")]
    [SerializeField] private Sprite keyboardJButtonSprite;
    [SerializeField] private Sprite gamepadBButtonSprite;

    [Header("Font Assets")]
    [SerializeField] private TMP_FontAsset koreanTmpFontAsset;
    [SerializeField] private TMP_FontAsset englishTmpFontAsset;

    [Header("Paper Unfold Animation")]
    [Min(0f)] [SerializeField] private float unfoldDuration = 0.65f;
    [SerializeField] private CanvasGroup guideCanvasGroup;

    [Header("Hold To Start")]
    [Min(0.1f)] [SerializeField] private float startHoldDuration = 2f;

    [Header("English Layout Fine Tuning")]
    [Min(0f)] [SerializeField] private float englishButtonGapAboveTarget = 12f;

    private bool guideVisible;
    private RectTransform animatedRect;
    private Vector2 originalPivot;
    private Vector2 originalAnchoredPosition;
    private Vector3 originalLocalScale;
    private float originalCanvasAlpha = 1f;
    private bool animationStateCaptured;
    private bool guideLayoutCaptured;
    private Vector2 leftButtonBasePosition;
    private Vector2 middleButtonBasePosition;
    private Vector2 rightButtonBasePosition;
    private Vector2 startPromptBasePosition;
    private TextAlignmentOptions leftTextBaseAlignment;
    private TextAlignmentOptions middleTextBaseAlignment;
    private TextAlignmentOptions rightTextBaseAlignment;
    private TextAlignmentOptions startPromptBaseAlignment;
    private TextWrappingModes leftTextBaseWrapping;
    private TextWrappingModes middleTextBaseWrapping;
    private TextWrappingModes rightTextBaseWrapping;
    private TextWrappingModes startPromptBaseWrapping;

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
        Sprite actionA = useGamepad ? gamepadAButtonSprite : keyboardDButtonSprite;

        ApplyButtonSprite(leftButtonImage, actionA);
        ApplyButtonSprite(middleButtonImage, actionA);
        ApplyButtonSprite(rightButtonImage, useGamepad ? gamepadBButtonSprite : keyboardJButtonSprite);
        ApplyOptionalSprite(leftTargetImage, oneHitLogSprite);
        ApplyOptionalSprite(middleTargetImage, twoHitLogSprite);
        ApplyOptionalSprite(rightTargetImage, holdLogSprite);

        TMP_FontAsset font = useEnglish ? englishTmpFontAsset : koreanTmpFontAsset;
        ApplyText(guideTitleText, useEnglish ? "How to Play" : "게임 방법", font);
        ApplyText(leftGuideText, useEnglish ? "Press once\non the beat!" : "박자에 맞춰 한 번!", font);
        ApplyText(middleGuideText, useEnglish ? "Press twice\non the beat!" : "박자에 맞춰 두 번!", font);
        ApplyText(rightGuideText, useEnglish ? "Hold to\ncarve!" : "길게 눌러 깎기!", font);
        ApplyText(startPromptText, useEnglish ? "Hold to Start!" : "꾹 눌러서 시작!", font);
        ApplyLanguageLayout(useEnglish);
    }

    private void CaptureGuideLayoutIfNeeded()
    {
        if (guideLayoutCaptured)
        {
            return;
        }

        leftButtonBasePosition = GetAnchoredPosition(leftButtonImage);
        middleButtonBasePosition = GetAnchoredPosition(middleButtonImage);
        rightButtonBasePosition = GetAnchoredPosition(rightButtonImage);
        startPromptBasePosition = GetAnchoredPosition(startPromptText);
        leftTextBaseAlignment = GetAlignment(leftGuideText);
        middleTextBaseAlignment = GetAlignment(middleGuideText);
        rightTextBaseAlignment = GetAlignment(rightGuideText);
        startPromptBaseAlignment = GetAlignment(startPromptText);
        leftTextBaseWrapping = GetWrapping(leftGuideText);
        middleTextBaseWrapping = GetWrapping(middleGuideText);
        rightTextBaseWrapping = GetWrapping(rightGuideText);
        startPromptBaseWrapping = GetWrapping(startPromptText);
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

        PlaceButtonAboveTarget(leftButtonImage, leftTargetImage);
        PlaceButtonAboveTarget(middleButtonImage, middleTargetImage);
        PlaceButtonAboveTarget(rightButtonImage, rightTargetImage);
        SetCenteredAndWrapped(leftGuideText);
        SetCenteredAndWrapped(middleGuideText);
        SetCenteredAndWrapped(rightGuideText);
        SetAnchoredPosition(startPromptText, new Vector2(0f, startPromptBasePosition.y));
        if (startPromptText != null)
        {
            startPromptText.alignment = TextAlignmentOptions.Center;
        }
    }

    private void RestoreGuideLayoutBaseline()
    {
        SetAnchoredPosition(leftButtonImage, leftButtonBasePosition);
        SetAnchoredPosition(middleButtonImage, middleButtonBasePosition);
        SetAnchoredPosition(rightButtonImage, rightButtonBasePosition);
        SetAnchoredPosition(startPromptText, startPromptBasePosition);
        RestoreTextLayout(leftGuideText, leftTextBaseAlignment, leftTextBaseWrapping);
        RestoreTextLayout(middleGuideText, middleTextBaseAlignment, middleTextBaseWrapping);
        RestoreTextLayout(rightGuideText, rightTextBaseAlignment, rightTextBaseWrapping);
        RestoreTextLayout(startPromptText, startPromptBaseAlignment, startPromptBaseWrapping);
    }

    private void PlaceButtonAboveTarget(Image button, Image target)
    {
        RectTransform buttonRect = GetRect(button);
        RectTransform targetRect = GetRect(target);
        if (buttonRect == null || targetRect == null)
        {
            return;
        }

        float targetTop = targetRect.anchoredPosition.y
            + targetRect.rect.height * (1f - targetRect.pivot.y);
        float buttonY = targetTop
            + englishButtonGapAboveTarget
            + buttonRect.rect.height * buttonRect.pivot.y;
        buttonRect.anchoredPosition = new Vector2(targetRect.anchoredPosition.x, buttonY);
    }

    private static RectTransform GetRect(Component component)
    {
        return component != null ? component.transform as RectTransform : null;
    }

    private static Vector2 GetAnchoredPosition(Component component)
    {
        RectTransform rect = GetRect(component);
        return rect != null ? rect.anchoredPosition : Vector2.zero;
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

    private static void SetCenteredAndWrapped(TMP_Text target)
    {
        if (target == null)
        {
            return;
        }

        target.alignment = TextAlignmentOptions.Center;
        target.textWrappingMode = TextWrappingModes.Normal;
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

    public IEnumerator ShowGuideUnfolded()
    {
        RestoreAnimationState();
        guideVisible = true;
        RefreshGuide();
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
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            animatedRect.localScale = Vector3.LerpUnclamped(startScale, originalLocalScale, EaseOutBack(normalized));
            guideCanvasGroup.alpha = Mathf.SmoothStep(0f, originalCanvasAlpha, normalized);
            yield return null;
        }

        RestoreAnimationState();
        SetStartControlsVisible(true);
    }

    public IEnumerator WaitForHoldStartInput()
    {
        SetStartControlsVisible(true);
        SetHoldProgress(0f);
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
                if (pressedInput != HoldInputSource.None) activeInput = pressedInput;
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
        SetHoldProgress(0f);
        SetStartControlsVisible(false);
        guideVisible = false;
        SetGuideRootActive(false);
    }

    private void OnDisable()
    {
        RestoreAnimationState();
        SetHoldProgress(0f);
    }

    private void SetGuideRootActive(bool active)
    {
        if (guideRoot != null && guideRoot.activeSelf != active) guideRoot.SetActive(active);
    }

    private bool TryCaptureAnimationState()
    {
        animatedRect = guideRoot != null ? guideRoot.transform as RectTransform : null;
        if (animatedRect == null) return false;

        if (guideCanvasGroup == null) guideCanvasGroup = guideRoot.GetComponent<CanvasGroup>();
        if (guideCanvasGroup == null) guideCanvasGroup = guideRoot.AddComponent<CanvasGroup>();

        originalPivot = animatedRect.pivot;
        originalAnchoredPosition = animatedRect.anchoredPosition;
        originalLocalScale = animatedRect.localScale;
        originalCanvasAlpha = guideCanvasGroup.alpha > 0f ? guideCanvasGroup.alpha : 1f;
        animationStateCaptured = true;
        return true;
    }

    private static void SetTemporaryCenterPivot(RectTransform rect)
    {
        Vector2 center = new(0.5f, 0.5f);
        Vector2 pivotDelta = center - rect.pivot;
        rect.pivot = center;
        rect.anchoredPosition += Vector2.Scale(pivotDelta, rect.rect.size);
    }

    private void RestoreAnimationState()
    {
        if (!animationStateCaptured || animatedRect == null) return;
        animatedRect.localScale = originalLocalScale;
        animatedRect.pivot = originalPivot;
        animatedRect.anchoredPosition = originalAnchoredPosition;
        if (guideCanvasGroup != null) guideCanvasGroup.alpha = originalCanvasAlpha;
        animationStateCaptured = false;
        animatedRect = null;
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.35f;
        float shifted = value - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
    }

    private void SetStartControlsVisible(bool visible)
    {
        if (startPromptText != null) startPromptText.gameObject.SetActive(visible);
        if (startHoldProgressImage == null) return;

        startHoldProgressImage.gameObject.SetActive(visible);
        startHoldProgressImage.type = Image.Type.Filled;
        startHoldProgressImage.fillMethod = Image.FillMethod.Radial360;
        startHoldProgressImage.fillOrigin = (int)Image.Origin360.Top;
        startHoldProgressImage.fillClockwise = true;
    }

    private void SetHoldProgress(float value)
    {
        if (startHoldProgressImage != null) startHoldProgressImage.fillAmount = Mathf.Clamp01(value);
    }

    private static HoldInputSource GetPressedHoldInput()
    {
        bool keyboardAllowed = TitleSession.SelectedInputMode != TitleSession.InputMode.Xbox;
        bool gamepadAllowed = TitleSession.SelectedInputMode != TitleSession.InputMode.KeyboardMouse;
        if (keyboardAllowed && Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed) return HoldInputSource.KeyboardD;
            if (Keyboard.current.jKey.isPressed) return HoldInputSource.KeyboardJ;
        }

        if (gamepadAllowed && Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.isPressed) return HoldInputSource.GamepadA;
            if (Gamepad.current.buttonEast.isPressed) return HoldInputSource.GamepadB;
        }

        return HoldInputSource.None;
    }

    private static bool IsHoldInputPressed(HoldInputSource input)
    {
        return input switch
        {
            HoldInputSource.KeyboardD => Keyboard.current?.dKey.isPressed == true,
            HoldInputSource.KeyboardJ => Keyboard.current?.jKey.isPressed == true,
            HoldInputSource.GamepadA => Gamepad.current?.buttonSouth.isPressed == true,
            HoldInputSource.GamepadB => Gamepad.current?.buttonEast.isPressed == true,
            _ => false
        };
    }

    private static bool IsDifferentHoldInputPressed(HoldInputSource activeInput)
    {
        bool keyboardAllowed = TitleSession.SelectedInputMode != TitleSession.InputMode.Xbox;
        bool gamepadAllowed = TitleSession.SelectedInputMode != TitleSession.InputMode.KeyboardMouse;
        return (keyboardAllowed && Keyboard.current != null &&
                ((activeInput != HoldInputSource.KeyboardD && Keyboard.current.dKey.isPressed) ||
                 (activeInput != HoldInputSource.KeyboardJ && Keyboard.current.jKey.isPressed))) ||
               (gamepadAllowed && Gamepad.current != null &&
                ((activeInput != HoldInputSource.GamepadA && Gamepad.current.buttonSouth.isPressed) ||
                 (activeInput != HoldInputSource.GamepadB && Gamepad.current.buttonEast.isPressed)));
    }

    private static void ApplyButtonSprite(Image target, Sprite sprite)
    {
        if (target == null) return;
        target.sprite = sprite;
        target.enabled = sprite != null;
        target.preserveAspect = true;
    }

    private static void ApplyOptionalSprite(Image target, Sprite sprite)
    {
        if (target == null || sprite == null) return;
        target.sprite = sprite;
        target.enabled = true;
        target.preserveAspect = true;
    }

    private static void ApplyText(TMP_Text target, string value, TMP_FontAsset font)
    {
        if (target == null) return;
        target.text = value;
        if (font != null) target.font = font;
        else if (target.font == null) target.font = TMP_Settings.defaultFontAsset;
    }
}
