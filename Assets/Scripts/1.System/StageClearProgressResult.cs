public readonly struct StageClearProgressResult
{
    public StageClearProgressResult(bool wasNewClear)
    {
        WasNewClear = wasNewClear;
    }

    public bool WasNewClear { get; }
}
