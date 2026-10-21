namespace EatMeServer
{
    /// <summary>
    /// What a server console line is about, which picks its color (ServerLog.GetColor).
    /// </summary>
    public enum eLogType
    {
        Info,
        Level,
        Join,
        Leave,
        Kill,
        Flag,
        PowerUp,
        Command,
        Help,
        Perf,
        Warning,
        Error,
    }
}
