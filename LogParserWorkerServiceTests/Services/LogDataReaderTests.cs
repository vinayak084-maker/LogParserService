using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

using LogParserWorkerService.Services.Services;

namespace LogParserWorkerService.Tests
{
    [TestClass]
    public class LogDataReaderTests
    {
        [TestMethod]
        public void Constructor_NullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            IConfiguration? configuration = null;
            var envMock = new Mock<IHostEnvironment>();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new LogDataReader(configuration!, envMock.Object));
        }

        [TestMethod]
        public void Constructor_NullEnvironment_ThrowsArgumentNullException()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LoggingFile:Path"] = "file.log"
            }).Build();
            IHostEnvironment? env = null;

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new LogDataReader(configuration, env!));
        }

        [TestMethod]
        public void Constructor_MissingPathConfig_ThrowsArgumentException()
        {
            // Arrange - configuration does NOT have the LoggingFile:Path key
            var configuration = new ConfigurationBuilder().Build();
            var envMock = new Mock<IHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath());

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => new LogDataReader(configuration, envMock.Object));
        }

        [TestMethod]
        public async Task ReadLineByLineAsync_FileNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                // point to a file that does not exist
                ["LoggingFile:Path"] = "nonexistent.log"
            }).Build();

            var envMock = new Mock<IHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(tempDir);

            var reader = new LogDataReader(configuration, envMock.Object);

            // Act & Assert
            var ex = await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
            {
                // Enumerate the async enumerable to trigger the file existence check
                await foreach (var _ in reader.ReadLineByLineAsync())
                {
                    // won't run
                }
            });

            // cleanup
            Directory.Delete(tempDir, true);
        }

        [TestMethod]
        public async Task ReadLineByLineAsync_ReturnsAllLines()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var fileName = "testlog.log";
            var filePath = Path.Combine(tempDir, fileName);
            var lines = new[] { "first line", "second line", "third line" };
            await File.WriteAllLinesAsync(filePath, lines);

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LoggingFile:Path"] = fileName
            }).Build();

            var envMock = new Mock<IHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(tempDir);

            var reader = new LogDataReader(configuration, envMock.Object);

            // Act
            var readLines = new List<string>();
            await foreach (var line in reader.ReadLineByLineAsync())
            {
                readLines.Add(line);
            }

            // Assert
            CollectionAssert.AreEqual(lines.ToList(), readLines);

            // cleanup
            File.Delete(filePath);
            Directory.Delete(tempDir, true);
        }

        [TestMethod]
        public async Task ReadLineByLineAsync_CancellationDuringEnumeration_ThrowsOperationCanceledException()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var fileName = "canceltest.log";
            var filePath = Path.Combine(tempDir, fileName);
            // ensure at least two lines so we can cancel after the first
            var lines = new[] { "line1", "line2", "line3" };
            await File.WriteAllLinesAsync(filePath, lines);

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LoggingFile:Path"] = fileName
            }).Build();

            var envMock = new Mock<IHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(tempDir);

            var reader = new LogDataReader(configuration, envMock.Object);

            var cts = new CancellationTokenSource();

            // Act & Assert
            var enumerator = reader.ReadLineByLineAsync(cts.Token).GetAsyncEnumerator();

            try
            {
                // read first line successfully
                var hasFirst = await enumerator.MoveNextAsync();
                Assert.IsTrue(hasFirst);
                Assert.AreEqual("line1", enumerator.Current);

                // cancel before trying to read the next line
                cts.Cancel();

                // Attempting to move to the next line should observe the cancellation
                await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
                {
                    await enumerator.MoveNextAsync();
                });
            }
            finally
            {
                // Dispose enumerator and cleanup
                await enumerator.DisposeAsync();
                File.Delete(filePath);
                Directory.Delete(tempDir, true);
            }
        }
    }
}