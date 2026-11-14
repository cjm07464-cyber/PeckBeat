using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage02InstructionUIBuilder
{
    private const string MenuPath = "PeckBeat/Stage02/Build Top Instruction UI";
    private const string Stage02ScenePath = "Assets/Scenes/Stage02.unity";

    private const string KeyboardDPath =
        "Assets/_PeckBeat/Art/Title/Test/D키캡_외곽선65px.png";
    private const string KeyboardJPath =
        "Assets/_PeckBeat/Art/Title/Test/J키캡_외곽선65px.png";
    private const string XboxAPath =
        "Assets/_PeckBeat/Art/Title/Test/XBOX_A키캡_외곽선65px.png";
    private const string XboxBPath =
        "Assets/_PeckBeat/Art/Title/Test/XBOX_B키캡_외곽선65px.png";
    private const string OneHitLogFallbackPath =
        "Assets/_PeckBeat/Art/Stage_02/Player/Objects/일반통나무.png";
    private const string TwoHitLogFallbackPath =
        "Assets/_PeckBeat/Art/Stage_02/Player/Objects/2타통나무.png";
    private const string HoldLogFallbackPath =
        "Assets/_PeckBeat/Art/Stage_02/Player/Objects/홀드_긴통나무_완전옆면.png";
    private const string KoreanFontPath =
        "Assets/_PeckBeat/Fonts/NotoSansKR-Bold SDF.asset";
    private const string EnglishFontPath =
        "Assets/_PeckBeat/Fonts/Segoe UI Bold SDF.asset";

    [MenuItem(MenuPath)]
    private static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage02ScenePath)
        {
            Debug.LogWarning($"[Stage02InstructionUIBuilder] Open {Stage02ScenePath} first.");
            return;
        }

        RectTransform canvasRect = GetOrCreateCanvas(scene, out bool canvasCreated);
        if (canvasRect == null)
        {
            return;
        }

        RectTransform instructionRoot = GetOrCreateRect(
            canvasRect,
            "Stage02InstructionRoot",
            out bool instructionRootCreated);
        if (instructionRoot == null)
        {
            Debug.LogWarning(
                "[Stage02InstructionUIBuilder] Stage02InstructionRoot has an incompatible Transform.");
            return;
        }

        if (instructionRootCreated)
        {
            SetTopStretch(instructionRoot, 190f, 24f);
        }

        GuideParts left = GetOrCreateGuide(
            instructionRoot,
            "LeftGuide",
            new Vector2(-540f, 0f),
            "1번!");
        GuideParts middle = GetOrCreateGuide(
            instructionRoot,
            "MiddleGuide",
            Vector2.zero,
            "2번!");
        GuideParts right = GetOrCreateGuide(
            instructionRoot,
            "RightGuide",
            new Vector2(540f, 0f),
            "꾹~ 눌러!");

        if (!left.IsValid || !middle.IsValid || !right.IsValid)
        {
            Debug.LogWarning(
                "[Stage02InstructionUIBuilder] An existing guide object has an incompatible Transform.");
            return;
        }

        Stage02InstructionUI instructionUI = instructionRoot.GetComponent<Stage02InstructionUI>();
        if (instructionUI == null)
        {
            instructionUI = Undo.AddComponent<Stage02InstructionUI>(instructionRoot.gameObject);
        }

        Stage02SculptingController controller = FindSceneComponent<Stage02SculptingController>(scene);
        Sprite oneHitLog = FindNormalLogSprite(controller, "oneHitBaseLogSetting", "oneHitBaseLog")
            ?? AssetDatabase.LoadAssetAtPath<Sprite>(OneHitLogFallbackPath);
        Sprite twoHitLog = FindNormalLogSprite(controller, "twoHitBaseLogSetting", "twoHitBaseLog")
            ?? AssetDatabase.LoadAssetAtPath<Sprite>(TwoHitLogFallbackPath);
        Sprite holdLog = FindHoldLogSprite(controller)
            ?? AssetDatabase.LoadAssetAtPath<Sprite>(HoldLogFallbackPath);

        Sprite keyboardD = AssetDatabase.LoadAssetAtPath<Sprite>(KeyboardDPath);
        Sprite keyboardJ = AssetDatabase.LoadAssetAtPath<Sprite>(KeyboardJPath);
        Sprite xboxA = AssetDatabase.LoadAssetAtPath<Sprite>(XboxAPath);
        Sprite xboxB = AssetDatabase.LoadAssetAtPath<Sprite>(XboxBPath);
        TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        TMP_FontAsset englishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EnglishFontPath);

        Undo.RecordObject(instructionUI, "Connect Stage02 Instruction UI");
        SerializedObject serializedUI = new(instructionUI);
        SetObjectReferenceIfEmpty(serializedUI, "leftGuideText", left.Text);
        SetObjectReferenceIfEmpty(serializedUI, "middleGuideText", middle.Text);
        SetObjectReferenceIfEmpty(serializedUI, "rightGuideText", right.Text);
        SetObjectReferenceIfEmpty(serializedUI, "leftButtonImage", left.Button);
        SetObjectReferenceIfEmpty(serializedUI, "middleButtonImage", middle.Button);
        SetObjectReferenceIfEmpty(serializedUI, "rightButtonImage", right.Button);
        SetObjectReferenceIfEmpty(serializedUI, "leftLogImage", left.Log);
        SetObjectReferenceIfEmpty(serializedUI, "middleLogImage", middle.Log);
        SetObjectReferenceIfEmpty(serializedUI, "rightLogImage", right.Log);
        SetObjectReferenceIfEmpty(serializedUI, "keyboardDButtonSprite", keyboardD);
        SetObjectReferenceIfEmpty(serializedUI, "keyboardJButtonSprite", keyboardJ);
        SetObjectReferenceIfEmpty(serializedUI, "gamepadAButtonSprite", xboxA);
        SetObjectReferenceIfEmpty(serializedUI, "gamepadBButtonSprite", xboxB);
        SetObjectReferenceIfEmpty(serializedUI, "oneHitLogSprite", oneHitLog);
        SetObjectReferenceIfEmpty(serializedUI, "twoHitLogSprite", twoHitLog);
        SetObjectReferenceIfEmpty(serializedUI, "holdLongLogSprite", holdLog);
        SetObjectReferenceIfEmpty(serializedUI, "koreanTmpFontAsset", koreanFont);
        SetObjectReferenceIfEmpty(serializedUI, "englishTmpFontAsset", englishFont);
        serializedUI.ApplyModifiedProperties();

        AssignSpriteIfMissing(left.Button, keyboardD);
        AssignSpriteIfMissing(middle.Button, keyboardD);
        AssignSpriteIfMissing(right.Button, keyboardJ);
        AssignSpriteIfMissing(left.Log, oneHitLog);
        AssignSpriteIfMissing(middle.Log, twoHitLog);
        AssignSpriteIfMissing(right.Log, holdLog);
        AssignFontIfMissing(left.Text, koreanFont);
        AssignFontIfMissing(middle.Text, koreanFont);
        AssignFontIfMissing(right.Text, koreanFont);

        EditorUtility.SetDirty(instructionUI);
        EditorSceneManager.MarkSceneDirty(scene);
        instructionUI.RefreshGuide();
        Selection.activeGameObject = instructionRoot.gameObject;

        ReportMissingAssets(
            keyboardD,
            keyboardJ,
            xboxA,
            xboxB,
            oneHitLog,
            twoHitLog,
            holdLog,
            koreanFont,
            englishFont);

        Debug.Log(
            "[Stage02InstructionUIBuilder] Stage02 top instruction UI was created or reused. "
            + $"Canvas created={canvasCreated}. Existing references and layouts were preserved. "
            + "The scene was marked dirty but was not saved.",
            instructionRoot);
    }

    private static RectTransform GetOrCreateCanvas(Scene scene, out bool created)
    {
        created = false;
        Transform existing = FindSceneTransform(scene, "Stage02UICanvas");
        GameObject canvasObject;
        if (existing != null)
        {
            RectTransform existingRect = existing as RectTransform;
            if (existingRect == null)
            {
                Debug.LogWarning(
                    "[Stage02InstructionUIBuilder] Stage02UICanvas is not a RectTransform.");
                return null;
            }

            canvasObject = existing.gameObject;
        }
        else
        {
            canvasObject = new GameObject(
                "Stage02UICanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Stage02 UI Canvas");
            created = true;
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = Undo.AddComponent<Canvas>(canvasObject);
        }

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = Undo.AddComponent<CanvasScaler>(canvasObject);
        }

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            Undo.AddComponent<GraphicRaycaster>(canvasObject);
        }

        if (created)
        {
            Undo.RecordObject(canvas, "Configure Stage02 UI Canvas");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            Undo.RecordObject(scaler, "Configure Stage02 UI Canvas Scaler");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            SetStretch(rect);
        }

        return canvasObject.GetComponent<RectTransform>();
    }

    private static GuideParts GetOrCreateGuide(
        RectTransform parent,
        string guideName,
        Vector2 position,
        string defaultText)
    {
        RectTransform guide = GetOrCreateRect(parent, guideName, out bool guideCreated);
        if (guide == null)
        {
            return default;
        }

        if (guideCreated)
        {
            SetRect(guide, position, new Vector2(500f, 160f));
        }

        RectTransform backgroundRect = GetOrCreateRect(guide, "BackgroundImage", out bool backgroundCreated);
        RectTransform textRect = GetOrCreateRect(guide, "GuideText", out bool textCreated);
        RectTransform buttonRect = GetOrCreateRect(guide, "ButtonImage", out bool buttonCreated);
        RectTransform logRect = GetOrCreateRect(guide, "LogImage", out bool logCreated);
        if (backgroundRect == null || textRect == null || buttonRect == null || logRect == null)
        {
            return default;
        }

        if (backgroundCreated)
        {
            SetStretch(backgroundRect);
        }

        if (textCreated)
        {
            SetRect(textRect, new Vector2(0f, 52f), new Vector2(440f, 48f));
        }

        if (buttonCreated)
        {
            SetRect(buttonRect, new Vector2(-105f, -25f), new Vector2(76f, 76f));
        }

        if (logCreated)
        {
            SetRect(logRect, new Vector2(95f, -25f), new Vector2(180f, 86f));
        }

        Image background = EnsureImage(backgroundRect.gameObject);
        Image button = EnsureImage(buttonRect.gameObject);
        Image log = EnsureImage(logRect.gameObject);
        TextMeshProUGUI text = EnsureText(textRect.gameObject, defaultText);

        if (backgroundCreated)
        {
            background.color = new Color(0.075f, 0.13f, 0.2f, 0.86f);
            background.preserveAspect = false;
        }

        return new GuideParts(background, text, button, log);
    }

    private static Sprite FindNormalLogSprite(
        Stage02SculptingController controller,
        string settingPropertyName,
        string legacyPropertyName)
    {
        Stage02SculptTarget prefab = GetObjectReference<Stage02SculptTarget>(
            controller,
            "normalTargetPrefab");
        Stage02SculptObjectVisual visual = prefab != null
            ? prefab.GetComponentInChildren<Stage02SculptObjectVisual>(true)
            : null;
        if (visual == null)
        {
            return null;
        }

        SerializedObject serializedVisual = new(visual);
        SerializedProperty setting = serializedVisual.FindProperty(settingPropertyName);
        Sprite settingSprite = setting?.FindPropertyRelative("sprite")?.objectReferenceValue as Sprite;
        if (settingSprite != null)
        {
            return settingSprite;
        }

        return serializedVisual.FindProperty(legacyPropertyName)?.objectReferenceValue as Sprite;
    }

    private static Sprite FindHoldLogSprite(Stage02SculptingController controller)
    {
        Stage02SculptTarget prefab = GetObjectReference<Stage02SculptTarget>(
            controller,
            "holdTargetPrefab");
        Stage02HoldTargetVisual visual = prefab != null
            ? prefab.GetComponentInChildren<Stage02HoldTargetVisual>(true)
            : null;
        return visual != null
            ? new SerializedObject(visual).FindProperty("longLogSprite")?.objectReferenceValue as Sprite
            : null;
    }

    private static T GetObjectReference<T>(Object target, string propertyName)
        where T : Object
    {
        if (target == null)
        {
            return null;
        }

        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        return property != null ? property.objectReferenceValue as T : null;
    }

    private static void ReportMissingAssets(params Object[] assets)
    {
        string[] labels =
        {
            "Keyboard D", "Keyboard J", "Xbox A", "Xbox B",
            "One-hit log", "Two-hit log", "Hold long log", "Korean font", "English font"
        };
        List<string> missing = new();
        for (int i = 0; i < assets.Length && i < labels.Length; i++)
        {
            if (assets[i] == null)
            {
                missing.Add(labels[i]);
            }
        }

        if (missing.Count > 0)
        {
            Debug.LogWarning(
                "[Stage02InstructionUIBuilder] Automatic assignment could not find: "
                + string.Join(", ", missing)
                + ". Leave the slots empty or assign them in the Inspector.");
        }
    }

    private static RectTransform GetOrCreateRect(
        Transform parent,
        string objectName,
        out bool created)
    {
        created = false;
        Transform existing = parent != null ? parent.Find(objectName) : null;
        if (existing != null)
        {
            return existing as RectTransform;
        }

        if (parent == null)
        {
            return null;
        }

        GameObject child = new(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(child, $"Create {objectName}");
        RectTransform rect = child.GetComponent<RectTransform>();
        Undo.SetTransformParent(rect, parent, $"Parent {objectName}");
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        created = true;
        return rect;
    }

    private static Image EnsureImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = Undo.AddComponent<Image>(target);
            image.raycastTarget = false;
            image.preserveAspect = true;
        }

        return image;
    }

    private static TextMeshProUGUI EnsureText(GameObject target, string defaultText)
    {
        TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = Undo.AddComponent<TextMeshProUGUI>(target);
            text.text = defaultText;
            text.fontSize = 34f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
        }

        return text;
    }

    private static void AssignSpriteIfMissing(Image image, Sprite sprite)
    {
        if (image != null && image.sprite == null && sprite != null)
        {
            Undo.RecordObject(image, "Assign Stage02 Instruction Sprite");
            image.sprite = sprite;
            image.preserveAspect = true;
        }
    }

    private static void AssignFontIfMissing(TMP_Text text, TMP_FontAsset font)
    {
        if (text != null && text.font == null && font != null)
        {
            Undo.RecordObject(text, "Assign Stage02 Instruction Font");
            text.font = font;
        }
    }

    private static void SetObjectReferenceIfEmpty(
        SerializedObject target,
        string propertyName,
        Object value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property != null && property.objectReferenceValue == null && value != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (component != null && component.gameObject.scene == scene
                && !EditorUtility.IsPersistent(component))
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

    private static void SetStretch(RectTransform rect)
    {
        Undo.RecordObject(rect, $"Layout {rect.name}");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void SetTopStretch(RectTransform rect, float height, float topMargin)
    {
        Undo.RecordObject(rect, $"Layout {rect.name}");
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -topMargin);
        rect.sizeDelta = new Vector2(0f, height);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        Undo.RecordObject(rect, $"Layout {rect.name}");
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private readonly struct GuideParts
    {
        public GuideParts(Image background, TMP_Text text, Image button, Image log)
        {
            Background = background;
            Text = text;
            Button = button;
            Log = log;
        }

        public Image Background { get; }
        public TMP_Text Text { get; }
        public Image Button { get; }
        public Image Log { get; }
        public bool IsValid => Background != null && Text != null && Button != null && Log != null;
    }
}
