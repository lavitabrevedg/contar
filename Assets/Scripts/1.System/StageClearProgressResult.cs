public readonly struct StageClearProgressResult
{
    public StageClearProgressResult(bool wasNewClear, bool grantedSkipTicket, int skipTicketCount)
    {
        WasNewClear = wasNewClear;
        GrantedSkipTicket = grantedSkipTicket;
        SkipTicketCount = skipTicketCount;
    }

    public bool WasNewClear { get; }
    public bool GrantedSkipTicket { get; }
    public int SkipTicketCount { get; }
}
