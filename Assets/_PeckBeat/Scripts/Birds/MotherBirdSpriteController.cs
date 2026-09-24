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

    [Header("State Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite feedSprite;
    [SerializeField] private Sprite peck1Sprite;
    [SerializeField] private Sprite peck2Sprite;
    [SerializeField] private Sprite wrongFeedSprite;
    [SerializeField] private Sprite wrongPeckSprite;

    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float stateDuration = 0.2f;

    private Coroutine returnToIdleRoutine;

    public State CurrentState { get; private set; }

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
        CurrentState = state;

        if (spriteRenderer == null)
        {
            return;
        }

        Sprite stateSprite = GetSprite(state);
        if (stateSprite != null)
        {
            spriteRenderer.sprite = stateSprite;
        }
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

    private Sprite GetSprite(State state)
    {
        return state switch
        {
            State.Feed => feedSprite,
            State.Peck1 => peck1Sprite,
            State.Peck2 => peck2Sprite,
            State.WrongFeed => wrongFeedSprite,
            State.WrongPeck => wrongPeckSprite,
            _ => idleSprite
        };
    }
}
