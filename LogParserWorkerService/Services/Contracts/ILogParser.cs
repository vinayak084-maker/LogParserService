namespace LogParserWorkerService.Services.Contracts
{
    public interface ILogParser
    {
        Task LogDataParseAsync(string line, CancellationToken cancellationToken = default);
    }
}
