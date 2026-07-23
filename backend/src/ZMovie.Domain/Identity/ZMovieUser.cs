namespace ZMovie.Domain.Identity;

public sealed class ZMovieUser
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string GoogleSubject { get; init; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSignedInAt { get; set; } = DateTimeOffset.UtcNow;
}
