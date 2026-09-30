using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Stage01LanguageApplier : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private Image imageTarget1;
    [SerializeField] private Sprite koreanSprite1;
    [SerializeField] private Sprite englishSprite1;
    [SerializeField] private Image imageTarget2;
    [SerializeField] private Sprite koreanSprite2;
    [SerializeField] private Sprite englishSprite2;

    [Header("Texts")]
    [SerializeField] private TMP_Text textTarget1;
    [SerializeField] private string koreanText1;
    [SerializeField] private string englishText1;
    [SerializeField] private TMP_Text textTarget2;
    [SerializeField] private string koreanText2;
    [SerializeField] private string englishText2;

    [Header("Fonts")]
    [InspectorName("Korean TMP Font Asset")]
    [SerializeField] private TMP_FontAsset koreanTmpFontAsset;
    [InspectorName("English TMP Font Asset")]
    [SerializeField] private TMP_FontAsset englishTmpFontAsset;

    private void Awake()
    {
        ApplySelectedLanguage();
    }

    public void ApplySelectedLanguage()
    {
        bool koreanSelected = TitleSession.SelectedLanguage == TitleSession.Language.Korean;
        TMP_FontAsset selectedFont = koreanSelected ? koreanTmpFontAsset : englishTmpFontAsset;

        ApplyImage(imageTarget1, koreanSelected ? koreanSprite1 : englishSprite1);
        ApplyImage(imageTarget2, koreanSelected ? koreanSprite2 : englishSprite2);
        ApplyText(textTarget1, koreanSelected ? koreanText1 : englishText1, selectedFont);
        ApplyText(textTarget2, koreanSelected ? koreanText2 : englishText2, selectedFont);
    }

    private static void ApplyImage(Image target, Sprite sprite)
    {
        if (target == null || sprite == null)
        {
            return;
        }

        target.sprite = sprite;
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
