using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParserWorkerService.Services.Interface
{
    public interface IUpsertService
    {
        Task<Guid> Save(CancellationToken cancellationToken = default);
    }
}
