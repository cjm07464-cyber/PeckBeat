using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stage01FeedingChart", menuName = "Peck Beat/Stage 1/Feeding Chart")]
public sealed class Stage01FeedingChart : ScriptableObject
{
    public enum BirdType
    {
        Chick,
        Cuckoo
    }

    [Serializable]
    public sealed class Event
    {
        [Min(0f)] public float targetBeat;
        public BirdType targetType;
        [Min(0.25f)] public float secondPeckBeatOffset = 1f;
    }

    [Header("Music Grid")]
    [Min(1f)] [SerializeField] private float bpm = 110f;
    [Min(0f)] [SerializeField] private float firstBeatOffset = 0.458f;
    [Range(-0.1f, 0.1f)] [SerializeField] private float offsetAdjustment;

    [Header("Stage 1 Timing")]
    [Min(2f)] [SerializeField] private float leadBeats = 2f;
    [Min(0.01f)] [SerializeField] private float judgeWindowSeconds = 0.14f;
    [Min(0f)] [SerializeField] private float resultHoldBeats = 0.5f;
    [Min(0.01f)] [SerializeField] private float exitDurationBeats = 1f;

    [Header("Chart Events")]
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
}
