using System.Collections;
using UnityEngine;

public sealed class MotherBirdSpriteController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Feed,
        FeedSuccess,
        Peck1,
        Peck1Success,
        Peck2,
        Peck2Success,
        WrongFeed,
        WrongPeck
    }

    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("State Poses")]
    [SerializeField] private SpritePose idle = new SpritePose();
    [SerializeField] private SpritePose feed = new SpritePose();
    [SerializeField] private SpritePose feedSuccess = new SpritePose();
    [SerializeField] private SpritePose peck1 = new SpritePose();
    [SerializeField] private SpritePose peck1Success = new SpritePose();
    [SerializeField] private SpritePose peck2 = new SpritePose();
    [SerializeField] private SpritePose peck2Success = new SpritePose();
    [SerializeField] private SpritePose wrongFeed = new SpritePose();
    [SerializeField] private SpritePose wrongPeck = new SpritePose();

    [Header("Split Idle Visual")]
    [SerializeField] private bool useSplitIdleVisual = true;
    [SerializeField] private SpriteRenderer idleBodyRenderer;
    [SerializeField] private SpriteRenderer idleEyeRenderer;
    [SerializeField] private SpriteRenderer idleBeakWormRenderer;
    [SerializeField] private int idleBodySortingOrder = -3;
    [SerializeField] private int idleFaceSortingOrder = -2;
    [SerializeField] private bool hideLegacyWormVisual = true;

    [Header("Worm Positions")]
    [SerializeField] private Transform wormVisual;
    [SerializeField] private Vector2 wormIdlePosition = Vector2.zero;
    [SerializeField] private Vector2 wormFeedPosition = Vector2.zero;
    [SerializeField] private Vector2 wormPeck1Position = Vector2.zero;
    [SerializeField] private Vector2 wormPeck2Position = Vector2.zero;
    [SerializeField] private Vector2 wormWrongFeedPosition = Vector2.zero;
    [SerializeField] private Vector2 wormWrongPeckPosition = Vector2.zero;

    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float stateDuration = 0.2f;

    [Header("Feed Success Sorting")]
    [SerializeField] private bool overrideSortingOrderDuringFeedSuccess = true;
    [SerializeField] private int feedSuccessSortingOrder = -5;

    private Coroutine returnToIdleRoutine;
    private bool feedSuccessSortingOrderOverridden;
    private int sortingOrderBeforeFeedSuccess;

    public State CurrentState { get; private set; }
    public bool HasPeck1SuccessSprite => peck1Success != null && peck1Success.sprite != null;
    public event System.Action<State> StateChanging;

    private void Awake()
    {
        SetState(State.Idle);
    }

    private void OnDisable()
    {
        RestoreFeedSuccessSortingOrder();
    }

    public void PlayFeed()
    {
        PlayTemporaryState(State.Feed);
    }

    public void PlayFeedSuccessComposite(float duration)
    {
        PlayTemporaryState(State.FeedSuccess, duration);
    }

    public void PlayPeck1()
    {
        PlayTemporaryState(State.Peck1);
    }

    public void PlayPeck1Success()
    {
        PlayTemporaryState(State.Peck1Success);
    }

    public void PlayPeck1Success(float duration)
    {
        PlayTemporaryState(State.Peck1Success, duration);
    }

    public void PlayPeck2()
    {
        PlayTemporaryState(State.Peck2);
    }

    public void PlayPeck2Success()
    {
        PlayTemporaryState(State.Peck2Success);
    }

    public void PlayWrongFeed()
    {
        PlayTemporaryState(State.WrongFeed);
    }

    public void PlayWrongPeck()
    {
        PlayTemporaryState(State.WrongPeck);
    }

    public void SetState(State state)
    {
        bool usesSuccessCompositeSorting = UsesSuccessCompositeSorting(state);
        if (!usesSuccessCompositeSorting)
        {
            RestoreFeedSuccessSortingOrder();
        }

        StateChanging?.Invoke(state);
        CurrentState = state;

        bool showSplitIdle = state == State.Idle && HasCompleteSplitIdleVisual();
        ApplyIdleVisualMode(showSplitIdle);
        if (!showSplitIdle)
        {
            SpritePose pose = GetPose(state);
            pose?.Apply(spriteRenderer);
        }

        ApplyWormPosition(state);

        if (usesSuccessCompositeSorting)
        {
            ApplyFeedSuccessSortingOrder();
        }
    }

    private static bool UsesSuccessCompositeSorting(State state)
    {
        return state == State.FeedSuccess || state == State.Peck1Success;
    }

    private void PlayTemporaryState(State state)
    {
        PlayTemporaryState(state, stateDuration);
    }

    private void PlayTemporaryState(State state, float duration)
    {
        if (returnToIdleRoutine != null)
        {
            StopCoroutine(returnToIdleRoutine);
        }

        SetState(state);
        returnToIdleRoutine = StartCoroutine(ReturnToIdleAfterDelay(duration));
    }

    private IEnumerator ReturnToIdleAfterDelay(float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, duration));
        SetState(State.Idle);
        returnToIdleRoutine = null;
    }

    private SpritePose GetPose(State state)
    {
        return state switch
        {
            State.Feed => feed,
            State.FeedSuccess => feedSuccess != null && feedSuccess.sprite != null ? feedSuccess : feed,
            State.Peck1 => peck1,
            State.Peck1Success => peck1Success != null && peck1Success.sprite != null ? peck1Success : peck1,
            State.Peck2 => peck2,
            State.Peck2Success => peck2Success != null && peck2Success.sprite != null ? peck2Success : peck2,
            State.WrongFeed => wrongFeed,
            State.WrongPeck => wrongPeck,
            _ => idle
        };
    }

    private bool HasCompleteSplitIdleVisual()
    {
        return useSplitIdleVisual &&
               idleBodyRenderer != null &&
               idleBodyRenderer.sprite != null &&
               idleEyeRenderer != null &&
               idleEyeRenderer.sprite != null &&
               idleBeakWormRenderer != null &&
               idleBeakWormRenderer.sprite != null;
    }

    private void ApplyIdleVisualMode(bool showSplitIdle)
    {
        SetRendererVisible(spriteRenderer, !showSplitIdle);
        ConfigureIdlePart(idleBodyRenderer, showSplitIdle, idleBodySortingOrder);
        ConfigureIdlePart(idleEyeRenderer, showSplitIdle, idleFaceSortingOrder);
        ConfigureIdlePart(idleBeakWormRenderer, showSplitIdle, idleFaceSortingOrder);

        if (hideLegacyWormVisual && wormVisual != null)
        {
            SpriteRenderer[] legacyWormRenderers =
                wormVisual.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer legacyRenderer in legacyWormRenderers)
            {
                SetRendererVisible(legacyRenderer, false);
            }
        }
    }

    private static void ConfigureIdlePart(
        SpriteRenderer renderer,
        bool visible,
        int sortingOrder)
    {
        if (renderer == null)
        {
            return;
        }

        Transform partTransform = renderer.transform;
        partTransform.localPosition = Vector3.zero;
        partTransform.localRotation = Quaternion.identity;
        partTransform.localScale = Vector3.one;
        renderer.sortingOrder = sortingOrder;
        renderer.enabled = visible;
    }

    private static void SetRendererVisible(SpriteRenderer renderer, bool visible)
    {
        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }

    private void ApplyFeedSuccessSortingOrder()
    {
        if (!overrideSortingOrderDuringFeedSuccess || spriteRenderer == null)
        {
            return;
        }

        if (!feedSuccessSortingOrderOverridden)
        {
            sortingOrderBeforeFeedSuccess = spriteRenderer.sortingOrder;
            feedSuccessSortingOrderOverridden = true;
        }

        spriteRenderer.sortingOrder = feedSuccessSortingOrder;
    }

    private void RestoreFeedSuccessSortingOrder()
    {
        if (!feedSuccessSortingOrderOverridden)
        {
            return;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = sortingOrderBeforeFeedSuccess;
        }

        feedSuccessSortingOrderOverridden = false;
    }

    private void ApplyWormPosition(State state)
    {
        if (wormVisual == null)
        {
            return;
        }

        Vector2 position = state switch
        {
            State.Feed => wormFeedPosition,
            State.FeedSuccess => wormFeedPosition,
            State.Peck1 => wormPeck1Position,
            State.Peck1Success => wormPeck1Position,
            State.Peck2 => wormPeck2Position,
            State.Peck2Success => wormPeck2Position,
            State.WrongFeed => wormWrongFeedPosition,
            State.WrongPeck => wormWrongPeckPosition,
            _ => wormIdlePosition
        };

        wormVisual.localPosition = new Vector3(position.x, position.y, wormVisual.localPosition.z);
    }
}
