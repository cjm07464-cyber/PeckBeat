using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageGoalCounter : MonoBehaviour
{
    public enum GoalType
    {
        Stage01Feeding,
        Stage01Cuckoo,
        Stage02OneHit,
        Stage02TwoHit,
        Stage02Hold
    }

    [Header("Stage01 Goals (0 = Count Only)")]
    [Min(0)] [SerializeField] private int stage01FeedingGoal;
    [Min(0)] [SerializeField] private int stage01CuckooGoal;

    [Header("Stage02 Goals (0 = Count Only)")]
    [Min(0)] [SerializeField] private int stage02OneHitGoal;
    [Min(0)] [SerializeField] private int stage02TwoHitGoal;
    [Min(0)] [SerializeField] private int stage02HoldGoal;

    [Header("Stage01 TMP Texts")]
    [SerializeField] private TMP_Text stage01FeedingText;
    [SerializeField] private TMP_Text stage01CuckooText;

    [Header("Stage02 TMP Texts")]
    [SerializeField] private TMP_Text stage02OneHitText;
    [SerializeField] private TMP_Text stage02TwoHitText;
    [SerializeField] private TMP_Text stage02HoldText;

    [Header("Language Fonts (Optional)")]
    [SerializeField] private TMP_FontAsset koreanFont;
    [SerializeField] private TMP_FontAsset englishFont;

    private readonly HashSet<object> processedSuccessSources = new();

    public int Stage01FeedingCount { get; private set; }
    public int Stage01CuckooCount { get; private set; }
    public int Stage02OneHitCount { get; private set; }
    public int Stage02TwoHitCount { get; private set; }
    public int Stage02HoldCount { get; private set; }

    public event Action<GoalType, int> OnCounterChanged;

    private void Awake()
    {
        ResetCounters();
    }

    private void OnEnable()
    {
        RefreshDisplay();
    }

    public void ResetCounters()
    {
        processedSuccessSources.Clear();
        Stage01FeedingCount = 0;
        Stage01CuckooCount = 0;
        Stage02OneHitCount = 0;
        Stage02TwoHitCount = 0;
        Stage02HoldCount = 0;
        RefreshDisplay();
    }

    public bool RegisterSuccess(GoalType goalType, object successSource = null)
    {
        if (successSource != null && !processedSuccessSources.Add(successSource))
        {
            return false;
        }

        int newCount;
        switch (goalType)
        {
            case GoalType.Stage01Feeding:
                newCount = ++Stage01FeedingCount;
                break;
            case GoalType.Stage01Cuckoo:
                newCount = ++Stage01CuckooCount;
                break;
            case GoalType.Stage02OneHit:
                newCount = ++Stage02OneHitCount;
                break;
            case GoalType.Stage02TwoHit:
                newCount = ++Stage02TwoHitCount;
                break;
            case GoalType.Stage02Hold:
                newCount = ++Stage02HoldCount;
                break;
            default:
                return false;
        }

        RefreshDisplay();
        OnCounterChanged?.Invoke(goalType, newCount);
        return true;
    }

    public void RefreshDisplay()
    {
        bool english = TitleSession.SelectedLanguage == TitleSession.Language.English;
        TMP_FontAsset selectedFont = english ? englishFont : koreanFont;

        ApplyFontIfAssigned(stage01FeedingText, selectedFont);
        ApplyFontIfAssigned(stage01CuckooText, selectedFont);
        ApplyFontIfAssigned(stage02OneHitText, selectedFont);
        ApplyFontIfAssigned(stage02TwoHitText, selectedFont);
        ApplyFontIfAssigned(stage02HoldText, selectedFont);

        SetLabeledCounterText(
            stage01FeedingText,
            english ? "Feed" : "먹이주기",
            Stage01FeedingCount,
            stage01FeedingGoal);
        SetLabeledCounterText(
            stage01CuckooText,
            english ? "Chase Away" : "쫓아내기",
            Stage01CuckooCount,
            stage01CuckooGoal);
        SetCounterText(
            stage02OneHitText,
            Stage02OneHitCount,
            stage02OneHitGoal);
        SetCounterText(
            stage02TwoHitText,
            Stage02TwoHitCount,
            stage02TwoHitGoal);
        SetCounterText(
            stage02HoldText,
            Stage02HoldCount,
            stage02HoldGoal);
    }

    private static void SetLabeledCounterText(
        TMP_Text target,
        string label,
        int count,
        int goal)
    {
        if (target == null)
        {
            return;
        }

        string value = goal > 0 ? $"{count} / {goal}" : count.ToString();
        target.text = $"<color=#FFFFFF>{label}</color> <color=#FFE400>{value}</color>";
    }

    private static void SetCounterText(TMP_Text target, int count, int goal)
    {
        if (target == null)
        {
            return;
        }

        target.text = goal > 0
            ? $"{count} / {goal}"
            : count.ToString();
    }

    private static void ApplyFontIfAssigned(TMP_Text target, TMP_FontAsset font)
    {
        if (target != null && font != null)
        {
            target.font = font;
        }
    }
}
