namespace LogParserWorkerService.Models
{
    public sealed class LogStats
    {
        public int UniqueIpCount { get; init; }
        public List<TopItem> TopUrls { get; init; } = new();
        public List<TopItem> TopIps { get; init; } = new();
    }
}
