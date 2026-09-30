using System.Collections;
using UnityEngine;

public sealed class MotherBirdSpriteController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Feed,
        Peck1,
        Peck2,
        WrongFeed,
        WrongPeck
    }

    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("State Poses")]
    [SerializeField] private SpritePose idle = new SpritePose();
    [SerializeField] private SpritePose feed = new SpritePose();
    [SerializeField] private SpritePose peck1 = new SpritePose();
    [SerializeField] private SpritePose peck2 = new SpritePose();
    [SerializeField] private SpritePose wrongFeed = new SpritePose();
    [SerializeField] private SpritePose wrongPeck = new SpritePose();

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

    private Coroutine returnToIdleRoutine;

    public State CurrentState { get; private set; }
    public event System.Action<State> StateChanging;

    private void Awake()
    {
        SetState(State.Idle);
    }

    public void PlayFeed()
    {
        PlayTemporaryState(State.Feed);
    }

    public void PlayPeck1()
    {
        PlayTemporaryState(State.Peck1);
    }

    public void PlayPeck2()
    {
        PlayTemporaryState(State.Peck2);
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
        StateChanging?.Invoke(state);
        CurrentState = state;

        SpritePose pose = GetPose(state);
        pose?.Apply(spriteRenderer);
        ApplyWormPosition(state);
    }

    private void PlayTemporaryState(State state)
    {
        if (returnToIdleRoutine != null)
        {
            StopCoroutine(returnToIdleRoutine);
        }

        SetState(state);
        returnToIdleRoutine = StartCoroutine(ReturnToIdleAfterDelay());
    }

    private IEnumerator ReturnToIdleAfterDelay()
    {
        yield return new WaitForSeconds(stateDuration);
        SetState(State.Idle);
        returnToIdleRoutine = null;
    }

    private SpritePose GetPose(State state)
    {
        return state switch
        {
            State.Feed => feed,
            State.Peck1 => peck1,
            State.Peck2 => peck2,
            State.WrongFeed => wrongFeed,
            State.WrongPeck => wrongPeck,
            _ => idle
        };
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
            State.Peck1 => wormPeck1Position,
            State.Peck2 => wormPeck2Position,
            State.WrongFeed => wormWrongFeedPosition,
            State.WrongPeck => wormWrongPeckPosition,
            _ => wormIdlePosition
        };

        wormVisual.localPosition = new Vector3(position.x, position.y, wormVisual.localPosition.z);
    }
}
