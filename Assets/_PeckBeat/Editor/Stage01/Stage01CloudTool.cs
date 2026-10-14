using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage01CloudTool
{
    private const string Stage01ScenePath = "Assets/Scenes/Stage01_Prototype.unity";
    private const string CloudSpriteFolder = "Assets/_PeckBeat/Art/Stage_01/Clouds/Test/";
    private const float RecommendedMinSpeed = 0.625f;
    private const float RecommendedMaxSpeed = 2.5f;

    private static readonly string[] SpawnPointNames =
    {
        "Spawn_Upper",
        "Spawn_Lower",
        "Spawn_Lower (1)"
    };

    private static readonly string[] ExitPointNames =
    {
        "Exit_Upper",
        "Exit_Lower",
        "Exit_Lower (1)"
    };

    private static readonly string[] CloudSpriteFileNames =
    {
        "cloud-01.png",
        "cloud-02.png",
        "cloud-03.png",
        "cloud-04.png",
        "cloud-08.png"
    };

    [MenuItem("PeckBeat/Stage01/Clouds/Refresh Cloud Spawn Points")]
    private static void RefreshCloudSpawnPoints()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage01ScenePath)
        {
            Debug.LogWarning($"[Stage01CloudTool] Open {Stage01ScenePath} before refreshing cloud spawn points.");
            return;
        }

        CloudSpawner cloudSpawner = FindCloudSpawner(scene);
        if (cloudSpawner == null)
        {
            Debug.LogWarning("[Stage01CloudTool] CloudSystem with CloudSpawner was not found in the active scene.");
            return;
        }

        Transform cloudSystem = cloudSpawner.transform;
        Transform[] spawnPoints = FindNamedChildren(cloudSystem, SpawnPointNames);
        Transform[] exitPoints = FindNamedChildren(cloudSystem, ExitPointNames);
        if (spawnPoints == null || exitPoints == null)
        {
            return;
        }

        SerializedObject serializedSpawner = new SerializedObject(cloudSpawner);
        SerializedProperty spawnPointsProperty = serializedSpawner.FindProperty("spawnPoints");
        SerializedProperty exitPointsProperty = serializedSpawner.FindProperty("exitPoints");
        SerializedProperty minSpeedProperty = serializedSpawner.FindProperty("minSpeed");
        SerializedProperty maxSpeedProperty = serializedSpawner.FindProperty("maxSpeed");
        if (spawnPointsProperty == null || !spawnPointsProperty.isArray ||
            exitPointsProperty == null || !exitPointsProperty.isArray ||
            minSpeedProperty == null || maxSpeedProperty == null)
        {
            Debug.LogError("[Stage01CloudTool] Required CloudSpawner serialized fields could not be found.");
            return;
        }

        float previousMinSpeed = minSpeedProperty.floatValue;
        float previousMaxSpeed = maxSpeedProperty.floatValue;

        Undo.RecordObject(cloudSpawner, "Refresh Stage01 Cloud Spawn Points");
        AssignTransformArray(spawnPointsProperty, spawnPoints);
        AssignTransformArray(exitPointsProperty, exitPoints);
        minSpeedProperty.floatValue = RecommendedMinSpeed;
        maxSpeedProperty.floatValue = RecommendedMaxSpeed;

        serializedSpawner.ApplyModifiedProperties();
        EditorUtility.SetDirty(cloudSpawner);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = cloudSpawner.gameObject;

        Debug.Log(
            $"[Stage01CloudTool] Refreshed 3 cloud lanes on {GetHierarchyPath(cloudSystem)}. " +
            $"Speed: {previousMinSpeed:0.###}-{previousMaxSpeed:0.###} -> " +
            $"{RecommendedMinSpeed:0.###}-{RecommendedMaxSpeed:0.###}. " +
            "Existing cloud sprite candidates were preserved. The scene was not saved.",
            cloudSpawner);
    }

    [MenuItem("PeckBeat/Stage01/Clouds/Add Test Cloud Sprites To Candidates")]
    private static void AddTestCloudSpritesToCandidates()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage01ScenePath)
        {
            Debug.LogWarning($"[Stage01CloudTool] Open {Stage01ScenePath} before adding cloud sprites.");
            return;
        }

        CloudSpawner cloudSpawner = FindCloudSpawner(scene);
        if (cloudSpawner == null)
        {
            Debug.LogWarning("[Stage01CloudTool] CloudSystem with CloudSpawner was not found in the active scene.");
            return;
        }

        SerializedObject serializedSpawner = new SerializedObject(cloudSpawner);
        SerializedProperty cloudSprites = serializedSpawner.FindProperty("cloudSprites");
        if (cloudSprites == null || !cloudSprites.isArray)
        {
            Debug.LogError("[Stage01CloudTool] CloudSpawner.cloudSprites could not be found.");
            return;
        }

        var existingSprites = new HashSet<Sprite>();
        for (int index = 0; index < cloudSprites.arraySize; index++)
        {
            Sprite existingSprite = cloudSprites.GetArrayElementAtIndex(index).objectReferenceValue as Sprite;
            if (existingSprite != null)
            {
                existingSprites.Add(existingSprite);
            }
        }

        var spritesToAdd = new List<Sprite>();
        foreach (string fileName in CloudSpriteFileNames)
        {
            string assetPath = CloudSpriteFolder + fileName;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Debug.LogWarning($"[Stage01CloudTool] Sprite was not found and was skipped: {assetPath}");
                continue;
            }

            if (!existingSprites.Add(sprite))
            {
                Debug.Log($"[Stage01CloudTool] Cloud candidate already exists: {fileName}");
                continue;
            }

            spritesToAdd.Add(sprite);
        }

        if (spritesToAdd.Count == 0)
        {
            Debug.Log("[Stage01CloudTool] No new cloud candidates were added. Existing candidates were preserved.");
            Selection.activeGameObject = cloudSpawner.gameObject;
            return;
        }

        Undo.RecordObject(cloudSpawner, "Add Stage01 Cloud Sprite Candidates");
        int firstNewIndex = cloudSprites.arraySize;
        cloudSprites.arraySize += spritesToAdd.Count;
        for (int index = 0; index < spritesToAdd.Count; index++)
        {
            cloudSprites.GetArrayElementAtIndex(firstNewIndex + index).objectReferenceValue = spritesToAdd[index];
        }

        serializedSpawner.ApplyModifiedProperties();
        EditorUtility.SetDirty(cloudSpawner);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = cloudSpawner.gameObject;

        Debug.Log(
            $"[Stage01CloudTool] Added {spritesToAdd.Count} cloud candidate(s) to {GetHierarchyPath(cloudSpawner.transform)}. " +
            $"Total candidates: {cloudSprites.arraySize}. Existing candidates were preserved. The scene was not saved.",
            cloudSpawner);
    }

    private static CloudSpawner FindCloudSpawner(Scene scene)
    {
        CloudSpawner[] spawners = Resources.FindObjectsOfTypeAll<CloudSpawner>();
        foreach (CloudSpawner spawner in spawners)
        {
            if (spawner != null && spawner.gameObject.scene == scene && !EditorUtility.IsPersistent(spawner))
            {
                return spawner;
            }
        }

        return null;
    }

    private static Transform[] FindNamedChildren(Transform root, string[] names)
    {
        var results = new Transform[names.Length];
        var usedTransforms = new HashSet<Transform>();
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
        {
            foreach (Transform child in children)
            {
                if (child != null && child.name == names[nameIndex] && usedTransforms.Add(child))
                {
                    results[nameIndex] = child;
                    break;
                }
            }

            if (results[nameIndex] == null)
            {
                Debug.LogWarning(
                    $"[Stage01CloudTool] Required cloud point was not found under {GetHierarchyPath(root)}: {names[nameIndex]}",
                    root);
                return null;
            }
        }

        return results;
    }

    private static void AssignTransformArray(SerializedProperty arrayProperty, Transform[] values)
    {
        arrayProperty.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
        {
            arrayProperty.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null)
        {
            return "<missing>";
        }

        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }
}
