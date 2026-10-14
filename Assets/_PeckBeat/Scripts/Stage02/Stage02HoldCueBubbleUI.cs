using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Stage02HoldCueBubbleUI : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite bubbleSprite;
    [SerializeField] private Sprite keyboardJIconSprite;
    [SerializeField] private Sprite xboxBIconSprite;

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset koreanFont;
    [SerializeField] private TMP_FontAsset englishFont;

    [Header("UI References")]
    [SerializeField] private Image bubbleImage;
    [SerializeField] private Image buttonIconImage;
    [SerializeField] private TMP_Text holdText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Display")]
    [Min(0f)] [SerializeField] private float fadeDuration = 0.08f;
    [Min(0.01f)] [SerializeField] private float defaultScale = 1f;

    private Coroutine fadeRoutine;
    private Vector3 authoredLocalScale = Vector3.one;

    private void Awake()
    {
        authoredLocalScale = transform.localScale;
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        HideImmediate();
    }

    private void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position + worldOffset;
        }
    }

    public void Show(TitleSession.InputMode inputMode, TitleSession.Language language)
    {
        if (bubbleImage != null && bubbleSprite != null)
        {
            bubbleImage.sprite = bubbleSprite;
            bubbleImage.enabled = true;
        }

        Sprite buttonSprite = inputMode == TitleSession.InputMode.Xbox
            ? xboxBIconSprite
            : keyboardJIconSprite;
        if (buttonIconImage != null)
        {
            buttonIconImage.sprite = buttonSprite;
            buttonIconImage.enabled = buttonSprite != null;
        }

        bool useEnglish = language == TitleSession.Language.English;
        if (holdText != null)
        {
            holdText.text = useEnglish ? "Hold!" : "꾹 눌러!";
            TMP_FontAsset selectedFont = useEnglish ? englishFont : koreanFont;
            if (selectedFont != null)
            {
                holdText.font = selectedFont;
            }
        }

        transform.localScale = authoredLocalScale * Mathf.Max(0.01f, defaultScale);
        gameObject.SetActive(true);

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        if (canvasGroup == null || fadeDuration <= 0f)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            fadeRoutine = null;
            return;
        }

        canvasGroup.alpha = 0f;
        fadeRoutine = StartCoroutine(FadeInRoutine());
    }

    public void Hide()
    {
        HideImmediate();
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        if (followTarget != null)
        {
            transform.position = followTarget.position + worldOffset;
        }
    }

    public void SetWorldOffset(Vector3 offset)
    {
        worldOffset = offset;
    }

    private IEnumerator FadeInRoutine()
    {
        float duration = Mathf.Max(0.001f, fadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        fadeRoutine = null;
    }

    private void HideImmediate()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        gameObject.SetActive(false);
    }
}
