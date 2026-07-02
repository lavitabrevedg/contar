using System;
using UnityEngine;

public class StageProgressService : MonoBehaviour
{
    private const string HighestClearedStageIndexKey = "contar.progress.highestClearedStageIndex";
    private const string SkipTicketCountKey = "contar.progress.skipTicketCount";
    private const string UpdatedAtUtcTicksKey = "contar.progress.updatedAtUtcTicks";
    private const string LegacyCurrentStageIndexKey = "contar.progress.currentStageIndex";
    private const string LegacyFailureStageIndexKey = "contar.progress.failureStageIndex";
    private const string LegacyFailureCountKey = "contar.progress.failureCount";

    private const int InitialStageIndex = 0;
    private const int InitialHighestClearedStageIndex = -1;
    private const int InitialSkipTicketCount = 3;
    private const int MaxSkipTicketCount = 5;
    private const int SkipTicketGrantInterval = 3;
    private const int AdFreeStageCount = 6;
    private static int pendingStageIndex = -1;

    public int CurrentStageIndex { get; private set; }
    public int HighestClearedStageIndex { get; private set; }
    public int SkipTicketCount { get; private set; }
    public long UpdatedAtUtcTicks { get; private set; }
    public bool HasSkipTicket => SkipTicketCount > 0;
    public bool HasAdSkipTicket => SkipTicketCount > 0;
    public int MaxSkipTicketCountValue => MaxSkipTicketCount;
    public int MaxAdSkipTicketCountValue => MaxSkipTicketCount;

    public event Action ProgressChanged;
    public event Action PersistentProgressChanged;

    private bool hasExplicitStageSelection;

    private void Awake()
    {
        Load();
    }

    public void Load()
    {
        HighestClearedStageIndex = PlayerPrefs.GetInt(HighestClearedStageIndexKey, InitialHighestClearedStageIndex);
        SkipTicketCount = Mathf.Clamp(PlayerPrefs.GetInt(SkipTicketCountKey, InitialSkipTicketCount), 0, MaxSkipTicketCount);
        string updatedAtUtcTicksText = PlayerPrefs.GetString(UpdatedAtUtcTicksKey, "0");
        if (!long.TryParse(updatedAtUtcTicksText, out long updatedAtUtcTicks))
            updatedAtUtcTicks = 0;

        UpdatedAtUtcTicks = updatedAtUtcTicks;

        if (pendingStageIndex >= 0)
        {
            CurrentStageIndex = Mathf.Max(InitialStageIndex, pendingStageIndex);
            pendingStageIndex = -1;
            hasExplicitStageSelection = true;
        }
        else
        {
            CurrentStageIndex = GetContinueStageIndex();
            hasExplicitStageSelection = false;
        }

        NotifyProgressChanged();
    }

    public void Save()
    {
        Save(true);
    }

    private void Save(bool updateTimestamp)
    {
        if (updateTimestamp)
            UpdatedAtUtcTicks = DateTime.UtcNow.Ticks;

        PlayerPrefs.SetInt(HighestClearedStageIndexKey, HighestClearedStageIndex);
        PlayerPrefs.SetInt(SkipTicketCountKey, SkipTicketCount);
        PlayerPrefs.SetString(UpdatedAtUtcTicksKey, UpdatedAtUtcTicks.ToString());
        PlayerPrefs.Save();
    }

    public StageProgressSnapshot CreateSnapshot()
    {
        return new StageProgressSnapshot(
            HighestClearedStageIndex,
            SkipTicketCount,
            UpdatedAtUtcTicks);
    }

    public void EnsurePersistentTimestamp()
    {
        if (UpdatedAtUtcTicks > 0)
            return;

        Save();
    }

    public void ApplySnapshot(StageProgressSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        HighestClearedStageIndex = Mathf.Max(InitialHighestClearedStageIndex, snapshot.highestClearedStageIndex);
        SkipTicketCount = Mathf.Clamp(snapshot.skipTicketCount, 0, MaxSkipTicketCount);
        UpdatedAtUtcTicks = Math.Max(0, snapshot.updatedAtUtcTicks);

        if (!hasExplicitStageSelection)
            CurrentStageIndex = GetContinueStageIndex();

        Save(false);
        NotifyProgressChanged();
    }

    public void SelectStageForPlay(int stageIndex)
    {
        SetCurrentStage(stageIndex);
        pendingStageIndex = CurrentStageIndex;
    }

    public void SetCurrentStage(int stageIndex)
    {
        int clampedStageIndex = Mathf.Max(InitialStageIndex, stageIndex);

        CurrentStageIndex = clampedStageIndex;
        hasExplicitStageSelection = true;

        NotifyProgressChanged();
    }

    public StageClearProgressResult MarkStageCleared(int stageIndex)
    {
        int clampedStageIndex = Mathf.Max(InitialStageIndex, stageIndex);
        bool wasNewClear = clampedStageIndex > HighestClearedStageIndex;
        bool grantedSkipTicket = false;

        if (wasNewClear)
        {
            HighestClearedStageIndex = clampedStageIndex;

            int clearedStageCount = HighestClearedStageIndex + 1;
            if (clearedStageCount % SkipTicketGrantInterval == 0)
                grantedSkipTicket = TryAddSkipTicket(1);
        }

        Save();
        NotifyProgressChanged();
        NotifyPersistentProgressChanged();

        return new StageClearProgressResult(wasNewClear, grantedSkipTicket, SkipTicketCount);
    }

    public bool TryUseSkipTicket()
    {
        if (SkipTicketCount <= 0)
            return false;

        SkipTicketCount--;
        Save();
        NotifyProgressChanged();
        NotifyPersistentProgressChanged();
        return true;
    }

    public bool TryUseAdSkipTicket()
    {
        return TryUseSkipTicket();
    }

    public bool ShouldSuppressAds(int stageIndex)
    {
        return stageIndex < AdFreeStageCount;
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(LegacyCurrentStageIndexKey);
        PlayerPrefs.DeleteKey(HighestClearedStageIndexKey);
        PlayerPrefs.DeleteKey(SkipTicketCountKey);
        PlayerPrefs.DeleteKey(LegacyFailureStageIndexKey);
        PlayerPrefs.DeleteKey(LegacyFailureCountKey);
        PlayerPrefs.DeleteKey(UpdatedAtUtcTicksKey);
        PlayerPrefs.Save();

        CurrentStageIndex = InitialStageIndex;
        HighestClearedStageIndex = InitialHighestClearedStageIndex;
        SkipTicketCount = InitialSkipTicketCount;
        UpdatedAtUtcTicks = DateTime.UtcNow.Ticks;
        hasExplicitStageSelection = false;
        pendingStageIndex = -1;
        Save(false);
        NotifyProgressChanged();
        NotifyPersistentProgressChanged();
    }

    public void ResetToNewUserDefaults()
    {
        PlayerPrefs.DeleteKey(LegacyCurrentStageIndexKey);
        PlayerPrefs.DeleteKey(LegacyFailureStageIndexKey);
        PlayerPrefs.DeleteKey(LegacyFailureCountKey);

        CurrentStageIndex = InitialStageIndex;
        HighestClearedStageIndex = InitialHighestClearedStageIndex;
        SkipTicketCount = InitialSkipTicketCount;
        UpdatedAtUtcTicks = DateTime.UtcNow.Ticks;
        hasExplicitStageSelection = false;
        pendingStageIndex = -1;

        Save(false);
        NotifyProgressChanged();
        NotifyPersistentProgressChanged();
    }

    private bool TryAddSkipTicket(int count)
    {
        int previousCount = SkipTicketCount;
        SkipTicketCount = Mathf.Clamp(SkipTicketCount + count, 0, MaxSkipTicketCount);
        return SkipTicketCount > previousCount;
    }

    private void NotifyProgressChanged()
    {
        ProgressChanged?.Invoke();
    }

    private void NotifyPersistentProgressChanged()
    {
        PersistentProgressChanged?.Invoke();
    }

    private int GetContinueStageIndex()
    {
        return Mathf.Max(InitialStageIndex, HighestClearedStageIndex + 1);
    }
}

[Serializable]
public class StageProgressSnapshot
{
    public int highestClearedStageIndex;
    public int skipTicketCount;
    public long updatedAtUtcTicks;

    public StageProgressSnapshot()
    {
    }

    public StageProgressSnapshot(
        int highestClearedStageIndex,
        int skipTicketCount,
        long updatedAtUtcTicks)
    {
        this.highestClearedStageIndex = highestClearedStageIndex;
        this.skipTicketCount = skipTicketCount;
        this.updatedAtUtcTicks = updatedAtUtcTicks;
    }
}
