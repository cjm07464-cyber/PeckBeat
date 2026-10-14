using System;
using System.Linq;
#if false
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TitleIntroDialogueHierarchyBuilder
{
    private const string MenuPath = "PeckBeat/Title/Build Intro Dialogue UI Hierarchy";
    private const string UndoName = "Build Title Intro Dialogue UI";

    [MenuItem(MenuPath)]
    private static void BuildHierarchy()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Title Intro Dialogue",
                "Exit Play Mode before building the hierarchy.",
                "OK");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog(
                "Title Intro Dialogue",
                "No loaded scene is active.",
                "OK");
            return;
        }

        if (!string.Equals(scene.name, "TitleScene", StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Title Intro Dialogue",
                $"Open TitleScene before running this builder.\nCurrent scene: {scene.name}",
                "OK");
            return;
        }

        TitleManager titleManager = FindComponentInScene<TitleManager>(scene);
        if (titleManager == null)
        {
            EditorUtility.DisplayDialog(
                "Title Intro Dialogue",
                "TitleManager was not found in the active TitleScene.",
                "OK");
            return;
        }

        Canvas canvas = titleManager.GetComponentInParent<Canvas>(true);
        if (canvas == null || canvas.gameObject.scene != scene)
        {
            canvas = FindBestCanvas(scene);
        }

        if (canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Title Intro Dialogue",
                "A Canvas was not found in the active TitleScene.",
                "OK");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);

        try
        {
            TitleIntroDialogueController controller =
                FindComponentInScene<TitleIntroDialogueController>(scene);
            if (controller == null)
            {
                controller = Undo.AddComponent<TitleIntroDialogueController>(titleManager.gameObject);
            }

            SerializedObject controllerObject = new SerializedObject(controller);
            RectTransform root = GetPreferredRoot(controllerObject, canvas.transform);
            bool rootCreated = false;
            if (root == null)
            {
                root = GetOrCreateRectTransform(canvas.transform, "IntroDialogueRoot", out rootCreated);
            }

            if (rootCreated)
            {
                SetStretch(root);
            }

            CanvasGroup rootCanvasGroup = GetOrAddComponent<CanvasGroup>(root.gameObject, out _);
            Undo.RecordObject(rootCanvasGroup, UndoName);
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;

            PlaceRootAboveTitleAndBelowCurtains(root, canvas.transform);

            Image darkBackground = BuildImage(
                root,
                "DarkBackground",
                new Color(0.015f, 0.02f, 0.035f, 0.9f),
                SetStretch);

            Image spotlightOverlay = BuildImage(
                root,
                "SpotlightOverlay",
                new Color(1f, 0.94f, 0.72f, 0.35f),
                rect => SetCentered(rect, new Vector2(0f, 90f), new Vector2(900f, 900f)));

            Image ratImage = BuildImage(
                root,
                "RatImage",
                Color.white,
                rect => SetCentered(rect, new Vector2(0f, -120f), new Vector2(520f, 520f)),
                preserveAspect: true);

            Image speechBubbleImage = BuildImage(
                root,
                "SpeechBubbleImage",
                new Color(1f, 1f, 1f, 0.95f),
                rect => SetCentered(rect, new Vector2(0f, 285f), new Vector2(1200f, 260f)));

            TextMeshProUGUI dialogueText = BuildText(
                root,
                "DialogueText",
                string.Empty,
                46f,
                new Color(0.06f, 0.06f, 0.07f, 1f),
                TextAlignmentOptions.Center,
                rect => SetCentered(rect, new Vector2(0f, 285f), new Vector2(1050f, 180f)));

            Image menuButtonIconImage = BuildImage(
                root,
                "MenuButtonIconImage",
                Color.white,
                rect => SetCentered(rect, new Vector2(0f, 205f), new Vector2(80f, 80f)),
                preserveAspect: true,
                deactivateWhenCreated: true);

            TextMeshProUGUI menuButtonFallbackText = BuildText(
                menuButtonIconImage.rectTransform,
                "MenuButtonFallbackText",
                "[Esc]",
                26f,
                Color.black,
                TextAlignmentOptions.Center,
                SetStretch);

            RectTransform choiceRoot = GetOrCreateRectTransform(root, "ChoiceRoot", out bool choiceCreated);
            if (choiceCreated)
            {
                SetCentered(choiceRoot, new Vector2(0f, -390f), new Vector2(1000f, 220f));
            }

            CanvasGroup choiceCanvasGroup = GetOrAddComponent<CanvasGroup>(
                choiceRoot.gameObject,
                out _);
            Undo.RecordObject(choiceCanvasGroup, UndoName);
            choiceCanvasGroup.alpha = 1f;
            choiceCanvasGroup.interactable = false;
            choiceCanvasGroup.blocksRaycasts = false;

            RectTransform yesGroup = GetOrCreateRectTransform(choiceRoot, "YesGroup", out bool yesGroupCreated);
            if (yesGroupCreated)
            {
                SetCentered(yesGroup, new Vector2(-250f, 0f), new Vector2(360f, 220f));
            }

            TextMeshProUGUI yesText = BuildText(
                yesGroup,
                "YesText",
                "예",
                60f,
                Color.white,
                TextAlignmentOptions.Center,
                rect => SetCentered(rect, new Vector2(0f, 48f), new Vector2(320f, 80f)));

            Image yesButtonImage = BuildImage(
                yesGroup,
                "YesButtonImage",
                Color.white,
                rect => SetCentered(rect, new Vector2(0f, -55f), new Vector2(110f, 110f)),
                preserveAspect: true);

            TextMeshProUGUI yesButtonFallbackText = BuildText(
                yesButtonImage.rectTransform,
                "YesButtonFallbackText",
                "D",
                36f,
                Color.black,
                TextAlignmentOptions.Center,
                SetStretch);

            RectTransform noGroup = GetOrCreateRectTransform(choiceRoot, "NoGroup", out bool noGroupCreated);
            if (noGroupCreated)
            {
                SetCentered(noGroup, new Vector2(250f, 0f), new Vector2(360f, 220f));
            }

            TextMeshProUGUI noText = BuildText(
                noGroup,
                "NoText",
                "아니오",
                60f,
                Color.white,
                TextAlignmentOptions.Center,
                rect => SetCentered(rect, new Vector2(0f, 48f), new Vector2(320f, 80f)));

            Image noButtonImage = BuildImage(
                noGroup,
                "NoButtonImage",
                Color.white,
                rect => SetCentered(rect, new Vector2(0f, -55f), new Vector2(110f, 110f)),
                preserveAspect: true);

            TextMeshProUGUI noButtonFallbackText = BuildText(
                noButtonImage.rectTransform,
                "NoButtonFallbackText",
                "J",
                36f,
                Color.black,
                TextAlignmentOptions.Center,
                SetStretch);

            if (choiceCreated)
            {
                Undo.RecordObject(choiceRoot.gameObject, UndoName);
                choiceRoot.gameObject.SetActive(false);
            }

            controllerObject.Update();
            AssignIfEmpty(controllerObject, "introDialogueRoot", root);
            AssignIfEmpty(controllerObject, "introDialogueRootCanvasGroup", rootCanvasGroup);
            AssignIfEmpty(controllerObject, "darkBackground", darkBackground);
            AssignIfEmpty(controllerObject, "spotlightOverlay", spotlightOverlay);
            AssignIfEmpty(controllerObject, "ratImage", ratImage);
            AssignIfEmpty(controllerObject, "speechBubbleImage", speechBubbleImage);
            AssignIfEmpty(controllerObject, "dialogueText", dialogueText);
            AssignIfEmpty(controllerObject, "menuButtonIconImage", menuButtonIconImage);
            AssignIfEmpty(controllerObject, "menuButtonFallbackText", menuButtonFallbackText);
            AssignIfEmpty(controllerObject, "choiceRoot", choiceRoot.gameObject);
            AssignIfEmpty(controllerObject, "yesText", yesText);
            AssignIfEmpty(controllerObject, "yesButtonImage", yesButtonImage);
            AssignIfEmpty(controllerObject, "yesButtonFallbackText", yesButtonFallbackText);
            AssignIfEmpty(controllerObject, "noText", noText);
            AssignIfEmpty(controllerObject, "noButtonImage", noButtonImage);
            AssignIfEmpty(controllerObject, "noButtonFallbackText", noButtonFallbackText);
            controllerObject.ApplyModifiedProperties();

            SerializedObject titleManagerObject = new SerializedObject(titleManager);
            titleManagerObject.Update();
            AssignIfEmpty(titleManagerObject, "introDialogueController", controller);
            titleManagerObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(titleManager);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root.gameObject;
            EditorGUIUtility.PingObject(root.gameObject);

            Debug.Log(
                "Title Intro Dialogue hierarchy is ready. Assign rat, button, font, and optional audio assets in the Inspector. The scene was not saved automatically.",
                root.gameObject);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "Title Intro Dialogue",
                "The hierarchy could not be completed. Check the Console for details.",
                "OK");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateBuildHierarchy()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .FirstOrDefault(component =>
                component != null &&
                !EditorUtility.IsPersistent(component) &&
                component.gameObject.scene == scene);
    }

    private static Canvas FindBestCanvas(Scene scene)
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>()
            .Where(candidate =>
                candidate != null &&
                !EditorUtility.IsPersistent(candidate) &&
                candidate.gameObject.scene == scene)
            .ToArray();

        return canvases.FirstOrDefault(candidate =>
                   candidate.isRootCanvas &&
                   string.Equals(candidate.name, "Canvas", StringComparison.OrdinalIgnoreCase))
               ?? canvases.FirstOrDefault(candidate => candidate.isRootCanvas)
               ?? canvases.FirstOrDefault();
    }

    private static RectTransform GetPreferredRoot(SerializedObject controllerObject, Transform canvas)
    {
        SerializedProperty rootProperty = controllerObject.FindProperty("introDialogueRoot");
        RectTransform assignedRoot = rootProperty?.objectReferenceValue as RectTransform;
        if (assignedRoot != null && assignedRoot.gameObject.scene == canvas.gameObject.scene)
        {
            return assignedRoot;
        }

        Transform directChild = canvas.Find("IntroDialogueRoot");
        if (directChild != null)
        {
            return RequireRectTransform(directChild, "IntroDialogueRoot");
        }

        RectTransform nestedRoot = canvas.GetComponentsInChildren<RectTransform>(true)
            .FirstOrDefault(rect => rect != canvas && rect.name == "IntroDialogueRoot");
        return nestedRoot;
    }

    private static RectTransform GetOrCreateRectTransform(
        Transform parent,
        string objectName,
        out bool created)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            created = false;
            return RequireRectTransform(existing, objectName);
        }

        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {objectName}");
        Undo.SetTransformParent(gameObject.transform, parent, $"Parent {objectName}");
        gameObject.layer = parent.gameObject.layer;
        created = true;
        return gameObject.GetComponent<RectTransform>();
    }

    private static RectTransform RequireRectTransform(Transform target, string objectName)
    {
        RectTransform rectTransform = target as RectTransform;
        if (rectTransform == null)
        {
            throw new InvalidOperationException(
                $"{objectName} exists but does not have a RectTransform.");
        }

        return rectTransform;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject, out bool added)
        where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component != null)
        {
            added = false;
            return component;
        }

        added = true;
        return Undo.AddComponent<T>(gameObject);
    }

    private static Image BuildImage(
        RectTransform parent,
        string objectName,
        Color color,
        Action<RectTransform> applyLayout,
        bool preserveAspect = false,
        bool deactivateWhenCreated = false)
    {
        RectTransform rect = GetOrCreateRectTransform(parent, objectName, out bool created);
        Image image = GetOrAddComponent<Image>(rect.gameObject, out bool componentAdded);

        if (created)
        {
            applyLayout(rect);
        }

        if (created || componentAdded)
        {
            Undo.RecordObject(image, UndoName);
            image.color = color;
            image.preserveAspect = preserveAspect;
        }

        Undo.RecordObject(image, UndoName);
        image.raycastTarget = false;
        if (preserveAspect)
        {
            image.preserveAspect = true;
        }

        if (created && deactivateWhenCreated)
        {
            Undo.RecordObject(rect.gameObject, UndoName);
            rect.gameObject.SetActive(false);
        }

        return image;
    }

    private static TextMeshProUGUI BuildText(
        RectTransform parent,
        string objectName,
        string defaultText,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Action<RectTransform> applyLayout)
    {
        RectTransform rect = GetOrCreateRectTransform(parent, objectName, out bool created);
        TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(
            rect.gameObject,
            out bool componentAdded);

        if (created)
        {
            applyLayout(rect);
        }

        if (created || componentAdded)
        {
            Undo.RecordObject(text, UndoName);
            text.text = defaultText;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
        }

        Undo.RecordObject(text, UndoName);
        text.raycastTarget = false;

        return text;
    }

    private static void AssignIfEmpty(
        SerializedObject serializedObject,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning(
                $"Serialized field '{propertyName}' was not found on {serializedObject.targetObject.name}.",
                serializedObject.targetObject);
            return;
        }

        if (property.objectReferenceValue == null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void PlaceRootAboveTitleAndBelowCurtains(RectTransform root, Transform canvas)
    {
        Transform[] siblings = Enumerable.Range(0, canvas.childCount)
            .Select(canvas.GetChild)
            .Where(child => child != root)
            .ToArray();

        int firstCurtainIndex = siblings
            .Where(child => child.name.IndexOf("Curtain", StringComparison.OrdinalIgnoreCase) >= 0)
            .Select(child => child.GetSiblingIndex())
            .DefaultIfEmpty(-1)
            .Min();

        Undo.RecordObject(root, UndoName);
        if (firstCurtainIndex >= 0)
        {
            root.SetSiblingIndex(firstCurtainIndex);
        }
        else
        {
            root.SetAsLastSibling();
        }
    }

    private static void SetStretch(RectTransform rect)
    {
        Undo.RecordObject(rect, UndoName);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
    {
        Undo.RecordObject(rect, UndoName);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}
#endif

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TitleIntroDialogueHierarchyBuilderV2
{
    [MenuItem("PeckBeat/Title/Build Intro Dialogue UI Hierarchy")]
    private static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[PeckBeat] Exit Play Mode before building the intro dialogue UI.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        TitleManager titleManager = FindInScene<TitleManager>(scene);
        Canvas canvas = titleManager != null ? titleManager.GetComponentInParent<Canvas>(true) : null;
        if (canvas == null)
        {
            canvas = FindInScene<Canvas>(scene);
        }
        if (canvas == null)
        {
            Debug.LogError("[PeckBeat] A Canvas is required in the open TitleScene.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Title Intro Dialogue UI");

        TitleIntroDialogueController controller = FindInScene<TitleIntroDialogueController>(scene);
        if (controller == null)
        {
            controller = Undo.AddComponent<TitleIntroDialogueController>(
                titleManager != null ? titleManager.gameObject : canvas.gameObject);
        }

        RectTransform root = GetRect(canvas.transform, "IntroDialogueRoot", out _);
        Undo.RecordObject(root, "Normalize Intro Dialogue Root");
        FullStretch(root);
        root.localScale = Vector3.one;
        root.localRotation = Quaternion.identity;

        CanvasGroup rootGroup = GetComponent<CanvasGroup>(root.gameObject);
        Undo.RecordObject(rootGroup, "Configure Intro Dialogue Root");
        rootGroup.alpha = 0f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;

        Image black = GetImage(root, "IntroBlackBackground", out bool blackNew);
        if (blackNew)
        {
            FullStretch(black.rectTransform);
            black.color = Color.black;
        }
        black.raycastTarget = false;

        Image floor = GetImage(root, "RatFloorImage", out bool floorNew);
        if (floorNew)
        {
            Center(floor.rectTransform, new Vector2(700f, 210f), new Vector2(0f, -315f));
            floor.preserveAspect = true;
            floor.color = Color.white;
        }
        floor.raycastTarget = false;

        Image rat = GetImage(root, "RatImage", out bool ratNew);
        if (ratNew)
        {
            Center(rat.rectTransform, new Vector2(520f, 520f), new Vector2(0f, -105f));
            rat.preserveAspect = true;
        }
        rat.raycastTarget = false;

        Image bubble = GetImage(root, "SpeechBubbleImage", out bool bubbleNew);
        if (bubbleNew)
        {
            Center(bubble.rectTransform, new Vector2(1200f, 260f), new Vector2(0f, 245f));
            bubble.color = new Color(1f, 1f, 1f, 0.95f);
        }
        bubble.raycastTarget = false;

        TextMeshProUGUI dialogue = GetText(root, "DialogueText", out bool dialogueNew);
        if (dialogueNew)
        {
            Center(dialogue.rectTransform, new Vector2(1050f, 180f), new Vector2(0f, 245f));
            dialogue.alignment = TextAlignmentOptions.Center;
            dialogue.fontSize = 46f;
            dialogue.color = Color.black;
            dialogue.text = string.Empty;
        }
        dialogue.raycastTarget = false;

        Image menuIcon = GetImage(root, "MenuButtonIconImage", out bool menuNew);
        if (menuNew)
        {
            Center(menuIcon.rectTransform, new Vector2(80f, 80f), new Vector2(0f, 165f));
            menuIcon.preserveAspect = true;
            menuIcon.gameObject.SetActive(false);
        }
        menuIcon.raycastTarget = false;

        RectTransform choiceRoot = GetRect(root, "ChoiceRoot", out bool choiceNew);
        if (choiceNew)
        {
            Center(choiceRoot, new Vector2(1000f, 220f), new Vector2(0f, -390f));
        }
        CanvasGroup choiceGroup = GetComponent<CanvasGroup>(choiceRoot.gameObject);
        choiceGroup.alpha = 0f;
        choiceGroup.interactable = false;
        choiceGroup.blocksRaycasts = false;

        RectTransform yesGroup = GetRect(choiceRoot, "YesGroup", out bool yesGroupNew);
        if (yesGroupNew)
        {
            Center(yesGroup, new Vector2(420f, 220f), new Vector2(-250f, 0f));
        }
        TextMeshProUGUI yesText = GetText(yesGroup, "YesText", out bool yesTextNew);
        if (yesTextNew)
        {
            Center(yesText.rectTransform, new Vector2(340f, 90f), new Vector2(0f, 55f));
            yesText.text = "예";
            yesText.fontSize = 58f;
            yesText.alignment = TextAlignmentOptions.Center;
        }
        yesText.raycastTarget = false;
        Image yesButton = GetImage(yesGroup, "YesButtonImage", out bool yesButtonNew);
        if (yesButtonNew)
        {
            Center(yesButton.rectTransform, new Vector2(110f, 110f), new Vector2(0f, -50f));
            yesButton.preserveAspect = true;
        }
        yesButton.raycastTarget = false;

        RectTransform noGroup = GetRect(choiceRoot, "NoGroup", out bool noGroupNew);
        if (noGroupNew)
        {
            Center(noGroup, new Vector2(420f, 220f), new Vector2(250f, 0f));
        }
        TextMeshProUGUI noText = GetText(noGroup, "NoText", out bool noTextNew);
        if (noTextNew)
        {
            Center(noText.rectTransform, new Vector2(340f, 90f), new Vector2(0f, 55f));
            noText.text = "아니오";
            noText.fontSize = 58f;
            noText.alignment = TextAlignmentOptions.Center;
        }
        noText.raycastTarget = false;
        Image noButton = GetImage(noGroup, "NoButtonImage", out bool noButtonNew);
        if (noButtonNew)
        {
            Center(noButton.rectTransform, new Vector2(110f, 110f), new Vector2(0f, -50f));
            noButton.preserveAspect = true;
        }
        noButton.raycastTarget = false;

        DisableNamed(root, "DarkBackground");
        DisableNamed(root, "SpotlightOverlay");
        DisableNamed(root, "MenuButtonFallbackText");

        black.rectTransform.SetSiblingIndex(0);
        floor.rectTransform.SetSiblingIndex(1);
        rat.rectTransform.SetSiblingIndex(2);
        bubble.rectTransform.SetSiblingIndex(3);
        dialogue.rectTransform.SetSiblingIndex(4);
        menuIcon.rectTransform.SetSiblingIndex(5);
        choiceRoot.SetSiblingIndex(6);
        PlaceBehindTitleRoot(root, canvas.transform);

        SerializedObject controllerData = new SerializedObject(controller);
        AssignIfEmpty(controllerData, "introDialogueRoot", root);
        AssignIfEmpty(controllerData, "rootCanvasGroup", rootGroup);
        AssignIfEmpty(controllerData, "introBlackBackground", black);
        AssignIfEmpty(controllerData, "ratFloorImage", floor);
        AssignIfEmpty(controllerData, "ratImage", rat);
        AssignIfEmpty(controllerData, "speechBubbleImage", bubble);
        AssignIfEmpty(controllerData, "dialogueText", dialogue);
        AssignIfEmpty(controllerData, "menuButtonIconImage", menuIcon);
        AssignIfEmpty(controllerData, "choiceRoot", choiceGroup);
        AssignIfEmpty(controllerData, "yesText", yesText);
        AssignIfEmpty(controllerData, "yesButtonImage", yesButton);
        AssignIfEmpty(controllerData, "noText", noText);
        AssignIfEmpty(controllerData, "noButtonImage", noButton);
        controllerData.ApplyModifiedProperties();

        if (titleManager != null)
        {
            SerializedObject managerData = new SerializedObject(titleManager);
            AssignIfEmpty(managerData, "introDialogueController", controller);
            managerData.ApplyModifiedProperties();
            EditorUtility.SetDirty(titleManager);
        }

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root.gameObject;
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("[PeckBeat] Intro dialogue hierarchy updated. Review it and save the scene manually.", root);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (T item in Resources.FindObjectsOfTypeAll<T>())
        {
            if (item != null && item.gameObject.scene == scene)
            {
                return item;
            }
        }
        return null;
    }

    private static RectTransform GetRect(Transform parent, string name, out bool created)
    {
        Transform found = parent.Find(name);
        if (found is RectTransform existing)
        {
            created = false;
            return existing;
        }
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        RectTransform rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        created = true;
        return rect;
    }

    private static Image GetImage(Transform parent, string name, out bool created)
    {
        RectTransform rect = GetRect(parent, name, out created);
        Image image = rect.GetComponent<Image>();
        return image != null ? image : Undo.AddComponent<Image>(rect.gameObject);
    }

    private static TextMeshProUGUI GetText(Transform parent, string name, out bool created)
    {
        RectTransform rect = GetRect(parent, name, out created);
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        return text != null ? text : Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
    }

    private static T GetComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(gameObject);
    }

    private static void FullStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    private static void DisableNamed(RectTransform root, string name)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            if (item.name == name)
            {
                Undo.RecordObject(item.gameObject, "Disable Legacy Intro UI");
                item.gameObject.SetActive(false);
            }
        }
    }

    private static void PlaceBehindTitleRoot(RectTransform root, Transform canvas)
    {
        Transform titleRoot = canvas.Find("TitleRoot");
        if (titleRoot != null)
        {
            int titleIndex = titleRoot.GetSiblingIndex();
            if (root.GetSiblingIndex() < titleIndex)
            {
                titleIndex--;
            }
            root.SetSiblingIndex(titleIndex);
            return;
        }
        Transform stageTitle = canvas.Find("Stage01_Title");
        root.SetSiblingIndex(stageTitle != null ? stageTitle.GetSiblingIndex() + 1 : 0);
    }

    private static void AssignIfEmpty(SerializedObject data, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = data.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.ObjectReference &&
            property.objectReferenceValue == null)
        {
            property.objectReferenceValue = value;
        }
    }
}
#if false
#endif
