using ImageSpider.Core.Abstractions;
using ImageSpider.Core.Options;
using ImageSpider.Infrastructure.Http;
using ImageSpider.Infrastructure.Providers;
using ImageSpider.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImageSpider.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImageSpiderInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<SpiderOptions>().BindConfiguration(SpiderOptions.SectionName);

        services.AddSingleton<SpiderHttpClientFactory>();

        // API
        services.AddHttpClient<BingImageSearchProvider>();
        services.AddSingleton<IImageSearchProvider>(sp => sp.GetRequiredService<BingImageSearchProvider>());

        services.AddHttpClient<GoogleImageSearchProvider>();
        services.AddSingleton<IImageSearchProvider>(sp => sp.GetRequiredService<GoogleImageSearchProvider>());

        services.AddHttpClient<PexelsImageSearchProvider>();
        services.AddSingleton<IImageSearchProvider>(sp => sp.GetRequiredService<PexelsImageSearchProvider>());

        services.AddHttpClient<PixabayImageSearchProvider>();
        services.AddSingleton<IImageSearchProvider>(sp => sp.GetRequiredService<PixabayImageSearchProvider>());

        services.AddHttpClient<UnsplashImageSearchProvider>();
        services.AddSingleton<IImageSearchProvider>(sp => sp.GetRequiredService<UnsplashImageSearchProvider>());

        // 爬虫
        services.AddSingleton<IImageSearchProvider, BaiduImageScraperProvider>();
        services.AddSingleton<IImageSearchProvider, SogouImageScraperProvider>();
        services.AddSingleton<IImageSearchProvider, So360ImageScraperProvider>();
        services.AddSingleton<IImageSearchProvider, BingImageScraperProvider>();
        services.AddSingleton<IImageSearchProvider, DuckDuckGoImageScraperProvider>();

        services.AddSingleton<IImageSearchService, ImageSearchService>();
        services.AddSingleton<IImageDownloadService, ImageDownloadService>();

        return services;
    }
}
