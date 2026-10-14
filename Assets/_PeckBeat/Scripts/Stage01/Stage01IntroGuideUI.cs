using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Stage01IntroGuideUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private RectTransform rootPanel;
    [SerializeField] private CanvasGroup rootCanvasGroup;

    [Header("Images")]
    [SerializeField] private Image feedKeyImage;
    [SerializeField] private Image peckKeyImage;
    [SerializeField] private Image feedBirdImage;
    [SerializeField] private Image peckBirdImage;

    [Header("Sprites")]
    [SerializeField] private Sprite keyboardDSprite;
    [SerializeField] private Sprite keyboardJSprite;
    [SerializeField] private Sprite gamepadASprite;
    [SerializeField] private Sprite gamepadBSprite;
    [SerializeField] private Sprite chickSmallSprite;
    [SerializeField] private Sprite cuckooSmallSprite;

    [Header("Button Image Scale")]
    [SerializeField] private float keyboardButtonScaleMultiplier = 0.7f;

    [Header("Texts")]
    [SerializeField] private TMP_Text feedText;
    [SerializeField] private TMP_Text peckText;

    [Header("Fonts")]
    [InspectorName("Korean TMP Font Asset")]
    [SerializeField] private TMP_FontAsset koreanTmpFontAsset;
    [InspectorName("English TMP Font Asset")]
    [SerializeField] private TMP_FontAsset englishTmpFontAsset;

    [Header("Timing")]
    [SerializeField] private float visibleDuration = 20f;
    [SerializeField] private float hideDuration = 0.7f;
    [SerializeField] private float hideMoveDistanceY = 300f;

    private Vector2 initialAnchoredPosition;
    private Vector3 originalFeedKeyScale = Vector3.one;
    private Vector3 originalPeckKeyScale = Vector3.one;

    private void Awake()
    {
        if (rootPanel != null)
        {
            initialAnchoredPosition = rootPanel.anchoredPosition;
        }

        if (feedKeyImage != null)
        {
            originalFeedKeyScale = feedKeyImage.rectTransform.localScale;
        }

        if (peckKeyImage != null)
        {
            originalPeckKeyScale = peckKeyImage.rectTransform.localScale;
        }

        ApplyInputSprites();
        ApplyBirdSprites();
        ApplyLanguage();
        SetCanvasGroupAlpha(1f);
    }

    private void Start()
    {
        StartCoroutine(HideAfterDelayRoutine());
    }

    private void ApplyInputSprites()
    {
        bool useGamepad = TitleSession.SelectedInputMode == TitleSession.InputMode.Xbox;
        ApplySprite(feedKeyImage, useGamepad ? gamepadASprite : keyboardDSprite);
        ApplySprite(peckKeyImage, useGamepad ? gamepadBSprite : keyboardJSprite);
        ApplyButtonImageScale(useGamepad);
    }

    private void ApplyButtonImageScale(bool useGamepad)
    {
        float multiplier = useGamepad ? 1f : Mathf.Max(0f, keyboardButtonScaleMultiplier);

        if (feedKeyImage != null)
        {
            feedKeyImage.rectTransform.localScale = originalFeedKeyScale * multiplier;
        }

        if (peckKeyImage != null)
        {
            peckKeyImage.rectTransform.localScale = originalPeckKeyScale * multiplier;
        }
    }

    private void ApplyBirdSprites()
    {
        ApplySprite(feedBirdImage, chickSmallSprite);
        ApplySprite(peckBirdImage, cuckooSmallSprite);
    }

    private void ApplyLanguage()
    {
        bool useEnglish = TitleSession.SelectedLanguage == TitleSession.Language.English;
        TMP_FontAsset selectedFont = useEnglish ? englishTmpFontAsset : koreanTmpFontAsset;

        ApplyText(feedText, useEnglish ? "Press once to feed!" : "한 번 눌러 먹이주기!", selectedFont);
        ApplyText(
            peckText,
            useEnglish ? "Press twice to chase away!" : "두 번 눌러 쫓아내기!",
            selectedFont);
    }

    private IEnumerator HideAfterDelayRoutine()
    {
        yield return WaitUnscaled(visibleDuration);

        Vector2 endPosition = initialAnchoredPosition + Vector2.down * hideMoveDistanceY;
        float duration = Mathf.Max(0f, hideDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, NormalizedTime(elapsed, duration));

            if (rootPanel != null)
            {
                rootPanel.anchoredPosition = Vector2.LerpUnclamped(
                    initialAnchoredPosition,
                    endPosition,
                    progress);
            }

            SetCanvasGroupAlpha(1f - progress);
            yield return null;
        }

        if (rootPanel != null)
        {
            rootPanel.anchoredPosition = endPosition;
        }

        SetCanvasGroupAlpha(0f);

        if (rootPanel != null)
        {
            rootPanel.gameObject.SetActive(false);
        }
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

    private static float NormalizedTime(float elapsed, float duration)
    {
        return duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
    }

    private static void ApplySprite(Image target, Sprite sprite)
    {
        if (target != null && sprite != null)
        {
            target.sprite = sprite;
        }
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

    private void SetCanvasGroupAlpha(float alpha)
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = alpha;
        }
    }
}
