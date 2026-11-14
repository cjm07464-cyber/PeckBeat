using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Stage01CompositionOffsetTool : EditorWindow
{
    private const string MenuPath = "PeckBeat/Stage01/Adjust Gameplay Composition Y";
    private const string Stage01ScenePath = "Assets/Scenes/Stage01_Prototype.unity";
    private float desiredYOffset = -0.4f;

    [MenuItem(MenuPath)]
    private static void OpenWindow()
    {
        GetWindow<Stage01CompositionOffsetTool>(true, "Stage01 Composition Y");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Stage01 Gameplay Composition", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Moves GamePlayRoot plus StageRoot Branch/Nest visuals only. "
            + "Sky, Clouds, camera, and UI are excluded. Existing X/Z, scale, rotation, "
            + "and sorting settings are preserved.",
            MessageType.Info);

        desiredYOffset = EditorGUILayout.FloatField("Desired Y Offset", desiredYOffset);

        Scene scene = SceneManager.GetActiveScene();
        Stage01CompositionOffsetMarker marker = FindSceneComponent<Stage01CompositionOffsetMarker>(scene);
        EditorGUILayout.LabelField(
            "Currently Applied",
            marker != null ? marker.AppliedYOffset.ToString("0.###") : "Not initialized");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Offset"))
            {
                ApplyDesiredOffset(desiredYOffset, false);
            }

            if (GUILayout.Button("Reset To Baseline"))
            {
                ApplyDesiredOffset(0f, false);
                desiredYOffset = 0f;
            }
        }

        if (GUILayout.Button("Recapture Current Positions As Zero"))
        {
            ApplyDesiredOffset(0f, true);
            desiredYOffset = 0f;
        }
    }

    private static void ApplyDesiredOffset(float desiredOffset, bool forceRecapture)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage01ScenePath)
        {
            Debug.LogWarning(
                $"[Stage01CompositionOffset] Open {Stage01ScenePath} before applying an offset.");
            return;
        }

        Transform gamePlayRoot = FindSceneTransform(scene, "GamePlayRoot");
        Transform stageRoot = FindSceneTransform(scene, "StageRoot");
        if (gamePlayRoot == null || stageRoot == null)
        {
            Debug.LogWarning(
                "[Stage01CompositionOffset] GamePlayRoot or StageRoot was not found.");
            return;
        }

        List<Transform> targets = CollectTargets(gamePlayRoot, stageRoot);
        if (targets.Count <= 1)
        {
            Debug.LogWarning(
                "[Stage01CompositionOffset] Branch/Nest visual targets were not found.",
                stageRoot);
            return;
        }

        Stage01CompositionOffsetMarker marker =
            gamePlayRoot.GetComponent<Stage01CompositionOffsetMarker>();
        if (marker == null)
        {
            marker = Undo.AddComponent<Stage01CompositionOffsetMarker>(gamePlayRoot.gameObject);
            forceRecapture = true;
        }

        Undo.RecordObject(marker, "Apply Stage01 Composition Y Offset");
        if (forceRecapture || !marker.Matches(targets))
        {
            marker.CaptureBaseline(targets);
        }

        var objectsToRecord = new UnityEngine.Object[targets.Count];
        for (int i = 0; i < targets.Count; i++)
        {
            objectsToRecord[i] = targets[i];
        }

        Undo.RecordObjects(objectsToRecord, "Move Stage01 Gameplay Composition");
        marker.ApplyOffset(desiredOffset);
        EditorUtility.SetDirty(marker);
        foreach (Transform target in targets)
        {
            EditorUtility.SetDirty(target);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = gamePlayRoot.gameObject;

        Debug.Log(
            "[Stage01CompositionOffset] Applied absolute Y offset "
            + $"{desiredOffset:0.###} to: {DescribeTargets(targets)}. "
            + "Sky, Clouds, camera, and UI were not moved. The scene was not saved.",
            marker);
    }

    private static List<Transform> CollectTargets(Transform gamePlayRoot, Transform stageRoot)
    {
        var targets = new List<Transform> { gamePlayRoot };
        var visualCandidates = new List<Transform>();
        CollectVisualCandidates(stageRoot, visualCandidates);

        foreach (Transform candidate in visualCandidates)
        {
            if (!HasSelectedVisualAncestor(candidate, visualCandidates))
            {
                targets.Add(candidate);
            }
        }

        return targets;
    }

    private static void CollectVisualCandidates(Transform parent, List<Transform> results)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.Equals("Branch", StringComparison.Ordinal) ||
                child.name.StartsWith("Nest", StringComparison.Ordinal))
            {
                results.Add(child);
            }

            CollectVisualCandidates(child, results);
        }
    }

    private static bool HasSelectedVisualAncestor(
        Transform candidate,
        List<Transform> visualCandidates)
    {
        Transform ancestor = candidate.parent;
        while (ancestor != null)
        {
            if (visualCandidates.Contains(ancestor))
            {
                return true;
            }

            ancestor = ancestor.parent;
        }

        return false;
    }

    private static string DescribeTargets(List<Transform> targets)
    {
        var names = new string[targets.Count];
        for (int i = 0; i < targets.Count; i++)
        {
            names[i] = targets[i] != null ? targets[i].name : "<missing>";
        }

        return string.Join(", ", names);
    }

    private static Transform FindSceneTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindRecursive(root.transform, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindRecursive(parent.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid())
        {
            return null;
        }

        T[] components = UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (T component in components)
        {
            if (component != null && component.gameObject.scene == scene)
            {
                return component;
            }
        }

        return null;
    }
}
