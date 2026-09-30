using UnityEngine;

public sealed class Stage01AudioController : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Stage 1 BGM")]
    [SerializeField] private AudioClip stageBgm;

    [Header("Stage 1 SFX")]
    [SerializeField] private AudioClip motherFeed;
    [SerializeField] private AudioClip motherPeck1;
    [SerializeField] private AudioClip motherPeck2;
    [SerializeField] private AudioClip miss;
    [SerializeField] private AudioClip chickEat;
    [SerializeField] private AudioClip cuckooHit1;
    [SerializeField] private AudioClip cuckooHit2;

    [Header("SFX Volumes")]
    [Range(0f, 2f)] [SerializeField] private float motherPeck1Volume = 1f;
    [Range(0f, 2f)] [SerializeField] private float motherPeck2Volume = 1f;

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

    public void PlayStageBgmScheduled(double dspStartTime)
    {
        if (bgmSource == null || stageBgm == null)
        {
            return;
        }

        bgmSource.clip = stageBgm;
        bgmSource.PlayScheduled(dspStartTime);
    }

    public void StopStageBgm()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void PlayMotherFeed() => PlaySfx(motherFeed);
    public void PlayMotherPeck1() => PlaySfx(motherPeck1, motherPeck1Volume);
    public void PlayMotherPeck2() => PlaySfx(motherPeck2, motherPeck2Volume);
    public void PlayMiss() => PlaySfx(miss);
    public void PlayChickEat() => PlaySfx(chickEat);
    public void PlayCuckooHit1() => PlaySfx(cuckooHit1);
    public void PlayCuckooHit2() => PlaySfx(cuckooHit2);

    private void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
