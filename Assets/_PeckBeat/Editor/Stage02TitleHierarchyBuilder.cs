using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage02TitleHierarchyBuilder
{
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";

    [MenuItem("PeckBeat/Title/Build Stage02 Title UI")]
    private static void BuildStage02TitleUI()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != TitleScenePath)
        {
            Debug.LogWarning($"[Stage02TitleBuilder] Open {TitleScenePath} before building the UI.");
            return;
        }

        Transform canvas = FindCanvasContainingStage01Title(scene);
        if (canvas == null)
        {
            Debug.LogWarning("[Stage02TitleBuilder] Canvas containing Stage01_Title was not found.");
            return;
        }

        RectTransform stage02Title = GetOrCreateRect(canvas, "Stage02_Title", out bool stage02Created);
        if (stage02Title == null)
        {
            return;
        }

        RectTransform background = GetOrCreateRect(stage02Title, "Stage02Background", out bool backgroundCreated);
        RectTransform logo = GetOrCreateRect(stage02Title, "Stage02Logo", out bool logoCreated);
        RectTransform guideRoot = GetOrCreateRect(stage02Title, "Stage02GuideRoot", out bool guideCreated);
        RectTransform guideTitle = GetOrCreateRect(guideRoot, "GuideTitleText", out bool guideTitleCreated);
        RectTransform leftGuide = GetOrCreateRect(guideRoot, "LeftGuide", out bool leftCreated);
        RectTransform leftButton = GetOrCreateRect(leftGuide, "LeftButtonImage", out bool leftButtonCreated);
        RectTransform leftText = GetOrCreateRect(leftGuide, "LeftGuideText", out bool leftTextCreated);
        RectTransform rightGuide = GetOrCreateRect(guideRoot, "RightGuide", out bool rightCreated);
        RectTransform rightButton = GetOrCreateRect(rightGuide, "RightButtonImage", out bool rightButtonCreated);
        RectTransform rightText = GetOrCreateRect(rightGuide, "RightGuideText", out bool rightTextCreated);

        if (background == null || logo == null || guideRoot == null || guideTitle == null
            || leftGuide == null || leftButton == null || leftText == null
            || rightGuide == null || rightButton == null || rightText == null)
        {
            Debug.LogWarning("[Stage02TitleBuilder] An existing object has an incompatible non-UI Transform.");
            return;
        }

        if (stage02Created)
        {
            SetStretch(stage02Title);
            stage02Title.gameObject.SetActive(false);
        }

        if (backgroundCreated)
        {
            SetStretch(background);
        }

        if (logoCreated)
        {
            SetRect(logo, new Vector2(0f, 170f), new Vector2(1000f, 360f));
        }

        if (guideCreated)
        {
            SetRect(guideRoot, new Vector2(0f, -245f), new Vector2(1500f, 220f));
        }

        if (guideTitleCreated)
        {
            SetRect(guideTitle, new Vector2(0f, 70f), new Vector2(700f, 56f));
        }

        if (leftCreated)
        {
            SetRect(leftGuide, new Vector2(-350f, -20f), new Vector2(650f, 130f));
        }

        if (rightCreated)
        {
            SetRect(rightGuide, new Vector2(350f, -20f), new Vector2(650f, 130f));
        }

        if (leftButtonCreated)
        {
            SetRect(leftButton, new Vector2(-245f, 0f), new Vector2(96f, 96f));
        }

        if (rightButtonCreated)
        {
            SetRect(rightButton, new Vector2(-245f, 0f), new Vector2(96f, 96f));
        }

        if (leftTextCreated)
        {
            SetRect(leftText, new Vector2(70f, 0f), new Vector2(500f, 110f));
        }

        if (rightTextCreated)
        {
            SetRect(rightText, new Vector2(70f, 0f), new Vector2(500f, 110f));
        }

        Image backgroundImage = EnsureImage(background.gameObject);
        Image logoImage = EnsureImage(logo.gameObject);
        Image leftButtonImage = EnsureImage(leftButton.gameObject);
        Image rightButtonImage = EnsureImage(rightButton.gameObject);
        TMP_Text guideTitleText = EnsureText(guideTitle.gameObject, "게임 방법", 38f);
        TMP_Text leftGuideText = EnsureText(leftText.gameObject, "박자에 맞춰 쪼기!", 38f);
        TMP_Text rightGuideText = EnsureText(rightText.gameObject, "길게 눌러 깎기!", 38f);

        if (stage02Title.GetComponent<CanvasGroup>() == null)
        {
            Undo.AddComponent<CanvasGroup>(stage02Title.gameObject);
        }

        Stage02TitleGuideUI guide = stage02Title.GetComponent<Stage02TitleGuideUI>();
        if (guide == null)
        {
            guide = Undo.AddComponent<Stage02TitleGuideUI>(stage02Title.gameObject);
        }

        Undo.RecordObject(guide, "Connect Stage02 Title Guide UI");
        SerializedObject serializedGuide = new SerializedObject(guide);
        SetObjectReferenceIfEmpty(serializedGuide, "guideRoot", guideRoot.gameObject);
        SetObjectReferenceIfEmpty(serializedGuide, "guideTitleText", guideTitleText);
        SetObjectReferenceIfEmpty(serializedGuide, "leftButtonImage", leftButtonImage);
        SetObjectReferenceIfEmpty(serializedGuide, "leftGuideText", leftGuideText);
        SetObjectReferenceIfEmpty(serializedGuide, "rightButtonImage", rightButtonImage);
        SetObjectReferenceIfEmpty(serializedGuide, "rightGuideText", rightGuideText);
        CopyStage01GuideAssets(scene, serializedGuide);
        serializedGuide.ApplyModifiedProperties();

        TitleManager titleManager = FindSceneComponent<TitleManager>(scene);
        if (titleManager != null)
        {
            Undo.RecordObject(titleManager, "Connect Stage02 Title UI");
            SerializedObject serializedManager = new SerializedObject(titleManager);
            SetObjectReferenceIfEmpty(serializedManager, "stage02Title", stage02Title.gameObject);
            SetObjectReferenceIfEmpty(serializedManager, "stage02BackgroundImage", backgroundImage);
            SetObjectReferenceIfEmpty(serializedManager, "stage02LogoImage", logoImage);
            SetObjectReferenceIfEmpty(serializedManager, "stage02TitleGuideUI", guide);
            serializedManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(titleManager);
        }
        else
        {
            Debug.LogWarning("[Stage02TitleBuilder] TitleManager was not found; UI references were not connected.");
        }

        EditorUtility.SetDirty(guide);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = stage02Title.gameObject;

        Debug.Log(
            "[Stage02TitleBuilder] Stage02 title UI is ready. Assign the background/logo and J/B button sprites in the Inspector. The scene was not saved.",
            stage02Title);
    }

    private static Transform FindCanvasContainingStage01Title(Scene scene)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform candidate in transforms)
        {
            if (candidate == null || candidate.name != "Canvas"
                || candidate.gameObject.scene != scene || EditorUtility.IsPersistent(candidate))
            {
                continue;
            }

            if (FindChildRecursive(candidate, "Stage01_Title") != null)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static RectTransform GetOrCreateRect(Transform parent, string name, out bool created)
    {
        created = false;
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing as RectTransform;
        }

        GameObject child = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        created = true;
        return rect;
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

    private static Image EnsureImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = Undo.AddComponent<Image>(target);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = true;
        }

        return image;
    }

    private static TMP_Text EnsureText(GameObject target, string defaultText, float defaultFontSize)
    {
        TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = Undo.AddComponent<TextMeshProUGUI>(target);
            text.text = defaultText;
            text.fontSize = defaultFontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
        }

        return text;
    }

    private static void CopyStage01GuideAssets(Scene scene, SerializedObject destination)
    {
        Stage01TitleGuideUI sourceGuide = FindSceneComponent<Stage01TitleGuideUI>(scene);
        if (sourceGuide == null)
        {
            return;
        }

        SerializedObject source = new SerializedObject(sourceGuide);
        CopyObjectReferenceIfEmpty(source, "keyboardDButtonSprite", destination, "keyboardDButtonSprite");
        CopyObjectReferenceIfEmpty(source, "keyboardJButtonSprite", destination, "keyboardJButtonSprite");
        CopyObjectReferenceIfEmpty(source, "gamepadAButtonSprite", destination, "gamepadAButtonSprite");
        CopyObjectReferenceIfEmpty(source, "gamepadBButtonSprite", destination, "gamepadBButtonSprite");
        CopyObjectReferenceIfEmpty(source, "koreanTmpFontAsset", destination, "koreanTmpFontAsset");
        CopyObjectReferenceIfEmpty(source, "englishTmpFontAsset", destination, "englishTmpFontAsset");
    }

    private static void CopyObjectReferenceIfEmpty(
        SerializedObject source,
        string sourceName,
        SerializedObject destination,
        string destinationName)
    {
        SerializedProperty sourceProperty = source.FindProperty(sourceName);
        SerializedProperty destinationProperty = destination.FindProperty(destinationName);
        if (sourceProperty != null && destinationProperty != null
            && destinationProperty.objectReferenceValue == null)
        {
            destinationProperty.objectReferenceValue = sourceProperty.objectReferenceValue;
        }
    }

    private static void SetObjectReferenceIfEmpty(
        SerializedObject target,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property != null && property.objectReferenceValue == null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        T[] components = Resources.FindObjectsOfTypeAll<T>();
        foreach (T component in components)
        {
            if (component != null && component.gameObject.scene == scene
                && !EditorUtility.IsPersistent(component))
            {
                return component;
            }
        }

        return null;
    }
}
