using LogParserWorkerService.Services.Contracts;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LogParserWorkerService
{
    [ExcludeFromCodeCoverage]
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ILogDataReader _logDataReader;
        private readonly ILogParser _logDataParser;
        private readonly IReportGenerator _reportGenerator;
        public Worker(ILogger<Worker> logger, ILogDataReader logDataReader, ILogParser logDataParser, IReportGenerator reportGenerator)
        {
            _logger = logger;
            _logDataReader = logDataReader;
            _logDataParser = logDataParser;
            _reportGenerator = reportGenerator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _logger.LogInformation("Starting log analysis...");
            try
            {
                var tasks = new List<Task>();

                await foreach (var value in _logDataReader.ReadLineByLineAsync(stoppingToken))
                {
                    tasks.Add(_logDataParser.LogDataParseAsync(value ?? string.Empty, stoppingToken));
                }

                await Task.WhenAll(tasks);


                var result = await _reportGenerator.GenerateReportAsync(stoppingToken);

                var sb = new StringBuilder();
                sb.AppendLine("=== Log Stats ===");
                sb.AppendLine($"Unique IPs: {result.UniqueIpCount}");
                sb.AppendLine("Top 3 URLs:");
                foreach (var u in result.TopUrls) sb.AppendLine($"  {u.Key} => {u.Count}");
                sb.AppendLine("Top 3 IPs:");
                foreach (var ip in result.TopIps) sb.AppendLine($"  {ip.Key} => {ip.Count}");

                _logger.LogInformation(sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while analyzing logs");
            }
        }
    }
}
