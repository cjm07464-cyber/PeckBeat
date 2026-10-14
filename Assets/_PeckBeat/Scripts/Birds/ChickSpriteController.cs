using UnityEngine;

public sealed class ChickSpriteController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Fed,
        Exit,
        FailedExit
    }

    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("State Poses")]
    [SerializeField] private SpritePose idle = new SpritePose();
    [SerializeField] private SpritePose fed = new SpritePose();
    [SerializeField] private SpritePose exit = new SpritePose();
    [SerializeField] private SpritePose failedExit = new SpritePose();

    public State CurrentState { get; private set; }

    private void OnEnable()
    {
        SetVisualVisible(true);
    }

    private void Awake()
    {
        SetState(State.Idle);
    }

    public void SetVisualVisible(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }
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
            State.FailedExit => failedExit != null && failedExit.sprite != null ? failedExit : exit,
            _ => idle
        };

        pose?.Apply(spriteRenderer);
    }
}
