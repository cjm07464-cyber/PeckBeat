using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StageGoalAndProgressUIBuilder
{
    private const string MenuPath = "PeckBeat/Shared/Build Goal And Progress UI";
    private const string Stage01ScenePath = "Assets/Scenes/Stage01_Prototype.unity";
    private const string Stage02ScenePath = "Assets/Scenes/Stage02.unity";
    private const string KoreanFontPath =
        "Assets/_PeckBeat/Fonts/NotoSansKR-Bold SDF.asset";
    private const string EnglishFontPath =
        "Assets/_PeckBeat/Fonts/Segoe UI Bold SDF.asset";

    [MenuItem(MenuPath)]
    private static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        bool isStage01 = scene.IsValid() && scene.path == Stage01ScenePath;
        bool isStage02 = scene.IsValid() && scene.path == Stage02ScenePath;
        if (!isStage01 && !isStage02)
        {
            Debug.LogWarning(
                "[StageGoalAndProgressUIBuilder] Open Stage01_Prototype or Stage02 first.");
            return;
        }

        RectTransform canvasRect = GetOrCreateCanvas(scene, out bool canvasCreated);
        if (canvasRect == null)
        {
            return;
        }

        TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        TMP_FontAsset englishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EnglishFontPath);

        RectTransform goalRoot = GetOrCreateRect(canvasRect, "StageGoalRoot", out bool goalRootCreated);
        if (goalRoot == null)
        {
            Debug.LogWarning("[StageGoalAndProgressUIBuilder] StageGoalRoot must use RectTransform.");
            return;
        }

        if (goalRootCreated)
        {
            goalRoot.anchorMin = new Vector2(0f, 1f);
            goalRoot.anchorMax = new Vector2(0f, 1f);
            goalRoot.pivot = new Vector2(0f, 1f);
            goalRoot.anchoredPosition = new Vector2(36f, -28f);
            goalRoot.sizeDelta = new Vector2(340f, isStage01 ? 92f : 132f);
            goalRoot.localScale = Vector3.one;
        }

        string[] textNames = isStage01
            ? new[] { "FeedingGoalText", "CuckooGoalText" }
            : new[] { "OneHitGoalText", "TwoHitGoalText", "HoldGoalText" };
        TMP_Text[] goalTexts = new TMP_Text[textNames.Length];
        for (int i = 0; i < textNames.Length; i++)
        {
            goalTexts[i] = GetOrCreateGoalText(
                goalRoot,
                textNames[i],
                i,
                koreanFont,
                out _);
        }

        StageGoalCounter goalCounter = FindSceneComponent<StageGoalCounter>(scene);
        if (goalCounter == null)
        {
            goalCounter = Undo.AddComponent<StageGoalCounter>(goalRoot.gameObject);
        }

        Undo.RecordObject(goalCounter, "Connect Stage Goal UI");
        SerializedObject serializedGoalCounter = new(goalCounter);
        if (isStage01)
        {
            SetObjectReferenceIfEmpty(serializedGoalCounter, "stage01FeedingText", goalTexts[0]);
            SetObjectReferenceIfEmpty(serializedGoalCounter, "stage01CuckooText", goalTexts[1]);
        }
        else
        {
            SetObjectReferenceIfEmpty(serializedGoalCounter, "stage02OneHitText", goalTexts[0]);
            SetObjectReferenceIfEmpty(serializedGoalCounter, "stage02TwoHitText", goalTexts[1]);
            SetObjectReferenceIfEmpty(serializedGoalCounter, "stage02HoldText", goalTexts[2]);
        }

        SetObjectReferenceIfEmpty(serializedGoalCounter, "koreanFont", koreanFont);
        SetObjectReferenceIfEmpty(serializedGoalCounter, "englishFont", englishFont);
        serializedGoalCounter.ApplyModifiedProperties();
        EditorUtility.SetDirty(goalCounter);

        RectTransform progressRoot = GetOrCreateRect(
            canvasRect,
            "StageMusicProgressRoot",
            out bool progressRootCreated);
        if (progressRoot == null)
        {
            Debug.LogWarning(
                "[StageGoalAndProgressUIBuilder] StageMusicProgressRoot must use RectTransform.");
            return;
        }

        if (progressRootCreated)
        {
            progressRoot.anchorMin = new Vector2(0.5f, 1f);
            progressRoot.anchorMax = new Vector2(0.5f, 1f);
            progressRoot.pivot = new Vector2(0.5f, 1f);
            progressRoot.anchoredPosition = new Vector2(0f, -34f);
            progressRoot.sizeDelta = new Vector2(560f, 24f);
            progressRoot.localScale = Vector3.one;
        }

        Image progressBackground = GetOrCreateImage(
            progressRoot,
            "ProgressBackground",
            new Color(0.05f, 0.06f, 0.08f, 0.8f),
            out bool backgroundCreated);
        Image progressFill = GetOrCreateImage(
            progressRoot,
            "ProgressFill",
            new Color(0.95f, 0.72f, 0.18f, 1f),
            out bool fillCreated);
        if (progressBackground == null || progressFill == null)
        {
            Debug.LogWarning(
                "[StageGoalAndProgressUIBuilder] Progress objects must contain Image components.");
            return;
        }

        if (backgroundCreated)
        {
            StretchToParent(progressBackground.rectTransform, Vector2.zero, Vector2.zero);
        }

        if (fillCreated)
        {
            StretchToParent(progressFill.rectTransform, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressFill.fillAmount = 0f;
        }

        StageMusicProgressGauge gauge = FindSceneComponent<StageMusicProgressGauge>(scene);
        if (gauge == null)
        {
            gauge = Undo.AddComponent<StageMusicProgressGauge>(progressRoot.gameObject);
        }

        PeckBeatRhythmManager rhythmManager = FindSceneComponent<PeckBeatRhythmManager>(scene);
        Undo.RecordObject(gauge, "Connect Stage Music Progress Gauge");
        SerializedObject serializedGauge = new(gauge);
        SetObjectReferenceIfEmpty(serializedGauge, "progressImage", progressFill);
        SetObjectReferenceIfEmpty(serializedGauge, "rhythmManager", rhythmManager);
        serializedGauge.ApplyModifiedProperties();
        EditorUtility.SetDirty(gauge);

        ConnectGameplayController(scene, goalCounter, isStage01);
        goalCounter.RefreshDisplay();
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = canvasRect.gameObject;

        if (rhythmManager == null)
        {
            Debug.LogWarning(
                "[StageGoalAndProgressUIBuilder] RhythmManager was not found. "
                + "The gauge will try runtime auto lookup.",
                gauge);
        }

        if (koreanFont == null || englishFont == null)
        {
            Debug.LogWarning(
                "[StageGoalAndProgressUIBuilder] One or more TMP fonts were not found. "
                + "Existing text fonts were preserved; assign empty font slots manually.",
                goalCounter);
        }

        Debug.Log(
            "[StageGoalAndProgressUIBuilder] Goal and progress UI created or reused. "
            + $"Scene={scene.name}, canvasCreated={canvasCreated}. "
            + "Existing object layouts and references were preserved. The scene was not saved.",
            canvasRect);
    }

    private static RectTransform GetOrCreateCanvas(Scene scene, out bool created)
    {
        created = false;
        Transform existing = FindSceneTransform(scene, "StageGoalAndProgressCanvas");
        if (existing != null)
        {
            return existing as RectTransform;
        }

        GameObject canvasObject = new(
            "StageGoalAndProgressCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Goal And Progress Canvas");
        SceneManager.MoveGameObjectToScene(canvasObject, scene);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        created = true;
        return canvasObject.GetComponent<RectTransform>();
    }

    private static RectTransform GetOrCreateRect(
        Transform parent,
        string objectName,
        out bool created)
    {
        created = false;
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing as RectTransform;
        }

        GameObject child = new(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(child, $"Create {objectName}");
        child.transform.SetParent(parent, false);
        created = true;
        return child.GetComponent<RectTransform>();
    }

    private static TMP_Text GetOrCreateGoalText(
        RectTransform parent,
        string objectName,
        int index,
        TMP_FontAsset defaultFont,
        out bool created)
    {
        created = false;
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing.GetComponent<TMP_Text>();
        }

        GameObject textObject = new(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(textObject, $"Create {objectName}");
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(0f, -index * 40f);
        rect.sizeDelta = new Vector2(340f, 38f);
        rect.localScale = Vector3.one;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = objectName;
        text.fontSize = 30f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.raycastTarget = false;
        if (defaultFont != null)
        {
            text.font = defaultFont;
        }

        created = true;
        return text;
    }

    private static Image GetOrCreateImage(
        RectTransform parent,
        string objectName,
        Color defaultColor,
        out bool created)
    {
        created = false;
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing.GetComponent<Image>();
        }

        GameObject imageObject = new(objectName, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(imageObject, $"Create {objectName}");
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = defaultColor;
        image.raycastTarget = false;
        created = true;
        return image;
    }

    private static void StretchToParent(
        RectTransform rect,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }

    private static void ConnectGameplayController(
        Scene scene,
        StageGoalCounter counter,
        bool isStage01)
    {
        MonoBehaviour controller = isStage01
            ? FindSceneComponent<Stage01FeedingController>(scene)
            : FindSceneComponent<Stage02SculptingController>(scene);
        if (controller == null)
        {
            Debug.LogWarning(
                "[StageGoalAndProgressUIBuilder] Stage gameplay controller was not found.",
                counter);
            return;
        }

        Undo.RecordObject(controller, "Connect Stage Goal Counter");
        SerializedObject serializedController = new(controller);
        SetObjectReferenceIfEmpty(serializedController, "goalCounter", counter);
        serializedController.ApplyModifiedProperties();
        EditorUtility.SetDirty(controller);
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

    private static void SetObjectReferenceIfEmpty(
        SerializedObject serializedObject,
        string propertyName,
        Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.objectReferenceValue == null && value != null)
        {
            property.objectReferenceValue = value;
        }
    }
}
