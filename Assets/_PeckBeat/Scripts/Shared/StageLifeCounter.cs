using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StageLifeCounter : MonoBehaviour
{
    public const int MaximumLives = 10;

    [Header("Lives")]
    [Range(0, MaximumLives)]
    [SerializeField] private int startingLives = MaximumLives;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private Image lifeImage;
    [SerializeField] private TMP_FontAsset koreanFont;
    [SerializeField] private TMP_FontAsset englishFont;

    private readonly HashSet<object> processedMissSources = new();
    private bool depletionEventRaised;

    public int CurrentLives { get; private set; }
    public int MaxLives => MaximumLives;
    public bool IsGameOver => CurrentLives <= 0;
    public Image LifeImage => lifeImage;

    public event Action<int, int> OnLivesChanged;
    public event Action OnLivesDepleted;

    private void Awake()
    {
        ResetLives();
    }

    private void OnEnable()
    {
        RefreshDisplay();
    }

    public void ResetLives()
    {
        processedMissSources.Clear();
        depletionEventRaised = false;
        CurrentLives = Mathf.Clamp(startingLives, 0, MaximumLives);
        RefreshDisplay();
        OnLivesChanged?.Invoke(CurrentLives, MaximumLives);

        if (CurrentLives == 0)
        {
            RaiseLivesDepletedOnce();
        }
    }

    public bool RegisterMiss(object missSource = null)
    {
        if (IsGameOver)
        {
            return false;
        }

        if (missSource != null && !processedMissSources.Add(missSource))
        {
            return false;
        }

        CurrentLives = Mathf.Clamp(CurrentLives - 1, 0, MaximumLives);
        RefreshDisplay();
        OnLivesChanged?.Invoke(CurrentLives, MaximumLives);

        if (CurrentLives == 0)
        {
            RaiseLivesDepletedOnce();
        }

        return true;
    }

    public void RefreshDisplay()
    {
        if (livesText == null)
        {
            return;
        }

        bool useEnglish = TitleSession.SelectedLanguage == TitleSession.Language.English;
        int displayedLives = Application.isPlaying
            ? CurrentLives
            : Mathf.Clamp(startingLives, 0, MaximumLives);
        livesText.text = displayedLives.ToString();

        TMP_FontAsset selectedFont = useEnglish ? englishFont : koreanFont;
        if (selectedFont != null)
        {
            livesText.font = selectedFont;
        }
    }

    private void RaiseLivesDepletedOnce()
    {
        if (depletionEventRaised)
        {
            return;
        }

        depletionEventRaised = true;
        OnLivesDepleted?.Invoke();
    }
}
