using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage02HoldCueBubbleBuilder
{
    private const string MenuPath = "PeckBeat/Stage02/Build Hold Cue Bubble UI";
    private const string Stage02ScenePath = "Assets/Scenes/Stage02.unity";
    private const string BubbleSpritePath = "Assets/_PeckBeat/Art/Stage_02/예고용말풍선.png";
    private const string KeyboardIconPath =
        "Assets/_PeckBeat/Art/Title/Test/J키캡_외곽선65px.png";
    private const string XboxIconPath =
        "Assets/_PeckBeat/Art/Title/Test/XBOX_B키캡_외곽선65px.png";
    private const string KoreanFontPath = "Assets/_PeckBeat/Fonts/NotoSansKR-Bold SDF.asset";
    private const string EnglishFontPath = "Assets/_PeckBeat/Fonts/Segoe UI Bold SDF.asset";

    [MenuItem(MenuPath)]
    private static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage02ScenePath)
        {
            Debug.LogWarning($"[Stage02HoldCueBubbleBuilder] Open {Stage02ScenePath} first.");
            return;
        }

        Transform parent = FindSceneTransform(scene, "GameplayRoot")
            ?? FindSceneTransform(scene, "StageRoot");
        Stage02SculptingController controller =
            Object.FindFirstObjectByType<Stage02SculptingController>(FindObjectsInactive.Include);
        Stage02ForemanRatVisual foreman =
            Object.FindFirstObjectByType<Stage02ForemanRatVisual>(FindObjectsInactive.Include);
        if (parent == null || controller == null)
        {
            Debug.LogWarning(
                "[Stage02HoldCueBubbleBuilder] GameplayRoot/StageRoot or controller was not found.");
            return;
        }

        Transform existingRoot = FindSceneTransform(scene, "HoldCueBubbleRoot");
        bool rootCreated = existingRoot == null;
        GameObject rootObject;
        if (rootCreated)
        {
            rootObject = new GameObject(
                "HoldCueBubbleRoot",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(Stage02HoldCueBubbleUI));
            Undo.RegisterCreatedObjectUndo(rootObject, "Create Hold Cue Bubble UI");
            Undo.SetTransformParent(rootObject.transform, parent, "Parent Hold Cue Bubble UI");

            RectTransform rootRect = (RectTransform)rootObject.transform;
            rootRect.localPosition = Vector3.zero;
            rootRect.localRotation = Quaternion.identity;
            rootRect.localScale = Vector3.one * 0.01f;
            rootRect.sizeDelta = new Vector2(420f, 260f);

            Canvas canvas = rootObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
        }
        else
        {
            rootObject = existingRoot.gameObject;
        }

        Image bubbleImage = GetOrCreateImage(
            rootObject.transform,
            "BubbleImage",
            new Vector2(400f, 240f),
            Vector2.zero);
        Image buttonIconImage = GetOrCreateImage(
            rootObject.transform,
            "ButtonIconImage",
            new Vector2(76f, 76f),
            new Vector2(0f, 35f));
        TextMeshProUGUI holdText = GetOrCreateText(
            rootObject.transform,
            "HoldText",
            new Vector2(300f, 75f),
            new Vector2(0f, -45f));

        Sprite bubbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BubbleSpritePath);
        Sprite keyboardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(KeyboardIconPath);
        Sprite xboxSprite = AssetDatabase.LoadAssetAtPath<Sprite>(XboxIconPath);
        TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        TMP_FontAsset englishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EnglishFontPath);

        AssignSpriteIfMissing(bubbleImage, bubbleSprite);

        Stage02HoldCueBubbleUI bubbleUI = rootObject.GetComponent<Stage02HoldCueBubbleUI>();
        SerializedObject serializedUI = new SerializedObject(bubbleUI);
        SetObjectReference(serializedUI, "bubbleSprite", bubbleSprite);
        SetObjectReference(serializedUI, "keyboardJIconSprite", keyboardSprite);
        SetObjectReference(serializedUI, "xboxBIconSprite", xboxSprite);
        SetObjectReference(serializedUI, "koreanFont", koreanFont);
        SetObjectReference(serializedUI, "englishFont", englishFont);
        SetObjectReference(serializedUI, "bubbleImage", bubbleImage);
        SetObjectReference(serializedUI, "buttonIconImage", buttonIconImage);
        SetObjectReference(serializedUI, "holdText", holdText);
        SetObjectReference(serializedUI, "canvasGroup", rootObject.GetComponent<CanvasGroup>());
        SetObjectReference(serializedUI, "followTarget", foreman != null ? foreman.transform : null);
        serializedUI.ApplyModifiedProperties();

        SerializedObject serializedController = new SerializedObject(controller);
        SetObjectReference(serializedController, "holdCueBubble", bubbleUI);
        serializedController.ApplyModifiedProperties();

        EditorUtility.SetDirty(bubbleUI);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = rootObject;

        if (rootCreated)
        {
            rootObject.SetActive(false);
        }

        Debug.Log(
            "[Stage02HoldCueBubbleBuilder] Hold cue UI was created or reused. "
            + "No existing ForemanRat or gameplay Transform was modified. "
            + "The scene was marked dirty but was not saved.",
            rootObject);
    }

    private static Image GetOrCreateImage(
        Transform parent,
        string objectName,
        Vector2 size,
        Vector2 position)
    {
        Transform child = parent.Find(objectName);
        bool created = child == null;
        if (created)
        {
            GameObject childObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {objectName}");
            Undo.SetTransformParent(childObject.transform, parent, $"Parent {objectName}");
            child = childObject.transform;

            RectTransform rect = (RectTransform)child;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        Image image = child.GetComponent<Image>();
        if (image == null)
        {
            image = Undo.AddComponent<Image>(child.gameObject);
        }

        if (created)
        {
            image.raycastTarget = false;
            image.preserveAspect = true;
        }

        return image;
    }

    private static TextMeshProUGUI GetOrCreateText(
        Transform parent,
        string objectName,
        Vector2 size,
        Vector2 position)
    {
        Transform child = parent.Find(objectName);
        bool created = child == null;
        if (created)
        {
            GameObject childObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {objectName}");
            Undo.SetTransformParent(childObject.transform, parent, $"Parent {objectName}");
            child = childObject.transform;

            RectTransform rect = (RectTransform)child;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = Undo.AddComponent<TextMeshProUGUI>(child.gameObject);
        }

        if (created)
        {
            text.text = "꾹 눌러!";
            text.fontSize = 48f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            text.raycastTarget = false;
        }

        return text;
    }

    private static void AssignSpriteIfMissing(Image image, Sprite sprite)
    {
        if (image != null && image.sprite == null && sprite != null)
        {
            Undo.RecordObject(image, "Assign Hold Cue Bubble Sprite");
            image.sprite = sprite;
        }
    }

    private static void SetObjectReference(
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

    private static Transform FindSceneTransform(Scene scene, string objectName)
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
}
