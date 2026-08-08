using System.IO;
using System.Windows;
using ImageSpider.App.Services;
using ImageSpider.App.ViewModels;
using ImageSpider.Core.Options;
using ImageSpider.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImageSpider.App;

public partial class App : Application
{
    public static IHost AppHost { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        var userConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ImageSpider",
            "appsettings.user.json");

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                if (File.Exists(userConfigPath))
                    config.AddJsonFile(userConfigPath, optional: true, reloadOnChange: true);
            })
            .ConfigureServices(services =>
            {
                services.AddOptions<SpiderOptions>().BindConfiguration(SpiderOptions.SectionName);
                services.AddImageSpiderInfrastructure();
                services.AddSingleton<UserSettingsStore>();
                services.AddSingleton<MainViewModel>();
                services.AddTransient<MainWindow>();
            })
            .Build();

        await AppHost.StartAsync();

        AppHost.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (AppHost is not null)
            await AppHost.StopAsync();
        AppHost?.Dispose();
        base.OnExit(e);
    }
}
