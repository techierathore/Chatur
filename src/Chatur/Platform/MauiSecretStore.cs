using Chatur.Core.Platform;
#if WINDOWS
using System.Runtime.InteropServices;
using System.Text;
#endif

namespace Chatur.Platform;

/// <summary>
/// The operating system's own secret store: Windows Credential Manager via P/Invoke on Windows, the
/// Keychain (through MAUI's own <see cref="SecureStorage"/>, which is Keychain-backed there) on Mac
/// Catalyst (Architecture §5 "Secrets", Coding Standards "Secrets go to the operating system's
/// store", REQ-NFR-001).
/// </summary>
public sealed class MauiSecretStore : ISecretStore
{
    private const string TargetPrefix = "Chatur:";

    /// <inheritdoc />
    public Task SaveAsync(string aName, string aSecret, CancellationToken aCt = default)
    {
#if WINDOWS
        WriteCredential(TargetPrefix + aName, aSecret);
        return Task.CompletedTask;
#elif MACCATALYST
        return SecureStorage.Default.SetAsync(aName, aSecret);
#else
        throw new PlatformNotSupportedException("Chatur ships for Windows and Mac Catalyst only.");
#endif
    }

    /// <inheritdoc />
    public Task<string?> ReadAsync(string aName, CancellationToken aCt = default)
    {
#if WINDOWS
        return Task.FromResult(ReadCredential(TargetPrefix + aName));
#elif MACCATALYST
        return SecureStorage.Default.GetAsync(aName);
#else
        throw new PlatformNotSupportedException("Chatur ships for Windows and Mac Catalyst only.");
#endif
    }

    /// <inheritdoc />
    public Task DeleteAsync(string aName, CancellationToken aCt = default)
    {
#if WINDOWS
        DeleteCredential(TargetPrefix + aName);
        return Task.CompletedTask;
#elif MACCATALYST
        SecureStorage.Default.Remove(aName);
        return Task.CompletedTask;
#else
        throw new PlatformNotSupportedException("Chatur ships for Windows and Mac Catalyst only.");
#endif
    }

#if WINDOWS
    private const int CredTypeGeneric = 1;
    private const int CredPersistLocalMachine = 2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string Comment;
        public long LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref Credential aCredential, uint aFlags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string aTarget, int aType, int aFlags, out IntPtr aCredentialPtr);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string aTarget, int aType, int aFlags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr aBuffer);

    private static void WriteCredential(string aTarget, string aSecret)
    {
        var vSecretBytes = Encoding.Unicode.GetBytes(aSecret);
        var vBlob = Marshal.AllocHGlobal(vSecretBytes.Length);
        try
        {
            Marshal.Copy(vSecretBytes, 0, vBlob, vSecretBytes.Length);
            var vCredential = new Credential
            {
                Type = CredTypeGeneric,
                TargetName = aTarget,
                Comment = string.Empty,
                CredentialBlobSize = vSecretBytes.Length,
                CredentialBlob = vBlob,
                Persist = CredPersistLocalMachine,
                TargetAlias = string.Empty,
                UserName = "Chatur"
            };

            if (!CredWrite(ref vCredential, 0))
            {
                throw new InvalidOperationException($"CredWrite failed for '{aTarget}' with error {Marshal.GetLastWin32Error()}.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(vBlob);
        }
    }

    private static string? ReadCredential(string aTarget)
    {
        if (!CredRead(aTarget, CredTypeGeneric, 0, out var vCredentialPtr))
        {
            return null;
        }

        try
        {
            var vCredential = Marshal.PtrToStructure<Credential>(vCredentialPtr);
            if (vCredential.CredentialBlob == IntPtr.Zero || vCredential.CredentialBlobSize == 0)
            {
                return null;
            }

            var vBytes = new byte[vCredential.CredentialBlobSize];
            Marshal.Copy(vCredential.CredentialBlob, vBytes, 0, vCredential.CredentialBlobSize);
            return Encoding.Unicode.GetString(vBytes);
        }
        finally
        {
            CredFree(vCredentialPtr);
        }
    }

    private static void DeleteCredential(string aTarget)
    {
        CredDelete(aTarget, CredTypeGeneric, 0);
    }
#endif
}
