using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage02ExhibitionChartTool
{
    private const string Stage02ScenePath = "Assets/Scenes/Stage02.unity";
    private const string BgmPath =
        "Assets/_PeckBeat/Audio/Stage_02/BGM/Log Carving Call.mp3";
    private const string ChartFolder = "Assets/_PeckBeat/Charts/Stage_02";
    private const string ChartPath =
        ChartFolder + "/Stage02_Sculpting_Exhibition50s.asset";

    private const float ExhibitionBpm = 109f;
    private const float ExhibitionFirstBeatOffset = 0.146f;

    private static readonly ChartEventDefinition[] ExhibitionEvents =
    {
        // 5-17 seconds: introductory one-hit targets.
        new(9f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(12f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureB),
        new(15f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(18f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureB),
        new(21f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(24f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureB),
        new(27f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(30f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureB),

        // 17-30 seconds: three two-hit targets. The second hit is targetBeat + 1.
        new(34f, Stage02SculptingChart.TargetType.Normal, 2, Stage02SculptingChart.SculptureType.SculptureA),
        new(42f, Stage02SculptingChart.TargetType.Normal, 2, Stage02SculptingChart.SculptureType.SculptureB),
        new(50f, Stage02SculptingChart.TargetType.Normal, 2, Stage02SculptingChart.SculptureType.SculptureA),

        // 30-40 seconds: two hold targets. Existing gameplay keeps each hold at two beats.
        new(56f, Stage02SculptingChart.TargetType.Hold, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(68f, Stage02SculptingChart.TargetType.Hold, 1, Stage02SculptingChart.SculptureType.SculptureB),

        // 40-47 seconds: easy one-hit finish.
        new(74f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(78f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureB),
        new(82f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureA),
        new(85f, Stage02SculptingChart.TargetType.Normal, 1, Stage02SculptingChart.SculptureType.SculptureB)
    };

    [MenuItem("PeckBeat/Stage02/Apply Exhibition 50s Chart")]
    public static void ApplyExhibitionChart()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Stage02ScenePath)
        {
            Debug.LogWarning(
                $"[Stage02 Exhibition] Open {Stage02ScenePath} before applying the chart.");
            return;
        }

        Stage02SculptingController controller = FindInScene<Stage02SculptingController>(scene);
        PeckBeatRhythmManager rhythmManager = FindInScene<PeckBeatRhythmManager>(scene);
        Stage02AudioController audioController = FindInScene<Stage02AudioController>(scene);
        AudioClip bgm = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);

        if (controller == null || rhythmManager == null || bgm == null)
        {
            Debug.LogWarning(
                "[Stage02 Exhibition] Apply stopped. Stage02SculptingController, "
                + "PeckBeatRhythmManager, and Log Carving Call.mp3 are required.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Apply Stage02 Exhibition Chart",
                "Create or update the separate 50-second Stage02 chart and connect it to "
                + "the currently open Stage02 scene. The scene will be marked dirty but will "
                + "not be saved automatically. Existing gameplay judgement values are preserved.",
                "Apply",
                "Cancel"))
        {
            return;
        }

        Stage02SculptingChart previousChart = GetObjectReference<Stage02SculptingChart>(
            controller,
            "chart");
        Stage02SculptingChart exhibitionChart = GetOrCreateChart(previousChart);
        if (exhibitionChart == null)
        {
            return;
        }

        ApplyChartData(exhibitionChart);
        AssignObjectReference(controller, "chart", exhibitionChart, "Apply Stage02 Exhibition Chart");
        AssignStageBgm(rhythmManager, audioController, bgm);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(rhythmManager);
        if (audioController != null)
        {
            EditorUtility.SetDirty(audioController);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssetIfDirty(exhibitionChart);

        Selection.activeObject = controller;
        Debug.Log(BuildChartReport(bgm), controller);
    }

    [MenuItem("PeckBeat/Stage02/Analyze Log Carving Call Beat Grid")]
    public static void AnalyzeBeatGrid()
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);
        if (clip == null)
        {
            Debug.LogWarning($"[Stage02 Exhibition] AudioClip was not found: {BgmPath}");
            return;
        }

        clip.LoadAudioData();
        float[] interleavedSamples = new float[clip.samples * clip.channels];
        if (!clip.GetData(interleavedSamples, 0))
        {
            Debug.LogWarning(
                "[Stage02 Exhibition] Audio samples could not be read. Run the analysis in "
                + "an Editor session where the clip audio data is available.",
                clip);
            return;
        }

        const int envelopeRate = 100;
        int hopFrames = Mathf.Max(1, clip.frequency / envelopeRate);
        int envelopeLength = Mathf.CeilToInt(clip.samples / (float)hopFrames);
        float[] energy = new float[envelopeLength];
        for (int envelopeIndex = 0; envelopeIndex < envelopeLength; envelopeIndex++)
        {
            int firstFrame = envelopeIndex * hopFrames;
            int lastFrame = Mathf.Min(firstFrame + hopFrames, clip.samples);
            double sum = 0d;
            int count = 0;
            for (int frame = firstFrame; frame < lastFrame; frame++)
            {
                int sampleIndex = frame * clip.channels;
                for (int channel = 0; channel < clip.channels; channel++)
                {
                    float sample = interleavedSamples[sampleIndex + channel];
                    sum += sample * sample;
                    count++;
                }
            }

            energy[envelopeIndex] = count > 0
                ? Mathf.Sqrt((float)(sum / count))
                : 0f;
        }

        float[] onset = BuildOnsetEnvelope(energy);
        List<BeatCandidate> candidates = new();
        for (float bpm = 107f; bpm <= 112.001f; bpm += 0.05f)
        {
            candidates.Add(EvaluateBeatCandidate(onset, envelopeRate, clip.length, bpm));
        }

        BeatCandidate[] bestCandidates = candidates
            .OrderByDescending(candidate => candidate.TotalScore)
            .Take(12)
            .ToArray();

        string candidateSummary = string.Join(
            "\n",
            bestCandidates.Select(candidate =>
                $"  BPM {candidate.Bpm:0.00}, phase {candidate.PhaseSeconds:0.000}s, "
                + $"score {candidate.TotalScore:0.0000} "
                + $"(early {candidate.EarlyScore:0.0000}, mid {candidate.MiddleScore:0.0000}, "
                + $"late {candidate.LateScore:0.0000})"));

        BeatCandidate candidate109 = FindClosestCandidate(candidates, 109f);
        BeatCandidate candidate110 = FindClosestCandidate(candidates, 110f);
        Debug.Log(
            $"[Stage02 Exhibition] Beat analysis completed.\n"
            + $"Clip: {BgmPath}\n"
            + $"Length: {clip.length:0.000}s, sample rate: {clip.frequency}, channels: {clip.channels}\n"
            + $"109 BPM: phase {candidate109.PhaseSeconds:0.000}s, score {candidate109.TotalScore:0.0000}, "
            + $"early/mid/late {candidate109.EarlyScore:0.0000}/{candidate109.MiddleScore:0.0000}/{candidate109.LateScore:0.0000}\n"
            + $"110 BPM: phase {candidate110.PhaseSeconds:0.000}s, score {candidate110.TotalScore:0.0000}, "
            + $"early/mid/late {candidate110.EarlyScore:0.0000}/{candidate110.MiddleScore:0.0000}/{candidate110.LateScore:0.0000}\n"
            + $"Top candidates:\n{candidateSummary}",
            clip);
    }

    private static Stage02SculptingChart GetOrCreateChart(Stage02SculptingChart previousChart)
    {
        Stage02SculptingChart chart = AssetDatabase.LoadAssetAtPath<Stage02SculptingChart>(ChartPath);
        bool created = chart == null;
        if (created)
        {
            EnsureFolder(ChartFolder);
            chart = ScriptableObject.CreateInstance<Stage02SculptingChart>();
            chart.name = "Stage02_Sculpting_Exhibition50s";
            AssetDatabase.CreateAsset(chart, ChartPath);
            Undo.RegisterCreatedObjectUndo(chart, "Create Stage02 Exhibition Chart");
        }
        else
        {
            Undo.RecordObject(chart, "Update Stage02 Exhibition Chart");
        }

        if (created && previousChart != null)
        {
            CopyTimingSettings(previousChart, chart);
        }

        return chart;
    }

    private static void CopyTimingSettings(
        Stage02SculptingChart source,
        Stage02SculptingChart destination)
    {
        SerializedObject sourceObject = new(source);
        SerializedObject destinationObject = new(destination);
        foreach (string propertyName in new[]
                 {
                     "leadBeats",
                     "judgeWindowSeconds",
                     "resultHoldBeats",
                     "exitDurationBeats"
                 })
        {
            SerializedProperty sourceProperty = sourceObject.FindProperty(propertyName);
            SerializedProperty destinationProperty = destinationObject.FindProperty(propertyName);
            if (sourceProperty != null && destinationProperty != null)
            {
                destinationProperty.floatValue = sourceProperty.floatValue;
            }
        }

        destinationObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyChartData(Stage02SculptingChart chart)
    {
        SerializedObject chartObject = new(chart);
        chartObject.FindProperty("bpm").floatValue = ExhibitionBpm;
        chartObject.FindProperty("firstBeatOffset").floatValue = ExhibitionFirstBeatOffset;
        chartObject.FindProperty("offsetAdjustment").floatValue = 0f;

        SerializedProperty eventsProperty = chartObject.FindProperty("events");
        eventsProperty.arraySize = ExhibitionEvents.Length;
        for (int i = 0; i < ExhibitionEvents.Length; i++)
        {
            ChartEventDefinition definition = ExhibitionEvents[i];
            SerializedProperty eventProperty = eventsProperty.GetArrayElementAtIndex(i);
            eventProperty.FindPropertyRelative("targetBeat").floatValue = definition.TargetBeat;
            eventProperty.FindPropertyRelative("targetType").enumValueIndex = (int)definition.TargetType;
            eventProperty.FindPropertyRelative("requiredHits").intValue = definition.RequiredHits;
            eventProperty.FindPropertyRelative("holdEndBeat").floatValue =
                definition.TargetType == Stage02SculptingChart.TargetType.Hold
                    ? definition.TargetBeat + 2f
                    : 0f;
            eventProperty.FindPropertyRelative("sculptureType").enumValueIndex =
                (int)definition.SculptureType;
        }

        chartObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(chart);
    }

    private static void AssignStageBgm(
        PeckBeatRhythmManager rhythmManager,
        Stage02AudioController audioController,
        AudioClip bgm)
    {
        AudioSource rhythmSource = GetObjectReference<AudioSource>(rhythmManager, "bgmSource");
        AssignAudioSourceClip(rhythmSource, bgm, "Assign Stage02 Exhibition BGM");

        if (audioController == null)
        {
            Debug.LogWarning(
                "[Stage02 Exhibition] Stage02AudioController was not found. The RhythmManager "
                + "BGM was connected, but the Stage02AudioController stageBgm slot was not changed.");
            return;
        }

        AssignObjectReference(audioController, "stageBgm", bgm, "Assign Stage02 Exhibition BGM");
        AudioSource audioControllerSource = GetObjectReference<AudioSource>(audioController, "bgmSource");
        if (audioControllerSource != rhythmSource)
        {
            AssignAudioSourceClip(
                audioControllerSource,
                bgm,
                "Assign Stage02 Exhibition BGM");
        }
    }

    private static void AssignAudioSourceClip(
        AudioSource source,
        AudioClip clip,
        string undoName)
    {
        if (source == null)
        {
            return;
        }

        Undo.RecordObject(source, undoName);
        source.clip = clip;
        source.loop = false;
        source.pitch = 1f;
        EditorUtility.SetDirty(source);
    }

    private static void AssignObjectReference(
        UnityEngine.Object target,
        string propertyName,
        UnityEngine.Object value,
        string undoName)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning(
                $"[Stage02 Exhibition] Property '{propertyName}' was not found on {target.name}.",
                target);
            return;
        }

        Undo.RecordObject(target, undoName);
        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedProperties();
    }

    private static T GetObjectReference<T>(UnityEngine.Object target, string propertyName)
        where T : UnityEngine.Object
    {
        if (target == null)
        {
            return null;
        }

        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        return property != null ? property.objectReferenceValue as T : null;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        return UnityEngine.Object
            .FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(component => component != null && component.gameObject.scene == scene);
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
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

    private static string BuildChartReport(AudioClip bgm)
    {
        StringBuilder builder = new();
        builder.AppendLine("[Stage02 Exhibition] 50-second chart applied (scene not saved).")
            .AppendLine($"BGM: {BgmPath} ({bgm.length:0.000}s)")
            .AppendLine($"Chart: {ChartPath}")
            .AppendLine($"Grid: {ExhibitionBpm:0.###} BPM, first beat {ExhibitionFirstBeatOffset:0.000}s")
            .AppendLine("Whistles: 1-hit T-1; 2-hit T-2/T-1; Hold long T-2")
            .AppendLine("Events:");

        foreach (ChartEventDefinition definition in ExhibitionEvents)
        {
            float targetTime = BeatToTime(definition.TargetBeat);
            if (definition.TargetType == Stage02SculptingChart.TargetType.Hold)
            {
                builder.AppendLine(
                    $"  Hold beat {definition.TargetBeat:0.###} ({targetTime:0.000}s), "
                    + $"long whistle beat {definition.TargetBeat - 2f:0.###} "
                    + $"({BeatToTime(definition.TargetBeat - 2f):0.000}s), "
                    + $"end beat {definition.TargetBeat + 2f:0.###} "
                    + $"({BeatToTime(definition.TargetBeat + 2f):0.000}s)");
                continue;
            }

            if (definition.RequiredHits == 2)
            {
                builder.AppendLine(
                    $"  2-hit beat {definition.TargetBeat:0.###}/{definition.TargetBeat + 1f:0.###} "
                    + $"({targetTime:0.000}/{BeatToTime(definition.TargetBeat + 1f):0.000}s), "
                    + $"whistles {definition.TargetBeat - 2f:0.###}/{definition.TargetBeat - 1f:0.###} "
                    + $"({BeatToTime(definition.TargetBeat - 2f):0.000}/"
                    + $"{BeatToTime(definition.TargetBeat - 1f):0.000}s)");
            }
            else
            {
                builder.AppendLine(
                    $"  1-hit beat {definition.TargetBeat:0.###} ({targetTime:0.000}s), "
                    + $"whistle {definition.TargetBeat - 1f:0.###} "
                    + $"({BeatToTime(definition.TargetBeat - 1f):0.000}s)");
            }
        }

        return builder.ToString();
    }

    private static float BeatToTime(float beat)
    {
        return ExhibitionFirstBeatOffset + beat * (60f / ExhibitionBpm);
    }

    private static float[] BuildOnsetEnvelope(float[] energy)
    {
        float[] onset = new float[energy.Length];
        const int localAverageWindow = 8;
        for (int i = 1; i < energy.Length; i++)
        {
            int start = Mathf.Max(0, i - localAverageWindow);
            float localAverage = 0f;
            for (int j = start; j < i; j++)
            {
                localAverage += energy[j];
            }

            localAverage /= Mathf.Max(1, i - start);
            onset[i] = Mathf.Max(0f, energy[i] - localAverage);
        }

        float maximum = onset.Length > 0 ? onset.Max() : 0f;
        if (maximum > 0f)
        {
            for (int i = 0; i < onset.Length; i++)
            {
                onset[i] /= maximum;
            }
        }

        return onset;
    }

    private static BeatCandidate EvaluateBeatCandidate(
        float[] onset,
        int envelopeRate,
        float clipLength,
        float bpm)
    {
        float beatInterval = 60f / bpm;
        float bestPhase = 0f;
        float bestScore = float.NegativeInfinity;
        float bestEarly = 0f;
        float bestMiddle = 0f;
        float bestLate = 0f;

        const float phaseStep = 0.002f;
        for (float phase = 0f; phase < beatInterval; phase += phaseStep)
        {
            float early = ScoreGrid(onset, envelopeRate, phase, beatInterval, 2f, clipLength / 3f);
            float middle = ScoreGrid(
                onset,
                envelopeRate,
                phase,
                beatInterval,
                clipLength / 3f,
                clipLength * 2f / 3f);
            float late = ScoreGrid(
                onset,
                envelopeRate,
                phase,
                beatInterval,
                clipLength * 2f / 3f,
                Mathf.Max(2f, clipLength - 2f));
            float total = (early + middle + late) / 3f;
            if (total <= bestScore)
            {
                continue;
            }

            bestScore = total;
            bestPhase = phase;
            bestEarly = early;
            bestMiddle = middle;
            bestLate = late;
        }

        return new BeatCandidate(
            bpm,
            bestPhase,
            bestScore,
            bestEarly,
            bestMiddle,
            bestLate);
    }

    private static float ScoreGrid(
        float[] onset,
        int envelopeRate,
        float phase,
        float beatInterval,
        float startTime,
        float endTime)
    {
        int count = 0;
        float score = 0f;
        int firstBeat = Mathf.CeilToInt((startTime - phase) / beatInterval);
        for (int beat = firstBeat;; beat++)
        {
            float time = phase + beat * beatInterval;
            if (time > endTime)
            {
                break;
            }

            int center = Mathf.RoundToInt(time * envelopeRate);
            float localPeak = 0f;
            for (int offset = -2; offset <= 2; offset++)
            {
                int index = center + offset;
                if (index >= 0 && index < onset.Length)
                {
                    localPeak = Mathf.Max(localPeak, onset[index]);
                }
            }

            score += localPeak;
            count++;
        }

        return count > 0 ? score / count : 0f;
    }

    private static BeatCandidate FindClosestCandidate(
        IEnumerable<BeatCandidate> candidates,
        float targetBpm)
    {
        return candidates.OrderBy(candidate => Mathf.Abs(candidate.Bpm - targetBpm)).First();
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

    private readonly struct BeatCandidate
    {
        public BeatCandidate(
            float bpm,
            float phaseSeconds,
            float totalScore,
            float earlyScore,
            float middleScore,
            float lateScore)
        {
            Bpm = bpm;
            PhaseSeconds = phaseSeconds;
            TotalScore = totalScore;
            EarlyScore = earlyScore;
            MiddleScore = middleScore;
            LateScore = lateScore;
        }

        public float Bpm { get; }
        public float PhaseSeconds { get; }
        public float TotalScore { get; }
        public float EarlyScore { get; }
        public float MiddleScore { get; }
        public float LateScore { get; }
    }
}
