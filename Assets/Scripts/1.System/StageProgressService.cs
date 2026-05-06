using UnityEngine;

public class StageProgressService : MonoBehaviour
{
    private const string CurrentStageIndexKey = "contar.progress.currentStageIndex";
    private const string HighestClearedStageIndexKey = "contar.progress.highestClearedStageIndex";
    private const string SkipTicketCountKey = "contar.progress.skipTicketCount";
    private const string FailureStageIndexKey = "contar.progress.failureStageIndex";
    private const string FailureCountKey = "contar.progress.failureCount";

    private const int InitialStageIndex = 0;
    private const int InitialHighestClearedStageIndex = -1;
    private const int InitialSkipTicketCount = 3;
    private const int MaxSkipTicketCount = 5;
    private const int SkipTicketGrantInterval = 3;
    private const int AdFreeStageCount = 6;

    public int CurrentStageIndex { get; private set; }
    public int HighestClearedStageIndex { get; private set; }
    public int SkipTicketCount { get; private set; }
    public int FailureStageIndex { get; private set; }
    public int FailureCount { get; private set; }

    private void Awake()
    {
        Load();
    }

    public void Load()
    {
        CurrentStageIndex = Mathf.Max(InitialStageIndex, PlayerPrefs.GetInt(CurrentStageIndexKey, InitialStageIndex));
        HighestClearedStageIndex = PlayerPrefs.GetInt(HighestClearedStageIndexKey, InitialHighestClearedStageIndex);
        SkipTicketCount = Mathf.Clamp(PlayerPrefs.GetInt(SkipTicketCountKey, InitialSkipTicketCount), 0, MaxSkipTicketCount);
        FailureStageIndex = PlayerPrefs.GetInt(FailureStageIndexKey, CurrentStageIndex);
        FailureCount = Mathf.Max(0, PlayerPrefs.GetInt(FailureCountKey, 0));
    }

    public void Save()
    {
        PlayerPrefs.SetInt(CurrentStageIndexKey, CurrentStageIndex);
        PlayerPrefs.SetInt(HighestClearedStageIndexKey, HighestClearedStageIndex);
        PlayerPrefs.SetInt(SkipTicketCountKey, SkipTicketCount);
        PlayerPrefs.SetInt(FailureStageIndexKey, FailureStageIndex);
        PlayerPrefs.SetInt(FailureCountKey, FailureCount);
        PlayerPrefs.Save();
    }

    public void SetCurrentStage(int stageIndex)
    {
        int clampedStageIndex = Mathf.Max(InitialStageIndex, stageIndex);
        bool stageChanged = CurrentStageIndex != clampedStageIndex;

        CurrentStageIndex = clampedStageIndex;

        if (stageChanged)
            ResetFailureCount();

        Save();
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

        ResetFailureCount();
        Save();

        return new StageClearProgressResult(wasNewClear, grantedSkipTicket, SkipTicketCount);
    }

    public int RecordFailure(int stageIndex)
    {
        int clampedStageIndex = Mathf.Max(InitialStageIndex, stageIndex);

        if (FailureStageIndex != clampedStageIndex)
        {
            FailureStageIndex = clampedStageIndex;
            FailureCount = 0;
        }

        FailureCount++;
        Save();

        return FailureCount;
    }

    public bool TryUseSkipTicket()
    {
        if (SkipTicketCount <= 0)
            return false;

        SkipTicketCount--;
        Save();
        return true;
    }

    public bool ShouldSuppressAds(int stageIndex)
    {
        return stageIndex < AdFreeStageCount;
    }

    public void ResetFailureCount()
    {
        FailureStageIndex = CurrentStageIndex;
        FailureCount = 0;
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(CurrentStageIndexKey);
        PlayerPrefs.DeleteKey(HighestClearedStageIndexKey);
        PlayerPrefs.DeleteKey(SkipTicketCountKey);
        PlayerPrefs.DeleteKey(FailureStageIndexKey);
        PlayerPrefs.DeleteKey(FailureCountKey);
        PlayerPrefs.Save();
        Load();
    }

    private bool TryAddSkipTicket(int count)
    {
        int previousCount = SkipTicketCount;
        SkipTicketCount = Mathf.Clamp(SkipTicketCount + count, 0, MaxSkipTicketCount);
        return SkipTicketCount > previousCount;
    }
}
