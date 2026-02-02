using LogParserWorkerService.Services.Contracts;
using System.Runtime.CompilerServices;


namespace LogParserWorkerService.Services.Services
{
    public class LogDataReader : ILogDataReader
    {
        private readonly string _fullFilePath;
        public LogDataReader(IConfiguration configuration, IHostEnvironment env)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            var path = configuration.GetValue<string>("LoggingFile:Path");
            if(string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Logging file path is not configured properly.");
            }

            _fullFilePath = Path.Combine(env.ContentRootPath, path);
        }       

        public async IAsyncEnumerable<string> ReadLineByLineAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_fullFilePath))
            {
                throw new FileNotFoundException("Log file not found.", _fullFilePath);
            }

            using (var stream = new FileStream(_fullFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield  return line;
                }
                
            }

           


        }   
    }
}
