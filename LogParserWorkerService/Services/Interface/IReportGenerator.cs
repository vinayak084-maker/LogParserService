using LogParserWorkerService.Models;

namespace LogParserWorkerService.Services.Contracts
{
    public interface IReportGenerator
    {
        Task<LogStats> GenerateReportAsync(CancellationToken cancellationToken = default);
    }
}
