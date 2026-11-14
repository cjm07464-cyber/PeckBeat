using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage01EnglishStartRowTool
{
    private const string MenuPath = "PeckBeat/Title/Build Stage01 English Start Row";
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";

    [MenuItem(MenuPath)]
    private static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != TitleScenePath)
        {
            Debug.LogWarning($"[Stage01EnglishStartRow] Open {TitleScenePath} first.");
            return;
        }

        Stage01TitleGuideUI guide = FindSceneComponent<Stage01TitleGuideUI>(scene);
        if (guide == null)
        {
            Debug.LogWarning("[Stage01EnglishStartRow] Stage01TitleGuideUI was not found.");
            return;
        }

        SerializedObject serializedGuide = new(guide);
        GameObject guideRoot = serializedGuide.FindProperty("guideRoot")
            ?.objectReferenceValue as GameObject;
        TMP_Text koreanPrompt = serializedGuide.FindProperty("startPromptText")
            ?.objectReferenceValue as TMP_Text;
        Image koreanProgress = serializedGuide.FindProperty("startHoldProgressImage")
            ?.objectReferenceValue as Image;
        TMP_FontAsset englishFont = serializedGuide.FindProperty("englishTmpFontAsset")
            ?.objectReferenceValue as TMP_FontAsset;

        RectTransform guideRootRect = guideRoot != null
            ? guideRoot.transform as RectTransform
            : null;
        if (guideRootRect == null || koreanPrompt == null || koreanProgress == null)
        {
            Debug.LogWarning(
                "[Stage01EnglishStartRow] GuideRoot, StartPromptText, or "
                + "StartHoldProgressImage is not connected.",
                guide);
            return;
        }

        RectTransform row = GetOrCreateRow(guideRootRect, out bool rowCreated);
        Image progress = GetOrCreateProgress(row, koreanProgress, out bool progressCreated);
        TMP_Text prompt = GetOrCreatePrompt(
            row,
            koreanPrompt,
            englishFont,
            out bool promptCreated);

        if (progress == null || prompt == null)
        {
            return;
        }

        progress.transform.SetSiblingIndex(0);
        prompt.transform.SetSiblingIndex(1);

        Undo.RecordObject(guide, "Connect Stage01 English Start Row");
        SetObjectReferenceIfEmpty(serializedGuide, "englishStartRow", row.gameObject);
        SetObjectReferenceIfEmpty(serializedGuide, "englishStartPromptText", prompt);
        SetObjectReferenceIfEmpty(
            serializedGuide,
            "englishStartHoldProgressImage",
            progress);
        serializedGuide.ApplyModifiedProperties();
        EditorUtility.SetDirty(guide);

        if (rowCreated && row.gameObject.activeSelf)
        {
            Undo.RecordObject(row.gameObject, "Hide English Start Row By Default");
            row.gameObject.SetActive(false);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = row.gameObject;
        Debug.Log(
            "[Stage01EnglishStartRow] EnglishStartRow connected. "
            + $"RowCreated={rowCreated}, ProgressCreated={progressCreated}, "
            + $"PromptCreated={promptCreated}. The scene was not saved.",
            guide);
    }

    private static RectTransform GetOrCreateRow(
        RectTransform parent,
        out bool created)
    {
        created = false;
        Transform existing = parent.Find("EnglishStartRow");
        RectTransform row = existing as RectTransform;
        if (row == null)
        {
            GameObject rowObject = new("EnglishStartRow", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rowObject, "Create English Start Row");
            rowObject.transform.SetParent(parent, false);
            row = rowObject.GetComponent<RectTransform>();
            row.anchorMin = new Vector2(1f, 0f);
            row.anchorMax = new Vector2(1f, 0f);
            row.pivot = new Vector2(1f, 0f);
            row.anchoredPosition = new Vector2(-55f, 55f);
            row.sizeDelta = new Vector2(420f, 70f);
            row.localScale = Vector3.one;
            created = true;
        }

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = Undo.AddComponent<HorizontalLayoutGroup>(row.gameObject);
            Undo.RecordObject(layout, "Configure English Start Row Layout");
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            EditorUtility.SetDirty(layout);
        }

        return row;
    }

    private static Image GetOrCreateProgress(
        RectTransform parent,
        Image source,
        out bool created)
    {
        created = false;
        Transform existing = parent.Find("EnglishStartHoldProgressImage");
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (existing != null && image == null)
        {
            Debug.LogWarning(
                "[Stage01EnglishStartRow] Existing EnglishStartHoldProgressImage "
                + "does not contain an Image.",
                existing);
            return null;
        }

        if (image == null)
        {
            GameObject imageObject = new(
                "EnglishStartHoldProgressImage",
                typeof(RectTransform),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(imageObject, "Create English Hold Progress");
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(56f, 56f);
            rect.localScale = Vector3.one;

            image = imageObject.GetComponent<Image>();
            image.sprite = source.sprite;
            image.color = source.color;
            image.material = source.material;
            image.raycastTarget = false;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.fillAmount = 0f;
            created = true;
        }

        LayoutElement element = image.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = Undo.AddComponent<LayoutElement>(image.gameObject);
            Undo.RecordObject(element, "Configure English Hold Progress Size");
            element.minWidth = 56f;
            element.preferredWidth = 56f;
            element.minHeight = 56f;
            element.preferredHeight = 56f;
            EditorUtility.SetDirty(element);
        }

        EditorUtility.SetDirty(image);
        return image;
    }

    private static TMP_Text GetOrCreatePrompt(
        RectTransform parent,
        TMP_Text source,
        TMP_FontAsset englishFont,
        out bool created)
    {
        created = false;
        Transform existing = parent.Find("EnglishStartPromptText");
        TMP_Text prompt = existing != null ? existing.GetComponent<TMP_Text>() : null;
        if (existing != null && prompt == null)
        {
            Debug.LogWarning(
                "[Stage01EnglishStartRow] Existing EnglishStartPromptText "
                + "does not contain TMP_Text.",
                existing);
            return null;
        }

        if (prompt == null)
        {
            GameObject promptObject = new(
                "EnglishStartPromptText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(promptObject, "Create English Start Prompt");
            promptObject.transform.SetParent(parent, false);
            RectTransform rect = promptObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 60f);
            rect.localScale = Vector3.one;

            prompt = promptObject.GetComponent<TMP_Text>();
            prompt.text = "Hold to Start!";
            prompt.font = englishFont != null ? englishFont : source.font;
            prompt.fontSharedMaterial = englishFont != null
                ? englishFont.material
                : source.fontSharedMaterial;
            prompt.fontSize = source.fontSize * 0.8f;
            prompt.color = source.color;
            prompt.alignment = TextAlignmentOptions.Center;
            prompt.textWrappingMode = TextWrappingModes.NoWrap;
            prompt.margin = Vector4.zero;
            prompt.raycastTarget = false;
            created = true;
        }
        else
        {
            Undo.RecordObject(prompt, "Normalize English Start Prompt Margin");
            prompt.margin = Vector4.zero;
        }

        LayoutElement element = prompt.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = Undo.AddComponent<LayoutElement>(prompt.gameObject);
            Undo.RecordObject(element, "Configure English Start Prompt Size");
            element.minWidth = 240f;
            element.preferredWidth = 280f;
            element.minHeight = 50f;
            element.preferredHeight = 60f;
            EditorUtility.SetDirty(element);
        }

        EditorUtility.SetDirty(prompt);
        return prompt;
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
