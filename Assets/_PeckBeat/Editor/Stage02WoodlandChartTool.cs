using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage02WoodlandChartTool
{
    private const string MenuPath = "PeckBeat/Stage02/Apply Woodland 60s Chart";
    private const string Stage02ScenePath = "Assets/Scenes/Stage02.unity";
    private const string BgmPath =
        "Assets/_PeckBeat/Audio/Stage_02/BGM/Woodland Workshop Theme.mp3";
    private const string ChartFolder = "Assets/_PeckBeat/Charts/Stage_02";
    private const string ChartPath =
        ChartFolder + "/Stage02_Sculpting_Woodland60s.asset";
    private const float WoodlandBpm = 109f;
    private const float WoodlandFirstBeatOffset = 0.101f;

    private static readonly ChartEventDefinition[] WoodlandEvents =
    {
        // Intro: learn the one-hit response on a regular four-beat phrase.
        new(9f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(13f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureB),
        new(17f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(21f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureB),
        new(25f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(29f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureB),

        // Three two-hit phrases, spaced by eight beats. The second input is
        // targetBeat + 1, following the musical pattern established at 36/37.
        new(36f, Stage02SculptingChart.TargetType.Normal, 2, Stage02SculptingChart.SculptureType.SculptureA),
        new(44f, Stage02SculptingChart.TargetType.Normal, 2, Stage02SculptingChart.SculptureType.SculptureB),
        new(52f, Stage02SculptingChart.TargetType.Normal, 2, Stage02SculptingChart.SculptureType.SculptureA),

        // Three two-beat holds, separated by eight beats.
        new(60f, Stage02SculptingChart.TargetType.Hold, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(68f, Stage02SculptingChart.TargetType.Hold, 1, Stage02SculptingChart.SculptureType.SculptureB),
        new(76f, Stage02SculptingChart.TargetType.Hold, 1, Stage02SculptingChart.SculptureType.SculptureA),

        // Easy one-hit finish.
        new(84f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureB),
        new(88f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(92f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureB),
        new(96f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(100f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureB)
    };

    [MenuItem(MenuPath)]
    private static void ApplyWoodlandChart()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage02ScenePath)
        {
            Debug.LogWarning($"[Stage02 Woodland] Open {Stage02ScenePath} first.");
            return;
        }

        AudioClip bgm = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);
        Stage02SculptingController controller = FindInScene<Stage02SculptingController>(scene);
        PeckBeatRhythmManager rhythmManager = FindInScene<PeckBeatRhythmManager>(scene);
        Stage02AudioController audioController = FindInScene<Stage02AudioController>(scene);
        if (bgm == null || controller == null || rhythmManager == null || audioController == null)
        {
            Debug.LogWarning(
                "[Stage02 Woodland] Apply stopped. The exact Woodland Workshop Theme.mp3, "
                + "Stage02SculptingController, RhythmManager, and Stage02AudioController are required.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Apply Woodland 60s Chart",
                "Create or update the separate Woodland 60s chart and connect the new BGM "
                + "to the open Stage02 scene? Gameplay judgement fields and visual transforms "
                + "will be preserved. The scene will only be marked dirty.",
                "Apply",
                "Cancel"))
        {
            return;
        }

        Stage02SculptingChart previousChart = GetReference<Stage02SculptingChart>(controller, "chart");
        Stage02SculptingChart chart = GetOrCreateChart(previousChart);
        ApplyChartData(chart);
        AssignReference(controller, "chart", chart, "Assign Woodland Stage02 Chart");

        AudioSource rhythmSource = GetReference<AudioSource>(rhythmManager, "bgmSource");
        AssignAudioClip(rhythmSource, bgm, "Assign Woodland Stage02 BGM");
        AssignReference(audioController, "stageBgm", bgm, "Assign Woodland Stage02 BGM");
        AudioSource audioSource = GetReference<AudioSource>(audioController, "bgmSource");
        AssignAudioClip(audioSource, bgm, "Assign Woodland Stage02 BGM");

        StageMusicProgressGauge progressGauge = FindInScene<StageMusicProgressGauge>(scene);
        if (progressGauge != null)
        {
            AssignReference(progressGauge, "rhythmManager", rhythmManager, "Connect Woodland Progress Gauge");
            AssignReference(progressGauge, "bgmSource", rhythmSource, "Connect Woodland Progress Gauge");
            EditorUtility.SetDirty(progressGauge);
        }

        EditorUtility.SetDirty(chart);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(rhythmManager);
        EditorUtility.SetDirty(audioController);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeObject = controller;
        Debug.Log(BuildReport(bgm), controller);
    }

    private static Stage02SculptingChart GetOrCreateChart(Stage02SculptingChart previousChart)
    {
        Stage02SculptingChart chart = AssetDatabase.LoadAssetAtPath<Stage02SculptingChart>(ChartPath);
        if (chart != null)
        {
            Undo.RecordObject(chart, "Update Woodland Stage02 Chart");
            return chart;
        }

        EnsureFolder(ChartFolder);
        chart = ScriptableObject.CreateInstance<Stage02SculptingChart>();
        chart.name = "Stage02_Sculpting_Woodland60s";
        AssetDatabase.CreateAsset(chart, ChartPath);
        Undo.RegisterCreatedObjectUndo(chart, "Create Woodland Stage02 Chart");

        if (previousChart != null)
        {
            SerializedObject source = new(previousChart);
            SerializedObject destination = new(chart);
            foreach (string propertyName in new[]
                     {
                         "leadBeats", "judgeWindowSeconds", "resultHoldBeats", "exitDurationBeats"
                     })
            {
                SerializedProperty sourceProperty = source.FindProperty(propertyName);
                SerializedProperty destinationProperty = destination.FindProperty(propertyName);
                if (sourceProperty != null && destinationProperty != null)
                {
                    destinationProperty.floatValue = sourceProperty.floatValue;
                }
            }

            destination.ApplyModifiedPropertiesWithoutUndo();
        }

        return chart;
    }

    private static void ApplyChartData(Stage02SculptingChart chart)
    {
        SerializedObject serializedChart = new(chart);
        serializedChart.FindProperty("bpm").floatValue = WoodlandBpm;
        serializedChart.FindProperty("firstBeatOffset").floatValue = WoodlandFirstBeatOffset;
        serializedChart.FindProperty("offsetAdjustment").floatValue = 0f;

        SerializedProperty events = serializedChart.FindProperty("events");
        events.arraySize = WoodlandEvents.Length;
        for (int i = 0; i < WoodlandEvents.Length; i++)
        {
            ChartEventDefinition definition = WoodlandEvents[i];
            SerializedProperty chartEvent = events.GetArrayElementAtIndex(i);
            chartEvent.FindPropertyRelative("targetBeat").floatValue = definition.TargetBeat;
            chartEvent.FindPropertyRelative("targetType").enumValueIndex = (int)definition.TargetType;
            chartEvent.FindPropertyRelative("requiredHits").intValue = definition.RequiredHits;
            chartEvent.FindPropertyRelative("holdEndBeat").floatValue =
                definition.TargetType == Stage02SculptingChart.TargetType.Hold
                    ? definition.TargetBeat + 2f
                    : 0f;
            chartEvent.FindPropertyRelative("sculptureType").enumValueIndex =
                (int)definition.SculptureType;
        }

        serializedChart.ApplyModifiedProperties();
    }

    private static void AssignAudioClip(AudioSource source, AudioClip clip, string undoName)
    {
        if (source == null)
        {
            return;
        }

        Undo.RecordObject(source, undoName);
        source.clip = clip;
        source.loop = false;
        EditorUtility.SetDirty(source);
    }

    private static void AssignReference(
        Object target,
        string propertyName,
        Object value,
        string undoName)
    {
        if (target == null || value == null)
        {
            return;
        }

        SerializedObject serializedTarget = new(target);
        SerializedProperty property = serializedTarget.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[Stage02 Woodland] Missing property '{propertyName}' on {target.name}.", target);
            return;
        }

        Undo.RecordObject(target, undoName);
        property.objectReferenceValue = value;
        serializedTarget.ApplyModifiedProperties();
    }

    private static T GetReference<T>(Object target, string propertyName) where T : Object
    {
        if (target == null)
        {
            return null;
        }

        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        return property?.objectReferenceValue as T;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(component => component != null && component.gameObject.scene == scene);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string BuildReport(AudioClip bgm)
    {
        StringBuilder report = new();
        report.AppendLine("[Stage02 Woodland] Woodland 60s chart applied; scene not saved.")
            .AppendLine($"BGM: {BgmPath} ({bgm.length:0.000}s)")
            .AppendLine($"Chart: {ChartPath}")
            .AppendLine($"Grid: {WoodlandBpm:0.###} BPM, first beat {WoodlandFirstBeatOffset:0.000}s")
            .AppendLine("Whistles: 1-hit T-1; 2-hit T-2/T-1; Hold long T-2")
            .AppendLine("Events:");

        foreach (ChartEventDefinition definition in WoodlandEvents)
        {
            float targetTime = BeatToTime(definition.TargetBeat);
            if (definition.TargetType == Stage02SculptingChart.TargetType.Hold)
            {
                report.AppendLine(
                    $"  Hold {definition.TargetBeat:0}/{definition.TargetBeat + 2f:0} "
                    + $"({targetTime:0.000}/{BeatToTime(definition.TargetBeat + 2f):0.000}s), "
                    + $"whistle {definition.TargetBeat - 2f:0} "
                    + $"({BeatToTime(definition.TargetBeat - 2f):0.000}s)");
            }
            else if (definition.RequiredHits == 2)
            {
                report.AppendLine(
                    $"  2-hit {definition.TargetBeat:0}/{definition.TargetBeat + 1f:0} "
                    + $"({targetTime:0.000}/{BeatToTime(definition.TargetBeat + 1f):0.000}s), "
                    + $"whistles {definition.TargetBeat - 2f:0}/{definition.TargetBeat - 1f:0} "
                    + $"({BeatToTime(definition.TargetBeat - 2f):0.000}/"
                    + $"{BeatToTime(definition.TargetBeat - 1f):0.000}s)");
            }
            else
            {
                report.AppendLine(
                    $"  1-hit {definition.TargetBeat:0} ({targetTime:0.000}s), "
                    + $"whistle {definition.TargetBeat - 1f:0} "
                    + $"({BeatToTime(definition.TargetBeat - 1f):0.000}s)");
            }
        }

        return report.ToString();
    }

    private static float BeatToTime(float beat)
    {
        return WoodlandFirstBeatOffset + beat * (60f / WoodlandBpm);
    }

    private readonly struct ChartEventDefinition
    {
        public ChartEventDefinition(
            float targetBeat,
            Stage02SculptingChart.TargetType targetType,
            int requiredHits,
            Stage02SculptingChart.SculptureType sculptureType)
        {
            TargetBeat = targetBeat;
            TargetType = targetType;
            RequiredHits = requiredHits;
            SculptureType = sculptureType;
        }

        public float TargetBeat { get; }
        public Stage02SculptingChart.TargetType TargetType { get; }
        public int RequiredHits { get; }
        public Stage02SculptingChart.SculptureType SculptureType { get; }
    }
}
