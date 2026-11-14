using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TitleHoldStartSetupTool
{
    private const string MenuPath = "PeckBeat/Title/Apply Hold Start UI And Stage Title BGM";
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
    private const string NewStageTitleBgmPath =
        "Assets/_PeckBeat/Audio/NEW_스테이지타이틀.mp3";

    [MenuItem(MenuPath)]
    private static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != TitleScenePath)
        {
            Debug.LogWarning($"[TitleHoldStartSetup] Open {TitleScenePath} first.");
            return;
        }

        TitleManager titleManager = FindSceneComponent<TitleManager>(scene);
        Stage01TitleGuideUI guide = FindSceneComponent<Stage01TitleGuideUI>(scene);
        if (titleManager == null || guide == null)
        {
            Debug.LogWarning(
                "[TitleHoldStartSetup] TitleManager or Stage01TitleGuideUI was not found.");
            return;
        }

        AudioClip stageTitleBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(NewStageTitleBgmPath);
        if (stageTitleBgm == null)
        {
            Debug.LogWarning(
                $"[TitleHoldStartSetup] New stage title BGM was not found: {NewStageTitleBgmPath}",
                titleManager);
            return;
        }

        SerializedObject serializedGuide = new(guide);
        TMP_Text startPromptText = serializedGuide.FindProperty("startPromptText")
            ?.objectReferenceValue as TMP_Text;
        if (startPromptText == null || startPromptText.transform.parent == null)
        {
            Debug.LogWarning(
                "[TitleHoldStartSetup] Stage01GuideRoot/StartPromptText is not connected. "
                + "Connect the existing prompt before applying the hold gauge.",
                guide);
            return;
        }

        RectTransform promptParent = startPromptText.transform.parent as RectTransform;
        RectTransform promptRect = startPromptText.rectTransform;
        if (promptParent == null || promptRect == null)
        {
            Debug.LogWarning(
                "[TitleHoldStartSetup] StartPromptText must be under a RectTransform.",
                guide);
            return;
        }

        Image progressImage = GetOrCreateGuideProgressImage(
            promptParent,
            promptRect,
            out bool progressCreated);
        if (progressImage == null)
        {
            return;
        }

        Undo.RecordObject(guide, "Connect Stage01 Guide Hold Progress");
        SetObjectReferenceIfEmpty(serializedGuide, "startHoldProgressImage", progressImage);
        serializedGuide.ApplyModifiedProperties();
        EditorUtility.SetDirty(guide);

        Undo.RecordObject(titleManager, "Connect New Stage Title BGM");
        SerializedObject serializedManager = new(titleManager);
        SerializedProperty bgmProperty = serializedManager.FindProperty("stage01TitleBgmClip");
        if (bgmProperty != null)
        {
            bgmProperty.objectReferenceValue = stageTitleBgm;
        }

        CleanupIncorrectMainTitleHoldUi(serializedManager);
        serializedManager.ApplyModifiedProperties();
        EditorUtility.SetDirty(titleManager);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = promptParent.gameObject;
        Debug.Log(
            "[TitleHoldStartSetup] Stage01GuideRoot hold-start gauge and stage title BGM connected. "
            + $"ProgressCreated={progressCreated}. "
            + "Incorrect MainTitle hold UI was disabled when found. "
            + "Existing UI layout and assignments were preserved. The scene was not saved.",
            guide);
    }

    private static Image GetOrCreateGuideProgressImage(
        RectTransform parent,
        RectTransform promptRect,
        out bool created)
    {
        created = false;
        Transform existing = parent.Find("StartHoldProgressImage");
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (existing != null && image == null)
        {
            Debug.LogWarning(
                "[TitleHoldStartSetup] Existing StartHoldProgressImage does not contain Image.",
                existing);
            return null;
        }

        if (image == null)
        {
            GameObject imageObject = new(
                "StartHoldProgressImage",
                typeof(RectTransform),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(imageObject, "Create Stage01 Guide Hold Progress");
            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = promptRect.anchorMin;
            rect.anchorMax = promptRect.anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = promptRect.anchoredPosition
                + new Vector2(promptRect.rect.width * 0.5f + 52f, 0f);
            rect.sizeDelta = new Vector2(68f, 68f);
            rect.localScale = Vector3.one;

            image = imageObject.GetComponent<Image>();
            image.color = new Color(0.25f, 0.9f, 0.42f, 1f);
            created = true;
        }

        Undo.RecordObject(image, "Configure Stage01 Guide Hold Progress");
        if (image.sprite == null)
        {
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        }

        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = (int)Image.Origin360.Top;
        image.fillClockwise = true;
        image.fillAmount = 0f;
        image.raycastTarget = false;
        EditorUtility.SetDirty(image);
        return image;
    }

    private static void CleanupIncorrectMainTitleHoldUi(SerializedObject serializedManager)
    {
        Image mainPromptImage = serializedManager.FindProperty("pressAnyButtonImage")
            ?.objectReferenceValue as Image;
        Transform mainPromptRoot = mainPromptImage != null ? mainPromptImage.transform.parent : null;
        if (mainPromptRoot == null)
        {
            return;
        }

        Transform incorrectProgress = mainPromptRoot.Find("StartHoldProgressImage");
        DisableWithUndo(incorrectProgress, "Disable Incorrect MainTitle Hold Progress");

        Transform incorrectPrompt = mainPromptRoot.Find("StartPromptText");
        TMP_Text incorrectPromptText = incorrectPrompt != null
            ? incorrectPrompt.GetComponent<TMP_Text>()
            : null;
        SerializedProperty promptProperty = serializedManager.FindProperty("pressAnyButtonText");
        if (promptProperty != null && promptProperty.objectReferenceValue == incorrectPromptText)
        {
            promptProperty.objectReferenceValue = null;
        }

        DisableWithUndo(incorrectPrompt, "Disable Incorrect MainTitle Hold Prompt");
    }

    private static void DisableWithUndo(Transform target, string undoName)
    {
        if (target == null || !target.gameObject.activeSelf)
        {
            return;
        }

        Undo.RecordObject(target.gameObject, undoName);
        target.gameObject.SetActive(false);
        EditorUtility.SetDirty(target.gameObject);
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

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        T[] components = Resources.FindObjectsOfTypeAll<T>();
        foreach (T component in components)
        {
            if (component != null && component.gameObject.scene == scene &&
                !EditorUtility.IsPersistent(component))
            {
                return component;
            }
        }

        return null;
    }
}
