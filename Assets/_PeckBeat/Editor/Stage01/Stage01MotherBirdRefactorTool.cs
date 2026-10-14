#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage01MotherBirdRefactorTool
{
    private const string MotherBirdPrefabPath = "Assets/Prefabs/PF_Stage_01/PF_MotherBird.prefab";
    private const string ChickPrefabPath = "Assets/Prefabs/PF_Stage_01/PF_Chick.prefab";
    private const string CuckooPrefabPath = "Assets/Prefabs/PF_Stage_01/PF_Cuckoo.prefab";
    private const string Stage01ScenePath = "Assets/Scenes/Stage01_Prototype.unity";
    private const string PrimaryTestSpriteFolder = "Assets/_PeckBeat/Art/Stage_01/Player/Test";
    private const string CuckooTest2SpriteFolder = "Assets/_PeckBeat/Art/Stage_01/Player/Test/Test2";
    private const float RecommendedCuckooRootScale = 0.42f;
    private const float RecommendedPeck1SuccessCompositeDuration = 0.35f;
    private const string SecondaryTestSpriteFolder = "Assets/Art/Stage_01/Player/Test";
    private const string BroadPlayerFolder = "Assets/_PeckBeat/Art/Stage_01/Player";

    private static readonly string[] SpriteSearchRoots =
    {
        "Assets/_PeckBeat/Art/Stage_01",
        "Assets/_PeckBeat/Art",
        "Assets/Art",
        "Assets"
    };

    private sealed class PoseSpec
    {
        public readonly string Label;
        public readonly string PropertyName;
        public readonly string[] ExactNames;
        public readonly bool Required;

        public PoseSpec(string label, string propertyName, bool required, params string[] exactNames)
        {
            Label = label;
            PropertyName = propertyName;
            Required = required;
            ExactNames = exactNames;
        }
    }

    private sealed class SpriteCandidate
    {
        public Sprite Sprite;
        public string Path;
        public string Guid;
        public string FileName;
        public TextureImporter Importer;
        public int Width;
        public int Height;
    }

    private sealed class TestMotherSprites
    {
        public Sprite Feed;
        public Sprite Peck1;
        public Sprite Peck2;
        public Sprite Wrong;
    }

    private sealed class NewStage01BirdSprites
    {
        public Sprite ChickIdle;
        public Sprite ChickFed;
        public Sprite MotherFeedSuccess;
        public Sprite CuckooWrongExit;
    }

    private sealed class NewCuckooAndPeckSprites
    {
        public Sprite IdleAndExit;
        public Sprite Fed;
        public Sprite WrongExit;
        public Sprite Hit1;
        public Sprite Hit2;
        public Sprite PartialHitExit;
        public Sprite MotherPeck1Success;
    }

    private static readonly PoseSpec[] PoseSpecs =
    {
        new PoseSpec("Idle", "idle", true, "Mother_Idle", "엄마새"),
        new PoseSpec("Feed", "feed", true, "Mother_Feed", "엄마새먹이주기"),
        new PoseSpec("FeedSuccess", "feedSuccess", false, "먹이주기대성공"),
        new PoseSpec("Peck1", "peck1", true, "Mother_Peck1", "빡친엄마새"),
        new PoseSpec(
            "Peck1Success",
            "peck1Success",
            false,
            "신엄마새1타대성공",
            "엄마새쪼기1타대성공",
            "Mother_Peck1Perfect"),
        new PoseSpec("Peck2", "peck2", true, "Mother_Peck2", "빡친엄마새2타"),
        new PoseSpec("Peck2Success", "peck2Success", false, "엄마새쪼기2타대성공", "Mother_Peck2Perfect"),
        new PoseSpec("WrongFeed", "wrongFeed", true, "Mother_WrongFeed", "먹이잘못주기"),
        new PoseSpec("WrongPeck", "wrongPeck", true, "Mother_WrongPeck", "먹이잘못주기"),
        new PoseSpec("Smile", "smile", false, "Mother_Smile", "엄마새미소")
    };

    [MenuItem("PeckBeat/Stage01/MotherBird/Analyze MotherBird Setup")]
    private static void AnalyzeMotherBirdSetup()
    {
        if (!CanRunEditorTool())
        {
            return;
        }

        Debug.Log("[Stage01 MotherBird Refactor] Analyze Start");
        AnalyzePrefab(false);
        InspectStage01Scene(false);

        List<SpriteCandidate> candidates = FindMotherBirdSpriteCandidates();
        Debug.Log($"[INFO] MotherBird sprite candidates: {candidates.Count}");
        foreach (SpriteCandidate candidate in candidates)
        {
            LogCandidate(candidate);
        }

        Debug.Log("[Stage01 MotherBird Refactor] Analyze Complete");
    }

    [MenuItem("PeckBeat/Stage01/MotherBird/Apply Unified MotherBird Poses To Prefab")]
    private static void ApplyUnifiedMotherBirdPosesToPrefab()
    {
        if (!CanRunEditorTool())
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(MotherBirdPrefabPath) == null)
        {
            Debug.LogError($"[FAIL] PF_MotherBird not found: {MotherBirdPrefabPath}");
            return;
        }

        List<SpriteCandidate> candidates = FindMotherBirdSpriteCandidates();
        GameObject prefabRoot = null;

        try
        {
            prefabRoot = PrefabUtility.LoadPrefabContents(MotherBirdPrefabPath);
            MotherBirdSpriteController controller =
                prefabRoot != null ? prefabRoot.GetComponentInChildren<MotherBirdSpriteController>(true) : null;
            if (controller == null)
            {
                Debug.LogError("[FAIL] MotherBirdSpriteController was not found in PF_MotherBird.");
                return;
            }

            SerializedObject serializedController = new SerializedObject(controller);
            if (!ValidatePosePropertyShape(serializedController, true))
            {
                Debug.LogError("[FAIL] Prefab apply aborted before making changes because the SpritePose structure is incompatible.");
                return;
            }

            Debug.Log("[Stage01 MotherBird Refactor] Apply Prefab Start");
            Sprite idleSprite = null;

            foreach (PoseSpec spec in PoseSpecs)
            {
                SerializedProperty pose = serializedController.FindProperty(spec.PropertyName);
                if (pose == null)
                {
                    Debug.LogWarning($"[WARN] Optional pose field not present; skipped: {spec.Label} ({spec.PropertyName})");
                    continue;
                }

                SerializedProperty spriteProperty = pose.FindPropertyRelative("sprite");
                SerializedProperty offsetProperty = pose.FindPropertyRelative("positionOffset");
                SerializedProperty scaleProperty = pose.FindPropertyRelative("scale");
                Sprite previousSprite = spriteProperty.objectReferenceValue as Sprite;
                Vector2 previousOffset = offsetProperty.vector2Value;
                Vector2 previousScale = scaleProperty.vector2Value;

                Debug.Log(
                    $"[INFO] Before {spec.Label}: sprite={GetObjectName(previousSprite)}, " +
                    $"offset={previousOffset}, scale={previousScale}");

                SpriteCandidate match = FindBestCandidate(spec, candidates);
                if (match != null && match.Sprite != null)
                {
                    spriteProperty.objectReferenceValue = match.Sprite;
                    Debug.Log($"[PASS] {spec.Label} sprite matched: {match.Path} / {match.Sprite.name}");
                    if (!HasRecommendedCanvas(match))
                    {
                        Debug.LogWarning(
                            $"[WARN] {spec.Label} matched sprite is not 1448x1086: " +
                            $"{match.Width}x{match.Height}. Verify the art before keeping this change.");
                    }
                }
                else
                {
                    if (spec.Required)
                    {
                        Debug.LogWarning(
                            $"[WARN] No confident sprite match for {spec.Label}. Existing sprite is preserved: " +
                            GetObjectName(previousSprite));
                    }
                    else
                    {
                        Debug.Log(
                            $"[INFO] Optional {spec.Label} Sprite was not found. " +
                            $"Existing value is preserved and runtime fallback remains active: {GetObjectName(previousSprite)}");
                    }
                }

                offsetProperty.vector2Value = Vector2.zero;
                scaleProperty.vector2Value = Vector2.one;

                if (spec.PropertyName == "idle")
                {
                    idleSprite = spriteProperty.objectReferenceValue as Sprite;
                }
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();

            SerializedProperty rendererProperty = serializedController.FindProperty("spriteRenderer");
            SpriteRenderer spriteRenderer = rendererProperty != null
                ? rendererProperty.objectReferenceValue as SpriteRenderer
                : null;

            if (spriteRenderer != null)
            {
                Transform visual = spriteRenderer.transform;
                Debug.Log(
                    $"[INFO] Before Visual transform: position={visual.localPosition}, scale={visual.localScale}");
                visual.localPosition = Vector3.zero;
                visual.localScale = Vector3.one;

                if (idleSprite != null)
                {
                    spriteRenderer.sprite = idleSprite;
                }
                else
                {
                    Debug.LogWarning("[WARN] Idle sprite is missing. SpriteRenderer initial sprite was not overwritten with null.");
                }
            }
            else
            {
                Debug.LogWarning("[WARN] Serialized spriteRenderer reference is missing. Visual transform was not changed.");
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, MotherBirdPrefabPath);
            Debug.Log($"[PASS] PF_MotherBird saved by explicit Apply menu: {MotherBirdPrefabPath}");
            Debug.Log("[Stage01 MotherBird Refactor] Apply Prefab Complete");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[FAIL] PF_MotherBird apply failed: {exception}");
        }
        finally
        {
            if (prefabRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    [MenuItem("PeckBeat/Stage01/MotherBird/Apply Recommended MotherBird Scene Transform")]
    private static void ApplyRecommendedMotherBirdSceneTransform()
    {
        if (!CanRunEditorTool())
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !string.Equals(activeScene.path, Stage01ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Stage01 MotherBird",
                $"Open {Stage01ScenePath} as the active scene before running this menu.",
                "OK");
            return;
        }

        MotherBirdSpriteController controller = FindMotherBirdController(activeScene);
        if (controller == null)
        {
            Debug.LogError("[FAIL] MotherBirdSpriteController was not found in the active Stage01 scene.");
            return;
        }

        GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(controller.gameObject);
        Transform motherBirdTransform = instanceRoot != null ? instanceRoot.transform : controller.transform;
        if (!EditorUtility.DisplayDialog(
                "Apply Recommended MotherBird Transform",
                $"Change only the MotherBird root scale from {motherBirdTransform.localScale} to (0.9, 0.9, 1)?\n\n" +
                "Position, Stage01FeedingController references, slots, and SpritePose overrides will not be changed. " +
                "The scene will be marked dirty but will not be saved automatically.",
                "Apply",
                "Cancel"))
        {
            return;
        }

        Undo.RecordObject(motherBirdTransform, "Apply Recommended MotherBird Transform");
        motherBirdTransform.localScale = new Vector3(0.9f, 0.9f, 1f);
        EditorUtility.SetDirty(motherBirdTransform);
        EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log(
            "[PASS] MotherBird root scale set to (0.9, 0.9, 1). Position and prefab overrides were preserved. " +
            "Scene was marked dirty and was not saved.");
    }

    [MenuItem("PeckBeat/Stage01/MotherBird/Apply Test 4 Mother Sprites To Scene")]
    private static void ApplyTestFourMotherSpritesToScene()
    {
        if (!CanRunEditorTool())
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() ||
            !string.Equals(activeScene.path, Stage01ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning(
                $"[WARN] Test Sprite apply aborted. Open {Stage01ScenePath} as the active scene first.");
            return;
        }

        MotherBirdSpriteController controller = FindMotherBirdController(activeScene);
        if (controller == null)
        {
            Debug.LogWarning("[WARN] MotherBirdSpriteController was not found in the active Stage01 scene.");
            return;
        }

        string testFolder = ResolveTestSpriteFolder();
        if (string.IsNullOrEmpty(testFolder))
        {
            Debug.LogWarning("[WARN] No unambiguous Player/Test folder was found. Nothing was applied.");
            return;
        }

        TestMotherSprites testSprites = FindTestMotherSprites(testFolder);
        if (testSprites.Feed == null && testSprites.Peck1 == null &&
            testSprites.Peck2 == null && testSprites.Wrong == null)
        {
            Debug.LogWarning("[WARN] None of the four required Test sprites could be matched. Nothing was applied.");
            return;
        }

        SerializedObject serializedController = new SerializedObject(controller);
        if (!ValidatePosePropertyShape(serializedController, true))
        {
            Debug.LogWarning("[WARN] Test Sprite apply aborted because the SpritePose structure is incompatible.");
            return;
        }

        GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(controller.gameObject);
        Transform rootTransform = instanceRoot != null ? instanceRoot.transform : controller.transform;
        SpriteRenderer spriteRenderer = GetControllerSpriteRenderer(controller);
        Transform visualTransform = spriteRenderer != null ? spriteRenderer.transform : null;
        Vector3 rootPositionBefore = rootTransform.localPosition;
        Sprite idleSpriteBefore = GetPoseSprite(serializedController, "idle");

        if (!EditorUtility.DisplayDialog(
                "Apply Test 4 Mother Sprites To Scene",
                $"Apply only the four MotherBird sprites found under:\n{testFolder}\n\n" +
                "Feed, Peck1, Peck2, WrongFeed, and WrongPeck will use the Test sprites. " +
                "All six Pose offsets/scales will be reset to (0,0)/(1,1). Idle Sprite and flipX values stay unchanged.\n\n" +
                $"MotherBird root position stays {rootPositionBefore}. Root scale will be set from " +
                $"{rootTransform.localScale} to (0.9, 0.9, 1).\n\n" +
                "Worm, Stage01FeedingController, slots, chicks, and cuckoos will not be changed. " +
                "The scene and prefab will not be saved automatically.",
                "Apply To Scene",
                "Cancel"))
        {
            return;
        }

        Undo.RecordObject(controller, "Apply Test MotherBird Sprites");
        Undo.RecordObject(rootTransform, "Apply Test MotherBird Root Scale");
        if (visualTransform != null)
        {
            Undo.RecordObject(visualTransform, "Reset MotherBird Visual Transform");
        }

        if (spriteRenderer != null)
        {
            Undo.RecordObject(spriteRenderer, "Enable MotherBird SpriteRenderer");
        }

        try
        {
            ApplyScenePose(serializedController, "idle", null, false);
            ApplyScenePose(serializedController, "feed", testSprites.Feed, true);
            ApplyScenePose(serializedController, "peck1", testSprites.Peck1, true);
            ApplyScenePose(serializedController, "peck2", testSprites.Peck2, true);
            ApplyScenePose(serializedController, "wrongFeed", testSprites.Wrong, true);
            ApplyScenePose(serializedController, "wrongPeck", testSprites.Wrong, true);
            serializedController.ApplyModifiedProperties();

            rootTransform.localScale = new Vector3(0.9f, 0.9f, 1f);
            if (visualTransform != null)
            {
                visualTransform.localPosition = Vector3.zero;
                visualTransform.localScale = Vector3.one;
            }
            else
            {
                Debug.LogWarning("[WARN] MotherBird Visual SpriteRenderer was not found; Visual transform was not reset.");
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(rootTransform);
            if (visualTransform != null)
            {
                EditorUtility.SetDirty(visualTransform);
            }

            if (spriteRenderer != null)
            {
                EditorUtility.SetDirty(spriteRenderer);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            serializedController.UpdateIfRequiredOrScript();
            string wormInfo = LogWormInfo(controller, "Apply Test");

            Debug.Log(
                "# Apply Test 4 Mother Sprites To Scene Result\n" +
                $"- Scene: {activeScene.path}\n" +
                $"- MotherBird object: {GetHierarchyPath(controller.transform)}\n" +
                $"- Feed sprite: {GetObjectName(GetPoseSprite(serializedController, "feed"))}\n" +
                $"- Peck1 sprite: {GetObjectName(GetPoseSprite(serializedController, "peck1"))}\n" +
                $"- Peck2 sprite: {GetObjectName(GetPoseSprite(serializedController, "peck2"))}\n" +
                $"- WrongFeed sprite: {GetObjectName(GetPoseSprite(serializedController, "wrongFeed"))}\n" +
                $"- WrongPeck sprite: {GetObjectName(GetPoseSprite(serializedController, "wrongPeck"))}\n" +
                $"- Idle sprite kept: {GetObjectName(idleSpriteBefore)}\n" +
                "- Pose offset reset: (0, 0)\n" +
                "- Pose scale reset: (1, 1)\n" +
                $"- MotherBird root position kept: {rootTransform.localPosition}\n" +
                $"- MotherBird root scale set: {rootTransform.localScale}\n" +
                $"- Visual localPosition: {(visualTransform != null ? visualTransform.localPosition.ToString() : "<missing>")}\n" +
                $"- Visual localScale: {(visualTransform != null ? visualTransform.localScale.ToString() : "<missing>")}\n" +
                $"- Worm info: {wormInfo}\n" +
                "- Scene saved automatically: No\n" +
                "- Prefab saved automatically: No");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[FAIL] Apply Test 4 Mother Sprites To Scene failed: {exception}");
        }
    }

    [MenuItem("PeckBeat/Stage01/MotherBird/Revert MotherBird Pose Overrides In Scene")]
    private static void RevertMotherBirdPoseOverridesInScene()
    {
        if (!CanRunEditorTool())
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() ||
            !string.Equals(activeScene.path, Stage01ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning(
                $"[WARN] Pose override revert aborted. Open {Stage01ScenePath} as the active scene first.");
            return;
        }

        MotherBirdSpriteController controller = FindMotherBirdController(activeScene);
        if (controller == null)
        {
            Debug.LogWarning("[WARN] MotherBirdSpriteController was not found in the active Stage01 scene.");
            return;
        }

        GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(controller.gameObject);
        MotherBirdSpriteController sourceController =
            PrefabUtility.GetCorrespondingObjectFromSource(controller);
        if (instanceRoot == null || !PrefabUtility.IsPartOfPrefabInstance(controller) || sourceController == null)
        {
            Debug.LogWarning(
                "[WARN] The found MotherBirdSpriteController is not a connected PF_MotherBird prefab instance. " +
                "No override was reverted.");
            return;
        }

        List<PropertyModification> poseOverrides =
            GetPoseOverrideModifications(instanceRoot, sourceController);
        if (poseOverrides.Count == 0)
        {
            Debug.Log("[PASS] No MotherBird SpritePose overrides remain in the active Stage01 scene.");
            return;
        }

        Transform protectedTransform = instanceRoot.transform;
        Vector3 positionBefore = protectedTransform.localPosition;
        Vector3 scaleBefore = protectedTransform.localScale;
        string targetPath = GetHierarchyPath(controller.transform);

        if (!EditorUtility.DisplayDialog(
                "Revert MotherBird Pose Overrides",
                "This will revert only MotherBirdSpriteController SpritePose overrides " +
                "(sprite, positionOffset, scale, and flipX) to the prefab values.\n\n" +
                "MotherBird Transform position/scale, Stage01FeedingController references, slots, and other " +
                "components will not be changed. The scene will not be saved automatically.\n\n" +
                "Review the result and save the scene manually if it is correct.",
                "Revert Pose Overrides",
                "Cancel"))
        {
            return;
        }

        SerializedObject serializedController = new SerializedObject(controller);
        if (!ValidatePosePropertyShape(serializedController, true))
        {
            Debug.LogWarning("[WARN] Pose override revert aborted because the SpritePose structure is incompatible.");
            return;
        }

        Debug.Log($"[INFO] Revert target object: {targetPath}");
        Debug.Log($"[INFO] Revert target component: {controller.GetType().Name}");
        Debug.Log($"[INFO] Before revert: {BuildPoseSummary(serializedController)}");

        int revertedCount = 0;
        Undo.RecordObject(controller, "Revert MotherBird Pose Overrides");

        try
        {
            foreach (string propertyPath in poseOverrides
                         .Select(modification => modification.propertyPath)
                         .Where(path => !string.IsNullOrEmpty(path))
                         .Distinct(StringComparer.Ordinal))
            {
                serializedController.UpdateIfRequiredOrScript();
                SerializedProperty property = serializedController.FindProperty(propertyPath);
                if (property == null)
                {
                    Debug.LogWarning($"[WARN] Pose override property was not found and was skipped: {propertyPath}");
                    continue;
                }

                PrefabUtility.RevertPropertyOverride(property, InteractionMode.UserAction);
                revertedCount++;
                Debug.Log($"[PASS] Reverted MotherBird Pose override: {propertyPath}");
            }

            serializedController.UpdateIfRequiredOrScript();
            Debug.Log($"[INFO] After revert: {BuildPoseSummary(serializedController)}");

            bool positionUnchanged = protectedTransform.localPosition == positionBefore;
            bool scaleUnchanged = protectedTransform.localScale == scaleBefore;
            if (positionUnchanged && scaleUnchanged)
            {
                Debug.Log(
                    $"[PASS] MotherBird Transform was not changed: position={protectedTransform.localPosition}, " +
                    $"scale={protectedTransform.localScale}");
            }
            else
            {
                Debug.LogError(
                    "[FAIL] MotherBird Transform changed unexpectedly during Pose revert. " +
                    $"Before position/scale={positionBefore}/{scaleBefore}, " +
                    $"after={protectedTransform.localPosition}/{protectedTransform.localScale}");
            }

            if (revertedCount > 0)
            {
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            Debug.Log(
                $"[PASS] Reverted {revertedCount} MotherBird Pose override properties. " +
                "The scene was marked dirty and was not saved automatically.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[FAIL] MotherBird Pose override revert failed: {exception}");
        }
    }

    [MenuItem("PeckBeat/Stage01/MotherBird/Apply Split Idle Mother Parts To Scene")]
    private static void ApplySplitIdleMotherPartsToScene()
    {
        if (!CanRunEditorTool())
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() ||
            !string.Equals(activeScene.path, Stage01ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"[WARN] Open {Stage01ScenePath} as the active scene before applying Split Idle parts.");
            return;
        }

        MotherBirdSpriteController controller = FindMotherBirdController(activeScene);
        if (controller == null)
        {
            Debug.LogWarning("[WARN] MotherBirdSpriteController was not found in the active Stage01 scene.");
            return;
        }

        Transform beatMoveRoot = controller.transform.Find("BeatMoveRoot");
        Transform beatSquashRoot = controller.transform.Find("BeatMoveRoot/BeatSquashRoot");
        SpriteRenderer spriteVisual = GetControllerSpriteRenderer(controller);
        if (beatMoveRoot == null || beatSquashRoot == null || spriteVisual == null)
        {
            Debug.LogWarning(
                $"[WARN] Split Idle apply aborted. BeatMoveRoot={beatMoveRoot != null}, " +
                $"BeatSquashRoot={beatSquashRoot != null}, SpriteVisual={spriteVisual != null}.");
            return;
        }

        Sprite bodySprite = LoadExactSplitIdleSprite("엄마새_몸통베이스.png");
        Sprite eyeSprite = LoadExactSplitIdleSprite("엄마새_눈파츠.png");
        Sprite beakWormSprite = LoadExactSplitIdleSprite("엄마새_부리_지렁이파츠.png");
        if (bodySprite == null || eyeSprite == null || beakWormSprite == null)
        {
            Debug.LogWarning("[WARN] Split Idle apply aborted because all three exact Test sprites were not available.");
            return;
        }

        SerializedObject controllerObject = new SerializedObject(controller);
        string[] requiredProperties =
        {
            "useSplitIdleVisual",
            "idleBodyRenderer",
            "idleEyeRenderer",
            "idleBeakWormRenderer",
            "idleBodySortingOrder",
            "idleFaceSortingOrder",
            "hideLegacyWormVisual"
        };
        if (requiredProperties.Any(name => controllerObject.FindProperty(name) == null))
        {
            Debug.LogError("[FAIL] Split Idle serialized fields are missing from MotherBirdSpriteController.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Apply Split Idle Mother Parts To Scene",
                "Create or reuse the three MotherBird Idle part renderers in the active Stage01 scene, " +
                "connect their Sprites, and hide the legacy WormVisual renderer when present.\n\n" +
                "The scene will be marked dirty but will not be saved automatically.",
                "Apply To Scene",
                "Cancel"))
        {
            return;
        }

        SpriteRenderer bodyRenderer = GetOrCreateSplitIdleRenderer(
            beatSquashRoot,
            "IdleBodyVisual",
            bodySprite,
            spriteVisual,
            -3);
        SpriteRenderer eyeRenderer = GetOrCreateSplitIdleRenderer(
            beatMoveRoot,
            "IdleEyeVisual",
            eyeSprite,
            spriteVisual,
            -2);
        SpriteRenderer beakWormRenderer = GetOrCreateSplitIdleRenderer(
            beatMoveRoot,
            "IdleBeakWormVisual",
            beakWormSprite,
            spriteVisual,
            -2);
        if (bodyRenderer == null || eyeRenderer == null || beakWormRenderer == null)
        {
            Debug.LogError("[FAIL] One or more Split Idle SpriteRenderers could not be created.");
            return;
        }

        Undo.RecordObject(controller, "Connect MotherBird Split Idle Renderers");
        controllerObject.UpdateIfRequiredOrScript();
        controllerObject.FindProperty("useSplitIdleVisual").boolValue = true;
        controllerObject.FindProperty("idleBodyRenderer").objectReferenceValue = bodyRenderer;
        controllerObject.FindProperty("idleEyeRenderer").objectReferenceValue = eyeRenderer;
        controllerObject.FindProperty("idleBeakWormRenderer").objectReferenceValue = beakWormRenderer;
        controllerObject.FindProperty("idleBodySortingOrder").intValue = -3;
        controllerObject.FindProperty("idleFaceSortingOrder").intValue = -2;
        controllerObject.FindProperty("hideLegacyWormVisual").boolValue = true;
        controllerObject.ApplyModifiedProperties();

        SerializedProperty wormProperty = controllerObject.FindProperty("wormVisual");
        Transform legacyWorm = wormProperty?.objectReferenceValue as Transform;
        int hiddenLegacyWormRenderers = 0;
        if (legacyWorm != null)
        {
            foreach (SpriteRenderer legacyRenderer in legacyWorm.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (legacyRenderer == null)
                {
                    continue;
                }

                Undo.RecordObject(legacyRenderer, "Hide Legacy MotherBird Worm Renderer");
                legacyRenderer.enabled = false;
                EditorUtility.SetDirty(legacyRenderer);
                hiddenLegacyWormRenderers++;
            }
        }

        Undo.RecordObject(spriteVisual, "Show MotherBird Split Idle Visual");
        spriteVisual.enabled = false;
        EditorUtility.SetDirty(spriteVisual);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(activeScene);
        Selection.activeGameObject = controller.gameObject;

        Debug.Log(
            "# Apply Split Idle Mother Parts To Scene Result\n" +
            $"- MotherBird: {GetHierarchyPath(controller.transform)}\n" +
            $"- Body: {GetHierarchyPath(bodyRenderer.transform)} / {GetSpriteReport(bodyRenderer.sprite)} / order={bodyRenderer.sortingOrder}\n" +
            $"- Eye: {GetHierarchyPath(eyeRenderer.transform)} / {GetSpriteReport(eyeRenderer.sprite)} / order={eyeRenderer.sortingOrder}\n" +
            $"- BeakWorm: {GetHierarchyPath(beakWormRenderer.transform)} / {GetSpriteReport(beakWormRenderer.sprite)} / order={beakWormRenderer.sortingOrder}\n" +
            $"- Legacy WormVisual present: {legacyWorm != null}\n" +
            $"- Legacy Worm renderers disabled: {hiddenLegacyWormRenderers}\n" +
            "- Scene saved automatically: No");
    }

    [MenuItem("PeckBeat/Stage01/Birds/Apply New Chick And Feed Success Sprites")]
    private static void ApplyNewChickAndFeedSuccessSprites()
    {
        if (!CanRunEditorTool())
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() ||
            !string.Equals(activeScene.path, Stage01ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"[WARN] Open {Stage01ScenePath} as the active scene before applying the new bird sprites.");
            return;
        }

        NewStage01BirdSprites sprites = FindNewStage01BirdSprites();
        if (sprites.ChickIdle == null || sprites.ChickFed == null ||
            sprites.MotherFeedSuccess == null)
        {
            Debug.LogWarning(
                "[WARN] New Chick Sprite apply aborted because the two new Chick Sprites or Mother feed-success Sprite were not available.");
            return;
        }

        GameObject chickPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ChickPrefabPath);
        MotherBirdSpriteController motherBird = FindMotherBirdController(activeScene);
        if (chickPrefabAsset == null || motherBird == null)
        {
            Debug.LogWarning(
                $"[WARN] Apply aborted. PF_Chick found={chickPrefabAsset != null}, " +
                $"scene MotherBird found={motherBird != null}.");
            return;
        }

        SerializedObject motherObject = new SerializedObject(motherBird);
        SerializedProperty feedSuccess = motherObject.FindProperty("feedSuccess");
        if (!HasSpritePoseShape(feedSuccess))
        {
            Debug.LogError("[FAIL] MotherBirdSpriteController.feedSuccess is missing or is not a valid SpritePose.");
            return;
        }

        Stage01IntroGuideUI introGuide = FindComponentInScene<Stage01IntroGuideUI>(activeScene);
        if (!EditorUtility.DisplayDialog(
                "Apply New Chick And Feed Success Sprites",
                "This will explicitly save the new Chick Idle, Fed, FailedExit, and initial Visual Sprite to PF_Chick.prefab. " +
                "The existing successful Exit Pose will be preserved.\n\n" +
                "The open Stage01_Prototype scene will receive the MotherBird feedSuccess Sprite and, when present, " +
                "the Stage01IntroGuideUI Chick icon. The scene will only be marked dirty and will not be saved.\n\n" +
                "If an exact optional Cuckoo WrongExit Sprite is found, only that PF_Cuckoo Pose will be saved. " +
                "All existing Cuckoo hit and knockaway settings will remain unchanged.",
                "Apply",
                "Cancel"))
        {
            return;
        }

        bool prefabSaved = ApplyNewChickSpritesToPrefab(sprites, out Sprite preservedExitSprite);
        if (!prefabSaved)
        {
            Debug.LogError("[FAIL] Scene changes were not applied because PF_Chick prefab update failed.");
            return;
        }

        bool cuckooPrefabSaved = ApplyCuckooWrongExitToPrefab(sprites.CuckooWrongExit);

        Undo.RecordObject(motherBird, "Apply MotherBird Feed Success Sprite");
        motherObject.UpdateIfRequiredOrScript();
        feedSuccess = motherObject.FindProperty("feedSuccess");
        feedSuccess.FindPropertyRelative("sprite").objectReferenceValue = sprites.MotherFeedSuccess;
        feedSuccess.FindPropertyRelative("positionOffset").vector2Value = Vector2.zero;
        feedSuccess.FindPropertyRelative("scale").vector2Value = Vector2.one;
        motherObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(motherBird);

        string guideResult = "Stage01IntroGuideUI not found; skipped";
        if (introGuide != null)
        {
            SerializedObject guideObject = new SerializedObject(introGuide);
            SerializedProperty chickSmallSprite = guideObject.FindProperty("chickSmallSprite");
            if (chickSmallSprite != null)
            {
                Undo.RecordObject(introGuide, "Apply Stage01 Guide Chick Sprite");
                chickSmallSprite.objectReferenceValue = sprites.ChickIdle;
                guideObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(introGuide);
                guideResult = GetSpriteReport(sprites.ChickIdle);
            }
            else
            {
                guideResult = "chickSmallSprite field missing; skipped";
                Debug.LogWarning("[WARN] Stage01IntroGuideUI.chickSmallSprite was not found.");
            }
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log(
            "# Apply New Chick And Feed Success Sprites Result\n" +
            $"- Chick Idle: {GetSpriteReport(sprites.ChickIdle)}\n" +
            $"- Chick Fed: {GetSpriteReport(sprites.ChickFed)}\n" +
            $"- Chick FailedExit: {GetSpriteReport(sprites.ChickIdle)}\n" +
            $"- Chick Exit kept: {GetSpriteReport(preservedExitSprite)}\n" +
            $"- Mother Feed Success: {GetSpriteReport(sprites.MotherFeedSuccess)}\n" +
            $"- PF_Chick prefab saved: {prefabSaved}\n" +
            $"- PF_Cuckoo WrongExit prefab saved: {cuckooPrefabSaved}\n" +
            "- Stage01 scene changed: Yes\n" +
            $"- Stage01IntroGuideUI chick icon: {guideResult}\n" +
            "- Scene saved automatically: No\n" +
            $"- Cuckoo changed: {(cuckooPrefabSaved ? "WrongExit only" : "No")}\n" +
            "- Stage01FeedingController changed by this menu: No\n" +
            "- MotherBirdSpriteController script changed by this menu: No\n" +
            "- ChickSpriteController script changed by this menu: No");
    }

    [MenuItem("PeckBeat/Stage01/MotherBird/Validate MotherBird Setup")]
    private static void ValidateMotherBirdSetup()
    {
        if (!CanRunEditorTool())
        {
            return;
        }

        Debug.Log("[Stage01 MotherBird Refactor] Validate Start");
        AnalyzePrefab(true);
        ValidateNewChickPrefab();
        ValidateCuckooWrongExitPrefab();
        InspectStage01Scene(true);
        Debug.Log("[Stage01 MotherBird Refactor] Validate Complete");
    }

    [MenuItem("PeckBeat/Stage01/Birds/Apply New Cuckoo And Peck Sprites")]
    private static void ApplyNewCuckooAndPeckSprites()
    {
        if (!CanRunEditorTool())
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() ||
            !string.Equals(activeScene.path, Stage01ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"[WARN] Open {Stage01ScenePath} as the active scene before applying Cuckoo sprites.");
            return;
        }

        MotherBirdSpriteController motherBird = FindMotherBirdController(activeScene);
        Stage01FeedingController feedingController = FindComponentInScene<Stage01FeedingController>(activeScene);
        if (motherBird == null || feedingController == null)
        {
            Debug.LogWarning(
                $"[WARN] Required Stage01 components were not found. MotherBird={motherBird != null}, " +
                $"Stage01FeedingController={feedingController != null}.");
            return;
        }

        NewCuckooAndPeckSprites sprites = FindNewCuckooAndPeckSprites();
        if (!HasAnyCuckooOrPeckSprite(sprites))
        {
            Debug.LogWarning($"[WARN] No exact Sprite files were found in {CuckooTest2SpriteFolder}.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Apply New Cuckoo And Peck Sprites",
                "Apply the exact Test/Test2 Cuckoo pose Sprites to PF_Cuckoo and save that prefab.\n\n" +
                "The open Stage01 scene MotherBird peck1Success Sprite will be updated when its exact Sprite is available. " +
                "The scene will only be marked dirty and will not be saved automatically. Missing Sprites keep their existing values.",
                "Apply",
                "Cancel"))
        {
            return;
        }

        bool cuckooPrefabSaved = ApplyNewCuckooSpritesToPrefab(sprites);
        bool motherBirdChanged = ApplyMotherPeck1SuccessToScene(motherBird, sprites.MotherPeck1Success);
        bool durationChanged = ApplyPeck1SuccessDurationToScene(feedingController);
        if (motherBirdChanged || durationChanged)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }

        Selection.activeObject = motherBird.gameObject;

        Debug.Log(
            "# Apply New Cuckoo And Peck Sprites Result\n" +
            $"- Cuckoo Idle/Exit: {GetSpriteReport(sprites.IdleAndExit)}\n" +
            $"- Cuckoo Fed: {GetSpriteReport(sprites.Fed)}\n" +
            $"- Cuckoo WrongExit: {GetSpriteReport(sprites.WrongExit)}\n" +
            $"- Cuckoo Hit1: {GetSpriteReport(sprites.Hit1)}\n" +
            $"- Cuckoo Hit2: {GetSpriteReport(sprites.Hit2)}\n" +
            $"- Cuckoo PartialHitExit: {GetSpriteReport(sprites.PartialHitExit)}\n" +
            $"- MotherBird Peck1Success: {GetSpriteReport(sprites.MotherPeck1Success)}\n" +
            $"- PF_Cuckoo prefab saved: {cuckooPrefabSaved}\n" +
            $"- PF_Cuckoo recommended root scale: {RecommendedCuckooRootScale:0.##}\n" +
            $"- Stage01 MotherBird changed: {motherBirdChanged}\n" +
            $"- Peck1 Perfect composite duration: {RecommendedPeck1SuccessCompositeDuration:0.##}s / changed={durationChanged}\n" +
            $"- Stable Selection: {GetHierarchyPath(motherBird.transform)}\n" +
            "- Stage01 scene saved automatically: No\n" +
            "- Chick mapping changed: No\n" +
            "- Cuckoo Hit2 Knockaway settings changed: No");
    }

    private static NewCuckooAndPeckSprites FindNewCuckooAndPeckSprites()
    {
        return new NewCuckooAndPeckSprites
        {
            IdleAndExit = LoadExactSpriteFromFolder(CuckooTest2SpriteFolder, "신뻐꾸기기본.png"),
            Fed = LoadExactSpriteFromFolder(CuckooTest2SpriteFolder, "신뻐꾸기먹이받기.png"),
            WrongExit = LoadExactSpriteFromFolder(CuckooTest2SpriteFolder, "신뻐꾸기먹이물고퇴장.png"),
            Hit1 = LoadExactSpriteFromFolder(CuckooTest2SpriteFolder, "신뻐꾸기1타.png"),
            Hit2 = LoadExactSpriteFromFolder(CuckooTest2SpriteFolder, "신뻐꾸기2타.png"),
            PartialHitExit = LoadExactSpriteFromFolder(CuckooTest2SpriteFolder, "1타만 맞은 뻐꾸기.png"),
            MotherPeck1Success = LoadExactSpriteFromFolder(CuckooTest2SpriteFolder, "신엄마새1타대성공.png")
        };
    }

    private static bool HasAnyCuckooOrPeckSprite(NewCuckooAndPeckSprites sprites)
    {
        return sprites != null &&
               (sprites.IdleAndExit != null ||
                sprites.Fed != null ||
                sprites.WrongExit != null ||
                sprites.Hit1 != null ||
                sprites.Hit2 != null ||
                sprites.PartialHitExit != null ||
                sprites.MotherPeck1Success != null);
    }

    private static Sprite LoadExactSpriteFromFolder(string folder, string exactFileName)
    {
        string assetPath = NormalizeAssetPath($"{folder}/{exactFileName}");
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) == null)
        {
            Debug.Log($"[INFO] Exact Sprite was not found; existing value will be kept: {assetPath}");
            return null;
        }

        return LoadBestSpriteAtPath(assetPath, exactFileName);
    }

    private static NewStage01BirdSprites FindNewStage01BirdSprites()
    {
        string[] chickPreferredFolders =
        {
            "Assets/_PeckBeat/Art/Stage_01/Chicks/Test",
            "Assets/_PeckBeat/Art/Stage_01/Chicks"
        };
        string[] motherPreferredFolders =
        {
            "Assets/_PeckBeat/Art/Stage_01/Player/Test",
            "Assets/_PeckBeat/Art/Stage_01/Player"
        };
        string[] cuckooPreferredFolders =
        {
            "Assets/_PeckBeat/Art/Stage_01/Cuckoo/Test",
            "Assets/_PeckBeat/Art/Stage_01/Cuckoo"
        };

        return new NewStage01BirdSprites
        {
            ChickIdle = FindExactNamedSprite("신아기새기본.png", chickPreferredFolders),
            ChickFed = FindExactNamedSprite("신아기새그냥먹이받기.png", chickPreferredFolders),
            MotherFeedSuccess = FindExactNamedSprite("먹이주기대성공.png", motherPreferredFolders),
            CuckooWrongExit = FindFirstAvailableExactSprite(
                cuckooPreferredFolders,
                "뻐꾸기먹이받고퇴장.png",
                "뻐꾸기WrongExit.png",
                "Cuckoo_WrongExit.png")
        };
    }

    private static Sprite LoadExactSplitIdleSprite(string exactFileName)
    {
        string assetPath = NormalizeAssetPath($"{PrimaryTestSpriteFolder}/{exactFileName}");
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) == null)
        {
            Debug.LogWarning($"[WARN] Split Idle Sprite file was not found at the exact path: {assetPath}");
            return null;
        }

        return LoadBestSpriteAtPath(assetPath, exactFileName);
    }

    private static SpriteRenderer GetOrCreateSplitIdleRenderer(
        Transform parent,
        string objectName,
        Sprite sprite,
        SpriteRenderer sourceRenderer,
        int sortingOrder)
    {
        if (parent == null || string.IsNullOrEmpty(objectName) || sprite == null || sourceRenderer == null)
        {
            return null;
        }

        Transform child = parent.Find(objectName);
        GameObject childObject;
        if (child == null)
        {
            childObject = new GameObject(objectName);
            childObject.layer = sourceRenderer.gameObject.layer;
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {objectName}");
            Undo.SetTransformParent(childObject.transform, parent, $"Parent {objectName}");
            child = childObject.transform;
        }
        else
        {
            childObject = child.gameObject;
            Undo.RecordObject(childObject, $"Configure {objectName}");
        }

        if (!childObject.activeSelf)
        {
            Undo.RecordObject(childObject, $"Activate {objectName}");
            childObject.SetActive(true);
        }

        SpriteRenderer renderer = childObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<SpriteRenderer>(childObject);
        }

        Undo.RecordObject(child, $"Reset {objectName} Transform");
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;

        Undo.RecordObject(renderer, $"Configure {objectName} Renderer");
        renderer.sprite = sprite;
        renderer.sharedMaterial = sourceRenderer.sharedMaterial;
        renderer.sortingLayerID = sourceRenderer.sortingLayerID;
        renderer.sortingOrder = sortingOrder;
        renderer.enabled = true;

        EditorUtility.SetDirty(childObject);
        EditorUtility.SetDirty(renderer);
        return renderer;
    }

    private static Sprite FindFirstAvailableExactSprite(
        IEnumerable<string> preferredFolders,
        params string[] exactFileNames)
    {
        foreach (string exactFileName in exactFileNames)
        {
            Sprite sprite = FindExactNamedSprite(exactFileName, preferredFolders);
            if (sprite != null)
            {
                return sprite;
            }
        }

        Debug.Log(
            "[INFO] No optional Sprite candidate was found for: " +
            string.Join(", ", exactFileNames) + ". Runtime fallback will be used.");
        return null;
    }

    private static Sprite FindExactNamedSprite(string exactFileName, IEnumerable<string> preferredFolders)
    {
        foreach (string folder in preferredFolders)
        {
            string preferredPath = NormalizeAssetPath($"{folder}/{exactFileName}");
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(preferredPath) != null)
            {
                return LoadBestSpriteAtPath(preferredPath, exactFileName);
            }
        }

        Sprite stageSprite = FindUniqueExactNamedSpriteInRoot(
            exactFileName,
            "Assets/_PeckBeat/Art/Stage_01",
            out bool foundInStageRoot);
        if (foundInStageRoot)
        {
            return stageSprite;
        }

        return FindUniqueExactNamedSpriteInRoot(exactFileName, "Assets", out _);
    }

    private static Sprite FindUniqueExactNamedSpriteInRoot(
        string exactFileName,
        string searchRoot,
        out bool anyExactMatch)
    {
        string[] matchedPaths = AssetDatabase.FindAssets("t:Texture2D", new[] { searchRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => string.Equals(
                Path.GetFileName(path),
                exactFileName,
                StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        anyExactMatch = matchedPaths.Length > 0;
        if (matchedPaths.Length == 0)
        {
            return null;
        }

        if (matchedPaths.Length > 1)
        {
            Debug.LogWarning(
                $"[WARN] Exact Sprite file match is ambiguous for {exactFileName}; nothing will be applied: " +
                string.Join(", ", matchedPaths));
            return null;
        }

        return LoadBestSpriteAtPath(matchedPaths[0], exactFileName);
    }

    private static Sprite LoadBestSpriteAtPath(string assetPath, string label)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .OrderByDescending(sprite => sprite.rect.width * sprite.rect.height)
            .ToArray();
        if (sprites.Length == 0)
        {
            Debug.LogWarning($"[WARN] {label} has no Sprite sub-asset: {assetPath}");
            return null;
        }

        float largestArea = sprites[0].rect.width * sprites[0].rect.height;
        if (sprites.Count(sprite => Mathf.Approximately(
                sprite.rect.width * sprite.rect.height,
                largestArea)) > 1)
        {
            Debug.LogWarning(
                $"[WARN] {label} has multiple equally large Sprite sub-assets; nothing will be applied: {assetPath}");
            return null;
        }

        Sprite result = sprites[0];
        LogNewSpriteImport(result, label);
        return result;
    }

    private static void LogNewSpriteImport(Sprite sprite, string label)
    {
        if (sprite == null)
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(sprite);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Debug.Log(
            $"[INFO] {label}: path={path}, sprite={sprite.name}, " +
            $"texture={(texture != null ? $"{texture.width}x{texture.height}" : "<missing>")}, " +
            $"PPU={(importer != null ? importer.spritePixelsPerUnit.ToString() : "<unknown>")}, " +
            $"alpha={(importer != null ? importer.alphaIsTransparency.ToString() : "<unknown>")}, " +
            $"mesh={(importer != null ? GetSpriteMeshType(importer).ToString() : "<unknown>")}");
    }

    private static bool ApplyNewChickSpritesToPrefab(
        NewStage01BirdSprites sprites,
        out Sprite preservedExitSprite)
    {
        preservedExitSprite = null;
        GameObject prefabRoot = null;
        try
        {
            prefabRoot = PrefabUtility.LoadPrefabContents(ChickPrefabPath);
            ChickSpriteController controller = prefabRoot != null
                ? prefabRoot.GetComponentInChildren<ChickSpriteController>(true)
                : null;
            if (controller == null)
            {
                Debug.LogError("[FAIL] ChickSpriteController was not found in PF_Chick.");
                return false;
            }

            SerializedObject chickObject = new SerializedObject(controller);
            SerializedProperty idle = chickObject.FindProperty("idle");
            SerializedProperty fed = chickObject.FindProperty("fed");
            SerializedProperty exit = chickObject.FindProperty("exit");
            SerializedProperty failedExit = chickObject.FindProperty("failedExit");
            SerializedProperty rendererProperty = chickObject.FindProperty("spriteRenderer");
            SpriteRenderer spriteRenderer = rendererProperty?.objectReferenceValue as SpriteRenderer;
            if (!HasSpritePoseShape(idle) || !HasSpritePoseShape(fed) || !HasSpritePoseShape(exit) ||
                !HasSpritePoseShape(failedExit) ||
                spriteRenderer == null)
            {
                Debug.LogError("[FAIL] PF_Chick SpritePose fields or Visual SpriteRenderer reference are invalid.");
                return false;
            }

            idle.FindPropertyRelative("sprite").objectReferenceValue = sprites.ChickIdle;
            fed.FindPropertyRelative("sprite").objectReferenceValue = sprites.ChickFed;
            failedExit.FindPropertyRelative("sprite").objectReferenceValue = sprites.ChickIdle;
            preservedExitSprite = exit.FindPropertyRelative("sprite").objectReferenceValue as Sprite;
            chickObject.ApplyModifiedPropertiesWithoutUndo();
            spriteRenderer.sprite = sprites.ChickIdle;
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(spriteRenderer);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, ChickPrefabPath);
            Debug.Log($"[PASS] PF_Chick explicitly saved by menu: {ChickPrefabPath}");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[FAIL] PF_Chick Sprite apply failed: {exception}");
            return false;
        }
        finally
        {
            if (prefabRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static bool ApplyCuckooWrongExitToPrefab(Sprite wrongExitSprite)
    {
        if (wrongExitSprite == null)
        {
            Debug.Log("[INFO] PF_Cuckoo WrongExit was not changed because no optional candidate Sprite was found.");
            return false;
        }

        GameObject prefabRoot = null;
        try
        {
            prefabRoot = PrefabUtility.LoadPrefabContents(CuckooPrefabPath);
            CuckooSpriteController controller = prefabRoot != null
                ? prefabRoot.GetComponentInChildren<CuckooSpriteController>(true)
                : null;
            if (controller == null)
            {
                Debug.LogError("[FAIL] CuckooSpriteController was not found in PF_Cuckoo.");
                return false;
            }

            SerializedObject cuckooObject = new SerializedObject(controller);
            SerializedProperty wrongExit = cuckooObject.FindProperty("wrongExit");
            if (!HasSpritePoseShape(wrongExit))
            {
                Debug.LogError("[FAIL] PF_Cuckoo wrongExit is missing or is not a valid SpritePose.");
                return false;
            }

            wrongExit.FindPropertyRelative("sprite").objectReferenceValue = wrongExitSprite;
            wrongExit.FindPropertyRelative("positionOffset").vector2Value = Vector2.zero;
            wrongExit.FindPropertyRelative("scale").vector2Value = Vector2.one;
            cuckooObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, CuckooPrefabPath);
            Debug.Log($"[PASS] PF_Cuckoo WrongExit explicitly saved by menu: {GetSpriteReport(wrongExitSprite)}");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[FAIL] PF_Cuckoo WrongExit apply failed: {exception}");
            return false;
        }
        finally
        {
            if (prefabRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static bool ApplyNewCuckooSpritesToPrefab(NewCuckooAndPeckSprites sprites)
    {
        if (sprites == null)
        {
            return false;
        }

        GameObject prefabRoot = null;
        try
        {
            prefabRoot = PrefabUtility.LoadPrefabContents(CuckooPrefabPath);
            CuckooSpriteController controller = prefabRoot != null
                ? prefabRoot.GetComponentInChildren<CuckooSpriteController>(true)
                : null;
            if (controller == null)
            {
                Debug.LogError("[FAIL] CuckooSpriteController was not found in PF_Cuckoo.");
                return false;
            }

            SerializedObject cuckooObject = new SerializedObject(controller);
            bool changed = false;
            Vector3 recommendedRootScale = new Vector3(
                RecommendedCuckooRootScale,
                RecommendedCuckooRootScale,
                1f);
            if ((prefabRoot.transform.localScale - recommendedRootScale).sqrMagnitude > 0.000001f)
            {
                prefabRoot.transform.localScale = recommendedRootScale;
                EditorUtility.SetDirty(prefabRoot.transform);
                changed = true;
            }

            changed |= ApplyPoseSpriteIfAvailable(cuckooObject.FindProperty("idle"), sprites.IdleAndExit, "Cuckoo Idle");
            changed |= ApplyPoseSpriteIfAvailable(cuckooObject.FindProperty("exit"), sprites.IdleAndExit, "Cuckoo Exit");
            changed |= ApplyPoseSpriteIfAvailable(cuckooObject.FindProperty("fed"), sprites.Fed, "Cuckoo Fed");
            changed |= ApplyPoseSpriteIfAvailable(cuckooObject.FindProperty("wrongExit"), sprites.WrongExit, "Cuckoo WrongExit");
            changed |= ApplyPoseSpriteIfAvailable(cuckooObject.FindProperty("hit1"), sprites.Hit1, "Cuckoo Hit1");
            changed |= ApplyPoseSpriteIfAvailable(cuckooObject.FindProperty("hit2"), sprites.Hit2, "Cuckoo Hit2");
            changed |= ApplyPoseSpriteIfAvailable(
                cuckooObject.FindProperty("partialHitExit"),
                sprites.PartialHitExit,
                "Cuckoo PartialHitExit");

            SerializedProperty rendererProperty = cuckooObject.FindProperty("spriteRenderer");
            SpriteRenderer spriteRenderer = rendererProperty?.objectReferenceValue as SpriteRenderer;
            if (sprites.IdleAndExit != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = sprites.IdleAndExit;
                EditorUtility.SetDirty(spriteRenderer);
                changed = true;
            }
            else if (spriteRenderer == null)
            {
                Debug.LogWarning("[WARN] PF_Cuckoo SpriteRenderer reference is missing; initial Sprite was not changed.");
            }

            if (!changed)
            {
                Debug.Log("[INFO] PF_Cuckoo had no applicable non-null Sprite changes; prefab was not saved.");
                return false;
            }

            cuckooObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, CuckooPrefabPath);
            Debug.Log(
                $"[PASS] PF_Cuckoo explicitly saved by menu: {CuckooPrefabPath} / " +
                $"rootScale={prefabRoot.transform.localScale}");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[FAIL] PF_Cuckoo Sprite mapping apply failed: {exception}");
            return false;
        }
        finally
        {
            if (prefabRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static bool ApplyMotherPeck1SuccessToScene(
        MotherBirdSpriteController motherBird,
        Sprite peck1SuccessSprite)
    {
        if (motherBird == null || peck1SuccessSprite == null)
        {
            if (peck1SuccessSprite == null)
            {
                Debug.Log("[INFO] MotherBird peck1Success was not changed because its exact Sprite is missing.");
            }

            return false;
        }

        SerializedObject motherObject = new SerializedObject(motherBird);
        SerializedProperty peck1Success = motherObject.FindProperty("peck1Success");
        if (!HasSpritePoseShape(peck1Success))
        {
            Debug.LogError("[FAIL] MotherBirdSpriteController.peck1Success is missing or invalid.");
            return false;
        }

        Undo.RecordObject(motherBird, "Apply MotherBird Peck1 Success Sprite");
        peck1Success.FindPropertyRelative("sprite").objectReferenceValue = peck1SuccessSprite;
        peck1Success.FindPropertyRelative("positionOffset").vector2Value = Vector2.zero;
        peck1Success.FindPropertyRelative("scale").vector2Value = Vector2.one;
        motherObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(motherBird);
        return true;
    }

    private static bool ApplyPeck1SuccessDurationToScene(Stage01FeedingController feedingController)
    {
        if (feedingController == null)
        {
            return false;
        }

        SerializedObject feedingObject = new SerializedObject(feedingController);
        SerializedProperty durationProperty = feedingObject.FindProperty("peck1SuccessCompositeDuration");
        if (durationProperty == null)
        {
            Debug.LogError("[FAIL] Stage01FeedingController.peck1SuccessCompositeDuration is missing.");
            return false;
        }

        if (Mathf.Approximately(durationProperty.floatValue, RecommendedPeck1SuccessCompositeDuration))
        {
            return false;
        }

        Undo.RecordObject(feedingController, "Set Cuckoo Peck1 Perfect Duration");
        durationProperty.floatValue = RecommendedPeck1SuccessCompositeDuration;
        feedingObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(feedingController);
        return true;
    }

    private static bool ApplyPoseSpriteIfAvailable(
        SerializedProperty pose,
        Sprite sprite,
        string label)
    {
        if (sprite == null)
        {
            Debug.Log($"[INFO] {label} Sprite is missing; existing value is preserved.");
            return false;
        }

        if (!HasSpritePoseShape(pose))
        {
            Debug.LogError($"[FAIL] {label} SpritePose field is missing or invalid; existing value is preserved.");
            return false;
        }

        pose.FindPropertyRelative("sprite").objectReferenceValue = sprite;
        pose.FindPropertyRelative("positionOffset").vector2Value = Vector2.zero;
        pose.FindPropertyRelative("scale").vector2Value = Vector2.one;
        return true;
    }

    private static bool HasSpritePoseShape(SerializedProperty pose)
    {
        return pose != null &&
               pose.FindPropertyRelative("sprite") != null &&
               pose.FindPropertyRelative("positionOffset") != null &&
               pose.FindPropertyRelative("scale") != null &&
               pose.FindPropertyRelative("flipX") != null;
    }

    private static string GetSpriteReport(Sprite sprite)
    {
        return sprite != null
            ? $"{sprite.name} ({AssetDatabase.GetAssetPath(sprite)})"
            : "<missing/null>";
    }

    private static string GetSpriteReportWithRect(Sprite sprite)
    {
        return sprite != null
            ? $"{GetSpriteReport(sprite)} / rect={sprite.rect.width:0.#}x{sprite.rect.height:0.#}"
            : "<missing/null>";
    }

    private static void ValidateNewChickPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChickPrefabPath);
        ChickSpriteController controller = prefab != null
            ? prefab.GetComponentInChildren<ChickSpriteController>(true)
            : null;
        if (controller == null)
        {
            Debug.LogError($"[FAIL] PF_Chick or ChickSpriteController is missing: {ChickPrefabPath}");
            return;
        }

        SerializedObject chickObject = new SerializedObject(controller);
        ValidateExactPoseSprite(chickObject, "idle", "신아기새기본.png", "PF_Chick Idle");
        ValidateExactPoseSprite(chickObject, "fed", "신아기새그냥먹이받기.png", "PF_Chick Fed");
        ValidateExactPoseSprite(chickObject, "exit", "아기새퇴장.png", "PF_Chick Exit");

        SerializedProperty failedExit = chickObject.FindProperty("failedExit");
        Sprite failedExitSprite = failedExit?.FindPropertyRelative("sprite")?.objectReferenceValue as Sprite;
        if (failedExit == null)
        {
            Debug.LogWarning("[WARN] PF_Chick failedExit Pose field is missing; runtime Exit fallback cannot be configured.");
        }
        else
        {
            LogCheck(
                IsExactSpriteFile(failedExitSprite, "신아기새기본.png"),
                "PF_Chick FailedExit uses 신아기새기본.png",
                $"PF_Chick FailedExit does not use 신아기새기본.png: {GetSpriteReport(failedExitSprite)}");
        }

        SpriteRenderer renderer = chickObject.FindProperty("spriteRenderer")?.objectReferenceValue as SpriteRenderer;
        LogCheck(
            renderer != null && IsExactSpriteFile(renderer.sprite, "신아기새기본.png"),
            "PF_Chick Visual initial Sprite is 신아기새기본.png",
            renderer == null
                ? "PF_Chick Visual SpriteRenderer reference is missing"
                : $"PF_Chick Visual initial Sprite is {GetSpriteReport(renderer.sprite)}");

        LogCheck(
            typeof(ChickSpriteController).GetMethod("SetVisualVisible") != null,
            "Chick Visual visibility API exists",
            "ChickSpriteController.SetVisualVisible is missing");
        Debug.Log("[PASS] Chick prefab validation is read-only and does not alter PF_Cuckoo.");
    }

    private static void ValidateCuckooWrongExitPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CuckooPrefabPath);
        CuckooSpriteController controller = prefab != null
            ? prefab.GetComponentInChildren<CuckooSpriteController>(true)
            : null;
        if (controller == null)
        {
            Debug.LogError($"[FAIL] PF_Cuckoo or CuckooSpriteController is missing: {CuckooPrefabPath}");
            return;
        }

        SerializedObject cuckooObject = new SerializedObject(controller);
        SpriteRenderer visualRenderer =
            cuckooObject.FindProperty("spriteRenderer")?.objectReferenceValue as SpriteRenderer;
        Vector3 rootScale = controller.transform.localScale;
        Vector3 visualScale = visualRenderer != null
            ? visualRenderer.transform.localScale
            : Vector3.zero;
        Debug.Log(
            $"[INFO] PF_Cuckoo root scale={rootScale}, " +
            $"Visual scale={(visualRenderer != null ? visualScale.ToString() : "<missing>")}");
        LogCheck(
            Mathf.Approximately(rootScale.x, RecommendedCuckooRootScale) &&
            Mathf.Approximately(rootScale.y, RecommendedCuckooRootScale),
            $"PF_Cuckoo root scale uses the recommended {RecommendedCuckooRootScale:0.##}",
            $"PF_Cuckoo root scale is {rootScale}; run Apply New Cuckoo And Peck Sprites to use " +
            $"{RecommendedCuckooRootScale:0.##}");
        LogCheck(
            visualRenderer != null && IsNearOne(visualScale),
            "PF_Cuckoo Visual localScale is one",
            visualRenderer == null
                ? "PF_Cuckoo Visual SpriteRenderer is missing"
                : $"PF_Cuckoo Visual localScale is {visualScale}");

        string[] poseNames =
        {
            "idle",
            "fed",
            "hit1",
            "hit2",
            "exit",
            "wrongExit",
            "partialHitExit"
        };
        foreach (string poseName in poseNames)
        {
            SerializedProperty pose = cuckooObject.FindProperty(poseName);
            Sprite poseSprite = pose?.FindPropertyRelative("sprite")?.objectReferenceValue as Sprite;
            Debug.Log($"[INFO] PF_Cuckoo {poseName}: {GetSpriteReportWithRect(poseSprite)}");
        }

        ValidateExactPoseSprite(cuckooObject, "idle", "신뻐꾸기기본.png", "PF_Cuckoo Idle");
        ValidateExactPoseSprite(cuckooObject, "fed", "신뻐꾸기먹이받기.png", "PF_Cuckoo Fed/WrongFeed");
        ValidateExactPoseSprite(cuckooObject, "hit1", "신뻐꾸기1타.png", "PF_Cuckoo Hit1");
        ValidateExactPoseSprite(cuckooObject, "hit2", "신뻐꾸기2타.png", "PF_Cuckoo Hit2");
        ValidateExactPoseSprite(cuckooObject, "exit", "신뻐꾸기기본.png", "PF_Cuckoo normal Exit");
        ValidateExactPoseSprite(
            cuckooObject,
            "wrongExit",
            "신뻐꾸기먹이물고퇴장.png",
            "PF_Cuckoo WrongExit");
        ValidateExactPoseSprite(
            cuckooObject,
            "partialHitExit",
            "1타만 맞은 뻐꾸기.png",
            "PF_Cuckoo PartialHitExit");

        SpriteRenderer renderer = visualRenderer;
        LogCheck(
            renderer != null && IsExactSpriteFile(renderer.sprite, "신뻐꾸기기본.png"),
            "PF_Cuckoo initial Visual uses 신뻐꾸기기본.png",
            renderer == null
                ? "PF_Cuckoo SpriteRenderer reference is missing"
                : $"PF_Cuckoo initial Visual uses {GetSpriteReport(renderer.sprite)}");
        LogCheck(
            typeof(CuckooSpriteController).GetMethod("SetVisualVisible") != null,
            "Cuckoo visibility API exists for the Peck1 Perfect composite",
            "CuckooSpriteController.SetVisualVisible is missing");
        LogCheck(
            typeof(CuckooSpriteController).GetMethod("PlayPartialHitExit") != null,
            "Cuckoo PartialHitExit API exists for a missed second peck",
            "CuckooSpriteController.PlayPartialHitExit is missing");
        LogCheck(
            typeof(CuckooSpriteController).GetMethod("PlayKnockaway") != null,
            "Cuckoo Hit2 Knockaway API remains available",
            "CuckooSpriteController.PlayKnockaway is missing");
    }

    private static void ValidateExactPoseSprite(
        SerializedObject serializedObject,
        string poseName,
        string expectedFileName,
        string label)
    {
        SerializedProperty pose = serializedObject.FindProperty(poseName);
        Sprite sprite = pose?.FindPropertyRelative("sprite")?.objectReferenceValue as Sprite;
        LogCheck(
            IsExactSpriteFile(sprite, expectedFileName),
            $"{label} uses {expectedFileName}",
            $"{label} does not use {expectedFileName}: {GetSpriteReport(sprite)}");
    }

    private static bool IsExactSpriteFile(Sprite sprite, string expectedFileName)
    {
        return sprite != null && string.Equals(
            Path.GetFileName(AssetDatabase.GetAssetPath(sprite)),
            expectedFileName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanRunEditorTool()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return true;
        }

        Debug.LogWarning("[WARN] Stage01 MotherBird tools are disabled while entering or running Play Mode.");
        return false;
    }

    private static void AnalyzePrefab(bool validate)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MotherBirdPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[FAIL] PF_MotherBird not found: {MotherBirdPrefabPath}");
            return;
        }

        Debug.Log($"[PASS] PF_MotherBird found: {MotherBirdPrefabPath}");
        MotherBirdSpriteController controller = prefab.GetComponentInChildren<MotherBirdSpriteController>(true);
        if (controller == null)
        {
            Debug.LogError("[FAIL] MotherBirdSpriteController was not found in the prefab.");
            return;
        }

        Debug.Log($"[PASS] Controller: {GetHierarchyPath(controller.transform)} / {controller.GetType().Name}");
        Debug.Log($"[INFO] Prefab root scale: {prefab.transform.localScale}");

        SerializedObject serializedController = new SerializedObject(controller);
        ValidatePosePropertyShape(serializedController, false);

        SerializedProperty rendererProperty = serializedController.FindProperty("spriteRenderer");
        SpriteRenderer spriteRenderer = rendererProperty != null
            ? rendererProperty.objectReferenceValue as SpriteRenderer
            : null;

        if (spriteRenderer == null)
        {
            Debug.LogError("[FAIL] SpriteRenderer reference is missing.");
        }
        else
        {
            Debug.Log(
                $"[INFO] Visual={GetHierarchyPath(spriteRenderer.transform)}, enabled={spriteRenderer.enabled}, " +
                $"localPosition={spriteRenderer.transform.localPosition}, localScale={spriteRenderer.transform.localScale}, " +
                $"sortingLayer={spriteRenderer.sortingLayerName}, order={spriteRenderer.sortingOrder}, " +
                $"material={GetObjectName(spriteRenderer.sharedMaterial)}, initialSprite={GetObjectName(spriteRenderer.sprite)}");
        }

        foreach (PoseSpec spec in PoseSpecs)
        {
            ReportPose(serializedController, spec, validate);
        }

        if (validate && spriteRenderer != null)
        {
            SerializedProperty idle = serializedController.FindProperty("idle");
            Sprite idleSprite = idle != null
                ? idle.FindPropertyRelative("sprite")?.objectReferenceValue as Sprite
                : null;
            LogCheck(spriteRenderer.sprite != null && spriteRenderer.sprite == idleSprite,
                "SpriteRenderer initial sprite matches Idle",
                spriteRenderer.sprite == null ? "initial sprite is null" : "initial sprite differs from Idle");
            LogCheck(IsNearZero(spriteRenderer.transform.localPosition),
                "Visual localPosition is zero",
                $"Visual localPosition={spriteRenderer.transform.localPosition}");
            LogCheck(IsNearOne(spriteRenderer.transform.localScale),
                "Visual localScale is one",
                $"Visual localScale={spriteRenderer.transform.localScale}");
        }
    }

    private static bool ValidatePosePropertyShape(SerializedObject serializedController, bool failFast)
    {
        bool valid = true;
        foreach (PoseSpec spec in PoseSpecs)
        {
            SerializedProperty pose = serializedController.FindProperty(spec.PropertyName);
            if (pose == null)
            {
                if (spec.Required)
                {
                    Debug.LogError($"[FAIL] Required pose field not found: {spec.PropertyName}");
                    valid = false;
                }
                else if (!failFast)
                {
                    Debug.LogWarning($"[WARN] Optional pose field not found: {spec.PropertyName}");
                }

                continue;
            }

            bool hasSprite = pose.FindPropertyRelative("sprite") != null;
            bool hasOffset = pose.FindPropertyRelative("positionOffset") != null;
            bool hasScale = pose.FindPropertyRelative("scale") != null;
            bool hasFlip = pose.FindPropertyRelative("flipX") != null;
            if (!hasSprite || !hasOffset || !hasScale || !hasFlip)
            {
                Debug.LogError(
                    $"[FAIL] SpritePose shape mismatch at {spec.PropertyName}: " +
                    $"sprite={hasSprite}, positionOffset={hasOffset}, scale={hasScale}, flipX={hasFlip}");
                valid = false;
            }
        }

        return valid;
    }

    private static void ReportPose(SerializedObject serializedController, PoseSpec spec, bool validate)
    {
        SerializedProperty pose = serializedController.FindProperty(spec.PropertyName);
        if (pose == null)
        {
            Debug.LogWarning($"[WARN] Pose field absent: {spec.Label} ({spec.PropertyName})");
            return;
        }

        SerializedProperty spriteProperty = pose.FindPropertyRelative("sprite");
        SerializedProperty offsetProperty = pose.FindPropertyRelative("positionOffset");
        SerializedProperty scaleProperty = pose.FindPropertyRelative("scale");
        SerializedProperty flipProperty = pose.FindPropertyRelative("flipX");
        if (spriteProperty == null || offsetProperty == null || scaleProperty == null || flipProperty == null)
        {
            Debug.LogError($"[FAIL] Invalid SpritePose structure: {spec.PropertyName}");
            return;
        }

        Sprite sprite = spriteProperty.objectReferenceValue as Sprite;
        Vector2 offset = offsetProperty.vector2Value;
        Vector2 scale = scaleProperty.vector2Value;
        Debug.Log(
            $"[INFO] Pose {spec.Label}: sprite={GetObjectName(sprite)}, offset={offset}, " +
            $"scale={scale}, flipX={flipProperty.boolValue}");

        if (!validate)
        {
            return;
        }

        if (sprite == null)
        {
            if (spec.Required)
            {
                Debug.LogError($"[FAIL] Pose {spec.Label} sprite is missing/null.");
            }
            else
            {
                Debug.Log(
                    $"[INFO] Optional Pose {spec.Label} Sprite is missing/null. " +
                    "The corresponding base Pose fallback will be used.");
            }
        }
        else
        {
            ValidateSprite(sprite, spec.Label);
        }

        LogCheck(offset.magnitude <= 0.2f,
            $"Pose {spec.Label} offset is near zero",
            $"Pose {spec.Label} has large offset: {offset}");
        LogCheck(scale.x >= 0.95f && scale.x <= 1.05f && scale.y >= 0.95f && scale.y <= 1.05f,
            $"Pose {spec.Label} scale is near one",
            $"Pose {spec.Label} scale is outside 0.95-1.05: {scale}");
    }

    private static void InspectStage01Scene(bool validate)
    {
        Scene scene = SceneManager.GetSceneByPath(Stage01ScenePath);
        bool openedByTool = !scene.IsValid() || !scene.isLoaded;

        try
        {
            if (openedByTool)
            {
                scene = EditorSceneManager.OpenScene(Stage01ScenePath, OpenSceneMode.Additive);
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError($"[FAIL] Could not inspect Stage01 scene: {Stage01ScenePath}");
                return;
            }

            MotherBirdSpriteController controller = FindMotherBirdController(scene);
            if (controller == null)
            {
                Debug.LogError("[FAIL] MotherBird instance was not found in Stage01_Prototype.");
                return;
            }

            GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(controller.gameObject);
            Transform rootTransform = instanceRoot != null ? instanceRoot.transform : controller.transform;
            Debug.Log(
                $"[PASS] Scene MotherBird: {GetHierarchyPath(rootTransform)} / " +
                $"position={rootTransform.localPosition}, scale={rootTransform.localScale}");

            PropertyModification[] modifications = instanceRoot != null
                ? PrefabUtility.GetPropertyModifications(instanceRoot)
                : null;
            int modificationCount = modifications != null ? modifications.Length : 0;
            MotherBirdSpriteController sourceController =
                PrefabUtility.GetCorrespondingObjectFromSource(controller);
            List<PropertyModification> poseOverrides = instanceRoot != null && sourceController != null
                ? GetPoseOverrideModifications(instanceRoot, sourceController)
                : new List<PropertyModification>();
            Debug.Log($"[INFO] MotherBird prefab property modifications: {modificationCount}");
            if (modifications != null)
            {
                foreach (PropertyModification modification in modifications)
                {
                    if (modification == null)
                    {
                        continue;
                    }

                    string objectName = modification.target != null ? modification.target.name : "<missing target>";
                    string objectValue = modification.objectReference != null
                        ? modification.objectReference.name
                        : modification.value;
                    Debug.Log($"[INFO] Override {objectName}.{modification.propertyPath} = {objectValue}");
                }

                if (poseOverrides.Count > 0)
                {
                    Debug.LogWarning(
                        $"[WARN] The Stage01 MotherBird instance still has {poseOverrides.Count} SpritePose override " +
                        "properties. They can override unified prefab values.");

                    if (validate)
                    {
                        foreach (PropertyModification poseOverride in poseOverrides)
                        {
                            string objectValue = poseOverride.objectReference != null
                                ? poseOverride.objectReference.name
                                : poseOverride.value;
                            Debug.LogWarning(
                                $"[WARN] Remaining MotherBird Pose override: " +
                                $"{poseOverride.propertyPath} = {objectValue}");
                        }
                    }
                }
                else if (validate)
                {
                    Debug.Log("[PASS] No MotherBird SpritePose overrides remain in the Stage01 scene instance.");
                }
            }

            Stage01FeedingController feedingController = FindComponentInScene<Stage01FeedingController>(scene);
            MotherBirdSpriteController referencedMotherBird = null;
            if (feedingController != null)
            {
                SerializedObject feedingObject = new SerializedObject(feedingController);
                SerializedProperty motherBirdProperty = feedingObject.FindProperty("motherBird");
                referencedMotherBird = motherBirdProperty != null
                    ? motherBirdProperty.objectReferenceValue as MotherBirdSpriteController
                    : null;
            }

            if (validate)
            {
                ValidateScenePoseValues(controller);
                ValidateSceneVisualAndWorm(controller);
                ValidateNewBirdSetupInScene(scene, controller);

                bool rootScaleRecommended =
                    rootTransform.localScale.x >= 0.8f && rootTransform.localScale.x <= 1f &&
                    rootTransform.localScale.y >= 0.8f && rootTransform.localScale.y <= 1f;
                LogCheck(rootScaleRecommended,
                    "Scene MotherBird root scale is within 0.8-1.0",
                    $"Scene MotherBird root scale is outside 0.8-1.0: {rootTransform.localScale}");
                LogCheck(referencedMotherBird == controller,
                    "Stage01FeedingController MotherBird reference is valid",
                    "Stage01FeedingController MotherBird reference is missing or points elsewhere");

                if (modificationCount > 10)
                {
                    Debug.LogWarning(
                        $"[WARN] MotherBird instance has many prefab property modifications ({modificationCount}). " +
                        "Review Pose overrides after applying the unified prefab.");
                }
                else
                {
                    Debug.Log($"[PASS] MotherBird instance override count is limited: {modificationCount}");
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[FAIL] Stage01 scene inspection failed: {exception}");
        }
        finally
        {
            if (openedByTool && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private static MotherBirdSpriteController FindMotherBirdController(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (!string.Equals(root.name, "GamePlayRoot", StringComparison.Ordinal))
            {
                continue;
            }

            Transform motherBird = root.transform.Find("MotherBird");
            MotherBirdSpriteController pathController = motherBird != null
                ? motherBird.GetComponentInChildren<MotherBirdSpriteController>(true)
                : null;
            if (pathController != null)
            {
                return pathController;
            }
        }

        return FindComponentInScene<MotherBirdSpriteController>(scene);
    }

    private static string ResolveTestSpriteFolder()
    {
        if (AssetDatabase.IsValidFolder(PrimaryTestSpriteFolder))
        {
            Debug.Log($"[PASS] MotherBird Test Sprite folder: {PrimaryTestSpriteFolder}");
            return PrimaryTestSpriteFolder;
        }

        Debug.LogWarning($"[WARN] Primary MotherBird Test folder is missing: {PrimaryTestSpriteFolder}");
        if (AssetDatabase.IsValidFolder(SecondaryTestSpriteFolder))
        {
            Debug.LogWarning($"[WARN] Using fallback Player/Test folder: {SecondaryTestSpriteFolder}");
            return SecondaryTestSpriteFolder;
        }

        string[] discoveredTestFolders = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrEmpty(path) &&
                           NormalizeAssetPath(path).IndexOf("/Player/Test/", StringComparison.OrdinalIgnoreCase) >= 0)
            .Select(path => NormalizeAssetPath(Path.GetDirectoryName(path)))
            .Where(path => !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (discoveredTestFolders.Length == 1)
        {
            Debug.LogWarning($"[WARN] Using discovered Player/Test folder: {discoveredTestFolders[0]}");
            return discoveredTestFolders[0];
        }

        if (discoveredTestFolders.Length > 1)
        {
            Debug.LogWarning(
                "[WARN] Multiple Player/Test folders were found; automatic Test Sprite selection was aborted: " +
                string.Join(", ", discoveredTestFolders));
            return null;
        }

        if (AssetDatabase.IsValidFolder(BroadPlayerFolder))
        {
            string[] broadCandidates = AssetDatabase.FindAssets("t:Texture2D", new[] { BroadPlayerFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path) && LooksLikeTestMotherSpriteName(path))
                .ToArray();
            Debug.LogWarning(
                $"[WARN] No Player/Test folder was found. Broader Player candidates are report-only and will not " +
                $"be applied automatically: {string.Join(", ", broadCandidates)}");
        }

        return null;
    }

    private static TestMotherSprites FindTestMotherSprites(string testFolder)
    {
        string[] texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { testFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrEmpty(path) &&
                           string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new TestMotherSprites
        {
            Feed = FindUniqueTestSprite(
                "Feed",
                texturePaths,
                fileName => fileName.Contains("엄마새먹이주기")),
            Peck1 = FindUniqueTestSprite(
                "Peck1",
                texturePaths,
                fileName => fileName.Contains("빡친엄마새") && !fileName.Contains("2타")),
            Peck2 = FindUniqueTestSprite(
                "Peck2",
                texturePaths,
                fileName => fileName.Contains("빡친엄마새2타")),
            Wrong = FindUniqueTestSprite(
                "WrongFeed/WrongPeck",
                texturePaths,
                fileName => fileName.Contains("먹이잘못주기"))
        };
    }

    private static Sprite FindUniqueTestSprite(
        string label,
        IEnumerable<string> texturePaths,
        Func<string, bool> fileNameMatch)
    {
        string[] matchedPaths = texturePaths
            .Where(path => fileNameMatch(Path.GetFileNameWithoutExtension(path)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (matchedPaths.Length == 0)
        {
            Debug.LogWarning($"[WARN] Test Sprite file was not found for {label}; the existing Sprite will be kept.");
            return null;
        }

        if (matchedPaths.Length > 1)
        {
            Debug.LogWarning(
                $"[WARN] Test Sprite file match is ambiguous for {label}; the existing Sprite will be kept: " +
                string.Join(", ", matchedPaths));
            return null;
        }

        string matchedPath = matchedPaths[0];
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(matchedPath)
            .OfType<Sprite>()
            .OrderByDescending(sprite => sprite.rect.width * sprite.rect.height)
            .ToArray();
        if (sprites.Length == 0)
        {
            Debug.LogWarning(
                $"[WARN] No Sprite sub-asset was found in {matchedPath} for {label}; the existing Sprite will be kept.");
            return null;
        }

        float largestArea = sprites[0].rect.width * sprites[0].rect.height;
        int largestCount = sprites.Count(sprite =>
            Mathf.Approximately(sprite.rect.width * sprite.rect.height, largestArea));
        if (largestCount > 1)
        {
            Debug.LogWarning(
                $"[WARN] Multiple equally large Sprite sub-assets were found in {matchedPath} for {label}; " +
                "the existing Sprite will be kept.");
            return null;
        }

        Sprite result = sprites[0];
        Debug.Log(
            $"[PASS] Test {label} Sprite matched: {matchedPath} / {result.name} / " +
            $"rect={result.rect.width}x{result.rect.height}");
        return result;
    }

    private static void ApplyScenePose(
        SerializedObject serializedController,
        string poseName,
        Sprite testSprite,
        bool replaceSprite)
    {
        if (serializedController == null)
        {
            return;
        }

        SerializedProperty pose = serializedController.FindProperty(poseName);
        SerializedProperty spriteProperty = pose?.FindPropertyRelative("sprite");
        SerializedProperty offsetProperty = pose?.FindPropertyRelative("positionOffset");
        SerializedProperty scaleProperty = pose?.FindPropertyRelative("scale");
        if (spriteProperty == null || offsetProperty == null || scaleProperty == null)
        {
            Debug.LogWarning($"[WARN] Scene Pose could not be updated: {poseName}");
            return;
        }

        if (replaceSprite)
        {
            if (testSprite != null)
            {
                spriteProperty.objectReferenceValue = testSprite;
            }
            else
            {
                Debug.LogWarning($"[WARN] Test Sprite is missing for {poseName}; the existing Sprite is preserved.");
            }
        }

        offsetProperty.vector2Value = Vector2.zero;
        scaleProperty.vector2Value = Vector2.one;
    }

    private static Sprite GetPoseSprite(SerializedObject serializedController, string poseName)
    {
        if (serializedController == null)
        {
            return null;
        }

        SerializedProperty pose = serializedController.FindProperty(poseName);
        SerializedProperty spriteProperty = pose?.FindPropertyRelative("sprite");
        return spriteProperty?.objectReferenceValue as Sprite;
    }

    private static SpriteRenderer GetControllerSpriteRenderer(MotherBirdSpriteController controller)
    {
        if (controller == null)
        {
            return null;
        }

        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty rendererProperty = serializedController.FindProperty("spriteRenderer");
        SpriteRenderer spriteRenderer = rendererProperty?.objectReferenceValue as SpriteRenderer;
        if (spriteRenderer != null)
        {
            return spriteRenderer;
        }

        Transform spriteVisual = controller.transform.Find("BeatMoveRoot/BeatSquashRoot/SpriteVisual");
        spriteRenderer = spriteVisual != null ? spriteVisual.GetComponent<SpriteRenderer>() : null;
        return spriteRenderer != null
            ? spriteRenderer
            : controller.GetComponentInChildren<SpriteRenderer>(true);
    }

    private static string LogWormInfo(MotherBirdSpriteController controller, string context)
    {
        if (controller == null)
        {
            Debug.LogWarning($"[WARN] {context} Worm info unavailable because MotherBird controller is missing.");
            return "<missing controller>";
        }

        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty wormProperty = serializedController.FindProperty("wormVisual");
        Transform wormTransform = wormProperty?.objectReferenceValue as Transform;
        if (wormTransform == null)
        {
            Debug.LogWarning($"[WARN] {context} MotherBird wormVisual reference is null.");
            return "wormVisual=<missing/null>";
        }

        SpriteRenderer wormRenderer = wormTransform.GetComponentInChildren<SpriteRenderer>(true);
        string rendererInfo = wormRenderer == null
            ? "SpriteRenderer=<missing>"
            : $"SpriteRenderer.enabled={wormRenderer.enabled}, sprite={GetObjectName(wormRenderer.sprite)}";
        string info =
            $"name={wormTransform.name}, path={GetHierarchyPath(wormTransform)}, " +
            $"localPosition={wormTransform.localPosition}, localScale={wormTransform.localScale}, {rendererInfo}";
        Debug.Log($"[INFO] {context} Worm: {info}");

        string[] wormPositionFields =
        {
            "wormIdlePosition",
            "wormFeedPosition",
            "wormPeck1Position",
            "wormPeck2Position",
            "wormWrongFeedPosition",
            "wormWrongPeckPosition"
        };
        foreach (string fieldName in wormPositionFields)
        {
            SerializedProperty positionProperty = serializedController.FindProperty(fieldName);
            if (positionProperty != null)
            {
                Debug.Log($"[INFO] {context} {fieldName}={positionProperty.vector2Value}");
            }
        }

        return info;
    }

    private static bool LooksLikeTestMotherSpriteName(string path)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);
        return fileName.Contains("엄마새먹이주기") ||
               fileName.Contains("빡친엄마새") ||
               fileName.Contains("먹이잘못주기");
    }

    private static bool IsSpriteFromPlayerTestFolder(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        string path = NormalizeAssetPath(AssetDatabase.GetAssetPath(sprite));
        return path.IndexOf("/Player/Test/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string NormalizeAssetPath(string path)
    {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
    }

    private static List<SpriteCandidate> FindMotherBirdSpriteCandidates()
    {
        string[] roots = SpriteSearchRoots.Where(AssetDatabase.IsValidFolder).Distinct().ToArray();
        var candidates = new List<SpriteCandidate>();
        var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", roots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !visitedPaths.Add(path) || !LooksLikeMotherBirdArt(path))
            {
                continue;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            Sprite[] sprites = assets.OfType<Sprite>().ToArray();
            if (sprites.Length == 0)
            {
                Sprite singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (singleSprite != null)
                {
                    sprites = new[] { singleSprite };
                }
            }

            foreach (Sprite sprite in sprites)
            {
                candidates.Add(new SpriteCandidate
                {
                    Sprite = sprite,
                    Path = path,
                    Guid = guid,
                    FileName = Path.GetFileNameWithoutExtension(path),
                    Importer = importer,
                    Width = texture != null ? texture.width : Mathf.RoundToInt(sprite.rect.width),
                    Height = texture != null ? texture.height : Mathf.RoundToInt(sprite.rect.height)
                });
            }
        }

        return candidates
            .OrderBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Sprite.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool LooksLikeMotherBirdArt(string path)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);
        return fileName.IndexOf("mother", StringComparison.OrdinalIgnoreCase) >= 0 ||
               fileName.Contains("엄마새") ||
               fileName.Contains("빡친") ||
               fileName.Contains("먹이잘못") ||
               fileName.Contains("미소");
    }

    private static SpriteCandidate FindBestCandidate(PoseSpec spec, List<SpriteCandidate> candidates)
    {
        SpriteCandidate best = null;
        int bestScore = int.MinValue;
        float bestSpriteArea = -1f;
        bool ambiguous = false;

        foreach (SpriteCandidate candidate in candidates)
        {
            string normalizedName = NormalizeName(candidate.FileName);
            int exactRank = Array.FindIndex(
                spec.ExactNames,
                name => NormalizeName(name) == normalizedName);
            bool fuzzyMatch = exactRank < 0 && IsFuzzyMatch(spec.PropertyName, normalizedName);
            if (exactRank < 0 && !fuzzyMatch)
            {
                continue;
            }

            int score = 0;
            if (HasRecommendedCanvas(candidate))
            {
                score += 10000;
            }

            if (candidate.Path.IndexOf("Placeholder", StringComparison.OrdinalIgnoreCase) < 0)
            {
                score += 1000;
            }

            score += exactRank >= 0 ? 500 - exactRank : 100;
            float spriteArea = candidate.Sprite != null
                ? candidate.Sprite.rect.width * candidate.Sprite.rect.height
                : 0f;
            if (score > bestScore || (score == bestScore && spriteArea > bestSpriteArea))
            {
                best = candidate;
                bestScore = score;
                bestSpriteArea = spriteArea;
                ambiguous = false;
            }
            else if (score == bestScore && Mathf.Approximately(spriteArea, bestSpriteArea))
            {
                ambiguous = true;
            }
        }

        if (ambiguous)
        {
            Debug.LogWarning(
                $"[WARN] Multiple equally ranked Sprite candidates were found for {spec.Label}. " +
                "The existing Sprite will be preserved; assign the intended asset manually.");
            return null;
        }

        return best;
    }

    private static bool IsFuzzyMatch(string propertyName, string normalizedName)
    {
        bool mother = normalizedName.Contains("mother") || normalizedName.Contains("엄마새");
        switch (propertyName)
        {
            case "idle":
                return (mother && (normalizedName.Contains("idle") || normalizedName.Contains("normal"))) ||
                       (normalizedName.StartsWith("엄마새") &&
                        !normalizedName.Contains("먹이") &&
                        !normalizedName.Contains("빡친") &&
                        !normalizedName.Contains("미소"));
            case "feed":
                return (mother && normalizedName.Contains("feed")) || normalizedName.Contains("엄마새먹이주기");
            case "peck1":
                return (mother && normalizedName.Contains("peck1")) ||
                       (normalizedName.Contains("빡친엄마새") && !normalizedName.Contains("2타"));
            case "peck1Success":
                return (mother && normalizedName.Contains("peck1perfect")) ||
                       normalizedName.Contains("엄마새쪼기1타대성공");
            case "peck2":
                return (mother && normalizedName.Contains("peck2")) || normalizedName.Contains("빡친엄마새2타");
            case "peck2Success":
                return (mother && normalizedName.Contains("peck2perfect")) ||
                       normalizedName.Contains("엄마새쪼기2타대성공");
            case "wrongFeed":
                return (mother && normalizedName.Contains("wrongfeed")) || normalizedName.Contains("먹이잘못");
            case "wrongPeck":
                return (mother && normalizedName.Contains("wrongpeck")) || normalizedName.Contains("먹이잘못");
            case "smile":
                return (mother && normalizedName.Contains("smile")) || normalizedName.Contains("엄마새미소");
            default:
                return false;
        }
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return new string(value
            .Where(character => char.IsLetterOrDigit(character))
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static void LogCandidate(SpriteCandidate candidate)
    {
        TextureImporter importer = candidate.Importer;
        SpriteMeshType meshType = GetSpriteMeshType(importer);
        string importerInfo = importer == null
            ? "TextureImporter unavailable"
            : $"type={importer.textureType}, mode={importer.spriteImportMode}, " +
              $"PPU={importer.spritePixelsPerUnit}, pivot={importer.spritePivot}, " +
              $"mesh={meshType}, alpha={importer.alphaIsTransparency}, maxSize={importer.maxTextureSize}";
        string sizeStatus = candidate.Width == 1448 && candidate.Height == 1086 ? "PASS" : "WARN";
        string guidStatus = string.IsNullOrEmpty(candidate.Guid) ? "<missing>" : candidate.Guid;
        Debug.Log(
            $"[{sizeStatus}] Candidate Sprite: {candidate.Path} / sprite={candidate.Sprite.name} / " +
            $"spriteRect={candidate.Sprite.rect.width}x{candidate.Sprite.rect.height} / " +
            $"texture={candidate.Width}x{candidate.Height} / GUID={guidStatus} / {importerInfo}");
    }

    private static void ValidateSprite(Sprite sprite, string label)
    {
        string path = AssetDatabase.GetAssetPath(sprite);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int width = texture != null ? texture.width : Mathf.RoundToInt(sprite.rect.width);
        int height = texture != null ? texture.height : Mathf.RoundToInt(sprite.rect.height);
        int widthDifference = Mathf.Abs(width - 1448);
        int heightDifference = Mathf.Abs(height - 1086);

        if (widthDifference == 0 && heightDifference == 0)
        {
            Debug.Log($"[PASS] Pose {label} sprite canvas is 1448x1086: {path}");
        }
        else
        {
            string strength = widthDifference >= 10 || heightDifference >= 10 ? "strong mismatch" : "minor mismatch";
            Debug.LogWarning($"[WARN] Pose {label} sprite canvas {width}x{height} ({strength}): {path}");
        }

        if (importer == null)
        {
            Debug.LogWarning($"[WARN] Pose {label} TextureImporter unavailable: {path}");
            return;
        }

        LogCheck(importer.textureType == TextureImporterType.Sprite,
            $"Pose {label} Texture Type is Sprite",
            $"Pose {label} Texture Type is {importer.textureType}");
        LogCheck(Mathf.Approximately(importer.spritePixelsPerUnit, 100f),
            $"Pose {label} PPU is 100",
            $"Pose {label} PPU is {importer.spritePixelsPerUnit}");
        LogCheck(importer.alphaIsTransparency,
            $"Pose {label} Alpha Is Transparency is enabled",
            $"Pose {label} Alpha Is Transparency is disabled");
        SpriteMeshType meshType = GetSpriteMeshType(importer);
        LogCheck(meshType == SpriteMeshType.FullRect,
            $"Pose {label} Mesh Type is Full Rect",
            $"Pose {label} Mesh Type is {meshType}");

        Vector2 normalizedPivot = new Vector2(
            sprite.rect.width > 0f ? sprite.pivot.x / sprite.rect.width : 0.5f,
            sprite.rect.height > 0f ? sprite.pivot.y / sprite.rect.height : 0.5f);
        LogCheck(Vector2.Distance(normalizedPivot, new Vector2(0.5f, 0.5f)) <= 0.001f,
            $"Pose {label} pivot is centered",
            $"Pose {label} pivot is {normalizedPivot}");
    }

    private static bool HasRecommendedCanvas(SpriteCandidate candidate)
    {
        return candidate != null && candidate.Width == 1448 && candidate.Height == 1086;
    }

    private static SpriteMeshType GetSpriteMeshType(TextureImporter importer)
    {
        if (importer == null)
        {
            return SpriteMeshType.Tight;
        }

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        return settings.spriteMeshType;
    }

    private static List<PropertyModification> GetPoseOverrideModifications(
        GameObject instanceRoot,
        MotherBirdSpriteController sourceController)
    {
        var result = new List<PropertyModification>();
        if (instanceRoot == null || sourceController == null)
        {
            return result;
        }

        PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(instanceRoot);
        if (modifications == null)
        {
            return result;
        }

        foreach (PropertyModification modification in modifications)
        {
            if (modification == null || modification.target != sourceController || !IsPoseOverride(modification))
            {
                continue;
            }

            result.Add(modification);
        }

        return result;
    }

    private static string BuildPoseSummary(SerializedObject serializedController)
    {
        if (serializedController == null)
        {
            return "<missing SerializedObject>";
        }

        serializedController.UpdateIfRequiredOrScript();
        var summaries = new List<string>();
        foreach (PoseSpec spec in PoseSpecs.Where(spec => spec.Required))
        {
            SerializedProperty pose = serializedController.FindProperty(spec.PropertyName);
            SerializedProperty spriteProperty = pose?.FindPropertyRelative("sprite");
            SerializedProperty offsetProperty = pose?.FindPropertyRelative("positionOffset");
            SerializedProperty scaleProperty = pose?.FindPropertyRelative("scale");
            SerializedProperty flipProperty = pose?.FindPropertyRelative("flipX");
            if (spriteProperty == null || offsetProperty == null || scaleProperty == null || flipProperty == null)
            {
                summaries.Add($"{spec.Label}=<invalid SpritePose>");
                continue;
            }

            summaries.Add(
                $"{spec.Label}[sprite={GetObjectName(spriteProperty.objectReferenceValue)}, " +
                $"offset={offsetProperty.vector2Value}, scale={scaleProperty.vector2Value}, " +
                $"flipX={flipProperty.boolValue}]");
        }

        return string.Join(" | ", summaries);
    }

    private static void ValidateScenePoseValues(MotherBirdSpriteController controller)
    {
        if (controller == null)
        {
            Debug.LogWarning("[WARN] Cannot validate scene Pose values because the controller is missing.");
            return;
        }

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.UpdateIfRequiredOrScript();
        foreach (PoseSpec spec in PoseSpecs.Where(spec => spec.Required))
        {
            SerializedProperty pose = serializedController.FindProperty(spec.PropertyName);
            SerializedProperty spriteProperty = pose?.FindPropertyRelative("sprite");
            SerializedProperty offsetProperty = pose?.FindPropertyRelative("positionOffset");
            SerializedProperty scaleProperty = pose?.FindPropertyRelative("scale");
            if (spriteProperty == null || offsetProperty == null || scaleProperty == null)
            {
                Debug.LogWarning($"[WARN] Scene Pose values could not be read: {spec.PropertyName}");
                continue;
            }

            Sprite sprite = spriteProperty.objectReferenceValue as Sprite;
            Vector2 offset = offsetProperty.vector2Value;
            Vector2 scale = scaleProperty.vector2Value;
            string spritePath = sprite != null ? AssetDatabase.GetAssetPath(sprite) : "<missing/null>";
            Debug.Log(
                $"[INFO] Scene Pose {spec.Label}: sprite={GetObjectName(sprite)}, path={spritePath}, " +
                $"offset={offset}, scale={scale}");

            if (!string.Equals(spec.PropertyName, "idle", StringComparison.Ordinal))
            {
                LogCheck(
                    IsSpriteFromPlayerTestFolder(sprite),
                    $"Scene Pose {spec.Label} uses a Player/Test Sprite",
                    $"Scene Pose {spec.Label} does not use a Player/Test Sprite: {spritePath}");
            }

            LogCheck(
                offset.magnitude <= 0.2f,
                $"Scene Pose {spec.Label} offset is near zero",
                $"Scene Pose {spec.Label} has large offset: {offset}");
            LogCheck(
                scale.x >= 0.95f && scale.x <= 1.05f && scale.y >= 0.95f && scale.y <= 1.05f,
                $"Scene Pose {spec.Label} scale is near one",
                $"Scene Pose {spec.Label} scale is outside 0.95-1.05: {scale}");
        }
    }

    private static void ValidateSceneVisualAndWorm(MotherBirdSpriteController controller)
    {
        if (controller == null)
        {
            Debug.LogWarning("[WARN] Cannot validate MotherBird Visual/Worm because the controller is missing.");
            return;
        }

        SpriteRenderer spriteRenderer = GetControllerSpriteRenderer(controller);
        if (spriteRenderer == null)
        {
            Debug.LogWarning("[WARN] Scene MotherBird Visual SpriteRenderer is missing.");
        }
        else
        {
            Transform visual = spriteRenderer.transform;
            Debug.Log(
                $"[INFO] Scene MotherBird Visual: path={GetHierarchyPath(visual)}, " +
                $"localPosition={visual.localPosition}, localScale={visual.localScale}, " +
                $"rendererEnabled={spriteRenderer.enabled}");
            LogCheck(
                IsNearZero(visual.localPosition),
                "Scene MotherBird Visual localPosition is zero",
                $"Scene MotherBird Visual localPosition is not zero: {visual.localPosition}");
            LogCheck(
                IsNearOne(visual.localScale),
                "Scene MotherBird Visual localScale is one",
                $"Scene MotherBird Visual localScale is not one: {visual.localScale}");
        }

        LogWormInfo(controller, "Validate");
    }

    private static void ValidateNewBirdSetupInScene(
        Scene scene,
        MotherBirdSpriteController motherBird)
    {
        SerializedObject motherObject = new SerializedObject(motherBird);
        ValidateSplitIdleMotherSetup(motherObject, motherBird);

        SerializedProperty feedSuccess = motherObject.FindProperty("feedSuccess");
        Sprite feedSuccessSprite = feedSuccess?.FindPropertyRelative("sprite")?.objectReferenceValue as Sprite;
        Vector2 feedSuccessOffset = feedSuccess?.FindPropertyRelative("positionOffset")?.vector2Value ?? Vector2.zero;
        Vector2 feedSuccessScale = feedSuccess?.FindPropertyRelative("scale")?.vector2Value ?? Vector2.zero;
        LogCheck(
            IsExactSpriteFile(feedSuccessSprite, "먹이주기대성공.png"),
            "MotherBird feedSuccess uses 먹이주기대성공.png",
            $"MotherBird feedSuccess Sprite is incorrect: {GetSpriteReport(feedSuccessSprite)}");
        LogCheck(
            feedSuccess != null && feedSuccessOffset.magnitude <= 0.001f,
            "MotherBird feedSuccess offset is (0, 0)",
            $"MotherBird feedSuccess offset is {feedSuccessOffset}");
        LogCheck(
            feedSuccess != null &&
            Mathf.Abs(feedSuccessScale.x - 1f) <= 0.001f &&
            Mathf.Abs(feedSuccessScale.y - 1f) <= 0.001f,
            "MotherBird feedSuccess scale is (1, 1)",
            $"MotherBird feedSuccess scale is {feedSuccessScale}");
        ValidateOptionalMotherPerfectPose(
            motherObject,
            "peck1Success",
            "peck1",
            "신엄마새1타대성공.png",
            "엄마새쪼기1타대성공.png",
            "Mother_Peck1Perfect.png");
        Sprite peck1SuccessSprite = motherObject.FindProperty("peck1Success")?
            .FindPropertyRelative("sprite")?.objectReferenceValue as Sprite;
        Debug.Log($"[INFO] Scene MotherBird peck1Success: {GetSpriteReport(peck1SuccessSprite)}");
        ValidateOptionalMotherPerfectPose(
            motherObject,
            "peck2Success",
            "peck2",
            "엄마새쪼기2타대성공.png",
            "Mother_Peck2Perfect.png");

        Stage01IntroGuideUI introGuide = FindComponentInScene<Stage01IntroGuideUI>(scene);
        Sprite guideSprite = null;
        if (introGuide != null)
        {
            SerializedObject guideObject = new SerializedObject(introGuide);
            guideSprite = guideObject.FindProperty("chickSmallSprite")?.objectReferenceValue as Sprite;
        }

        LogCheck(
            introGuide != null && IsExactSpriteFile(guideSprite, "신아기새기본.png"),
            "Stage01IntroGuideUI Chick icon uses 신아기새기본.png",
            introGuide == null
                ? "Stage01IntroGuideUI was not found"
                : $"Stage01IntroGuideUI Chick icon is {GetSpriteReport(guideSprite)}");

        Stage01FeedingController feedingController = FindComponentInScene<Stage01FeedingController>(scene);
        SerializedProperty durationProperty = null;
        SerializedProperty peck1CompositeDurationProperty = null;
        SerializedProperty perfectWindowProperty = null;
        SerializedProperty goodWindowProperty = null;
        if (feedingController != null)
        {
            SerializedObject feedingObject = new SerializedObject(feedingController);
            durationProperty = feedingObject.FindProperty("feedSuccessCompositeDuration");
            peck1CompositeDurationProperty = feedingObject.FindProperty("peck1SuccessCompositeDuration");
            perfectWindowProperty = feedingObject.FindProperty("perfectTimingWindowSeconds");
            goodWindowProperty = feedingObject.FindProperty("goodTimingWindowSeconds");
        }

        LogCheck(
            durationProperty != null && durationProperty.floatValue >= 0f,
            $"Stage01 feedSuccessCompositeDuration is available ({(durationProperty?.floatValue ?? 0f):0.###}s)",
            "Stage01FeedingController.feedSuccessCompositeDuration is missing or invalid");
        LogCheck(
            peck1CompositeDurationProperty != null &&
            Mathf.Approximately(
                peck1CompositeDurationProperty.floatValue,
                RecommendedPeck1SuccessCompositeDuration),
            $"Cuckoo Peck1 Perfect visibility duration is available ({(peck1CompositeDurationProperty?.floatValue ?? 0f):0.###}s)",
            $"Stage01FeedingController.peck1SuccessCompositeDuration is " +
            $"{(peck1CompositeDurationProperty?.floatValue ?? -1f):0.###}s; recommended is " +
            $"{RecommendedPeck1SuccessCompositeDuration:0.##}s");
        LogCheck(
            perfectWindowProperty != null && Mathf.Approximately(perfectWindowProperty.floatValue, 0.08f),
            "Stage01 Perfect timing window is 0.08s",
            $"Stage01 Perfect timing window is {(perfectWindowProperty?.floatValue ?? -1f):0.###}s");
        LogCheck(
            goodWindowProperty != null && Mathf.Approximately(goodWindowProperty.floatValue, 0.18f),
            "Stage01 Good timing window is 0.18s",
            $"Stage01 Good timing window is {(goodWindowProperty?.floatValue ?? -1f):0.###}s");
        LogCheck(
            typeof(MotherBirdSpriteController).GetMethod("PlayFeedSuccessComposite") != null,
            "MotherBird feed-success presentation API exists",
            "MotherBirdSpriteController.PlayFeedSuccessComposite is missing");
        LogCheck(
            typeof(MotherBirdSpriteController).GetMethod(
                "PlayPeck1Success",
                Type.EmptyTypes) != null &&
            typeof(MotherBirdSpriteController).GetMethod("PlayPeck2Success") != null,
            "MotherBird Cuckoo Perfect peck APIs exist",
            "MotherBird Perfect peck API is missing");
        LogCheck(
            typeof(MotherBirdSpriteController).GetProperty("HasPeck1SuccessSprite") != null,
            "MotherBird Peck1 Perfect composite availability check exists",
            "MotherBirdSpriteController.HasPeck1SuccessSprite is missing");
        LogCheck(
            typeof(MotherBirdSpriteController).GetMethod(
                "PlayPeck1Success",
                new[] { typeof(float) }) != null,
            "MotherBird Peck1 Perfect duration overload is callable",
            "MotherBirdSpriteController.PlayPeck1Success(float) is missing");
        LogCheck(
            typeof(ChickSpriteController).GetMethod("SetVisualVisible") != null,
            "Chick Visual hide/restore API exists",
            "ChickSpriteController.SetVisualVisible is missing");
        LogCheck(
            typeof(CuckooSpriteController).GetMethod("SetVisualVisible") != null,
            "Cuckoo Visual hide/restore API exists for Peck1 Perfect",
            "CuckooSpriteController.SetVisualVisible is missing");
        LogCheck(
            typeof(CuckooSpriteController).GetMethod("PlayPartialHitExit") != null,
            "Cuckoo second-peck miss uses a dedicated PartialHitExit API",
            "CuckooSpriteController.PlayPartialHitExit is missing");
        LogCheck(
            typeof(CuckooSpriteController).GetMethod("PlayKnockaway") != null,
            "Cuckoo second-hit Knockaway remains available",
            "CuckooSpriteController.PlayKnockaway is missing");
        Debug.Log("[PASS] New bird setup validation is read-only.");
    }

    private static void ValidateSplitIdleMotherSetup(
        SerializedObject motherObject,
        MotherBirdSpriteController motherBird)
    {
        if (motherObject == null || motherBird == null)
        {
            Debug.LogWarning("[WARN] Split Idle validation skipped because MotherBird is missing.");
            return;
        }

        SerializedProperty useSplitProperty = motherObject.FindProperty("useSplitIdleVisual");
        SerializedProperty bodyProperty = motherObject.FindProperty("idleBodyRenderer");
        SerializedProperty eyeProperty = motherObject.FindProperty("idleEyeRenderer");
        SerializedProperty beakWormProperty = motherObject.FindProperty("idleBeakWormRenderer");
        SerializedProperty bodyOrderProperty = motherObject.FindProperty("idleBodySortingOrder");
        SerializedProperty faceOrderProperty = motherObject.FindProperty("idleFaceSortingOrder");
        SerializedProperty hideLegacyProperty = motherObject.FindProperty("hideLegacyWormVisual");
        SerializedProperty wormProperty = motherObject.FindProperty("wormVisual");

        SpriteRenderer bodyRenderer = bodyProperty?.objectReferenceValue as SpriteRenderer;
        SpriteRenderer eyeRenderer = eyeProperty?.objectReferenceValue as SpriteRenderer;
        SpriteRenderer beakWormRenderer = beakWormProperty?.objectReferenceValue as SpriteRenderer;
        Transform beatMoveRoot = motherBird.transform.Find("BeatMoveRoot");
        Transform beatSquashRoot = motherBird.transform.Find("BeatMoveRoot/BeatSquashRoot");

        Debug.Log(
            "# MotherBird Split Idle Validation\n" +
            $"- useSplitIdleVisual: {useSplitProperty?.boolValue ?? false}\n" +
            $"- idleBodyRenderer: {GetHierarchyPath(bodyRenderer != null ? bodyRenderer.transform : null)}\n" +
            $"- idleEyeRenderer: {GetHierarchyPath(eyeRenderer != null ? eyeRenderer.transform : null)}\n" +
            $"- idleBeakWormRenderer: {GetHierarchyPath(beakWormRenderer != null ? beakWormRenderer.transform : null)}\n" +
            $"- hideLegacyWormVisual: {hideLegacyProperty?.boolValue ?? false}");

        LogCheck(
            useSplitProperty != null && useSplitProperty.boolValue,
            "MotherBird Split Idle visual is enabled",
            "MotherBird useSplitIdleVisual is disabled or missing");
        LogCheck(
            bodyRenderer != null && IsExactSpriteFile(bodyRenderer.sprite, "엄마새_몸통베이스.png"),
            "IdleBodyRenderer uses 엄마새_몸통베이스.png",
            $"IdleBodyRenderer is missing or uses the wrong Sprite: {GetSpriteReport(bodyRenderer != null ? bodyRenderer.sprite : null)}");
        LogCheck(
            eyeRenderer != null && IsExactSpriteFile(eyeRenderer.sprite, "엄마새_눈파츠.png"),
            "IdleEyeRenderer uses 엄마새_눈파츠.png",
            $"IdleEyeRenderer is missing or uses the wrong Sprite: {GetSpriteReport(eyeRenderer != null ? eyeRenderer.sprite : null)}");
        LogCheck(
            beakWormRenderer != null && IsExactSpriteFile(beakWormRenderer.sprite, "엄마새_부리_지렁이파츠.png"),
            "IdleBeakWormRenderer uses 엄마새_부리_지렁이파츠.png",
            $"IdleBeakWormRenderer is missing or uses the wrong Sprite: {GetSpriteReport(beakWormRenderer != null ? beakWormRenderer.sprite : null)}");

        LogCheck(
            bodyRenderer != null && bodyRenderer.transform.parent == beatSquashRoot,
            "IdleBodyVisual is a direct child of BeatSquashRoot",
            "IdleBodyVisual must be a direct child of BeatSquashRoot");
        LogCheck(
            eyeRenderer != null && eyeRenderer.transform.parent == beatMoveRoot,
            "IdleEyeVisual is a direct child of BeatMoveRoot",
            "IdleEyeVisual must be a direct child of BeatMoveRoot");
        LogCheck(
            beakWormRenderer != null && beakWormRenderer.transform.parent == beatMoveRoot,
            "IdleBeakWormVisual is a direct child of BeatMoveRoot",
            "IdleBeakWormVisual must be a direct child of BeatMoveRoot");

        bool splitTransformsReset =
            HasResetLocalTransform(bodyRenderer) &&
            HasResetLocalTransform(eyeRenderer) &&
            HasResetLocalTransform(beakWormRenderer);
        LogCheck(
            splitTransformsReset,
            "Split Idle part local transforms are position zero, rotation identity, and scale one",
            "One or more Split Idle part local transforms are not reset");
        LogCheck(
            bodyOrderProperty != null && bodyOrderProperty.intValue == -3 &&
            bodyRenderer != null && bodyRenderer.sortingOrder == -3,
            "Idle Body sorting order is -3",
            $"Idle Body sorting order differs from -3 (field={bodyOrderProperty?.intValue ?? 0}, renderer={bodyRenderer?.sortingOrder ?? 0})");
        LogCheck(
            faceOrderProperty != null && faceOrderProperty.intValue == -2 &&
            eyeRenderer != null && eyeRenderer.sortingOrder == -2 &&
            beakWormRenderer != null && beakWormRenderer.sortingOrder == -2,
            "Idle Eye and BeakWorm sorting order is -2",
            "Idle Face sorting field or renderer order differs from -2");

        SpriteRenderer sourceRenderer = GetControllerSpriteRenderer(motherBird);
        LogCheck(
            sourceRenderer != null && sourceRenderer.sprite != null,
            "Existing SpriteVisual remains available for non-Idle fallback",
            "Existing SpriteVisual or its Sprite is missing; non-Idle fallback cannot render");

        Transform legacyWorm = wormProperty?.objectReferenceValue as Transform;
        if (legacyWorm == null)
        {
            Debug.Log("[INFO] MotherBird WormVisual is not connected; there is no legacy object to delete or hide.");
        }
        else
        {
            SpriteRenderer[] legacyRenderers = legacyWorm.GetComponentsInChildren<SpriteRenderer>(true);
            bool allDisabled = legacyRenderers.All(renderer => renderer == null || !renderer.enabled);
            LogCheck(
                legacyWorm.gameObject != null,
                $"Legacy WormVisual is preserved: {GetHierarchyPath(legacyWorm)}",
                "Legacy WormVisual reference is invalid");
            LogCheck(
                !hideLegacyProperty.boolValue || allDisabled,
                "Legacy WormVisual renderers are hidden",
                "Legacy WormVisual has an enabled renderer while hideLegacyWormVisual is enabled");
        }
    }

    private static bool HasResetLocalTransform(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        Transform target = renderer.transform;
        return IsNearZero(target.localPosition) &&
               Quaternion.Angle(target.localRotation, Quaternion.identity) <= 0.001f &&
               IsNearOne(target.localScale);
    }

    private static void ValidateOptionalMotherPerfectPose(
        SerializedObject motherObject,
        string perfectPoseName,
        string fallbackPoseName,
        params string[] supportedFileNames)
    {
        SerializedProperty perfectPose = motherObject.FindProperty(perfectPoseName);
        SerializedProperty fallbackPose = motherObject.FindProperty(fallbackPoseName);
        Sprite perfectSprite = perfectPose?.FindPropertyRelative("sprite")?.objectReferenceValue as Sprite;
        Sprite fallbackSprite = fallbackPose?.FindPropertyRelative("sprite")?.objectReferenceValue as Sprite;
        if (!HasSpritePoseShape(perfectPose) || perfectSprite == null)
        {
            Debug.Log(
                $"[INFO] MotherBird {perfectPoseName} Sprite is not assigned. " +
                $"Runtime will fall back to {fallbackPoseName}: {GetSpriteReport(fallbackSprite)}");
            return;
        }

        string fileName = Path.GetFileName(AssetDatabase.GetAssetPath(perfectSprite));
        LogCheck(
            supportedFileNames.Any(name => string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase)),
            $"MotherBird {perfectPoseName} uses a supported candidate: {GetSpriteReport(perfectSprite)}",
            $"MotherBird {perfectPoseName} uses an unrecognized Sprite: {GetSpriteReport(perfectSprite)}");

        Vector2 offset = perfectPose.FindPropertyRelative("positionOffset").vector2Value;
        Vector2 scale = perfectPose.FindPropertyRelative("scale").vector2Value;
        LogCheck(
            offset.magnitude <= 0.2f,
            $"MotherBird {perfectPoseName} offset is near zero",
            $"MotherBird {perfectPoseName} offset is {offset}");
        LogCheck(
            scale.x >= 0.95f && scale.x <= 1.05f && scale.y >= 0.95f && scale.y <= 1.05f,
            $"MotherBird {perfectPoseName} scale is near one",
            $"MotherBird {perfectPoseName} scale is {scale}");
    }

    private static bool IsPoseOverride(PropertyModification modification)
    {
        if (modification == null || string.IsNullOrEmpty(modification.propertyPath))
        {
            return false;
        }

        foreach (PoseSpec spec in PoseSpecs)
        {
            if (!spec.Required)
            {
                continue;
            }

            if (modification.propertyPath == spec.PropertyName ||
                modification.propertyPath.StartsWith(spec.PropertyName + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void LogCheck(bool passed, string passMessage, string warningMessage)
    {
        if (passed)
        {
            Debug.Log($"[PASS] {passMessage}");
        }
        else
        {
            Debug.LogWarning($"[WARN] {warningMessage}");
        }
    }

    private static bool IsNearZero(Vector3 value)
    {
        return value.sqrMagnitude <= 0.000001f;
    }

    private static bool IsNearOne(Vector3 value)
    {
        return (value - Vector3.one).sqrMagnitude <= 0.000001f;
    }

    private static string GetObjectName(UnityEngine.Object target)
    {
        return target != null ? target.name : "<missing/null>";
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null)
        {
            return "<missing>";
        }

        var names = new Stack<string>();
        Transform current = target;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }
}
#endif
