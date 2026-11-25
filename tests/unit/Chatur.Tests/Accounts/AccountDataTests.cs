using Chatur.Core;
using Chatur.Core.Accounts;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.Tests.Support;
using ChaturDb;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Chatur.Tests.Accounts;

/// <summary>
/// Each account has its own Chatur (BRD-210 to BRD-216; the owner's 17 Nov 2026 report, a new account
/// that opened another account's project, chat and project list). Every test composes Chatur.Core the
/// way the heads do — <c>AddChaturCore</c>, a real SQLite install file, a real migrator for each
/// account's file — and signs accounts in and out through <see cref="AppState"/>, exactly as
/// <c>AccountActions</c> does after App Manager accepts a sign-in.
/// </summary>
public sealed class AccountDataTests : IDisposable
{
    private const string FirstAccount = "1001";
    private const string SecondAccount = "1002";

    private readonly string objRoot = Path.Combine(Chatur.Tests.Support.TestScratch.Root, $"chatur-accounts-{Guid.NewGuid():N}");

    /// <summary>Creates the test's own folder for the install's files and the projects on disk.</summary>
    public AccountDataTests()
    {
        Directory.CreateDirectory(objRoot);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(objRoot, true);
        }
        catch (IOException)
        {
            // SQLite may still hold a pooled handle for a moment; the temp folder is left for the OS.
        }
    }

    /// <summary>
    /// When a first account has named a folder, opened a project from it and left it selected, and a
    /// second account then signs in on the same install, then the second account's Start has no folder,
    /// no project and no selected project — the empty start — while the first account still finds its
    /// own project selected when it signs in again (REQ-UI-091, REQ-UI-008, REQ-FN-006).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-091 REQ-UI-008 REQ-FN-006 a second account on the install starts empty and the first keeps its own")]
    public async Task SecondAccountStartsEmpty()
    {
        using var vInstall = new Install(objRoot);
        var vFolder = ProjectsOnDisk("named");

        vInstall.SignIn(FirstAccount);
        var vProjects = vInstall.Get<IProjectActions>();
        await vProjects.AddFolderAsync(vFolder);
        var vFound = await vProjects.ScanAsync();
        var vChaturCheck = Assert.Single(vFound, p => p.Name == "ChaturCheck");
        await vProjects.SelectProjectAsync(vChaturCheck.ProjectId);

        vInstall.SignIn(SecondAccount);
        Assert.Null(vInstall.AppState.SelectedProject);
        Assert.Empty(await vProjects.ListFoldersAsync());
        Assert.Empty(await vProjects.ScanAsync());
        Assert.Null(await vProjects.SelectedProjectAsync());

        vInstall.SignIn(FirstAccount);
        Assert.Equal("ChaturCheck", (await vProjects.SelectedProjectAsync())?.Name);
        Assert.Equal(2, (await vProjects.ScanAsync()).Count);
    }

    /// <summary>
    /// When a first account has chatted in a folder ("What commands do you have ?"), and a second account
    /// opens the very same folder, then the second account is offered no "Continuing after" line, lists
    /// none of the first account's chats, and its conversation starts empty (REQ-FN-302, REQ-FN-008).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-302 REQ-FN-008 a chat started by one account is never continued by another")]
    public async Task ChatIsNeverContinuedByAnotherAccount()
    {
        using var vInstall = new Install(objRoot);
        var vShared = Directory.CreateDirectory(Path.Combine(objRoot, "SharedFolder")).FullName;

        vInstall.SignIn(FirstAccount);
        var vFirstProject = await vInstall.Get<IProjectOpenActions>().OpenFolderAsync(vShared);
        var vAgents = vInstall.Get<IAgentActions>();
        var vFirstChat = await vAgents.EnsureActiveSessionAsync(vFirstProject.ProjectId);
        RecordUserMessage(vInstall, vFirstChat.SessionId, "What commands do you have ?");
        Assert.StartsWith("Continuing after", (await vAgents.SuggestAgentAsync(vFirstProject.ProjectId)).Why);

        vInstall.SignIn(SecondAccount);
        var vSecondProject = await vInstall.Get<IProjectOpenActions>().OpenFolderAsync(vShared);
        var vSuggestion = await vAgents.SuggestAgentAsync(vSecondProject.ProjectId);
        Assert.DoesNotContain("Continuing after", vSuggestion.Why);
        Assert.Empty(await vAgents.SessionsAsync(vSecondProject.ProjectId));

        var vSecondChat = await vAgents.EnsureActiveSessionAsync(vSecondProject.ProjectId);
        var vDetail = await vAgents.ContinueSessionAsync(vSecondChat.SessionId);
        Assert.DoesNotContain(vDetail.Events, e => e.Payload.Contains("What commands do you have", StringComparison.Ordinal));
        Assert.Single(await vAgents.SessionsAsync(vSecondProject.ProjectId));
    }

    /// <summary>
    /// When a first account connects a provider with its key, and a second account opens Settings ▸
    /// Providers, then the second account's list and models are empty, adding the same provider by name
    /// works, and each key is filed under a name that carries its own account, so neither overwrites or
    /// reads the other's (REQ-UI-092, REQ-FN-017).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-092 REQ-FN-017 one account's provider key is never offered to another")]
    public async Task ProviderKeyIsNotOfferedToAnotherAccount()
    {
        using var vInstall = new Install(objRoot);

        vInstall.SignIn(FirstAccount);
        var vProviders = vInstall.Get<IProviderActions>();
        var vFirst = await vProviders.AddWithKeyAsync("Anthropic", "Anthropic", string.Empty, "first-account-key");
        Assert.NotEmpty(await vProviders.ListModelsAsync());

        vInstall.SignIn(SecondAccount);
        Assert.Empty(await vProviders.ListAsync());
        Assert.Empty(await vProviders.ListModelsAsync());
        var vSecond = await vProviders.AddWithKeyAsync("Anthropic", "Anthropic", string.Empty, "second-account-key");

        var vFirstName = AccountSecretNames.ForAccount(FirstAccount, $"chatur.provider.{vFirst.ProviderId}");
        var vSecondName = AccountSecretNames.ForAccount(SecondAccount, $"chatur.provider.{vSecond.ProviderId}");
        Assert.NotEqual(vFirstName, vSecondName);
        Assert.Equal("first-account-key", await vInstall.Secrets.ReadAsync(vFirstName));
        Assert.Equal("second-account-key", await vInstall.Secrets.ReadAsync(vSecondName));
        Assert.False(vInstall.Secrets.Contains($"chatur.provider.{vSecond.ProviderId}"));

        vInstall.SignIn(FirstAccount);
        var vListed = Assert.Single(await vProviders.ListAsync());
        Assert.Equal(vFirst.ProviderId, vListed.ProviderId);
    }

    /// <summary>
    /// When a first account has queued a run, and a second account opens Run queue, then the second
    /// account's queue is empty while the first account's still holds its run (REQ-FN-303).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-303 run history belongs to the account that ran it")]
    public async Task RunHistoryBelongsToTheAccount()
    {
        using var vInstall = new Install(objRoot);
        var vFolder = Directory.CreateDirectory(Path.Combine(objRoot, "Queued")).FullName;

        vInstall.SignIn(FirstAccount);
        var vProject = await vInstall.Get<IProjectOpenActions>().OpenFolderAsync(vFolder);
        using (var vConnection = vInstall.Get<IDbConnectionFactory>().OpenConnection())
        {
            vConnection.Execute(
                "INSERT INTO RunQueueItem (ProjectId, Brief, Status, QueuedUtc) VALUES (@ProjectId, 'Add a greeting', 'Queued', @Now);",
                new { vProject.ProjectId, Now = DateTime.UtcNow.ToString("O") });
        }

        var vQueue = vInstall.Get<IRunQueueActions>();
        Assert.Single((await vQueue.ListAsync()).Items);

        vInstall.SignIn(SecondAccount);
        Assert.Empty((await vQueue.ListAsync()).Items);
    }

    /// <summary>
    /// When the install holds a named folder, a selected project and a chat kept before accounts had
    /// files of their own (written with nobody signed in, as the old build wrote everything), and an
    /// account signs in, then the account sees none of it, and none of it is deleted from the install's
    /// file (REQ-UI-093).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-093 data kept before accounts belongs to nobody and is not deleted")]
    public async Task DataKeptBeforeBelongsToNobody()
    {
        using var vInstall = new Install(objRoot);
        var vFolder = ProjectsOnDisk("legacy");

        var vProjects = vInstall.Get<IProjectActions>();
        await vProjects.AddFolderAsync(vFolder);
        var vOld = (await vProjects.ScanAsync()).First();
        await vProjects.SelectProjectAsync(vOld.ProjectId);
        await vInstall.Get<IAgentActions>().EnsureActiveSessionAsync(vOld.ProjectId);

        vInstall.SignIn(FirstAccount);
        Assert.Empty(await vProjects.ListFoldersAsync());
        Assert.Empty(await vProjects.ScanAsync());
        Assert.Null(await vProjects.SelectedProjectAsync());

        using var vInstallFile = vInstall.Get<IDbConnectionFactory>().OpenInstallConnection();
        Assert.Equal(1, vInstallFile.ExecuteScalar<int>("SELECT COUNT(1) FROM ProjectFolder;"));
        Assert.Equal(2, vInstallFile.ExecuteScalar<int>("SELECT COUNT(1) FROM Project;"));
        Assert.Equal(1, vInstallFile.ExecuteScalar<int>("SELECT COUNT(1) FROM Session;"));
        Assert.Equal(1, vInstallFile.ExecuteScalar<int>("SELECT COUNT(1) FROM Setting WHERE Key = 'SelectedProjectId';"));
    }

    /// <summary>
    /// When the person chooses Open a file and gives a file deep inside a solution's folder, inside a
    /// repository with no solution, or in a plain folder, then the project opened is the solution, the
    /// repository's top folder, or the file's own folder, it is selected, and the file's path is given
    /// relative to it for the Workbench to open (REQ-UI-094).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-094 Open a file opens the folder that holds it, with the file to show")]
    public async Task OpenFileOpensTheFolderThatHoldsIt()
    {
        using var vInstall = new Install(objRoot);
        vInstall.SignIn(FirstAccount);
        var vOpen = vInstall.Get<IProjectOpenActions>();

        var vSolutionRoot = Directory.CreateDirectory(Path.Combine(objRoot, "PracticeSln")).FullName;
        File.WriteAllText(Path.Combine(vSolutionRoot, "Practice.sln"), string.Empty);
        var vDeep = Path.Combine(vSolutionRoot, "src", "App", "Program.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(vDeep)!);
        File.WriteAllText(vDeep, "// hello");

        var vInSolution = await vOpen.OpenFileAsync(vDeep);
        Assert.Equal("Practice", vInSolution.Project.Name);
        Assert.Equal("Solution", vInSolution.Project.Kind);
        Assert.Equal("src/App/Program.cs", vInSolution.RelativePath);
        Assert.Equal(vInSolution.Project.ProjectId, vInstall.AppState.SelectedProject?.ProjectId);

        var vRepository = Directory.CreateDirectory(Path.Combine(objRoot, "Repo")).FullName;
        Directory.CreateDirectory(Path.Combine(vRepository, ".git"));
        var vNotes = Path.Combine(vRepository, "docs", "notes.md");
        Directory.CreateDirectory(Path.GetDirectoryName(vNotes)!);
        File.WriteAllText(vNotes, "# notes");

        var vInRepository = await vOpen.OpenFileAsync(vNotes);
        Assert.Equal("Repo", vInRepository.Project.Name);
        Assert.Equal("Folder", vInRepository.Project.Kind);
        Assert.Equal("docs/notes.md", vInRepository.RelativePath);

        // A file with neither a solution nor a repository above it opens its own folder. Since 21 Nov 2026 a unit
        // test's scratch space is under this test project's bin folder, inside Chatur's own repository, so every
        // folder there has Chatur.sln above it: a ".git" marker stops the walk at the loose file's own folder.
        var vPlain = Directory.CreateDirectory(Path.Combine(objRoot, "Loose")).FullName;
        Directory.CreateDirectory(Path.Combine(vPlain, ".git"));
        var vLoose = Path.Combine(vPlain, "todo.txt");
        File.WriteAllText(vLoose, "todo");
        var vAlone = await vOpen.OpenFileAsync(vLoose);
        Assert.Equal(vPlain, vAlone.Project.Path);
        Assert.Equal("todo.txt", vAlone.RelativePath);

        await Assert.ThrowsAsync<InvalidOperationException>(() => vOpen.OpenFileAsync(Path.Combine(vPlain, "missing.txt")));
    }

    /// <summary>
    /// When one account signs out and another signs in, then the selected project the first account had
    /// is no longer the app's; the same account signing in again keeps it (REQ-UI-095).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-095 another account signing in drops the open project")]
    public void AnotherAccountDropsTheOpenProject()
    {
        var vState = new AppState();
        vState.SetCurrentUser(new CurrentUser(FirstAccount, "first@example.test", "First"));
        vState.SetSelectedProject(new ProjectSummary(1, "ChaturCheck", "C:\\ChaturCheck", "Folder", null));

        vState.SetCurrentUser(new CurrentUser(FirstAccount, "first@example.test", "First"));
        Assert.NotNull(vState.SelectedProject);

        vState.SetCurrentUser(null);
        Assert.Null(vState.SelectedProject);

        vState.SetSelectedProject(new ProjectSummary(1, "ChaturCheck", "C:\\ChaturCheck", "Folder", null));
        vState.SetCurrentUser(new CurrentUser(SecondAccount, "second@example.test", "Second"));
        Assert.Null(vState.SelectedProject);
    }

    /// <summary>
    /// When two accounts sign in on one install, then App Manager is sent the same device identifier for
    /// both, kept in the install's own file (REQ-FN-002 stays true with a file per account).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-002 the device id is the install's whoever is signed in")]
    public async Task DeviceIdIsTheInstalls()
    {
        using var vInstall = new Install(objRoot);
        var vDb = vInstall.Get<IDbConnectionFactory>();

        vInstall.SignIn(FirstAccount);
        var vFirst = await InstallationId.GetOrCreateAsync(vDb, new SystemClock());
        vInstall.SignIn(SecondAccount);
        var vSecond = await InstallationId.GetOrCreateAsync(vDb, new SystemClock());

        Assert.Equal(vFirst, vSecond);
        using var vInstallFile = vDb.OpenInstallConnection();
        Assert.Equal(vFirst, vInstallFile.ExecuteScalar<string>("SELECT DeviceId FROM Installation;"));
    }

    /// <summary>A named folder holding two projects: <c>ChaturCheck</c>, a solution, and <c>SmokeProj</c>, a bare folder.</summary>
    private string ProjectsOnDisk(string aName)
    {
        var vFolder = Directory.CreateDirectory(Path.Combine(objRoot, aName)).FullName;
        var vCheck = Directory.CreateDirectory(Path.Combine(vFolder, "ChaturCheck")).FullName;
        File.WriteAllText(Path.Combine(vCheck, "ChaturCheck.sln"), string.Empty);
        Directory.CreateDirectory(Path.Combine(vFolder, "SmokeProj"));
        return vFolder;
    }

    /// <summary>Writes one message of the owner's into a chat's record, as the conversation does when it is sent.</summary>
    private static void RecordUserMessage(Install aInstall, int aSessionId, string aText)
    {
        using var vConnection = aInstall.Get<IDbConnectionFactory>().OpenConnection();
        vConnection.Execute(
            "INSERT INTO SessionEvent (SessionId, Seq, Kind, Payload, CreatedUtc) VALUES (@aSessionId, 1, 'user', @aText, @Now);",
            new { aSessionId, aText, Now = DateTime.UtcNow.ToString("O") });
    }

    /// <summary>
    /// One install of Chatur, composed as the heads compose it: <c>AddChaturCore</c>, the install's own
    /// migrated file, a DbUp migrator for each account's file, and an in-memory secret store standing in
    /// for the operating system's.
    /// </summary>
    private sealed class Install : IDisposable
    {
        private readonly ServiceProvider objProvider;
        private readonly IServiceScope objScope;

        public Install(string aRoot)
        {
            var vPaths = new InstallPaths(Path.Combine(aRoot, "app-data"));
            Assert.True(ChaturDbMigrator.Migrate($"Data Source={vPaths.DatabasePath}").Successful);

            var vServices = new ServiceCollection();
            vServices.AddLogging();
            vServices.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            vServices.AddSingleton<IClock, SystemClock>();
            vServices.AddSingleton<IAppPaths>(vPaths);
            vServices.AddSingleton<ISecretStore>(Secrets);
            vServices.AddSingleton<IProcessLauncher, NoLauncher>();
            vServices.AddSingleton<IDatabaseMigrator>(new DelegateDatabaseMigrator(aPath => ChaturDbMigrator.Migrate($"Data Source={aPath}").Successful));
            vServices.AddChaturCore();
            objProvider = vServices.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });
            objScope = objProvider.CreateScope();
            AppState = objProvider.GetRequiredService<AppState>();
        }

        public InMemorySecretStore Secrets { get; } = new();

        public AppState AppState { get; }

        public T Get<T>()
            where T : notnull => objScope.ServiceProvider.GetRequiredService<T>();

        /// <summary>Signs an account in, as <c>AccountActions</c> records it once App Manager has said yes.</summary>
        public void SignIn(string aUserId) =>
            AppState.SetCurrentUser(new CurrentUser(aUserId, aUserId == FirstAccount ? "first@example.test" : "second@example.test", $"User {aUserId}"));

        public void Dispose()
        {
            objScope.Dispose();
            objProvider.Dispose();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        }
    }

    private sealed class InstallPaths : IAppPaths
    {
        public InstallPaths(string aFolder)
        {
            Directory.CreateDirectory(aFolder);
            AppDataFolder = aFolder;
        }

        public string AppDataFolder { get; }

        public string DatabasePath => Path.Combine(AppDataFolder, "chatur.db");

        public string LogFolder => Path.Combine(AppDataFolder, "logs");
    }

    private sealed class NoLauncher : IProcessLauncher
    {
        public Task OpenInEditorAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task<int> RunAsync(string aCommand, string aArguments, string aWorkingDirectory, Action<string> aOnOutputLine, CancellationToken aCt = default) => Task.FromResult(0);
    }
}
