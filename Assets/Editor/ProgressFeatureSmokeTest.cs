using System;
using UnityEditor;
using UnityEngine;

public static class ProgressFeatureSmokeTest
{
    private const string CurrentStageIndexKey = "contar.progress.currentStageIndex";
    private const string HighestClearedStageIndexKey = "contar.progress.highestClearedStageIndex";
    private const string SkipTicketCountKey = "contar.progress.skipTicketCount";
    private const string FailureStageIndexKey = "contar.progress.failureStageIndex";
    private const string FailureCountKey = "contar.progress.failureCount";
    private const string CatalogPath = "Assets/Resources/StageCatalog.asset";

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
            AssertEqual(3, progressService.SkipTicketCount, "default skip tickets");
            AssertTrue(progressService.ShouldSuppressAds(0), "stage 0 should suppress ads");
            AssertTrue(!progressService.ShouldSuppressAds(6), "stage 6 should allow ads");

            StageClearProgressResult firstReward = progressService.MarkStageCleared(2);
            AssertTrue(firstReward.WasNewClear, "stage 2 should be a new clear");
            AssertTrue(firstReward.GrantedSkipTicket, "stage 2 should grant a skip ticket");
            AssertEqual(4, firstReward.SkipTicketCount, "skip ticket after third clear");

            StageClearProgressResult duplicateReward = progressService.MarkStageCleared(2);
            AssertTrue(!duplicateReward.WasNewClear, "duplicate clear should not be new");
            AssertTrue(!duplicateReward.GrantedSkipTicket, "duplicate clear should not grant");
            AssertEqual(4, duplicateReward.SkipTicketCount, "skip ticket after duplicate clear");

            int firstFailureCount = progressService.RecordFailure(7);
            int secondFailureCount = progressService.RecordFailure(7);
            AssertEqual(1, firstFailureCount, "first failure count");
            AssertEqual(2, secondFailureCount, "second failure count");

            AssertTrue(progressService.TryUseSkipTicket(), "first skip ticket should be usable");
            AssertTrue(progressService.TryUseSkipTicket(), "second skip ticket should be usable");
            AssertTrue(progressService.TryUseSkipTicket(), "third skip ticket should be usable");
            AssertTrue(progressService.TryUseSkipTicket(), "fourth skip ticket should be usable");
            AssertTrue(!progressService.TryUseSkipTicket(), "skip ticket should not be usable at zero");
            AssertEqual(0, progressService.SkipTicketCount, "skip ticket after uses");

            progressService.SetCurrentStage(6);
            AssertTrue(!progressService.ShouldSuppressAds(progressService.CurrentStageIndex), "stage 6 should allow ads");

            DummyAdService adService = gameObject.AddComponent<DummyAdService>();
            bool adCompleted = false;
            bool adSucceeded = false;
            adService.Show(AdPlacement.SkipStage, success =>
            {
                adCompleted = true;
                adSucceeded = success;
            });
            AssertTrue(adCompleted, "dummy skip ad should complete");
            AssertTrue(adSucceeded, "dummy skip ad should succeed");

            ProgressFeatureSetup.SyncStageCatalog();
            StageCatalog catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath);
            AssertTrue(catalog != null, "stage catalog should exist");
            AssertTrue(catalog.StageCount > 0, "stage catalog should have at least one stage");

            bool foundFirstStage = catalog.TryGetStage(0, out MapData firstStage);
            AssertTrue(foundFirstStage, "first stage should load");
            AssertTrue(firstStage != null, "first stage should not be null");

            Debug.Log("[ProgressFeatureSmokeTest] Passed.");
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
            new SavedPref(SkipTicketCountKey),
            new SavedPref(FailureStageIndexKey),
            new SavedPref(FailureCountKey)
        };
    }

    private static void RestorePrefs(SavedPref[] savedPrefs)
    {
        for (int i = 0; i < savedPrefs.Length; i++)
            savedPrefs[i].Restore();

        PlayerPrefs.Save();
    }

    private static void AssertEqual(int expected, int actual, string label)
    {
        if (expected != actual)
            throw new InvalidOperationException($"{label}: expected={expected}, actual={actual}");
    }

    private static void AssertEqual(string expected, string actual, string label)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
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
