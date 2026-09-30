using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stage02SculptingChart", menuName = "Peck Beat/Stage 2/Sculpting Chart")]
public sealed class Stage02SculptingChart : ScriptableObject
{
    public enum TargetType
    {
        Normal,
        Hold
    }

    public enum SculptureType
    {
        SculptureA,
        SculptureB
    }

    [Serializable]
    public sealed class Event
    {
        [Min(0f)] public float targetBeat;
        public TargetType targetType;
        [Range(1, 2)] public int requiredHits = 1;
        [Tooltip("Hold always lasts 2 beats; this value is updated to Target Beat + 2.")]
        [Min(0f)] public float holdEndBeat;
        public SculptureType sculptureType;
    }

    [Header("Music Grid")]
    [Min(1f)] [SerializeField] private float bpm = 120f;
    [Min(0f)] [SerializeField] private float firstBeatOffset;
    [Range(-0.1f, 0.1f)] [SerializeField] private float offsetAdjustment;

    [Header("Stage 2 Timing")]
    [Min(0.01f)] [SerializeField] private float leadBeats = 2f;
    [Min(0.01f)] [SerializeField] private float judgeWindowSeconds = 0.14f;
    [Min(0f)] [SerializeField] private float resultHoldBeats = 0.5f;
    [Min(0.01f)] [SerializeField] private float exitDurationBeats = 1f;

    [Header("Target Events")]
    [SerializeField] private List<Event> events = new();

    public float BeatInterval => 60f / bpm;
    public float LeadBeats => leadBeats;
    public float JudgeWindowSeconds => judgeWindowSeconds;
    public float ResultHoldBeats => resultHoldBeats;
    public float ExitDurationBeats => exitDurationBeats;
    public IReadOnlyList<Event> Events => events;

    public double GetBeatTime(float beat)
    {
        return firstBeatOffset + offsetAdjustment + beat * BeatInterval;
    }

    private void OnValidate()
    {
        foreach (Event target in events)
        {
            if (target != null && target.targetType == TargetType.Hold)
                target.holdEndBeat = target.targetBeat + 2f;
        }
    }
}
