using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StageProgressGaugeArtApplier
{
    private const string MenuPath = "PeckBeat/Shared/Apply Progress Gauge Art";
    private const string Stage01ScenePath = "Assets/Scenes/Stage01_Prototype.unity";
    private const string Stage02ScenePath = "Assets/Scenes/Stage02.unity";
    private const string BackgroundSpritePath =
        "Assets/_PeckBeat/Art/Shared/ProgressGauge/progress_bg.png";
    private const string FillSpritePath =
        "Assets/_PeckBeat/Art/Shared/ProgressGauge/progress_fill_white.png";
    private const string FrameSpritePath =
        "Assets/_PeckBeat/Art/Shared/ProgressGauge/progress_frame.png";

    private static readonly Color DefaultFillColor = new(0.22f, 0.82f, 0.36f, 1f);

    [MenuItem(MenuPath)]
    private static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() ||
            (scene.path != Stage01ScenePath && scene.path != Stage02ScenePath))
        {
            Debug.LogWarning(
                "[StageProgressGaugeArtApplier] Open Stage01_Prototype or Stage02 first.");
            return;
        }

        Sprite backgroundSprite = LoadUiSprite(BackgroundSpritePath);
        Sprite fillSprite = LoadUiSprite(FillSpritePath);
        Sprite frameSprite = LoadUiSprite(FrameSpritePath);
        if (backgroundSprite == null || fillSprite == null || frameSprite == null)
        {
            Debug.LogWarning(
                "[StageProgressGaugeArtApplier] One or more progress gauge sprites could not be loaded. "
                + "Confirm that all three PNG files are present and reimported as Sprite (2D and UI).");
            return;
        }

        StageMusicProgressGauge gauge = FindSceneComponent<StageMusicProgressGauge>(scene);
        Transform progressRoot = gauge != null
            ? gauge.transform
            : FindSceneTransform(scene, "StageMusicProgressRoot");
        progressRoot ??= FindSceneTransform(scene, "StageProgressRoot");
        if (progressRoot == null)
        {
            Debug.LogWarning(
                "[StageProgressGaugeArtApplier] Existing music progress gauge was not found. "
                + "This menu does not create or rebuild the complete gauge UI.");
            return;
        }

        Image background = FindDirectImage(progressRoot, "ProgressBackground");
        Image fill = FindDirectImage(progressRoot, "ProgressFill");
        if (background == null || fill == null)
        {
            Debug.LogWarning(
                "[StageProgressGaugeArtApplier] ProgressBackground or ProgressFill is missing.",
                progressRoot);
            return;
        }

        Image frame = FindDirectImage(progressRoot, "ProgressFrame");
        bool frameCreated = frame == null;
        if (frameCreated)
        {
            GameObject frameObject = new(
                "ProgressFrame",
                typeof(RectTransform),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(frameObject, "Create Progress Gauge Frame");
            frameObject.transform.SetParent(progressRoot, false);
            frame = frameObject.GetComponent<Image>();
        }

        ConfigureImage(background, backgroundSprite, Image.Type.Simple, Color.white);
        ConfigureImage(fill, fillSprite, Image.Type.Filled, DefaultFillColor);
        ConfigureImage(frame, frameSprite, Image.Type.Simple, Color.white);

        StretchToRoot(background.rectTransform);
        StretchToRoot(fill.rectTransform);
        StretchToRoot(frame.rectTransform);
        SetDisplayOrder(background, fill, frame);

        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillClockwise = true;
        fill.preserveAspect = false;

        if (gauge == null)
        {
            gauge = Undo.AddComponent<StageMusicProgressGauge>(progressRoot.gameObject);
        }

        PeckBeatRhythmManager rhythmManager = FindSceneComponent<PeckBeatRhythmManager>(scene);
        AudioSource rhythmBgmSource = GetRhythmManagerBgmSource(rhythmManager);

        Undo.RecordObject(gauge, "Connect Progress Gauge Art");
        SerializedObject serializedGauge = new(gauge);
        SetObjectReference(serializedGauge, "progressImage", fill, overwrite: true);
        SetObjectReference(serializedGauge, "rhythmManager", rhythmManager, overwrite: false);
        SetObjectReference(serializedGauge, "bgmSource", rhythmBgmSource, overwrite: false);
        serializedGauge.ApplyModifiedProperties();
        EditorUtility.SetDirty(gauge);

        WarnIfBgmReferenceDiffers(serializedGauge, rhythmBgmSource, gauge);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = progressRoot.gameObject;

        Debug.Log(
            "[StageProgressGaugeArtApplier] Progress gauge art applied. "
            + $"Scene={scene.name}, frameCreated={frameCreated}. "
            + "The existing progress root position and size were preserved. "
            + "The scene was marked dirty but was not saved.",
            gauge);
    }

    private static Sprite LoadUiSprite(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return null;
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void ConfigureImage(
        Image image,
        Sprite sprite,
        Image.Type type,
        Color color)
    {
        if (image == null)
        {
            return;
        }

        Undo.RecordObject(image, "Apply Progress Gauge Sprite");
        image.sprite = sprite;
        image.type = type;
        image.color = color;
        image.raycastTarget = false;
        EditorUtility.SetDirty(image);
    }

    private static void StretchToRoot(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        Undo.RecordObject(rect, "Align Progress Gauge Image");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        EditorUtility.SetDirty(rect);
    }

    private static void SetDisplayOrder(Image background, Image fill, Image frame)
    {
        Undo.RecordObject(background.transform, "Order Progress Gauge Images");
        Undo.RecordObject(fill.transform, "Order Progress Gauge Images");
        Undo.RecordObject(frame.transform, "Order Progress Gauge Images");
        background.transform.SetSiblingIndex(0);
        fill.transform.SetSiblingIndex(1);
        frame.transform.SetSiblingIndex(2);
    }

    private static Image FindDirectImage(Transform parent, string objectName)
    {
        Transform child = parent.Find(objectName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static AudioSource GetRhythmManagerBgmSource(PeckBeatRhythmManager rhythmManager)
    {
        if (rhythmManager == null)
        {
            return null;
        }

        SerializedObject serializedRhythm = new(rhythmManager);
        SerializedProperty bgmProperty = serializedRhythm.FindProperty("bgmSource");
        return bgmProperty?.objectReferenceValue as AudioSource;
    }

    private static void WarnIfBgmReferenceDiffers(
        SerializedObject serializedGauge,
        AudioSource rhythmBgmSource,
        Object context)
    {
        if (rhythmBgmSource == null)
        {
            Debug.LogWarning(
                "[StageProgressGaugeArtApplier] RhythmManager BGM AudioSource was not assigned. "
                + "The gauge will keep its existing reference or use runtime auto lookup.",
                context);
            return;
        }

        SerializedProperty gaugeBgmProperty = serializedGauge.FindProperty("bgmSource");
        AudioSource gaugeBgm = gaugeBgmProperty?.objectReferenceValue as AudioSource;
        if (gaugeBgm != null && gaugeBgm != rhythmBgmSource)
        {
            Debug.LogWarning(
                "[StageProgressGaugeArtApplier] The gauge has a custom BGM AudioSource different "
                + "from RhythmManager. It was preserved; verify it in the Inspector.",
                context);
        }
    }

    private static void SetObjectReference(
        SerializedObject serializedObject,
        string propertyName,
        Object value,
        bool overwrite)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || value == null)
        {
            return;
        }

        if (overwrite || property.objectReferenceValue == null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        T[] components = Object.FindObjectsByType<T>(
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
}
