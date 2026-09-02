using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace ZMovie.Api.Tests.Architecture;

public sealed partial class ArchitectureDependencyTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ExpectedProjectReferences =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["ZMovie.Domain"] = Set(),
            ["ZMovie.Application"] = Set("ZMovie.Domain"),
            ["ZMovie.Infrastructure"] = Set("ZMovie.Application", "ZMovie.Domain"),
            ["ZMovie.ServiceDefaults"] = Set(),
            ["ZMovie.Api"] = Set("ZMovie.Application", "ZMovie.Infrastructure", "ZMovie.ServiceDefaults"),
            ["ZMovie.AppHost"] = Set("ZMovie.Api"),
        };

    [Fact]
    public void Source_projects_keep_the_existing_dependency_direction()
    {
        var sourceRoot = Path.Combine(FindBackendRoot(), "src");
        var actual = Directory.EnumerateFiles(sourceRoot, "*.csproj", SearchOption.AllDirectories)
            .ToDictionary(
                project => Path.GetFileNameWithoutExtension(project)!,
                project => (IReadOnlySet<string>)XDocument.Load(project)
                    .Descendants("ProjectReference")
                    .Select(reference => reference.Attribute("Include")?.Value)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(path => Path.GetFileNameWithoutExtension(path!))
                    .ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);

        AssertExactMap(ExpectedProjectReferences, actual, "source project reference");
    }

    [Fact]
    public void Domain_has_no_framework_or_outer_layer_dependencies()
    {
        var domainProject = Path.Combine(FindBackendRoot(), "src", "ZMovie.Domain", "ZMovie.Domain.csproj");
        var dependencyItems = XDocument.Load(domainProject)
            .Descendants()
            .Where(element => element.Name.LocalName is "ProjectReference" or "PackageReference" or "FrameworkReference")
            .Select(element => $"{element.Name.LocalName}: {element.Attribute("Include")?.Value}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            dependencyItems.Length == 0,
            $"Domain must remain framework-free and cannot reference an outer layer:{Environment.NewLine}{string.Join(Environment.NewLine, dependencyItems)}");
    }

    [Fact]
    public void Inner_layer_namespaces_never_reference_an_outer_layer()
    {
        var sourceRoot = Path.Combine(FindBackendRoot(), "src");
        var violations = DiscoverLayerDependencies(sourceRoot)
            .Where(dependency => LayerRank(dependency.Target) > LayerRank(dependency.Source))
            .OrderBy(dependency => dependency.Source, StringComparer.Ordinal)
            .ThenBy(dependency => dependency.Target, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Inner layers may only depend on the same layer or point inward (Domain <- Application <- Infrastructure <- API):{Environment.NewLine}{Format(violations)}");
    }

    [Fact]
    public void Domain_contexts_do_not_reference_other_domain_contexts()
    {
        var domainRoot = Path.Combine(FindBackendRoot(), "src", "ZMovie.Domain");
        var violations = DiscoverCrossContextDependencies(domainRoot)
            .Where(dependency => dependency.Source.StartsWith("ZMovie.Domain.", StringComparison.Ordinal)
                                 && dependency.Target.StartsWith("ZMovie.Domain.", StringComparison.Ordinal))
            .OrderBy(dependency => dependency.Source, StringComparer.Ordinal)
            .ThenBy(dependency => dependency.Target, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"A Domain context cannot reference another context's Domain model:{Environment.NewLine}{Format(violations)}");
    }

    [Fact]
    public void Production_namespaces_match_the_exact_temporary_cross_context_allowlist()
    {
        var sourceRoot = Path.Combine(FindBackendRoot(), "src");
        var actual = DiscoverCrossContextDependencies(sourceRoot);
        var expected = TemporaryCrossContextDependencyAllowlist.Entries
            .Select(entry => entry.Dependency)
            .ToHashSet();

        var duplicateEntries = TemporaryCrossContextDependencyAllowlist.Entries
            .GroupBy(entry => entry.Dependency)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(edge => edge.Target, StringComparer.Ordinal)
            .ToArray();
        Assert.True(duplicateEntries.Length == 0, $"Duplicate allowlist entries:{Environment.NewLine}{Format(duplicateEntries)}");

        var unexpected = actual.Except(expected)
            .OrderBy(edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(edge => edge.Target, StringComparer.Ordinal)
            .ToArray();
        var stale = expected.Except(actual)
            .OrderBy(edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(edge => edge.Target, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            unexpected.Length == 0 && stale.Length == 0,
            $"Cross-context namespace dependencies changed. Additions require an explicit architecture decision; removals require deleting the stale exception."
            + $"{Environment.NewLine}Unexpected:{Environment.NewLine}{Format(unexpected)}"
            + $"{Environment.NewLine}Stale:{Environment.NewLine}{Format(stale)}");
    }

    [Fact]
    public void Application_cross_context_calls_use_explicit_contracts_or_read_models()
    {
        var sourceRoot = Path.Combine(FindBackendRoot(), "src");
        var actual = DiscoverCrossContextApplicationTypeDependencies(sourceRoot);
        var entries = TemporaryCrossContextDependencyAllowlist.ApplicationTypeEntries;
        var expected = entries.Select(entry => entry.Dependency).ToHashSet();

        var duplicateEntries = entries
            .GroupBy(entry => entry.Dependency)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(edge => edge.Target, StringComparer.Ordinal)
            .ToArray();
        var undocumentedEntries = entries
            .Where(entry => string.IsNullOrWhiteSpace(entry.Reason) || string.IsNullOrWhiteSpace(entry.RemovalCheckpoint))
            .Select(entry => entry.Dependency)
            .OrderBy(edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(edge => edge.Target, StringComparer.Ordinal)
            .ToArray();
        var unexpected = actual.Except(expected)
            .OrderBy(edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(edge => edge.Target, StringComparer.Ordinal)
            .ToArray();
        var stale = expected.Except(actual)
            .OrderBy(edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(edge => edge.Target, StringComparer.Ordinal)
            .ToArray();

        Assert.True(duplicateEntries.Length == 0, $"Duplicate type allowlist entries:{Environment.NewLine}{Format(duplicateEntries)}");
        Assert.True(undocumentedEntries.Length == 0, $"Every type exception needs a reason and removal checkpoint:{Environment.NewLine}{Format(undocumentedEntries)}");
        Assert.True(
            unexpected.Length == 0 && stale.Length == 0,
            "Cross-context Application type dependencies changed. Only explicit contracts/read models are allowed; temporary violations require a documented exception."
            + $"{Environment.NewLine}Unexpected:{Environment.NewLine}{Format(unexpected)}"
            + $"{Environment.NewLine}Stale:{Environment.NewLine}{Format(stale)}");
    }

    [Fact]
    public void Temporary_namespace_exceptions_have_reasons_and_removal_checkpoints()
    {
        var undocumented = TemporaryCrossContextDependencyAllowlist.Entries
            .Where(entry => string.IsNullOrWhiteSpace(entry.Reason) || string.IsNullOrWhiteSpace(entry.RemovalCheckpoint))
            .Select(entry => entry.Dependency)
            .OrderBy(edge => edge.Source, StringComparer.Ordinal)
            .ThenBy(edge => edge.Target, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            undocumented.Length == 0,
            $"Every temporary namespace exception needs a reason and removal checkpoint:{Environment.NewLine}{Format(undocumented)}");
    }

    private static HashSet<NamespaceDependency> DiscoverCrossContextDependencies(string sourceRoot)
    {
        var dependencies = new HashSet<NamespaceDependency>();
        foreach (var path in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
                     .Where(path => !IsGeneratedOrBuildOutput(path)))
        {
            var source = File.ReadAllText(path);
            var declaredNamespace = NamespacePattern().Match(source).Groups[1].Value;
            if (string.IsNullOrEmpty(declaredNamespace)) continue;

            var sourceModule = ModuleNamespace(declaredNamespace);
            foreach (Match match in QualifiedNamespacePattern().Matches(source))
            {
                var targetModule = ModuleNamespace(match.Groups[1].Value);
                if (IsCrossContext(sourceModule, targetModule))
                    dependencies.Add(new NamespaceDependency(sourceModule, targetModule));
            }
        }

        return dependencies;
    }

    private static HashSet<NamespaceDependency> DiscoverLayerDependencies(string sourceRoot)
    {
        var dependencies = new HashSet<NamespaceDependency>();
        foreach (var path in ProductionSourceFiles(sourceRoot))
        {
            var source = SourceWithoutTriviaPattern().Replace(File.ReadAllText(path), " ");
            var declaredNamespace = LayerNamespacePattern().Match(source).Groups[1].Value;
            if (string.IsNullOrEmpty(declaredNamespace)) continue;

            var sourceLayer = LayerNamespace(declaredNamespace);
            foreach (Match match in LayerReferencePattern().Matches(source))
            {
                var targetLayer = LayerNamespace(match.Groups[1].Value);
                if (sourceLayer != targetLayer)
                    dependencies.Add(new NamespaceDependency(sourceLayer, targetLayer));
            }
        }

        return dependencies;
    }

    private static HashSet<CrossContextTypeDependency> DiscoverCrossContextApplicationTypeDependencies(string sourceRoot)
    {
        var sourceFiles = ProductionSourceFiles(sourceRoot).ToArray();
        var declaredTypes = sourceFiles
            .Select(path => SourceWithoutTriviaPattern().Replace(File.ReadAllText(path), " "))
            .SelectMany(source =>
            {
                var declaredNamespace = NamespacePattern().Match(source).Groups[1].Value;
                return string.IsNullOrEmpty(declaredNamespace)
                    ? []
                    : TypeDeclarationPattern().Matches(source).Select(match => new DeclaredType(declaredNamespace, match.Groups[1].Value));
            })
            .Distinct()
            .ToArray();
        var typesByNamespace = declaredTypes
            .GroupBy(type => type.Namespace, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var dependencies = new HashSet<CrossContextTypeDependency>();
        foreach (var path in sourceFiles.Where(path => path.Contains($"{Path.DirectorySeparatorChar}ZMovie.Application{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            var source = SourceWithoutTriviaPattern().Replace(File.ReadAllText(path), " ");
            var declaredNamespace = NamespacePattern().Match(source).Groups[1].Value;
            if (string.IsNullOrEmpty(declaredNamespace)) continue;

            var sourceModule = ModuleNamespace(declaredNamespace);
            foreach (Match usingMatch in UsingNamespacePattern().Matches(source))
            {
                var importedNamespace = usingMatch.Groups[1].Value;
                if (!typesByNamespace.TryGetValue(importedNamespace, out var importedTypes)) continue;

                foreach (var targetType in importedTypes.Where(type => IsCrossContext(sourceModule, ModuleNamespace(type.Namespace))))
                {
                    if (ContainsIdentifier(source, targetType.Name))
                        dependencies.Add(new CrossContextTypeDependency(sourceModule, targetType.FullName));
                }
            }

            foreach (var targetType in declaredTypes.Where(type => IsCrossContext(sourceModule, ModuleNamespace(type.Namespace))))
            {
                if (source.Contains(targetType.FullName, StringComparison.Ordinal))
                    dependencies.Add(new CrossContextTypeDependency(sourceModule, targetType.FullName));
            }
        }

        return dependencies;
    }

    private static bool IsCrossContext(string source, string target)
    {
        if (source == target || target is "ZMovie.Application.Common" or "ZMovie.Domain.Common") return false;
        return ModuleName(source) != ModuleName(target);
    }

    private static string ModuleNamespace(string value)
    {
        var parts = value.Split('.');
        return parts.Length < 3 ? value : string.Join('.', parts.Take(3));
    }

    private static string ModuleName(string value) => value.Split('.').ElementAtOrDefault(2) ?? value;

    private static bool IsGeneratedOrBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj");

    private static IEnumerable<string> ProductionSourceFiles(string sourceRoot) =>
        Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedOrBuildOutput(path));

    private static int LayerRank(string layer) => layer switch
    {
        "ZMovie.Domain" => 0,
        "ZMovie.Application" => 1,
        "ZMovie.Infrastructure" => 2,
        "ZMovie.Api" => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, "Unknown architecture layer."),
    };

    private static string LayerNamespace(string value) => string.Join('.', value.Split('.').Take(2));

    private static bool ContainsIdentifier(string source, string identifier) =>
        Regex.IsMatch(source, $@"(?<![A-Za-z0-9_]){Regex.Escape(identifier)}(?![A-Za-z0-9_])");

    private static void AssertExactMap(
        IReadOnlyDictionary<string, IReadOnlySet<string>> expected,
        IReadOnlyDictionary<string, IReadOnlySet<string>> actual,
        string description)
    {
        var unexpectedProjects = actual.Keys.Except(expected.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var missingProjects = expected.Keys.Except(actual.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var changed = expected.Keys.Intersect(actual.Keys, StringComparer.Ordinal)
            .Where(project => !expected[project].SetEquals(actual[project]))
            .Order(StringComparer.Ordinal)
            .Select(project => $"{project}: expected [{string.Join(", ", expected[project].Order(StringComparer.Ordinal))}], actual [{string.Join(", ", actual[project].Order(StringComparer.Ordinal))}]")
            .ToArray();

        Assert.True(
            unexpectedProjects.Length == 0 && missingProjects.Length == 0 && changed.Length == 0,
            $"The {description} graph changed."
            + $"{Environment.NewLine}Unexpected projects: {string.Join(", ", unexpectedProjects)}"
            + $"{Environment.NewLine}Missing projects: {string.Join(", ", missingProjects)}"
            + $"{Environment.NewLine}Changed references:{Environment.NewLine}{string.Join(Environment.NewLine, changed)}");
    }

    private static string FindBackendRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ZMovie.slnx"))
                && Directory.Exists(Path.Combine(directory.FullName, "src")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException($"Could not locate the backend root above {AppContext.BaseDirectory}.");
    }

    private static IReadOnlySet<string> Set(params string[] values) => values.ToHashSet(StringComparer.Ordinal);

    private static string Format(IEnumerable<NamespaceDependency> dependencies) =>
        string.Join(Environment.NewLine, dependencies.Select(dependency => $"  {dependency}"));

    private static string Format(IEnumerable<CrossContextTypeDependency> dependencies) =>
        string.Join(Environment.NewLine, dependencies.Select(dependency => $"  {dependency}"));

    private readonly record struct DeclaredType(string Namespace, string Name)
    {
        public string FullName => $"{Namespace}.{Name}";
    }

    [GeneratedRegex(@"^\s*namespace\s+(ZMovie\.(?:Domain|Application|Infrastructure)\.[A-Za-z0-9_.]+)\s*[;{]", RegexOptions.Multiline)]
    private static partial Regex NamespacePattern();

    [GeneratedRegex(@"\b(ZMovie\.(?:Domain|Application|Infrastructure)\.[A-Za-z_][A-Za-z0-9_]*)")]
    private static partial Regex QualifiedNamespacePattern();

    [GeneratedRegex(@"^\s*namespace\s+(ZMovie\.(?:Domain|Application|Infrastructure|Api)(?:\.[A-Za-z0-9_]+)*)\s*[;{]", RegexOptions.Multiline)]
    private static partial Regex LayerNamespacePattern();

    [GeneratedRegex(@"\b(ZMovie\.(?:Domain|Application|Infrastructure|Api)(?:\.[A-Za-z_][A-Za-z0-9_]*)?)")]
    private static partial Regex LayerReferencePattern();

    [GeneratedRegex(@"^\s*(?:global\s+)?using\s+(ZMovie\.(?:Domain|Application)\.[A-Za-z_][A-Za-z0-9_.]*)\s*;", RegexOptions.Multiline)]
    private static partial Regex UsingNamespacePattern();

    [GeneratedRegex(@"\b(?:class|interface|struct|enum|record(?:\s+(?:class|struct))?)\s+([A-Za-z_][A-Za-z0-9_]*)")]
    private static partial Regex TypeDeclarationPattern();

    [GeneratedRegex("""//[^\r\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'""")]
    private static partial Regex SourceWithoutTriviaPattern();
}
