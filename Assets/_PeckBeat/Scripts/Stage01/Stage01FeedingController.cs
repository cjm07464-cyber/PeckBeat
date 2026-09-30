using System.Collections.Generic;
using UnityEngine;

public sealed class Stage01FeedingController : MonoBehaviour
{
    private enum BirdPhase
    {
        Approaching,
        WaitingSecondPeck,
        Resolved,
        Exiting
    }

    private sealed class RuntimeBird
    {
        public Stage01FeedingChart.Event ChartEvent;
        public Transform Transform;
        public ChickSpriteController Chick;
        public CuckooSpriteController Cuckoo;
        public BirdPhase Phase;
        public double ResultEndTime;
        public double ExitEndTime;
        public Vector3 ExitStartPosition;

        public bool IsChick => Chick != null;
        public bool IsCuckoo => Cuckoo != null;
    }

    [Header("Stage Data")]
    [SerializeField] private Stage01FeedingChart chart;

    [Header("Shared Systems")]
    [SerializeField] private PeckBeatInput input;
    [SerializeField] private PeckBeatRhythmManager rhythmManager;
    [SerializeField] private Stage01AudioController audioController;

    [Header("Characters")]
    [SerializeField] private MotherBirdSpriteController motherBird;
    [SerializeField] private GameObject chickPrefab;
    [SerializeField] private GameObject cuckooPrefab;
    [SerializeField] private Transform birdContainer;

    [Header("Nest Slots")]
    [SerializeField] private Transform slot1;
    [SerializeField] private Transform slot2;
    [SerializeField] private Transform slot3;
    [SerializeField] private Transform exitSlot;

    private readonly List<RuntimeBird> activeBirds = new();
    private readonly List<Stage01FeedingChart.Event> runtimeEvents = new();
    private int nextEventIndex;

    public int SuccessCount { get; private set; }
    public int MissCount { get; private set; }

    private void Start()
    {
        if (chart == null)
        {
            return;
        }

        runtimeEvents.AddRange(chart.Events);
        runtimeEvents.Sort((left, right) => left.targetBeat.CompareTo(right.targetBeat));
    }

    private void Update()
    {
        if (!IsReady())
        {
            return;
        }

        double songTime = rhythmManager.SongTime;
        SpawnDueBirds(songTime);
        UpdateBirds(songTime);
        ProcessInput(songTime);
    }

    private bool IsReady()
    {
        return chart != null && input != null && rhythmManager != null &&
               chickPrefab != null && cuckooPrefab != null &&
               slot1 != null && slot2 != null && slot3 != null && exitSlot != null;
    }

    private void SpawnDueBirds(double songTime)
    {
        while (nextEventIndex < runtimeEvents.Count)
        {
            Stage01FeedingChart.Event chartEvent = runtimeEvents[nextEventIndex];
            double spawnTime = chart.GetBeatTime(chartEvent.targetBeat - chart.LeadBeats);
            if (songTime < spawnTime)
            {
                break;
            }

            SpawnBird(chartEvent);
            nextEventIndex++;
        }
    }

    private void SpawnBird(Stage01FeedingChart.Event chartEvent)
    {
        GameObject prefab = chartEvent.targetType == Stage01FeedingChart.BirdType.Chick
            ? chickPrefab
            : cuckooPrefab;
        GameObject instance = Instantiate(prefab, slot3.position, slot3.rotation, birdContainer);

        RuntimeBird bird = new()
        {
            ChartEvent = chartEvent,
            Transform = instance.transform,
            Chick = instance.GetComponent<ChickSpriteController>(),
            Cuckoo = instance.GetComponent<CuckooSpriteController>(),
            Phase = BirdPhase.Approaching
        };

        if (!bird.IsChick && !bird.IsCuckoo)
        {
            Debug.LogError("Stage01FeedingController: Spawned bird has no Stage 1 SpriteController.", instance);
            Destroy(instance);
            return;
        }

        activeBirds.Add(bird);
    }

    private void UpdateBirds(double songTime)
    {
        for (int i = activeBirds.Count - 1; i >= 0; i--)
        {
            RuntimeBird bird = activeBirds[i];

            if (bird.Phase == BirdPhase.Exiting)
            {
                float progress = InverseLerp(bird.ResultEndTime, bird.ExitEndTime, songTime);
                bird.Transform.position = Vector3.Lerp(bird.ExitStartPosition, exitSlot.position, progress);

                if (songTime >= bird.ExitEndTime)
                {
                    Destroy(bird.Transform.gameObject);
                    activeBirds.RemoveAt(i);
                }

                continue;
            }

            if (bird.Phase == BirdPhase.Resolved)
            {
                if (songTime >= bird.ResultEndTime)
                {
                    BeginExit(bird, songTime);
                }

                continue;
            }

            UpdateApproachPosition(bird, songTime);
            CheckMissedWindow(bird, songTime);
        }
    }

    private void UpdateApproachPosition(RuntimeBird bird, double songTime)
    {
        float targetBeat = bird.ChartEvent.targetBeat;
        double slot3MoveTime = chart.GetBeatTime(targetBeat - 2f);
        double slot2Time = chart.GetBeatTime(targetBeat - 1f);
        double slot1Time = chart.GetBeatTime(targetBeat);

        if (songTime < slot3MoveTime)
        {
            bird.Transform.position = slot3.position;
        }
        else if (songTime < slot2Time)
        {
            float progress = InverseLerp(slot3MoveTime, slot2Time, songTime);
            bird.Transform.position = Vector3.Lerp(slot3.position, slot2.position, progress);
        }
        else
        {
            float progress = InverseLerp(slot2Time, slot1Time, songTime);
            bird.Transform.position = Vector3.Lerp(slot2.position, slot1.position, progress);
        }
    }

    private void CheckMissedWindow(RuntimeBird bird, double songTime)
    {
        double judgeTime = bird.Phase == BirdPhase.WaitingSecondPeck
            ? chart.GetBeatTime(bird.ChartEvent.targetBeat + bird.ChartEvent.secondPeckBeatOffset)
            : chart.GetBeatTime(bird.ChartEvent.targetBeat);

        if (songTime <= judgeTime + chart.JudgeWindowSeconds)
        {
            return;
        }

        MissCount++;
        audioController?.PlayMiss();
        ResolveBird(bird, songTime);
    }

    private void ProcessInput(double songTime)
    {
        RuntimeBird target = FindInputTarget(songTime);
        if (target == null)
        {
            // Empty inputs affect only the mother and result count, never chart birds.
            if (ActionAPressedThisFrame())
            {
                motherBird?.PlayWrongFeed();
                MissCount++;
                audioController?.PlayMiss();
            }

            if (ActionBPressedThisFrame())
            {
                motherBird?.PlayWrongPeck();
                MissCount++;
                audioController?.PlayMiss();
            }

            return;
        }

        if (ActionAPressedThisFrame())
        {
            HandleActionA(target, songTime);
            return;
        }

        if (ActionBPressedThisFrame())
        {
            HandleActionB(target, songTime);
        }
    }

    private bool ActionAPressedThisFrame()
    {
        return TitleSession.SelectedInputMode switch
        {
            TitleSession.InputMode.Xbox => input.ActionAGamepadPressedThisFrame(),
            TitleSession.InputMode.KeyboardMouse => input.ActionAKeyboardPressedThisFrame(),
            _ => input.ActionAPressedThisFrame()
        };
    }

    private bool ActionBPressedThisFrame()
    {
        return TitleSession.SelectedInputMode switch
        {
            TitleSession.InputMode.Xbox => input.ActionBGamepadPressedThisFrame(),
            TitleSession.InputMode.KeyboardMouse => input.ActionBKeyboardPressedThisFrame(),
            _ => input.ActionBPressedThisFrame()
        };
    }

    private RuntimeBird FindInputTarget(double songTime)
    {
        foreach (RuntimeBird bird in activeBirds)
        {
            if (bird.Phase != BirdPhase.Approaching && bird.Phase != BirdPhase.WaitingSecondPeck)
            {
                continue;
            }

            double judgeTime = bird.Phase == BirdPhase.WaitingSecondPeck
                ? chart.GetBeatTime(bird.ChartEvent.targetBeat + bird.ChartEvent.secondPeckBeatOffset)
                : chart.GetBeatTime(bird.ChartEvent.targetBeat);

            if (System.Math.Abs(songTime - judgeTime) <= chart.JudgeWindowSeconds)
            {
                return bird;
            }
        }

        return null;
    }

    private void HandleActionA(RuntimeBird bird, double songTime)
    {
        if (bird.IsChick)
        {
            motherBird?.PlayFeed();
            bird.Chick.SetState(ChickSpriteController.State.Fed);
            audioController?.PlayMotherFeed();
            audioController?.PlayChickEat();
            SuccessCount++;
        }
        else
        {
            motherBird?.PlayWrongFeed();
            bird.Cuckoo.SetState(CuckooSpriteController.State.Fed);
            audioController?.PlayMiss();
            MissCount++;
        }

        ResolveBird(bird, songTime);
    }

    private void HandleActionB(RuntimeBird bird, double songTime)
    {
        if (bird.IsChick)
        {
            motherBird?.PlayWrongPeck();
            bird.Chick.SetState(ChickSpriteController.State.Idle);
            audioController?.PlayMiss();
            MissCount++;
            ResolveBird(bird, songTime);
            return;
        }

        if (bird.Phase == BirdPhase.Approaching)
        {
            motherBird?.PlayPeck1();
            bird.Cuckoo.SetState(CuckooSpriteController.State.Hit1);
            audioController?.PlayMotherPeck1();
            audioController?.PlayCuckooHit1();
            bird.Phase = BirdPhase.WaitingSecondPeck;
            return;
        }

        motherBird?.PlayPeck2();
        bird.Cuckoo.SetState(CuckooSpriteController.State.Hit2);
        // Transfer movement ownership before starting the Hit2-only exit.
        activeBirds.Remove(bird);
        bird.Cuckoo.PlayKnockaway();
        audioController?.PlayMotherPeck2();
        audioController?.PlayCuckooHit2();
        SuccessCount++;
    }

    private void ResolveBird(RuntimeBird bird, double songTime)
    {
        bird.Phase = BirdPhase.Resolved;
        bird.ResultEndTime = songTime + chart.ResultHoldBeats * chart.BeatInterval;
    }

    private void BeginExit(RuntimeBird bird, double songTime)
    {
        bird.Phase = BirdPhase.Exiting;
        bird.ResultEndTime = songTime;
        bird.ExitEndTime = songTime + chart.ExitDurationBeats * chart.BeatInterval;
        bird.ExitStartPosition = bird.Transform.position;

        if (bird.IsChick)
        {
            bird.Chick.SetState(ChickSpriteController.State.Exit);
        }
        else
        {
            bird.Cuckoo.SetState(CuckooSpriteController.State.Exit);
        }
    }

    private static float InverseLerp(double start, double end, double value)
    {
        if (end <= start)
        {
            return 1f;
        }

        return Mathf.Clamp01((float)((value - start) / (end - start)));
    }
}
