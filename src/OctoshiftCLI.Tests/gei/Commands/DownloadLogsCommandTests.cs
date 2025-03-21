using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using OctoshiftCLI.GithubEnterpriseImporter;
using OctoshiftCLI.GithubEnterpriseImporter.Commands;
using Xunit;

namespace OctoshiftCLI.Tests.GithubEnterpriseImporter.Commands
{
    public class DownloadLogsCommandTests
    {
        private readonly DownloadLogsCommand _command;
        private readonly Mock<GithubApi> _mockGithubApi = TestHelpers.CreateMock<GithubApi>();
        private readonly Mock<ITargetGithubApiFactory> _targetGithubApiFactory = new Mock<ITargetGithubApiFactory>();
        private readonly Mock<HttpDownloadService> _mockHttpDownloadService = TestHelpers.CreateMock<HttpDownloadService>();
        private readonly Mock<OctoLogger> _mockLogger = TestHelpers.CreateMock<OctoLogger>();

        public DownloadLogsCommandTests()
        {
            _command = new DownloadLogsCommand(_mockLogger.Object, _targetGithubApiFactory.Object, _mockHttpDownloadService.Object, new RetryPolicy(_mockLogger.Object) { _retryOnResultInterval = 0 });
        }

        [Fact]
        public void Should_Have_Options()
        {
            Assert.NotNull(_command);
            Assert.Equal("download-logs", _command.Name);
            Assert.Equal(7, _command.Options.Count);

            TestHelpers.VerifyCommandOption(_command.Options, "github-target-org", true);
            TestHelpers.VerifyCommandOption(_command.Options, "target-repo", true);
            TestHelpers.VerifyCommandOption(_command.Options, "target-api-url", false);
            TestHelpers.VerifyCommandOption(_command.Options, "github-target-pat", false);
            TestHelpers.VerifyCommandOption(_command.Options, "migration-log-file", false);
            TestHelpers.VerifyCommandOption(_command.Options, "overwrite", false);
            TestHelpers.VerifyCommandOption(_command.Options, "verbose", false);
        }

        [Fact]
        public async Task Happy_Path()
        {
            // Arrange
            const string githubOrg = "FooOrg";
            const string repo = "foo-repo";
            const string logUrl = "some-url";
            const string defaultFileName = $"migration-log-{githubOrg}-{repo}.log";

            _mockGithubApi.Setup(m => m.GetMigrationLogUrl(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(logUrl);
            _targetGithubApiFactory.Setup(m => m.Create(null, null)).Returns(_mockGithubApi.Object);
            _mockHttpDownloadService.Setup(m => m.Download(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await _command.Invoke(githubOrg, repo);

            // Assert
            _mockHttpDownloadService.Verify(m => m.Download(logUrl, defaultFileName));
        }

        [Fact]
        public async Task Calls_GetMigrationLogUrl_With_Expected_Org_And_Repo()
        {
            // Arrange
            const string githubOrg = "FooOrg";
            const string repo = "foo-repo";
            const string logUrl = "some-url";

            _mockGithubApi.Setup(m => m.GetMigrationLogUrl(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(logUrl);
            _targetGithubApiFactory.Setup(m => m.Create(null, null)).Returns(_mockGithubApi.Object);
            _mockHttpDownloadService.Setup(m => m.Download(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await _command.Invoke(githubOrg, repo);

            // Assert
            _mockGithubApi.Verify(m => m.GetMigrationLogUrl(githubOrg, repo));
        }

        [Fact]
        public async Task Calls_ITargetGithubApiFactory_With_Expected_Target_Api_Url()
        {
            // Arrange
            const string githubOrg = "FooOrg";
            const string repo = "foo-repo";
            const string logUrl = "some-url";
            const string targetApiUrl = "api-url";

            _mockGithubApi.Setup(m => m.GetMigrationLogUrl(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(logUrl);

            _targetGithubApiFactory.Setup(m => m.Create(It.IsAny<string>(), null)).Returns(_mockGithubApi.Object);

            _mockHttpDownloadService.Setup(m => m.Download(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await _command.Invoke(githubOrg, repo, targetApiUrl);

            // Assert
            _targetGithubApiFactory.Verify(m => m.Create(targetApiUrl, null));
        }

        [Fact]
        public async Task Calls_ITargetGithubApiFactory_With_Expected_Target_GitHub_PAT()
        {
            // Arrange
            const string githubOrg = "FooOrg";
            const string repo = "foo-repo";
            const string logUrl = "some-url";
            const string githubTargetPat = "github-target-pat";

            _mockGithubApi.Setup(m => m.GetMigrationLogUrl(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(logUrl);
            _targetGithubApiFactory.Setup(m => m.Create(null, It.IsAny<string>())).Returns(_mockGithubApi.Object);
            _mockHttpDownloadService.Setup(m => m.Download(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await _command.Invoke(githubOrg, repo, null, githubTargetPat);

            // Assert
            _targetGithubApiFactory.Verify(m => m.Create(null, githubTargetPat));
        }

        [Fact]
        public async Task Calls_Download_With_Expected_Migration_Log_File()
        {
            // Arrange
            const string githubOrg = "FooOrg";
            const string repo = "foo-repo";
            const string logUrl = "some-url";
            const string migrationLogFile = "migration-log-file";

            _mockGithubApi.Setup(m => m.GetMigrationLogUrl(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(logUrl);
            _targetGithubApiFactory.Setup(m => m.Create(null, null)).Returns(_mockGithubApi.Object);
            _mockHttpDownloadService.Setup(m => m.Download(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await _command.Invoke(githubOrg, repo, null, null, migrationLogFile);

            // Assert
            _mockHttpDownloadService.Verify(m => m.Download(It.IsAny<string>(), migrationLogFile));
        }

        [Fact]
        public async Task Waits_For_Url_To_Populate()
        {
            // Arrange
            const string githubOrg = "FooOrg";
            const string repo = "foo-repo";
            const string logUrlEmpty = "";
            const string logUrlPopulated = "some-url";
            const string defaultFileName = $"migration-log-{githubOrg}-{repo}.log";

            _mockGithubApi.SetupSequence(m => m.GetMigrationLogUrl(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(logUrlEmpty)
                .ReturnsAsync(logUrlEmpty)
                .ReturnsAsync(logUrlEmpty)
                .ReturnsAsync(logUrlEmpty)
                .ReturnsAsync(logUrlEmpty)
                .ReturnsAsync(logUrlPopulated);

            _targetGithubApiFactory.Setup(m => m.Create(null, null)).Returns(_mockGithubApi.Object);
            _mockHttpDownloadService.Setup(m => m.Download(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await _command.Invoke(githubOrg, repo);

            // Assert
            _mockGithubApi.Verify(m => m.GetMigrationLogUrl(githubOrg, repo), Times.Exactly(6));
            _mockHttpDownloadService.Verify(m => m.Download(logUrlPopulated, defaultFileName));
        }

        [Fact]
        public async Task Calls_Download_When_File_Exists_And_Overwrite_Requested()
        {
            // Arrange
            const string githubOrg = "FooOrg";
            const string repo = "foo-repo";
            const string logUrl = "some-url";
            const bool overwrite = true;

            _mockGithubApi.Setup(m => m.GetMigrationLogUrl(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(logUrl);
            _targetGithubApiFactory.Setup(m => m.Create(null, null)).Returns(_mockGithubApi.Object);
            _mockHttpDownloadService.Setup(m => m.Download(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await _command.Invoke(githubOrg, repo, null, null, null, overwrite);

            // Assert
            _mockHttpDownloadService.Verify(m => m.Download(It.IsAny<string>(), It.IsAny<string>()));
        }

        [Fact]
        public async Task File_Already_Exists_No_Overwrite_Flag_Should_Throw_OctoshiftCliException()
        {
            // Arrange
            const string githubOrg = "FooOrg";
            const string repo = "foo-repo";

            // Act
            _command.FileExists = _ => true;

            // Assert
            await FluentActions
                .Invoking(async () => await _command.Invoke(githubOrg, repo))
                .Should().ThrowAsync<OctoshiftCliException>();
        }

        [Fact]
        public async Task Throw_OctoshiftCliException_When_No_Migration()
        {
            // Arrange
            const string githubOrg = "FooOrg";
            const string repo = "foo-repo";
            const string logUrl = null;

            _mockGithubApi.Setup(m => m.GetMigrationLogUrl(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(logUrl);

            _targetGithubApiFactory.Setup(m => m.Create(null, null)).Returns(_mockGithubApi.Object);

            // Assert
            await FluentActions
                .Invoking(async () => await _command.Invoke(githubOrg, repo))
                .Should().ThrowAsync<OctoshiftCliException>();
        }

        [Fact]
        public async Task Throws_OctoshiftCliException_When_Migration_Log_Url_Doesnt_Populate_After_6_Attempts()
        {
            // Arrange
            const string githubOrg = "FooOrg";
            const string repo = "foo-repo";
            const string logUrl = "";

            _mockGithubApi.Setup(m => m.GetMigrationLogUrl(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(logUrl);
            _targetGithubApiFactory.Setup(m => m.Create(null, null)).Returns(_mockGithubApi.Object);
            _mockHttpDownloadService.Setup(m => m.Download(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await FluentActions
                .Invoking(async () => await _command.Invoke(githubOrg, repo))
                .Should().ThrowAsync<OctoshiftCliException>();

            _mockGithubApi.Verify(m => m.GetMigrationLogUrl(githubOrg, repo), Times.Exactly(6));
            _mockHttpDownloadService.Verify(m => m.Download(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }
    }
}
