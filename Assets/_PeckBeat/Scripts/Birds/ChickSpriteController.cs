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

    [Header("State Poses")]
    [SerializeField] private SpritePose idle = new SpritePose();
    [SerializeField] private SpritePose fed = new SpritePose();
    [SerializeField] private SpritePose exit = new SpritePose();

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

        SpritePose pose = state switch
        {
            State.Fed => fed,
            State.Exit => exit,
            _ => idle
        };

        pose?.Apply(spriteRenderer);
    }
}
