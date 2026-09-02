using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ZMovie.Application.Common;
using ZMovie.Domain.Catalog;
using ZMovie.Domain.Common;
using ZMovie.Infrastructure.Catalog.Persistence;
using ZMovie.Infrastructure.Common;
using Xunit;

namespace ZMovie.Api.Tests.Infrastructure;

public sealed class DomainEventDispatcherTests
{
    private sealed record DummyDomainEvent(DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

    private sealed class FakePublisher : IPublisher
    {
        public List<INotification> PublishedNotifications { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            if (notification is INotification notif)
            {
                PublishedNotifications.Add(notif);
            }
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            PublishedNotifications.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDomainEventDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> DispatchedEvents { get; } = [];

        public Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
        {
            DispatchedEvents.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task MediatRDomainEventDispatcher_publishes_wrapped_notification()
    {
        var publisher = new FakePublisher();
        var dispatcher = new MediatRDomainEventDispatcher(publisher);

        var occurredAt = DateTimeOffset.UtcNow;
        var dummyEvent = new DummyDomainEvent(occurredAt);

        await dispatcher.DispatchEventsAsync([dummyEvent], CancellationToken.None);

        publisher.PublishedNotifications.Should().HaveCount(1);
        var notification = publisher.PublishedNotifications.Single()
            .Should().BeOfType<DomainEventNotification<DummyDomainEvent>>().Subject;
        notification.DomainEvent.Should().Be(dummyEvent);
    }

    [Fact]
    public async Task PublishDomainEventsInterceptor_dispatches_and_clears_events_on_save()
    {
        var dispatcher = new FakeDomainEventDispatcher();
        var interceptor = new PublishDomainEventsInterceptor(dispatcher);

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new CatalogDbContext(options);

        var title = Title.Create(
            TitleId.New(),
            TitleSlug.Parse("intercepted-title"),
            new LocalizedText("Tên", "Name"),
            new LocalizedText("Tóm tắt", "Synopsis"),
            "Action",
            ReleaseYear.FromInt(2025),
            TitleType.Movie,
            "https://cdn.example.com/poster.jpg",
            Runtime.FromMinutes(120),
            false,
            DateTimeOffset.UtcNow);

        dbContext.Titles.Add(title);

        var aggregate = (IAggregateRoot)title;
        aggregate.DomainEvents.Should().NotBeEmpty();

        await dbContext.SaveChangesAsync();

        dispatcher.DispatchedEvents.Should().ContainSingle(e => e is TitleCreatedDomainEvent);
        aggregate.DomainEvents.Should().BeEmpty();
    }
}
