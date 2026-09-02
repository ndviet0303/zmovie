using System.Text.RegularExpressions;
using Xunit;

namespace ZMovie.Api.Tests.Architecture;

public sealed partial class CompatibilityAdapterRegistryTests
{
    // Add an entry only with a documented non-atomic cutover reason and deletion
    // checkpoint in docs/backend-compatibility-adapters.md.
    private static readonly IReadOnlySet<string> ActiveAdapters = new HashSet<string>(StringComparer.Ordinal);

    [Fact]
    public void Production_compatibility_adapters_match_the_active_registry()
    {
        var sourceRoot = Path.Combine(FindBackendRoot(), "src");
        var actual = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj"))
            .SelectMany(path => AdapterPattern().Matches(File.ReadAllText(path)).Select(match => match.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(ActiveAdapters.Order(), actual.Order());
    }

    private static string FindBackendRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ZMovie.slnx"))) return directory.FullName;
        }

        throw new DirectoryNotFoundException($"Could not locate the backend root above {AppContext.BaseDirectory}.");
    }

    [GeneratedRegex(@"\b(?:class|record)\s+([A-Za-z_][A-Za-z0-9_]*CompatibilityAdapter)\b")]
    private static partial Regex AdapterPattern();
}
