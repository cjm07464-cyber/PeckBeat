using System;
using UnityEngine;

[Serializable]
public sealed class SpritePose
{
    public Sprite sprite;
    public Vector2 positionOffset = Vector2.zero;
    public Vector2 scale = Vector2.one;
    public bool flipX;

    public void Apply(SpriteRenderer renderer)
    {
        if (renderer == null) return;

        // Preserve the existing missing-sprite fallback, but always apply this pose.
        if (sprite != null) renderer.sprite = sprite;
        renderer.flipX = flipX;
        Transform visual = renderer.transform;
        visual.localPosition = new Vector3(positionOffset.x, positionOffset.y, visual.localPosition.z);
        visual.localScale = new Vector3(scale.x, scale.y, visual.localScale.z);
    }
}
