using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Richie.Infrastructure;
using Serilog;

namespace Richie.UI;

/// <summary>
/// Application composition root: builds the generic host (DI + Serilog), starts it,
/// and shows the main shell window resolved from the container.
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Richie", "logs");
        Directory.CreateDirectory(logDirectory);

        _host = Host.CreateDefaultBuilder()
            .UseSerilog((context, configuration) => configuration
                .MinimumLevel.Information()
                .WriteTo.Debug()
                .WriteTo.File(
                    Path.Combine(logDirectory, "richie-.log"),
                    rollingInterval: RollingInterval.Day))
            .ConfigureServices((context, services) =>
            {
                services.AddInfrastructure();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await _host.StartAsync();
        _host.Services.GetRequiredService<MainWindow>().Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
