using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Stage01CompositionOffsetMarker : MonoBehaviour
{
    [SerializeField, HideInInspector] private float appliedYOffset;
    [SerializeField, HideInInspector] private Transform[] targets;
    [SerializeField, HideInInspector] private Vector3[] baselineLocalPositions;

    public float AppliedYOffset => appliedYOffset;
    public int TargetCount => targets?.Length ?? 0;

    public bool Matches(IReadOnlyList<Transform> expectedTargets)
    {
        if (expectedTargets == null || targets == null ||
            baselineLocalPositions == null ||
            targets.Length != expectedTargets.Count ||
            baselineLocalPositions.Length != expectedTargets.Count)
        {
            return false;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != expectedTargets[i])
            {
                return false;
            }
        }

        return true;
    }

    public void CaptureBaseline(IReadOnlyList<Transform> newTargets)
    {
        int count = newTargets?.Count ?? 0;
        targets = new Transform[count];
        baselineLocalPositions = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            targets[i] = newTargets[i];
            baselineLocalPositions[i] = newTargets[i] != null
                ? newTargets[i].localPosition
                : Vector3.zero;
        }

        appliedYOffset = 0f;
    }

    public void ApplyOffset(float yOffset)
    {
        if (targets == null || baselineLocalPositions == null)
        {
            return;
        }

        int count = Mathf.Min(targets.Length, baselineLocalPositions.Length);
        for (int i = 0; i < count; i++)
        {
            Transform target = targets[i];
            if (target == null)
            {
                continue;
            }

            Vector3 position = baselineLocalPositions[i];
            position.y += yOffset;
            target.localPosition = position;
        }

        appliedYOffset = yOffset;
    }

    public Transform GetTarget(int index)
    {
        return targets != null && index >= 0 && index < targets.Length
            ? targets[index]
            : null;
    }
}
