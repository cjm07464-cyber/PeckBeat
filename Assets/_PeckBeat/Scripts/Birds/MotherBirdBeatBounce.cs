using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class MotherBirdBeatBounce : MonoBehaviour
{
    [FormerlySerializedAs("visual")]
    [SerializeField] private Transform beatMoveRoot;
    [SerializeField] private Transform beatSquashRoot;
    [SerializeField] private PeckBeatRhythmManager rhythmManager;
    [SerializeField] private Stage01FeedingChart chart;
    [Min(1)][SerializeField] private int beatsPerBounce = 2;
    [Min(0f)][SerializeField] private float beatBounceDistance = 0.5f;
    [Range(0.05f, 0.95f)][SerializeField] private float downDurationRatio = 0.5f;
    [Range(0.1f, 1f)]
    [SerializeField] private float bounceDurationBeats = 0.25f;
    [Min(0f)] [SerializeField] private float scaleYSquash = 0.05f;
    [Min(0f)] [SerializeField] private float actionGraceBeats = 1f;
    private Transform boundMoveRoot;
    private Transform boundSquashRoot;
    private Vector3 initialMoveLocalPosition;
    private Vector3 initialSquashLocalScale;
    private MotherBirdSpriteController motherBird;
    private bool actionActive;
    private double resumeBeat = double.NegativeInfinity;
    private Sequence bounce;

    private void Awake()
    {
        boundMoveRoot = beatMoveRoot;
        boundSquashRoot = beatSquashRoot;
        if (boundMoveRoot != null)
        {
            initialMoveLocalPosition = boundMoveRoot.localPosition;
        }
        if (boundSquashRoot != null)
        {
            initialSquashLocalScale = boundSquashRoot.localScale;
        }
        motherBird = GetComponentInParent<MotherBirdSpriteController>();
    }

    private void OnEnable()
    {
        if (motherBird != null)
        {
            motherBird.StateChanging += OnStateChanging;
            OnStateChanging(motherBird.CurrentState);
        }
    }

    private void OnStateChanging(MotherBirdSpriteController.State state)
    {
        if (state != MotherBirdSpriteController.State.Idle)
        {
            actionActive = true;
            resumeBeat = double.PositiveInfinity;
            ClearBounce();
        }
        else if (actionActive)
        {
            actionActive = false;
            if (rhythmManager != null && chart != null)
            {
                double endBeat = (rhythmManager.PlaybackSongTime - chart.GetBeatTime(0f)) /
                                 chart.BeatInterval;
                int cycle = Math.Max(1, beatsPerBounce);
                resumeBeat = Math.Ceiling((endBeat + Mathf.Max(0f, actionGraceBeats)) / cycle) * cycle;
            }
            else
            {
                resumeBeat = double.NegativeInfinity;
            }
        }
    }

    private void LateUpdate()
    {
        if (boundMoveRoot == null || boundSquashRoot == null || rhythmManager == null || chart == null ||
            !rhythmManager.IsSongPlaying)
        {
            ClearBounce();
            return;
        }

        // Visual timing uses the existing DSP start, without input latency compensation.
        double beatPosition = (rhythmManager.PlaybackSongTime - chart.GetBeatTime(0f)) /
                              chart.BeatInterval;
        if (double.IsNaN(beatPosition) || double.IsInfinity(beatPosition) || beatPosition < 0 ||
            actionActive || beatPosition < resumeBeat)
        {
            ClearBounce();
            return;
        }

        if (bounce == null || !bounce.IsActive())
        {
            boundMoveRoot.localPosition = initialMoveLocalPosition;
            boundSquashRoot.localScale = initialSquashLocalScale;
            float ratio = Mathf.Clamp(downDurationRatio, 0.05f, 0.95f);
            float duration = Mathf.Clamp(bounceDurationBeats, 0.1f, 1f);

            bounce = DOTween.Sequence();

            bounce.Append(boundMoveRoot.DOLocalMoveY(
                initialMoveLocalPosition.y - Mathf.Max(0f, beatBounceDistance),
                duration * ratio).SetEase(Ease.OutQuad));
            bounce.Join(boundSquashRoot.DOScaleY(
                initialSquashLocalScale.y - Mathf.Max(0f, scaleYSquash),
                duration * ratio).SetEase(Ease.OutQuad));

            bounce.Append(boundMoveRoot.DOLocalMoveY(
                initialMoveLocalPosition.y,
                duration * (1f - ratio)).SetEase(Ease.InQuad));
            bounce.Join(boundSquashRoot.DOScaleY(
                initialSquashLocalScale.y,
                duration * (1f - ratio)).SetEase(Ease.InQuad));
            bounce.SetAutoKill(false).Pause();
        }

        // Bounce during the first beat of each cycle; hold at the origin for the remaining beats.
        // The cycle phase comes directly from DSP timing, so skipped frames cannot accumulate drift.
        double cyclePosition = beatPosition % Math.Max(1, beatsPerBounce);
        bounce.Goto((float)Math.Min(cyclePosition, bounceDurationBeats), false);
        if (cyclePosition >= bounceDurationBeats)
        {
            boundMoveRoot.localPosition = initialMoveLocalPosition;
            boundSquashRoot.localScale = initialSquashLocalScale;
        }
    }

    private void ClearBounce()
    {
        if (bounce != null)
        {
            bounce.Kill();
            bounce = null;
        }

        if (boundMoveRoot != null)
        {
            boundMoveRoot.localPosition = initialMoveLocalPosition;
        }
        if (boundSquashRoot != null)
        {
            boundSquashRoot.localScale = initialSquashLocalScale;
        }
    }

    private void OnDisable()
    {
        if (motherBird != null)
        {
            motherBird.StateChanging -= OnStateChanging;
        }
        ClearBounce();
    }
    private void OnDestroy() => ClearBounce();
}
