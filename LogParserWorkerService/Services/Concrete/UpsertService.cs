using LogParserWorkerService.Repositories.Interface;
using LogParserWorkerService.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParserWorkerService.Services.Concrete
{
    public class UpsertService : IUpsertService
    {
        private readonly IBulkLogRepository _bulkLogRepository;
        public UpsertService(IBulkLogRepository bulkLogRepository)
        {
            _bulkLogRepository = bulkLogRepository;
        }

        public async Task<Guid> Save(CancellationToken cancellationToken = default)
        {
            return await _bulkLogRepository.InsertSnapshotBulkAsync();
        }
    }

}
