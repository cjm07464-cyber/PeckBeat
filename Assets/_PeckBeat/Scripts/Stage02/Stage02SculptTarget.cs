using UnityEngine;

public sealed class Stage02SculptTarget : MonoBehaviour
{
    [Header("Target Visual")]
    [SerializeField] private SpriteRenderer objectRenderer;
    [SerializeField] private SpriteRenderer signNumberRenderer;

    [Header("Sign Numbers")]
    [SerializeField] private Sprite number1Sprite;
    [SerializeField] private Sprite number2Sprite;

    [Header("Finished Objects")]
    [SerializeField] private Sprite sculptureASprite;
    [SerializeField] private Sprite sculptureBSprite;
    [SerializeField] private Sprite baseballBatSprite;

    public void ConfigureNormal(int requiredHits)
    {
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

    public void ConfigureHold()
    {
        if (signNumberRenderer != null)
        {
            signNumberRenderer.enabled = false;
        }
    }

    public void ShowSculpture(Stage02SculptingChart.SculptureType sculptureType)
    {
        Sprite resultSprite = sculptureType switch
        {
            Stage02SculptingChart.SculptureType.SculptureB => sculptureBSprite,
            _ => sculptureASprite
        };

        SetObjectSprite(resultSprite);
    }

    public void ShowBaseballBat()
    {
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
