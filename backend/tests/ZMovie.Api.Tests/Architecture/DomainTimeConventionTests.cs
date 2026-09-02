using System.Text.RegularExpressions;
using Xunit;

namespace ZMovie.Api.Tests.Architecture;

public sealed partial class DomainTimeConventionTests
{
    private static readonly IReadOnlyDictionary<string, int> LegacyClockUsage =
        new Dictionary<string, int>(StringComparer.Ordinal);

    [Fact]
    public void Domain_does_not_add_new_wall_clock_dependencies()
    {
        var domainRoot = Path.Combine(FindBackendRoot(), "src", "ZMovie.Domain");
        var actual = Directory.EnumerateFiles(domainRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj"))
            .Select(path => new
            {
                Path = Path.GetRelativePath(domainRoot, path).Replace(Path.DirectorySeparatorChar, '/'),
                Count = WallClockPattern().Count(File.ReadAllText(path)),
            })
            .Where(item => item.Count > 0)
            .ToDictionary(item => item.Path, item => item.Count, StringComparer.Ordinal);

        Assert.Equal(LegacyClockUsage.OrderBy(item => item.Key), actual.OrderBy(item => item.Key));
    }

    private static string FindBackendRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ZMovie.slnx"))) return directory.FullName;
        }

        throw new DirectoryNotFoundException($"Could not locate the backend root above {AppContext.BaseDirectory}.");
    }

    [GeneratedRegex(@"\b(?:DateTime(?:Offset)?\.(?:UtcNow|Now)|TimeProvider\.System)\b")]
    private static partial Regex WallClockPattern();
}
