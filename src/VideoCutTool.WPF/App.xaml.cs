using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO;
using System.Windows;
using VideoCutTool.WPF.ViewModels;
using VideoCutTool.Core.Interfaces;
using VideoCutTool.Infrastructure.Services;

namespace VideoCutTool.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Host not initialized");

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                Log.Information("Starting Video Cut Tool application");

                // Build host
                _host = Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.SetBasePath(Directory.GetCurrentDirectory())
                              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        ConfigureServices(services, context.Configuration);
                    })
                    .UseSerilog((context, config) =>
                    {
                        config.MinimumLevel.Debug()
                              .WriteTo.File("logs/videocuttool-.log", rollingInterval: RollingInterval.Day)
                              .WriteTo.Debug();
                    })
                    .Build();

                await _host.StartAsync();

                Log.Information("Application started successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application failed to start");
                MessageBox.Show($"应用程序启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration
            services.AddSingleton(configuration);

            // Register services
            services.AddSingleton<IVideoService, VideoService>();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddSingleton<IProjectService, ProjectService>();

            // Register ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<TimelineControlViewModel>();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }

            Log.Information("Application shutting down");
            Log.CloseAndFlush();

            base.OnExit(e);
        }
    }
}

