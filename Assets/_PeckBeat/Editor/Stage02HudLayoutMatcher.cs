using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage02HudLayoutMatcher
{
    private const string MenuPath = "PeckBeat/Stage02/Match HUD Layout From Stage01";
    private const string Stage01ScenePath = "Assets/Scenes/Stage01_Prototype.unity";
    private const string Stage02ScenePath = "Assets/Scenes/Stage02.unity";

    [MenuItem(MenuPath)]
    private static void MatchLayout()
    {
        Scene stage02 = SceneManager.GetActiveScene();
        if (!stage02.IsValid() || stage02.path != Stage02ScenePath)
        {
            Debug.LogWarning($"[Stage02 HUD] Open {Stage02ScenePath} first.");
            return;
        }

        Scene stage01 = SceneManager.GetSceneByPath(Stage01ScenePath);
        bool closeStage01AfterRead = !stage01.IsValid() || !stage01.isLoaded;
        if (closeStage01AfterRead)
        {
            stage01 = EditorSceneManager.OpenScene(Stage01ScenePath, OpenSceneMode.Additive);
        }

        try
        {
            HudReferences source = HudReferences.Find(stage01);
            HudReferences destination = HudReferences.Find(stage02);
            if (!source.IsComplete || !destination.IsComplete)
            {
                Debug.LogWarning(
                    "[Stage02 HUD] Match cancelled. Both scenes must already contain the existing "
                    + "progress and life HUD objects. Nothing was created or changed.");
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Match Stage02 HUD Layout From Stage01");

            CopyRect(source.ProgressRoot, destination.ProgressRoot);
            CopyRect(source.ProgressBackground.rectTransform, destination.ProgressBackground.rectTransform);
            CopyRect(source.ProgressFill.rectTransform, destination.ProgressFill.rectTransform);
            CopyRect(source.ProgressFrame.rectTransform, destination.ProgressFrame.rectTransform);
            CopyImagePresentation(source.ProgressBackground, destination.ProgressBackground);
            CopyImagePresentation(source.ProgressFill, destination.ProgressFill);
            CopyImagePresentation(source.ProgressFrame, destination.ProgressFrame);

            // StageGoalAndProgressCanvas also owns the left goal counter. Never alter it here.
            bool progressCanvasMatches = CanvasLayoutMatches(
                source.ProgressCanvas,
                destination.ProgressCanvas);

            CopyRect(source.LifeCanvasRect, destination.LifeCanvasRect);
            CopyCanvas(source.LifeCanvas, destination.LifeCanvas);
            CopyCanvasScaler(source.LifeCanvasScaler, destination.LifeCanvasScaler);
            CopyRect(source.LifeRoot, destination.LifeRoot);
            CopyRect(source.HeartImage.rectTransform, destination.HeartImage.rectTransform);
            CopyImagePresentation(source.HeartImage, destination.HeartImage);
            CopyRect(source.LifeText.rectTransform, destination.LifeText.rectTransform);
            CopyTextPresentation(source.LifeText, destination.LifeText);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(stage02);
            Selection.activeGameObject = destination.ProgressRoot.gameObject;

            if (!progressCanvasMatches)
            {
                Debug.LogWarning(
                    "[Stage02 HUD] StageGoalAndProgressCanvas settings differ between scenes. "
                    + "They were deliberately preserved because that canvas also owns the Stage02 "
                    + "left goal counter. Progress child RectTransforms were still matched.",
                    destination.ProgressCanvas);
            }

            Debug.Log(
                "[Stage02 HUD] Matched StageMusicProgressRoot, ProgressBackground/Fill/Frame, "
                + "StageLifeCanvas/Root, HeartImage, and LifeText from the saved Stage01 scene. "
                + "Sprites and runtime references were preserved. Stage01 was not modified and "
                + "Stage02 was marked dirty without saving.",
                destination.ProgressRoot);
        }
        finally
        {
            if (closeStage01AfterRead && stage01.IsValid() && stage01.isLoaded)
            {
                EditorSceneManager.CloseScene(stage01, true);
            }

            if (stage02.IsValid() && stage02.isLoaded)
            {
                SceneManager.SetActiveScene(stage02);
            }
        }
    }

    private static void CopyRect(RectTransform source, RectTransform destination)
    {
        Undo.RecordObject(destination, "Match HUD RectTransform");
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.pivot = source.pivot;
        destination.anchoredPosition3D = source.anchoredPosition3D;
        destination.sizeDelta = source.sizeDelta;
        destination.localScale = source.localScale;
        destination.localRotation = source.localRotation;
        EditorUtility.SetDirty(destination);
    }

    private static void CopyImagePresentation(Image source, Image destination)
    {
        Undo.RecordObject(destination, "Match HUD Image Presentation");
        destination.color = source.color;
        destination.material = source.material;
        destination.raycastTarget = source.raycastTarget;
        destination.maskable = source.maskable;
        destination.preserveAspect = source.preserveAspect;
        destination.type = source.type;
        destination.fillMethod = source.fillMethod;
        destination.fillOrigin = source.fillOrigin;
        destination.fillClockwise = source.fillClockwise;
        destination.fillCenter = source.fillCenter;
        // Keep the Stage02 sprite and live fillAmount/reference intact.
        EditorUtility.SetDirty(destination);
    }

    private static void CopyTextPresentation(TMP_Text source, TMP_Text destination)
    {
        Undo.RecordObject(destination, "Match Life Text Presentation");
        destination.font = source.font;
        destination.fontSharedMaterial = source.fontSharedMaterial;
        destination.fontSize = source.fontSize;
        destination.fontStyle = source.fontStyle;
        destination.alignment = source.alignment;
        destination.color = source.color;
        destination.enableAutoSizing = source.enableAutoSizing;
        destination.fontSizeMin = source.fontSizeMin;
        destination.fontSizeMax = source.fontSizeMax;
        destination.raycastTarget = source.raycastTarget;
        EditorUtility.SetDirty(destination);
    }

    private static void CopyCanvas(Canvas source, Canvas destination)
    {
        Undo.RecordObject(destination, "Match Life Canvas");
        destination.renderMode = source.renderMode;
        destination.pixelPerfect = source.pixelPerfect;
        destination.sortingOrder = source.sortingOrder;
        destination.overrideSorting = source.overrideSorting;
        EditorUtility.SetDirty(destination);
    }

    private static void CopyCanvasScaler(CanvasScaler source, CanvasScaler destination)
    {
        Undo.RecordObject(destination, "Match Life Canvas Scaler");
        destination.uiScaleMode = source.uiScaleMode;
        destination.referencePixelsPerUnit = source.referencePixelsPerUnit;
        destination.scaleFactor = source.scaleFactor;
        destination.referenceResolution = source.referenceResolution;
        destination.screenMatchMode = source.screenMatchMode;
        destination.matchWidthOrHeight = source.matchWidthOrHeight;
        destination.physicalUnit = source.physicalUnit;
        destination.fallbackScreenDPI = source.fallbackScreenDPI;
        destination.defaultSpriteDPI = source.defaultSpriteDPI;
        EditorUtility.SetDirty(destination);
    }

    private static bool CanvasLayoutMatches(Canvas source, Canvas destination)
    {
        CanvasScaler sourceScaler = source.GetComponent<CanvasScaler>();
        CanvasScaler destinationScaler = destination.GetComponent<CanvasScaler>();
        if (sourceScaler == null || destinationScaler == null)
        {
            return sourceScaler == destinationScaler;
        }

        return source.renderMode == destination.renderMode
            && source.pixelPerfect == destination.pixelPerfect
            && sourceScaler.uiScaleMode == destinationScaler.uiScaleMode
            && sourceScaler.referenceResolution == destinationScaler.referenceResolution
            && sourceScaler.screenMatchMode == destinationScaler.screenMatchMode
            && Mathf.Approximately(
                sourceScaler.matchWidthOrHeight,
                destinationScaler.matchWidthOrHeight);
    }

    private sealed class HudReferences
    {
        public Canvas ProgressCanvas;
        public RectTransform ProgressRoot;
        public Image ProgressBackground;
        public Image ProgressFill;
        public Image ProgressFrame;
        public Canvas LifeCanvas;
        public RectTransform LifeCanvasRect;
        public CanvasScaler LifeCanvasScaler;
        public RectTransform LifeRoot;
        public Image HeartImage;
        public TMP_Text LifeText;

        public bool IsComplete =>
            ProgressCanvas != null
            && ProgressRoot != null
            && ProgressBackground != null
            && ProgressFill != null
            && ProgressFrame != null
            && LifeCanvas != null
            && LifeCanvasRect != null
            && LifeCanvasScaler != null
            && LifeRoot != null
            && HeartImage != null
            && LifeText != null;

        public static HudReferences Find(Scene scene)
        {
            RectTransform progressRoot = FindRect(scene, "StageMusicProgressRoot");
            RectTransform lifeRoot = FindRect(scene, "StageLifeRoot");
            RectTransform lifeCanvasRect = FindRect(scene, "StageLifeCanvas");
            return new HudReferences
            {
                ProgressCanvas = FindComponent<Canvas>(scene, "StageGoalAndProgressCanvas"),
                ProgressRoot = progressRoot,
                ProgressBackground = FindChildComponent<Image>(progressRoot, "ProgressBackground"),
                ProgressFill = FindChildComponent<Image>(progressRoot, "ProgressFill"),
                ProgressFrame = FindChildComponent<Image>(progressRoot, "ProgressFrame"),
                LifeCanvas = lifeCanvasRect != null ? lifeCanvasRect.GetComponent<Canvas>() : null,
                LifeCanvasRect = lifeCanvasRect,
                LifeCanvasScaler = lifeCanvasRect != null
                    ? lifeCanvasRect.GetComponent<CanvasScaler>()
                    : null,
                LifeRoot = lifeRoot,
                HeartImage = FindChildComponent<Image>(lifeRoot, "HeartImage"),
                LifeText = FindChildComponent<TMP_Text>(lifeRoot, "LifeText")
            };
        }
    }

    private static RectTransform FindRect(Scene scene, string objectName)
    {
        return FindComponent<RectTransform>(scene, objectName);
    }

    private static T FindComponent<T>(Scene scene, string objectName) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = FindComponentRecursive<T>(root.transform, objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static T FindComponentRecursive<T>(Transform current, string objectName)
        where T : Component
    {
        if (NamesMatch(current.name, objectName))
        {
            T component = current.GetComponent<T>();
            if (component != null)
            {
                return component;
            }
        }

        for (int i = 0; i < current.childCount; i++)
        {
            T result = FindComponentRecursive<T>(current.GetChild(i), objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform parent, string objectName)
        where T : Component
    {
        return parent != null ? FindComponentRecursive<T>(parent, objectName) : null;
    }

    private static bool NamesMatch(string left, string right)
    {
        return string.Equals(left?.Trim(), right, System.StringComparison.OrdinalIgnoreCase);
    }
}
