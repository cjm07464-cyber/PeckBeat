using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Stage01FeedingController : MonoBehaviour
{
    private enum FeedingGrade
    {
        Perfect,
        Good,
        Miss
    }

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
        public bool UseFailedExit;
        public bool UseCuckooWrongExit;
        public bool UseCuckooPartialHitExit;

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

    [Header("Feed Success Presentation")]
    [Min(0f)]
    [SerializeField] private float feedSuccessCompositeDuration = 0.45f;

    [Header("Cuckoo Peck Success Presentation")]
    [Min(0f)]
    [SerializeField] private float peck1SuccessCompositeDuration = 0.35f;

    [Header("Feed Timing Grades")]
    [Min(0f)]
    [SerializeField] private float perfectTimingWindowSeconds = 0.08f;
    [Min(0f)]
    [SerializeField] private float goodTimingWindowSeconds = 0.18f;

    [Header("Stage Clear Transition")]
    [Min(0f)] [SerializeField] private float stageEndToTitleTransitionDelay = 1f;

    [Header("Nest Slots")]
    [SerializeField] private Transform slot1;
    [SerializeField] private Transform slot2;
    [SerializeField] private Transform slot3;
    [SerializeField] private Transform exitSlot;

    private readonly List<RuntimeBird> activeBirds = new();
    private readonly List<Stage01FeedingChart.Event> runtimeEvents = new();
    private int nextEventIndex;
    private bool stage01ClearTransitionStarted;

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
        if (TryStartStage01ClearTransition())
        {
            return;
        }

        if (!IsReady())
        {
            return;
        }

        double songTime = rhythmManager.SongTime;
        SpawnDueBirds(songTime);
        UpdateBirds(songTime);
        ProcessInput(songTime);
    }

    private bool TryStartStage01ClearTransition()
    {
        if (stage01ClearTransitionStarted)
        {
            return true;
        }

        if (rhythmManager == null || !rhythmManager.HasSongEnded)
        {
            return false;
        }

        stage01ClearTransitionStarted = true;
        StartCoroutine(Stage01ClearTransitionRoutine());
        return true;
    }

    private IEnumerator Stage01ClearTransitionRoutine()
    {
        float delay = Mathf.Max(0f, stageEndToTitleTransitionDelay);
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        TitleSession.RequestStage01ClearToStage02();
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

        double missWindow = bird.IsChick || bird.IsCuckoo
            ? GetGoodTimingWindow()
            : chart.JudgeWindowSeconds;
        if (songTime <= judgeTime + missWindow)
        {
            return;
        }

        MissCount++;
        audioController?.PlayMiss();
        if (bird.IsChick)
        {
            bird.UseFailedExit = true;
        }
        else if (bird.Phase == BirdPhase.WaitingSecondPeck)
        {
            bird.UseCuckooPartialHitExit = true;
        }

        ResolveBird(bird, songTime);
    }

    private void ProcessInput(double songTime)
    {
        bool actionAPressed = ActionAPressedThisFrame();
        bool actionBPressed = ActionBPressedThisFrame();
        RuntimeBird target = FindInputTarget(songTime, actionAPressed, actionBPressed);
        if (target == null)
        {
            // Empty inputs affect only the mother and result count, never chart birds.
            if (actionAPressed)
            {
                motherBird?.PlayWrongFeed();
                MissCount++;
                audioController?.PlayMiss();
            }

            if (actionBPressed)
            {
                motherBird?.PlayWrongPeck();
                MissCount++;
                audioController?.PlayMiss();
            }

            return;
        }

        if (actionAPressed)
        {
            HandleActionA(target, songTime);
            return;
        }

        if (actionBPressed)
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

    private RuntimeBird FindInputTarget(
        double songTime,
        bool useChickFeedWindow,
        bool useCuckooPeckWindow)
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

            bool useGradedWindow = (useChickFeedWindow && bird.IsChick) ||
                                    (useCuckooPeckWindow && bird.IsCuckoo);
            double inputWindow = useGradedWindow ? GetGoodTimingWindow() : chart.JudgeWindowSeconds;
            if (System.Math.Abs(songTime - judgeTime) <= inputWindow)
            {
                return bird;
            }
        }

        return null;
    }

    private void HandleActionA(RuntimeBird bird, double songTime)
    {
        bool playedFeedSuccessComposite = false;
        if (bird.IsChick)
        {
            double judgeTime = chart.GetBeatTime(bird.ChartEvent.targetBeat);
            float absoluteTimingError = (float)System.Math.Abs(songTime - judgeTime);
            FeedingGrade grade = GetFeedingGrade(absoluteTimingError);
            if (grade == FeedingGrade.Perfect)
            {
                bird.Chick.SetState(ChickSpriteController.State.Fed);
                bird.Chick.SetVisualVisible(false);
                motherBird?.PlayFeedSuccessComposite(feedSuccessCompositeDuration);
                StartCoroutine(RestoreChickVisualAfterDelay(bird.Chick, feedSuccessCompositeDuration));
                audioController?.PlayMotherFeed();
                audioController?.PlayChickEat();
                SuccessCount++;
                playedFeedSuccessComposite = true;
            }
            else if (grade == FeedingGrade.Good)
            {
                motherBird?.PlayFeed();
                bird.Chick.SetState(ChickSpriteController.State.Fed);
                audioController?.PlayMotherFeed();
                audioController?.PlayChickEat();
                SuccessCount++;
            }
            else
            {
                bird.Chick.SetState(ChickSpriteController.State.Idle);
                bird.UseFailedExit = true;
                audioController?.PlayMiss();
                MissCount++;
            }
        }
        else
        {
            motherBird?.PlayWrongFeed();
            bird.Cuckoo.SetVisualVisible(true);
            bird.Cuckoo.SetState(CuckooSpriteController.State.Fed);
            bird.UseCuckooWrongExit = true;
            audioController?.PlayMiss();
            MissCount++;
        }

        ResolveBird(bird, songTime);
        if (playedFeedSuccessComposite)
        {
            bird.ResultEndTime = System.Math.Max(
                bird.ResultEndTime,
                songTime + Mathf.Max(0f, feedSuccessCompositeDuration));
        }
    }

    private FeedingGrade GetFeedingGrade(float absoluteTimingError)
    {
        float perfectWindow = Mathf.Max(0f, perfectTimingWindowSeconds);
        float goodWindow = Mathf.Max(perfectWindow, goodTimingWindowSeconds);
        if (absoluteTimingError <= perfectWindow)
        {
            return FeedingGrade.Perfect;
        }

        return absoluteTimingError <= goodWindow
            ? FeedingGrade.Good
            : FeedingGrade.Miss;
    }

    private float GetGoodTimingWindow()
    {
        return Mathf.Max(0f, Mathf.Max(perfectTimingWindowSeconds, goodTimingWindowSeconds));
    }

    private static IEnumerator RestoreChickVisualAfterDelay(
        ChickSpriteController chick,
        float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));

        if (chick != null)
        {
            chick.SetVisualVisible(true);
        }
    }

    private void HandleActionB(RuntimeBird bird, double songTime)
    {
        if (bird.IsChick)
        {
            motherBird?.PlayWrongPeck();
            bird.Chick.SetState(ChickSpriteController.State.Idle);
            bird.UseFailedExit = true;
            audioController?.PlayMiss();
            MissCount++;
            ResolveBird(bird, songTime);
            return;
        }

        if (bird.Phase == BirdPhase.Approaching)
        {
            double judgeTime = chart.GetBeatTime(bird.ChartEvent.targetBeat);
            FeedingGrade grade = GetFeedingGrade((float)System.Math.Abs(songTime - judgeTime));
            bool usePeck1SuccessComposite =
                grade == FeedingGrade.Perfect &&
                motherBird != null &&
                motherBird.HasPeck1SuccessSprite;
            if (grade == FeedingGrade.Perfect)
            {
                motherBird?.PlayPeck1Success(peck1SuccessCompositeDuration);
            }
            else
            {
                motherBird?.PlayPeck1();
            }

            bird.Cuckoo.SetState(CuckooSpriteController.State.Hit1);
            if (usePeck1SuccessComposite)
            {
                bird.Cuckoo.SetVisualVisible(false);
                StartCoroutine(RestoreCuckooVisualAfterDelay(
                    bird.Cuckoo,
                    peck1SuccessCompositeDuration));
            }

            audioController?.PlayMotherPeck1();
            audioController?.PlayCuckooHit1();
            bird.Phase = BirdPhase.WaitingSecondPeck;
            return;
        }

        double secondJudgeTime = chart.GetBeatTime(
            bird.ChartEvent.targetBeat + bird.ChartEvent.secondPeckBeatOffset);
        FeedingGrade secondGrade = GetFeedingGrade(
            (float)System.Math.Abs(songTime - secondJudgeTime));
        if (secondGrade == FeedingGrade.Perfect)
        {
            motherBird?.PlayPeck2Success();
        }
        else
        {
            motherBird?.PlayPeck2();
        }

        bird.Cuckoo.SetVisualVisible(true);
        bird.Cuckoo.SetState(CuckooSpriteController.State.Hit2);
        // Transfer movement ownership before starting the Hit2-only exit.
        activeBirds.Remove(bird);
        if (secondGrade == FeedingGrade.Perfect)
        {
            bird.Cuckoo.PlayPerfectKnockaway();
        }
        else
        {
            bird.Cuckoo.PlayKnockaway();
        }

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
            bird.Chick.SetState(
                bird.UseFailedExit
                    ? ChickSpriteController.State.FailedExit
                    : ChickSpriteController.State.Exit);
        }
        else
        {
            bird.Cuckoo.SetVisualVisible(true);
            if (bird.UseCuckooWrongExit)
            {
                bird.Cuckoo.SetState(CuckooSpriteController.State.WrongExit);
            }
            else if (bird.UseCuckooPartialHitExit)
            {
                bird.Cuckoo.PlayPartialHitExit();
            }
            else
            {
                bird.Cuckoo.SetState(CuckooSpriteController.State.Exit);
            }
        }
    }

    private static IEnumerator RestoreCuckooVisualAfterDelay(
        CuckooSpriteController cuckoo,
        float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));

        if (cuckoo != null)
        {
            cuckoo.SetVisualVisible(true);
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
