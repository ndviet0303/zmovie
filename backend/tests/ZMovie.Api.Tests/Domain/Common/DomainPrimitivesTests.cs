using FluentAssertions;
using ZMovie.Domain.Common;
using Xunit;

namespace ZMovie.Api.Tests.Domain.Common;

public sealed class DomainPrimitivesTests
{
    private sealed class TestId(Guid value)
    {
        public Guid Value { get; } = value;
        public override bool Equals(object? obj) => obj is TestId other && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
    }

    private sealed class TestEntity : Entity<TestId>
    {
        public TestEntity(TestId id)
        {
            Id = id;
        }
    }

    private sealed class TestAggregate : AggregateRoot
    {
        public void DoSomething(DateTimeOffset occurredAt)
        {
            RaiseDomainEvent(new TestDomainEvent(occurredAt));
        }
    }

    private sealed record TestDomainEvent(DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

    private sealed class TestValueObject(string name, int value) : ValueObject
    {
        public string Name { get; } = name;
        public int Value { get; } = value;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Name;
            yield return Value;
        }
    }

    [Fact]
    public void Entity_equality_is_based_on_identifier()
    {
        var guid = Guid.NewGuid();
        var id1 = new TestId(guid);
        var id2 = new TestId(guid);
        var id3 = new TestId(Guid.NewGuid());

        var entity1 = new TestEntity(id1);
        var entity2 = new TestEntity(id2);
        var entity3 = new TestEntity(id3);

        (entity1 == entity2).Should().BeTrue();
        (entity1 != entity3).Should().BeTrue();
        entity1.Equals(entity2).Should().BeTrue();
        entity1.Equals(entity3).Should().BeFalse();
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void AggregateRoot_records_and_clears_domain_events()
    {
        var aggregate = new TestAggregate();
        var aggregateRoot = (IAggregateRoot)aggregate;

        aggregateRoot.DomainEvents.Should().BeEmpty();

        var now = DateTimeOffset.UtcNow;
        aggregate.DoSomething(now);

        aggregateRoot.DomainEvents.Should().HaveCount(1);
        var domainEvent = aggregateRoot.DomainEvents.Single().Should().BeOfType<TestDomainEvent>().Subject;
        domainEvent.OccurredAt.Should().Be(now);
        domainEvent.EventId.Should().NotBeEmpty();

        aggregateRoot.ClearDomainEvents();
        aggregateRoot.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ValueObject_equality_is_based_on_equality_components()
    {
        var vo1 = new TestValueObject("Alpha", 42);
        var vo2 = new TestValueObject("Alpha", 42);
        var vo3 = new TestValueObject("Beta", 42);

        (vo1 == vo2).Should().BeTrue();
        (vo1 != vo3).Should().BeTrue();
        vo1.Equals(vo2).Should().BeTrue();
        vo1.Equals(vo3).Should().BeFalse();
        vo1.GetHashCode().Should().Be(vo2.GetHashCode());
    }
}
