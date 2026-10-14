using System;
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
    private bool playbackPaused;
    private double pauseStartedDspTime;

    public double SongTime => songTime;
    public bool IsSongPlaying => !playbackPaused && isActiveAndEnabled && bgmSource != null &&
                                 bgmSource.isPlaying && AudioSettings.dspTime >= scheduledStartDspTime;
    public bool IsPlaybackPaused => playbackPaused;
    public double PlaybackSongTime =>
        (playbackPaused ? pauseStartedDspTime : AudioSettings.dspTime) - scheduledStartDspTime;
    public bool HasSongEnded
    {
        get
        {
            if (bgmSource == null || bgmSource.clip == null || scheduledStartDspTime <= 0d)
            {
                return false;
            }

            double playbackSpeed = Mathf.Max(0.01f, Mathf.Abs(bgmSource.pitch));
            double scheduledDuration = bgmSource.clip.length / playbackSpeed;
            return PlaybackSongTime >= scheduledDuration;
        }
    }

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
        if (bgmSource != null && !playbackPaused)
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

    public bool PausePlayback()
    {
        if (playbackPaused || !IsSongPlaying || bgmSource == null)
        {
            return false;
        }

        pauseStartedDspTime = AudioSettings.dspTime;
        playbackPaused = true;
        bgmSource.Pause();
        return true;
    }

    public void ResumePlayback()
    {
        if (!playbackPaused)
        {
            return;
        }

        double pauseDuration = Math.Max(0d, AudioSettings.dspTime - pauseStartedDspTime);
        scheduledStartDspTime += pauseDuration;
        playbackPaused = false;
        pauseStartedDspTime = 0d;
        bgmSource?.UnPause();
    }

}
