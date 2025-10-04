using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Control.Core;

namespace Control.Wpf;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices(ConfigureServices)
            .UseSerilog((context, services, configuration) =>
            {
                var settings = context.Configuration.GetSection("ControlSettings").Get<ControlSettings>() ?? new ControlSettings();
                
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .WriteTo.File(settings.LogFilePath, rollingInterval: RollingInterval.Day);
                
                if (settings.EnableConsoleLogging)
                {
                    configuration.WriteTo.Console();
                }
            })
            .Build();

        try
        {
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            throw;
        }

        base.OnStartup(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register services
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IInterlockService, DemoInterlocks>();
        services.AddSingleton<IJobService, DemoJobs>();
        
        // Register ViewModels
        services.AddTransient<MainViewModel>();
        
        // Register Windows
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
