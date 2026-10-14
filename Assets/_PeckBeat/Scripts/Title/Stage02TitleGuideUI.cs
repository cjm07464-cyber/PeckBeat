using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Stage02TitleGuideUI : MonoBehaviour
{
    [Header("Guide References")]
    [SerializeField] private GameObject guideRoot;
    [SerializeField] private TMP_Text guideTitleText;
    [SerializeField] private Image leftButtonImage;
    [SerializeField] private TMP_Text leftGuideText;
    [SerializeField] private Image rightButtonImage;
    [SerializeField] private TMP_Text rightGuideText;

    [Header("Action A Button Sprites")]
    [SerializeField] private Sprite keyboardDButtonSprite;
    [SerializeField] private Sprite gamepadAButtonSprite;

    [Header("Action B Button Sprites")]
    [SerializeField] private Sprite keyboardJButtonSprite;
    [SerializeField] private Sprite gamepadBButtonSprite;

    [Header("Font Assets")]
    [SerializeField] private TMP_FontAsset koreanTmpFontAsset;
    [SerializeField] private TMP_FontAsset englishTmpFontAsset;

    private void OnEnable()
    {
        RefreshGuide();
    }

    public void RefreshGuide()
    {
        bool useGamepad = TitleSession.SelectedInputMode == TitleSession.InputMode.Xbox;
        bool useEnglish = TitleSession.SelectedLanguage == TitleSession.Language.English;

        if (guideRoot != null)
        {
            guideRoot.SetActive(true);
        }

        ApplyButtonSprite(
            leftButtonImage,
            useGamepad ? gamepadAButtonSprite : keyboardDButtonSprite);
        ApplyButtonSprite(
            rightButtonImage,
            useGamepad ? gamepadBButtonSprite : keyboardJButtonSprite);

        TMP_FontAsset selectedFont = useEnglish ? englishTmpFontAsset : koreanTmpFontAsset;
        ApplyText(guideTitleText, useEnglish ? "How to Play" : "게임 방법", selectedFont);
        ApplyText(leftGuideText, useEnglish ? "Peck on beat!" : "박자에 맞춰 쪼기!", selectedFont);
        ApplyText(rightGuideText, useEnglish ? "Hold to carve!" : "길게 눌러 깎기!", selectedFont);
    }

    private static void ApplyButtonSprite(Image target, Sprite sprite)
    {
        if (target == null)
        {
            return;
        }

        target.sprite = sprite;
        target.enabled = sprite != null;
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
        else if (target.font == null)
        {
            target.font = TMP_Settings.defaultFontAsset;
        }
    }
}
