using System.Collections;
using UnityEngine;

public sealed class Stage02WoodpeckerController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite peckSprite;

    [Header("Timing")]
    [Min(0.01f)] [SerializeField] private float peckDuration = 0.12f;
    private Coroutine peckRoutine;
    private bool holding;
    private double holdStartSongTime;
    private double holdStrikeInterval;
    private double holdPeckEndTime;
    private int lastHoldStrike;

    private void Awake()
    {
        ShowIdle();
    }

    public void PlayPeck()
    {
        if (holding) return;
        StopPeckRoutine();
        peckRoutine = StartCoroutine(PeckThenIdle());
    }

    public void StartHoldPeck(double startSongTime, float beatInterval)
    {
        StopPeckRoutine();
        holding = true;
        holdStartSongTime = startSongTime;
        holdStrikeInterval = beatInterval * 0.125d;
        lastHoldStrike = -1;
        ShowIdle();
    }

    public void StopHoldPeck()
    {
        holding = false;
        ShowIdle();
    }

    // Called with the same DSP song clock used by the chart. Each eighth-beat
    // subdivision contains a complete Peck -> Idle pulse, for 16 pecks across a 2-beat hold.
    public void UpdateHoldPeck(double songTime)
    {
        if (!holding) return;
        double elapsed = songTime - holdStartSongTime;
        if (elapsed < 0d) return;
        int strike = (int)(elapsed / holdStrikeInterval);
        if (strike >= 16)
        {
            StopHoldPeck();
            return;
        }

        if (strike != lastHoldStrike)
        {
            lastHoldStrike = strike;
            double nextStrikeTime = holdStartSongTime + (strike + 1) * holdStrikeInterval;
            // Preserve an Idle gap even when the accepted first press is late.
            holdPeckEndTime = songTime + System.Math.Min(holdStrikeInterval * 0.5d,
                (nextStrikeTime - songTime) * 0.5d);
            ShowPeck();
        }
        else if (songTime >= holdPeckEndTime)
        {
            ShowIdle();
        }
    }

    private IEnumerator PeckThenIdle()
    {
        ShowPeck();
        yield return new WaitForSeconds(peckDuration);
        ShowIdle();
        peckRoutine = null;
    }

    private void StopPeckRoutine()
    {
        if (peckRoutine != null)
        {
            StopCoroutine(peckRoutine);
            peckRoutine = null;
        }
    }

    private void ShowIdle()
    {
        if (spriteRenderer != null && idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
    }

    private void ShowPeck()
    {
        if (spriteRenderer != null && peckSprite != null)
        {
            spriteRenderer.sprite = peckSprite;
        }
    }

    private void OnDisable()
    {
        StopPeckRoutine();
        StopHoldPeck();
    }
}
