using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Miner.Ledger;
using Miner.Services;
using Miner.ViewModels;
using Miner.Views;
using Serilog;
using Splat;

namespace Miner;

static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static void Main(string[] args) => BuildAvaloniaApp()
            .Start<MainWindow>(() => new MainWindowViewModel());
    

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        const string mt = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] [{MemberName}:{LineNumber}] {Message}{NewLine}{Exception}";
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "miner.log"), outputTemplate: mt,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                rollOnFileSizeLimit: true)
            .CreateLogger();
        
        Locator.CurrentMutable.RegisterConstant(Log.Logger);
        Locator.CurrentMutable.RegisterConstant<ISessionService>(new SessionService());
        Locator.CurrentMutable.Register<IBlockchain>(() =>
            new Blockchain(Locator.Current.GetService<ISessionService>()));
        
        return AppBuilder.Configure<App>()
            .UseReactiveUI()
            .UsePlatformDetect()
            .LogToTrace();
    }
}