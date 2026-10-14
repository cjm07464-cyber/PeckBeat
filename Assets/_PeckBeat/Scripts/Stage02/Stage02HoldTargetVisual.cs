using System.Collections;
using UnityEngine;

public sealed class Stage02HoldTargetVisual : MonoBehaviour
{
    private static readonly int[] RunFrameSequence = { 0, 1, 2, 1, 0, 1, 2, 1 };

    [Header("Roots")]
    [SerializeField] private GameObject runRoot;
    [SerializeField] private GameObject idleRoot;

    [Header("Rat Renderers")]
    [SerializeField] private SpriteRenderer frontRatRunRenderer;
    [SerializeField] private SpriteRenderer backRatRunRenderer;
    [SerializeField] private SpriteRenderer frontRatIdleRenderer;
    [SerializeField] private SpriteRenderer backRatIdleRenderer;
    [SerializeField] private SpriteRenderer frontRunLegsRenderer;
    [SerializeField] private SpriteRenderer backRunLegsRenderer;

    [Header("Object Renderers")]
    [SerializeField] private SpriteRenderer longLogRunRenderer;
    [SerializeField] private SpriteRenderer longLogIdleRenderer;
    [SerializeField] private SpriteRenderer batRunRenderer;
    [SerializeField] private SpriteRenderer batIdleRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite frontRatRunSprite;
    [SerializeField] private Sprite backRatRunSprite;
    [SerializeField] private Sprite frontRatIdleSprite;
    [SerializeField] private Sprite backRatIdleSprite;
    [SerializeField] private Sprite longLogSprite;
    [SerializeField] private Sprite batSprite;
    [SerializeField] private Sprite ratRunA;
    [SerializeField] private Sprite ratRunB;
    [SerializeField] private Sprite ratRunC;

    [Header("Run Animation")]
    [Min(0.01f)] [SerializeField] private float runFrameInterval = 0.08f;

    [Header("Hold Display Mode")]
    [SerializeField] private bool useRunVisualForHold = true;
    [SerializeField] private bool animateRunLegsWhileHolding = true;

    [Header("Hold Visual Movement")]
    [SerializeField] private bool moveVisualDuringHold = true;
    [Min(0f)] [SerializeField] private float holdMoveDistance = 3.0f;
    [SerializeField] private Vector3 holdMoveDirection = Vector3.right;
    [SerializeField] private bool resetHoldMoveOnFail = true;

    [Header("Hold Reveal")]
    [SerializeField] private bool revealFromRight = true;
    [Range(0.001f, 1f)] [SerializeField] private float logRevealMinScaleX = 0.02f;
    [SerializeField] private bool logShakeDuringHold = true;
    [Min(0f)] [SerializeField] private float holdShakeMagnitude = 0.06f;
    [Min(0f)] [SerializeField] private float holdShakeFrequency = 35f;

    [Header("Hold Wood Chip Burst")]
    [SerializeField] private Sprite[] holdChipSprites;
    [SerializeField] private Sprite holdChipFallbackSprite;
    [Min(1)] [SerializeField] private int holdChipCountPerPulse = 2;
    [Min(0.01f)] [SerializeField] private float holdChipDuration = 0.75f;
    [Min(0f)] [SerializeField] private float holdChipGravity = 11f;
    [Min(0f)] [SerializeField] private float holdChipMinSpeed = 2.0f;
    [Min(0f)] [SerializeField] private float holdChipMaxSpeed = 4.8f;
    [Min(0f)] [SerializeField] private float holdChipSpreadX = 1.5f;
    [SerializeField] private float holdChipUpwardBias = 0.8f;
    [Min(0f)] [SerializeField] private float holdChipHorizontalBoost = 1.2f;
    [Min(0.01f)] [SerializeField] private float holdChipMinScale = 0.12f;
    [Min(0.01f)] [SerializeField] private float holdChipMaxScale = 0.24f;
    [SerializeField] private int holdChipSortingOrderOffset = 2;
    [SerializeField] private float holdChipSpawnYOffset = 0.1f;

    [Header("Layout Offsets")]
    [SerializeField] private Vector3 frontRatLocalOffset;
    [SerializeField] private Vector3 backRatLocalOffset;
    [SerializeField] private Vector3 logLocalOffset;
    [SerializeField] private Vector3 batLocalOffset;
    [SerializeField] private Vector3 runLegsLocalOffset;
    [SerializeField] private int sortingBaseOrder;

    private Vector3 runLogRestPosition;
    private Vector3 idleLogRestPosition;
    private Vector3 runLogRestScale = Vector3.one;
    private Vector3 idleLogRestScale = Vector3.one;
    private Quaternion runLogRestRotation = Quaternion.identity;
    private Quaternion idleLogRestRotation = Quaternion.identity;
    private float holdProgress;
    private float shakeOffsetX;
    private float shakeRotationZ;
    private float frameElapsed;
    private int frameSequenceIndex;
    private bool isRunning;
    private bool holdEffectsActive;
    private bool layoutCached;
    private bool holdMoveStartCaptured;
    private Vector3 holdMoveStartLocalPosition;
    private Vector3 resolvedHoldMoveDirection = Vector3.right;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        ApplyAssignedSprites();
        CacheLogLayout();
        ApplyHoldReveal();
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        frameElapsed += Time.deltaTime;
        float interval = Mathf.Max(0.01f, runFrameInterval);
        while (frameElapsed >= interval)
        {
            frameElapsed -= interval;
            frameSequenceIndex = (frameSequenceIndex + 1) % RunFrameSequence.Length;
            ApplyRunLegFrame();
        }
    }

    public void ShowRun()
    {
        ApplyAssignedSprites();
        holdEffectsActive = false;
        isRunning = true;
        frameElapsed = 0f;
        frameSequenceIndex = 0;

        if (runRoot != null)
        {
            runRoot.SetActive(true);
        }

        if (idleRoot != null)
        {
            idleRoot.SetActive(false);
        }

        ApplyRunLegFrame();
        ApplyHoldReveal();
    }

    public void ShowIdle()
    {
        ApplyAssignedSprites();

        if (useRunVisualForHold)
        {
            isRunning = animateRunLegsWhileHolding;
            if (!isRunning)
            {
                frameElapsed = 0f;
            }

            if (runRoot != null)
            {
                runRoot.SetActive(true);
            }

            if (idleRoot != null)
            {
                idleRoot.SetActive(false);
            }

            ApplyRunLegFrame();
            ApplyHoldReveal();
            return;
        }

        isRunning = false;
        frameElapsed = 0f;

        if (runRoot != null)
        {
            runRoot.SetActive(false);
        }

        if (idleRoot != null)
        {
            idleRoot.SetActive(true);
        }

        ApplyHoldReveal();
    }

    public void SetHoldProgress01(float progress)
    {
        holdProgress = Mathf.Clamp01(progress);
        holdEffectsActive = holdProgress < 1f;
        ApplyHoldReveal();
        ApplyHoldMove();
    }

    public void CaptureHoldMoveStart(Vector3 slot2ToSlot3WorldDirection)
    {
        if (runRoot == null)
        {
            return;
        }

        Transform moveTransform = runRoot.transform;
        holdMoveStartLocalPosition = moveTransform.localPosition;
        holdMoveStartCaptured = true;

        Vector3 configuredDirection = holdMoveDirection;
        if (configuredDirection.sqrMagnitude > 0.0001f)
        {
            resolvedHoldMoveDirection = configuredDirection.normalized;
        }
        else
        {
            Vector3 localExitDirection = moveTransform.parent != null
                ? moveTransform.parent.InverseTransformDirection(slot2ToSlot3WorldDirection)
                : slot2ToSlot3WorldDirection;
            resolvedHoldMoveDirection = localExitDirection.sqrMagnitude > 0.0001f
                ? localExitDirection.normalized
                : Vector3.right;
        }

        ApplyHoldMove();
    }

    public Vector3 ConsumeHoldMoveWorldOffsetForExit()
    {
        if (runRoot == null || !holdMoveStartCaptured)
        {
            return Vector3.zero;
        }

        Transform moveTransform = runRoot.transform;
        Vector3 currentWorldPosition = moveTransform.position;
        Vector3 startWorldPosition = moveTransform.parent != null
            ? moveTransform.parent.TransformPoint(holdMoveStartLocalPosition)
            : holdMoveStartLocalPosition;
        Vector3 worldOffset = currentWorldPosition - startWorldPosition;

        moveTransform.localPosition = holdMoveStartLocalPosition;
        holdMoveStartCaptured = false;
        return worldOffset;
    }

    public void ResetHoldVisual()
    {
        StopShakeAndRestore();
        ResetHoldMovePosition();
        holdEffectsActive = false;
        holdProgress = 0f;
        ApplyHoldReveal();
    }

    public void PlayHoldChipOrShakePulse()
    {
        if (!holdEffectsActive)
        {
            return;
        }

        if (logShakeDuringHold)
        {
            StopShakeAndRestore();
            shakeRoutine = StartCoroutine(HoldShakeRoutine());
        }

        SpawnHoldChipBurst();
    }

    public void StopAllHoldVisualEffects()
    {
        isRunning = false;
        frameElapsed = 0f;
        StopShakeAndRestore();
    }

    private IEnumerator HoldShakeRoutine()
    {
        const float shakeDuration = 0.1f;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / shakeDuration);
            float strength = 1f - progress;
            float wave = Mathf.Sin(elapsed * Mathf.Max(0f, holdShakeFrequency));
            shakeOffsetX = wave * holdShakeMagnitude * strength;
            shakeRotationZ = wave * 2f * strength;
            ApplyHoldReveal();
            yield return null;
        }

        shakeOffsetX = 0f;
        shakeRotationZ = 0f;
        ApplyHoldReveal();
        shakeRoutine = null;
    }

    private void ApplyHoldMove()
    {
        if (!moveVisualDuringHold || runRoot == null || !holdMoveStartCaptured)
        {
            return;
        }

        runRoot.transform.localPosition = holdMoveStartLocalPosition
            + resolvedHoldMoveDirection * Mathf.Max(0f, holdMoveDistance) * holdProgress;
    }

    private void ResetHoldMovePosition()
    {
        if (runRoot == null || !holdMoveStartCaptured)
        {
            return;
        }

        if (resetHoldMoveOnFail)
        {
            runRoot.transform.localPosition = holdMoveStartLocalPosition;
        }

        holdMoveStartCaptured = false;
    }

    private void ApplyHoldReveal()
    {
        if (!layoutCached)
        {
            CacheLogLayout();
        }

        ApplyLogReveal(
            longLogRunRenderer,
            runLogRestPosition,
            runLogRestScale,
            runLogRestRotation);
        ApplyLogReveal(
            longLogIdleRenderer,
            idleLogRestPosition,
            idleLogRestScale,
            idleLogRestRotation);
    }

    private void ApplyLogReveal(
        SpriteRenderer renderer,
        Vector3 restPosition,
        Vector3 restScale,
        Quaternion restRotation)
    {
        if (renderer == null)
        {
            return;
        }

        float remaining = Mathf.Lerp(1f, Mathf.Clamp(logRevealMinScaleX, 0.001f, 1f), holdProgress);
        Vector3 scale = restScale;
        scale.x = restScale.x * remaining;

        float spriteWidth = renderer.sprite != null ? renderer.sprite.bounds.size.x : 0f;
        float lostWidth = spriteWidth * Mathf.Abs(restScale.x) * (1f - remaining);
        float direction = revealFromRight ? -1f : 1f;

        renderer.transform.localScale = scale;
        renderer.transform.localPosition = restPosition
            + Vector3.right * (direction * lostWidth * 0.5f + shakeOffsetX);
        renderer.transform.localRotation = restRotation
            * Quaternion.Euler(0f, 0f, shakeRotationZ);
    }

    private void ApplyAssignedSprites()
    {
        SetSpriteIfMissing(frontRatRunRenderer, frontRatRunSprite);
        SetSpriteIfMissing(backRatRunRenderer, backRatRunSprite);
        SetSpriteIfMissing(frontRatIdleRenderer, frontRatIdleSprite);
        SetSpriteIfMissing(backRatIdleRenderer, backRatIdleSprite);
        SetSpriteIfMissing(longLogRunRenderer, longLogSprite);
        SetSpriteIfMissing(longLogIdleRenderer, longLogSprite);
        SetSpriteIfMissing(batRunRenderer, batSprite);
        SetSpriteIfMissing(batIdleRenderer, batSprite);
    }

    private void ApplyRunLegFrame()
    {
        Sprite frame = RunFrameSequence[frameSequenceIndex] switch
        {
            1 => ratRunB,
            2 => ratRunC,
            _ => ratRunA
        };

        SetSprite(frontRunLegsRenderer, frame);
        SetSprite(backRunLegsRenderer, frame);
    }

    private static void SetSprite(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer != null && sprite != null)
        {
            renderer.sprite = sprite;
            renderer.enabled = true;
        }
    }

    private static void SetSpriteIfMissing(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer != null && renderer.sprite == null && sprite != null)
        {
            renderer.sprite = sprite;
        }
    }

    private void SpawnHoldChipBurst()
    {
        if (longLogRunRenderer == null || !HasHoldChipSprite())
        {
            return;
        }

        Bounds bounds = longLogRunRenderer.bounds;
        float spawnX = revealFromRight
            ? Mathf.Lerp(bounds.max.x, bounds.min.x, holdProgress)
            : Mathf.Lerp(bounds.min.x, bounds.max.x, holdProgress);
        Vector3 spawnPosition = new Vector3(
            spawnX,
            bounds.center.y + holdChipSpawnYOffset,
            bounds.center.z);

        int count = Mathf.Max(1, holdChipCountPerPulse);
        float minimumSpeed = Mathf.Min(holdChipMinSpeed, holdChipMaxSpeed);
        float maximumSpeed = Mathf.Max(holdChipMinSpeed, holdChipMaxSpeed);
        float minimumScale = Mathf.Min(holdChipMinScale, holdChipMaxScale);
        float maximumScale = Mathf.Max(holdChipMinScale, holdChipMaxScale);

        for (int i = 0; i < count; i++)
        {
            Sprite chipSprite = GetRandomHoldChipSprite();
            if (chipSprite == null)
            {
                continue;
            }

            GameObject chipObject = new GameObject($"HoldWoodChip_{i + 1}");
            chipObject.transform.SetParent(transform, true);
            chipObject.transform.position = spawnPosition;
            chipObject.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Random.Range(0f, 360f));

            float scale = Random.Range(minimumScale, maximumScale);
            chipObject.transform.localScale = Vector3.one * scale;

            SpriteRenderer chipRenderer = chipObject.AddComponent<SpriteRenderer>();
            chipRenderer.sprite = chipSprite;
            chipRenderer.sharedMaterial = longLogRunRenderer.sharedMaterial;
            chipRenderer.sortingLayerID = longLogRunRenderer.sortingLayerID;
            chipRenderer.sortingOrder = longLogRunRenderer.sortingOrder
                + holdChipSortingOrderOffset;

            Vector3 velocity = new Vector3(
                Random.Range(-holdChipSpreadX, holdChipSpreadX)
                    * holdChipHorizontalBoost,
                Random.Range(minimumSpeed, maximumSpeed) + holdChipUpwardBias,
                0f);
            float rotationSpeed = Random.Range(-540f, 540f);
            StartCoroutine(AnimateHoldChip(
                chipObject.transform,
                spawnPosition,
                velocity,
                rotationSpeed));
        }
    }

    private bool HasHoldChipSprite()
    {
        if (holdChipSprites != null)
        {
            foreach (Sprite sprite in holdChipSprites)
            {
                if (sprite != null)
                {
                    return true;
                }
            }
        }

        return holdChipFallbackSprite != null;
    }

    private Sprite GetRandomHoldChipSprite()
    {
        int validSpriteCount = 0;
        if (holdChipSprites != null)
        {
            foreach (Sprite sprite in holdChipSprites)
            {
                if (sprite != null)
                {
                    validSpriteCount++;
                }
            }
        }

        if (validSpriteCount == 0)
        {
            return holdChipFallbackSprite;
        }

        int selectedIndex = Random.Range(0, validSpriteCount);
        foreach (Sprite sprite in holdChipSprites)
        {
            if (sprite == null)
            {
                continue;
            }

            if (selectedIndex == 0)
            {
                return sprite;
            }

            selectedIndex--;
        }

        return holdChipFallbackSprite;
    }

    private IEnumerator AnimateHoldChip(
        Transform chip,
        Vector3 startPosition,
        Vector3 velocity,
        float rotationSpeed)
    {
        float duration = Mathf.Max(0.01f, holdChipDuration);
        float elapsed = 0f;
        while (chip != null && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Min(elapsed, duration);
            chip.position = startPosition
                + velocity * time
                + Vector3.down * holdChipGravity * time * time;
            chip.Rotate(0f, 0f, rotationSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }

        if (chip != null)
        {
            Destroy(chip.gameObject);
        }
    }

    private void ApplyConfiguredOffsets()
    {
        AddLocalOffset(frontRatRunRenderer, frontRatLocalOffset);
        AddLocalOffset(frontRatIdleRenderer, frontRatLocalOffset);
        AddLocalOffset(backRatRunRenderer, backRatLocalOffset);
        AddLocalOffset(backRatIdleRenderer, backRatLocalOffset);
        AddLocalOffset(longLogRunRenderer, logLocalOffset);
        AddLocalOffset(longLogIdleRenderer, logLocalOffset);
        AddLocalOffset(batRunRenderer, batLocalOffset);
        AddLocalOffset(batIdleRenderer, batLocalOffset);
        AddLocalOffset(frontRunLegsRenderer, runLegsLocalOffset);
        AddLocalOffset(backRunLegsRenderer, runLegsLocalOffset);
    }

    private static void AddLocalOffset(SpriteRenderer renderer, Vector3 offset)
    {
        if (renderer != null)
        {
            renderer.transform.localPosition += offset;
        }
    }

    private void ApplySortingOrders()
    {
        SetSortingOrder(batRunRenderer, sortingBaseOrder);
        SetSortingOrder(batIdleRenderer, sortingBaseOrder);
        SetSortingOrder(longLogRunRenderer, sortingBaseOrder + 1);
        SetSortingOrder(longLogIdleRenderer, sortingBaseOrder + 1);
        SetSortingOrder(backRatRunRenderer, sortingBaseOrder + 2);
        SetSortingOrder(backRunLegsRenderer, sortingBaseOrder + 2);
        SetSortingOrder(backRatIdleRenderer, sortingBaseOrder + 2);
        SetSortingOrder(frontRatRunRenderer, sortingBaseOrder + 3);
        SetSortingOrder(frontRunLegsRenderer, sortingBaseOrder + 3);
        SetSortingOrder(frontRatIdleRenderer, sortingBaseOrder + 3);
    }

    private static void SetSortingOrder(SpriteRenderer renderer, int sortingOrder)
    {
        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
        }
    }

    private void CacheLogLayout()
    {
        if (layoutCached)
        {
            return;
        }

        CacheRendererTransform(
            longLogRunRenderer,
            out runLogRestPosition,
            out runLogRestScale,
            out runLogRestRotation);
        CacheRendererTransform(
            longLogIdleRenderer,
            out idleLogRestPosition,
            out idleLogRestScale,
            out idleLogRestRotation);
        layoutCached = true;
    }

    private static void CacheRendererTransform(
        SpriteRenderer renderer,
        out Vector3 localPosition,
        out Vector3 localScale,
        out Quaternion localRotation)
    {
        if (renderer == null)
        {
            localPosition = Vector3.zero;
            localScale = Vector3.one;
            localRotation = Quaternion.identity;
            return;
        }

        localPosition = renderer.transform.localPosition;
        localScale = renderer.transform.localScale;
        localRotation = renderer.transform.localRotation;
    }

    private void StopShakeAndRestore()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        shakeOffsetX = 0f;
        shakeRotationZ = 0f;
        ApplyHoldReveal();
    }

    private void OnDisable()
    {
        holdEffectsActive = false;
        ResetHoldMovePosition();
        StopAllHoldVisualEffects();
    }
}
