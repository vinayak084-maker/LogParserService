
namespace LogParserWorkerService.Repositories.Interface
{
    public interface IBulkLogRepository
    {
        Task<Guid> InsertSnapshotBulkAsync();
    }
}
