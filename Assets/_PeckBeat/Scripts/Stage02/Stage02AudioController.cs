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
    public void PlayMiss() => PlaySfx(missSfx);

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}
