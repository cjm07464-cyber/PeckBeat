using DG.Tweening;
using UnityEngine;

public sealed class CuckooSpriteController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Fed,
        Exit,
        Hit1,
        Hit2
    }

    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("State Poses")]
    [SerializeField] private SpritePose idle = new SpritePose();
    [SerializeField] private SpritePose fed = new SpritePose();
    [SerializeField] private SpritePose exit = new SpritePose();
    [SerializeField] private SpritePose hit1 = new SpritePose();
    [SerializeField] private SpritePose hit2 = new SpritePose();

    [Header("Hit2 Knockaway")]
    [SerializeField] private Transform rotationVisual;
    [Min(0.01f)] [SerializeField] private float knockawayDuration = 1.2f;
    [SerializeField] private float horizontalDistance = 2f;
    [Min(0.01f)] [SerializeField] private float arcHeight = 7f;
    [Min(0.01f)] [SerializeField] private float fallDistance = 7f;
    [Range(0f, 6f)] [SerializeField] private float rotationTurns = 2.5f;

    private bool knockingAway;
    private bool descending;
    private double knockawayStartTime;
    private Vector3 knockawayStartPosition;
    private Tween rotationTween;

    public State CurrentState { get; private set; }

    private void Awake()
    {
        SetState(State.Idle);
    }

    public void PlayKnockaway()
    {
        if (knockingAway || CurrentState != State.Hit2)
        {
            return;
        }

        descending = false;
        knockawayStartPosition = transform.position;
        knockawayStartTime = AudioSettings.dspTime;
        knockingAway = true;

        if (rotationVisual != null)
        {
            rotationTween?.Kill();
            rotationTween = rotationVisual.DOLocalRotate(
                    new Vector3(0f, 0f, -360f * Mathf.Max(0f, rotationTurns)),
                    Mathf.Max(0.01f, knockawayDuration),
                    RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear);
        }
    }

    private void Update()
    {
        if (!knockingAway)
        {
            return;
        }

        float t = Mathf.Clamp01((float)((AudioSettings.dspTime - knockawayStartTime) /
                                      Mathf.Max(0.01f, knockawayDuration)));
        float height = Mathf.Max(0.01f, arcHeight);
        float fall = Mathf.Max(0.01f, fallDistance);
        // y = rise*t - (rise+fall)*t*t: apex is arcHeight, endpoint is -fallDistance.
        float rise = 2f * (height + Mathf.Sqrt(height * (height + fall)));
        // The vertical derivative changes sign at the apex.
        float apexTime = rise / (2f * (rise + fall));
        if (!descending && t > apexTime)
        {
            descending = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = -29;
            }
        }

        float y = rise * t - (rise + fall) * t * t;
        transform.position = knockawayStartPosition + new Vector3(horizontalDistance * t, y, 0f);

        if (t >= 1f)
        {
            Destroy(gameObject);
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
            State.Hit1 => hit1,
            State.Hit2 => hit2,
            _ => idle
        };

        pose?.Apply(spriteRenderer);
    }

    private void OnDestroy()
    {
        rotationTween?.Kill();
        rotationTween = null;
    }
}
