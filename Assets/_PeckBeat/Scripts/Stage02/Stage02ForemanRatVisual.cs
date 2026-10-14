using System.Collections;
using UnityEngine;

public sealed class Stage02ForemanRatVisual : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer visualRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite whistleSprite;

    private Coroutine whistleRoutine;

    private void Awake()
    {
        ShowIdle();
    }

    public void ShowIdle()
    {
        ApplySprite(idleSprite);
    }

    public void ShowWhistle()
    {
        ApplySprite(whistleSprite);
    }

    public void PlayWhistle(float duration)
    {
        if (visualRenderer == null || whistleSprite == null)
        {
            return;
        }

        if (whistleRoutine != null)
        {
            StopCoroutine(whistleRoutine);
        }

        whistleRoutine = StartCoroutine(PlayWhistleRoutine(Mathf.Max(0f, duration)));
    }

    private IEnumerator PlayWhistleRoutine(float duration)
    {
        ShowWhistle();

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        ShowIdle();
        whistleRoutine = null;
    }

    private void ApplySprite(Sprite sprite)
    {
        if (visualRenderer == null || sprite == null)
        {
            return;
        }

        visualRenderer.sprite = sprite;
        visualRenderer.enabled = true;
    }

    private void OnDisable()
    {
        whistleRoutine = null;
    }
}
