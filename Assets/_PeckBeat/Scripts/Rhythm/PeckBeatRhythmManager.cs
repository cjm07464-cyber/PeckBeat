using UnityEngine;

public sealed class PeckBeatRhythmManager : MonoBehaviour
{
    private const double StartDelaySeconds = 1.0;

    [Header("Audio")]
    [SerializeField] private AudioSource bgmSource;

    [Header("Input")]
    [SerializeField] private PeckBeatInput input;
    [SerializeField] private float inputTimingOffset;

    [Header("Chart")]
    [TextArea(3, 10)]
    [SerializeField] private string chartText;

    private double scheduledStartDspTime;
    private double songTime;
    private int primaryCount;
    private int secondaryCount;

    public double SongTime => songTime;
    public bool IsSongPlaying => isActiveAndEnabled && bgmSource != null &&
                                 bgmSource.isPlaying && AudioSettings.dspTime >= scheduledStartDspTime;
    public double PlaybackSongTime => AudioSettings.dspTime - scheduledStartDspTime;

    private void Start()
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("PeckBeatRhythmManager: BGM AudioSource is not assigned.", this);
            return;
        }

        scheduledStartDspTime = AudioSettings.dspTime + StartDelaySeconds;
        bgmSource.PlayScheduled(scheduledStartDspTime);
    }

    private void Update()
    {
        if (bgmSource != null)
        {
            songTime = AudioSettings.dspTime - scheduledStartDspTime + inputTimingOffset;
        }

        if (input == null)
        {
            return;
        }

        if (input.ActionAPressedThisFrame())
        {
            primaryCount++;
            Debug.Log($"[ActionA] songTime: {songTime:F3}", this);
        }

        if (input.ActionBPressedThisFrame())
        {
            secondaryCount++;
            Debug.Log($"[ActionB] songTime: {songTime:F3}", this);
        }
    }

}
