using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StagePauseMenuBuilder
{
    private const string Stage01ScenePath = "Assets/Scenes/Stage01_Prototype.unity";
    private const string Stage02ScenePath = "Assets/Scenes/Stage02.unity";
    private const string KoreanFontPath = "Assets/_PeckBeat/Fonts/NotoSansKR-Bold SDF.asset";
    private const string EnglishFontPath = "Assets/_PeckBeat/Fonts/Segoe UI Bold SDF.asset";

    [MenuItem("PeckBeat/Shared/Build Stage Pause Menu")]
    private static void BuildStagePauseMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || (scene.path != Stage01ScenePath && scene.path != Stage02ScenePath))
        {
            Debug.LogWarning(
                $"[StagePauseMenuBuilder] Open {Stage01ScenePath} or {Stage02ScenePath} first.");
            return;
        }

        Transform existingRoot = FindSceneObject(scene, "PauseMenuCanvas");
        if (existingRoot != null && existingRoot is not RectTransform)
        {
            Debug.LogWarning("[StagePauseMenuBuilder] PauseMenuCanvas has a non-UI Transform.");
            return;
        }

        RectTransform root = existingRoot as RectTransform;
        bool rootCreated = false;
        if (root == null)
        {
            GameObject rootObject = new GameObject("PauseMenuCanvas", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rootObject, "Create Pause Menu Canvas");
            root = rootObject.GetComponent<RectTransform>();
            rootCreated = true;
        }

        if (!root.gameObject.activeSelf)
        {
            Undo.RecordObject(root.gameObject, "Enable Pause Menu Controller Root");
            root.gameObject.SetActive(true);
        }

        Canvas canvas = root.GetComponent<Canvas>();
        bool canvasCreated = canvas == null;
        if (canvasCreated)
        {
            canvas = Undo.AddComponent<Canvas>(root.gameObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
        }

        if (root.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = Undo.AddComponent<CanvasScaler>(root.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (root.GetComponent<GraphicRaycaster>() == null)
        {
            Undo.AddComponent<GraphicRaycaster>(root.gameObject);
        }

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = Undo.AddComponent<CanvasGroup>(root.gameObject);
        }

        AudioSource audioSource = root.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = Undo.AddComponent<AudioSource>(root.gameObject);
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        RectTransform dimOverlay = GetOrCreateRect(root, "DimOverlay", out bool dimCreated);
        RectTransform curtainRoot = GetOrCreateRect(root, "CurtainRoot", out bool curtainCreated);
        RectTransform curtainPanel = GetOrCreateRect(curtainRoot, "CurtainPanel", out bool panelCreated);
        RectTransform pauseRat = GetOrCreateRect(curtainRoot, "PauseRatImage", out bool pauseRatCreated);
        RectTransform menuRoot = GetOrCreateRect(root, "MenuRoot", out bool menuCreated);
        RectTransform title = GetOrCreateRect(menuRoot, "TitleText", out bool titleCreated);
        RectTransform continueText = GetOrCreateRect(menuRoot, "ContinueText", out bool continueCreated);
        RectTransform restartText = GetOrCreateRect(menuRoot, "RestartText", out bool restartCreated);
        RectTransform quitText = GetOrCreateRect(menuRoot, "QuitText", out bool quitCreated);
        RectTransform focusRat = GetOrCreateRect(menuRoot, "FocusRatImage", out bool focusCreated);
        RectTransform countdownRoot = GetOrCreateRect(root, "CountdownRoot", out bool countdownRootCreated);
        RectTransform countdownImage = GetOrCreateRect(countdownRoot, "CountdownImage", out bool countdownCreated);
        RectTransform fadeOverlay = GetOrCreateRect(root, "FadeOverlay", out bool fadeCreated);

        if (dimOverlay == null || curtainRoot == null || curtainPanel == null || pauseRat == null
            || menuRoot == null || title == null || continueText == null || restartText == null
            || quitText == null || focusRat == null || countdownRoot == null
            || countdownImage == null || fadeOverlay == null)
        {
            Debug.LogWarning("[StagePauseMenuBuilder] An existing pause object has a non-UI Transform.");
            return;
        }

        if (rootCreated)
        {
            SetStretch(root);
        }

        if (dimCreated) SetStretch(dimOverlay);
        if (curtainCreated) SetStretch(curtainRoot);
        if (panelCreated) SetStretch(curtainPanel);
        if (pauseRatCreated) SetRect(pauseRat, new Vector2(0f, -210f), new Vector2(420f, 420f));
        if (menuCreated) SetStretch(menuRoot);
        if (titleCreated) SetRect(title, new Vector2(0f, 210f), new Vector2(900f, 110f));
        if (continueCreated) SetRect(continueText, new Vector2(-340f, 20f), new Vector2(400f, 90f));
        if (restartCreated) SetRect(restartText, new Vector2(0f, 20f), new Vector2(520f, 90f));
        if (quitCreated) SetRect(quitText, new Vector2(340f, 20f), new Vector2(400f, 90f));
        if (focusCreated) SetRect(focusRat, new Vector2(-340f, -105f), new Vector2(180f, 180f));
        if (countdownRootCreated) SetStretch(countdownRoot);
        if (countdownCreated) SetRect(countdownImage, Vector2.zero, new Vector2(500f, 500f));
        if (fadeCreated) SetStretch(fadeOverlay);

        Image dimImage = EnsureImage(dimOverlay.gameObject, out bool dimImageCreated);
        Image curtainImage = EnsureImage(curtainPanel.gameObject, out bool curtainImageCreated);
        Image pauseRatImage = EnsureImage(pauseRat.gameObject, out _);
        Image focusRatImage = EnsureImage(focusRat.gameObject, out _);
        Image countdown = EnsureImage(countdownImage.gameObject, out _);
        Image fadeImage = EnsureImage(fadeOverlay.gameObject, out bool fadeImageCreated);

        if (dimImageCreated)
        {
            dimImage.color = new Color(0f, 0f, 0f, 0.55f);
        }

        if (curtainImageCreated)
        {
            curtainImage.color = new Color(0f, 0f, 0f, 0.9f);
        }

        if (fadeImageCreated)
        {
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
        }

        TMP_Text titleLabel = EnsureText(title.gameObject, "일시 정지", 64f);
        TMP_Text continueLabel = EnsureText(continueText.gameObject, "계속하기", 42f);
        TMP_Text restartLabel = EnsureText(restartText.gameObject, "스테이지 다시하기", 42f);
        TMP_Text quitLabel = EnsureText(quitText.gameObject, "그만하기", 42f);

        StagePauseMenuController controller = root.GetComponent<StagePauseMenuController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<StagePauseMenuController>(root.gameObject);
        }

        Undo.RecordObject(controller, "Connect Stage Pause Menu");
        SerializedObject serialized = new SerializedObject(controller);
        SetReferenceIfEmpty(serialized, "pauseCanvas", canvas);
        SetReferenceIfEmpty(serialized, "pauseCanvasGroup", canvasGroup);
        SetReferenceIfEmpty(serialized, "curtainRoot", curtainRoot);
        SetReferenceIfEmpty(serialized, "menuRoot", menuRoot.gameObject);
        SetReferenceIfEmpty(serialized, "countdownRoot", countdownRoot.gameObject);
        SetReferenceIfEmpty(serialized, "pauseRatImage", pauseRatImage);
        SetReferenceIfEmpty(serialized, "focusRatImage", focusRatImage);
        SetReferenceIfEmpty(serialized, "countdownImage", countdown);
        SetReferenceIfEmpty(serialized, "dimOverlay", dimImage);
        SetReferenceIfEmpty(serialized, "fadeOverlay", fadeImage);
        SetReferenceIfEmpty(serialized, "titleText", titleLabel);
        SetReferenceIfEmpty(serialized, "continueText", continueLabel);
        SetReferenceIfEmpty(serialized, "restartText", restartLabel);
        SetReferenceIfEmpty(serialized, "quitText", quitLabel);
        SetReferenceIfEmpty(serialized, "uiAudioSource", audioSource);

        TMP_FontAsset koreanFont = FindExistingSceneFont(
            scene,
            "koreanFont",
            "koreanTmpFontAsset");
        TMP_FontAsset englishFont = FindExistingSceneFont(
            scene,
            "englishFont",
            "englishTmpFontAsset");
        koreanFont ??= AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        englishFont ??= AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EnglishFontPath);
        AssignAssetIfEmpty(serialized, "koreanFont", koreanFont);
        AssignAssetIfEmpty(serialized, "englishFont", englishFont);

        AssignAssetIfEmpty(serialized, "pauseRatSprite", FindAssetByExactName<Sprite>("일시정쥐"));
        AssignAssetIfEmpty(serialized, "focusRatSprite", FindAssetByExactName<Sprite>("이쪽이쥐"));
        AssignAssetIfEmpty(serialized, "countdown3Sprite", FindAssetByExactName<Sprite>("카운터다운3"));
        AssignAssetIfEmpty(serialized, "countdown2Sprite", FindAssetByExactName<Sprite>("카운터다운2"));
        AssignAssetIfEmpty(serialized, "countdown1Sprite", FindAssetByExactName<Sprite>("카운터다운1"));
        AssignAssetIfEmpty(serialized, "pauseCurtainSfx", FindAssetByExactName<AudioClip>("일시정지커튼소리"));
        AssignAssetIfEmpty(serialized, "countdownSfx", FindAssetByExactName<AudioClip>("카운트다운소리"));
        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root.gameObject;

        Debug.Log(
            "[StagePauseMenuBuilder] Pause menu is ready. Missing art/audio can be assigned on StagePauseMenuController. The scene was not saved.",
            root);
    }

    private static Transform FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindChildRecursive(root.transform, objectName);
            if (result != null)
            {
                return result;
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
            Transform result = FindChildRecursive(parent.GetChild(i), objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static RectTransform GetOrCreateRect(Transform parent, string objectName, out bool created)
    {
        created = false;
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing as RectTransform;
        }

        GameObject child = new GameObject(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(child, $"Create {objectName}");
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        created = true;
        return rect;
    }

    private static Image EnsureImage(GameObject target, out bool created)
    {
        Image image = target.GetComponent<Image>();
        created = image == null;
        if (created)
        {
            image = Undo.AddComponent<Image>(target);
            image.raycastTarget = false;
            image.preserveAspect = true;
        }

        return image;
    }

    private static TMP_Text EnsureText(GameObject target, string defaultText, float fontSize)
    {
        TMP_Text text = target.GetComponent<TMP_Text>();
        if (text == null)
        {
            TextMeshProUGUI created = Undo.AddComponent<TextMeshProUGUI>(target);
            created.text = defaultText;
            created.fontSize = fontSize;
            created.color = Color.white;
            created.alignment = TextAlignmentOptions.Center;
            created.textWrappingMode = TextWrappingModes.NoWrap;
            created.raycastTarget = false;
            text = created;
        }

        return text;
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

    private static void SetReferenceIfEmpty(
        SerializedObject serialized,
        string propertyName,
        Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null && property.objectReferenceValue == null && value != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void AssignAssetIfEmpty(
        SerializedObject serialized,
        string propertyName,
        Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue != null)
        {
            return;
        }

        if (value != null)
        {
            property.objectReferenceValue = value;
        }
        else
        {
            Debug.LogWarning($"[StagePauseMenuBuilder] Asset for '{propertyName}' was not found.");
        }
    }

    private static TMP_FontAsset FindExistingSceneFont(
        Scene scene,
        params string[] propertyNames)
    {
        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour.gameObject.scene != scene)
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(behaviour);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property?.objectReferenceValue is TMP_FontAsset font)
                {
                    return font;
                }
            }
        }

        return null;
    }

    private static T FindAssetByExactName<T>(string assetName) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:{typeof(T).Name}", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != assetName)
            {
                continue;
            }

            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }
        }

        return null;
    }
}
