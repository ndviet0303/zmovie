using ErrorOr;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ZMovie.Api.Tests.Infrastructure;
using ZMovie.Application.Engagement;
using Xunit;

namespace ZMovie.Api.Tests.FourLayer;

public sealed class ReviewQueryCharacterizationTests
{
    public static TheoryData<int[], double> RoundedAverageCases => new()
    {
        { [8, 9, 9], 8.7 },
        { [8, 8, 8, 9], 8.2 },
    };

    [Fact]
    public async Task Listing_orders_by_updated_at_descending_and_maps_each_item()
    {
        using var factory = new ZMovieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var reviews = new[]
        {
            new ReviewInput(Guid.Parse("11111111-1111-4111-8111-111111111111"), "Oldest author", 4, "Oldest comment"),
            new ReviewInput(Guid.Parse("22222222-2222-4222-8222-222222222222"), "Middle author", 7, null),
            new ReviewInput(Guid.Parse("33333333-3333-4333-8333-333333333333"), "Newest author", 10, "Newest comment"),
        };

        foreach (var review in reviews)
        {
            var submitted = await sender.Send(new SubmitTitleReviewCommand(
                review.UserId,
                review.AuthorName,
                "baseline-title",
                review.Rating,
                review.Comment));
            submitted.IsError.Should().BeFalse();
            await Task.Delay(10);
        }

        var result = await sender.Send(new GetTitleReviewsQuery("baseline-title"));

        result.IsError.Should().BeFalse();
        result.Value.RatingCount.Should().Be(3);
        result.Value.Items.Select(item => (item.AuthorName, item.Rating, item.Comment)).Should().Equal(
            ("Newest author", 10, "Newest comment"),
            ("Middle author", 7, null),
            ("Oldest author", 4, "Oldest comment"));
        result.Value.Items.Select(item => item.Id).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
        result.Value.Items.Select(item => item.UpdatedAt).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Empty_listing_reports_zero_average_and_count()
    {
        var result = await QueryAsync([]);

        result.IsError.Should().BeFalse();
        result.Value.AverageRating.Should().Be(0);
        result.Value.RatingCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(RoundedAverageCases))]
    public async Task Average_uses_one_decimal_math_round_semantics(int[] ratings, double expected)
    {
        var reviews = ratings.Select((rating, index) => new ReviewInput(
            Guid.CreateVersion7(),
            $"Author {index}",
            rating,
            $"Comment {index}")).ToArray();

        var result = await QueryAsync(reviews);

        result.IsError.Should().BeFalse();
        result.Value.AverageRating.Should().Be(expected);
        result.Value.RatingCount.Should().Be(ratings.Length);
    }

    [Fact]
    public async Task Missing_catalog_title_returns_existing_not_found_error()
    {
        var result = await QueryAsync([], "missing-title");

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("catalog.title.not_found");
        result.FirstError.Description.Should().Be("Catalog title not found.");
    }

    private static async Task<ErrorOr<TitleReviewsResponse>> QueryAsync(
        IReadOnlyList<ReviewInput> reviews,
        string slug = "baseline-title")
    {
        using var factory = new ZMovieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        foreach (var review in reviews)
        {
            var submitted = await sender.Send(new SubmitTitleReviewCommand(
                review.UserId,
                review.AuthorName,
                "baseline-title",
                review.Rating,
                review.Comment));
            submitted.IsError.Should().BeFalse();
        }

        return await sender.Send(new GetTitleReviewsQuery(slug));
    }

    private sealed record ReviewInput(Guid UserId, string AuthorName, int Rating, string? Comment);
}
