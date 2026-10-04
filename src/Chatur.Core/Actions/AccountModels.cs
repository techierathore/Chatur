namespace Chatur.Core.Actions;

/// <summary>The owner signed in on this machine, as every screen needs to show who they are.</summary>
/// <param name="UserId">App Manager's identifier for the account.</param>
/// <param name="Email">The account's email address.</param>
/// <param name="DisplayName">The name shown beside the avatar.</param>
public sealed record CurrentUser(string UserId, string Email, string DisplayName);

/// <summary>What Register (BRD-6) sends to create an account.</summary>
/// <param name="FirstName">1 to 60 characters.</param>
/// <param name="LastName">1 to 60 characters.</param>
/// <param name="Email">Must parse as an address; App Manager refuses one already in use.</param>
/// <param name="Password">At least 8 characters with a capital, a number and a special character.</param>
public sealed record RegisterRequest(string FirstName, string LastName, string Email, string Password);

/// <summary>The result of a sign-in or a registration attempt (REQ-UI-002, REQ-UI-004).</summary>
/// <param name="Succeeded">Whether the account is now signed in.</param>
/// <param name="ErrorMessage">App Manager's own words when it refused, verbatim; <see langword="null"/> on success.</param>
/// <param name="User">The signed-in owner; <see langword="null"/> when <paramref name="Succeeded"/> is <see langword="false"/>.</param>
public sealed record SignInResult(bool Succeeded, string? ErrorMessage, CurrentUser? User);
