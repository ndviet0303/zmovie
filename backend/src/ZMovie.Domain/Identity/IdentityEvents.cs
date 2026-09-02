using ZMovie.Domain.Common;

namespace ZMovie.Domain.Identity;

public sealed record UserCreatedDomainEvent(
    UserId UserId,
    ExternalIdentity ExternalIdentity,
    string Email,
    Role Role,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record UserSignedInDomainEvent(
    UserId UserId,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);

public sealed record UserRoleChangedDomainEvent(
    UserId UserId,
    Role OldRole,
    Role NewRole,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);
