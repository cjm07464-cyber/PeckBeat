using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage02RatVisualBuilder
{
    private const string MenuPath = "PeckBeat/Stage02/Build Rat Visuals";
    private const string SpriteFolder = "Assets/_PeckBeat/Art/Stage_02/Player/Rat/";
    private const string ObjectSpriteFolder = "Assets/_PeckBeat/Art/Stage_02/Player/Objects/";
    private static readonly Vector3 DefaultIdleLogPosition = new Vector3(0.8f, 3.5f, 0f);
    private static readonly Vector3 DefaultIdleLogScale = Vector3.one * 0.45f;
    private const int DefaultIdleLogSortingOrder = -2;

    [MenuItem(MenuPath)]
    private static void BuildRatVisuals()
    {
        List<Stage02SculptTarget> targets = FindEditableTargets();
        if (targets.Count == 0)
        {
            Debug.LogWarning(
                "[Stage02RatVisualBuilder] No editable Stage02SculptTarget was found. "
                + "Open PF_Stage02_NormalTarget or PF_Stage02_HoldTarget in Prefab Mode, "
                + "select its root, and run the menu again. The prefab will not be saved automatically.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Stage02 Rat Visuals");

        GameObject lastBuiltRoot = null;
        HashSet<Scene> dirtyScenes = new HashSet<Scene>();
        foreach (Stage02SculptTarget target in targets)
        {
            if (target == null || !target.gameObject.scene.IsValid())
            {
                continue;
            }

            GameObject builtRoot = BuildTarget(target);
            if (builtRoot == null)
            {
                continue;
            }

            lastBuiltRoot = builtRoot;
            dirtyScenes.Add(target.gameObject.scene);
        }

        foreach (Scene scene in dirtyScenes)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        if (lastBuiltRoot != null)
        {
            Selection.activeGameObject = lastBuiltRoot;
            Debug.Log(
                "[Stage02RatVisualBuilder] Rat Visual hierarchy was created or reused. "
                + "The edited scene or Prefab Stage was marked dirty but was not saved.",
                lastBuiltRoot);
        }

        Undo.CollapseUndoOperations(undoGroup);
    }

    private static List<Stage02SculptTarget> FindEditableTargets()
    {
        List<Stage02SculptTarget> targets = new List<Stage02SculptTarget>();
        GameObject selected = Selection.activeGameObject;
        if (selected != null && selected.scene.IsValid())
        {
            Stage02SculptTarget selectedTarget = selected.GetComponentInParent<Stage02SculptTarget>();
            if (selectedTarget == null)
            {
                selectedTarget = selected.GetComponentInChildren<Stage02SculptTarget>(true);
            }

            if (selectedTarget != null)
            {
                targets.Add(selectedTarget);
                return targets;
            }
        }

        Scene activeScene = SceneManager.GetActiveScene();
        Stage02SculptTarget[] sceneTargets = Object.FindObjectsByType<Stage02SculptTarget>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (Stage02SculptTarget target in sceneTargets)
        {
            if (target != null && target.gameObject.scene == activeScene)
            {
                targets.Add(target);
            }
        }

        return targets;
    }

    private static GameObject BuildTarget(Stage02SculptTarget target)
    {
        Transform carrier = target.transform.Find("Carrier");
        if (carrier == null)
        {
            Debug.LogWarning(
                $"[Stage02RatVisualBuilder] Carrier was not found under {target.name}.",
                target);
            return null;
        }

        Transform ratVisualRoot = GetOrCreateChild(carrier, "RatVisualRoot", out bool ratVisualRootCreated);
        if (ratVisualRootCreated)
        {
            Undo.RecordObject(ratVisualRoot, "Set Rat Visual Root Scale");
            ratVisualRoot.localScale = Vector3.one * 0.24f;
        }

        Transform runRoot = GetOrCreateChild(ratVisualRoot, "RunRoot");
        Transform idleRoot = GetOrCreateChild(ratVisualRoot, "IdleRoot");

        SerializedObject targetObject = new SerializedObject(target);
        bool isNormalTarget = targetObject.FindProperty("signNumberRenderer")?.objectReferenceValue != null;
        SpriteRenderer carrierRenderer = carrier.GetComponent<SpriteRenderer>();
        int baseSortingOrder = carrierRenderer != null ? carrierRenderer.sortingOrder : 0;

        SpriteRenderer legacyRunUpperBodyRenderer = PreserveLegacyRunUpperBody(runRoot);
        Sprite legacyRunUpperBodySprite = LoadSprite("쥐_달리기_상체만.png");
        int runBaseSortingOrder = legacyRunUpperBodyRenderer != null
            ? legacyRunUpperBodyRenderer.sortingOrder
            : baseSortingOrder - 1;
        SpriteRenderer runBaseRenderer = ConfigureRenderer(
            runRoot,
            "RunBase",
            "달리는쥐_귀_팔_분리.png",
            carrierRenderer,
            runBaseSortingOrder,
            out Sprite runBaseSprite);
        SpriteRenderer runLegsRenderer = ConfigureRenderer(
            runRoot,
            "RunLegs",
            "Rat_Run_A.png",
            carrierRenderer,
            baseSortingOrder - 2,
            out Sprite runLegsA);
        SpriteRenderer runLogRenderer = null;
        Sprite baseLogSprite = null;
        if (isNormalTarget)
        {
            runLogRenderer = ConfigureObjectLog(
                runRoot,
                "RunLog",
                new Vector3(0.8f, 3.5f, 0f),
                Vector3.one * 0.45f,
                4,
                carrierRenderer,
                out baseLogSprite);
        }

        int runOverlaySortingOrder = runLogRenderer != null
            ? runLogRenderer.sortingOrder + 1
            : runBaseSortingOrder + 1;
        SpriteRenderer runSeparatedEarRenderer = ConfigureRenderer(
            runRoot,
            "RunSeparatedEar",
            "달리기쥐_귀.png",
            carrierRenderer,
            runOverlaySortingOrder,
            out Sprite runSeparatedEarSprite);
        SpriteRenderer legacyRunSeparatedArmLeftRenderer = PreserveLegacyRunSeparatedArmLeft(runRoot);
        SpriteRenderer runSeparatedArmRightRenderer = ConfigureMirroredRenderer(
            runRoot,
            "RunSeparatedArmRight",
            "달리쥐_왼팔.png",
            carrierRenderer,
            runOverlaySortingOrder,
            out Sprite runSeparatedArmSprite);

        SpriteRenderer legacyIdleRenderer = PreserveLegacyIdle(idleRoot);
        Sprite legacyIdleSprite = LoadSprite("쥐_Idle.png");
        SpriteRenderer idleBaseRenderer = ConfigureRenderer(
            idleRoot,
            "IdleBase",
            "쥐_Idle_오른쪽귀팔제거.png",
            carrierRenderer,
            -1,
            out Sprite idleBaseSprite);
        SpriteRenderer idleLogRenderer = ConfigureObjectLog(
            idleRoot,
            "IdleLog",
            DefaultIdleLogPosition,
            DefaultIdleLogScale,
            DefaultIdleLogSortingOrder,
            carrierRenderer,
            out _);
        SpriteRenderer idleSeparatedEarRenderer = ConfigureRenderer(
            idleRoot,
            "IdleSeparatedEar",
            "02_쥐_달리기_귀_분리.png",
            carrierRenderer,
            2,
            out Sprite idleSeparatedEarSprite);
        SpriteRenderer idleSeparatedArmRenderer = ConfigureRenderer(
            idleRoot,
            "IdleSeparatedArm",
            "02_쥐_달리기_팔_분리.png",
            carrierRenderer,
            3,
            out Sprite idleSeparatedArmSprite);

        Sprite runLegsB = LoadSprite("Rat_Run_B.png");
        Sprite runLegsC = LoadSprite("Rat_Run_C.png");
        bool allSpritesAssigned = runBaseSprite != null
            && runSeparatedEarSprite != null
            && runSeparatedArmSprite != null
            && runLegsA != null
            && runLegsB != null
            && runLegsC != null
            && idleBaseSprite != null
            && idleSeparatedEarSprite != null
            && idleSeparatedArmSprite != null;

        Stage02RatVisualController ratVisual =
            ratVisualRoot.GetComponent<Stage02RatVisualController>();
        if (ratVisual == null)
        {
            ratVisual = Undo.AddComponent<Stage02RatVisualController>(ratVisualRoot.gameObject);
        }

        SerializedObject ratVisualObject = new SerializedObject(ratVisual);
        SetObjectReference(ratVisualObject, "runRoot", runRoot.gameObject);
        SetObjectReference(ratVisualObject, "idleRoot", idleRoot.gameObject);
        SetObjectReference(ratVisualObject, "runBaseRenderer", runBaseRenderer);
        SetObjectReference(ratVisualObject, "runSeparatedEarRenderer", runSeparatedEarRenderer);
        SetObjectReference(ratVisualObject, "runSeparatedArmLeftRenderer", legacyRunSeparatedArmLeftRenderer);
        SetObjectReference(ratVisualObject, "runSeparatedArmRightRenderer", runSeparatedArmRightRenderer);
        SetObjectReference(ratVisualObject, "runLegsRenderer", runLegsRenderer);
        SetObjectReference(ratVisualObject, "idleBaseRenderer", idleBaseRenderer);
        SetObjectReference(ratVisualObject, "idleSeparatedEarRenderer", idleSeparatedEarRenderer);
        SetObjectReference(ratVisualObject, "idleSeparatedArmRenderer", idleSeparatedArmRenderer);
        SetObjectReference(ratVisualObject, "idleRenderer", legacyIdleRenderer);
        SetObjectReference(ratVisualObject, "runUpperBodyRenderer", legacyRunUpperBodyRenderer);
        SetObjectReference(ratVisualObject, "runBaseSprite", runBaseSprite);
        SetObjectReference(ratVisualObject, "runSeparatedEarSprite", runSeparatedEarSprite);
        SetObjectReference(ratVisualObject, "runSeparatedArmSprite", runSeparatedArmSprite);
        SetObjectReference(ratVisualObject, "runLegsA", runLegsA);
        SetObjectReference(ratVisualObject, "runLegsB", runLegsB);
        SetObjectReference(ratVisualObject, "runLegsC", runLegsC);
        SetObjectReference(ratVisualObject, "idleBaseSprite", idleBaseSprite);
        SetObjectReference(ratVisualObject, "idleSeparatedEarSprite", idleSeparatedEarSprite);
        SetObjectReference(ratVisualObject, "idleSeparatedArmSprite", idleSeparatedArmSprite);
        SetObjectReference(ratVisualObject, "runUpperBodySprite", legacyRunUpperBodySprite);
        SetObjectReference(ratVisualObject, "idleSprite", legacyIdleSprite);
        ratVisualObject.ApplyModifiedProperties();

        SpriteRenderer legacyObjectRenderer = targetObject.FindProperty("objectRenderer")?.objectReferenceValue
            as SpriteRenderer;

        Stage02SculptObjectVisual sculptObjectVisual = null;
        if (isNormalTarget)
        {
            sculptObjectVisual = target.GetComponent<Stage02SculptObjectVisual>();
            if (sculptObjectVisual == null)
            {
                sculptObjectVisual = Undo.AddComponent<Stage02SculptObjectVisual>(target.gameObject);
            }

            SerializedObject sculptObject = new SerializedObject(sculptObjectVisual);
            SetObjectReference(sculptObject, "runLogRenderer", runLogRenderer);
            SetObjectReference(sculptObject, "idleLogRenderer", idleLogRenderer);
            SetObjectReference(sculptObject, "legacyObjectRenderer", legacyObjectRenderer);
            SetObjectReference(sculptObject, "oneHitBaseLog", baseLogSprite);
            SetObjectReference(sculptObject, "twoHitBaseLog", baseLogSprite);
            InitializeVisualSettingIfNeeded(
                sculptObject,
                "oneHitBaseLogSetting",
                "oneHitBaseLog",
                runLogRenderer,
                idleLogRenderer);
            InitializeVisualSettingIfNeeded(
                sculptObject,
                "oneHitCompletedSculptureSetting",
                "oneHitCompletedSculpture",
                runLogRenderer,
                idleLogRenderer);
            InitializeVisualSettingIfNeeded(
                sculptObject,
                "twoHitBaseLogSetting",
                "twoHitBaseLog",
                runLogRenderer,
                idleLogRenderer);
            InitializeVisualSettingIfNeeded(
                sculptObject,
                "twoHitDamagedLogSetting",
                "twoHitDamagedLog",
                runLogRenderer,
                idleLogRenderer);
            InitializeVisualSettingIfNeeded(
                sculptObject,
                "twoHitCompletedSculptureSetting",
                "twoHitCompletedSculpture",
                runLogRenderer,
                idleLogRenderer);
            sculptObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(sculptObjectVisual);
        }

        SetObjectReference(targetObject, "ratVisual", ratVisual);
        if (isNormalTarget)
        {
            SetObjectReference(targetObject, "sculptObjectVisual", sculptObjectVisual);
        }

        targetObject.ApplyModifiedProperties();

        Undo.RecordObject(runRoot.gameObject, "Set Rat Run Root Visibility");
        Undo.RecordObject(idleRoot.gameObject, "Set Rat Idle Root Visibility");
        runRoot.gameObject.SetActive(allSpritesAssigned);
        idleRoot.gameObject.SetActive(false);

        if (carrierRenderer != null)
        {
            Undo.RecordObject(carrierRenderer, "Set Legacy Carrier Visibility");
            carrierRenderer.enabled = !allSpritesAssigned;
            EditorUtility.SetDirty(carrierRenderer);
        }

        EditorUtility.SetDirty(ratVisual);
        EditorUtility.SetDirty(target);

        if (!allSpritesAssigned)
        {
            Debug.LogWarning(
                $"[Stage02RatVisualBuilder] One or more Rat Sprites were not found for {target.name}. "
                + "The legacy Carrier renderer was kept enabled.",
                target);
        }

        return ratVisualRoot.gameObject;
    }

    private static SpriteRenderer ConfigureObjectLog(
        Transform parent,
        string objectName,
        Vector3 defaultPosition,
        Vector3 defaultScale,
        int defaultSortingOrder,
        SpriteRenderer referenceRenderer,
        out Sprite sprite)
    {
        Transform log = GetOrCreateChild(parent, objectName, out bool childCreated);
        SpriteRenderer renderer = log.GetComponent<SpriteRenderer>();
        bool rendererCreated = renderer == null;
        if (rendererCreated)
        {
            renderer = Undo.AddComponent<SpriteRenderer>(log.gameObject);
        }

        sprite = LoadSprite(ObjectSpriteFolder, "일반통나무.png");
        Undo.RecordObject(renderer, $"Configure {objectName}");
        if (sprite != null)
        {
            renderer.sprite = sprite;
        }

        if (childCreated)
        {
            Undo.RecordObject(log, $"Set {objectName} Default Transform");
            log.localPosition = defaultPosition;
            log.localRotation = Quaternion.identity;
            log.localScale = defaultScale;
        }

        if (childCreated || rendererCreated)
        {
            if (referenceRenderer != null)
            {
                renderer.sharedMaterial = referenceRenderer.sharedMaterial;
                renderer.sortingLayerID = referenceRenderer.sortingLayerID;
            }

            renderer.sortingOrder = defaultSortingOrder;
        }

        if (!log.gameObject.activeSelf)
        {
            Undo.RecordObject(log.gameObject, $"Enable {objectName}");
            log.gameObject.SetActive(true);
        }

        renderer.enabled = sprite != null;
        EditorUtility.SetDirty(renderer);
        return renderer;
    }

    private static SpriteRenderer PreserveLegacyIdle(Transform idleRoot)
    {
        Transform legacyIdle = idleRoot.Find("LegacyIdle");
        Transform existingIdle = idleRoot.Find("Idle");
        if (existingIdle != null)
        {
            if (legacyIdle == null)
            {
                Undo.RecordObject(existingIdle.gameObject, "Preserve Legacy Rat Idle");
                existingIdle.name = "LegacyIdle";
                legacyIdle = existingIdle;
            }
            else
            {
                Undo.RecordObject(existingIdle.gameObject, "Disable Legacy Rat Idle");
                existingIdle.gameObject.SetActive(false);
            }
        }

        if (legacyIdle == null)
        {
            return null;
        }

        Undo.RecordObject(legacyIdle.gameObject, "Disable Legacy Rat Idle");
        legacyIdle.gameObject.SetActive(false);
        SpriteRenderer renderer = legacyIdle.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Undo.RecordObject(renderer, "Disable Legacy Rat Idle Renderer");
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }

        return renderer;
    }

    private static SpriteRenderer PreserveLegacyRunUpperBody(Transform runRoot)
    {
        Transform legacyUpperBody = runRoot.Find("LegacyRunUpperBody");
        Transform existingUpperBody = runRoot.Find("RunUpperBody");
        if (existingUpperBody != null)
        {
            if (legacyUpperBody == null)
            {
                Undo.RecordObject(existingUpperBody.gameObject, "Preserve Legacy Rat Run Upper Body");
                existingUpperBody.name = "LegacyRunUpperBody";
                legacyUpperBody = existingUpperBody;
            }
            else
            {
                Undo.RecordObject(existingUpperBody.gameObject, "Disable Legacy Rat Run Upper Body");
                existingUpperBody.gameObject.SetActive(false);
            }
        }

        if (legacyUpperBody == null)
        {
            return null;
        }

        Undo.RecordObject(legacyUpperBody.gameObject, "Disable Legacy Rat Run Upper Body");
        legacyUpperBody.gameObject.SetActive(false);
        SpriteRenderer renderer = legacyUpperBody.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Undo.RecordObject(renderer, "Disable Legacy Rat Run Upper Body Renderer");
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }

        return renderer;
    }

    private static SpriteRenderer PreserveLegacyRunSeparatedArmLeft(Transform runRoot)
    {
        Transform legacyArm = runRoot.Find("LegacyRunSeparatedArmLeft");
        Transform existingArm = runRoot.Find("RunSeparatedArmLeft");
        if (existingArm != null)
        {
            if (legacyArm == null)
            {
                Undo.RecordObject(existingArm.gameObject, "Preserve Legacy Rat Run Left Arm");
                existingArm.name = "LegacyRunSeparatedArmLeft";
                legacyArm = existingArm;
            }
            else
            {
                Undo.RecordObject(existingArm.gameObject, "Disable Legacy Rat Run Left Arm");
                existingArm.gameObject.SetActive(false);
            }
        }

        if (legacyArm == null)
        {
            return null;
        }

        Undo.RecordObject(legacyArm.gameObject, "Disable Legacy Rat Run Left Arm");
        legacyArm.gameObject.SetActive(false);
        SpriteRenderer renderer = legacyArm.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Undo.RecordObject(renderer, "Disable Legacy Rat Run Left Arm Renderer");
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }

        return renderer;
    }

    private static void InitializeVisualSettingIfNeeded(
        SerializedObject serializedObject,
        string settingPropertyName,
        string legacySpritePropertyName,
        SpriteRenderer runRenderer,
        SpriteRenderer idleRenderer)
    {
        SerializedProperty setting = serializedObject.FindProperty(settingPropertyName);
        SerializedProperty initialized = setting?.FindPropertyRelative("initialized");
        SerializedProperty separateTransformsInitialized =
            setting?.FindPropertyRelative("separateTransformsInitialized");
        if (setting == null || initialized == null || separateTransformsInitialized == null)
        {
            return;
        }

        SerializedProperty sprite = setting.FindPropertyRelative("sprite");
        SerializedProperty legacyScale = setting.FindPropertyRelative("localScale");
        SerializedProperty runPositionProperty = setting.FindPropertyRelative("runLocalPosition");
        SerializedProperty runScaleProperty = setting.FindPropertyRelative("runLocalScale");
        SerializedProperty runRotationProperty = setting.FindPropertyRelative("runLocalRotationZ");
        SerializedProperty idlePositionProperty = setting.FindPropertyRelative("idleLocalPosition");
        SerializedProperty idleScaleProperty = setting.FindPropertyRelative("idleLocalScale");
        SerializedProperty idleRotationProperty = setting.FindPropertyRelative("idleLocalRotationZ");
        SerializedProperty sortingOrderProperty = setting.FindPropertyRelative("sortingOrder");
        if (sprite == null
            || legacyScale == null
            || runPositionProperty == null
            || runScaleProperty == null
            || runRotationProperty == null
            || idlePositionProperty == null
            || idleScaleProperty == null
            || idleRotationProperty == null
            || sortingOrderProperty == null)
        {
            Debug.LogWarning(
                $"[Stage02RatVisualBuilder] Visual setting could not be initialized: {settingPropertyName}");
            return;
        }

        bool wasCompletelyEmpty = sprite.objectReferenceValue == null
            && legacyScale.vector3Value == Vector3.zero
            && runPositionProperty.vector3Value == Vector3.zero
            && runScaleProperty.vector3Value == Vector3.zero
            && Mathf.Approximately(runRotationProperty.floatValue, 0f)
            && idlePositionProperty.vector3Value == Vector3.zero
            && idleScaleProperty.vector3Value == Vector3.zero
            && Mathf.Approximately(idleRotationProperty.floatValue, 0f)
            && sortingOrderProperty.intValue == 0;

        SerializedProperty legacySprite = serializedObject.FindProperty(legacySpritePropertyName);
        if (sprite.objectReferenceValue == null && legacySprite != null)
        {
            sprite.objectReferenceValue = legacySprite.objectReferenceValue;
        }

        if (!separateTransformsInitialized.boolValue)
        {
            Vector3 oldScale = legacyScale.vector3Value;
            if (runPositionProperty.vector3Value == Vector3.zero && runRenderer != null)
            {
                runPositionProperty.vector3Value = runRenderer.transform.localPosition;
            }

            if (idlePositionProperty.vector3Value == Vector3.zero && idleRenderer != null)
            {
                idlePositionProperty.vector3Value = idleRenderer.transform.localPosition;
            }

            if (runScaleProperty.vector3Value == Vector3.zero)
            {
                runScaleProperty.vector3Value = oldScale != Vector3.zero
                    ? oldScale
                    : GetRendererScaleOrOne(runRenderer);
            }

            if (idleScaleProperty.vector3Value == Vector3.zero)
            {
                idleScaleProperty.vector3Value = oldScale != Vector3.zero
                    ? oldScale
                    : GetRendererScaleOrOne(idleRenderer);
            }

            if (Mathf.Approximately(runRotationProperty.floatValue, 0f) && runRenderer != null)
            {
                runRotationProperty.floatValue = runRenderer.transform.localEulerAngles.z;
            }

            if (Mathf.Approximately(idleRotationProperty.floatValue, 0f) && idleRenderer != null)
            {
                idleRotationProperty.floatValue = idleRenderer.transform.localEulerAngles.z;
            }

            if (wasCompletelyEmpty)
            {
                sortingOrderProperty.intValue = runRenderer != null
                    ? runRenderer.sortingOrder
                    : idleRenderer != null ? idleRenderer.sortingOrder : 0;
            }

            separateTransformsInitialized.boolValue = true;
        }

        if (runScaleProperty.vector3Value == Vector3.zero)
        {
            runScaleProperty.vector3Value = GetRendererScaleOrOne(runRenderer);
        }

        if (idleScaleProperty.vector3Value == Vector3.zero)
        {
            idleScaleProperty.vector3Value = GetRendererScaleOrOne(idleRenderer);
        }

        initialized.boolValue = true;
    }

    private static Vector3 GetRendererScaleOrOne(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.transform.localScale == Vector3.zero)
        {
            return Vector3.one;
        }

        return renderer.transform.localScale;
    }

    private static SpriteRenderer ConfigureMirroredRenderer(
        Transform parent,
        string objectName,
        string spriteFileName,
        SpriteRenderer referenceRenderer,
        int sortingOrder,
        out Sprite sprite)
    {
        Transform existing = parent.Find(objectName);
        bool wasMissing = existing == null;
        SpriteRenderer renderer = ConfigureRenderer(
            parent,
            objectName,
            spriteFileName,
            referenceRenderer,
            sortingOrder,
            out sprite);

        if (wasMissing)
        {
            Transform child = renderer.transform;
            Undo.RecordObject(child, $"Mirror {objectName}");
            Vector3 scale = child.localScale;
            scale.x = -Mathf.Abs(Mathf.Approximately(scale.x, 0f) ? 1f : scale.x);
            child.localScale = scale;
            EditorUtility.SetDirty(child);
        }

        return renderer;
    }

    private static SpriteRenderer ConfigureRenderer(
        Transform parent,
        string objectName,
        string spriteFileName,
        SpriteRenderer referenceRenderer,
        int sortingOrder,
        out Sprite sprite)
    {
        Transform child = GetOrCreateChild(parent, objectName, out bool childCreated);
        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        bool rendererCreated = renderer == null;
        if (rendererCreated)
        {
            renderer = Undo.AddComponent<SpriteRenderer>(child.gameObject);
        }

        sprite = LoadSprite(spriteFileName);
        Undo.RecordObject(renderer, $"Configure {objectName}");
        if (sprite != null)
        {
            renderer.sprite = sprite;
        }

        if ((childCreated || rendererCreated) && referenceRenderer != null)
        {
            renderer.sharedMaterial = referenceRenderer.sharedMaterial;
            renderer.sortingLayerID = referenceRenderer.sortingLayerID;
        }

        if (childCreated || rendererCreated)
        {
            renderer.sortingOrder = sortingOrder;
        }

        renderer.enabled = sprite != null;
        EditorUtility.SetDirty(renderer);
        return renderer;
    }

    private static Sprite LoadSprite(string fileName)
    {
        return LoadSprite(SpriteFolder, fileName);
    }

    private static Sprite LoadSprite(string folder, string fileName)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(folder + fileName);
        if (sprite == null)
        {
            Debug.LogWarning($"[Stage02RatVisualBuilder] Sprite was not found: {folder}{fileName}");
        }

        return sprite;
    }

    private static Transform GetOrCreateChild(Transform parent, string objectName)
    {
        return GetOrCreateChild(parent, objectName, out _);
    }

    private static Transform GetOrCreateChild(
        Transform parent,
        string objectName,
        out bool created)
    {
        Transform child = parent.Find(objectName);
        created = child == null;
        if (child == null)
        {
            GameObject childObject = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {objectName}");
            Undo.SetTransformParent(childObject.transform, parent, $"Parent {objectName}");
            child = childObject.transform;

            Undo.RecordObject(child, $"Initialize {objectName} Transform");
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }
        return child;
    }

    private static void SetObjectReference(
        SerializedObject serializedObject,
        string propertyName,
        Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }
}
