namespace Chatur.Core.Actions;

/// <summary>What the Licence screen shows (BRD-141 to BRD-144): the licence in force, the machines on it, the warning and the features.</summary>
/// <param name="Available">Whether the licence could be read; <see langword="false"/> carries <paramref name="Message"/>.</param>
/// <param name="Message">Why the licence could not be read, or <see langword="null"/>.</param>
/// <param name="AccountName">The signed-in owner's name.</param>
/// <param name="AccountEmail">The signed-in owner's email address.</param>
/// <param name="CheckedAtUtc">When the licence source was last asked.</param>
/// <param name="Licence">The licence in force, or <see langword="null"/> when the account holds none.</param>
/// <param name="Warning">The warning the server sent for being over the device cap, or <see langword="null"/>.</param>
/// <param name="Devices">Every machine the licence source knows.</param>
/// <param name="Features">Every Chatur feature with whether this licence has it on.</param>
/// <param name="CanChangeFeatures">Whether the licence source lets the owner switch a feature.</param>
public sealed record LicenceView(
    bool Available,
    string? Message,
    string AccountName,
    string AccountEmail,
    DateTime CheckedAtUtc,
    LicenceInfo? Licence,
    LicenceWarning? Warning,
    IReadOnlyList<LicenceDevice> Devices,
    IReadOnlyList<LicenceFeature> Features,
    bool CanChangeFeatures);

/// <summary>The licence in force (REQ-UI-064).</summary>
/// <param name="Name">The licence's name, e.g. Professional.</param>
/// <param name="State">The licence's state, e.g. Active.</param>
/// <param name="Key">The licence key, or <see langword="null"/>.</param>
/// <param name="ExpiresOn">When it runs out, or <see langword="null"/> when it never does.</param>
/// <param name="DaysRemaining">Days left, or <see langword="null"/> with no expiry.</param>
/// <param name="StartedOn">When it started, or <see langword="null"/>.</param>
/// <param name="MaxDevices">The device cap, or <see langword="null"/> when none is known.</param>
public sealed record LicenceInfo(string Name, string State, string? Key, DateTime? ExpiresOn, int? DaysRemaining, DateTime? StartedOn, int? MaxDevices);

/// <summary>The warning for being over the device cap (REQ-UI-065).</summary>
/// <param name="Title">One line saying how far over.</param>
/// <param name="Message">The server's own words, verbatim.</param>
public sealed record LicenceWarning(string Title, string Message);

/// <summary>One machine on the licence (REQ-UI-066).</summary>
/// <param name="DeviceId">The identifier the licence source files the machine under; also names its test ids.</param>
/// <param name="Name">The machine's name.</param>
/// <param name="Platform">The platform, or <see langword="null"/>.</param>
/// <param name="AppVersion">The Chatur version last seen on it, or <see langword="null"/>.</param>
/// <param name="LastSeenUtc">When it was last seen, or <see langword="null"/>.</param>
/// <param name="SignInCount">How many times it signed in.</param>
/// <param name="IsBlocked">Whether it is blocked.</param>
/// <param name="IsThisDevice">Whether it is the machine the owner is on.</param>
public sealed record LicenceDevice(string DeviceId, string Name, string? Platform, string? AppVersion, DateTime? LastSeenUtc, int SignInCount, bool IsBlocked, bool IsThisDevice);

/// <summary>One Chatur feature and whether the licence has it on (REQ-FN-075).</summary>
/// <param name="Code">The feature's code, matching <c>Chatur.Core.Licences.ChaturFeatures</c>.</param>
/// <param name="Name">The name shown.</param>
/// <param name="Description">One line about it.</param>
/// <param name="IsOn">Whether it is on for this licence.</param>
public sealed record LicenceFeature(string Code, string Name, string Description, bool IsOn);

/// <summary>The answer to forgetting, blocking or switching.</summary>
/// <param name="Succeeded">Whether it was done.</param>
/// <param name="Message">The reason when it was not, or <see langword="null"/>.</param>
public sealed record LicenceActionResult(bool Succeeded, string? Message);

/// <summary>Where the account stands against its device cap, as the sign-in reported it (App Manager <c>deviceCap</c>).</summary>
/// <param name="DeviceCount">Devices counted.</param>
/// <param name="MaxDevices">The cap.</param>
/// <param name="IsOverCap">Whether the count is over the cap.</param>
/// <param name="Warning">The server's warning, or <see langword="null"/>.</param>
/// <param name="LicenceKey">The licence key, or <see langword="null"/>.</param>
public sealed record DeviceCapReport(int DeviceCount, int MaxDevices, bool IsOverCap, string? Warning, string? LicenceKey);
