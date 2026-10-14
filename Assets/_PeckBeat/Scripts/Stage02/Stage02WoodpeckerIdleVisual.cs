using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public sealed class Stage02WoodpeckerIdleVisual : MonoBehaviour
{
    [Header("Split Idle Hierarchy")]
    [SerializeField] private GameObject splitIdleRoot;
    [SerializeField] private Transform bodyBobRoot;
    [SerializeField] private Transform wingBobRoot;
    [SerializeField] private Transform headBobRoot;

    [Header("Peck Head Composite")]
    [SerializeField] private Sprite peckHeadCompositeSprite;
    [SerializeField] private SpriteRenderer peckHeadRenderer;
    [Min(0.01f)] [SerializeField] private float peckHeadDuration = 0.08f;
    [SerializeField] private Vector3 peckHeadLocalOffset = Vector3.zero;

    [Header("Hold Peck Visual")]
    [SerializeField] private bool keepBodyVisibleDuringPeck = true;
    [SerializeField] private bool useHoldPeckAfterimage = true;
    [Min(0)] [SerializeField] private int holdPeckAfterimageCount = 2;
    [Range(0f, 1f)] [SerializeField] private float holdPeckAfterimageAlpha = 0.35f;
    [SerializeField] private Vector3 holdPeckAfterimageOffset = new Vector3(0f, 0.08f, 0f);
    [SerializeField] private int holdPeckAfterimageSortingOffset = -1;

    [Header("Rapid Hold Peck Flicker")]
    [SerializeField] private bool useRapidHoldPeckFlicker = true;
    [Min(0.005f)] [SerializeField] private float holdPeckFlickerOnDuration = 0.035f;
    [Min(0.005f)] [SerializeField] private float holdPeckFlickerOffDuration = 0.025f;
    [SerializeField] private bool disableHoldPeckAfterimage = true;

    [Header("Miss Peck Head")]
    [SerializeField] private Sprite missPeckHeadSprite;
    [Min(0.01f)] [SerializeField] private float missPeckHeadDuration = 0.12f;
    [SerializeField] private Vector3 missPeckHeadLocalOffset = Vector3.zero;

    [Header("Perfect Smile")]
    [SerializeField] private Sprite smileEyesSprite;
    [Min(1)] [SerializeField] private int perfectSmileBeatCount = 2;
    [Min(0.01f)] [SerializeField] private float perfectSmileFallbackDuration = 1.0f;

    [Header("Rhythm")]
    [SerializeField] private PeckBeatRhythmManager rhythmManager;
    [SerializeField] private Stage02SculptingChart chart;

    [Header("Rhythm Pulse")]
    [SerializeField] private bool useRhythmPulse = true;
    [Min(1)] [SerializeField] private int pulseEveryBeats = 2;
    [SerializeField] private float bodyPulseYOffset = 0f;
    [Min(0.01f)] [SerializeField] private float bodyPulseScaleX = 1f;
    [Min(0.01f)] [SerializeField] private float bodyPulseScaleY = 1f;
    [Min(0f)] [SerializeField] private float headPulseDelay = 0f;
    [SerializeField] private float headPulseYOffset = -0.14f;
    [Min(0.01f)] [SerializeField] private float headPulseScaleX = 1f;
    [Min(0.01f)] [SerializeField] private float headPulseScaleY = 1f;
    [Min(0f)] [SerializeField] private float wingPulseDelay = 0f;
    [SerializeField] private float wingPulseYOffset = -0.04f;
    [Min(0.01f)] [SerializeField] private float wingPulseScaleX = 1f;
    [Min(0.01f)] [SerializeField] private float wingPulseScaleY = 1f;
    [Min(0.01f)] [SerializeField] private float pulseReturnDuration = 0.20f;

    [Header("Legacy Sine Bob Fallback")]
    [Min(0f)] [SerializeField] private float bodyBobAmplitude = 0.03f;
    [Min(0f)] [SerializeField] private float bodyBobSpeed = 1.5f;
    [Min(0f)] [SerializeField] private float headBobAmplitude = 0.045f;
    [Min(0f)] [SerializeField] private float headBobSpeed = 1.7f;

    private Vector3 bodyRestPosition;
    private Vector3 wingRestPosition;
    private Vector3 headRestPosition;
    private Vector3 bodyRestScale;
    private Vector3 wingRestScale;
    private Vector3 headRestScale;
    private float animationTime;
    private bool restPositionsCached;
    private long lastObservedBeat = long.MinValue;
    private Coroutine pulseRoutine;
    private Coroutine peckHeadRoutine;
    private Coroutine holdRapidPeckRoutine;
    private Coroutine missPeckHeadRoutine;
    private Coroutine perfectSmileRoutine;
    private SpriteRenderer idleHeadRenderer;
    private SpriteRenderer idleEyesRenderer;
    private SpriteRenderer idleBeakRenderer;
    private Sprite originalEyesSprite;
    private bool originalEyesSpriteCached;
    private readonly List<GameObject> peckAfterimages = new List<GameObject>();

    public bool CanShowSplitIdle
    {
        get
        {
            if (splitIdleRoot == null || bodyBobRoot == null || wingBobRoot == null
                || headBobRoot == null)
            {
                return false;
            }

            SpriteRenderer[] partRenderers = splitIdleRoot.GetComponentsInChildren<SpriteRenderer>(true);
            if (partRenderers.Length < 7)
            {
                return false;
            }

            int idlePartCount = 0;
            foreach (SpriteRenderer partRenderer in partRenderers)
            {
                if (partRenderer == peckHeadRenderer)
                {
                    continue;
                }

                if (partRenderer == null || partRenderer.sprite == null)
                {
                    return false;
                }

                idlePartCount++;
            }

            return idlePartCount >= 7;
        }
    }

    public bool IsHoldRapidPeckPlaying => holdRapidPeckRoutine != null;

    private void Awake()
    {
        CacheRestPositions();
        CachePeckHeadParts();
        CacheOriginalEyesSprite();
        RestoreIdleHeadParts();

        Stage02SculptingController sculptingController = null;
        if (rhythmManager == null || chart == null)
        {
            sculptingController = FindFirstObjectByType<Stage02SculptingController>();
        }

        if (rhythmManager == null && sculptingController != null)
        {
            rhythmManager = GetControllerReference<PeckBeatRhythmManager>(
                sculptingController,
                "rhythmManager");
        }

        if (chart == null && sculptingController != null)
        {
            chart = GetControllerReference<Stage02SculptingChart>(sculptingController, "chart");
        }

        if (rhythmManager == null)
        {
            rhythmManager = FindFirstObjectByType<PeckBeatRhythmManager>();
        }
    }

    private void OnEnable()
    {
        CachePeckHeadParts();
        CacheOriginalEyesSprite();
    }

    private static T GetControllerReference<T>(
        Stage02SculptingController controller,
        string fieldName) where T : UnityEngine.Object
    {
        if (controller == null)
        {
            return null;
        }

        FieldInfo field = typeof(Stage02SculptingController).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        return field != null ? field.GetValue(controller) as T : null;
    }

    private void Update()
    {
        if (!CanShowSplitIdle || !splitIdleRoot.activeInHierarchy)
        {
            return;
        }

        if (useRhythmPulse && TryGetCurrentBeatIndex(out long beatIndex))
        {
            if (lastObservedBeat == long.MinValue || beatIndex != lastObservedBeat)
            {
                lastObservedBeat = beatIndex;
                int beatStep = Mathf.Max(1, pulseEveryBeats);
                if (beatStep == 1 || beatIndex % beatStep == 0)
                {
                    StartRhythmPulse();
                }
            }

            return;
        }

        ApplySineBob();
    }

    private void ApplySineBob()
    {
        animationTime += Time.deltaTime;
        bodyBobRoot.localPosition = bodyRestPosition;
        wingBobRoot.localPosition = wingRestPosition
            + Vector3.up * (Mathf.Sin(animationTime * bodyBobSpeed) * bodyBobAmplitude);
        headBobRoot.localPosition = headRestPosition
            + Vector3.up * (Mathf.Sin(animationTime * headBobSpeed) * headBobAmplitude);
    }

    public bool SetIdleVisible(bool visible)
    {
        CacheRestPositions();
        CachePeckHeadParts();

        StopPulseRoutine();
        StopPeckHeadRoutine();
        RestoreIdleHeadParts();
        ResetMotion();

        bool canShow = CanShowSplitIdle;
        if (splitIdleRoot != null)
        {
            splitIdleRoot.SetActive(visible && canShow);
        }

        if (visible && canShow && !TryGetCurrentBeatIndex(out lastObservedBeat))
        {
            lastObservedBeat = long.MinValue;
        }

        return visible && canShow;
    }

    public bool PlayPeckHeadComposite()
    {
        return PlayPeckHeadCompositeInternal(false);
    }

    public bool PlayHoldPeckVisual()
    {
        if (IsHoldRapidPeckPlaying)
        {
            return true;
        }

        if (!keepBodyVisibleDuringPeck)
        {
            return false;
        }

        bool createAfterimages = useHoldPeckAfterimage && !disableHoldPeckAfterimage;
        return PlayPeckHeadCompositeInternal(createAfterimages);
    }

    public void StartHoldRapidPeckVisual()
    {
        if (!useRapidHoldPeckFlicker || IsHoldRapidPeckPlaying)
        {
            return;
        }

        CachePeckHeadParts();
        if (peckHeadCompositeSprite == null
            || peckHeadRenderer == null
            || idleHeadRenderer == null
            || idleEyesRenderer == null
            || idleBeakRenderer == null
            || !SetIdleVisible(true))
        {
            return;
        }

        StopPeckHeadRoutine();
        ClearPeckAfterimages();
        peckHeadRenderer.sprite = peckHeadCompositeSprite;
        peckHeadRenderer.transform.localPosition = peckHeadLocalOffset;
        holdRapidPeckRoutine = StartCoroutine(HoldRapidPeckFlickerRoutine());
    }

    public void StopHoldRapidPeckVisual()
    {
        if (holdRapidPeckRoutine != null)
        {
            StopCoroutine(holdRapidPeckRoutine);
            holdRapidPeckRoutine = null;
        }

        ClearPeckAfterimages();
        RestoreIdleHeadParts();
    }

    public void PlayMissPeckFace()
    {
        StopHoldRapidPeckVisual();
        StopPeckHeadRoutine();
        StopMissPeckHeadRoutine();
        CachePeckHeadParts();
        if (missPeckHeadSprite == null
            || peckHeadRenderer == null
            || idleHeadRenderer == null
            || idleEyesRenderer == null
            || idleBeakRenderer == null
            || !SetIdleVisible(true))
        {
            return;
        }

        SetIdleHeadPartsVisible(false);
        peckHeadRenderer.sprite = missPeckHeadSprite;
        peckHeadRenderer.transform.localPosition = missPeckHeadLocalOffset;
        peckHeadRenderer.enabled = true;
        missPeckHeadRoutine = StartCoroutine(MissPeckHeadRoutine());
    }

    private bool PlayPeckHeadCompositeInternal(bool createAfterimages)
    {
        CachePeckHeadParts();
        if (peckHeadCompositeSprite == null
            || peckHeadRenderer == null
            || idleHeadRenderer == null
            || idleEyesRenderer == null
            || idleBeakRenderer == null
            || !SetIdleVisible(true))
        {
            return false;
        }

        StopPeckHeadRoutine();
        SetIdleHeadPartsVisible(false);
        peckHeadRenderer.sprite = peckHeadCompositeSprite;
        peckHeadRenderer.transform.localPosition = peckHeadLocalOffset;
        peckHeadRenderer.enabled = true;
        if (createAfterimages)
        {
            CreateHoldPeckAfterimages();
        }

        peckHeadRoutine = StartCoroutine(PeckHeadThenIdleRoutine());
        return true;
    }

    public void PlayPerfectSmile()
    {
        CachePeckHeadParts();
        CacheOriginalEyesSprite();
        if (smileEyesSprite == null || idleEyesRenderer == null)
        {
            return;
        }

        StopPerfectSmileRoutine();
        idleEyesRenderer.sprite = smileEyesSprite;
        perfectSmileRoutine = StartCoroutine(PerfectSmileRoutine());
    }

    public void StopPerfectSmileAndRestore()
    {
        StopPerfectSmileRoutine();
        if (idleEyesRenderer != null && originalEyesSpriteCached)
        {
            idleEyesRenderer.sprite = originalEyesSprite;
        }
    }

    private IEnumerator PerfectSmileRoutine()
    {
        int remainingPulses = Mathf.Max(1, perfectSmileBeatCount);
        float fallbackElapsed = 0f;

        if (TryGetCurrentBeatIndex(out long observedBeat))
        {
            while (remainingPulses > 0)
            {
                fallbackElapsed += Time.unscaledDeltaTime;
                if (TryGetCurrentBeatIndex(out long currentBeat))
                {
                    if (currentBeat > observedBeat)
                    {
                        int beatStep = useRhythmPulse ? Mathf.Max(1, pulseEveryBeats) : 1;
                        long crossedPulses = currentBeat / beatStep - observedBeat / beatStep;
                        if (crossedPulses > 0)
                        {
                            remainingPulses -= (int)Math.Min(crossedPulses, int.MaxValue);
                        }

                        observedBeat = currentBeat;
                    }
                }
                else if (fallbackElapsed >= Mathf.Max(0.01f, perfectSmileFallbackDuration))
                {
                    break;
                }

                yield return null;
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0.01f, perfectSmileFallbackDuration));
        }

        perfectSmileRoutine = null;
        if (idleEyesRenderer != null && originalEyesSpriteCached)
        {
            idleEyesRenderer.sprite = originalEyesSprite;
        }
    }

    private IEnumerator PeckHeadThenIdleRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, peckHeadDuration));
        RestoreIdleHeadParts();
        ClearPeckAfterimages();
        peckHeadRoutine = null;
    }

    private IEnumerator HoldRapidPeckFlickerRoutine()
    {
        while (true)
        {
            SetIdleHeadPartsVisible(false);
            if (peckHeadRenderer != null)
            {
                peckHeadRenderer.enabled = true;
            }

            yield return new WaitForSeconds(
                Mathf.Max(0.005f, holdPeckFlickerOnDuration));

            if (peckHeadRenderer != null)
            {
                peckHeadRenderer.enabled = false;
            }

            SetIdleHeadPartsVisible(true);
            yield return new WaitForSeconds(
                Mathf.Max(0.005f, holdPeckFlickerOffDuration));
        }
    }

    private IEnumerator MissPeckHeadRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, missPeckHeadDuration));
        RestoreIdleHeadParts();
        missPeckHeadRoutine = null;
    }

    private void CreateHoldPeckAfterimages()
    {
        ClearPeckAfterimages();
        if (peckHeadRenderer == null || peckHeadRenderer.sprite == null)
        {
            return;
        }

        int count = Mathf.Max(0, holdPeckAfterimageCount);
        for (int i = 0; i < count; i++)
        {
            GameObject afterimageObject = new GameObject($"PeckHeadAfterimage_{i + 1}");
            Transform afterimageTransform = afterimageObject.transform;
            afterimageTransform.SetParent(peckHeadRenderer.transform.parent, false);
            afterimageTransform.localPosition = peckHeadRenderer.transform.localPosition
                + holdPeckAfterimageOffset * (i + 1);
            afterimageTransform.localRotation = peckHeadRenderer.transform.localRotation;
            afterimageTransform.localScale = peckHeadRenderer.transform.localScale;

            SpriteRenderer afterimageRenderer = afterimageObject.AddComponent<SpriteRenderer>();
            afterimageRenderer.sprite = peckHeadRenderer.sprite;
            afterimageRenderer.sharedMaterial = peckHeadRenderer.sharedMaterial;
            afterimageRenderer.sortingLayerID = peckHeadRenderer.sortingLayerID;
            afterimageRenderer.sortingOrder = peckHeadRenderer.sortingOrder
                + holdPeckAfterimageSortingOffset * (i + 1);
            afterimageRenderer.flipX = peckHeadRenderer.flipX;
            afterimageRenderer.flipY = peckHeadRenderer.flipY;

            Color color = peckHeadRenderer.color;
            color.a *= Mathf.Clamp01(holdPeckAfterimageAlpha);
            afterimageRenderer.color = color;
            peckAfterimages.Add(afterimageObject);
        }
    }

    private void ClearPeckAfterimages()
    {
        for (int i = 0; i < peckAfterimages.Count; i++)
        {
            GameObject afterimageObject = peckAfterimages[i];
            if (afterimageObject == null)
            {
                continue;
            }

            afterimageObject.SetActive(false);
            Destroy(afterimageObject);
        }

        peckAfterimages.Clear();
    }

    private void CachePeckHeadParts()
    {
        if (headBobRoot == null)
        {
            return;
        }

        idleHeadRenderer ??= headBobRoot.Find("Head")?.GetComponent<SpriteRenderer>();
        idleEyesRenderer ??= headBobRoot.Find("Eyes")?.GetComponent<SpriteRenderer>();
        idleBeakRenderer ??= headBobRoot.Find("Beak")?.GetComponent<SpriteRenderer>();
    }

    private void CacheOriginalEyesSprite()
    {
        if (originalEyesSpriteCached || idleEyesRenderer == null)
        {
            return;
        }

        originalEyesSprite = idleEyesRenderer.sprite;
        originalEyesSpriteCached = true;
    }

    private void SetIdleHeadPartsVisible(bool visible)
    {
        if (idleHeadRenderer != null) idleHeadRenderer.enabled = visible;
        if (idleEyesRenderer != null) idleEyesRenderer.enabled = visible;
        if (idleBeakRenderer != null) idleBeakRenderer.enabled = visible;
    }

    private void RestoreIdleHeadParts()
    {
        SetIdleHeadPartsVisible(true);
        if (peckHeadRenderer != null)
        {
            peckHeadRenderer.enabled = false;
        }
    }

    private void StopPeckHeadRoutine()
    {
        if (peckHeadRoutine != null)
        {
            StopCoroutine(peckHeadRoutine);
            peckHeadRoutine = null;
        }

        ClearPeckAfterimages();
    }

    private void StopPerfectSmileRoutine()
    {
        if (perfectSmileRoutine != null)
        {
            StopCoroutine(perfectSmileRoutine);
            perfectSmileRoutine = null;
        }
    }

    private void StopMissPeckHeadRoutine()
    {
        if (missPeckHeadRoutine != null)
        {
            StopCoroutine(missPeckHeadRoutine);
            missPeckHeadRoutine = null;
        }
    }

    private bool TryGetCurrentBeatIndex(out long beatIndex)
    {
        beatIndex = long.MinValue;
        if (rhythmManager == null || chart == null || !rhythmManager.IsSongPlaying)
        {
            return false;
        }

        double beatInterval = chart.BeatInterval;
        double firstBeatTime = chart.GetBeatTime(0f);
        double songTime = rhythmManager.SongTime;
        if (beatInterval <= 0d || songTime < firstBeatTime)
        {
            return false;
        }

        beatIndex = (long)Math.Floor((songTime - firstBeatTime) / beatInterval);
        return true;
    }

    private void StartRhythmPulse()
    {
        StopPulseRoutine();
        RestoreRestPose();
        pulseRoutine = StartCoroutine(RhythmPulseRoutine());
    }

    private IEnumerator RhythmPulseRoutine()
    {
        float returnDuration = Mathf.Max(0.01f, pulseReturnDuration);
        float headDelay = Mathf.Max(0f, headPulseDelay);
        float wingDelay = Mathf.Max(0f, wingPulseDelay);
        float totalDuration = Mathf.Max(
            returnDuration,
            Mathf.Max(headDelay + returnDuration, wingDelay + returnDuration));
        float elapsed = 0f;

        ApplyPulsePose(
            bodyBobRoot,
            bodyRestPosition,
            bodyRestScale,
            bodyPulseYOffset,
            bodyPulseScaleX,
            bodyPulseScaleY,
            0f);

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float bodyProgress = Mathf.Clamp01(elapsed / returnDuration);
            ApplyPulsePose(
                bodyBobRoot,
                bodyRestPosition,
                bodyRestScale,
                bodyPulseYOffset,
                bodyPulseScaleX,
                bodyPulseScaleY,
                Mathf.SmoothStep(0f, 1f, bodyProgress));

            if (elapsed >= headDelay)
            {
                float headProgress = Mathf.Clamp01((elapsed - headDelay) / returnDuration);
                ApplyPulsePose(
                    headBobRoot,
                    headRestPosition,
                    headRestScale,
                    headPulseYOffset,
                    headPulseScaleX,
                    headPulseScaleY,
                    Mathf.SmoothStep(0f, 1f, headProgress));
            }

            if (elapsed >= wingDelay)
            {
                float wingProgress = Mathf.Clamp01((elapsed - wingDelay) / returnDuration);
                ApplyPulsePose(
                    wingBobRoot,
                    wingRestPosition,
                    wingRestScale,
                    wingPulseYOffset,
                    wingPulseScaleX,
                    wingPulseScaleY,
                    Mathf.SmoothStep(0f, 1f, wingProgress));
            }

            yield return null;
        }

        RestoreRestPose();
        pulseRoutine = null;
    }

    private static void ApplyPulsePose(
        Transform target,
        Vector3 restPosition,
        Vector3 restScale,
        float yOffset,
        float scaleX,
        float scaleY,
        float returnProgress)
    {
        if (target == null)
        {
            return;
        }

        Vector3 pulsePosition = restPosition + Vector3.up * yOffset;
        Vector3 pulseScale = new Vector3(
            restScale.x * scaleX,
            restScale.y * scaleY,
            restScale.z);
        target.localPosition = Vector3.LerpUnclamped(pulsePosition, restPosition, returnProgress);
        target.localScale = Vector3.LerpUnclamped(pulseScale, restScale, returnProgress);
    }

    private void CacheRestPositions()
    {
        if (restPositionsCached || bodyBobRoot == null || wingBobRoot == null
            || headBobRoot == null)
        {
            return;
        }

        bodyRestPosition = bodyBobRoot.localPosition;
        wingRestPosition = wingBobRoot.localPosition;
        headRestPosition = headBobRoot.localPosition;
        bodyRestScale = bodyBobRoot.localScale;
        wingRestScale = wingBobRoot.localScale;
        headRestScale = headBobRoot.localScale;
        restPositionsCached = true;
    }

    private void ResetMotion()
    {
        animationTime = 0f;
        RestoreRestPose();
    }

    private void RestoreRestPose()
    {
        if (!restPositionsCached)
        {
            return;
        }

        if (bodyBobRoot != null)
        {
            bodyBobRoot.localPosition = bodyRestPosition;
            bodyBobRoot.localScale = bodyRestScale;
        }

        if (headBobRoot != null)
        {
            headBobRoot.localPosition = headRestPosition;
            headBobRoot.localScale = headRestScale;
        }

        if (wingBobRoot != null)
        {
            wingBobRoot.localPosition = wingRestPosition;
            wingBobRoot.localScale = wingRestScale;
        }
    }

    private void StopPulseRoutine()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
    }

    private void OnDisable()
    {
        StopPulseRoutine();
        StopHoldRapidPeckVisual();
        StopPeckHeadRoutine();
        StopMissPeckHeadRoutine();
        StopPerfectSmileAndRestore();
        RestoreIdleHeadParts();
        ResetMotion();
    }
}
