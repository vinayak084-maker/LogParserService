namespace LogParserWorkerService.Models
{
    public sealed class LogReportGenerator
    {
        public  HashSet<string> uniqueIps = new();
        public Dictionary<string, int> ipCounts = new();
        public Dictionary<string, int> urlCounts = new();

    }
}
