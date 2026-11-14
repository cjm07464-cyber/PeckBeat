using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StageLifeUIBuilder
{
    private const string MenuPath = "PeckBeat/Shared/Build Stage Life UI";
    private const string Stage01ScenePath = "Assets/Scenes/Stage01_Prototype.unity";
    private const string Stage02ScenePath = "Assets/Scenes/Stage02.unity";
    private const string KoreanFontPath =
        "Assets/_PeckBeat/Fonts/NotoSansKR-Bold SDF.asset";
    private const string EnglishFontPath =
        "Assets/_PeckBeat/Fonts/Segoe UI Bold SDF.asset";
    private const string HeartSpritePath =
        "Assets/_PeckBeat/Art/부리박자슬슬마무리/빨간 하트 아이콘.png";

    [MenuItem(MenuPath)]
    private static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() ||
            (scene.path != Stage01ScenePath && scene.path != Stage02ScenePath))
        {
            Debug.LogWarning(
                "[StageLifeUIBuilder] Open Stage01_Prototype or Stage02 before building the life UI.");
            return;
        }

        RectTransform canvasRect = GetOrCreateCanvas(scene, out bool canvasCreated);
        if (canvasRect == null)
        {
            return;
        }

        RectTransform lifeRoot = GetOrCreateRect(canvasRect, "StageLifeRoot", out bool rootCreated);
        if (lifeRoot == null)
        {
            Debug.LogWarning("[StageLifeUIBuilder] StageLifeRoot must use a RectTransform.");
            return;
        }

        if (rootCreated)
        {
            lifeRoot.anchorMin = Vector2.one;
            lifeRoot.anchorMax = Vector2.one;
            lifeRoot.pivot = Vector2.one;
            lifeRoot.anchoredPosition = new Vector2(-40f, -30f);
            lifeRoot.sizeDelta = new Vector2(360f, 80f);
            lifeRoot.localScale = Vector3.one;
        }

        TMP_Text lifeText = GetOrCreateLifeText(lifeRoot, out bool textCreated);
        if (lifeText == null)
        {
            Debug.LogWarning(
                "[StageLifeUIBuilder] Existing LifeText does not contain a TMP_Text component.");
            return;
        }

        TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        TMP_FontAsset englishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EnglishFontPath);
        Image heartImage = GetOrCreateHeartImage(
            lifeRoot,
            AssetDatabase.LoadAssetAtPath<Sprite>(HeartSpritePath),
            out _);
        if (textCreated && koreanFont != null)
        {
            lifeText.font = koreanFont;
        }

        StageLifeCounter counter = FindSceneComponent<StageLifeCounter>(scene);
        if (counter == null)
        {
            counter = Undo.AddComponent<StageLifeCounter>(lifeRoot.gameObject);
        }

        Undo.RecordObject(counter, "Connect Stage Life UI");
        SerializedObject serializedCounter = new(counter);
        SetObjectReferenceIfEmpty(serializedCounter, "livesText", lifeText);
        SetObjectReferenceIfEmpty(serializedCounter, "lifeImage", heartImage);
        SetObjectReferenceIfEmpty(serializedCounter, "koreanFont", koreanFont);
        SetObjectReferenceIfEmpty(serializedCounter, "englishFont", englishFont);
        serializedCounter.ApplyModifiedProperties();

        ConnectController(scene, counter);
        counter.RefreshDisplay();
        EditorUtility.SetDirty(counter);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = lifeRoot.gameObject;

        if (koreanFont == null || englishFont == null)
        {
            Debug.LogWarning(
                "[StageLifeUIBuilder] One or more TMP fonts could not be found. Assign the empty font slots manually.",
                counter);
        }

        Debug.Log(
            "[StageLifeUIBuilder] Stage life UI was created or reused. "
            + $"Canvas created={canvasCreated}. Existing UI layouts were preserved. "
            + "The scene was marked dirty but was not saved.",
            counter);
    }

    private static RectTransform GetOrCreateCanvas(Scene scene, out bool created)
    {
        created = false;
        Transform existing = FindSceneTransform(scene, "StageLifeCanvas");
        if (existing != null)
        {
            return existing as RectTransform;
        }

        GameObject canvasObject = new(
            "StageLifeCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Stage Life Canvas");
        SceneManager.MoveGameObjectToScene(canvasObject, scene);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

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

    private static TMP_Text GetOrCreateLifeText(RectTransform parent, out bool created)
    {
        created = false;
        Transform existing = parent.Find("LifeText");
        if (existing != null)
        {
            TMP_Text existingText = existing.GetComponent<TMP_Text>();
            return existingText;
        }

        GameObject textObject = new(
            "LifeText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(textObject, "Create Life Text");
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = "목숨 10 / 10";
        text.fontSize = 36f;
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.color = Color.white;
        text.raycastTarget = false;
        created = true;
        return text;
    }

    private static Image GetOrCreateHeartImage(
        RectTransform parent,
        Sprite defaultSprite,
        out bool created)
    {
        created = false;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.Trim().StartsWith("HeartImage"))
            {
                return child.GetComponent<Image>();
            }
        }

        if (defaultSprite == null)
        {
            Debug.LogWarning(
                "[StageLifeUIBuilder] Stage01 heart sprite was not found. "
                + "The text-only life UI remains available.");
            return null;
        }

        GameObject imageObject = new("HeartImage", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(imageObject, "Create Stage Life Heart Image");
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-59f, -13f);
        rect.sizeDelta = new Vector2(120f, 110f);
        rect.localScale = Vector3.one;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = defaultSprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        created = true;
        return image;
    }

    private static void ConnectController(Scene scene, StageLifeCounter counter)
    {
        MonoBehaviour controller = scene.path == Stage01ScenePath
            ? FindSceneComponent<Stage01FeedingController>(scene)
            : FindSceneComponent<Stage02SculptingController>(scene);
        if (controller == null)
        {
            Debug.LogWarning("[StageLifeUIBuilder] Stage gameplay controller was not found.", counter);
            return;
        }

        Undo.RecordObject(controller, "Connect Stage Life Counter");
        SerializedObject serializedController = new(controller);
        SetObjectReferenceIfEmpty(serializedController, "lifeCounter", counter);
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
