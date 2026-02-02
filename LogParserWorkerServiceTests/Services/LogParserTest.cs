using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using LogParserWorkerService.Services.Services;
using LogParserWorkerService.Models;

namespace LogParserWorkerService.Tests
{
    [TestClass]
    public class LogParserTests
    {
        private static string MakeLogLine(string ip, string ts, string url) =>
            $"{ip} - - [{ts}] \"GET {url} HTTP/1.1\" 200 123 \"-\" \"UnitTestAgent/1.0\"";

        [TestMethod]
        public void TryParseLine_ValidLine_ReturnsTrueAndPopulatesLogData()
        {
            // Arrange
            var tsRaw = "10/Jul/2018:22:21:28 +0200"; // offset without colon
            var url = "/index.html?x=1";
            var line = MakeLogLine("127.0.0.1", tsRaw, url);

            // Act
            var ok = LogParser.TryParseLine(line, out LogData? entry);

            // Assert
            Assert.IsTrue(ok, "Expected TryParseLine to return true for a valid log line.");
            Assert.IsNotNull(entry);
            Assert.AreEqual("127.0.0.1", entry!.IPAddress);
            Assert.AreEqual(url, entry.Url);

            // ParseExact expects +02:00 (with colon) so build expected the same way ParseTimestamp does
            var expectedTimestamp = DateTime.ParseExact(
                "10/Jul/2018:22:21:28 +02:00",
                "dd/MMM/yyyy:HH:mm:ss zzz",
                CultureInfo.InvariantCulture);

            Assert.AreEqual(expectedTimestamp, entry.TimeStamp);
        }

        [TestMethod]
        public void TryParseLine_ValidLine_WithColonInOffset_ParsesTimestampCorrectly()
        {
            // Arrange
            var tsRaw = "10/Jul/2018:22:21:28 +02:00"; // offset already containing colon
            var url = "/path/resource";
            var line = MakeLogLine("10.0.0.5", tsRaw, url);

            // Act
            var ok = LogParser.TryParseLine(line, out LogData? entry);

            // Assert
            Assert.IsTrue(ok);
            Assert.IsNotNull(entry);
            Assert.AreEqual("10.0.0.5", entry!.IPAddress);
            Assert.AreEqual(url, entry.Url);

            var expected = DateTime.ParseExact(tsRaw, "dd/MMM/yyyy:HH:mm:ss zzz", CultureInfo.InvariantCulture);
            Assert.AreEqual(expected, entry.TimeStamp);
        }

        [TestMethod]
        public void TryParseLine_InvalidFormat_ReturnsFalse()
        {
            // Arrange - not matching the expected log format at all
            var line = "this is not an apache log line";

            // Act
            var ok = LogParser.TryParseLine(line, out LogData? entry);

            // Assert
            Assert.IsFalse(ok);
            Assert.IsNull(entry);
        }

        [TestMethod]
        public void TryParseLine_InvalidTimestamp_ReturnsFalse()
        {
            // Arrange - regex will match but timestamp is invalid (day 99)
            var tsRaw = "99/Jul/2018:22:21:28 +0200";
            var line = MakeLogLine("1.2.3.4", tsRaw, "/ok");

            // Act
            var ok = LogParser.TryParseLine(line, out LogData? entry);

            // Assert - ParseTimestamp should throw internally and TryParseLine should return false
            Assert.IsFalse(ok);
            Assert.IsNull(entry);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TryParseLine_NullInput_ThrowsArgumentNullException()
        {
            // Arrange & Act
            // Regex.Match(null) throws ArgumentNullException; TryParseLine doesn't guard against null, so expect the same.
            LogParser.TryParseLine(null!, out _);
        }
    }
}