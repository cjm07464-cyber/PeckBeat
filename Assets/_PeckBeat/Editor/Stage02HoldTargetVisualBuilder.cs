using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage02HoldTargetVisualBuilder
{
    private const string MenuPath = "PeckBeat/Stage02/Build Hold Target Visual";
    private const string HoldRatFolder = "Assets/_PeckBeat/Art/Stage_02/Player/Rat/Hold_Rat/";
    private const string RatFolder = "Assets/_PeckBeat/Art/Stage_02/Player/Rat/";
    private const string ObjectFolder = "Assets/_PeckBeat/Art/Stage_02/Player/Objects/";

    [MenuItem(MenuPath)]
    private static void BuildHoldTargetVisual()
    {
        Stage02SculptTarget target = FindSelectedOrOpenHoldTarget();
        if (target == null)
        {
            Debug.LogWarning(
                "[Stage02HoldTargetVisualBuilder] Select PF_Stage02_HoldTarget in an open scene "
                + "or open it in Prefab Mode before running this menu. Nothing was saved.");
            return;
        }

        if (EditorUtility.IsPersistent(target))
        {
            Debug.LogWarning(
                "[Stage02HoldTargetVisualBuilder] Open PF_Stage02_HoldTarget in Prefab Mode first. "
                + "The builder does not directly save prefab assets.",
                target);
            return;
        }

        if (target.name.IndexOf("Hold", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            Debug.LogWarning(
                "[Stage02HoldTargetVisualBuilder] The selected target does not look like a HoldTarget.",
                target);
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Stage02 Hold Target Visual");

        Transform visualRoot = GetOrCreateChild(target.transform, "HoldVisualRoot", out _);
        Transform runRoot = GetOrCreateChild(visualRoot, "RunRoot", out bool runRootCreated);
        Transform idleRoot = GetOrCreateChild(visualRoot, "IdleRoot", out bool idleRootCreated);

        SpriteRenderer referenceRenderer = GetTargetReferenceRenderer(target);
        Sprite frontRunSprite = LoadSprite(HoldRatFolder, "Run_긴통나무_앞쥐.png");
        Sprite backRunSprite = LoadSprite(HoldRatFolder, "Run_긴통나무_뒷쥐.png");
        Sprite frontIdleSprite = LoadSprite(HoldRatFolder, "Idle_긴통나무_앞쥐.png");
        Sprite backIdleSprite = LoadSprite(HoldRatFolder, "Idle_긴통나무_뒷쥐.png");
        Sprite longLogSprite = LoadSprite(ObjectFolder, "홀드_긴통나무_완전옆면.png")
            ?? LoadSprite(ObjectFolder, "홀드_긴통나무.png");
        Sprite batSprite = LoadSprite(ObjectFolder, "홀드_긴야구방망이.png");
        Sprite ratRunA = LoadSprite(RatFolder, "Rat_Run_A.png");
        Sprite ratRunB = LoadSprite(RatFolder, "Rat_Run_B.png");
        Sprite ratRunC = LoadSprite(RatFolder, "Rat_Run_C.png");

        SpriteRenderer batRun = GetOrCreateRenderer(
            runRoot, "BatRun", batSprite, referenceRenderer, 0);
        SpriteRenderer longLogRun = GetOrCreateRenderer(
            runRoot, "LongLogRun", longLogSprite, referenceRenderer, 1);
        SpriteRenderer backRatRun = GetOrCreateRenderer(
            runRoot, "BackRatRun", backRunSprite, referenceRenderer, 2);
        SpriteRenderer backRatRunLegs = GetOrCreateRenderer(
            runRoot, "BackRatRunLegs", ratRunA, referenceRenderer, 2);
        SpriteRenderer frontRatRun = GetOrCreateRenderer(
            runRoot, "FrontRatRun", frontRunSprite, referenceRenderer, 3);
        SpriteRenderer frontRatRunLegs = GetOrCreateRenderer(
            runRoot, "FrontRatRunLegs", ratRunA, referenceRenderer, 3);

        SpriteRenderer batIdle = GetOrCreateRenderer(
            idleRoot, "BatIdle", batSprite, referenceRenderer, 0);
        SpriteRenderer longLogIdle = GetOrCreateRenderer(
            idleRoot, "LongLogIdle", longLogSprite, referenceRenderer, 1);
        SpriteRenderer backRatIdle = GetOrCreateRenderer(
            idleRoot, "BackRatIdle", backIdleSprite, referenceRenderer, 2);
        SpriteRenderer frontRatIdle = GetOrCreateRenderer(
            idleRoot, "FrontRatIdle", frontIdleSprite, referenceRenderer, 3);

        Stage02HoldTargetVisual holdVisual =
            visualRoot.GetComponent<Stage02HoldTargetVisual>();
        if (holdVisual == null)
        {
            holdVisual = Undo.AddComponent<Stage02HoldTargetVisual>(visualRoot.gameObject);
        }

        SerializedObject visualObject = new SerializedObject(holdVisual);
        SetReferenceIfEmpty(visualObject, "runRoot", runRoot.gameObject);
        SetReferenceIfEmpty(visualObject, "idleRoot", idleRoot.gameObject);
        SetReferenceIfEmpty(visualObject, "frontRatRunRenderer", frontRatRun);
        SetReferenceIfEmpty(visualObject, "backRatRunRenderer", backRatRun);
        SetReferenceIfEmpty(visualObject, "frontRatIdleRenderer", frontRatIdle);
        SetReferenceIfEmpty(visualObject, "backRatIdleRenderer", backRatIdle);
        SetReferenceIfEmpty(visualObject, "frontRunLegsRenderer", frontRatRunLegs);
        SetReferenceIfEmpty(visualObject, "backRunLegsRenderer", backRatRunLegs);
        SetReferenceIfEmpty(visualObject, "longLogRunRenderer", longLogRun);
        SetReferenceIfEmpty(visualObject, "longLogIdleRenderer", longLogIdle);
        SetReferenceIfEmpty(visualObject, "batRunRenderer", batRun);
        SetReferenceIfEmpty(visualObject, "batIdleRenderer", batIdle);
        SetReferenceIfEmpty(visualObject, "frontRatRunSprite", frontRunSprite);
        SetReferenceIfEmpty(visualObject, "backRatRunSprite", backRunSprite);
        SetReferenceIfEmpty(visualObject, "frontRatIdleSprite", frontIdleSprite);
        SetReferenceIfEmpty(visualObject, "backRatIdleSprite", backIdleSprite);
        SetReferenceIfEmpty(visualObject, "longLogSprite", longLogSprite);
        SetReferenceIfEmpty(visualObject, "batSprite", batSprite);
        SetReferenceIfEmpty(visualObject, "ratRunA", ratRunA);
        SetReferenceIfEmpty(visualObject, "ratRunB", ratRunB);
        SetReferenceIfEmpty(visualObject, "ratRunC", ratRunC);
        visualObject.ApplyModifiedProperties();

        SerializedObject targetObject = new SerializedObject(target);
        SetReferenceIfEmpty(targetObject, "holdVisual", holdVisual);
        targetObject.ApplyModifiedProperties();

        if (runRootCreated)
        {
            Undo.RecordObject(runRoot.gameObject, "Show Hold Run Root");
            runRoot.gameObject.SetActive(true);
        }

        if (idleRootCreated)
        {
            Undo.RecordObject(idleRoot.gameObject, "Hide Hold Idle Root");
            idleRoot.gameObject.SetActive(false);
        }

        EditorUtility.SetDirty(holdVisual);
        EditorUtility.SetDirty(target);
        Scene scene = target.gameObject.scene;
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        Selection.activeGameObject = visualRoot.gameObject;
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            "[Stage02HoldTargetVisualBuilder] HoldVisualRoot was created or reused. "
            + "Existing transforms, sprites, and sorting values were preserved. "
            + "The scene or Prefab Stage was marked dirty but was not saved.",
            visualRoot.gameObject);
    }

    private static Stage02SculptTarget FindSelectedOrOpenHoldTarget()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected != null)
        {
            Stage02SculptTarget selectedTarget =
                selected.GetComponentInParent<Stage02SculptTarget>()
                ?? selected.GetComponentInChildren<Stage02SculptTarget>(true);
            if (selectedTarget != null)
            {
                return selectedTarget;
            }
        }

        Scene activeScene = SceneManager.GetActiveScene();
        Stage02SculptTarget[] targets = Object.FindObjectsByType<Stage02SculptTarget>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (Stage02SculptTarget target in targets)
        {
            if (target != null
                && target.gameObject.scene == activeScene
                && target.name.IndexOf("Hold", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return target;
            }
        }

        return null;
    }

    private static SpriteRenderer GetTargetReferenceRenderer(Stage02SculptTarget target)
    {
        SerializedObject targetObject = new SerializedObject(target);
        return targetObject.FindProperty("objectRenderer")?.objectReferenceValue
            as SpriteRenderer;
    }

    private static Transform GetOrCreateChild(
        Transform parent,
        string childName,
        out bool created)
    {
        Transform child = parent.Find(childName);
        created = child == null;
        if (!created)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
        Undo.SetTransformParent(childObject.transform, parent, $"Parent {childName}");
        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        return childObject.transform;
    }

    private static SpriteRenderer GetOrCreateRenderer(
        Transform parent,
        string objectName,
        Sprite sprite,
        SpriteRenderer referenceRenderer,
        int sortingOrder)
    {
        Transform child = GetOrCreateChild(parent, objectName, out bool childCreated);
        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        bool rendererCreated = renderer == null;
        if (rendererCreated)
        {
            renderer = Undo.AddComponent<SpriteRenderer>(child.gameObject);
        }

        if (rendererCreated || childCreated)
        {
            Undo.RecordObject(renderer, $"Configure {objectName} Renderer");
            if (referenceRenderer != null)
            {
                renderer.sharedMaterial = referenceRenderer.sharedMaterial;
                renderer.sortingLayerID = referenceRenderer.sortingLayerID;
            }

            renderer.sortingOrder = sortingOrder;
        }

        if (renderer.sprite == null && sprite != null)
        {
            Undo.RecordObject(renderer, $"Assign {objectName} Sprite");
            renderer.sprite = sprite;
        }

        EditorUtility.SetDirty(renderer);
        return renderer;
    }

    private static Sprite LoadSprite(string folder, string fileName)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(folder + fileName);
        if (sprite == null)
        {
            Debug.LogWarning(
                $"[Stage02HoldTargetVisualBuilder] Sprite was not found: {folder}{fileName}");
        }

        return sprite;
    }

    private static void SetReferenceIfEmpty(
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
}
