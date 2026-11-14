using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TitlePostStage02OutroSpriteBinder
{
    private const string MenuPath = "PeckBeat/Title/Bind Post Stage02 Outro Sprites";
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
    private const string RatArtPath = "Assets/_PeckBeat/Art/Title/Rat";
    private const string EndingArtPath = "Assets/_PeckBeat/Art/부리박자슬슬마무리";

    [MenuItem(MenuPath)]
    private static void BindSprites()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != TitleScenePath)
        {
            Debug.LogWarning(
                $"[TitlePostStage02OutroSpriteBinder] Open {TitleScenePath} first.");
            return;
        }

        TitleIntroDialogueController controller =
            Object.FindFirstObjectByType<TitleIntroDialogueController>(FindObjectsInactive.Include);
        if (controller == null || controller.gameObject.scene != scene)
        {
            Debug.LogWarning(
                "[TitlePostStage02OutroSpriteBinder] TitleIntroDialogueController was not found in TitleScene.");
            return;
        }

        Undo.RecordObject(controller, "Bind Post Stage02 Outro Sprites");
        SerializedObject serialized = new SerializedObject(controller);
        AssignSpriteIfEmpty(serialized, "resultCheckSprite", "결과확인");
        AssignSpriteIfEmpty(serialized, "resultCheckMouthOpenSprite", "결과확인_입열기");
        AssignSpriteIfEmpty(serialized, "listeningSprite", "귀_기울이기");
        AssignSpriteIfEmpty(
            serialized,
            "endingDeepBowSprite",
            "쥐_엔딩_깊은인사",
            EndingArtPath);
        bool changed = serialized.ApplyModifiedProperties();

        if (changed)
        {
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        Selection.activeObject = controller;
        Debug.Log(
            "[TitlePostStage02OutroSpriteBinder] Outro sprite binding finished. The scene was not saved.",
            controller);
    }

    private static void AssignSpriteIfEmpty(
        SerializedObject serialized,
        string propertyName,
        string assetName,
        string searchPath = RatArtPath)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning(
                $"[TitlePostStage02OutroSpriteBinder] Serialized field '{propertyName}' was not found.");
            return;
        }

        if (property.objectReferenceValue != null)
        {
            return;
        }

        Sprite sprite = FindSpriteByExactName(assetName, searchPath);
        if (sprite == null)
        {
            Debug.LogWarning(
                $"[TitlePostStage02OutroSpriteBinder] Sprite '{assetName}' was not found under {searchPath}.");
            return;
        }

        property.objectReferenceValue = sprite;
    }

    private static Sprite FindSpriteByExactName(string assetName, string searchPath)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:Sprite", new[] { searchPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != assetName)
            {
                continue;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }
}
