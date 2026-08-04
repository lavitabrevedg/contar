using System;
using UnityEditor;
using UnityEngine;

public static class ProgressFeatureSmokeTest
{
    private const string CurrentStageIndexKey = "contar.progress.currentStageIndex";
    private const string HighestClearedStageIndexKey = "contar.progress.highestClearedStageIndex";
    private const string FailureStageIndexKey = "contar.progress.failureStageIndex";
    private const string FailureCountKey = "contar.progress.failureCount";
    private const string CatalogPath = "Assets/Resources/SettingDatas/StageCatalog.asset";

    [MenuItem("contar/Run Progress Feature Smoke Test")]
    private static void RunFromMenu()
    {
        Run();
    }

    public static void Run()
    {
        SavedPref[] savedPrefs = CapturePrefs();
        GameObject gameObject = new GameObject("ProgressFeatureSmokeTest");

        try
        {
            StageProgressService progressService = gameObject.AddComponent<StageProgressService>();
            progressService.ResetProgress();

            AssertEqual(0, progressService.CurrentStageIndex, "default current stage");
            AssertEqual(-1, progressService.HighestClearedStageIndex, "default highest clear");

            StageClearProgressResult firstClear = progressService.MarkStageCleared(2);
            AssertTrue(firstClear.WasNewClear, "stage 2 should be a new clear");
            AssertEqual(2, progressService.HighestClearedStageIndex, "highest clear after stage 2");

            StageClearProgressResult duplicateClear = progressService.MarkStageCleared(2);
            AssertTrue(!duplicateClear.WasNewClear, "duplicate clear should not be new");

            StageProgressSnapshot snapshot = progressService.CreateSnapshot();
            string snapshotJson = JsonUtility.ToJson(snapshot);
            AssertTrue(!snapshotJson.Contains("failureCount"), "snapshot should not include failure count");
            AssertTrue(!snapshotJson.Contains("skipTicketCount"), "snapshot should not include removed ticket data");
            AssertTrue(snapshotJson.Contains("highestClearedStageIndex"), "snapshot should include highest clear");
            AssertTrue(snapshotJson.Contains("updatedAtUtcTicks"), "snapshot should include update timestamp");

            PlayerPrefs.SetInt(FailureCountKey, 9);
            progressService.ResetProgress();
            AssertTrue(!PlayerPrefs.HasKey(FailureCountKey), "reset should clear legacy failure count key");

            ProgressFeatureSetup.SyncStageCatalog();
            StageCatalog catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath);
            AssertTrue(catalog != null, "stage catalog should exist");
            AssertTrue(catalog.StageCount > 0, "stage catalog should have at least one stage");

            MapData firstStage;
            bool foundFirstStage = catalog.TryGetStage(0, out firstStage);
            AssertTrue(foundFirstStage, "first stage should load");
            AssertTrue(firstStage != null, "first stage should not be null");

            Debug.Log("[ProgressFeatureSmokeTest] Passed. Progress contains only highest clear and timestamp.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
            RestorePrefs(savedPrefs);
        }
    }

    private static SavedPref[] CapturePrefs()
    {
        return new[]
        {
            new SavedPref(CurrentStageIndexKey),
            new SavedPref(HighestClearedStageIndexKey),
            new SavedPref(FailureStageIndexKey),
            new SavedPref(FailureCountKey)
        };
    }

    private static void RestorePrefs(SavedPref[] savedPrefs)
    {
        for (int prefIndex = 0; prefIndex < savedPrefs.Length; prefIndex++)
            savedPrefs[prefIndex].Restore();

        PlayerPrefs.Save();
    }

    private static void AssertEqual(int expected, int actual, string label)
    {
        if (expected != actual)
            throw new InvalidOperationException($"{label}: expected={expected}, actual={actual}");
    }

    private static void AssertTrue(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException(label);
    }

    private readonly struct SavedPref
    {
        private readonly string key;
        private readonly bool hadValue;
        private readonly int value;

        public SavedPref(string key)
        {
            this.key = key;
            hadValue = PlayerPrefs.HasKey(key);
            value = hadValue ? PlayerPrefs.GetInt(key) : 0;
        }

        public void Restore()
        {
            if (hadValue)
            {
                PlayerPrefs.SetInt(key, value);
                return;
            }

            PlayerPrefs.DeleteKey(key);
        }
    }
}
