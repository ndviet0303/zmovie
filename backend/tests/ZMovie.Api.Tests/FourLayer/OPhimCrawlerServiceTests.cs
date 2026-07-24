using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using ZMovie.Api.Services;
using Xunit;

namespace ZMovie.Api.Tests.FourLayer;

public sealed class OPhimCrawlerServiceTests
{
    [Fact]
    public async Task Validates_ranges_and_reports_startup_failures()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        using var service = new OPhimCrawlerService(provider.GetRequiredService<IServiceScopeFactory>());

        Assert.False(service.TryStart(new OPhimCrawlerStartOptions(0, null, false)));
        Assert.False(service.TryStart(new OPhimCrawlerStartOptions(3, 2, false)));
        Assert.False(service.TryStop());

        Assert.True(service.TryStart(new OPhimCrawlerStartOptions(1, null, false)));
        for (var attempt = 0; attempt < 20 && service.GetStatus().IsRunning; attempt++) await Task.Delay(10);

        var status = service.GetStatus();
        Assert.False(status.IsRunning);
        Assert.Equal("Crawler gặp lỗi.", status.Message);
        Assert.NotNull(status.Error);
    }

    [Fact]
    public void Reports_plain_and_structured_progress_messages()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        using var service = new OPhimCrawlerService(provider.GetRequiredService<IServiceScopeFactory>());
        var report = typeof(OPhimCrawlerService).GetMethod("Report", BindingFlags.Instance | BindingFlags.NonPublic)!;

        report.Invoke(service, ["Đang chuẩn bị…"]);
        Assert.Equal("Đang chuẩn bị…", service.GetStatus().Message);
        report.Invoke(service, ["OPhim catalog: page 2/4 (10 titles, 12 episodes)"]);
        var status = service.GetStatus();
        Assert.Equal(2, status.CurrentPage);
        Assert.Equal(4, status.TotalPages);
        Assert.Equal(10, status.TitlesImported);
        Assert.Equal(12, status.EpisodesImported);
    }
}
