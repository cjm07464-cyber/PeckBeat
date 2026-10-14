using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class TitleIntroDialogueController : MonoBehaviour
{
    [Header("Intro Backing")]
    [SerializeField] private Image introBlackBackground;
    [SerializeField] private Image ratFloorImage;
    [SerializeField] private Sprite ratFloorSprite;

    private bool introBackgroundPrepared;

    [Header("Dialogue Appearance Timing")]
    [SerializeField, Min(0f)] private float ratAppearDelayAfterTow = 0.45f;
    [SerializeField, Min(0f)] private float speechBubbleDelayAfterRatAppear = 1f;
    [SerializeField, Min(0f)] private float choiceDelayAfterReadyQuestion = 0.3f;
    [Tooltip("Legacy / Unused: choice appearance SFX timing is no longer applied.")]
    [SerializeField, Min(0f)] private float choiceVisualDelayAfterSfx = 0.03f;
    [SerializeField, Min(0f)] private float choiceDelayAfterNoResponse = 0.3f;
    [SerializeField, Min(0f)] private float delayAfterFinalLineComplete = 1f;
    [SerializeField, Min(0f)] private float introDialogueFadeOutDuration = 1f;

    [Header("Dialogue Text Wrapping")]
    [SerializeField, Min(1f)] private float dialogueMaxTextWidth = 850f;
    [SerializeField, Min(1)] private int dialogueMaxLines = 3;
    [SerializeField] private float dialogueLineSpacing = 0f;
    [SerializeField] private bool autoWrapDialogueText = true;
    [SerializeField] private Vector2 koreanMenuButtonIconPosition = new Vector2(-165f, 0f);
    [SerializeField] private Vector2 englishMenuButtonIconPosition = new Vector2(-115f, 0f);
    [SerializeField] private Vector2 menuButtonIconSize = new Vector2(56f, 56f);

    [Header("Typing SFX")]
    [SerializeField, Min(1)] private int typingSfxCharactersPerPlay = 3;
    [Tooltip("Legacy / Unused")]
    [SerializeField, Min(1)] private int typingSfxMinCount = 5;
    [Tooltip("Legacy / Unused")]
    [SerializeField, Min(1)] private int typingSfxMaxCount = 8;
    [Tooltip("Legacy / Unused")]
    [SerializeField, Min(1)] private int typingSfxCharsPerExtra = 24;
    [SerializeField, Min(0f)] private float typingSfxMinInterval = 0.06f;
    [Tooltip("Legacy / Unused")]
    [SerializeField, Min(1)] private int readyQuestionTypingSfxCount = 3;
    [SerializeField] private bool debugIntroDialogueTiming;

    private bool suppressLegacyTypingSfx;
    private bool playingScheduledTypingSfx;
    private AudioClip scheduledTypingSfxClip;
    private bool isTypingLine;
    private string currentTypingLine = string.Empty;

    [Header("Optional UI References")]
    [SerializeField] private RectTransform introDialogueRoot;
    [SerializeField] private CanvasGroup introDialogueRootCanvasGroup;
    [SerializeField] private Image darkBackground;
    [SerializeField] private Image spotlightOverlay;
    [SerializeField] private Image ratImage;
    [SerializeField] private Image speechBubbleImage;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image menuButtonIconImage;
    [SerializeField] private TMP_Text menuButtonFallbackText;
    [SerializeField] private GameObject choiceRoot;
    [SerializeField] private TMP_Text yesText;
    [SerializeField] private Image yesButtonImage;
    [SerializeField] private TMP_Text yesButtonFallbackText;
    [SerializeField] private TMP_Text noText;
    [SerializeField] private Image noButtonImage;
    [SerializeField] private TMP_Text noButtonFallbackText;

    [Header("Rat Sprites")]
    [SerializeField] private Sprite ratFrontSprite;
    [SerializeField] private Sprite ratMouthOpenSprite;

    [Header("Post Stage02 Outro Sprites")]
    [SerializeField] private Sprite resultCheckSprite;
    [SerializeField] private Sprite resultCheckMouthOpenSprite;
    [SerializeField] private Sprite listeningSprite;
    [Min(0f)] [SerializeField] private float listeningLineHoldDuration = 3f;

    [Header("Optional UI Sprites")]
    [SerializeField] private Sprite speechBubbleSprite;
    [SerializeField] private Sprite keyboardDSprite;
    [SerializeField] private Sprite keyboardJSprite;
    [SerializeField] private Sprite gamepadASprite;
    [SerializeField] private Sprite gamepadBSprite;
    [SerializeField] private Sprite keyboardMenuButtonSprite;
    [SerializeField] private Sprite gamepadMenuButtonSprite;

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset koreanTmpFontAsset;
    [SerializeField] private TMP_FontAsset englishTmpFontAsset;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip typingSfx;
    [SerializeField] private AudioClip confirmSfx;
    [SerializeField] private AudioClip choiceAppearSfx;
    [SerializeField] private AudioClip spotlightOnSfx;

    [Header("Timing")]
    [Min(0f)] [SerializeField] private float fadeInDuration = 0.35f;
    [Min(0f)] [SerializeField] private float spotlightFadeDuration = 0.35f;
    [Min(0f)] [SerializeField] private float characterInterval = 0.035f;
    [Min(0f)] [SerializeField] private float typingSfxInterval = 0.07f;
    [Min(0f)] [SerializeField] private float mouthFrameInterval = 0.12f;
    [Min(0f)] [SerializeField] private float fadeOutDuration = 0.5f;

    private Sprite generatedSpotlightSprite;
    private Texture2D generatedSpotlightTexture;
    private bool isPlaying;
    private Coroutine mouthAnimationCoroutine;
    private Sprite mouthClosedSpriteOverride;
    private Sprite mouthOpenSpriteOverride;
    private bool suppressMouthAnimationForCurrentLine;
    private bool suppressConfirmInputForCurrentLine;
    private bool usePostStage02OutroInput;

    public void PrepareIntroBackgroundForTow()
    {
        EnsureRuntimeUI();

        if (introDialogueRoot == null || introDialogueRootCanvasGroup == null)
        {
            Debug.LogWarning("[TitleIntroDialogue] Intro UI could not be prepared.", this);
            return;
        }

        introDialogueRoot.gameObject.SetActive(true);
        introDialogueRootCanvasGroup.alpha = 1f;
        introDialogueRootCanvasGroup.interactable = false;
        introDialogueRootCanvasGroup.blocksRaycasts = false;

        Image background = introBlackBackground != null ? introBlackBackground : darkBackground;
        SetIntroImageVisibility(background, true, 1f);
        if (darkBackground != null && darkBackground != background)
        {
            SetIntroImageVisibility(darkBackground, false, 0f);
        }

        SetIntroImageVisibility(spotlightOverlay, false, 0f);
        SetIntroImageVisibility(ratFloorImage, false, 0f);
        SetIntroImageVisibility(ratImage, false, 0f);
        SetIntroImageVisibility(speechBubbleImage, false, 0f);

        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(false);
        }

        SetMenuButtonVisible(false);
        SetChoiceVisible(false);
        KeepIntroBlackBackgroundVisible();
        introBackgroundPrepared = true;
    }

    public IEnumerator PlayIntroDialogue()
    {
        if (isPlaying)
        {
            yield break;
        }

        isPlaying = true;
        EnsureRuntimeUI();
        EnsureAudioSource();

        if (introDialogueRoot == null || introDialogueRootCanvasGroup == null || dialogueText == null)
        {
            Debug.LogWarning("TitleIntroDialogueController: Dialogue UI could not be prepared.", this);
            isPlaying = false;
            yield break;
        }

        ApplyLocalizedPresentation();
        if (!introBackgroundPrepared)
        {
            PrepareIntroBackgroundForTow();
        }

        KeepIntroBlackBackgroundVisible();
        float ratDelay = Mathf.Max(0f, ratAppearDelayAfterTow);
        if (debugIntroDialogueTiming)
        {
            Debug.Log($"[TitleIntroDialogue] Rat appear delay start: {ratDelay:0.###}s", this);
        }
        if (ratDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(ratDelay);
        }
        if (debugIntroDialogueTiming)
        {
            Debug.Log("[TitleIntroDialogue] Rat appear delay complete", this);
        }

        PrepareDialogueElementsAfterTow();
        yield return FadeCanvasGroup(introDialogueRootCanvasGroup, 0f, 1f, fadeInDuration);

        PlaySfx(spotlightOnSfx);
        if (debugIntroDialogueTiming)
        {
            Debug.Log("[TitleIntroDialogue] spotlightOnSfx / rat appearance", this);
        }
        yield return RevealSpotlightAndRat();

        if (speechBubbleDelayAfterRatAppear > 0f)
        {
            yield return new WaitForSecondsRealtime(speechBubbleDelayAfterRatAppear);
        }

        ShowSpeechBubbleAndDialogue();

        string[] lines = GetIntroLines();
        for (int i = 0; i < lines.Length - 1; i++)
        {
            yield return ShowDialogueLine(lines[i], true, i == 3);
        }

        yield return ShowDialogueLine(lines[lines.Length - 1], false, false);
        yield return RunChoiceLoop();

        SetRatSprite(ratFrontSprite);
        SetChoiceVisible(false);
        SetMenuButtonVisible(false);
        KeepIntroBlackBackgroundVisible();

        introDialogueRootCanvasGroup.blocksRaycasts = false;
        introDialogueRootCanvasGroup.interactable = false;
        HideIntroDialogueForeground();
        KeepIntroBlackBackgroundVisible();
        introBackgroundPrepared = false;
        introBackgroundPrepared = false;
        isPlaying = false;
    }

    public IEnumerator PlayPostStage02Outro()
    {
        if (isPlaying)
        {
            yield break;
        }

        isPlaying = true;
        usePostStage02OutroInput = true;
        EnsureRuntimeUI();
        EnsureAudioSource();

        if (introDialogueRoot == null || introDialogueRootCanvasGroup == null || dialogueText == null)
        {
            Debug.LogWarning(
                "[TitleIntroDialogue] Post Stage02 outro UI could not be prepared.",
                this);
            usePostStage02OutroInput = false;
            isPlaying = false;
            yield break;
        }

        ApplySelectedFont(dialogueText);
        PrepareIntroBackgroundForTow();
        KeepIntroBlackBackgroundVisible();
        introDialogueRoot.SetAsLastSibling();

        float ratDelay = Mathf.Max(0f, ratAppearDelayAfterTow);
        if (ratDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(ratDelay);
        }

        PrepareDialogueElementsAfterTow();
        yield return FadeCanvasGroup(introDialogueRootCanvasGroup, 0f, 1f, fadeInDuration);
        PlaySfx(spotlightOnSfx);
        yield return RevealSpotlightAndRat();

        if (speechBubbleDelayAfterRatAppear > 0f)
        {
            yield return new WaitForSecondsRealtime(speechBubbleDelayAfterRatAppear);
        }

        ShowSpeechBubbleAndDialogue();

        Sprite resultClosed = ResolveOutroSprite(
            resultCheckSprite,
            ratFrontSprite,
            nameof(resultCheckSprite));
        Sprite resultOpen = ResolveOutroSprite(
            resultCheckMouthOpenSprite,
            ratMouthOpenSprite,
            nameof(resultCheckMouthOpenSprite));
        Sprite listening = ResolveOutroSprite(
            listeningSprite,
            ratFrontSprite,
            nameof(listeningSprite));
        string[] lines = GetPostStage02OutroLines();

        for (int i = 0; i < lines.Length; i++)
        {
            bool resultCheckLine = i >= 2 && i <= 4;
            bool listeningLine = i == 7;
            Sprite closedSprite = resultCheckLine
                ? resultClosed
                : listeningLine ? listening : ratFrontSprite;
            Sprite openSprite = resultCheckLine ? resultOpen : ratMouthOpenSprite;
            bool animateMouth = !listeningLine;
            bool waitForNextLine = i < lines.Length - 1;

            yield return ShowPostStage02OutroLine(
                lines[i],
                closedSprite,
                openSprite,
                animateMouth,
                waitForNextLine,
                listeningLine);
        }

        SetRatSprite(ratFrontSprite);
        SetChoiceVisible(false);
        SetMenuButtonVisible(false);
        KeepIntroBlackBackgroundVisible();
        introDialogueRootCanvasGroup.interactable = false;
        introDialogueRootCanvasGroup.blocksRaycasts = false;
        usePostStage02OutroInput = false;
        isPlaying = false;
    }

    private IEnumerator ShowPostStage02OutroLine(
        string line,
        Sprite closedSprite,
        Sprite openSprite,
        bool animateMouth,
        bool waitForNextLine,
        bool addListeningHold)
    {
        SetChoiceVisible(false);
        SetMenuButtonVisible(false);
        ApplySelectedFont(dialogueText);

        yield return TypeLineWithRatSprites(
            line,
            closedSprite,
            openSprite,
            animateMouth,
            !addListeningHold);

        if (addListeningHold)
        {
            float holdDuration = Mathf.Max(0f, listeningLineHoldDuration);
            if (holdDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(holdDuration);
            }

            yield break;
        }

        if (!waitForNextLine)
        {
            yield break;
        }

        yield return WaitForConfirmRelease();
        while (!ConfirmPressedThisFrame())
        {
            yield return null;
        }

        PlayConfirmSfxIfAllowed();
        yield return WaitForConfirmRelease();
    }

    private string[] GetPostStage02OutroLines()
    {
        if (TitleSession.SelectedLanguage == TitleSession.Language.English)
        {
            return new[]
            {
                "Hello. It is nice to see you again.",
                "How was our little performance?",
                "A note just arrived from the birds.\nLet me see....",
                "It says the cuckoos were finally accepted as family and decided to live together.",
                "After that, the woodpecker and the rats went out for a warm meal together.",
                "I am glad it worked out.",
                "Did you enjoy today?",
                "...",
                "Then, we will come back to you again with more stories next time.",
                "Thank you."
            };
        }

        return new[]
        {
            "안녕하세요. 또 뵙네요.",
            "어떠셨나요? 저희들의 무대.",
            "마침 새들에게 쪽지가 왔어요.\n어디보자....",
            "뻐꾸기들은 결국 가족으로 인정받고 같이 살기로 했다네요.",
            "딱다구리와 쥐들은 이후에 국밥집에서 한 잔 했다고 해요.",
            "잘 됐네요.",
            "오늘 즐겨주셨나요?",
            "...",
            "그럼, 다음에 더 많은 내용과 함께 여러분 앞으로 다시 찾아오겠습니다.",
            "감사합니다."
        };
    }

    private Sprite ResolveOutroSprite(Sprite preferred, Sprite fallback, string fieldName)
    {
        if (preferred != null)
        {
            return preferred;
        }

        Debug.LogWarning(
            $"[TitleIntroDialogue] {fieldName} is not assigned. The front-facing rat sprite will be used.",
            this);
        return fallback;
    }

    private IEnumerator RunChoiceLoop()
    {
        int noCount = 0;
        bool isKorean = IsKoreanDialogueLanguage();
        SetChoiceVisible(true);

        while (true)
        {
            if (ActionAPressedThisFrame())
            {
                PlayConfirmSfxIfAllowed();
                SetChoiceVisible(false);
                string yesLine = isKorean
                    ? "좋아요! 무대가 끝난 뒤 다시 봅시다!"
                    : "Great! See you again after the stage!";
                yield return null;
                yield return TypeLine(yesLine);

                float finalLineDelay = Mathf.Max(0f, delayAfterFinalLineComplete);
                if (finalLineDelay > 0f)
                {
                    yield return new WaitForSecondsRealtime(finalLineDelay);
                }

                yield return FadeIntroDialogueForeground(Mathf.Max(0f, introDialogueFadeOutDuration));
                yield break;
            }

            if (!ActionBPressedThisFrame())
            {
                yield return null;
                continue;
            }

            noCount++;
            PlayConfirmSfxIfAllowed();

            bool keepChoiceVisible = noCount >= 3;
            if (!keepChoiceVisible)
            {
                SetChoiceVisible(false);
            }

            string noLine;
            if (noCount == 1)
            {
                noLine = isKorean ? "해주실거죠?" : "You'll play, right?";
            }
            else if (noCount == 2)
            {
                noLine = isKorean ? "해주실...거죠?" : "You'll... play, right?";
            }
            else
            {
                noLine = isKorean
                    ? "너 고집 좀 있구나. 혼나기 싫으면 \"예\"라고 해."
                    : "You're pretty stubborn. Say \"Yes\" unless you want to get scolded.";
            }

            yield return null;
            yield return TypeLine(noLine);

            if (!keepChoiceVisible)
            {
                float responseDelay = Mathf.Max(0f, choiceDelayAfterNoResponse);
                if (responseDelay > 0f)
                {
                    yield return new WaitForSecondsRealtime(responseDelay);
                }

                SetChoiceVisible(true);
            }
        }
    }

    private bool IsKoreanDialogueLanguage()
    {
        string[] lines = GetIntroLines();
        if (lines == null || lines.Length == 0 || string.IsNullOrEmpty(lines[0]))
        {
            return true;
        }

        foreach (char character in lines[0])
        {
            if (character >= '\uAC00' && character <= '\uD7A3')
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator LegacyRunChoiceLoop()
    {
        int noCount = 0;
        bool playedChoiceAppearSfx = false;

        while (true)
        {
            if (!playedChoiceAppearSfx)
            {
        if (choiceDelayAfterReadyQuestion > 0f)
        {
            yield return new WaitForSecondsRealtime(choiceDelayAfterReadyQuestion);
        }

        SetChoiceVisible(true);
        if (debugIntroDialogueTiming)
        {
            Debug.Log("[TitleIntroDialogue] ChoiceRoot visible", this);
        }
                playedChoiceAppearSfx = true;
            }

            yield return WaitForChoiceRelease();
            while (!ActionAPressedThisFrame() && !ActionBPressedThisFrame())
            {
                yield return null;
            }

            bool selectedYes = ActionAPressedThisFrame();
                PlayConfirmSfxIfAllowed();
            SetChoiceVisible(false);
            yield return WaitForChoiceRelease();

            if (selectedYes)
            {
                yield return ShowDialogueLine(GetYesLine(), true, false);
                yield break;
            }

            noCount++;
            yield return ShowDialogueLine(GetNoLine(noCount), false, false);
        }
    }

    private IEnumerator ShowDialogueLine(string line, bool waitForConfirmation, bool showMenuButton)
    {
        SetChoiceVisible(false);
        SetMenuButtonVisible(false);
        ApplySelectedFont(dialogueText);

        yield return TypeLine(line);

        if (!waitForConfirmation)
        {
            yield break;
        }

        yield return WaitForConfirmRelease();
        while (!ConfirmPressedThisFrame())
        {
            yield return null;
        }

                PlayConfirmSfxIfAllowed();
        yield return WaitForConfirmRelease();
    }

    private void ConfigureDialogueTextForWrapping()
    {
        if (dialogueText == null)
        {
            return;
        }

        dialogueText.enableWordWrapping = true;
        dialogueText.overflowMode = TextOverflowModes.Truncate;
        dialogueText.alignment = TextAlignmentOptions.Top;
        dialogueText.lineSpacing = dialogueLineSpacing;
    }

    private void ConfigureDialogueTextLayoutForLineCount(int finalLineCount)
    {
        if (dialogueText == null)
        {
            return;
        }

        // TMP's Top alignment is horizontally centered and vertically top-aligned.
        // Keeping the existing RectTransform untouched makes the first visible line
        // occupy the same final layout position throughout the typewriter effect.
        dialogueText.alignment = TextAlignmentOptions.Top;

        if (debugIntroDialogueTiming)
        {
            Debug.Log($"[TitleIntroDialogue] Fixed dialogue layout line count = {Mathf.Max(1, finalLineCount)}");
        }
    }

    private string WrapDialogueLine(string rawLine)
    {
        if (dialogueText == null || string.IsNullOrEmpty(rawLine))
        {
            return rawLine;
        }

        bool isMenuInstruction = IsMenuInstructionLine(rawLine);
        rawLine = GetMenuInstructionLine(rawLine);
        if (isMenuInstruction)
        {
            return rawLine;
        }

        float maxWidth = Mathf.Max(1f, dialogueMaxTextWidth);
        float rectWidth = dialogueText.rectTransform.rect.width;
        if (rectWidth > 1f)
        {
            maxWidth = Mathf.Min(maxWidth, rectWidth);
        }

        var wrappedLines = new System.Collections.Generic.List<string>();
        string[] paragraphs = rawLine.Replace("\r", string.Empty).Split('\n');
        for (int i = 0; i < paragraphs.Length; i++)
        {
            WrapDialogueParagraph(paragraphs[i], maxWidth, wrappedLines);
        }

        return string.Join("\n", wrappedLines);
    }

    private string GetMenuInstructionLine(string rawLine)
    {
        if (rawLine.Contains("중간에 그만두고 싶으실 때는") && rawLine.Contains("저장되지 않습니다"))
        {
            return "중간에 그만두고 싶으실 때는\n   버튼을 눌러주시면 됩니다.\n단, 진행 내용은 저장되지 않습니다.";
        }

        if (rawLine.IndexOf("quit midway", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
            rawLine.IndexOf("progress will not be saved", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "If you want to quit midway,\npress the      button.\nProgress will not be saved.";
        }

        return rawLine;
    }

    private static bool IsMenuInstructionLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return false;
        }

        return (line.Contains("중간에 그만두고 싶으실 때는") && line.Contains("저장되지 않습니다")) ||
               (line.IndexOf("quit midway", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                line.IndexOf("progress will not be saved", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void ApplyMenuButtonIconLayout(bool isKorean)
    {
        if (menuButtonIconImage == null)
        {
            return;
        }

        RectTransform iconRectTransform = menuButtonIconImage.rectTransform;
        iconRectTransform.anchoredPosition = isKorean
            ? koreanMenuButtonIconPosition
            : englishMenuButtonIconPosition;
        iconRectTransform.sizeDelta = menuButtonIconSize;
    }

    private void WrapDialogueParagraph(
        string paragraph,
        float maxWidth,
        System.Collections.Generic.List<string> wrappedLines)
    {
        if (string.IsNullOrEmpty(paragraph))
        {
            wrappedLines.Add(string.Empty);
            return;
        }

        string[] words = paragraph.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        string currentLine = string.Empty;

        for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
        {
            string word = words[wordIndex];
            string candidate = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;

            if (MeasureDialogueWidth(candidate) <= maxWidth)
            {
                currentLine = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                AddWrappedDialogueLine(wrappedLines, currentLine);
                currentLine = string.Empty;
            }

            if (MeasureDialogueWidth(word) <= maxWidth)
            {
                currentLine = word;
                continue;
            }

            string characterLine = string.Empty;
            for (int characterIndex = 0; characterIndex < word.Length; characterIndex++)
            {
                string characterCandidate = characterLine + word[characterIndex];
                if (!string.IsNullOrEmpty(characterLine) && MeasureDialogueWidth(characterCandidate) > maxWidth)
                {
                    AddWrappedDialogueLine(wrappedLines, characterLine);
                    characterLine = word[characterIndex].ToString();
                }
                else
                {
                    characterLine = characterCandidate;
                }
            }

            currentLine = characterLine;
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            AddWrappedDialogueLine(wrappedLines, currentLine);
        }
    }

    private void AddWrappedDialogueLine(System.Collections.Generic.List<string> wrappedLines, string line)
    {
        int maxLines = Mathf.Max(1, dialogueMaxLines);
        if (wrappedLines.Count < maxLines)
        {
            wrappedLines.Add(line);
            return;
        }

        int lastLineIndex = wrappedLines.Count - 1;
        wrappedLines[lastLineIndex] = string.IsNullOrEmpty(wrappedLines[lastLineIndex])
            ? line
            : wrappedLines[lastLineIndex] + " " + line;
    }

    private float MeasureDialogueWidth(string text)
    {
        return dialogueText == null || string.IsNullOrEmpty(text)
            ? 0f
            : dialogueText.GetPreferredValues(text).x;
    }

    private IEnumerator TypeLineWithRatSprites(
        string line,
        Sprite closedSprite,
        Sprite openSprite,
        bool animateMouth,
        bool allowTypingSkip)
    {
        mouthClosedSpriteOverride = closedSprite;
        mouthOpenSpriteOverride = openSprite;
        suppressMouthAnimationForCurrentLine = !animateMouth;
        suppressConfirmInputForCurrentLine = !allowTypingSkip;
        SetRatSprite(closedSprite);

        yield return TypeLine(line);

        SetRatSprite(closedSprite);
        mouthClosedSpriteOverride = null;
        mouthOpenSpriteOverride = null;
        suppressMouthAnimationForCurrentLine = false;
        suppressConfirmInputForCurrentLine = false;
    }

    private IEnumerator TypeLine(string line)
    {
        bool isMenuInstruction = IsMenuInstructionLine(line);
        SetMenuButtonVisible(false);
        ConfigureDialogueTextForWrapping();
        if (isMenuInstruction)
        {
            line = GetMenuInstructionLine(line);
            ApplyMenuButtonIconLayout(IsKoreanDialogueLanguage());
        }
        else if (autoWrapDialogueText)
        {
            line = WrapDialogueLine(line);
        }

        int finalLineCount = string.IsNullOrEmpty(line) ? 1 : line.Split('\n').Length;
        ConfigureDialogueTextLayoutForLineCount(finalLineCount);

        line = line ?? string.Empty;
        int menuIconRevealIndex = GetMenuButtonIconRevealIndex(line, isMenuInstruction);
        bool menuIconRevealed = false;
        int charactersPerPlay = typingSfxCharactersPerPlay > 0 ? typingSfxCharactersPerPlay : 3;
        float minimumInterval = typingSfxMinInterval >= 0f ? typingSfxMinInterval : 0.06f;
        int previousVisibleCharacterCount = 0;
        int nextSoundCharacterCount = charactersPerPlay;
        float lastTypingSfxTime = float.NegativeInfinity;

        isTypingLine = true;
        currentTypingLine = line;
        suppressLegacyTypingSfx = false;
        if (suppressMouthAnimationForCurrentLine)
        {
            StopMouthAnimation();
            SetRatSprite(GetCurrentMouthClosedSprite());
        }
        else
        {
            StartMouthAnimation();
        }

        IEnumerator legacyRoutine = LegacyTypeLine(line);
        while (legacyRoutine.MoveNext())
        {
            int visibleCharacterCount = CountVisibleTypingCharacters(line);
            int revealedThisStep = visibleCharacterCount - previousVisibleCharacterCount;

            if (!menuIconRevealed && menuIconRevealIndex >= 0 &&
                dialogueText != null && dialogueText.maxVisibleCharacters >= menuIconRevealIndex)
            {
                SetMenuButtonVisible(true);
                menuIconRevealed = true;
            }

            if (revealedThisStep > 1)
            {
                // A large jump means the player skipped the typewriter. Do not emit
                // sounds for characters that were revealed all at once.
                nextSoundCharacterCount = visibleCharacterCount + charactersPerPlay;
                if (debugIntroDialogueTiming)
                {
                    Debug.Log("[TitleIntroDialogue] Typewriter skipped by input", this);
                }
            }
            else if (visibleCharacterCount >= nextSoundCharacterCount &&
                     Time.unscaledTime - lastTypingSfxTime >= minimumInterval)
            {
                PlaySfx(typingSfx);
                lastTypingSfxTime = Time.unscaledTime;
                do
                {
                    nextSoundCharacterCount += charactersPerPlay;
                }
                while (nextSoundCharacterCount <= visibleCharacterCount);
            }

            previousVisibleCharacterCount = visibleCharacterCount;
            yield return legacyRoutine.Current;
        }

        isTypingLine = false;
        currentTypingLine = string.Empty;
        StopMouthAnimation();

        if (!isMenuInstruction)
        {
            SetMenuButtonVisible(false);
        }
    }

    private static int GetMenuButtonIconRevealIndex(string line, bool isMenuInstruction)
    {
        if (!isMenuInstruction || string.IsNullOrEmpty(line))
        {
            return -1;
        }

        int koreanIndex = line.IndexOf("버튼", System.StringComparison.Ordinal);
        if (koreanIndex >= 0)
        {
            return koreanIndex;
        }

        return line.IndexOf("button", System.StringComparison.OrdinalIgnoreCase);
    }

    private int CountVisibleTypingCharacters(string fullLine)
    {
        if (dialogueText == null)
        {
            return 0;
        }

        string displayedText = dialogueText.text ?? string.Empty;
        int visibleLength = displayedText.Length;
        if (displayedText == fullLine)
        {
            visibleLength = Mathf.Min(visibleLength, dialogueText.maxVisibleCharacters);
        }

        int count = 0;
        for (int i = 0; i < visibleLength; i++)
        {
            if (!char.IsWhiteSpace(displayedText[i]))
            {
                count++;
            }
        }

        return count;
    }

    private IEnumerator LegacyScheduledTypeLine(string line)
    {
        line = line ?? string.Empty;

        int requestedCount = GetTypingSfxCount(line);
        float estimatedTypingDuration = Mathf.Max(0f, line.Length * characterInterval);
        float minimumTypingSfxInterval = typingSfxMinInterval >= 0f ? typingSfxMinInterval : 0.12f;
        int playableCount = requestedCount;
        if (!IsReadyQuestion(line) && minimumTypingSfxInterval > 0f)
        {
            int countAllowedByDuration = Mathf.FloorToInt(estimatedTypingDuration / minimumTypingSfxInterval) + 1;
            playableCount = Mathf.Clamp(countAllowedByDuration, 1, requestedCount);
        }

        float scheduledInterval = playableCount > 1
            ? Mathf.Max(minimumTypingSfxInterval, estimatedTypingDuration / (playableCount - 1))
            : 0f;

        if (debugIntroDialogueTiming)
        {
            Debug.Log(
                $"[TitleIntroDialogue] Typing SFX count = {playableCount} / line = {line}",
                this);
        }

        scheduledTypingSfxClip = typingSfx;
        suppressLegacyTypingSfx = true;
        isTypingLine = true;
        currentTypingLine = line;
        IEnumerator legacyRoutine = LegacyTypeLine(line);
        float startedAt = Time.unscaledTime;
        float nextSfxAt = 0f;
        int playedCount = 0;

        while (legacyRoutine.MoveNext())
        {
            float elapsed = Time.unscaledTime - startedAt;
            while (playedCount < playableCount && elapsed + 0.001f >= nextSfxAt)
            {
                PlayScheduledTypingSfx();
                playedCount++;
                nextSfxAt = playedCount * scheduledInterval;
            }

            yield return legacyRoutine.Current;
        }

        float totalElapsed = Time.unscaledTime - startedAt;
        if (debugIntroDialogueTiming && totalElapsed + Mathf.Max(0f, characterInterval) < estimatedTypingDuration)
        {
            Debug.Log("[TitleIntroDialogue] Typewriter skipped by input", this);
        }
        while (playedCount < playableCount && totalElapsed + 0.001f >= nextSfxAt)
        {
            PlayScheduledTypingSfx();
            playedCount++;
            nextSfxAt = playedCount * scheduledInterval;
        }

        isTypingLine = false;
        currentTypingLine = string.Empty;
        suppressLegacyTypingSfx = false;
        scheduledTypingSfxClip = null;
    }

    private int GetTypingSfxCount(string line)
    {
        if (IsReadyQuestion(line))
        {
            return readyQuestionTypingSfxCount > 0 ? readyQuestionTypingSfxCount : 3;
        }

        int minimum = typingSfxMinCount > 0 ? typingSfxMinCount : 5;
        int maximum = typingSfxMaxCount >= minimum ? typingSfxMaxCount : Mathf.Max(minimum, 8);
        int charsPerExtra = typingSfxCharsPerExtra > 0 ? typingSfxCharsPerExtra : 24;
        int extraCount = Mathf.Max(0, line.Length - 1) / charsPerExtra;
        return Mathf.Clamp(minimum + extraCount, minimum, maximum);
    }

    private static bool IsReadyQuestion(string line)
    {
        string normalized = (line ?? string.Empty).Trim();
        return normalized == "그럼 준비 되셨나요?" || normalized == "Are you ready?";
    }

    private void PlayScheduledTypingSfx()
    {
        playingScheduledTypingSfx = true;
        PlaySfx(scheduledTypingSfxClip);
        playingScheduledTypingSfx = false;
    }

    private IEnumerator LegacyTypeLine(string line)
    {
        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;

        float nextCharacterTime = Time.unscaledTime;
        float nextSfxTime = Time.unscaledTime;
        int visibleCharacters = 0;

        while (visibleCharacters < line.Length)
        {
            if (ConfirmPressedThisFrame())
            {
                PlayConfirmSfxIfAllowed();
                dialogueText.maxVisibleCharacters = int.MaxValue;
                yield return WaitForConfirmRelease();
                yield break;
            }

            if (Time.unscaledTime >= nextCharacterTime)
            {
                visibleCharacters++;
                dialogueText.maxVisibleCharacters = visibleCharacters;

                char currentCharacter = line[visibleCharacters - 1];
                if (!char.IsWhiteSpace(currentCharacter) && Time.unscaledTime >= nextSfxTime)
                {
                    nextSfxTime = Time.unscaledTime + Mathf.Max(0f, typingSfxInterval);
                }

                nextCharacterTime = Time.unscaledTime + Mathf.Max(0f, characterInterval);
            }

            yield return null;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
    }

    private void StartMouthAnimation()
    {
        StopMouthAnimation();

        Sprite closedSprite = GetCurrentMouthClosedSprite();
        Sprite openSprite = GetCurrentMouthOpenSprite();
        if (ratImage == null || closedSprite == null || openSprite == null)
        {
            SetRatSprite(closedSprite);
            return;
        }

        mouthAnimationCoroutine = StartCoroutine(MouthAnimationLoop(closedSprite, openSprite));
    }

    private void StopMouthAnimation()
    {
        if (mouthAnimationCoroutine != null)
        {
            StopCoroutine(mouthAnimationCoroutine);
            mouthAnimationCoroutine = null;
        }

        Sprite closedSprite = GetCurrentMouthClosedSprite();
        if (ratImage != null && closedSprite != null)
        {
            SetRatSprite(closedSprite);
        }
    }

    private IEnumerator MouthAnimationLoop(Sprite closedSprite, Sprite openSprite)
    {
        bool mouthOpen = false;
        float interval = Mathf.Max(0.01f, mouthFrameInterval);

        while (true)
        {
            mouthOpen = !mouthOpen;
            SetRatSprite(mouthOpen ? openSprite : closedSprite);
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    private Sprite GetCurrentMouthClosedSprite()
    {
        return mouthClosedSpriteOverride != null ? mouthClosedSpriteOverride : ratFrontSprite;
    }

    private Sprite GetCurrentMouthOpenSprite()
    {
        return mouthOpenSpriteOverride != null ? mouthOpenSpriteOverride : ratMouthOpenSprite;
    }

    private void PrepareForPlayback()
    {
        introDialogueRoot.gameObject.SetActive(true);
        introDialogueRoot.SetAsLastSibling();
        introDialogueRootCanvasGroup.alpha = 0f;
        introDialogueRootCanvasGroup.interactable = true;
        introDialogueRootCanvasGroup.blocksRaycasts = true;
        dialogueText.text = string.Empty;
        dialogueText.maxVisibleCharacters = int.MaxValue;
        SetChoiceVisible(false);
        SetMenuButtonVisible(false);
        SetRatSprite(ratFrontSprite);
        SetImageAlpha(spotlightOverlay, 0f);
        SetImageAlpha(ratImage, 0f);
    }

    private void KeepIntroBlackBackgroundVisible()
    {
        if (introDialogueRoot == null)
        {
            return;
        }

        introDialogueRoot.gameObject.SetActive(true);
        if (introDialogueRootCanvasGroup != null)
        {
            introDialogueRootCanvasGroup.alpha = 1f;
            introDialogueRootCanvasGroup.interactable = false;
            introDialogueRootCanvasGroup.blocksRaycasts = false;
        }

        Image background = introBlackBackground != null ? introBlackBackground : darkBackground;
        if (background == null)
        {
            GameObject fallback = new GameObject(
                "IntroBlackBackground_RuntimeFallback",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform rect = (RectTransform)fallback.transform;
            rect.SetParent(introDialogueRoot, false);
            introBlackBackground = fallback.GetComponent<Image>();
            background = introBlackBackground;
        }

        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.localScale = Vector3.one;
        backgroundRect.SetSiblingIndex(0);

        background.gameObject.SetActive(true);
        background.enabled = true;
        background.raycastTarget = false;
        background.color = Color.black;
    }

    private IEnumerator FadeIntroDialogueForeground(float duration)
    {
        Color ratFloorColor = ratFloorImage != null ? ratFloorImage.color : Color.white;
        Color ratColor = ratImage != null ? ratImage.color : Color.white;
        Color speechBubbleColor = speechBubbleImage != null ? speechBubbleImage.color : Color.white;
        Color menuButtonColor = menuButtonIconImage != null ? menuButtonIconImage.color : Color.white;
        Color dialogueColor = dialogueText != null ? dialogueText.color : Color.white;
        CanvasGroup choiceCanvasGroup = choiceRoot != null ? choiceRoot.GetComponent<CanvasGroup>() : null;
        Graphic choiceGraphic = choiceRoot != null ? choiceRoot.GetComponent<Graphic>() : null;
        float choiceAlpha = choiceCanvasGroup != null
            ? choiceCanvasGroup.alpha
            : choiceGraphic != null ? choiceGraphic.color.a : 0f;

        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alphaMultiplier = 1f - Mathf.Clamp01(elapsed / duration);

                SetImageAlpha(ratFloorImage, ratFloorColor, alphaMultiplier);
                SetImageAlpha(ratImage, ratColor, alphaMultiplier);
                SetImageAlpha(speechBubbleImage, speechBubbleColor, alphaMultiplier);
                SetImageAlpha(menuButtonIconImage, menuButtonColor, alphaMultiplier);

                if (dialogueText != null)
                {
                    Color color = dialogueColor;
                    color.a *= alphaMultiplier;
                    dialogueText.color = color;
                }

                SetChoiceRootAlpha(choiceCanvasGroup, choiceGraphic, choiceAlpha * alphaMultiplier);

                yield return null;
            }
        }

        HideIntroDialogueForeground();
        SetRatSprite(ratFrontSprite);

        RestoreImageColor(ratFloorImage, ratFloorColor);
        RestoreImageColor(ratImage, ratColor);
        RestoreImageColor(speechBubbleImage, speechBubbleColor);
        RestoreImageColor(menuButtonIconImage, menuButtonColor);

        if (dialogueText != null)
        {
            dialogueText.color = dialogueColor;
        }

        SetChoiceRootAlpha(choiceCanvasGroup, choiceGraphic, choiceAlpha);

        KeepIntroBlackBackgroundVisible();
    }

    private static void SetImageAlpha(Image image, Color baseColor, float alphaMultiplier)
    {
        if (image == null)
        {
            return;
        }

        Color color = baseColor;
        color.a *= alphaMultiplier;
        image.color = color;
    }

    private static void RestoreImageColor(Image image, Color color)
    {
        if (image != null)
        {
            image.color = color;
        }
    }

    private static void SetChoiceRootAlpha(CanvasGroup canvasGroup, Graphic graphic, float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            return;
        }

        if (graphic != null)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }

    private void HideIntroDialogueForeground()
    {
        SetIntroImageVisibility(ratFloorImage, false, 0f);
        SetIntroImageVisibility(ratImage, false, 0f);
        SetIntroImageVisibility(speechBubbleImage, false, 0f);
        SetMenuButtonVisible(false);
        SetChoiceVisible(false);
        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(false);
        }
    }

    public void ReleaseIntroBackground()
    {
        HideIntroDialogueForeground();
        Image background = introBlackBackground != null ? introBlackBackground : darkBackground;
        if (background != null)
        {
            SetIntroImageVisibility(background, false, 0f);
        }

        if (introDialogueRootCanvasGroup != null)
        {
            introDialogueRootCanvasGroup.alpha = 0f;
        }

        if (introDialogueRoot != null)
        {
            introDialogueRoot.gameObject.SetActive(false);
        }

        introBackgroundPrepared = false;
    }

    private static void SetIntroImageVisibility(Image image, bool visible, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
        image.raycastTarget = false;
        image.gameObject.SetActive(visible);
    }

    private void PrepareDialogueElementsAfterTow()
    {
        introDialogueRootCanvasGroup.alpha = 1f;
        SetIntroImageVisibility(spotlightOverlay, false, 0f);

        if (ratFloorImage != null)
        {
            ratFloorImage.sprite = ratFloorSprite;
            bool hasFloorSprite = ratFloorSprite != null;
            SetIntroImageVisibility(ratFloorImage, hasFloorSprite, hasFloorSprite ? 1f : 0f);
        }

        if (ratImage != null)
        {
            ratImage.sprite = ratFrontSprite;
            bool hasRatSprite = ratFrontSprite != null;
            SetIntroImageVisibility(ratImage, hasRatSprite, hasRatSprite ? 1f : 0f);
        }

        SetIntroImageVisibility(speechBubbleImage, false, 0f);

        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(false);
        }

        SetMenuButtonVisible(false);
        SetChoiceVisible(false);
    }

    private void ShowSpeechBubbleAndDialogue()
    {
        SetIntroImageVisibility(speechBubbleImage, true, 1f);
        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(true);
        }
    }

    private IEnumerator RevealSpotlightAndRat()
    {
        // Spotlight reveal is intentionally retired. Rat and floor are made visible
        // immediately after the tow transition by PrepareDialogueElementsAfterTow().
        yield break;

        float duration = Mathf.Max(0f, spotlightFadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.SmoothStep(0f, 1f, NormalizedTime(elapsed, duration));
            SetImageAlpha(spotlightOverlay, alpha);
            SetImageAlpha(ratImage, alpha);
            yield return null;
        }

        SetImageAlpha(spotlightOverlay, 1f);
        SetImageAlpha(ratImage, 1f);
    }

    private void ApplyLocalizedPresentation()
    {
        bool english = TitleSession.SelectedLanguage == TitleSession.Language.English;
        yesText.text = english ? "Yes" : "예";
        noText.text = english ? "No" : "아니오";
        ApplySelectedFont(dialogueText);
        ApplySelectedFont(yesText);
        ApplySelectedFont(noText);
        ApplySelectedFont(menuButtonFallbackText);
        ApplySelectedFont(yesButtonFallbackText);
        ApplySelectedFont(noButtonFallbackText);

        bool gamepad = TitleSession.SelectedInputMode == TitleSession.InputMode.Xbox;
        ApplyButtonPresentation(yesButtonImage, yesButtonFallbackText,
            gamepad ? gamepadASprite : keyboardDSprite, gamepad ? "A" : "D");
        ApplyButtonPresentation(noButtonImage, noButtonFallbackText,
            gamepad ? gamepadBSprite : keyboardJSprite, gamepad ? "B" : "J");

        Sprite menuSprite = gamepad ? gamepadMenuButtonSprite : keyboardMenuButtonSprite;
        string menuFallback = gamepad ? "[Menu]" : "[Esc]";
        ApplyButtonPresentation(menuButtonIconImage, menuButtonFallbackText, menuSprite, menuFallback);
    }

    private string[] GetIntroLines()
    {
        string[] lines = GetLegacyIntroLines();
        if (lines == null || lines.Length <= 3)
        {
            return lines;
        }

        string firstLine = lines[0] ?? string.Empty;
        bool isKorean = false;
        for (int i = 0; i < firstLine.Length; i++)
        {
            char character = firstLine[i];
            if (character >= '\uAC00' && character <= '\uD7A3')
            {
                isKorean = true;
                break;
            }
        }

        lines[3] = isKorean
            ? "중간에 그만두고 싶으실 때는 를 눌러주시면 됩니다. 단, 진행 내용은 저장되지 않습니다."
            : "If you want to quit midway, press . However, your progress will not be saved.";
        return lines;
    }

    private string[] GetLegacyIntroLines()
    {
        bool english = TitleSession.SelectedLanguage == TitleSession.Language.English;
        string menuLabel = GetMenuLabelForDialogue();
        if (english)
        {
            return new[]
            {
                "Welcome to the world of PeckBeat!",
                "This is a rhythm game played with two buttons.",
                "There are three stages, and they progress in order.",
                $"If you want to quit midway, press {menuLabel}. However, your progress will not be saved.",
                "Are you ready?"
            };
        }

        return new[]
        {
            "부리박자의 세계에 오신걸 환영합니다!",
            "이 게임은 두 개의 버튼을 사용해서 즐기는 리듬게임입니다.",
            "총 3개의 무대가 준비되어 있으며, 순서대로 진행됩니다.",
            $"중간에 그만두고 싶으실 때는 \"{menuLabel}\" 를 눌러주시면 됩니다. 단, 진행 내용은 저장되지 않습니다.",
            "그럼 준비 되셨나요?"
        };
    }

    private string GetYesLine()
    {
        return TitleSession.SelectedLanguage == TitleSession.Language.English
            ? "Great! See you again after the stage!"
            : "좋아요! 무대가 끝난 뒤 다시 봅시다!";
    }

    private string GetNoLine(int noCount)
    {
        bool english = TitleSession.SelectedLanguage == TitleSession.Language.English;
        if (noCount == 1)
        {
            return english ? "You'll play, right?" : "해주실거죠?";
        }

        if (noCount == 2)
        {
            return english ? "You'll... play, right?" : "해주실...거죠?";
        }

        return english
            ? "You're pretty stubborn. Say \"Yes\" unless you want to get scolded."
            : "너 고집 좀 있구나. 혼나기 싫으면 \"예\"라고 해.";
    }

    private string GetMenuLabelForDialogue()
    {
        bool gamepad = TitleSession.SelectedInputMode == TitleSession.InputMode.Xbox;
        Sprite selectedSprite = gamepad ? gamepadMenuButtonSprite : keyboardMenuButtonSprite;
        if (selectedSprite != null)
        {
            return TitleSession.SelectedLanguage == TitleSession.Language.English
                ? "the menu button shown below"
                : "아래 메뉴 버튼";
        }

        return gamepad ? "[Menu]" : "[Esc]";
    }

    private bool ConfirmPressedThisFrame()
    {
        if (suppressConfirmInputForCurrentLine)
        {
            return false;
        }

        if (usePostStage02OutroInput)
        {
            return PostStage02OutroConfirmPressedThisFrame();
        }

        return ActionAPressedThisFrame() || ActionBPressedThisFrame();
    }

    private static bool PostStage02OutroConfirmPressedThisFrame()
    {
        bool keyboardPressed = Keyboard.current != null &&
            (Keyboard.current.dKey.wasPressedThisFrame ||
             Keyboard.current.jKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame);
        bool gamepadPressed = Gamepad.current != null &&
            (Gamepad.current.buttonSouth.wasPressedThisFrame ||
             Gamepad.current.buttonEast.wasPressedThisFrame ||
             Gamepad.current.startButton.wasPressedThisFrame);
        return keyboardPressed || gamepadPressed;
    }

    private static bool PostStage02OutroConfirmIsPressed()
    {
        bool keyboardPressed = Keyboard.current != null &&
            (Keyboard.current.dKey.isPressed ||
             Keyboard.current.jKey.isPressed ||
             Keyboard.current.enterKey.isPressed ||
             Keyboard.current.numpadEnterKey.isPressed ||
             Keyboard.current.spaceKey.isPressed);
        bool gamepadPressed = Gamepad.current != null &&
            (Gamepad.current.buttonSouth.isPressed ||
             Gamepad.current.buttonEast.isPressed ||
             Gamepad.current.startButton.isPressed);
        return keyboardPressed || gamepadPressed;
    }

    private bool ActionAPressedThisFrame()
    {
        return TitleSession.SelectedInputMode switch
        {
            TitleSession.InputMode.Xbox => Gamepad.current != null &&
                                           Gamepad.current.buttonSouth.wasPressedThisFrame,
            TitleSession.InputMode.KeyboardMouse => Keyboard.current != null &&
                                                   Keyboard.current.dKey.wasPressedThisFrame,
            _ => (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) ||
                 (Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame)
        };
    }

    private bool ActionBPressedThisFrame()
    {
        return TitleSession.SelectedInputMode switch
        {
            TitleSession.InputMode.Xbox => Gamepad.current != null &&
                                           Gamepad.current.buttonEast.wasPressedThisFrame,
            TitleSession.InputMode.KeyboardMouse => Keyboard.current != null &&
                                                   Keyboard.current.jKey.wasPressedThisFrame,
            _ => (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) ||
                 (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
        };
    }

    private bool ActionAIsPressed()
    {
        return TitleSession.SelectedInputMode switch
        {
            TitleSession.InputMode.Xbox => Gamepad.current != null && Gamepad.current.buttonSouth.isPressed,
            TitleSession.InputMode.KeyboardMouse => Keyboard.current != null && Keyboard.current.dKey.isPressed,
            _ => (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed) ||
                 (Keyboard.current != null && Keyboard.current.dKey.isPressed)
        };
    }

    private bool ActionBIsPressed()
    {
        return TitleSession.SelectedInputMode switch
        {
            TitleSession.InputMode.Xbox => Gamepad.current != null && Gamepad.current.buttonEast.isPressed,
            TitleSession.InputMode.KeyboardMouse => Keyboard.current != null && Keyboard.current.jKey.isPressed,
            _ => (Gamepad.current != null && Gamepad.current.buttonEast.isPressed) ||
                 (Keyboard.current != null && Keyboard.current.jKey.isPressed)
        };
    }

    private IEnumerator WaitForConfirmRelease()
    {
        if (usePostStage02OutroInput)
        {
            while (PostStage02OutroConfirmIsPressed())
            {
                yield return null;
            }

            yield break;
        }

        while (ActionBIsPressed())
        {
            yield return null;
        }
    }

    private IEnumerator WaitForChoiceRelease()
    {
        while (ActionAIsPressed() || ActionBIsPressed())
        {
            yield return null;
        }
    }

    private void EnsureRuntimeUI()
    {
        if (!HasCompleteUIReferences())
        {
            if (introDialogueRoot != null)
            {
                Debug.LogWarning(
                    "TitleIntroDialogueController: Some UI references are missing. Using runtime fallback UI.",
                    this);
            }

            BuildRuntimeUI();
        }

        if (introDialogueRootCanvasGroup == null && introDialogueRoot != null)
        {
            introDialogueRootCanvasGroup = introDialogueRoot.GetComponent<CanvasGroup>();
            if (introDialogueRootCanvasGroup == null)
            {
                introDialogueRootCanvasGroup = introDialogueRoot.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (spotlightOverlay != null && spotlightOverlay.sprite == null)
        {
            generatedSpotlightSprite = CreateSpotlightSprite();
            spotlightOverlay.sprite = generatedSpotlightSprite;
        }

        if (speechBubbleImage != null)
        {
            speechBubbleImage.sprite = speechBubbleSprite;
            speechBubbleImage.color = speechBubbleSprite != null
                ? Color.white
                : new Color(1f, 1f, 1f, 0.92f);
        }
    }

    private bool HasCompleteUIReferences()
    {
        return introDialogueRoot != null &&
               introDialogueRootCanvasGroup != null &&
               introBlackBackground != null &&
               ratFloorImage != null &&
               ratImage != null &&
               speechBubbleImage != null &&
               dialogueText != null &&
               menuButtonIconImage != null &&
               choiceRoot != null &&
               yesText != null &&
               yesButtonImage != null &&
               noText != null &&
               noButtonImage != null;
    }

    private bool HasLegacyCompleteUIReferences()
    {
        return introDialogueRoot != null
            && darkBackground != null
            && spotlightOverlay != null
            && ratImage != null
            && speechBubbleImage != null
            && dialogueText != null
            && menuButtonIconImage != null
            && menuButtonFallbackText != null
            && choiceRoot != null
            && yesText != null
            && yesButtonImage != null
            && yesButtonFallbackText != null
            && noText != null
            && noButtonImage != null
            && noButtonFallbackText != null;
    }

    private void BuildRuntimeUI()
    {
        GameObject rootObject = new GameObject(
            "IntroDialogueRoot_Runtime",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));
        rootObject.transform.SetParent(null, false);

        Canvas canvas = rootObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = rootObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        introDialogueRoot = rootObject.GetComponent<RectTransform>();
        StretchToParent(introDialogueRoot);
        introDialogueRootCanvasGroup = rootObject.GetComponent<CanvasGroup>();

        darkBackground = CreateImage("DarkBackground", introDialogueRoot,
            new Color(0.015f, 0.02f, 0.035f, 0.97f));
        StretchToParent(darkBackground.rectTransform);

        spotlightOverlay = CreateImage("SpotlightOverlay", introDialogueRoot,
            new Color(1f, 0.94f, 0.68f, 0.72f));
        SetRect(spotlightOverlay.rectTransform, new Vector2(-430f, 70f), new Vector2(980f, 980f));

        ratImage = CreateImage("RatImage", introDialogueRoot, Color.white);
        ratImage.preserveAspect = true;
        SetRect(ratImage.rectTransform, new Vector2(-440f, -90f), new Vector2(560f, 700f));

        speechBubbleImage = CreateImage("SpeechBubbleImage", introDialogueRoot,
            new Color(1f, 1f, 1f, 0.92f));
        SetRect(speechBubbleImage.rectTransform, new Vector2(310f, 125f), new Vector2(920f, 430f));

        dialogueText = CreateText("DialogueText", speechBubbleImage.rectTransform, 42f,
            new Color(0.08f, 0.08f, 0.1f, 1f), TextAlignmentOptions.MidlineLeft);
        StretchToParent(dialogueText.rectTransform, new Vector2(65f, 65f), new Vector2(-65f, -65f));

        menuButtonIconImage = CreateImage("MenuButtonIconImage", introDialogueRoot, Color.white);
        menuButtonIconImage.preserveAspect = true;
        SetRect(menuButtonIconImage.rectTransform, new Vector2(310f, -145f), new Vector2(120f, 72f));
        menuButtonFallbackText = CreateText("MenuButtonFallbackText", menuButtonIconImage.rectTransform,
            30f, Color.white, TextAlignmentOptions.Center);
        StretchToParent(menuButtonFallbackText.rectTransform);

        choiceRoot = new GameObject("ChoiceRoot", typeof(RectTransform));
        RectTransform choiceRect = choiceRoot.GetComponent<RectTransform>();
        choiceRect.SetParent(introDialogueRoot, false);
        SetRect(choiceRect, new Vector2(310f, -210f), new Vector2(760f, 190f));

        BuildChoiceGroup(choiceRect, "YesGroup", new Vector2(-210f, 0f),
            out yesText, out yesButtonImage, out yesButtonFallbackText);
        BuildChoiceGroup(choiceRect, "NoGroup", new Vector2(210f, 0f),
            out noText, out noButtonImage, out noButtonFallbackText);
    }

    private void BuildChoiceGroup(
        RectTransform parent,
        string objectName,
        Vector2 anchoredPosition,
        out TMP_Text label,
        out Image buttonImage,
        out TMP_Text fallbackText)
    {
        GameObject groupObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform groupRect = groupObject.GetComponent<RectTransform>();
        groupRect.SetParent(parent, false);
        SetRect(groupRect, anchoredPosition, new Vector2(280f, 180f));

        label = CreateText("Label", groupRect, 42f, Color.white, TextAlignmentOptions.Center);
        SetRect(label.rectTransform, new Vector2(0f, 45f), new Vector2(280f, 70f));

        buttonImage = CreateImage("ButtonImage", groupRect, Color.white);
        buttonImage.preserveAspect = true;
        SetRect(buttonImage.rectTransform, new Vector2(0f, -45f), new Vector2(96f, 72f));

        fallbackText = CreateText("FallbackText", buttonImage.rectTransform, 36f,
            Color.white, TextAlignmentOptions.Center);
        StretchToParent(fallbackText.rectTransform);
    }

    private Sprite CreateSpotlightSprite()
    {
        return null;

        const int size = 128;
        generatedSpotlightTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "RuntimeSpotlightGradient",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = 1f - Mathf.SmoothStep(0.15f, 1f, distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        generatedSpotlightTexture.SetPixels(pixels);
        generatedSpotlightTexture.Apply();
        return Sprite.Create(generatedSpotlightTexture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f);
    }

    private void EnsureAudioSource()
    {
        if (sfxSource != null)
        {
            return;
        }

        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
    }

    private void PlayConfirmSfxIfAllowed()
    {
        bool lineFullyVisible = dialogueText != null &&
                                dialogueText.text == currentTypingLine &&
                                dialogueText.maxVisibleCharacters >= currentTypingLine.Length;

        if (isTypingLine && !lineFullyVisible)
        {
            if (debugIntroDialogueTiming)
            {
                Debug.Log("[TitleIntroDialogue] Typewriter skipped without confirmSfx", this);
            }
            return;
        }

        isTypingLine = false;

        PlaySfx(confirmSfx);
        if (debugIntroDialogueTiming)
        {
            Debug.Log("[TitleIntroDialogue] Completed line advance / confirmSfx", this);
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == typingSfx && suppressLegacyTypingSfx && !playingScheduledTypingSfx)
        {
            return;
        }

        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void ApplySelectedFont(TMP_Text target)
    {
        if (target == null)
        {
            return;
        }

        TMP_FontAsset selectedFont = TitleSession.SelectedLanguage == TitleSession.Language.English
            ? englishTmpFontAsset
            : koreanTmpFontAsset;
        if (selectedFont != null)
        {
            target.font = selectedFont;
        }
        else if (target.font == null)
        {
            target.font = TMP_Settings.defaultFontAsset;
        }
    }

    private static void ApplyButtonPresentation(
        Image image,
        TMP_Text fallbackText,
        Sprite sprite,
        string fallback)
    {
        if (image != null)
        {
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.18f);
        }

        if (fallbackText != null)
        {
            fallbackText.gameObject.SetActive(sprite == null);
            fallbackText.text = fallback;
        }
    }

    private void SetRatSprite(Sprite sprite)
    {
        if (ratImage == null)
        {
            return;
        }

        ratImage.sprite = sprite;
        ratImage.color = sprite != null
            ? new Color(1f, 1f, 1f, ratImage.color.a)
            : new Color(0.75f, 0.78f, 0.82f, ratImage.color.a);
    }

    private void SetChoiceVisible(bool visible)
    {
        if (choiceRoot != null)
        {
            choiceRoot.SetActive(visible);
        }
    }

    private void SetMenuButtonVisible(bool visible)
    {
        if (menuButtonFallbackText != null)
        {
            menuButtonFallbackText.gameObject.SetActive(false);
        }

        if (menuButtonIconImage != null)
        {
            menuButtonIconImage.gameObject.SetActive(visible && menuButtonIconImage.sprite != null);
        }

        return;

        if (menuButtonIconImage != null)
        {
            menuButtonIconImage.gameObject.SetActive(visible);
        }
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float seconds)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0f, seconds);
        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f,
                NormalizedTime(elapsed, duration)));
            yield return null;
        }

        canvasGroup.alpha = to;
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

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static void StretchToParent(RectTransform target)
    {
        StretchToParent(target, Vector2.zero, Vector2.zero);
    }

    private static void StretchToParent(RectTransform target, Vector2 offsetMin, Vector2 offsetMax)
    {
        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.pivot = new Vector2(0.5f, 0.5f);
        target.offsetMin = offsetMin;
        target.offsetMax = offsetMax;
        target.localScale = Vector3.one;
    }

    private static void SetRect(RectTransform target, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = anchoredPosition;
        target.sizeDelta = sizeDelta;
        target.localScale = Vector3.one;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private static float NormalizedTime(float elapsed, float duration)
    {
        return duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
    }

    private void OnDestroy()
    {
        if (generatedSpotlightSprite != null)
        {
            Destroy(generatedSpotlightSprite);
        }

        if (generatedSpotlightTexture != null)
        {
            Destroy(generatedSpotlightTexture);
        }
    }
}
