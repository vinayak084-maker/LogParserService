using System.Runtime.CompilerServices;

namespace LogParserWorkerService.Services.Contracts
{
   public interface ILogDataReader
    {
        IAsyncEnumerable<string> ReadLineByLineAsync([EnumeratorCancellation] CancellationToken cancellationToken = default);
    }
}
