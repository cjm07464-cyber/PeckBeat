using UnityEngine;

public sealed class ChickSpriteController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Fed,
        Exit
    }

    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("State Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite fedSprite;
    [SerializeField] private Sprite exitSprite;

    public State CurrentState { get; private set; }

    private void Awake()
    {
        SetState(State.Idle);
    }

    public void SetState(State state)
    {
        CurrentState = state;

        if (spriteRenderer == null)
        {
            return;
        }

        Sprite stateSprite = state switch
        {
            State.Fed => fedSprite,
            State.Exit => exitSprite,
            _ => idleSprite
        };

        if (stateSprite != null)
        {
            spriteRenderer.sprite = stateSprite;
        }
    }
}
