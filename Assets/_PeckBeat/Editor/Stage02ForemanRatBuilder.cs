using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage02ForemanRatBuilder
{
    private const string MenuPath = "PeckBeat/Stage02/Build Foreman Rat";
    private const string Stage02ScenePath = "Assets/Scenes/Stage02.unity";
    private static readonly Vector3 DefaultOffsetFromWoodpecker = new Vector3(-2f, 0f, 0f);
    private static readonly Vector3 DefaultOffsetFromTreeRing = new Vector3(-2f, 1.5f, 0f);
    private const int DefaultSortingOrder = 5;

    [MenuItem(MenuPath)]
    private static void BuildForemanRat()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage02ScenePath)
        {
            Debug.LogWarning(
                $"[Stage02ForemanRatBuilder] Open {Stage02ScenePath} before running this menu.");
            return;
        }

        Transform parent = FindSceneTransform(scene, "GameplayRoot")
            ?? FindSceneTransform(scene, "StageRoot");
        if (parent == null)
        {
            Debug.LogWarning(
                "[Stage02ForemanRatBuilder] GameplayRoot or StageRoot was not found.");
            return;
        }

        Transform foremanRat = FindTransformByName(scene, "ForemanRat");
        bool foremanCreated = foremanRat == null;
        if (foremanCreated)
        {
            foremanRat = CreateChild(parent, "ForemanRat");
            ApplyDefaultPosition(scene, parent, foremanRat);
        }

        Transform visual = foremanRat.Find("Visual");
        bool visualCreated = visual == null;
        if (visualCreated)
        {
            visual = CreateChild(foremanRat, "Visual");
        }

        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        bool rendererCreated = renderer == null;
        if (rendererCreated)
        {
            renderer = Undo.AddComponent<SpriteRenderer>(visual.gameObject);
        }

        if (rendererCreated || visualCreated)
        {
            SpriteRenderer referenceRenderer = GetWoodpeckerReferenceRenderer(scene);
            Undo.RecordObject(renderer, "Configure Foreman Rat Renderer");
            if (referenceRenderer != null)
            {
                renderer.sharedMaterial = referenceRenderer.sharedMaterial;
                renderer.sortingLayerID = referenceRenderer.sortingLayerID;
                renderer.sortingOrder = referenceRenderer.sortingOrder + 1;
            }
            else
            {
                renderer.sortingOrder = DefaultSortingOrder;
            }
        }

        Stage02ForemanRatVisual foremanVisual =
            foremanRat.GetComponent<Stage02ForemanRatVisual>();
        if (foremanVisual == null)
        {
            foremanVisual = Undo.AddComponent<Stage02ForemanRatVisual>(foremanRat.gameObject);
        }

        SerializedObject serializedVisual = new SerializedObject(foremanVisual);
        SerializedProperty rendererProperty = serializedVisual.FindProperty("visualRenderer");
        if (rendererProperty != null)
        {
            rendererProperty.objectReferenceValue = renderer;
        }
        serializedVisual.ApplyModifiedProperties();

        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(foremanVisual);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = foremanRat.gameObject;

        Debug.Log(
            "[Stage02ForemanRatBuilder] ForemanRat/Visual was created or reused. "
            + "Assign Idle Sprite and Whistle Sprite in the Inspector. "
            + "The scene was marked dirty but was not saved.",
            foremanRat.gameObject);
    }

    private static void ApplyDefaultPosition(Scene scene, Transform parent, Transform foremanRat)
    {
        Transform woodpecker = FindSceneTransform(scene, "GameplayRoot/Woodpecker");
        Transform treeRing = FindSceneTransform(scene, "StageRoot/TreeRing");

        Undo.RecordObject(foremanRat, "Position Foreman Rat");
        if (woodpecker != null && woodpecker.parent == parent)
        {
            foremanRat.localPosition = woodpecker.localPosition + DefaultOffsetFromWoodpecker;
        }
        else if (treeRing != null)
        {
            foremanRat.position = treeRing.position + DefaultOffsetFromTreeRing;
        }
        else
        {
            foremanRat.localPosition = Vector3.zero;
        }
    }

    private static SpriteRenderer GetWoodpeckerReferenceRenderer(Scene scene)
    {
        Transform woodpecker = FindSceneTransform(scene, "GameplayRoot/Woodpecker");
        return woodpecker != null
            ? woodpecker.GetComponentInChildren<SpriteRenderer>(true)
            : null;
    }

    private static Transform CreateChild(Transform parent, string objectName)
    {
        GameObject childObject = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(childObject, $"Create {objectName}");
        Undo.SetTransformParent(childObject.transform, parent, $"Parent {objectName}");
        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        return childObject.transform;
    }

    private static Transform FindTransformByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChildRecursive(root.transform, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindSceneTransform(Scene scene, string hierarchyPath)
    {
        string[] pathParts = hierarchyPath.Split('/');
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != pathParts[0])
            {
                continue;
            }

            Transform current = root.transform;
            for (int i = 1; i < pathParts.Length && current != null; i++)
            {
                current = current.Find(pathParts[i]);
            }

            if (current != null)
            {
                return current;
            }
        }

        return null;
    }
}
