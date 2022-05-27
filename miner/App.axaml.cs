using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Miner.Services;
using Miner.ViewModels;
using Miner.Views;
using ReactiveUI;
using Splat;
using Splat.Serilog;

namespace Miner
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        public override void OnFrameworkInitializationCompleted()
        {
            // Create the AutoSuspendHelper.
            var suspension = new AutoSuspendHelper(ApplicationLifetime);
            RxApp.SuspensionHost.CreateNewAppState = () => new MainWindowViewModel();
            RxApp.SuspensionHost.SetupDefaultSuspendResume(new NewtonsoftJsonSuspensionDriver("appstate.json"));
            suspension.OnFrameworkInitializationCompleted();
            
            Locator.CurrentMutable.RegisterConstant<IScreen>(RxApp.SuspensionHost.GetAppState<MainWindowViewModel>());
            Locator.CurrentMutable.UseSerilogFullLogger();
            Locator.CurrentMutable.Register<IViewFor<MinerViewModel>>(() => new MinerView());
            base.OnFrameworkInitializationCompleted();
        }
    }
}