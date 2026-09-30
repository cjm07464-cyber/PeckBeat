using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class Stage02SculptingController : MonoBehaviour
{
    private enum TargetPhase
    {
        Active,
        Holding,
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
        public double NextHoldPeckSoundTime;
    }

    [Header("Stage Data")]
    [SerializeField] private Stage02SculptingChart chart;

    [Header("Shared Systems")]
    [SerializeField] private PeckBeatInput input;
    [SerializeField] private PeckBeatRhythmManager rhythmManager;
    [SerializeField] private Stage02AudioController audioController;

    [Header("Stage 02 Actors")]
    [SerializeField] private Stage02WoodpeckerController woodpecker;
    [SerializeField] private Stage02SculptTarget normalTargetPrefab;
    [SerializeField] private Stage02SculptTarget holdTargetPrefab;
    [SerializeField] private Transform targetContainer;

    [Header("Target Slots")]
    [SerializeField] private Transform slot1;
    [SerializeField] private Transform slot2;
    [SerializeField] private Transform slot3;

    private readonly List<Stage02SculptingChart.Event> orderedEvents = new();
    private readonly List<RuntimeTarget> activeTargets = new();
    private int nextEventIndex;

    public int SuccessCount { get; private set; }
    public int MissCount { get; private set; }

    private void Start()
    {
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
        if (!HasRequiredReferences())
        {
            return;
        }

        double songTime = rhythmManager.SongTime;

        SpawnDueTargets(songTime);
        UpdateTargetMovement(songTime);
        ProcessInput(songTime);
        UpdateHoldPeckAudio(songTime);
        EvaluateTargetTiming(songTime);
        woodpecker.UpdateHoldPeck(songTime);
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

        activeTargets.Add(new RuntimeTarget
        {
            ChartEvent = chartEvent,
            Transform = instance.transform,
            Visual = instance,
            Phase = TargetPhase.Active
        });
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

                case TargetPhase.Resolved:
                    target.Transform.position = slot2.position;
                    if (songTime >= target.ResultEndSongTime)
                    {
                        BeginExit(target, songTime);
                    }
                    break;

                case TargetPhase.Exiting:
                    UpdateExitPosition(target, songTime);
                    if (songTime >= target.ExitEndSongTime)
                    {
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
    }

    private void BeginExit(RuntimeTarget target, double songTime)
    {
        target.Phase = TargetPhase.Exiting;
        target.ExitStartSongTime = songTime;
        target.ExitEndSongTime = songTime + chart.ExitDurationBeats * chart.BeatInterval;
        target.ExitStartPosition = target.Transform.position;
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
        RuntimeTarget target = FindNormalTargetInWindow(songTime);
        if (target == null)
        {
            return;
        }

        target.HitCount++;
        woodpecker.PlayPeck();
        audioController?.PlayPeck();

        int requiredHits = Mathf.Clamp(target.ChartEvent.requiredHits, 1, 2);
        if (target.HitCount >= requiredHits)
        {
            target.Visual.ShowSculpture(target.ChartEvent.sculptureType);
            ResolveSuccess(target, songTime);
        }
    }

    private RuntimeTarget FindNormalTargetInWindow(double songTime)
    {
        RuntimeTarget bestTarget = null;
        double bestDifference = double.MaxValue;

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
            }
        }

        return bestTarget;
    }

    private void ProcessHoldStart(double songTime)
    {
        if (FindHoldingTarget() != null) return;
        RuntimeTarget target = FindHoldStartTargetInWindow(songTime);
        if (target == null)
        {
            return;
        }

        double holdStartTime = chart.GetBeatTime(target.ChartEvent.targetBeat);

        target.Phase = TargetPhase.Holding;
        target.HoldPeckSoundCount = 0;
        target.NextHoldPeckSoundTime = holdStartTime;

        woodpecker.StartHoldPeck(holdStartTime, chart.BeatInterval);
    }

    private void UpdateHoldPeckAudio(double songTime)
    {
        foreach (RuntimeTarget target in activeTargets)
        {
            if (target.Phase != TargetPhase.Holding)
            {
                continue;
            }

            while (target.HoldPeckSoundCount < 16
                && songTime >= target.NextHoldPeckSoundTime)
            {
                audioController?.PlayHoldPeck();

                target.HoldPeckSoundCount++;
                target.NextHoldPeckSoundTime = chart.GetBeatTime(
                    target.ChartEvent.targetBeat + target.HoldPeckSoundCount * 0.125f);
            }
        }
    }

    private RuntimeTarget FindHoldStartTargetInWindow(double songTime)
    {
        RuntimeTarget bestTarget = null;
        double bestDifference = double.MaxValue;

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
            double startWindow = Math.Min(chart.JudgeWindowSeconds, chart.BeatInterval * 0.125d);
            if (difference <= startWindow && difference < bestDifference)
            {
                bestTarget = target;
                bestDifference = difference;
            }
        }

        return bestTarget;
    }

    private void ProcessHoldRelease(double songTime)
    {
        RuntimeTarget target = FindHoldingTarget();
        if (target == null)
        {
            return;
        }

        double holdEndTime = GetHoldEndTime(target);
        if (songTime >= holdEndTime)
        {
            target.Visual.ShowBaseballBat();
            ResolveSuccess(target, songTime);
        }
        else
        {
            ResolveMiss(target, songTime);
        }
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

                if (songTime > judgeTime + chart.JudgeWindowSeconds)
                {
                    ResolveMiss(target, songTime);
                }
            }
            else if (target.Phase == TargetPhase.Holding)
            {
                if (!input.ActionBIsPressed())
                {
                    ResolveMiss(target, songTime);
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

    private void ResolveSuccess(RuntimeTarget target, double songTime)
    {
        if (target.Phase == TargetPhase.Resolved || target.Phase == TargetPhase.Exiting)
        {
            return;
        }

        StopHoldVisualIfNeeded(target);
        target.Phase = TargetPhase.Resolved;
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

        StopHoldVisualIfNeeded(target);
        target.Phase = TargetPhase.Resolved;
        target.ResultEndSongTime = songTime + chart.ResultHoldBeats * chart.BeatInterval;
        MissCount++;
        audioController?.PlayMiss();
        Debug.Log($"[Stage02][MISS] beat: {target.ChartEvent.targetBeat:0.###}", this);
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
        if (woodpecker != null) woodpecker.StopHoldPeck();
    }
}
