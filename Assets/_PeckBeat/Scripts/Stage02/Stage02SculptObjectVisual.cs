using System;
using System.Collections;
using UnityEngine;

public sealed class Stage02SculptObjectVisual : MonoBehaviour
{
    [Serializable]
    private struct VisualSetting
    {
        public Sprite sprite;
        public Vector3 runLocalPosition;
        public Vector3 runLocalScale;
        public float runLocalRotationZ;
        public Vector3 idleLocalPosition;
        public Vector3 idleLocalScale;
        public float idleLocalRotationZ;
        public int sortingOrder;
        [HideInInspector] public Vector3 localScale;
        [HideInInspector] public bool initialized;
        [HideInInspector] public bool separateTransformsInitialized;
    }

    [Header("Renderers")]
    [SerializeField] private SpriteRenderer runLogRenderer;
    [SerializeField] private SpriteRenderer idleLogRenderer;
    [SerializeField] private SpriteRenderer legacyObjectRenderer;

    [Header("Visual Settings")]
    [SerializeField] private VisualSetting oneHitBaseLogSetting;
    [SerializeField] private VisualSetting oneHitCompletedSculptureSetting;
    [SerializeField] private VisualSetting twoHitBaseLogSetting;
    [SerializeField] private VisualSetting twoHitDamagedLogSetting;
    [SerializeField] private VisualSetting twoHitCompletedSculptureSetting;

    [Header("Legacy Sprite Slots")]
    [SerializeField] private Sprite oneHitBaseLog;
    [SerializeField] private Sprite oneHitCompletedSculpture;
    [SerializeField] private Sprite twoHitBaseLog;
    [SerializeField] private Sprite twoHitDamagedLog;
    [SerializeField] private Sprite twoHitCompletedSculpture;

    [Header("Wood Chip Burst")]
    [Tooltip("Random chip sprites. If empty or all entries are null, Wood Chip Sprite is used as a fallback.")]
    [SerializeField] private Sprite[] woodChipSprites;
    [Tooltip("Legacy fallback used when Wood Chip Sprites has no valid entries.")]
    [SerializeField] private Sprite woodChipSprite;
    [Min(1)] [SerializeField] private int woodChipCount = 5;
    [Min(1)] [SerializeField] private int twoHitFirstChipCount = 3;
    [Min(0.01f)] [SerializeField] private float woodChipDuration = 0.9f;
    [Min(0f)] [SerializeField] private float woodChipGravity = 11f;
    [Min(0f)] [SerializeField] private float woodChipMinSpeed = 2.5f;
    [Min(0f)] [SerializeField] private float woodChipMaxSpeed = 4.5f;
    [Min(0f)] [SerializeField] private float woodChipSpreadX = 2.0f;
    [SerializeField] private float woodChipUpwardBias = 1.0f;
    [Min(0f)] [SerializeField] private float woodChipHorizontalBoost = 1.25f;
    [SerializeField] private int woodChipSortingOrderOffset = 2;
    [Min(0.01f)] [SerializeField] private float woodChipMinScale = 0.15f;
    [Min(0.01f)] [SerializeField] private float woodChipMaxScale = 0.28f;

    [Header("Normal Hit Shake")]
    [SerializeField] private bool hitShakeEnabled = true;
    [Min(0.01f)] [SerializeField] private float hitShakeDuration = 0.14f;
    [Min(0f)] [SerializeField] private float hitShakeMagnitude = 0.16f;
    [Min(0f)] [SerializeField] private float hitShakeFrequency = 26f;
    [Min(0f)] [SerializeField] private float hitShakeRotation = 4.0f;

    private int requiredHits = 1;
    private int lastWoodChipHitCount;
    private Coroutine hitShakeRoutine;
    private Transform hitShakeTarget;
    private Vector3 hitShakeRestLocalPosition;
    private Quaternion hitShakeRestLocalRotation;

    public void ConfigureNormal(int hitRequirement)
    {
        StopHitShakeAndRestore();
        requiredHits = Mathf.Clamp(hitRequirement, 1, 2);
        lastWoodChipHitCount = 0;
        ApplyNormalHitVisual(0);
    }

    public void ApplyNormalHitVisual(int hitCount)
    {
        int clampedHitCount = Mathf.Max(0, hitCount);
        int previousHitCount = lastWoodChipHitCount;
        bool isNewValidHit = clampedHitCount > 0 && clampedHitCount > previousHitCount;
        bool shouldShake = isNewValidHit
            && IsLogStateBeforeHit(previousHitCount)
            && !IsCompletedSculptureState(clampedHitCount);

        if (isNewValidHit)
        {
            StopHitShakeAndRestore();
        }

        VisualSetting setting = GetSettingForState(clampedHitCount);
        if (setting.sprite == null)
        {
            Debug.LogWarning(
                $"[Stage02SculptObjectVisual] Sprite is not assigned for requiredHits={requiredHits}, hitCount={clampedHitCount}.",
                this);
        }
        else
        {
            ApplySetting(setting);
        }

        if (isNewValidHit)
        {
            lastWoodChipHitCount = clampedHitCount;
            SpawnWoodChipBurst(previousHitCount);

            if (shouldShake)
            {
                StartHitShake();
            }
        }
    }

    public void ShowCompletedVisual()
    {
        ApplyNormalHitVisual(requiredHits);
    }

    private VisualSetting GetSettingForState(int hitCount)
    {
        if (requiredHits <= 1)
        {
            if (hitCount <= 0)
            {
                return GetCompatibleSetting(ref oneHitBaseLogSetting, oneHitBaseLog);
            }

            return GetCompatibleSetting(
                ref oneHitCompletedSculptureSetting,
                oneHitCompletedSculpture);
        }

        if (hitCount <= 0)
        {
            return GetCompatibleSetting(ref twoHitBaseLogSetting, twoHitBaseLog);
        }

        if (hitCount == 1)
        {
            return GetCompatibleSetting(ref twoHitDamagedLogSetting, twoHitDamagedLog);
        }

        return GetCompatibleSetting(
            ref twoHitCompletedSculptureSetting,
            twoHitCompletedSculpture);
    }

    private VisualSetting GetCompatibleSetting(ref VisualSetting setting, Sprite legacySprite)
    {
        bool wasCompletelyEmpty = setting.sprite == null
            && setting.localScale == Vector3.zero
            && setting.runLocalPosition == Vector3.zero
            && setting.runLocalScale == Vector3.zero
            && Mathf.Approximately(setting.runLocalRotationZ, 0f)
            && setting.idleLocalPosition == Vector3.zero
            && setting.idleLocalScale == Vector3.zero
            && Mathf.Approximately(setting.idleLocalRotationZ, 0f)
            && setting.sortingOrder == 0;

        if (setting.sprite == null && !setting.initialized)
        {
            setting.sprite = legacySprite;
        }

        if (!setting.separateTransformsInitialized)
        {
            Vector3 legacyScale = setting.localScale != Vector3.zero
                ? setting.localScale
                : GetCurrentScale(runLogRenderer);

            if (setting.runLocalScale == Vector3.zero)
            {
                setting.runLocalScale = legacyScale;
            }

            if (setting.idleLocalScale == Vector3.zero)
            {
                setting.idleLocalScale = setting.localScale != Vector3.zero
                    ? setting.localScale
                    : GetCurrentScale(idleLogRenderer);
            }

            if (setting.runLocalPosition == Vector3.zero && runLogRenderer != null)
            {
                setting.runLocalPosition = runLogRenderer.transform.localPosition;
            }

            if (setting.idleLocalPosition == Vector3.zero && idleLogRenderer != null)
            {
                setting.idleLocalPosition = idleLogRenderer.transform.localPosition;
            }

            if (Mathf.Approximately(setting.runLocalRotationZ, 0f) && runLogRenderer != null)
            {
                setting.runLocalRotationZ = runLogRenderer.transform.localEulerAngles.z;
            }

            if (Mathf.Approximately(setting.idleLocalRotationZ, 0f) && idleLogRenderer != null)
            {
                setting.idleLocalRotationZ = idleLogRenderer.transform.localEulerAngles.z;
            }

            if (wasCompletelyEmpty)
            {
                setting.sortingOrder = runLogRenderer != null
                    ? runLogRenderer.sortingOrder
                    : idleLogRenderer != null ? idleLogRenderer.sortingOrder : 0;
            }

            if (setting.localScale == Vector3.zero)
            {
                setting.localScale = setting.runLocalScale;
            }

            setting.separateTransformsInitialized = true;
        }

        setting.runLocalScale = EnsureUsableScale(setting.runLocalScale, runLogRenderer);
        setting.idleLocalScale = EnsureUsableScale(setting.idleLocalScale, idleLogRenderer);
        setting.initialized = true;
        return setting;
    }

    private static Vector3 GetCurrentScale(SpriteRenderer renderer)
    {
        Vector3 scale = renderer != null ? renderer.transform.localScale : Vector3.one;
        return scale == Vector3.zero ? Vector3.one : scale;
    }

    private static Vector3 EnsureUsableScale(Vector3 scale, SpriteRenderer renderer)
    {
        return scale == Vector3.zero ? GetCurrentScale(renderer) : scale;
    }

    private void ApplySetting(VisualSetting setting)
    {
        bool hasRatLogRenderer = runLogRenderer != null || idleLogRenderer != null;
        if (hasRatLogRenderer)
        {
            ApplyToRenderer(
                runLogRenderer,
                setting,
                setting.runLocalPosition,
                setting.runLocalScale,
                setting.runLocalRotationZ);
            ApplyToRenderer(
                idleLogRenderer,
                setting,
                setting.idleLocalPosition,
                setting.idleLocalScale,
                setting.idleLocalRotationZ);

            if (legacyObjectRenderer != null)
            {
                legacyObjectRenderer.enabled = false;
            }

            return;
        }

        if (legacyObjectRenderer != null)
        {
            legacyObjectRenderer.sprite = setting.sprite;
            legacyObjectRenderer.enabled = true;
            return;
        }

        Debug.LogWarning("[Stage02SculptObjectVisual] No log renderer is assigned.", this);
    }

    private static void ApplyToRenderer(
        SpriteRenderer renderer,
        VisualSetting setting,
        Vector3 localPosition,
        Vector3 localScale,
        float localRotationZ)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.sprite = setting.sprite;
        renderer.transform.localPosition = localPosition;
        renderer.transform.localScale = localScale;
        renderer.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);
        renderer.sortingOrder = setting.sortingOrder;
        renderer.enabled = true;
    }

    private void SpawnWoodChipBurst(int previousHitCount)
    {
        if (!HasWoodChipSprite())
        {
            return;
        }

        SpriteRenderer sourceRenderer = GetVisibleLogRenderer();
        if (sourceRenderer == null)
        {
            return;
        }

        bool isTwoHitFirstStrike = requiredHits == 2 && previousHitCount == 0;
        int count = Mathf.Max(
            1,
            isTwoHitFirstStrike ? twoHitFirstChipCount : woodChipCount);
        float minimumSpeed = Mathf.Min(woodChipMinSpeed, woodChipMaxSpeed);
        float maximumSpeed = Mathf.Max(woodChipMinSpeed, woodChipMaxSpeed);
        float minimumScale = Mathf.Min(woodChipMinScale, woodChipMaxScale);
        float maximumScale = Mathf.Max(woodChipMinScale, woodChipMaxScale);
        Vector3 startPosition = sourceRenderer.bounds.center;

        for (int i = 0; i < count; i++)
        {
            GameObject chipObject = new GameObject($"WoodChip_{i + 1}");
            chipObject.transform.SetParent(transform, true);
            chipObject.transform.position = startPosition;
            chipObject.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

            float scale = UnityEngine.Random.Range(minimumScale, maximumScale);
            chipObject.transform.localScale = Vector3.one * scale;

            SpriteRenderer chipRenderer = chipObject.AddComponent<SpriteRenderer>();
            chipRenderer.sprite = GetRandomWoodChipSprite();
            chipRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            chipRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            chipRenderer.sortingOrder = sourceRenderer.sortingOrder + woodChipSortingOrderOffset;

            Vector3 velocity = new Vector3(
                UnityEngine.Random.Range(-woodChipSpreadX, woodChipSpreadX)
                    * woodChipHorizontalBoost,
                UnityEngine.Random.Range(minimumSpeed, maximumSpeed) + woodChipUpwardBias,
                0f);
            float rotationSpeed = UnityEngine.Random.Range(-540f, 540f);
            StartCoroutine(AnimateWoodChip(
                chipObject.transform,
                startPosition,
                velocity,
                rotationSpeed));
        }
    }

    private bool HasWoodChipSprite()
    {
        if (woodChipSprites != null)
        {
            foreach (Sprite sprite in woodChipSprites)
            {
                if (sprite != null)
                {
                    return true;
                }
            }
        }

        return woodChipSprite != null;
    }

    private Sprite GetRandomWoodChipSprite()
    {
        int validSpriteCount = 0;
        if (woodChipSprites != null)
        {
            foreach (Sprite sprite in woodChipSprites)
            {
                if (sprite != null)
                {
                    validSpriteCount++;
                }
            }
        }

        if (validSpriteCount <= 0)
        {
            return woodChipSprite;
        }

        int selectedIndex = UnityEngine.Random.Range(0, validSpriteCount);
        foreach (Sprite sprite in woodChipSprites)
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

        return woodChipSprite;
    }

    private SpriteRenderer GetVisibleLogRenderer()
    {
        if (IsRendererVisible(idleLogRenderer))
        {
            return idleLogRenderer;
        }

        if (IsRendererVisible(runLogRenderer))
        {
            return runLogRenderer;
        }

        return legacyObjectRenderer;
    }

    private static bool IsRendererVisible(SpriteRenderer renderer)
    {
        return renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy;
    }

    private bool IsLogStateBeforeHit(int previousHitCount)
    {
        if (requiredHits <= 1)
        {
            return previousHitCount == 0;
        }

        return previousHitCount == 0 || previousHitCount == 1;
    }

    private bool IsCompletedSculptureState(int hitCount)
    {
        return hitCount >= requiredHits;
    }

    private bool IsCompletedSculptureSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        return sprite == oneHitCompletedSculptureSetting.sprite
            || sprite == twoHitCompletedSculptureSetting.sprite
            || sprite == oneHitCompletedSculpture
            || sprite == twoHitCompletedSculpture;
    }

    private void StartHitShake()
    {
        if (!hitShakeEnabled)
        {
            return;
        }

        SpriteRenderer sourceRenderer = GetVisibleLogRenderer();
        if (sourceRenderer == null)
        {
            return;
        }

        if (IsCompletedSculptureSprite(sourceRenderer.sprite))
        {
            return;
        }

        hitShakeTarget = sourceRenderer.transform;
        hitShakeRestLocalPosition = hitShakeTarget.localPosition;
        hitShakeRestLocalRotation = hitShakeTarget.localRotation;
        hitShakeRoutine = StartCoroutine(HitShakeRoutine());
    }

    private IEnumerator HitShakeRoutine()
    {
        float duration = Mathf.Max(0.01f, hitShakeDuration);
        float elapsed = 0f;
        while (hitShakeTarget != null && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float strength = 1f - progress;
            float wave = Mathf.Sin(elapsed * Mathf.Max(0f, hitShakeFrequency));

            hitShakeTarget.localPosition = hitShakeRestLocalPosition
                + Vector3.right * (wave * hitShakeMagnitude * strength);
            hitShakeTarget.localRotation = hitShakeRestLocalRotation
                * Quaternion.Euler(0f, 0f, wave * hitShakeRotation * strength);

            yield return null;
        }

        RestoreHitShakeTransform();
        hitShakeRoutine = null;
    }

    private void StopHitShakeAndRestore()
    {
        if (hitShakeRoutine != null)
        {
            StopCoroutine(hitShakeRoutine);
            hitShakeRoutine = null;
        }

        RestoreHitShakeTransform();
    }

    private void RestoreHitShakeTransform()
    {
        if (hitShakeTarget != null)
        {
            hitShakeTarget.localPosition = hitShakeRestLocalPosition;
            hitShakeTarget.localRotation = hitShakeRestLocalRotation;
        }

        hitShakeTarget = null;
    }

    private IEnumerator AnimateWoodChip(
        Transform chip,
        Vector3 startPosition,
        Vector3 velocity,
        float rotationSpeed)
    {
        float duration = Mathf.Max(0.01f, woodChipDuration);
        float elapsed = 0f;
        while (chip != null && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Min(elapsed, duration);
            chip.position = startPosition
                + velocity * time
                + Vector3.down * woodChipGravity * time * time;
            chip.Rotate(0f, 0f, rotationSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }

        if (chip != null)
        {
            Destroy(chip.gameObject);
        }
    }

    private void OnDisable()
    {
        StopHitShakeAndRestore();
    }
}
