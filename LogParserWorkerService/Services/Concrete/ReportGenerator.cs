using LogParserWorkerService.Models;
using LogParserWorkerService.Services.Contracts;

namespace LogParserWorkerService.Services.Services
{
    public class ReportGenerator:IReportGenerator
    {
        private readonly LogReportGenerator _logReportGenerator;
        public ReportGenerator(LogReportGenerator logReportGenerator)
        {
            _logReportGenerator = logReportGenerator;
        }
        public async Task<LogStats> GenerateReportAsync(CancellationToken cancellationToken = default)
        {
            var topUrls = _logReportGenerator.urlCounts
               .OrderByDescending(kv => kv.Value)
               .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
               .Take(3)
               .Select(kv => new TopItem { Key = kv.Key, Count = kv.Value })
               .ToList();

            var topIps = _logReportGenerator.ipCounts
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .Select(kv => new TopItem { Key = kv.Key, Count = kv.Value })
                .ToList();

            return new LogStats
            {
                UniqueIpCount = _logReportGenerator.ipCounts.Count,
                TopUrls = topUrls,
                TopIps = topIps
            };
           
        }
    }
}
