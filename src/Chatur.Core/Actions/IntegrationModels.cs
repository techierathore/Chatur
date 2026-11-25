namespace Chatur.Core.Actions;

/// <summary>One action Chatur offers to outside tools (REQ-FN-081).</summary>
/// <param name="Code">The action's short code, e.g. <c>"start"</c>.</param>
/// <param name="Name">Its name on the screen.</param>
/// <param name="Does">What it does, in a line.</param>
/// <param name="StartedBy">What starts it: another tool, a schedule, a process.</param>
/// <param name="Method">The HTTP method of its address.</param>
/// <param name="Path">The path of its address.</param>
/// <param name="Built"><see langword="false"/> while Chatur does not yet answer this address; such an action is never on.</param>
/// <param name="Enabled">Whether it is on. An action that is off answers 404, as though it were never there.</param>
public sealed record PublishedAction(string Code, string Name, string Does, string StartedBy, string Method, string Path, bool Built, bool Enabled)
{
    /// <summary>The address as one line: the method, a space and the path.</summary>
    public string Address => $"{Method} {Path}";
}

/// <summary>A token made for another tool. <see cref="Value"/> is shown once and never kept.</summary>
/// <param name="TokenId">The token's row.</param>
/// <param name="Value">The whole token, as the other tool sends it in <c>Authorization: Bearer</c>.</param>
public sealed record NewIntegrationToken(int TokenId, string Value);

/// <summary>A process an outside tool started (REQ-FN-082).</summary>
/// <param name="RunId">The run number given back to the tool.</param>
/// <param name="ProcessCode">The process started.</param>
/// <param name="ProjectName">The project it runs on.</param>
/// <param name="CalledUtc">When the call came in.</param>
public sealed record OutsideRun(int RunId, string ProcessCode, string ProjectName, DateTime CalledUtc);

/// <summary>How an outside call ended.</summary>
public enum OutsideCallOutcome
{
    /// <summary>The process started, or was already running, and a run number is given back.</summary>
    Started,

    /// <summary>The token is missing, unknown or revoked.</summary>
    Unauthorized,

    /// <summary>The action is off or not built: answered as though it were never there.</summary>
    NotThere,

    /// <summary>The call is not understood: no such project or process.</summary>
    Refused,
}

/// <summary>The answer to an outside call.</summary>
/// <param name="Outcome">How it ended.</param>
/// <param name="RunId">The run number; 0 unless <see cref="Outcome"/> is <see cref="OutsideCallOutcome.Started"/>.</param>
/// <param name="State">The run's state, e.g. <c>"Running"</c>; empty unless started.</param>
/// <param name="Project">The project the process runs on; empty unless started.</param>
/// <param name="Process">The process started; empty unless started.</param>
/// <param name="Message">A plain line saying why a call was refused; empty when started.</param>
public sealed record OutsideCallResult(OutsideCallOutcome Outcome, int RunId, string State, string Project, string Process, string Message);

/// <summary>One check of a built application, driven in its own window by the verifier (REQ-FN-083).</summary>
/// <param name="Application">The application and platform, e.g. <c>"TfLens · Mac Catalyst"</c>.</param>
/// <param name="Platform">Either <c>"Mac Catalyst"</c> or <c>"Windows"</c>.</param>
/// <param name="Machine">The machine it was driven on.</param>
/// <param name="ScreenshotsKept">How many screenshots were kept.</param>
/// <param name="CheckedUtc">When.</param>
/// <param name="Result">The result as shown, e.g. <c>"passed"</c> or <c>"2 to look at"</c>.</param>
public sealed record WindowCheck(string Application, string Platform, string Machine, int ScreenshotsKept, DateTime CheckedUtc, string Result)
{
    /// <summary>Whether the result is a pass.</summary>
    public bool Passed => string.Equals(Result, "passed", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Whether a download carries Apple's notarisation.</summary>
public enum NotarisationState
{
    /// <summary>Signed and notarised by Apple.</summary>
    Notarised,

    /// <summary>Signed, but Apple has not notarised it.</summary>
    NotNotarised,

    /// <summary>Notarising is an Apple step and does not apply to this platform.</summary>
    NotApplicable,
}

/// <summary>One signed download of a published release (REQ-UI-069).</summary>
/// <param name="Version">The release's version.</param>
/// <param name="Platform">Either <c>"Mac"</c> or <c>"Windows"</c>.</param>
/// <param name="FileName">The file, e.g. <c>Chatur-0.1.0.dmg</c>.</param>
/// <param name="SignedBy">Who signed it, in plain words.</param>
/// <param name="Notarisation">The notarisation state.</param>
/// <param name="PublishedUtc">When it was published.</param>
public sealed record SignedDownload(string Version, string Platform, string FileName, string SignedBy, NotarisationState Notarisation, DateTime PublishedUtc);
