using System.Collections;
using UnityEngine;

public sealed class Stage02WoodpeckerController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Stage02WoodpeckerIdleVisual idleVisual;

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
    private bool holdCompositeVisualActive;

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

    public void PlayPerfectSmile()
    {
        if (idleVisual != null)
        {
            idleVisual.PlayPerfectSmile();
        }
    }

    public void PlayMissPeckFace()
    {
        if (idleVisual != null)
        {
            idleVisual.PlayMissPeckFace();
        }
    }

    public void StartHoldPeck(double startSongTime, float beatInterval)
    {
        StopPeckRoutine();
        holding = true;
        holdStartSongTime = startSongTime;
        holdStrikeInterval = beatInterval * 0.125d;
        lastHoldStrike = -1;
        holdCompositeVisualActive = false;
        ShowIdle();
        idleVisual?.StartHoldRapidPeckVisual();
    }

    public void StopHoldPeck()
    {
        holding = false;
        holdCompositeVisualActive = false;
        idleVisual?.StopHoldRapidPeckVisual();
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
            ShowHoldPeck();
        }
        else if (songTime >= holdPeckEndTime)
        {
            // A composite peck restores its own head parts after peckHeadDuration.
            // Preserve the legacy Idle gap only for the full-sprite fallback.
            if (!holdCompositeVisualActive)
            {
                ShowIdle();
            }
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
        bool splitIdleVisible = false;
        if (idleVisual != null)
        {
            // Keep the split body visible during a hold. Only the three head
            // parts are swapped by the hold peck visual.
            splitIdleVisible = idleVisual.SetIdleVisible(true);
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.enabled = !splitIdleVisible;
        if (!splitIdleVisible && idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
    }

    private void ShowPeck()
    {
        if (idleVisual != null && idleVisual.PlayPeckHeadComposite())
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            return;
        }

        ShowLegacyPeckFallback();
    }

    private void ShowHoldPeck()
    {
        if (idleVisual != null && idleVisual.PlayHoldPeckVisual())
        {
            holdCompositeVisualActive = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            return;
        }

        holdCompositeVisualActive = false;
        ShowLegacyPeckFallback();
    }

    private void ShowLegacyPeckFallback()
    {
        if (idleVisual != null)
        {
            idleVisual.SetIdleVisible(false);
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.enabled = true;
        if (peckSprite != null)
        {
            spriteRenderer.sprite = peckSprite;
        }
        else if (idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
    }

    private void OnDisable()
    {
        StopPeckRoutine();
        StopHoldPeck();
    }
}
