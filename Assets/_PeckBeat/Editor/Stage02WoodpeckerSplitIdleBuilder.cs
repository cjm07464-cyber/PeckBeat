using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage02WoodpeckerSplitIdleBuilder
{
    private const string MenuPath = "PeckBeat/Stage02/Build Woodpecker Split Idle";
    private const string FitMenuPath = "PeckBeat/Stage02/Fit Woodpecker To TreeRing";
    private const string Stage02ScenePath = "Assets/Scenes/Stage02.unity";
    private const string SpriteFolder = "Assets/_PeckBeat/Art/Stage_02/Player/Woodpecker/";
    private const float RecommendedWoodpeckerScale = 0.55f;
    private const float HorizontalOffsetFromTreeRing = 0f;
    private const float FeetSeatOffset = -0.05f;

    [MenuItem(MenuPath)]
    private static void BuildWoodpeckerSplitIdle()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage02ScenePath)
        {
            Debug.LogWarning(
                $"[Stage02WoodpeckerSplitIdleBuilder] Open {Stage02ScenePath} before running this menu.");
            return;
        }

        Transform woodpecker = FindSceneTransform(scene, "GameplayRoot/Woodpecker");
        if (woodpecker == null)
        {
            Debug.LogWarning(
                "[Stage02WoodpeckerSplitIdleBuilder] GameplayRoot/Woodpecker was not found.");
            return;
        }

        Stage02WoodpeckerController controller =
            woodpecker.GetComponent<Stage02WoodpeckerController>();
        if (controller == null)
        {
            Debug.LogWarning(
                "[Stage02WoodpeckerSplitIdleBuilder] Stage02WoodpeckerController was not found.",
                woodpecker);
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Stage02 Woodpecker Split Idle");

        Transform splitIdleRoot = GetOrCreateChild(woodpecker, "SplitIdleRoot");
        Transform bodyBobRoot = GetOrCreateChild(splitIdleRoot, "BodyBobRoot");
        Transform wingBobRoot = GetOrCreateChild(splitIdleRoot, "WingBobRoot");
        Transform headBobRoot = GetOrCreateChild(splitIdleRoot, "HeadBobRoot");
        MoveExistingPart(bodyBobRoot, wingBobRoot, "Wings");

        SpriteRenderer legacyRenderer = null;
        Transform legacyVisual = woodpecker.Find("Visual");
        if (legacyVisual != null)
        {
            legacyRenderer = legacyVisual.GetComponent<SpriteRenderer>();
        }

        int baseSortingOrder = legacyRenderer != null ? legacyRenderer.sortingOrder : 0;
        bool allSpritesAssigned = true;
        allSpritesAssigned &= ConfigurePart(
            bodyBobRoot, "Tail", "꼬리(한쪽만).png", legacyRenderer, baseSortingOrder);
        allSpritesAssigned &= ConfigurePart(
            bodyBobRoot, "Body", "몸통.png", legacyRenderer, baseSortingOrder + 1);
        allSpritesAssigned &= ConfigurePart(
            wingBobRoot, "Wings", "날개.png", legacyRenderer, baseSortingOrder + 2);
        allSpritesAssigned &= ConfigurePart(
            bodyBobRoot, "Feet", "발.png", legacyRenderer, baseSortingOrder + 3);
        allSpritesAssigned &= ConfigurePart(
            headBobRoot, "Head", "머리.png", legacyRenderer, baseSortingOrder + 4);
        allSpritesAssigned &= ConfigurePart(
            headBobRoot, "Eyes", "눈.png", legacyRenderer, baseSortingOrder + 5);
        allSpritesAssigned &= ConfigurePart(
            headBobRoot, "Beak", "부리.png", legacyRenderer, baseSortingOrder + 6);
        SpriteRenderer peckHeadRenderer = ConfigurePeckHeadComposite(headBobRoot);

        Stage02WoodpeckerIdleVisual idleVisual =
            woodpecker.GetComponent<Stage02WoodpeckerIdleVisual>();
        if (idleVisual == null)
        {
            idleVisual = Undo.AddComponent<Stage02WoodpeckerIdleVisual>(woodpecker.gameObject);
        }

        SerializedObject idleVisualObject = new SerializedObject(idleVisual);
        idleVisualObject.FindProperty("splitIdleRoot").objectReferenceValue =
            splitIdleRoot.gameObject;
        idleVisualObject.FindProperty("bodyBobRoot").objectReferenceValue = bodyBobRoot;
        idleVisualObject.FindProperty("wingBobRoot").objectReferenceValue = wingBobRoot;
        idleVisualObject.FindProperty("headBobRoot").objectReferenceValue = headBobRoot;
        SerializedProperty peckHeadRendererProperty =
            idleVisualObject.FindProperty("peckHeadRenderer");
        bool connectedPeckHeadRenderer = false;
        if (peckHeadRendererProperty != null
            && peckHeadRendererProperty.objectReferenceValue == null)
        {
            peckHeadRendererProperty.objectReferenceValue = peckHeadRenderer;
            connectedPeckHeadRenderer = true;
        }

        SerializedProperty peckHeadSpriteProperty =
            idleVisualObject.FindProperty("peckHeadCompositeSprite");
        if (peckHeadSpriteProperty != null
            && peckHeadSpriteProperty.objectReferenceValue == null
            && peckHeadRenderer != null
            && peckHeadRenderer.sprite != null)
        {
            peckHeadSpriteProperty.objectReferenceValue = peckHeadRenderer.sprite;
        }

        SerializedProperty peckHeadOffsetProperty =
            idleVisualObject.FindProperty("peckHeadLocalOffset");
        if (connectedPeckHeadRenderer
            && peckHeadOffsetProperty != null
            && peckHeadOffsetProperty.vector3Value == Vector3.zero
            && peckHeadRenderer != null)
        {
            peckHeadOffsetProperty.vector3Value = peckHeadRenderer.transform.localPosition;
        }
        idleVisualObject.FindProperty("bodyPulseYOffset").floatValue = 0f;
        idleVisualObject.FindProperty("bodyPulseScaleX").floatValue = 1f;
        idleVisualObject.FindProperty("bodyPulseScaleY").floatValue = 1f;
        idleVisualObject.ApplyModifiedProperties();

        SerializedObject controllerObject = new SerializedObject(controller);
        controllerObject.FindProperty("idleVisual").objectReferenceValue = idleVisual;
        controllerObject.ApplyModifiedProperties();

        Undo.RecordObject(splitIdleRoot.gameObject, "Set Split Idle Visibility");
        splitIdleRoot.gameObject.SetActive(allSpritesAssigned);

        if (legacyRenderer != null)
        {
            Undo.RecordObject(legacyRenderer, "Set Legacy Woodpecker Visibility");
            legacyRenderer.enabled = !allSpritesAssigned;
        }

        EditorUtility.SetDirty(idleVisual);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = splitIdleRoot.gameObject;
        Undo.CollapseUndoOperations(undoGroup);

        if (allSpritesAssigned)
        {
            Debug.Log(
                "[Stage02WoodpeckerSplitIdleBuilder] Split Idle hierarchy and all seven part Sprites were connected. "
                + "The scene was marked dirty but was not saved.",
                splitIdleRoot.gameObject);
        }
        else
        {
            Debug.LogWarning(
                "[Stage02WoodpeckerSplitIdleBuilder] One or more part Sprites were not found. "
                + "SplitIdleRoot remains inactive and the legacy Visual remains enabled. "
                + "The scene was not saved.",
                splitIdleRoot.gameObject);
        }
    }

    [MenuItem(FitMenuPath)]
    private static void FitWoodpeckerToTreeRing()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage02ScenePath)
        {
            Debug.LogWarning(
                $"[Stage02WoodpeckerSplitIdleBuilder] Open {Stage02ScenePath} before running this menu.");
            return;
        }

        Transform woodpecker = FindSceneTransform(scene, "GameplayRoot/Woodpecker");
        Transform treeRing = FindSceneTransform(scene, "StageRoot/TreeRing");
        if (woodpecker == null || treeRing == null)
        {
            Debug.LogWarning(
                "[Stage02WoodpeckerSplitIdleBuilder] GameplayRoot/Woodpecker or StageRoot/TreeRing was not found.");
            return;
        }

        SpriteRenderer treeRingRenderer = treeRing.GetComponent<SpriteRenderer>();
        Transform splitIdleRoot = woodpecker.Find("SplitIdleRoot");
        Transform feet = splitIdleRoot != null
            ? splitIdleRoot.Find("BodyBobRoot/Feet")
            : null;
        SpriteRenderer feetRenderer = feet != null ? feet.GetComponent<SpriteRenderer>() : null;
        SpriteRenderer[] partRenderers = splitIdleRoot != null
            ? splitIdleRoot.GetComponentsInChildren<SpriteRenderer>(true)
            : null;

        if (treeRingRenderer == null
            || !TryGetSpriteWorldBounds(treeRingRenderer, out Bounds treeRingBounds)
            || feetRenderer == null
            || !TryGetSpriteWorldBounds(feetRenderer, out _)
            || partRenderers == null
            || partRenderers.Length == 0)
        {
            Debug.LogWarning(
                "[Stage02WoodpeckerSplitIdleBuilder] TreeRing or Split Idle renderers are incomplete. "
                + "Run Build Woodpecker Split Idle first and verify its Sprite assignments.",
                woodpecker.gameObject);
            return;
        }

        Undo.RecordObject(woodpecker, "Fit Woodpecker To TreeRing");

        woodpecker.localScale = Vector3.one * RecommendedWoodpeckerScale;
        Vector3 fittedPosition = woodpecker.position;
        fittedPosition.x = treeRingBounds.center.x;
        fittedPosition.y = treeRingBounds.max.y;
        woodpecker.position = fittedPosition;

        bool hasWoodpeckerBounds = false;
        Bounds woodpeckerBounds = default;
        foreach (SpriteRenderer partRenderer in partRenderers)
        {
            if (!TryGetSpriteWorldBounds(partRenderer, out Bounds partBounds))
            {
                continue;
            }

            if (!hasWoodpeckerBounds)
            {
                woodpeckerBounds = partBounds;
                hasWoodpeckerBounds = true;
            }
            else
            {
                woodpeckerBounds.Encapsulate(partBounds);
            }
        }

        if (!hasWoodpeckerBounds
            || !TryGetSpriteWorldBounds(feetRenderer, out Bounds feetBounds))
        {
            Debug.LogWarning(
                "[Stage02WoodpeckerSplitIdleBuilder] Could not calculate the Split Idle Sprite bounds.",
                woodpecker.gameObject);
            return;
        }

        float horizontalDelta = treeRingBounds.center.x
            + HorizontalOffsetFromTreeRing
            - woodpeckerBounds.center.x;
        float verticalDelta = treeRingBounds.max.y
            + FeetSeatOffset
            - feetBounds.min.y;
        woodpecker.position += new Vector3(horizontalDelta, verticalDelta, 0f);

        EditorUtility.SetDirty(woodpecker);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = woodpecker.gameObject;

        Debug.Log(
            $"[Stage02WoodpeckerSplitIdleBuilder] Fitted Woodpecker to TreeRing. "
            + $"World Position={woodpecker.position}, Local Scale={woodpecker.localScale}, "
            + $"TreeRing Top Y={treeRingBounds.max.y:0.###}. "
            + "The scene was marked dirty but was not saved.",
            woodpecker.gameObject);
    }

    private static bool TryGetSpriteWorldBounds(
        SpriteRenderer spriteRenderer,
        out Bounds bounds)
    {
        bounds = default;
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return false;
        }

        Vector2[] vertices = spriteRenderer.sprite.vertices;
        if (vertices == null || vertices.Length == 0)
        {
            return false;
        }

        Vector2 firstVertex = vertices[0];
        if (spriteRenderer.flipX)
        {
            firstVertex.x = -firstVertex.x;
        }
        if (spriteRenderer.flipY)
        {
            firstVertex.y = -firstVertex.y;
        }

        Vector3 firstWorldVertex = spriteRenderer.transform.TransformPoint(firstVertex);
        bounds = new Bounds(firstWorldVertex, Vector3.zero);

        for (int i = 1; i < vertices.Length; i++)
        {
            Vector2 vertex = vertices[i];
            if (spriteRenderer.flipX)
            {
                vertex.x = -vertex.x;
            }
            if (spriteRenderer.flipY)
            {
                vertex.y = -vertex.y;
            }

            bounds.Encapsulate(spriteRenderer.transform.TransformPoint(vertex));
        }

        return true;
    }

    private static bool ConfigurePart(
        Transform parent,
        string objectName,
        string spriteFileName,
        SpriteRenderer legacyRenderer,
        int sortingOrder)
    {
        Transform part = GetOrCreateChild(parent, objectName);
        SpriteRenderer partRenderer = part.GetComponent<SpriteRenderer>();
        if (partRenderer == null)
        {
            partRenderer = Undo.AddComponent<SpriteRenderer>(part.gameObject);
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + spriteFileName);
        Undo.RecordObject(partRenderer, $"Configure {objectName}");

        if (sprite != null)
        {
            partRenderer.sprite = sprite;
        }
        else
        {
            Debug.LogWarning(
                $"[Stage02WoodpeckerSplitIdleBuilder] Sprite was not found: {spriteFileName}");
        }

        if (legacyRenderer != null)
        {
            partRenderer.sharedMaterial = legacyRenderer.sharedMaterial;
            partRenderer.sortingLayerID = legacyRenderer.sortingLayerID;
        }

        partRenderer.sortingOrder = sortingOrder;
        partRenderer.enabled = true;
        EditorUtility.SetDirty(partRenderer);
        return sprite != null;
    }

    private static SpriteRenderer ConfigurePeckHeadComposite(Transform headBobRoot)
    {
        Transform composite = headBobRoot.Find("PeckHeadComposite");
        bool childCreated = composite == null;
        if (childCreated)
        {
            GameObject compositeObject = new GameObject("PeckHeadComposite");
            Undo.RegisterCreatedObjectUndo(compositeObject, "Create Peck Head Composite");
            Undo.SetTransformParent(
                compositeObject.transform,
                headBobRoot,
                "Parent Peck Head Composite");
            composite = compositeObject.transform;
            composite.localPosition = Vector3.zero;
            composite.localRotation = Quaternion.identity;
            composite.localScale = Vector3.one;
        }

        SpriteRenderer compositeRenderer = composite.GetComponent<SpriteRenderer>();
        bool rendererCreated = compositeRenderer == null;
        if (rendererCreated)
        {
            compositeRenderer = Undo.AddComponent<SpriteRenderer>(composite.gameObject);
        }

        if (childCreated || rendererCreated)
        {
            SpriteRenderer headRenderer = headBobRoot.Find("Head")?.GetComponent<SpriteRenderer>();
            Undo.RecordObject(compositeRenderer, "Configure Peck Head Composite Renderer");
            if (headRenderer != null)
            {
                compositeRenderer.sharedMaterial = headRenderer.sharedMaterial;
                compositeRenderer.sortingLayerID = headRenderer.sortingLayerID;
                compositeRenderer.sortingOrder = headRenderer.sortingOrder + 1;
            }
        }

        Undo.RecordObject(compositeRenderer, "Hide Peck Head Composite");
        compositeRenderer.enabled = false;
        EditorUtility.SetDirty(compositeRenderer);
        return compositeRenderer;
    }

    private static void MoveExistingPart(
        Transform sourceParent,
        Transform destinationParent,
        string objectName)
    {
        if (sourceParent == null || destinationParent == null)
        {
            return;
        }

        Transform sourcePart = sourceParent.Find(objectName);
        if (sourcePart == null)
        {
            return;
        }

        Transform existingDestinationPart = destinationParent.Find(objectName);
        if (existingDestinationPart != null)
        {
            Undo.RecordObject(sourcePart.gameObject, $"Disable Duplicate {objectName}");
            sourcePart.gameObject.SetActive(false);
            Debug.LogWarning(
                $"[Stage02WoodpeckerSplitIdleBuilder] A duplicate {objectName} remained under "
                + $"{sourceParent.name}. It was disabled instead of deleted.",
                sourcePart.gameObject);
            return;
        }

        Undo.SetTransformParent(
            sourcePart,
            destinationParent,
            $"Move {objectName} To {destinationParent.name}");
        Undo.RecordObject(sourcePart, $"Reset {objectName} Transform");
        sourcePart.localPosition = Vector3.zero;
        sourcePart.localRotation = Quaternion.identity;
        sourcePart.localScale = Vector3.one;
    }

    private static Transform GetOrCreateChild(Transform parent, string objectName)
    {
        Transform child = parent.Find(objectName);
        if (child == null)
        {
            GameObject childObject = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {objectName}");
            Undo.SetTransformParent(childObject.transform, parent, $"Parent {objectName}");
            child = childObject.transform;
        }

        Undo.RecordObject(child, $"Reset {objectName} Transform");
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return child;
    }

    private static Transform FindSceneTransform(Scene scene, string hierarchyPath)
    {
        string[] pathParts = hierarchyPath.Split('/');
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != pathParts[0])
            {
                continue;
            }

            Transform current = root.transform;
            for (int i = 1; i < pathParts.Length && current != null; i++)
            {
                current = current.Find(pathParts[i]);
            }

            if (current != null)
            {
                return current;
            }
        }

        return null;
    }
}
