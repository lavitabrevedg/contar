using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ContarOptimizationProfiler
{
    private const string InGameScenePath = "Assets/Scenes/InGameScene.unity";
    private const string FontAssetPath = "Assets/TextMesh Pro/Fonts/Digitalt SDF.asset";
    private const string StateKey = "ContarOptimizationProfiler.State";
    private const string LabelKey = "ContarOptimizationProfiler.Label";
    private const string RunInBackgroundKey = "ContarOptimizationProfiler.RunInBackground";
    private const string EnterPlayState = "EnterPlay";
    private const string ExitPlayState = "ExitPlay";
    private const int WarmupFrameCount = 120;
    private const int MeasurementFrameCount = 600;
    private const int MapGenerationIterationCount = 20;
    private const int SolverIterationCount = 10;

    private static readonly List<long> mainThreadSamples = new List<long>(MeasurementFrameCount);
    private static readonly List<long> gcAllocatedSamples = new List<long>(MeasurementFrameCount);
    private static readonly List<long> drawCallSamples = new List<long>(MeasurementFrameCount);
    private static readonly List<long> batchSamples = new List<long>(MeasurementFrameCount);
    private static readonly List<long> mapGenerationNanoseconds = new List<long>(MapGenerationIterationCount);
    private static readonly List<long> mapGenerationManagedBytes = new List<long>(MapGenerationIterationCount);

    private static ProfilerRecorder mainThreadRecorder;
    private static ProfilerRecorder gcAllocatedRecorder;
    private static ProfilerRecorder totalUsedMemoryRecorder;
    private static ProfilerRecorder totalReservedMemoryRecorder;
    private static ProfilerRecorder drawCallsRecorder;
    private static ProfilerRecorder batchesRecorder;
    private static bool recordersStarted;
    private static bool isCompletingEditMode;
    private static int warmupFrames;
    private static int measuredFrames;
    private static int mapGenerationIterations;
    private static int mapWaitFrames;
    private static int lastObservedFrame = -1;

    static ContarOptimizationProfiler()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
    }

    [MenuItem("Tools/Optimization/Run Baseline _F8")]
    public static void RunBaseline()
    {
        BeginProfile("baseline");
    }

    [MenuItem("Tools/Optimization/Run After _F9")]
    public static void RunAfter()
    {
        BeginProfile("after");
    }

    public static void RunBuildOnlyFromCommandLine()
    {
        string label = GetCommandLineArgument("-optimizationProfileLabel", "baseline");
        OptimizationProfileData profileData = ReadProfile(label);
        CaptureAndroidBuildProfile(profileData);
        WriteProfile(profileData);

        if (!string.Equals(profileData.buildResult, BuildResult.Succeeded.ToString(), StringComparison.Ordinal))
            throw new InvalidOperationException($"Android profile build failed: {profileData.buildResult}");
    }

    private static void BeginProfile(string label)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            UnityEngine.Debug.LogError("[Optimization] Exit Play Mode before starting a profile run.");
            return;
        }

        SessionState.SetString(LabelKey, label);
        SessionState.SetString(StateKey, EnterPlayState);
        SessionState.SetBool(RunInBackgroundKey, Application.runInBackground);
        Application.runInBackground = true;

        UnityEngine.Debug.Log($"[Optimization] Starting {label} profile.");
        EditorApplication.isPlaying = true;
    }

    private static void Update()
    {
        string state = SessionState.GetString(StateKey, string.Empty);
        if (string.Equals(state, EnterPlayState, StringComparison.Ordinal) && EditorApplication.isPlaying)
        {
            UpdatePlayModeProfile();
            return;
        }

        if (string.Equals(state, ExitPlayState, StringComparison.Ordinal)
            && !EditorApplication.isPlayingOrWillChangePlaymode
            && !isCompletingEditMode)
        {
            CompleteEditModeProfile();
        }
    }

    private static void UpdatePlayModeProfile()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.Equals(activeScene.path, InGameScenePath, StringComparison.Ordinal))
        {
            SceneManager.LoadScene(InGameScenePath, LoadSceneMode.Single);
            return;
        }

        if (!recordersStarted)
            StartRecorders();

        if (Time.frameCount == lastObservedFrame)
            return;

        lastObservedFrame = Time.frameCount;
        if (warmupFrames < WarmupFrameCount)
        {
            warmupFrames++;
            return;
        }

        if (measuredFrames < MeasurementFrameCount)
        {
            CaptureFrameSamples();
            measuredFrames++;
            return;
        }

        if (mapGenerationIterations < MapGenerationIterationCount)
        {
            if (mapWaitFrames > 0)
            {
                mapWaitFrames--;
                return;
            }

            CaptureMapGenerationSample();
            mapGenerationIterations++;
            mapWaitFrames = 2;
            return;
        }

        SaveRuntimeProfile();
        StopRecorders();
        SessionState.SetString(StateKey, ExitPlayState);
        EditorApplication.isPlaying = false;
    }

    private static void StartRecorders()
    {
        ResetRuntimeState();
        mainThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", MeasurementFrameCount);
        gcAllocatedRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", MeasurementFrameCount);
        totalUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
        totalReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
        drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", MeasurementFrameCount);
        batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", MeasurementFrameCount);
        recordersStarted = true;
    }

    private static void ResetRuntimeState()
    {
        mainThreadSamples.Clear();
        gcAllocatedSamples.Clear();
        drawCallSamples.Clear();
        batchSamples.Clear();
        mapGenerationNanoseconds.Clear();
        mapGenerationManagedBytes.Clear();
        warmupFrames = 0;
        measuredFrames = 0;
        mapGenerationIterations = 0;
        mapWaitFrames = 0;
        lastObservedFrame = -1;
    }

    private static void CaptureFrameSamples()
    {
        AddRecorderValue(mainThreadRecorder, mainThreadSamples);
        AddRecorderValue(gcAllocatedRecorder, gcAllocatedSamples);
        AddRecorderValue(drawCallsRecorder, drawCallSamples);
        AddRecorderValue(batchesRecorder, batchSamples);
    }

    private static void AddRecorderValue(ProfilerRecorder recorder, List<long> samples)
    {
        if (recorder.Valid)
            samples.Add(recorder.LastValue);
    }

    private static void CaptureMapGenerationSample()
    {
        MapGenerator mapGenerator = UnityEngine.Object.FindFirstObjectByType<MapGenerator>();
        if (mapGenerator == null)
            return;

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long timestampBefore = Stopwatch.GetTimestamp();
        mapGenerator.GenerateMap();
        long timestampAfter = Stopwatch.GetTimestamp();
        long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

        double elapsedSeconds = (timestampAfter - timestampBefore) / (double)Stopwatch.Frequency;
        mapGenerationNanoseconds.Add((long)(elapsedSeconds * 1000000000d));
        mapGenerationManagedBytes.Add(Math.Max(0L, allocatedAfter - allocatedBefore));
    }

    private static void SaveRuntimeProfile()
    {
        OptimizationProfileData profileData = new OptimizationProfileData();
        profileData.label = SessionState.GetString(LabelKey, "profile");
        profileData.measuredAtUtc = DateTime.UtcNow.ToString("O");
        profileData.unityVersion = Application.unityVersion;
        profileData.activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
        profileData.frameSampleCount = mainThreadSamples.Count;
        profileData.mainThreadAverageMs = OptimizationStatistics.AverageMilliseconds(mainThreadSamples);
        profileData.mainThreadP95Ms = OptimizationStatistics.PercentileMilliseconds(mainThreadSamples, 0.95d);
        profileData.mainThreadP99Ms = OptimizationStatistics.PercentileMilliseconds(mainThreadSamples, 0.99d);
        profileData.gcAllocatedAverageBytes = OptimizationStatistics.Average(gcAllocatedSamples);
        profileData.gcAllocatedPeakBytes = OptimizationStatistics.Maximum(gcAllocatedSamples);
        profileData.drawCallsAverage = OptimizationStatistics.Average(drawCallSamples);
        profileData.batchesAverage = OptimizationStatistics.Average(batchSamples);
        profileData.totalUsedMemoryBytes = totalUsedMemoryRecorder.Valid ? totalUsedMemoryRecorder.LastValue : 0L;
        profileData.totalReservedMemoryBytes = totalReservedMemoryRecorder.Valid ? totalReservedMemoryRecorder.LastValue : 0L;
        profileData.mapGenerationIterations = mapGenerationNanoseconds.Count;
        profileData.mapGenerationAverageMs = OptimizationStatistics.AverageMilliseconds(mapGenerationNanoseconds);
        profileData.mapGenerationP95Ms = OptimizationStatistics.PercentileMilliseconds(mapGenerationNanoseconds, 0.95d);
        profileData.mapGenerationAverageManagedBytes = OptimizationStatistics.Average(mapGenerationManagedBytes);
        WriteProfile(profileData);
    }

    private static void StopRecorders()
    {
        DisposeRecorder(ref mainThreadRecorder);
        DisposeRecorder(ref gcAllocatedRecorder);
        DisposeRecorder(ref totalUsedMemoryRecorder);
        DisposeRecorder(ref totalReservedMemoryRecorder);
        DisposeRecorder(ref drawCallsRecorder);
        DisposeRecorder(ref batchesRecorder);
        recordersStarted = false;
    }

    private static void DisposeRecorder(ref ProfilerRecorder recorder)
    {
        if (recorder.Valid)
            recorder.Dispose();
    }

    private static void CompleteEditModeProfile()
    {
        isCompletingEditMode = true;
        string label = SessionState.GetString(LabelKey, "profile");
        OptimizationProfileData profileData = ReadProfile(label);

        try
        {
            CaptureSolverProfile(profileData);
            CaptureFontProfile(profileData);
        }
        catch (Exception exception)
        {
            profileData.errorMessage = exception.ToString();
            UnityEngine.Debug.LogException(exception);
        }
        finally
        {
            WriteProfile(profileData);
            Application.runInBackground = SessionState.GetBool(RunInBackgroundKey, Application.runInBackground);
            SessionState.EraseString(StateKey);
            SessionState.EraseString(LabelKey);
            SessionState.EraseBool(RunInBackgroundKey);
            isCompletingEditMode = false;
            UnityEngine.Debug.Log($"[Optimization] Completed {label} profile: {GetProfilePath(label)}");
        }
    }

    private static void CaptureSolverProfile(OptimizationProfileData profileData)
    {
        string[] stageGuids = AssetDatabase.FindAssets("t:MapData", new[] { "Assets/Data/Stages" });
        Array.Sort(stageGuids, StringComparer.Ordinal);
        List<MapData> stages = new List<MapData>(stageGuids.Length);

        for (int stageIndex = 0; stageIndex < stageGuids.Length; stageIndex++)
        {
            string stagePath = AssetDatabase.GUIDToAssetPath(stageGuids[stageIndex]);
            MapData mapData = AssetDatabase.LoadAssetAtPath<MapData>(stagePath);
            if (mapData != null)
                stages.Add(mapData);
        }

        for (int stageIndex = 0; stageIndex < stages.Count; stageIndex++)
            PuzzleSolver.SolveInitial(stages[stageIndex]);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        int maximumStatesExplored = 0;
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int iterationIndex = 0; iterationIndex < SolverIterationCount; iterationIndex++)
        {
            for (int stageIndex = 0; stageIndex < stages.Count; stageIndex++)
            {
                PuzzleSolveResult solveResult = PuzzleSolver.SolveInitial(stages[stageIndex]);
                maximumStatesExplored = Math.Max(maximumStatesExplored, solveResult.StatesExplored);
            }
        }

        stopwatch.Stop();
        long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
        int operationCount = stages.Count * SolverIterationCount;
        profileData.solverStageCount = stages.Count;
        profileData.solverIterationsPerStage = SolverIterationCount;
        profileData.solverAverageMs = operationCount == 0 ? 0d : stopwatch.Elapsed.TotalMilliseconds / operationCount;
        profileData.solverAverageManagedBytes = operationCount == 0
            ? 0L
            : Math.Max(0L, allocatedAfter - allocatedBefore) / operationCount;
        profileData.solverMaximumStatesExplored = maximumStatesExplored;
    }

    private static void CaptureFontProfile(OptimizationProfileData profileData)
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
            return;

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (!string.IsNullOrEmpty(projectRoot))
        {
            string absoluteFontPath = Path.Combine(projectRoot, FontAssetPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absoluteFontPath))
                profileData.fontAssetFileBytes = new FileInfo(absoluteFontPath).Length;
        }

        Texture2D atlasTexture = fontAsset.atlasTexture;
        if (atlasTexture != null)
            profileData.fontAtlasRuntimeBytes = Profiler.GetRuntimeMemorySizeLong(atlasTexture);

        profileData.fontAtlasWidth = fontAsset.atlasWidth;
        profileData.fontAtlasHeight = fontAsset.atlasHeight;
        profileData.fontCharacterCount = fontAsset.characterTable == null ? 0 : fontAsset.characterTable.Count;
    }

    private static void CaptureAndroidBuildProfile(OptimizationProfileData profileData)
    {
        List<string> scenePaths = new List<string>();
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        for (int sceneIndex = 0; sceneIndex < buildScenes.Length; sceneIndex++)
        {
            if (buildScenes[sceneIndex].enabled)
                scenePaths.Add(buildScenes[sceneIndex].path);
        }

        if (scenePaths.Count == 0)
            throw new InvalidOperationException("No enabled build scenes were found.");

        string outputFolder = Path.Combine(Path.GetTempPath(), "ContarOptimizationProfiler");
        Directory.CreateDirectory(outputFolder);
        string outputPath = Path.Combine(outputFolder, $"Contar-{profileData.label}.apk");
        bool previousBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
        bool previousUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        Stopwatch buildStopwatch = Stopwatch.StartNew();

        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.useCustomKeystore = false;
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenePaths.ToArray(),
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development | BuildOptions.CompressWithLz4
            };

            BuildReport buildReport = BuildPipeline.BuildPlayer(buildOptions);
            profileData.buildResult = buildReport.summary.result.ToString();
            profileData.buildReportBytes = buildReport.summary.totalSize;
            if (buildReport.summary.result == BuildResult.Succeeded && File.Exists(outputPath))
                profileData.apkFileBytes = new FileInfo(outputPath).Length;
        }
        finally
        {
            buildStopwatch.Stop();
            profileData.buildDurationSeconds = buildStopwatch.Elapsed.TotalSeconds;
            EditorUserBuildSettings.buildAppBundle = previousBuildAppBundle;
            PlayerSettings.Android.useCustomKeystore = previousUseCustomKeystore;
        }
    }

    private static OptimizationProfileData ReadProfile(string label)
    {
        string profilePath = GetProfilePath(label);
        if (!File.Exists(profilePath))
            return new OptimizationProfileData { label = label };

        string profileJson = File.ReadAllText(profilePath);
        OptimizationProfileData profileData = JsonUtility.FromJson<OptimizationProfileData>(profileJson);
        return profileData ?? new OptimizationProfileData { label = label };
    }

    private static void WriteProfile(OptimizationProfileData profileData)
    {
        string profilePath = GetProfilePath(profileData.label);
        string profileFolder = Path.GetDirectoryName(profilePath);
        if (!string.IsNullOrEmpty(profileFolder))
            Directory.CreateDirectory(profileFolder);

        File.WriteAllText(profilePath, JsonUtility.ToJson(profileData, true));
    }

    private static string GetProfilePath(string label)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            throw new InvalidOperationException("Could not resolve the project root.");

        return Path.Combine(projectRoot, "Docs", "Optimization", $"{label}.json");
    }

    private static string GetCommandLineArgument(string argumentName, string defaultValue)
    {
        string[] commandLineArguments = Environment.GetCommandLineArgs();
        for (int argumentIndex = 0; argumentIndex < commandLineArguments.Length - 1; argumentIndex++)
        {
            if (string.Equals(commandLineArguments[argumentIndex], argumentName, StringComparison.Ordinal))
                return commandLineArguments[argumentIndex + 1];
        }

        return defaultValue;
    }
}
