using UnityEngine;

public sealed class Stage02AudioController : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Stage 2 Audio")]
    [SerializeField] private AudioClip stageBgm;
    [SerializeField] private AudioClip peckSfx;
    [SerializeField] private AudioClip holdPeckSfx;
    [SerializeField] private AudioClip completeSfx;
    [SerializeField] private AudioClip missSfx;

    [Header("Stage 2 Prepared SFX")]
    [SerializeField] private AudioClip normalPeckSfx;
    [SerializeField] private AudioClip breakFinalSfx;
    [SerializeField] private AudioClip twoHitFirstBreakSfx;
    [SerializeField] private AudioClip holdRapidBreakSfx;
    [SerializeField] private AudioClip whistleShortSfx;
    [SerializeField] private AudioClip whistleLongSfx;
    [SerializeField] private AudioClip perfectSfx;

    [Header("SFX Volumes")]
    [Range(0f, 2f)] [SerializeField] private float normalPeckVolume = 1f;
    [Range(0f, 2f)] [SerializeField] private float breakFinalVolume = 1f;
    [Range(0f, 2f)] [SerializeField] private float twoHitFirstBreakVolume = 1f;
    [Range(0f, 2f)] [SerializeField] private float holdRapidBreakVolume = 1f;
    [Range(0f, 2f)] [SerializeField] private float missVolume = 1f;
    [Range(0f, 2f)] [SerializeField] private float whistleShortVolume = 1f;
    [Range(0f, 2f)] [SerializeField] private float whistleLongVolume = 1f;
    [Range(0f, 2f)] [SerializeField] private float perfectVolume = 1f;

    private void Awake()
    {
        if (bgmSource != null)
        {
            bgmSource.playOnAwake = false;
            if (stageBgm != null)
            {
                bgmSource.clip = stageBgm;
            }
        }

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
        }
    }

    public void PlayPeck() => PlaySfx(peckSfx);

    public void PlayHoldPeck()
    {
        PlaySfx(holdPeckSfx != null ? holdPeckSfx : peckSfx);
    }

    public void PlayComplete() => PlaySfx(completeSfx);
    public void PlayMiss() => PlaySfx(missSfx, missVolume);
    public void PlayNormalPeck() => PlaySfx(normalPeckSfx, normalPeckVolume);
    public void PlayBreakFinal() => PlaySfx(breakFinalSfx, breakFinalVolume);
    public void PlayTwoHitFirstBreak() =>
        PlaySfx(twoHitFirstBreakSfx, twoHitFirstBreakVolume);
    public void PlayHoldBreak() => PlaySfx(holdRapidBreakSfx, holdRapidBreakVolume);
    public void PlayWhistleShort() => PlaySfx(whistleShortSfx, whistleShortVolume);
    public void PlayWhistleLong() => PlaySfx(whistleLongSfx, whistleLongVolume);
    public float GetWhistleLongDuration() => whistleLongSfx != null ? whistleLongSfx.length : 0f;
    public void PlayPerfect() => PlaySfx(perfectSfx, perfectVolume);

    private void PlaySfx(AudioClip clip)
    {
        PlaySfx(clip, 1f);
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, Mathf.Max(0f, volume));
        }
    }
}
