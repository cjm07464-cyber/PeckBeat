using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StageMusicProgressGauge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PeckBeatRhythmManager rhythmManager;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private Image progressImage;

    [Header("Reference Lookup")]
    [SerializeField] private bool autoFindReferences = true;

    private void Awake()
    {
        ConfigureProgressImage();
        ResolveReferences();
        SetProgress(0f);
    }

    private void OnEnable()
    {
        ResolveReferences();
        UpdateProgress();
    }

    private void Update()
    {
        UpdateProgress();
    }

    private void UpdateProgress()
    {
        if (rhythmManager == null || bgmSource == null || bgmSource.clip == null)
        {
            ResolveReferences();
        }

        if (progressImage == null || rhythmManager == null)
        {
            return;
        }

        double playbackTime = rhythmManager.PlaybackSongTime;
        if (playbackTime <= 0d)
        {
            SetProgress(0f);
            return;
        }

        double playbackDuration = GetPlaybackDuration();
        if (playbackDuration <= 0d)
        {
            SetProgress(0f);
            return;
        }

        SetProgress((float)(playbackTime / playbackDuration));
    }

    private double GetPlaybackDuration()
    {
        if (bgmSource == null || bgmSource.clip == null)
        {
            return 0d;
        }

        float playbackSpeed = Mathf.Max(0.01f, Mathf.Abs(bgmSource.pitch));
        return bgmSource.clip.length / playbackSpeed;
    }

    private void ResolveReferences()
    {
        if (!autoFindReferences)
        {
            return;
        }

        if (rhythmManager == null)
        {
            rhythmManager = Object.FindFirstObjectByType<PeckBeatRhythmManager>(
                FindObjectsInactive.Include);
        }

        if (bgmSource == null || bgmSource.clip == null)
        {
            bgmSource = FindStageBgmSource();
        }
    }

    private AudioSource FindStageBgmSource()
    {
        AudioSource[] sources = Object.FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        AudioSource bestSource = null;
        double bestDuration = 0d;
        bool bestSourceIsPlaying = false;

        foreach (AudioSource source in sources)
        {
            if (source == null || source.clip == null)
            {
                continue;
            }

            if (rhythmManager != null && source.gameObject.scene != rhythmManager.gameObject.scene)
            {
                continue;
            }

            float playbackSpeed = Mathf.Max(0.01f, Mathf.Abs(source.pitch));
            double duration = source.clip.length / playbackSpeed;
            bool sourceIsPlaying = source.isPlaying;

            if (bestSource == null ||
                (sourceIsPlaying && !bestSourceIsPlaying) ||
                (sourceIsPlaying == bestSourceIsPlaying && duration > bestDuration))
            {
                bestSource = source;
                bestDuration = duration;
                bestSourceIsPlaying = sourceIsPlaying;
            }
        }

        return bestSource;
    }

    private void ConfigureProgressImage()
    {
        if (progressImage == null)
        {
            return;
        }

        progressImage.type = Image.Type.Filled;
        progressImage.fillMethod = Image.FillMethod.Horizontal;
        progressImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private void SetProgress(float progress)
    {
        if (progressImage != null)
        {
            progressImage.fillAmount = Mathf.Clamp01(progress);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ConfigureProgressImage();
    }
#endif
}
