using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage01TitleGuideHierarchyBuilder
{
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
    private const string KeyboardDPath = "Assets/_PeckBeat/Art/Title/D키캡_0.png";
    private const string KeyboardJPath = "Assets/_PeckBeat/Art/Title/J키_0.png";
    private const string GamepadAPath = "Assets/_PeckBeat/Art/Title/XBOX_A키캡_0.png";
    private const string GamepadBPath = "Assets/_PeckBeat/Art/Title/XBOX_B키캡_0.png";
    private const string ChickIconPath =
        "Assets/_PeckBeat/Art/Stage_01/Player/Test/Test2/아기새 아이콘.png";
    private const string CuckooIconPath =
        "Assets/_PeckBeat/Art/Stage_01/Player/Test/Test2/뻐꾸기아이콘.png";

    [MenuItem("PeckBeat/Title/Build Stage01 Title Guide UI")]
    private static void BuildStage01TitleGuideUI()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != TitleScenePath)
        {
            Debug.LogWarning($"[Stage01TitleGuideBuilder] Open {TitleScenePath} before building the guide UI.");
            return;
        }

        Transform stage01Title = FindStage01Title(scene);
        if (stage01Title == null)
        {
            Debug.LogWarning("[Stage01TitleGuideBuilder] Canvas/Stage01_Title was not found.");
            return;
        }

        RectTransform guideRoot = GetOrCreateRect(stage01Title, "Stage01GuideRoot", out bool rootCreated);
        RectTransform guideTitle = GetOrCreateRect(guideRoot, "GuideTitleText", out bool guideTitleCreated);
        RectTransform leftGuide = GetOrCreateRect(guideRoot, "LeftGuide", out bool leftCreated);
        RectTransform leftTarget = GetOrCreateRect(leftGuide, "LeftTargetImage", out bool leftTargetCreated);
        RectTransform leftHeading = GetOrCreateRect(leftGuide, "LeftHeadingText", out bool leftHeadingCreated);
        RectTransform leftButton = GetOrCreateRect(leftGuide, "LeftButtonImage", out bool leftButtonCreated);
        RectTransform leftText = GetOrCreateRect(leftGuide, "LeftGuideText", out bool leftTextCreated);
        RectTransform rightGuide = GetOrCreateRect(guideRoot, "RightGuide", out bool rightCreated);
        RectTransform rightTarget = GetOrCreateRect(rightGuide, "RightTargetImage", out bool rightTargetCreated);
        RectTransform rightHeading = GetOrCreateRect(rightGuide, "RightHeadingText", out bool rightHeadingCreated);
        RectTransform rightButton = GetOrCreateRect(rightGuide, "RightButtonImage", out bool rightButtonCreated);
        RectTransform rightText = GetOrCreateRect(rightGuide, "RightGuideText", out bool rightTextCreated);
        RectTransform startPrompt = GetOrCreateRect(
            guideRoot,
            "StartPromptText",
            out bool startPromptCreated);

        if (rootCreated)
        {
            SetRect(guideRoot, new Vector2(960f, 360f), new Vector2(1500f, 580f));
        }

        if (leftCreated)
        {
            SetRect(leftGuide, new Vector2(-350f, -10f), new Vector2(650f, 360f));
        }

        if (rightCreated)
        {
            SetRect(rightGuide, new Vector2(350f, -10f), new Vector2(650f, 360f));
        }

        Image guidePanelImage = EnsurePanelImage(guideRoot.gameObject);
        TMP_Text guideTitleText = EnsureText(guideTitle.gameObject, "게임 방법", 42f);
        Image leftTargetImage = EnsureImage(leftTarget.gameObject);
        TMP_Text leftHeadingText = EnsureText(leftHeading.gameObject, "아기새 먹이주기", 34f);
        Image leftButtonImage = EnsureImage(leftButton.gameObject);
        Image rightButtonImage = EnsureImage(rightButton.gameObject);
        TMP_Text leftGuideText = EnsureText(
            leftText.gameObject,
            "왼쪽 버튼을 한 번 눌러 먹이를 주세요!",
            30f);
        Image rightTargetImage = EnsureImage(rightTarget.gameObject);
        TMP_Text rightHeadingText = EnsureText(rightHeading.gameObject, "뻐꾸기 쫓아내기", 34f);
        TMP_Text rightGuideText = EnsureText(
            rightText.gameObject,
            "오른쪽 버튼을 두 번 눌러 쫓아내세요!",
            30f);
        TMP_Text startPromptText = EnsureText(
            startPrompt.gameObject,
            "왼쪽 또는 오른쪽 버튼을 눌러 시작하세요!",
            30f);

        if (guideTitleCreated)
        {
            SetRect(guideTitle, new Vector2(0f, 245f), new Vector2(700f, 60f));
        }

        if (leftTargetCreated)
        {
            SetRect(leftTarget, new Vector2(-205f, 5f), new Vector2(190f, 190f));
        }

        if (rightTargetCreated)
        {
            SetRect(rightTarget, new Vector2(-205f, 5f), new Vector2(190f, 190f));
        }

        if (leftHeadingCreated)
        {
            SetRect(leftHeading, new Vector2(95f, 115f), new Vector2(400f, 55f));
        }

        if (rightHeadingCreated)
        {
            SetRect(rightHeading, new Vector2(95f, 115f), new Vector2(400f, 55f));
        }

        if (leftButtonCreated)
        {
            SetRect(leftButton, new Vector2(-205f, -125f), new Vector2(92f, 92f));
        }

        if (rightButtonCreated)
        {
            SetRect(rightButton, new Vector2(-205f, -125f), new Vector2(92f, 92f));
        }

        if (leftTextCreated)
        {
            SetRect(leftText, new Vector2(95f, -15f), new Vector2(400f, 170f));
        }

        if (rightTextCreated)
        {
            SetRect(rightText, new Vector2(95f, -15f), new Vector2(400f, 170f));
        }

        if (startPromptCreated)
        {
            SetRect(startPrompt, new Vector2(0f, -245f), new Vector2(1100f, 55f));
        }

        Stage01TitleGuideUI guide = stage01Title.GetComponent<Stage01TitleGuideUI>();
        if (guide == null)
        {
            guide = Undo.AddComponent<Stage01TitleGuideUI>(stage01Title.gameObject);
        }

        Undo.RecordObject(guide, "Connect Stage01 Title Guide UI");
        SerializedObject serializedGuide = new SerializedObject(guide);
        SetObjectReferenceIfEmpty(serializedGuide, "guideRoot", guideRoot.gameObject);
        SetObjectReferenceIfEmpty(serializedGuide, "guidePanelImage", guidePanelImage);
        SetObjectReferenceIfEmpty(serializedGuide, "guideTitleText", guideTitleText);
        SetObjectReferenceIfEmpty(serializedGuide, "leftTargetImage", leftTargetImage);
        SetObjectReferenceIfEmpty(serializedGuide, "leftHeadingText", leftHeadingText);
        SetObjectReferenceIfEmpty(serializedGuide, "leftButtonImage", leftButtonImage);
        SetObjectReferenceIfEmpty(serializedGuide, "leftGuideText", leftGuideText);
        SetObjectReferenceIfEmpty(serializedGuide, "rightTargetImage", rightTargetImage);
        SetObjectReferenceIfEmpty(serializedGuide, "rightHeadingText", rightHeadingText);
        SetObjectReferenceIfEmpty(serializedGuide, "rightButtonImage", rightButtonImage);
        SetObjectReferenceIfEmpty(serializedGuide, "rightGuideText", rightGuideText);
        SetObjectReferenceIfEmpty(serializedGuide, "startPromptText", startPromptText);
        SetObjectReferenceIfEmpty(
            serializedGuide,
            "keyboardDButtonSprite",
            AssetDatabase.LoadAssetAtPath<Sprite>(KeyboardDPath));
        SetObjectReferenceIfEmpty(
            serializedGuide,
            "keyboardJButtonSprite",
            AssetDatabase.LoadAssetAtPath<Sprite>(KeyboardJPath));
        SetObjectReferenceIfEmpty(
            serializedGuide,
            "gamepadAButtonSprite",
            AssetDatabase.LoadAssetAtPath<Sprite>(GamepadAPath));
        SetObjectReferenceIfEmpty(
            serializedGuide,
            "gamepadBButtonSprite",
            AssetDatabase.LoadAssetAtPath<Sprite>(GamepadBPath));
        SetObjectReferenceIfEmpty(
            serializedGuide,
            "chickGuideSprite",
            AssetDatabase.LoadAssetAtPath<Sprite>(ChickIconPath));
        SetObjectReferenceIfEmpty(
            serializedGuide,
            "cuckooGuideSprite",
            AssetDatabase.LoadAssetAtPath<Sprite>(CuckooIconPath));
        CopyTitleManagerFonts(scene, serializedGuide);
        serializedGuide.ApplyModifiedProperties();

        TitleManager titleManager = FindSceneComponent<TitleManager>(scene);
        if (titleManager != null)
        {
            Undo.RecordObject(titleManager, "Connect Stage01 Title Guide To Manager");
            SerializedObject serializedManager = new SerializedObject(titleManager);
            SetObjectReferenceIfEmpty(serializedManager, "stage01TitleGuideUI", guide);
            serializedManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(titleManager);
        }

        EditorUtility.SetDirty(guide);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = guideRoot.gameObject;

        Debug.Log(
            "[Stage01TitleGuideBuilder] Stage01 guide hierarchy is ready. " +
            "Missing tutorial objects were added without replacing existing layout or assignments. " +
            "The scene was not saved.",
            guideRoot);
    }

    private static Transform FindStage01Title(Scene scene)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform candidate in transforms)
        {
            if (candidate == null || candidate.name != "Canvas" ||
                candidate.gameObject.scene != scene || EditorUtility.IsPersistent(candidate))
            {
                continue;
            }

            Transform stage01Title = FindChildRecursive(candidate, "Stage01_Title");
            if (stage01Title != null)
            {
                return stage01Title;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == childName)
            {
                return child;
            }

            Transform nestedMatch = FindChildRecursive(child, childName);
            if (nestedMatch != null)
            {
                return nestedMatch;
            }
        }

        return null;
    }

    private static RectTransform GetOrCreateRect(Transform parent, string name, out bool created)
    {
        Transform existing = parent.Find(name);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            created = false;
            return existingRect;
        }

        GameObject child = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        created = true;
        return rect;
    }

    private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Undo.RecordObject(rect, $"Layout {rect.name}");
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static Image EnsureImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = Undo.AddComponent<Image>(target);
            Undo.RecordObject(image, $"Configure {target.name}");
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = image.sprite != null;
        }

        return image;
    }

    private static Image EnsurePanelImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = Undo.AddComponent<Image>(target);
            Undo.RecordObject(image, "Configure Stage01 Guide Panel");
            image.color = new Color(0.04f, 0.06f, 0.1f, 0.88f);
            image.raycastTarget = false;
        }

        return image;
    }

    private static TMP_Text EnsureText(GameObject target, string defaultText, float fontSize)
    {
        TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = Undo.AddComponent<TextMeshProUGUI>(target);
            Undo.RecordObject(text, $"Configure {target.name}");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.text = defaultText;
            text.raycastTarget = false;
        }

        return text;
    }

    private static void CopyTitleManagerFonts(Scene scene, SerializedObject serializedGuide)
    {
        TitleManager titleManager = FindSceneComponent<TitleManager>(scene);
        if (titleManager == null)
        {
            return;
        }

        SerializedObject serializedManager = new SerializedObject(titleManager);
        CopyObjectReferenceIfEmpty(serializedManager, "koreanTmpFontAsset", serializedGuide, "koreanTmpFontAsset");
        CopyObjectReferenceIfEmpty(serializedManager, "englishTmpFontAsset", serializedGuide, "englishTmpFontAsset");
    }

    private static void CopyObjectReferenceIfEmpty(
        SerializedObject source,
        string sourceName,
        SerializedObject destination,
        string destinationName)
    {
        SerializedProperty sourceProperty = source.FindProperty(sourceName);
        SerializedProperty destinationProperty = destination.FindProperty(destinationName);
        if (sourceProperty != null && destinationProperty != null &&
            destinationProperty.objectReferenceValue == null)
        {
            destinationProperty.objectReferenceValue = sourceProperty.objectReferenceValue;
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
        T[] components = Resources.FindObjectsOfTypeAll<T>();
        foreach (T component in components)
        {
            if (component != null && component.gameObject.scene == scene && !EditorUtility.IsPersistent(component))
            {
                return component;
            }
        }

        return null;
    }
}
