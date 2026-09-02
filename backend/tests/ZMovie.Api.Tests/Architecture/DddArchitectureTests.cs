using System.Reflection;
using FluentAssertions;
using ZMovie.Application.Common;
using ZMovie.Domain.Common;
using Xunit;

namespace ZMovie.Api.Tests.Architecture;

public sealed class DddArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(IDomainEvent).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IDomainEventHandler<>).Assembly;

    [Fact]
    public void Domain_aggregates_implement_IAggregateRoot()
    {
        var aggregateTypes = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Namespace is not null
                        && t.Namespace.StartsWith("ZMovie.Domain.", StringComparison.Ordinal)
                        && t.Namespace != "ZMovie.Domain.Common"
                        && (t.Name is "Title" or "Genre" or "User" or "Review" or "SavedTitle" or "WatchProgress" or "TitleViewEvent" or "AssistantLearningEvent"))
            .ToList();

        aggregateTypes.Should().NotBeEmpty();

        foreach (var type in aggregateTypes)
        {
            typeof(IAggregateRoot).IsAssignableFrom(type).Should().BeTrue(
                $"Aggregate root '{type.Name}' must implement IAggregateRoot");
        }
    }

    [Fact]
    public void Domain_events_implement_IDomainEvent_and_are_sealed_records()
    {
        var domainEventTypes = DomainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Name.EndsWith("DomainEvent", StringComparison.Ordinal))
            .ToList();

        domainEventTypes.Should().NotBeEmpty();

        foreach (var eventType in domainEventTypes)
        {
            typeof(IDomainEvent).IsAssignableFrom(eventType).Should().BeTrue(
                $"Domain event '{eventType.Name}' must implement IDomainEvent");
            eventType.IsSealed.Should().BeTrue(
                $"Domain event '{eventType.Name}' should be a sealed record");
        }
    }

    [Fact]
    public void Domain_event_handlers_implement_IDomainEventHandler()
    {
        var domainEventHandlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Name.EndsWith("DomainEventHandler", StringComparison.Ordinal))
            .ToList();

        foreach (var handlerType in domainEventHandlerTypes)
        {
            var implementsDomainEventHandler = handlerType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));

            implementsDomainEventHandler.Should().BeTrue(
                $"Domain event handler '{handlerType.Name}' must implement IDomainEventHandler<T>");
        }
    }
}
