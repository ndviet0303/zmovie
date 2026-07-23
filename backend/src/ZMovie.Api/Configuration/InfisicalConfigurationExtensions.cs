using Infisical.Sdk;
using Infisical.Sdk.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ZMovie.Api.Configuration;

/// <summary>
/// Loads production configuration from Infisical before services are registered.
/// The machine-identity credentials remain bootstrap environment variables; all
/// application secrets are added to IConfiguration with their normal .NET keys.
/// </summary>
internal static class InfisicalConfigurationExtensions
{
    private const string ClientIdVariable = "INFISICAL_CLIENT_ID";
    private const string ClientSecretVariable = "INFISICAL_CLIENT_SECRET";
    private const string ProjectIdVariable = "INFISICAL_PROJECT_ID";

    public static async Task AddInfisicalSecretsAsync(this ConfigurationManager configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction()) return;

        var clientId = RequiredEnvironmentVariable(ClientIdVariable);
        var clientSecret = RequiredEnvironmentVariable(ClientSecretVariable);
        var projectId = RequiredEnvironmentVariable(ProjectIdVariable);
        var environmentSlug = Environment.GetEnvironmentVariable("INFISICAL_ENVIRONMENT") ?? "prod";
        var secretPath = Environment.GetEnvironmentVariable("INFISICAL_SECRET_PATH") ?? "/";
        var hostUri = Environment.GetEnvironmentVariable("INFISICAL_API_URL");

        var settingsBuilder = new InfisicalSdkSettingsBuilder();
        if (!string.IsNullOrWhiteSpace(hostUri)) settingsBuilder.WithHostUri(hostUri);

        var client = new InfisicalClient(settingsBuilder.Build());
        await client.Auth().UniversalAuth().LoginAsync(clientId, clientSecret);

        var secrets = await client.Secrets().ListAsync(new ListSecretsOptions
        {
            ProjectId = projectId,
            EnvironmentSlug = environmentSlug,
            SecretPath = secretPath,
            Recursive = true,
            ExpandSecretReferences = true,
            SetSecretsAsEnvironmentVariables = true
        }) ?? throw new InvalidOperationException("Infisical returned no secrets.");

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var secret in secrets)
        {
            // Infisical keys use the portable environment-variable convention.
            // IConfiguration uses ':' for hierarchical keys.
            values[secret.SecretKey.Replace("__", ":", StringComparison.Ordinal)] = secret.SecretValue;
        }

        configuration.AddInMemoryCollection(values);
    }

    private static string RequiredEnvironmentVariable(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Missing required production bootstrap variable: {name}.");
}
