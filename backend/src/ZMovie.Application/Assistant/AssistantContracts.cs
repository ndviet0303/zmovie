using ErrorOr;
using FluentValidation;
using MediatR;
using ZMovie.Application.Catalog;
using ZMovie.Application.Common;

namespace ZMovie.Application.Assistant;

public sealed record AssistantCatalogTitle(TitleSummary Title, string Synopsis);
public sealed record AssistantReply(string Message, IReadOnlyList<TitleSummary> Suggestions);
public sealed record AssistantContextResponse(IReadOnlyList<AssistantCatalogTitle> Matches);
public sealed record AssistantGenerationRequest(string Message, string Locale, IReadOnlyList<AssistantCatalogTitle> Matches);

public interface ICatalogAssistantStore
{
    Task<IReadOnlyList<AssistantCatalogTitle>> SearchAsync(Guid userId, string message, string locale, int limit, CancellationToken ct);
}

public interface IAssistantTextGenerator
{
    Task<string?> GenerateAsync(AssistantGenerationRequest request, CancellationToken ct);
}

public sealed record AskCatalogAssistantQuery(Guid UserId, string Message, string? Locale) : IQuery<AssistantReply>;
public sealed record GetAssistantContextQuery(Guid UserId, string Message, string? Locale) : IQuery<AssistantContextResponse>;
public sealed class GetAssistantContextHandler(ICatalogAssistantStore store) : IRequestHandler<GetAssistantContextQuery, ErrorOr<AssistantContextResponse>>
{
    public async Task<ErrorOr<AssistantContextResponse>> Handle(GetAssistantContextQuery request, CancellationToken ct)
    {
        var matches = await store.SearchAsync(request.UserId, request.Message, Locale.Normalize(request.Locale), 8, ct);
        return new AssistantContextResponse(matches);
    }
}

public sealed class AskCatalogAssistantValidator : AbstractValidator<AskCatalogAssistantQuery>
{
    public AskCatalogAssistantValidator() => RuleFor(x => x.Message).NotEmpty().MaximumLength(500);
}
public sealed class AskCatalogAssistantHandler(ICatalogAssistantStore store, IAssistantTextGenerator generator) : IRequestHandler<AskCatalogAssistantQuery, ErrorOr<AssistantReply>>
{
    public async Task<ErrorOr<AssistantReply>> Handle(AskCatalogAssistantQuery request, CancellationToken ct)
    {
        var locale = Locale.Normalize(request.Locale);
        var matches = await store.SearchAsync(request.UserId, request.Message, locale, 8, ct);
        var suggestions = matches.Take(3).Select(x => x.Title).ToList();
        var generated = matches.Count > 0 ? await generator.GenerateAsync(new AssistantGenerationRequest(request.Message.Trim(), locale, matches), ct) : null;
        var message = string.IsNullOrWhiteSpace(generated)
            ? FallbackMessage(request.Message, locale, suggestions.Count)
            : generated.Trim();
        return new AssistantReply(message, suggestions);
    }

    private static string FallbackMessage(string request, string locale, int suggestionCount)
    {
        if (locale == "vi" && AssistantMood.WantsComfort(request))
            return suggestionCount > 0
                ? "Nghe như hôm nay bạn đang cần một bộ phim thật nhẹ nhàng. Mình chọn vài lựa chọn ấm áp và có chút hy vọng để bạn xem nhé."
                : "Nếu hôm nay bạn đang thấy buồn, hãy cho mình biết bạn muốn một bộ phim nhẹ nhàng, hài hước hay có chút lãng mạn nhé.";

        return locale == "vi"
            ? suggestionCount > 0
                ? $"Mình tìm được {suggestionCount} phim hợp với “{request.Trim()}”. Bạn thử xem các lựa chọn bên dưới nhé."
                : "Mình chưa tìm được phim khớp. Bạn thử nêu thể loại, tâm trạng hoặc tên diễn viên nhé."
            : suggestionCount > 0
                ? $"I found {suggestionCount} titles that match “{request.Trim()}”. Try these picks."
                : "I could not find a close match. Try a genre, mood, or actor name.";
    }
}
