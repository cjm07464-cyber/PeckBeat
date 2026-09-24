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

    [Header("State Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite fedSprite;
    [SerializeField] private Sprite exitSprite;
    [SerializeField] private Sprite hit1Sprite;
    [SerializeField] private Sprite hit2Sprite;

    [Header("Hit2 Knockaway")]
    [Min(0.01f)] [SerializeField] private float knockawayDuration = 1.2f;
    [SerializeField] private float horizontalDistance = 2f;
    [Min(0.01f)] [SerializeField] private float arcHeight = 3f;
    [Min(0.01f)] [SerializeField] private float fallDistance = 7f;

    private bool knockingAway;
    private bool descending;
    private double knockawayStartTime;
    private Vector3 knockawayStartPosition;

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

        Sprite stateSprite = state switch
        {
            State.Fed => fedSprite,
            State.Exit => exitSprite,
            State.Hit1 => hit1Sprite,
            State.Hit2 => hit2Sprite,
            _ => idleSprite
        };

        if (stateSprite != null)
        {
            spriteRenderer.sprite = stateSprite;
        }
    }
}
