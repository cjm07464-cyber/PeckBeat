using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Stage02SculptingController : MonoBehaviour
{
    private enum TargetPhase
    {
        Active,
        Holding,
        CompletedApproach,
        Resolved,
        Exiting
    }

    private sealed class RuntimeTarget
    {
        public Stage02SculptingChart.Event ChartEvent;
        public Transform Transform;
        public Stage02SculptTarget Visual;
        public TargetPhase Phase;
        public int HitCount;
        public double ResultEndSongTime;
        public double ExitStartSongTime;
        public double ExitEndSongTime;
        public Vector3 ExitStartPosition;
        public int HoldPeckSoundCount;
        public double HoldStartSongTime;
        public double NextHoldPeckSoundTime;
    }

    [Header("Stage Data")]
    [SerializeField] private Stage02SculptingChart chart;

    [Header("Shared Systems")]
    [SerializeField] private PeckBeatInput input;
    [SerializeField] private PeckBeatRhythmManager rhythmManager;
    [SerializeField] private Stage02AudioController audioController;
    [SerializeField] private Stage02HoldCueBubbleUI holdCueBubble;

    [Header("Stage 02 Actors")]
    [SerializeField] private Stage02WoodpeckerController woodpecker;
    [SerializeField] private Stage02ForemanRatVisual foremanRat;
    [SerializeField] private Stage02SculptTarget normalTargetPrefab;
    [SerializeField] private Stage02SculptTarget holdTargetPrefab;
    [SerializeField] private Transform targetContainer;

    [Header("Target Slots")]
    [SerializeField] private Transform slot1;
    [SerializeField] private Transform slot2;
    [SerializeField] private Transform slot3;

    [Header("Incoming Target Signal")]
    [Min(0f)] [SerializeField] private float whistleVisualDuration = 0.18f;
    [Min(0f)] [SerializeField] private float whistleRepeatInterval = 0.22f;
    [Min(0f)] [SerializeField] private float holdWhistleFallbackDuration = 0.8f;

    [Header("Hold Cue")]
    [Min(0f)] [SerializeField] private float holdCueLeadSeconds = 0.35f;
    [SerializeField] private Vector3 cueWorldOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Normal Target Judgement")]
    [Min(0f)] [SerializeField] private float perfectWindowSeconds = 0.08f;

    [Header("Hold Target Judgement")]
    [Min(0f)] [SerializeField] private float holdPerfectWindowSeconds = 0.08f;
    [Min(0f)] [SerializeField] private float holdGoodWindowSeconds = 0.22f;
    [Min(0f)] [SerializeField] private float holdReleaseGraceSeconds = 0.12f;

    [Header("Stage Clear Transition")]
    [Min(0f)] [SerializeField] private float stageEndToTitleTransitionDelay = 1f;

    private readonly List<Stage02SculptingChart.Event> orderedEvents = new();
    private readonly List<RuntimeTarget> activeTargets = new();
    private int nextEventIndex;
    private RuntimeTarget holdCueTarget;
    private bool stage02ClearTransitionStarted;

    public int SuccessCount { get; private set; }
    public int MissCount { get; private set; }

    private void Start()
    {
        if (foremanRat == null)
        {
            foremanRat = UnityEngine.Object.FindFirstObjectByType<Stage02ForemanRatVisual>(
                FindObjectsInactive.Include);
        }

        if (holdCueBubble == null)
        {
            holdCueBubble = UnityEngine.Object.FindFirstObjectByType<Stage02HoldCueBubbleUI>(
                FindObjectsInactive.Include);
        }

        if (holdCueBubble != null)
        {
            holdCueBubble.SetFollowTarget(
                foremanRat != null ? foremanRat.transform : transform);
            holdCueBubble.SetWorldOffset(cueWorldOffset);
            holdCueBubble.Hide();
        }

        if (chart == null)
        {
            Debug.LogWarning("[Stage02] Sculpting Chart is not assigned.", this);
            return;
        }

        orderedEvents.AddRange(chart.Events);
        orderedEvents.Sort((left, right) => left.targetBeat.CompareTo(right.targetBeat));
    }

    private void Update()
    {
        if (TryStartStage02ClearTransition())
        {
            return;
        }

        if (!HasRequiredReferences())
        {
            return;
        }

        double songTime = rhythmManager.SongTime;

        SpawnDueTargets(songTime);
        UpdateTargetMovement(songTime);
        UpdateHoldCue(songTime);
        ProcessInput(songTime);
        UpdateHoldPeckAudio(songTime);
        EvaluateTargetTiming(songTime);
        woodpecker.UpdateHoldPeck(songTime);
    }

    private bool TryStartStage02ClearTransition()
    {
        if (stage02ClearTransitionStarted)
        {
            return true;
        }

        if (rhythmManager == null || !rhythmManager.HasSongEnded)
        {
            return false;
        }

        stage02ClearTransitionStarted = true;
        StartCoroutine(Stage02ClearTransitionRoutine());
        return true;
    }

    private IEnumerator Stage02ClearTransitionRoutine()
    {
        float delay = Mathf.Max(0f, stageEndToTitleTransitionDelay);
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        TitleSession.RequestStage02ClearToPostIntro();
    }

    private bool HasRequiredReferences()
    {
        return chart != null
            && input != null
            && rhythmManager != null
            && woodpecker != null
            && normalTargetPrefab != null
            && holdTargetPrefab != null
            && targetContainer != null
            && slot1 != null
            && slot2 != null
            && slot3 != null;
    }

    private void SpawnDueTargets(double songTime)
    {
        while (nextEventIndex < orderedEvents.Count)
        {
            Stage02SculptingChart.Event chartEvent = orderedEvents[nextEventIndex];
            double spawnTime = chart.GetBeatTime(chartEvent.targetBeat - chart.LeadBeats);
            if (songTime < spawnTime)
            {
                break;
            }

            SpawnTarget(chartEvent);
            nextEventIndex++;
        }
    }

    private void SpawnTarget(Stage02SculptingChart.Event chartEvent)
    {
        Stage02SculptTarget prefab = chartEvent.targetType == Stage02SculptingChart.TargetType.Hold
            ? holdTargetPrefab
            : normalTargetPrefab;

        Stage02SculptTarget instance = Instantiate(prefab, slot1.position, slot1.rotation, targetContainer);
        if (chartEvent.targetType == Stage02SculptingChart.TargetType.Hold)
        {
            Debug.Log($"[Stage02] Hold Spawn beat={chartEvent.targetBeat:0.###}", this);
            instance.ConfigureHold();
        }
        else
        {
            instance.ConfigureNormal(chartEvent.requiredHits);
        }

        instance.BeginApproachVisual();

        if (chartEvent.targetType == Stage02SculptingChart.TargetType.Normal)
        {
            StartCoroutine(SignalIncomingTarget(chartEvent.requiredHits));
        }
        else
        {
            SignalIncomingHoldTarget();
        }

        activeTargets.Add(new RuntimeTarget
        {
            ChartEvent = chartEvent,
            Transform = instance.transform,
            Visual = instance,
            Phase = TargetPhase.Active
        });
    }

    private IEnumerator SignalIncomingTarget(int requiredHits)
    {
        int whistleCount = Mathf.Clamp(requiredHits, 1, 2);
        for (int i = 0; i < whistleCount; i++)
        {
            audioController?.PlayWhistleShort();
            foremanRat?.PlayWhistle(Mathf.Max(0f, whistleVisualDuration));

            if (i < whistleCount - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, whistleRepeatInterval));
            }
        }
    }

    private void SignalIncomingHoldTarget()
    {
        audioController?.PlayWhistleLong();

        float duration = audioController != null
            ? audioController.GetWhistleLongDuration()
            : 0f;
        if (duration <= 0f)
        {
            duration = Mathf.Max(0f, holdWhistleFallbackDuration);
        }

        if (duration > 0f)
        {
            foremanRat?.PlayWhistle(duration);
        }
    }

    private void UpdateTargetMovement(double songTime)
    {
        for (int i = activeTargets.Count - 1; i >= 0; i--)
        {
            RuntimeTarget target = activeTargets[i];
            switch (target.Phase)
            {
                case TargetPhase.Active:
                case TargetPhase.Holding:
                    UpdateApproachPosition(target, songTime);
                    break;

                case TargetPhase.CompletedApproach:
                    UpdateApproachPosition(target, songTime);
                    if (songTime >= chart.GetBeatTime(target.ChartEvent.targetBeat))
                    {
                        target.Transform.position = slot2.position;
                        target.Visual.UpdateApproachVisual(1f);
                        BeginExit(target, songTime);
                    }
                    break;

                case TargetPhase.Resolved:
                    target.Transform.position = slot2.position;
                    target.Visual.UpdateApproachVisual(1f);
                    if (songTime >= target.ResultEndSongTime)
                    {
                        BeginExit(target, songTime);
                    }
                    break;

                case TargetPhase.Exiting:
                    UpdateExitPosition(target, songTime);
                    if (songTime >= target.ExitEndSongTime)
                    {
                        HideHoldCue(target);
                        Destroy(target.Transform.gameObject);
                        activeTargets.RemoveAt(i);
                    }
                    break;
            }
        }
    }

    private void UpdateApproachPosition(RuntimeTarget target, double songTime)
    {
        double targetTime = chart.GetBeatTime(target.ChartEvent.targetBeat);
        double spawnTime = chart.GetBeatTime(target.ChartEvent.targetBeat - chart.LeadBeats);
        float progress = Mathf.Clamp01((float)((songTime - spawnTime) / Math.Max(targetTime - spawnTime, 0.0001d)));
        target.Transform.position = Vector3.LerpUnclamped(slot1.position, slot2.position, progress);
        target.Visual.UpdateApproachVisual(progress);
    }

    private void BeginExit(RuntimeTarget target, double songTime)
    {
        target.Phase = TargetPhase.Exiting;
        target.ExitStartSongTime = songTime;
        target.ExitEndSongTime = songTime + chart.ExitDurationBeats * chart.BeatInterval;

        if (target.ChartEvent.targetType == Stage02SculptingChart.TargetType.Hold)
        {
            target.Visual.BeginExitVisual();
        }

        target.ExitStartPosition = target.Transform.position;

        if (target.ChartEvent.targetType == Stage02SculptingChart.TargetType.Normal)
        {
            target.Visual.BeginExitVisual();
        }
    }

    private void UpdateExitPosition(RuntimeTarget target, double songTime)
    {
        double duration = Math.Max(target.ExitEndSongTime - target.ExitStartSongTime, 0.0001d);
        float progress = Mathf.Clamp01((float)((songTime - target.ExitStartSongTime) / duration));
        target.Transform.position = Vector3.LerpUnclamped(target.ExitStartPosition, slot3.position, progress);
    }

    private void ProcessInput(double songTime)
    {
        if (input.ActionAPressedThisFrame())
        {
            ProcessNormalInput(songTime);
        }

        if (input.ActionBPressedThisFrame())
        {
            ProcessHoldStart(songTime);
        }

        if (input.ActionBReleasedThisFrame())
        {
            ProcessHoldRelease(songTime);
        }
    }

    private void ProcessNormalInput(double songTime)
    {
        RuntimeTarget target = FindNormalTargetInWindow(
            songTime,
            out double targetTime,
            out double absoluteTimingError);
        if (target == null)
        {
            return;
        }

        double perfectWindow = Math.Min(
            Math.Max(0d, perfectWindowSeconds),
            Math.Max(0d, chart.JudgeWindowSeconds));
        bool isPerfect = absoluteTimingError <= perfectWindow;
        bool isEarlySuccess = songTime < targetTime;

        target.HitCount++;
        target.Visual.ApplyNormalHitVisual(target.HitCount);
        woodpecker.PlayPeck();
        audioController?.PlayNormalPeck();

        int requiredHits = Mathf.Clamp(target.ChartEvent.requiredHits, 1, 2);
        if (requiredHits == 2 && target.HitCount == 1)
        {
            audioController?.PlayTwoHitFirstBreak();
        }

        if (target.HitCount >= requiredHits)
        {
            audioController?.PlayBreakFinal();
            target.Visual.ShowSculpture(target.ChartEvent.sculptureType);

            bool skipIdleOnArrival = requiredHits == 1 && isEarlySuccess;
            if (skipIdleOnArrival)
            {
                target.Visual.SetSkipIdleOnArrival(true);
            }

            ResolveSuccess(target, songTime, skipIdleOnArrival);
        }

        if (isPerfect)
        {
            woodpecker.PlayPerfectSmile();
            audioController?.PlayPerfect();
        }
    }

    private RuntimeTarget FindNormalTargetInWindow(
        double songTime,
        out double bestTargetTime,
        out double bestAbsoluteTimingError)
    {
        RuntimeTarget bestTarget = null;
        double bestDifference = double.MaxValue;
        bestTargetTime = 0d;
        bestAbsoluteTimingError = double.MaxValue;

        foreach (RuntimeTarget target in activeTargets)
        {
            if (target.Phase != TargetPhase.Active
                || target.ChartEvent.targetType != Stage02SculptingChart.TargetType.Normal)
            {
                continue;
            }

            double judgeTime = chart.GetBeatTime(target.ChartEvent.targetBeat + target.HitCount);
            double difference = Math.Abs(songTime - judgeTime);
            if (difference <= chart.JudgeWindowSeconds && difference < bestDifference)
            {
                bestTarget = target;
                bestDifference = difference;
                bestTargetTime = judgeTime;
                bestAbsoluteTimingError = difference;
            }
        }

        return bestTarget;
    }

    private void ProcessHoldStart(double songTime)
    {
        if (FindHoldingTarget() != null) return;
        RuntimeTarget target = FindHoldStartTargetInWindow(
            songTime,
            out double absoluteTimingError);
        if (target == null)
        {
            RuntimeTarget earlyTarget = holdCueTarget;
            if (earlyTarget != null
                && earlyTarget.Phase == TargetPhase.Active
                && songTime < chart.GetBeatTime(earlyTarget.ChartEvent.targetBeat)
                    - GetHoldStartWindowSeconds())
            {
                ResolveHoldFailureAndExit(earlyTarget, songTime, false);
            }

            return;
        }

        HideHoldCue(target);

        double holdStartTime = chart.GetBeatTime(target.ChartEvent.targetBeat);

        target.Phase = TargetPhase.Holding;
        target.HoldPeckSoundCount = 0;
        target.HoldStartSongTime = holdStartTime;
        target.NextHoldPeckSoundTime = holdStartTime;
        target.Visual.CaptureHoldMoveStart(slot3.position - slot2.position);

        woodpecker.StartHoldPeck(holdStartTime, chart.BeatInterval);

        bool isPerfect = absoluteTimingError <= GetHoldPerfectWindowSeconds();
        if (isPerfect)
        {
            woodpecker.PlayPerfectSmile();
            audioController?.PlayPerfect();
            Debug.Log("[Stage02] Hold Perfect", this);
        }
        else
        {
            Debug.Log("[Stage02] Hold Good", this);
        }
    }

    private void UpdateHoldPeckAudio(double songTime)
    {
        foreach (RuntimeTarget target in activeTargets)
        {
            if (target.Phase != TargetPhase.Holding)
            {
                continue;
            }

            double holdDuration = Math.Max(
                GetHoldEndTime(target) - target.HoldStartSongTime,
                0.0001d);
            float holdProgress = Mathf.Clamp01(
                (float)((songTime - target.HoldStartSongTime) / holdDuration));
            target.Visual.SetHoldProgress01(holdProgress);

            while (target.HoldPeckSoundCount < 16
                && songTime >= target.NextHoldPeckSoundTime)
            {
                audioController?.PlayHoldPeck();
                target.Visual.PlayHoldChipOrShakePulse();

                target.HoldPeckSoundCount++;
                target.NextHoldPeckSoundTime = chart.GetBeatTime(
                    target.ChartEvent.targetBeat + target.HoldPeckSoundCount * 0.125f);
            }
        }
    }

    private RuntimeTarget FindHoldStartTargetInWindow(
        double songTime,
        out double bestAbsoluteTimingError)
    {
        RuntimeTarget bestTarget = null;
        double bestDifference = double.MaxValue;
        bestAbsoluteTimingError = double.MaxValue;

        foreach (RuntimeTarget target in activeTargets)
        {
            if (target.Phase != TargetPhase.Active
                || target.ChartEvent.targetType != Stage02SculptingChart.TargetType.Hold)
            {
                continue;
            }

            double judgeTime = chart.GetBeatTime(target.ChartEvent.targetBeat);
            double difference = Math.Abs(songTime - judgeTime);
            // A late start must still leave time to display the first of eight strikes.
            double startWindow = GetHoldStartWindowSeconds();
            if (difference <= startWindow && difference < bestDifference)
            {
                bestTarget = target;
                bestDifference = difference;
                bestAbsoluteTimingError = difference;
            }
        }

        return bestTarget;
    }

    private double GetHoldStartWindowSeconds()
    {
        return Math.Max(0d, holdGoodWindowSeconds);
    }

    private double GetHoldPerfectWindowSeconds()
    {
        return Math.Min(
            Math.Max(0d, holdPerfectWindowSeconds),
            GetHoldStartWindowSeconds());
    }

    private void ProcessHoldRelease(double songTime)
    {
        RuntimeTarget target = FindHoldingTarget();
        if (target == null)
        {
            return;
        }

        double holdEndTime = GetHoldEndTime(target);
        if (songTime >= holdEndTime - GetHoldReleaseGraceSeconds())
        {
            target.Visual.ShowBaseballBat();
            ResolveSuccess(target, songTime);
        }
        else
        {
            ResolveHoldFailureAndExit(target, songTime, true);
        }
    }

    private double GetHoldReleaseGraceSeconds()
    {
        return Math.Max(0d, holdReleaseGraceSeconds);
    }

    private RuntimeTarget FindHoldingTarget()
    {
        foreach (RuntimeTarget target in activeTargets)
        {
            if (target.Phase == TargetPhase.Holding)
            {
                return target;
            }
        }

        return null;
    }

    private void EvaluateTargetTiming(double songTime)
    {
        foreach (RuntimeTarget target in activeTargets)
        {
            if (target.Phase == TargetPhase.Active)
            {
                double judgeTime = target.ChartEvent.targetType == Stage02SculptingChart.TargetType.Normal
                    ? chart.GetBeatTime(target.ChartEvent.targetBeat + target.HitCount)
                    : chart.GetBeatTime(target.ChartEvent.targetBeat);

                double lateWindow = target.ChartEvent.targetType == Stage02SculptingChart.TargetType.Hold
                    ? GetHoldStartWindowSeconds()
                    : chart.JudgeWindowSeconds;
                if (songTime > judgeTime + lateWindow)
                {
                    ResolveMiss(target, songTime);
                }
            }
            else if (target.Phase == TargetPhase.Holding)
            {
                if (!input.ActionBIsPressed())
                {
                    if (songTime >= GetHoldEndTime(target) - GetHoldReleaseGraceSeconds())
                    {
                        target.Visual.ShowBaseballBat();
                        ResolveSuccess(target, songTime);
                    }
                    else
                    {
                        ResolveHoldFailureAndExit(target, songTime, true);
                    }
                }
                else if (songTime >= GetHoldEndTime(target))
                {
                    target.Visual.ShowBaseballBat();
                    ResolveSuccess(target, songTime);
                }
            }
        }
    }

    private double GetHoldEndTime(RuntimeTarget target)
    {
        return chart.GetBeatTime(target.ChartEvent.targetBeat + 2f);
    }

    private void ResolveSuccess(
        RuntimeTarget target,
        double songTime,
        bool continueApproachUntilSlot2 = false)
    {
        if (target.Phase == TargetPhase.CompletedApproach
            || target.Phase == TargetPhase.Resolved
            || target.Phase == TargetPhase.Exiting)
        {
            return;
        }

        HideHoldCue(target);
        StopHoldVisualIfNeeded(target);
        target.Phase = continueApproachUntilSlot2
            ? TargetPhase.CompletedApproach
            : TargetPhase.Resolved;
        target.ResultEndSongTime = songTime + chart.ResultHoldBeats * chart.BeatInterval;
        SuccessCount++;
        audioController?.PlayComplete();
        Debug.Log($"[Stage02][SUCCESS] beat: {target.ChartEvent.targetBeat:0.###}", this);
    }

    private void ResolveMiss(RuntimeTarget target, double songTime)
    {
        if (target.Phase == TargetPhase.Resolved || target.Phase == TargetPhase.Exiting)
        {
            return;
        }

        HideHoldCue(target);
        if (target.ChartEvent.targetType == Stage02SculptingChart.TargetType.Hold)
        {
            target.Visual.ResetHoldVisual();
        }

        StopHoldVisualIfNeeded(target);
        woodpecker.PlayMissPeckFace();
        target.Phase = TargetPhase.Resolved;
        target.ResultEndSongTime = songTime + chart.ResultHoldBeats * chart.BeatInterval;
        MissCount++;
        audioController?.PlayMiss();
        Debug.Log($"[Stage02][MISS] beat: {target.ChartEvent.targetBeat:0.###}", this);
    }

    private void ResolveHoldFailureAndExit(
        RuntimeTarget target,
        double songTime,
        bool preserveCurrentHoldVisual)
    {
        if (target == null
            || target.Phase == TargetPhase.Resolved
            || target.Phase == TargetPhase.Exiting)
        {
            return;
        }

        HideHoldCue(target);
        if (!preserveCurrentHoldVisual)
        {
            target.Visual.ResetHoldVisual();
        }

        StopHoldVisualIfNeeded(target);
        woodpecker.PlayMissPeckFace();
        MissCount++;
        audioController?.PlayMiss();
        Debug.Log($"[Stage02][MISS] beat: {target.ChartEvent.targetBeat:0.###}", this);
        BeginExit(target, songTime);
    }

    private void UpdateHoldCue(double songTime)
    {
        if (holdCueBubble == null)
        {
            return;
        }

        if (holdCueTarget != null)
        {
            if (holdCueTarget.Phase == TargetPhase.Active
                && activeTargets.Contains(holdCueTarget))
            {
                return;
            }

            HideHoldCue(holdCueTarget);
        }

        RuntimeTarget nextHoldTarget = null;
        double earliestTargetTime = double.MaxValue;
        foreach (RuntimeTarget target in activeTargets)
        {
            if (target.Phase != TargetPhase.Active
                || target.ChartEvent.targetType != Stage02SculptingChart.TargetType.Hold)
            {
                continue;
            }

            double targetTime = chart.GetBeatTime(target.ChartEvent.targetBeat);
            double showTime = targetTime - Mathf.Max(0f, holdCueLeadSeconds);
            if (songTime < showTime || targetTime >= earliestTargetTime)
            {
                continue;
            }

            nextHoldTarget = target;
            earliestTargetTime = targetTime;
        }

        if (nextHoldTarget == null)
        {
            return;
        }

        holdCueTarget = nextHoldTarget;
        holdCueBubble.Show(
            TitleSession.SelectedInputMode,
            TitleSession.SelectedLanguage);
    }

    private void HideHoldCue(RuntimeTarget target = null)
    {
        if (target != null && holdCueTarget != target)
        {
            return;
        }

        holdCueBubble?.Hide();
        holdCueTarget = null;
    }

    private void StopHoldVisualIfNeeded(RuntimeTarget target)
    {
        if (target.ChartEvent.targetType == Stage02SculptingChart.TargetType.Hold)
        {
            woodpecker.StopHoldPeck();
        }
    }

    private void OnDisable()
    {
        HideHoldCue();
        if (woodpecker != null) woodpecker.StopHoldPeck();
    }
}
