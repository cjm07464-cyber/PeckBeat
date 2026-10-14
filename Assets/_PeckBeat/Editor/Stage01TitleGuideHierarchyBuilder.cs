using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage01TitleGuideHierarchyBuilder
{
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";

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
        RectTransform leftButton = GetOrCreateRect(leftGuide, "LeftButtonImage", out bool leftButtonCreated);
        RectTransform leftText = GetOrCreateRect(leftGuide, "LeftGuideText", out bool leftTextCreated);
        RectTransform rightGuide = GetOrCreateRect(guideRoot, "RightGuide", out bool rightCreated);
        RectTransform rightButton = GetOrCreateRect(rightGuide, "RightButtonImage", out bool rightButtonCreated);
        RectTransform rightText = GetOrCreateRect(rightGuide, "RightGuideText", out bool rightTextCreated);

        if (rootCreated)
        {
            SetRect(guideRoot, new Vector2(960f, 250f), new Vector2(1400f, 170f));
        }

        if (leftCreated)
        {
            SetRect(leftGuide, new Vector2(-350f, -30f), new Vector2(620f, 130f));
        }
        else
        {
            MoveGuideDownIfAtDefaultPosition(leftGuide);
        }

        if (rightCreated)
        {
            SetRect(rightGuide, new Vector2(350f, -30f), new Vector2(620f, 130f));
        }
        else
        {
            MoveGuideDownIfAtDefaultPosition(rightGuide);
        }

        TMP_Text guideTitleText = EnsureText(guideTitle.gameObject, "게임 방법", 36f);
        Image leftButtonImage = EnsureImage(leftButton.gameObject);
        Image rightButtonImage = EnsureImage(rightButton.gameObject);
        TMP_Text leftGuideText = EnsureText(leftText.gameObject, "한 번 누르기!", 38f);
        TMP_Text rightGuideText = EnsureText(rightText.gameObject, "두 번 누르기!", 38f);

        if (guideTitleCreated)
        {
            SetRect(guideTitle, new Vector2(0f, 50f), new Vector2(600f, 50f));
        }

        if (leftButtonCreated)
        {
            SetRect(leftButton, new Vector2(-235f, 0f), new Vector2(96f, 96f));
        }

        if (rightButtonCreated)
        {
            SetRect(rightButton, new Vector2(-235f, 0f), new Vector2(96f, 96f));
        }

        if (leftTextCreated)
        {
            SetRect(leftText, new Vector2(55f, 0f), new Vector2(470f, 110f));
        }

        if (rightTextCreated)
        {
            SetRect(rightText, new Vector2(55f, 0f), new Vector2(470f, 110f));
        }

        Stage01TitleGuideUI guide = stage01Title.GetComponent<Stage01TitleGuideUI>();
        if (guide == null)
        {
            guide = Undo.AddComponent<Stage01TitleGuideUI>(stage01Title.gameObject);
        }

        Undo.RecordObject(guide, "Connect Stage01 Title Guide UI");
        SerializedObject serializedGuide = new SerializedObject(guide);
        SetObjectReference(serializedGuide, "guideRoot", guideRoot.gameObject);
        SetObjectReference(serializedGuide, "guideTitleText", guideTitleText);
        SetObjectReference(serializedGuide, "leftButtonImage", leftButtonImage);
        SetObjectReference(serializedGuide, "leftGuideText", leftGuideText);
        SetObjectReference(serializedGuide, "rightButtonImage", rightButtonImage);
        SetObjectReference(serializedGuide, "rightGuideText", rightGuideText);
        CopyTitleManagerFonts(scene, serializedGuide);
        serializedGuide.ApplyModifiedProperties();

        EditorUtility.SetDirty(guide);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = guideRoot.gameObject;

        Debug.Log(
            "[Stage01TitleGuideBuilder] Stage01 guide hierarchy is ready. " +
            "Assign D/J/A/B button sprites on Stage01TitleGuideUI. The scene was not saved.",
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

    private static void MoveGuideDownIfAtDefaultPosition(RectTransform rect)
    {
        if (!Mathf.Approximately(rect.anchoredPosition.y, 0f))
        {
            return;
        }

        Undo.RecordObject(rect, $"Adjust {rect.name} For Guide Title");
        Vector2 position = rect.anchoredPosition;
        position.y = -30f;
        rect.anchoredPosition = position;
    }

    private static Image EnsureImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = Undo.AddComponent<Image>(target);
        }

        Undo.RecordObject(image, $"Configure {target.name}");
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = image.sprite != null;
        return image;
    }

    private static TMP_Text EnsureText(GameObject target, string defaultText, float fontSize)
    {
        TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = Undo.AddComponent<TextMeshProUGUI>(target);
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
        }

        Undo.RecordObject(text, $"Configure {target.name}");
        text.text = defaultText;
        text.raycastTarget = false;
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

    private static void SetObjectReference(SerializedObject target, string propertyName, Object value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property != null)
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
