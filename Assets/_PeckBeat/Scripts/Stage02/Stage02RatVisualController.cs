using UnityEngine;

public sealed class Stage02RatVisualController : MonoBehaviour
{
    private static readonly int[] RunFrameSequence = { 0, 1, 2, 1, 0, 1, 2, 1 };

    [Header("Visual Roots")]
    [SerializeField] private GameObject runRoot;
    [SerializeField] private GameObject idleRoot;

    [Header("Renderers")]
    [SerializeField] private SpriteRenderer runBaseRenderer;
    [SerializeField] private SpriteRenderer runSeparatedEarRenderer;
    [SerializeField] private SpriteRenderer runSeparatedArmLeftRenderer;
    [SerializeField] private SpriteRenderer runSeparatedArmRightRenderer;
    [SerializeField] private SpriteRenderer runLegsRenderer;
    [SerializeField] private SpriteRenderer idleBaseRenderer;
    [SerializeField] private SpriteRenderer idleSeparatedEarRenderer;
    [SerializeField] private SpriteRenderer idleSeparatedArmRenderer;

    [Header("Legacy / Reference Only")]
    [SerializeField] private SpriteRenderer runUpperBodyRenderer;
    [SerializeField] private SpriteRenderer idleRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite runBaseSprite;
    [SerializeField] private Sprite runSeparatedEarSprite;
    [SerializeField] private Sprite runSeparatedArmSprite;
    [SerializeField] private Sprite runLegsA;
    [SerializeField] private Sprite runLegsB;
    [SerializeField] private Sprite runLegsC;
    [SerializeField] private Sprite idleBaseSprite;
    [SerializeField] private Sprite idleSeparatedEarSprite;
    [SerializeField] private Sprite idleSeparatedArmSprite;
    [SerializeField] private Sprite runUpperBodySprite;
    [SerializeField] private Sprite idleSprite;

    [Header("Idle Visual")]
    [Min(0.01f)] [SerializeField] private float idleScaleMultiplier = 1.2f;

    [Header("Run Animation")]
    [Min(0.01f)] [SerializeField] private float runFrameInterval = 0.08f;

    private float frameElapsed;
    private int sequenceIndex;
    private bool isRunning;

    private void Awake()
    {
        ApplyIdleScale();
        ApplyStaticSprites();
    }

    private void Update()
    {
        if (!isRunning || runLegsRenderer == null)
        {
            return;
        }

        frameElapsed += Time.deltaTime;
        float interval = Mathf.Max(0.01f, runFrameInterval);
        while (frameElapsed >= interval)
        {
            frameElapsed -= interval;
            sequenceIndex = (sequenceIndex + 1) % RunFrameSequence.Length;
            ApplyRunLegFrame();
        }
    }

    public void PlayRun()
    {
        ApplyStaticSprites();

        bool canShowRun = runRoot != null
            && runBaseRenderer != null
            && runSeparatedEarRenderer != null
            && runSeparatedArmRightRenderer != null
            && runLegsRenderer != null
            && runBaseSprite != null
            && runSeparatedEarSprite != null
            && runSeparatedArmSprite != null
            && runLegsA != null
            && runLegsB != null
            && runLegsC != null;

        isRunning = canShowRun;
        frameElapsed = 0f;
        sequenceIndex = 0;

        if (runRoot != null)
        {
            runRoot.SetActive(canShowRun);
        }

        if (idleRoot != null)
        {
            idleRoot.SetActive(!canShowRun && CanShowSplitIdle());
        }

        if (canShowRun)
        {
            ApplyRunLegFrame();
        }
    }

    public void ShowIdle()
    {
        ApplyStaticSprites();
        isRunning = false;
        frameElapsed = 0f;

        if (runRoot != null)
        {
            runRoot.SetActive(false);
        }

        if (idleRoot != null)
        {
            idleRoot.SetActive(CanShowSplitIdle());
        }
    }

    private void ApplyStaticSprites()
    {
        SetRendererSprite(runBaseRenderer, runBaseSprite);
        SetRendererSprite(runSeparatedEarRenderer, runSeparatedEarSprite);
        SetRendererSprite(runSeparatedArmRightRenderer, runSeparatedArmSprite);

        if (runSeparatedArmLeftRenderer != null)
        {
            runSeparatedArmLeftRenderer.enabled = false;
            runSeparatedArmLeftRenderer.gameObject.SetActive(false);
        }

        // 기존 달리기 상체는 Inspector/프리팹 참조 보존용이며 런타임에는 표시하지 않는다.
        if (runUpperBodyRenderer != null)
        {
            if (runUpperBodySprite != null)
            {
                runUpperBodyRenderer.sprite = runUpperBodySprite;
            }

            runUpperBodyRenderer.enabled = false;
            runUpperBodyRenderer.gameObject.SetActive(false);
        }

        SetRendererSprite(idleBaseRenderer, idleBaseSprite);
        SetRendererSprite(idleSeparatedEarRenderer, idleSeparatedEarSprite);
        SetRendererSprite(idleSeparatedArmRenderer, idleSeparatedArmSprite);

        // 쥐_Idle.png는 조립 위치를 비교하기 위한 레거시 참고 이미지로만 보존한다.
        if (idleRenderer != null)
        {
            if (idleSprite != null)
            {
                idleRenderer.sprite = idleSprite;
            }

            idleRenderer.enabled = false;
        }
    }

    private bool CanShowSplitIdle()
    {
        return idleRoot != null
            && idleBaseRenderer != null
            && idleSeparatedEarRenderer != null
            && idleSeparatedArmRenderer != null
            && idleBaseSprite != null
            && idleSeparatedEarSprite != null
            && idleSeparatedArmSprite != null;
    }

    private void ApplyIdleScale()
    {
        if (idleRoot == null)
        {
            return;
        }

        float multiplier = Mathf.Max(0.01f, idleScaleMultiplier);
        idleRoot.transform.localScale *= multiplier;
    }

    private static void SetRendererSprite(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer == null)
        {
            return;
        }

        if (sprite != null)
        {
            renderer.sprite = sprite;
        }

        renderer.enabled = sprite != null;
    }

    private void ApplyRunLegFrame()
    {
        if (runLegsRenderer == null)
        {
            return;
        }

        Sprite frame = RunFrameSequence[sequenceIndex] switch
        {
            1 => runLegsB,
            2 => runLegsC,
            _ => runLegsA
        };

        if (frame != null)
        {
            runLegsRenderer.sprite = frame;
        }
    }

    private void OnDisable()
    {
        isRunning = false;
        frameElapsed = 0f;
    }
}
