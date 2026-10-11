namespace Chatur.Core.Actions;

/// <summary>
/// The twelve TechieFlow commands, starting one on the selected project and the documents a process
/// may write (page Processes; Architecture §7 "Roles and rules"). The steps and checks are code in
/// <c>Chatur.Core.Processes.ProcessCatalogue</c>; only their wording is a database row.
/// </summary>
public interface IProcessActions
{
    /// <summary>
    /// Every command with what it does, its shape and when it was last started on the project
    /// (REQ-FN-049). Always twelve rows, in the order TechieFlow runs them.
    /// </summary>
    /// <param name="aProjectId">The project whose last runs to show, or <see langword="null"/> for dashes.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<ProcessSummary>> ListAsync(int? aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// A process's rows in order, with the wording the database holds now (REQ-UI-045, REQ-UI-046).
    /// </summary>
    /// <param name="aProcessCode">The command, e.g. <c>"verify-phase"</c>.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The rows, or an empty list when the command is not known.</returns>
    Task<IReadOnlyList<ProcessStepInfo>> StepsAsync(string aProcessCode, CancellationToken aCt = default);

    /// <summary>
    /// Starts a process on a project: records a running run on its first step, then the page opens
    /// <c>/processes/{RunId}/run</c> (REQ-FN-050). A process already running on the project is
    /// returned, not started twice.
    /// </summary>
    /// <param name="aProjectId">The project to run on, normally the selected one.</param>
    /// <param name="aProcessCode">The command to start.</param>
    /// <param name="aCt">A token that cancels the start.</param>
    /// <exception cref="ArgumentException">The command or the project is not known.</exception>
    Task<ProcessRunStarted> StartAsync(int aProjectId, string aProcessCode, CancellationToken aCt = default);

    /// <summary>
    /// The project's own BRD, checklist and status documents, for the "What a process touches" panel
    /// (REQ-FN-051).
    /// </summary>
    /// <param name="aProjectId">The project to look in.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The documents, or <see langword="null"/> when the project is not known.</returns>
    Task<ProcessDocuments?> DocumentsAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Writes one of the project's own BRD, checklist or status documents. Every other path — another
    /// file, another folder, a path that leaves the project — is refused and nothing is written
    /// (REQ-FN-051). This is the only way a process writes into a project's folder.
    /// </summary>
    /// <param name="aProjectId">The project whose document is written.</param>
    /// <param name="aRelativePath">The project-relative path, e.g. <c>"docs/App-P2-Checklist.md"</c>.</param>
    /// <param name="aContent">The whole new content of the document.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <exception cref="InvalidOperationException">The path is not one of the project's own documents.</exception>
    Task WriteDocumentAsync(int aProjectId, string aRelativePath, string aContent, CancellationToken aCt = default);
}
