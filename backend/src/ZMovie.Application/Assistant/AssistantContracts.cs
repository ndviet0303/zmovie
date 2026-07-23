using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Catalog;
using ZMovie.Application.Common;

namespace ZMovie.Application.Assistant;

public sealed record AssistantCatalogTitle(TitleSummary Title, string Synopsis);
public sealed record AssistantReply(string Message, IReadOnlyList<TitleSummary> Suggestions);

public interface ICatalogAssistantStore
{
    Task<IReadOnlyList<AssistantCatalogTitle>> SearchAsync(string message, string locale, int limit, CancellationToken ct);
}

public sealed record AskCatalogAssistantQuery(string Message, string? Locale) : IQuery<AssistantReply>;
public sealed class AskCatalogAssistantValidator : AbstractValidator<AskCatalogAssistantQuery>
{
    public AskCatalogAssistantValidator() => RuleFor(x => x.Message).NotEmpty().MaximumLength(500);
}
public sealed class AskCatalogAssistantHandler(ICatalogAssistantStore store) : IRequestHandler<AskCatalogAssistantQuery, ErrorOr<AssistantReply>>
{
    public async Task<ErrorOr<AssistantReply>> Handle(AskCatalogAssistantQuery request, CancellationToken ct)
    {
        var locale = Locale.Normalize(request.Locale);
        var matches = await store.SearchAsync(request.Message, locale, 3, ct);
        var suggestions = matches.Select(x => x.Title).ToList();
        var message = locale == "vi"
            ? suggestions.Count > 0
                ? $"Mình tìm được {suggestions.Count} phim hợp với “{request.Message.Trim()}”. Bạn thử xem các lựa chọn bên dưới nhé."
                : "Mình chưa tìm được phim khớp. Bạn thử nêu thể loại, tâm trạng hoặc tên diễn viên nhé."
            : suggestions.Count > 0
                ? $"I found {suggestions.Count} titles that match “{request.Message.Trim()}”. Try these picks."
                : "I could not find a close match. Try a genre, mood, or actor name.";
        return new AssistantReply(message, suggestions);
    }
}
