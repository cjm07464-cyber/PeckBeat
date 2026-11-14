using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage02TitleHierarchyBuilder
{
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
    private const string OneHitLogPath =
        "Assets/_PeckBeat/Art/Stage_02/Player/Objects/일반통나무.png";
    private const string TwoHitLogPath =
        "Assets/_PeckBeat/Art/Stage_02/Player/Objects/2타통나무.png";
    private const string HoldLogPath =
        "Assets/_PeckBeat/Art/Stage_02/Player/Objects/홀드_긴통나무_완전옆면.png";

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
        RectTransform guidePanel = GetOrCreateRect(guideRoot, "GuidePanelImage", out bool guidePanelCreated);
        RectTransform guideTitle = GetOrCreateRect(guideRoot, "GuideTitleText", out bool guideTitleCreated);
        RectTransform leftGuide = GetOrCreateRect(guideRoot, "LeftGuide", out bool leftCreated);
        RectTransform leftTarget = GetOrCreateRect(leftGuide, "LogImage", out bool leftTargetCreated);
        RectTransform leftButton = GetOrCreateRect(leftGuide, "LeftButtonImage", out bool leftButtonCreated);
        RectTransform leftText = GetOrCreateRect(leftGuide, "LeftGuideText", out bool leftTextCreated);
        RectTransform middleGuide = GetOrCreateRect(guideRoot, "MiddleGuide", out bool middleCreated);
        RectTransform middleTarget = GetOrCreateRect(middleGuide, "LogImage", out bool middleTargetCreated);
        RectTransform middleButton = GetOrCreateRect(middleGuide, "MiddleButtonImage", out bool middleButtonCreated);
        RectTransform middleText = GetOrCreateRect(middleGuide, "MiddleGuideText", out bool middleTextCreated);
        RectTransform rightGuide = GetOrCreateRect(guideRoot, "RightGuide", out bool rightCreated);
        RectTransform rightTarget = GetOrCreateRect(rightGuide, "LogImage", out bool rightTargetCreated);
        RectTransform rightButton = GetOrCreateRect(rightGuide, "RightButtonImage", out bool rightButtonCreated);
        RectTransform rightText = GetOrCreateRect(rightGuide, "RightGuideText", out bool rightTextCreated);
        RectTransform startPrompt = GetOrCreateRect(guideRoot, "StartPromptText", out bool promptCreated);
        RectTransform holdProgress = GetOrCreateRect(startPrompt, "StartHoldProgressImage", out bool progressCreated);

        if (background == null || logo == null || guideRoot == null || guidePanel == null
            || guideTitle == null || leftGuide == null || leftTarget == null
            || leftButton == null || leftText == null || middleGuide == null
            || middleTarget == null || middleButton == null || middleText == null
            || rightGuide == null || rightTarget == null || rightButton == null
            || rightText == null || startPrompt == null || holdProgress == null)
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
            SetRect(guideRoot, new Vector2(0f, -180f), new Vector2(1580f, 540f));
        }
        else
        {
            MigrateOldDefaultRect(
                guideRoot,
                new Vector2(0f, -245f),
                new Vector2(1500f, 220f),
                new Vector2(0f, -180f),
                new Vector2(1580f, 540f));
        }

        if (guidePanelCreated)
        {
            SetStretch(guidePanel);
            guidePanel.SetAsFirstSibling();
        }

        if (guideTitleCreated)
        {
            SetRect(guideTitle, new Vector2(0f, 70f), new Vector2(700f, 56f));
        }

        if (leftCreated)
        {
            SetRect(leftGuide, new Vector2(-510f, -25f), new Vector2(480f, 300f));
        }
        else
        {
            MigrateOldDefaultRect(
                leftGuide,
                new Vector2(-350f, -20f),
                new Vector2(650f, 130f),
                new Vector2(-510f, -25f),
                new Vector2(480f, 300f));
        }

        if (middleCreated)
        {
            SetRect(middleGuide, new Vector2(0f, -25f), new Vector2(480f, 300f));
        }

        if (rightCreated)
        {
            SetRect(rightGuide, new Vector2(510f, -25f), new Vector2(480f, 300f));
        }
        else
        {
            MigrateOldDefaultRect(
                rightGuide,
                new Vector2(350f, -20f),
                new Vector2(650f, 130f),
                new Vector2(510f, -25f),
                new Vector2(480f, 300f));
        }

        if (leftButtonCreated)
        {
            SetRect(leftButton, new Vector2(-140f, -75f), new Vector2(88f, 88f));
        }
        else
        {
            MigrateOldDefaultRect(leftButton, new Vector2(-245f, 0f), new Vector2(96f, 96f),
                new Vector2(-140f, -75f), new Vector2(88f, 88f));
        }

        if (middleButtonCreated)
        {
            SetRect(middleButton, new Vector2(-140f, -75f), new Vector2(88f, 88f));
        }

        if (rightButtonCreated)
        {
            SetRect(rightButton, new Vector2(-140f, -75f), new Vector2(88f, 88f));
        }
        else
        {
            MigrateOldDefaultRect(rightButton, new Vector2(-245f, 0f), new Vector2(96f, 96f),
                new Vector2(-140f, -75f), new Vector2(88f, 88f));
        }

        if (leftTextCreated)
        {
            SetRect(leftText, new Vector2(70f, -75f), new Vector2(330f, 110f));
        }
        else
        {
            MigrateOldDefaultRect(leftText, new Vector2(70f, 0f), new Vector2(500f, 110f),
                new Vector2(70f, -75f), new Vector2(330f, 110f));
        }

        if (middleTextCreated)
        {
            SetRect(middleText, new Vector2(70f, -75f), new Vector2(330f, 110f));
        }

        if (rightTextCreated)
        {
            SetRect(rightText, new Vector2(70f, -75f), new Vector2(330f, 110f));
        }
        else
        {
            MigrateOldDefaultRect(rightText, new Vector2(70f, 0f), new Vector2(500f, 110f),
                new Vector2(70f, -75f), new Vector2(330f, 110f));
        }

        if (leftTargetCreated) SetRect(leftTarget, new Vector2(0f, 65f), new Vector2(330f, 150f));
        if (middleTargetCreated) SetRect(middleTarget, new Vector2(0f, 65f), new Vector2(330f, 150f));
        if (rightTargetCreated) SetRect(rightTarget, new Vector2(0f, 65f), new Vector2(330f, 150f));
        if (promptCreated)
        {
            SetRect(startPrompt, new Vector2(0f, -215f), new Vector2(760f, 70f));
        }

        if (progressCreated)
        {
            SetRect(holdProgress, new Vector2(420f, 0f), new Vector2(54f, 54f));
        }

        Image backgroundImage = EnsureImage(background.gameObject);
        Image logoImage = EnsureImage(logo.gameObject);
        Image guidePanelImage = EnsureImage(guidePanel.gameObject);
        Image leftTargetImage = EnsureImage(leftTarget.gameObject);
        Image leftButtonImage = EnsureImage(leftButton.gameObject);
        Image middleTargetImage = EnsureImage(middleTarget.gameObject);
        Image middleButtonImage = EnsureImage(middleButton.gameObject);
        Image rightTargetImage = EnsureImage(rightTarget.gameObject);
        Image rightButtonImage = EnsureImage(rightButton.gameObject);
        Image progressImage = EnsureImage(holdProgress.gameObject);
        TMP_Text guideTitleText = EnsureText(guideTitle.gameObject, "게임 방법", 38f);
        TMP_Text leftGuideText = EnsureText(leftText.gameObject, "박자에 맞춰 한 번!", 32f);
        TMP_Text middleGuideText = EnsureText(middleText.gameObject, "박자에 맞춰 두 번!", 32f);
        TMP_Text rightGuideText = EnsureText(rightText.gameObject, "길게 눌러 깎기!", 38f);
        TMP_Text startPromptText = EnsureText(startPrompt.gameObject, "꾹 눌러서 시작!", 36f);

        if (guideRoot.GetComponent<CanvasGroup>() == null)
        {
            Undo.AddComponent<CanvasGroup>(guideRoot.gameObject);
        }

        if (progressCreated)
        {
            progressImage.type = Image.Type.Filled;
            progressImage.fillMethod = Image.FillMethod.Radial360;
            progressImage.fillOrigin = (int)Image.Origin360.Top;
            progressImage.fillClockwise = true;
            progressImage.fillAmount = 0f;
            progressImage.color = Color.white;
        }

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
        SetObjectReferenceIfEmpty(serializedGuide, "guidePanelImage", guidePanelImage);
        SetObjectReferenceIfEmpty(serializedGuide, "guideCanvasGroup", guideRoot.GetComponent<CanvasGroup>());
        SetObjectReferenceIfEmpty(serializedGuide, "guideTitleText", guideTitleText);
        SetObjectReferenceIfEmpty(serializedGuide, "leftTargetImage", leftTargetImage);
        SetObjectReferenceIfEmpty(serializedGuide, "leftButtonImage", leftButtonImage);
        SetObjectReferenceIfEmpty(serializedGuide, "leftGuideText", leftGuideText);
        SetObjectReferenceIfEmpty(serializedGuide, "middleTargetImage", middleTargetImage);
        SetObjectReferenceIfEmpty(serializedGuide, "middleButtonImage", middleButtonImage);
        SetObjectReferenceIfEmpty(serializedGuide, "middleGuideText", middleGuideText);
        SetObjectReferenceIfEmpty(serializedGuide, "rightTargetImage", rightTargetImage);
        SetObjectReferenceIfEmpty(serializedGuide, "rightButtonImage", rightButtonImage);
        SetObjectReferenceIfEmpty(serializedGuide, "rightGuideText", rightGuideText);
        SetObjectReferenceIfEmpty(serializedGuide, "startPromptText", startPromptText);
        SetObjectReferenceIfEmpty(serializedGuide, "startHoldProgressImage", progressImage);
        SetObjectReferenceIfEmpty(
            serializedGuide,
            "oneHitLogSprite",
            AssetDatabase.LoadAssetAtPath<Sprite>(OneHitLogPath));
        SetObjectReferenceIfEmpty(
            serializedGuide,
            "twoHitLogSprite",
            AssetDatabase.LoadAssetAtPath<Sprite>(TwoHitLogPath));
        SetObjectReferenceIfEmpty(
            serializedGuide,
            "holdLogSprite",
            AssetDatabase.LoadAssetAtPath<Sprite>(HoldLogPath));
        CopyStage01GuideAssets(scene, serializedGuide, guidePanelImage, progressImage);
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

    private static void MigrateOldDefaultRect(
        RectTransform rect,
        Vector2 oldPosition,
        Vector2 oldSize,
        Vector2 newPosition,
        Vector2 newSize)
    {
        if (rect == null || Vector2.Distance(rect.anchoredPosition, oldPosition) > 0.01f
            || Vector2.Distance(rect.sizeDelta, oldSize) > 0.01f)
        {
            return;
        }

        SetRect(rect, newPosition, newSize);
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

    private static void CopyStage01GuideAssets(
        Scene scene,
        SerializedObject destination,
        Image destinationPanel,
        Image destinationProgress)
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
        CopyImageStyleIfEmpty(source, "guidePanelImage", destinationPanel);
        CopyImageStyleIfEmpty(source, "startHoldProgressImage", destinationProgress);
    }

    private static void CopyImageStyleIfEmpty(
        SerializedObject source,
        string sourcePropertyName,
        Image destination)
    {
        Image sourceImage = source.FindProperty(sourcePropertyName)?.objectReferenceValue as Image;
        if (sourceImage == null || destination == null)
        {
            return;
        }

        Undo.RecordObject(destination, $"Copy {sourcePropertyName} Style");
        if (destination.sprite == null)
        {
            destination.sprite = sourceImage.sprite;
        }

        if (destination.sprite != null)
        {
            destination.color = sourceImage.color;
            destination.preserveAspect = sourceImage.preserveAspect;
        }
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
