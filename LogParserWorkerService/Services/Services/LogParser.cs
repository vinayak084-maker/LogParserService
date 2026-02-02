using LogParserWorkerService.Models;
using LogParserWorkerService.Services.Contracts;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LogParserWorkerService.Services.Services
{
    public class LogParser:ILogParser
    {
        private static readonly Regex LineRegex = new(
          @"^(?<ip>\S+) \S+ \S+ \[(?<ts>[^\]]+)\] ""(?<method>\S+)\s+(?<url>\S+)\s+(?<proto>[^""]+)"" (?<status>\d{3}) (?<size>\S+) ""(?<referrer>[^""]*)"" ""(?<agent>[^""]*)""",
          RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private readonly LogReportGenerator _reportGenerator;
        public LogParser(LogReportGenerator reportGenerator)
        {
            _reportGenerator = reportGenerator;
        }

        public async Task LogDataParseAsync(string line,CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(line))
            {
                throw new ArgumentException("Log line cannot be null or empty.", nameof(line));
            }
           
            var entry = TryParseLine(line, out var logData);
            if(!entry || logData == null)
            {
                // Handle parsing error if needed
                throw new FormatException("Log line is not in the expected format.");
            }

            // Count unique IPs and ip activity
            if (!string.IsNullOrEmpty(logData.IPAddress))
            {
                _reportGenerator.uniqueIps.Add(logData.IPAddress);
                _reportGenerator.ipCounts.TryGetValue(logData.IPAddress, out var c);
                _reportGenerator.ipCounts[logData.IPAddress] = c + 1;
            }

            var urlKey = NormalizeUrlForCounting(logData.Url);

            if (!string.IsNullOrEmpty(logData.Url))
            {
                _reportGenerator.urlCounts.TryGetValue(logData.Url, out var uc);
                _reportGenerator.urlCounts[logData.Url] = uc + 1;
            }
           
            
        }

        public static bool TryParseLine(string line, out LogData? entry)
        {
            entry = null;
            var m = LineRegex.Match(line);
            if (!m.Success) return false;

            try
            {
                var ip = m.Groups["ip"].Value;
                var tsRaw = m.Groups["ts"].Value; 
                var url = m.Groups["url"].Value;               

                var timestamp = ParseTimestamp(tsRaw);

              
                entry = new LogData
                {
                    IPAddress = ip,
                    TimeStamp = timestamp,
                    Url = url,

                };
                return true;
            }
            catch
            {
                return false;
            }
        }
        private static DateTime ParseTimestamp(string tsRaw)
        {
            // tsRaw example: 10/Jul/2018:22:21:28 +0200
            // DateTimeOffset expects +02:00 so insert colon into offset
            // if offset is +HHMM or -HHMM:
            if (!string.IsNullOrEmpty(tsRaw) && tsRaw.Length > 5)
            {
                var off = tsRaw.Substring(tsRaw.Length - 5);
                if ((off[0] == '+' || off[0] == '-') && int.TryParse(off.Substring(1), out _))
                {
                    // create +HH:MM
                    var withColon = off.Insert(3, ":");
                    tsRaw = tsRaw.Substring(0, tsRaw.Length - 5) + withColon;
                }
            }

            // parse format: dd/MMM/yyyy:HH:mm:ss zzz
            return DateTime.ParseExact(tsRaw, "dd/MMM/yyyy:HH:mm:ss zzz", CultureInfo.InvariantCulture);
        }

        private static string NormalizeUrlForCounting(string rawUrl)
        {
            if (string.IsNullOrEmpty(rawUrl)) return rawUrl;

            // Option: strip query string to group by path only:
            var idx = rawUrl.IndexOf('?');
            if (idx >= 0) return rawUrl.Substring(0, idx);

            return rawUrl;
        }

    }
}
