using UnityEngine;

public sealed class Stage02SculptTarget : MonoBehaviour
{
    [Header("Target Visual")]
    [SerializeField] private SpriteRenderer objectRenderer;
    [SerializeField] private SpriteRenderer signNumberRenderer;
    [SerializeField] private Stage02RatVisualController ratVisual;
    [SerializeField] private Stage02SculptObjectVisual sculptObjectVisual;
    [SerializeField] private Stage02HoldTargetVisual holdVisual;

    [Header("Sign Numbers")]
    [SerializeField] private Sprite number1Sprite;
    [SerializeField] private Sprite number2Sprite;

    [Header("Finished Objects")]
    [SerializeField] private Sprite sculptureASprite;
    [SerializeField] private Sprite sculptureBSprite;
    [SerializeField] private Sprite baseballBatSprite;

    private bool ratReachedSlot2;
    private bool ratStartedExitRun;
    private bool skipIdleOnArrival;

    public void BeginApproachVisual()
    {
        ratReachedSlot2 = false;
        ratStartedExitRun = false;
        skipIdleOnArrival = false;
        if (holdVisual != null)
        {
            holdVisual.ShowRun();
        }
        else
        {
            ratVisual?.PlayRun();
        }
    }

    public void UpdateApproachVisual(float progress)
    {
        if (ratReachedSlot2 || progress < 1f)
        {
            return;
        }

        ratReachedSlot2 = true;
        if (skipIdleOnArrival)
        {
            return;
        }

        if (holdVisual != null)
        {
            holdVisual.ShowIdle();
        }
        else
        {
            ratVisual?.ShowIdle();
        }
    }

    public void SetSkipIdleOnArrival(bool skip)
    {
        skipIdleOnArrival = skip;
    }

    public void BeginExitVisual()
    {
        if (ratStartedExitRun)
        {
            return;
        }

        ratStartedExitRun = true;
        if (holdVisual != null)
        {
            transform.position += holdVisual.ConsumeHoldMoveWorldOffsetForExit();
            return;
        }

        ratVisual?.PlayRun();
    }

    public void ConfigureNormal(int requiredHits)
    {
        sculptObjectVisual?.ConfigureNormal(requiredHits);

        if (signNumberRenderer == null)
        {
            return;
        }

        signNumberRenderer.enabled = true;
        Sprite numberSprite = requiredHits == 2 ? number2Sprite : number1Sprite;

        if (numberSprite != null)
        {
            signNumberRenderer.sprite = numberSprite;
        }
    }

    public void ApplyNormalHitVisual(int hitCount)
    {
        sculptObjectVisual?.ApplyNormalHitVisual(hitCount);

        // Future wood-chip effect hook: trigger it here after the valid hit visual is applied.
    }

    public void ConfigureHold()
    {
        holdVisual?.ResetHoldVisual();

        if (holdVisual != null)
        {
            if (ratVisual != null)
            {
                ratVisual.gameObject.SetActive(false);
            }

            if (objectRenderer != null)
            {
                objectRenderer.enabled = false;
            }
        }

        if (signNumberRenderer != null)
        {
            signNumberRenderer.enabled = false;
        }
    }

    public void SetHoldProgress01(float progress)
    {
        holdVisual?.SetHoldProgress01(progress);
    }

    public void CaptureHoldMoveStart(Vector3 slot2ToSlot3WorldDirection)
    {
        holdVisual?.CaptureHoldMoveStart(slot2ToSlot3WorldDirection);
    }

    public void PlayHoldChipOrShakePulse()
    {
        holdVisual?.PlayHoldChipOrShakePulse();
    }

    public void ResetHoldVisual()
    {
        holdVisual?.ResetHoldVisual();
    }

    public void ShowSculpture(Stage02SculptingChart.SculptureType sculptureType)
    {
        if (sculptObjectVisual != null)
        {
            sculptObjectVisual.ShowCompletedVisual();
            return;
        }

        Sprite resultSprite = sculptureType switch
        {
            Stage02SculptingChart.SculptureType.SculptureB => sculptureBSprite,
            _ => sculptureASprite
        };

        SetObjectSprite(resultSprite);
    }

    public void ShowBaseballBat()
    {
        if (holdVisual != null)
        {
            holdVisual.SetHoldProgress01(1f);
            return;
        }

        SetObjectSprite(baseballBatSprite);
    }

    private void SetObjectSprite(Sprite sprite)
    {
        if (objectRenderer != null && sprite != null)
        {
            objectRenderer.sprite = sprite;
        }
    }
}
