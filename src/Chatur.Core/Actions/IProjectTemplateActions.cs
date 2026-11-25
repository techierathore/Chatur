namespace Chatur.Core.Actions;

/// <summary>
/// New project, in every state (BRD-244, BRD-247, BRD-276, REQ-UI-112, REQ-FN-310): the project templates this
/// machine's <c>dotnet new</c> offers, the frameworks one can target, what Create will put on disk, and making it:
/// the project, in the solution the folder has or in a new one. Everything runs the machine's own
/// <c>dotnet</c>: in the solution's folder when one is open, else in the folder that is open, else in the
/// person's own folder.
/// </summary>
public interface IProjectTemplateActions
{
    /// <summary>
    /// The C# project templates this machine's <c>dotnet new list</c> shows. Read once per process; the
    /// first call takes a few seconds, later calls answer at once.
    /// </summary>
    /// <param name="aProjectId">The project open now, whose folder is where <c>dotnet</c> runs; <see langword="null"/> with nothing open.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <exception cref="InvalidOperationException"><c>dotnet</c> is not on this machine, or the list could not be read.</exception>
    Task<IReadOnlyList<ProjectTemplate>> TemplatesAsync(int? aProjectId, CancellationToken aCt = default);

    /// <summary>The frameworks one template can target, newest first, read from its own help. Cached per template.</summary>
    /// <param name="aProjectId">The project open now, or <see langword="null"/> with nothing open.</param>
    /// <param name="aShortName">The template's short name, e.g. <c>console</c>.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The choices; empty when the template has no framework choice.</returns>
    Task<TemplateFrameworks> FrameworksAsync(int? aProjectId, string aShortName, CancellationToken aCt = default);

    /// <summary>
    /// The extension a new solution file gets on this machine: <c>.slnx</c> from the .NET 10 SDK on, <c>.sln</c>
    /// before it. Read once per process from <c>dotnet --version</c>, so the dialog can show the solution file's
    /// full path before it exists.
    /// </summary>
    /// <param name="aProjectId">The project open now, whose folder is where <c>dotnet</c> runs; <see langword="null"/> with nothing open.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<string> SolutionExtensionAsync(int? aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// What Create would put on disk (BRD-247, BRD-276; owner's test of 23 Nov 2026), from the disk as it is now and
    /// without changing it. One rule for every starting point: <see cref="NewProjectRequest.Location"/> is the
    /// solution's folder. The solution file is in that folder itself and the project in one folder directly under it,
    /// never in a folder named after the solution. A solution already in the folder is the one the project is added
    /// to; a folder with projects and no solution gets a solution that takes those projects in.
    /// </summary>
    /// <param name="aRequest">The location and the names typed so far; the names may still be empty.</param>
    /// <param name="aSolutionExtension">The extension from <see cref="SolutionExtensionAsync"/>.</param>
    NewProjectPlan Plan(NewProjectRequest aRequest, string aSolutionExtension);

    /// <summary>
    /// Makes what <see cref="Plan"/> showed: the solution with <c>dotnet new sln</c> when the folder has none, the
    /// project with <c>dotnet new</c> in a folder named after it directly under the solution's folder, and
    /// <c>dotnet sln add</c> for it and for the projects the folder already held outside any solution. Then the
    /// solution is opened and its stored run targets forgotten, so Run has the new project at once (BRD-248). A
    /// folder project open on exactly that folder is taken off the list, since the solution now stands for it.
    /// Nothing but the solution file and the project folder is made.
    /// </summary>
    /// <param name="aRequest">What to make, and where.</param>
    /// <param name="aCt">A token that cancels the creation.</param>
    /// <returns>What was made, with the solution open.</returns>
    /// <exception cref="InvalidOperationException">A name or the location is not usable, or a command failed (the message carries what it said).</exception>
    Task<NewProjectResult> CreateAsync(NewProjectRequest aRequest, CancellationToken aCt = default);
}

/// <summary>A new project, and the solution it goes in (BRD-244, BRD-247, BRD-276).</summary>
/// <param name="Location">The solution's folder: the folder that is open, or the one the person picked. Empty with <paramref name="SolutionFile"/> means that solution's own folder.</param>
/// <param name="SolutionFile">The solution open on the Workbench, which the project is added to; <see langword="null"/> to use the solution in <paramref name="Location"/>, or make one there.</param>
/// <param name="SolutionName">The name for a new solution; not used when the folder already has one.</param>
/// <param name="ProjectName">The project's name; empty takes the new solution's name.</param>
/// <param name="ShortName">The template's short name, e.g. <c>console</c>.</param>
/// <param name="Framework">The framework, or <see langword="null"/> for the template's own.</param>
public sealed record NewProjectRequest(string Location, string? SolutionFile, string SolutionName, string ProjectName, string ShortName, string? Framework);

/// <summary>What Create would put on disk, shown in the dialog before anything is made.</summary>
/// <param name="Folder">The solution's folder; empty until a location is known.</param>
/// <param name="SolutionFile">The solution file's full path, existing or to be made; <see langword="null"/> until it can be named.</param>
/// <param name="SolutionExists">Whether <paramref name="SolutionFile"/> is already on disk, so the project is added to it.</param>
/// <param name="ProjectFolder">The project folder's full path; <see langword="null"/> until the project has a name.</param>
/// <param name="ProjectFile">The project file's full path; <see langword="null"/> until the project has a name.</param>
/// <param name="ProjectsToAdd">Project files already in the folder and in no solution there, relative to it, which the new solution takes in (BRD-276).</param>
/// <param name="Problem">Why Create cannot go ahead as things stand, in plain words; <see langword="null"/> when it can.</param>
public sealed record NewProjectPlan(string Folder, string? SolutionFile, bool SolutionExists, string? ProjectFolder, string? ProjectFile, IReadOnlyList<string> ProjectsToAdd, string? Problem);

/// <summary>What New project made.</summary>
/// <param name="ProjectId">The solution's own id in Chatur's project list.</param>
/// <param name="SolutionFile">The solution file's full path.</param>
/// <param name="ProjectFile">The new project file's full path.</param>
/// <param name="ProjectsAdded">The project files that were already in the folder and were added to the new solution, as full paths.</param>
/// <param name="OpenedNow">Whether the solution was opened by this call; <see langword="false"/> when it was the one already open.</param>
public sealed record NewProjectResult(int ProjectId, string SolutionFile, string ProjectFile, IReadOnlyList<string> ProjectsAdded, bool OpenedNow);

/// <summary>One project template from <c>dotnet new list</c>.</summary>
/// <param name="Name">The template's name as the table shows it (long names arrive cut with <c>...</c>).</param>
/// <param name="ShortName">The name <c>dotnet new</c> takes, e.g. <c>console</c>; the first when several are listed.</param>
/// <param name="Languages">The languages column, e.g. <c>[C#],F#,VB</c>.</param>
/// <param name="Tags">The tags column, e.g. <c>Common/Console</c>.</param>
public sealed record ProjectTemplate(string Name, string ShortName, string Languages, string Tags);

/// <summary>The frameworks a template can target.</summary>
/// <param name="Choices">The choices, newest first; empty when the template offers none.</param>
/// <param name="Default">The template's own default, or <see langword="null"/>.</param>
public sealed record TemplateFrameworks(IReadOnlyList<string> Choices, string? Default)
{
    /// <summary>The framework a new project starts with: <c>net10.0</c> when offered, else the newest choice; <see langword="null"/> when none is offered.</summary>
    public string? Preferred =>
        Choices.Count == 0 ? null : Choices.Contains("net10.0") ? "net10.0" : Choices[0];
}
