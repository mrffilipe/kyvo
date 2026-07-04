namespace Kyvo.Infrastructure.Configurations;

/// <summary>
/// Configuration for the ASP.NET Core Data Protection keyring used to encrypt
/// sensitive identity provider configuration values at rest.
/// </summary>
public sealed record SecretProtectionOptions
{
    public const string SECTION = "SecretProtection";

    /// <summary>
    /// Filesystem directory where the data protection key ring is persisted.
    /// In production this should be a persistent, app-only directory backed up alongside the database
    /// (lose the keys and previously encrypted secrets become unreadable).
    /// </summary>
    public required string KeyDirectoryPath { get; init; }

    /// <summary>
    /// Application name used to isolate the key ring from other applications sharing the same directory.
    /// </summary>
    public required string ApplicationName { get; init; }
}
