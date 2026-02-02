using System.Data;
using System.Data.SqlClient;
using Dapper;
using LogParserWorkerService.Models;
using LogParserWorkerService.Repositories.Interface;
using LogParserWorkerService.Services.Contracts;


namespace LogParserWorkerService.Repositories.Concrete
{
    public class BulkLogRepository: IBulkLogRepository
    {
        private readonly string _connectionString;
        private readonly LogReportGenerator _reportGenerator;
        public BulkLogRepository(IConfiguration configuration, LogReportGenerator reportGenerator)
        {
            _connectionString = configuration.GetValue<string>("ConnectionStrings:LogParserDB");
            _reportGenerator = reportGenerator;
        }
        public async Task<Guid> InsertSnapshotBulkAsync()
        {
            var snapshotId = Guid.NewGuid();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync().ConfigureAwait(false);

            using var tx = conn.BeginTransaction();
            try
            {
                // 1) Insert snapshot row using Dapper (quick single insert)
                const string insertSnapshotSql = "INSERT INTO dbo.LogSnapshot (SnapshotId) VALUES (@SnapshotId);";
                await conn.ExecuteAsync(insertSnapshotSql, new { SnapshotId = snapshotId }, tx).ConfigureAwait(false);

                // 2) Prepare DataTables
                DataTable dtUniqueIps = new DataTable();
                dtUniqueIps.Columns.Add("SnapshotId", typeof(Guid));
                dtUniqueIps.Columns.Add("Ip", typeof(string));
                if (_reportGenerator.uniqueIps != null)
                {
                    foreach (var ip in _reportGenerator.uniqueIps.Where(ip => !string.IsNullOrWhiteSpace(ip)))
                        dtUniqueIps.Rows.Add(snapshotId, ip.Trim());
                }

                DataTable dtIpCounts = new DataTable();
                dtIpCounts.Columns.Add("SnapshotId", typeof(Guid));
                dtIpCounts.Columns.Add("Ip", typeof(string));
                dtIpCounts.Columns.Add("Count", typeof(int));
                if (_reportGenerator.ipCounts != null)
                {
                    foreach (var kv in _reportGenerator.ipCounts.Where(kv => !string.IsNullOrWhiteSpace(kv.Key)))
                        dtIpCounts.Rows.Add(snapshotId, kv.Key.Trim(), kv.Value);
                }

                DataTable dtUrlCounts = new DataTable();
                dtUrlCounts.Columns.Add("SnapshotId", typeof(Guid));
                dtUrlCounts.Columns.Add("Url", typeof(string));
                dtUrlCounts.Columns.Add("Count", typeof(int));
                if (_reportGenerator.urlCounts != null)
                {
                    foreach (var kv in _reportGenerator.urlCounts.Where(kv => !string.IsNullOrWhiteSpace(kv.Key)))
                    {
                        var url = kv.Key.Trim();
                        if (url.Length > 500) url = url.Substring(0, 500); // same rule as table definition
                        dtUrlCounts.Rows.Add(snapshotId, url, kv.Value);
                    }
                }

                // 3) Bulk copy each DataTable
                // Use SqlBulkCopyOptions.CheckConstraints if you want constraints checked during bulk load
                // Use SqlBulkCopyOptions.TableLock for max speed (acquires table-level lock)
                var bulkOptions = SqlBulkCopyOptions.FireTriggers; // triggers will still fire; add TableLock for speed if acceptable
                using (var bulk = new SqlBulkCopy(conn, bulkOptions, tx))
                {
                    bulk.DestinationTableName = "dbo.UniqueIps";
                    await bulk.WriteToServerAsync(dtUniqueIps).ConfigureAwait(false);
                }

                using (var bulk = new SqlBulkCopy(conn, bulkOptions, tx))
                {
                    bulk.DestinationTableName = "dbo.IpCounts";
                    await bulk.WriteToServerAsync(dtIpCounts).ConfigureAwait(false);
                }

                using (var bulk = new SqlBulkCopy(conn, bulkOptions, tx))
                {
                    bulk.DestinationTableName = "dbo.UrlCounts";
                    await bulk.WriteToServerAsync(dtUrlCounts).ConfigureAwait(false);
                }

                tx.Commit();
                return snapshotId;
            }
            catch
            {
                try { tx.Rollback(); } catch { /* best-effort */ }
                throw;
            }
        }
    }
}
