using ReactiveUI;

namespace Miner.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IScreen
    {
        public RoutingState Router { get; }
        
        public MainWindowViewModel()
        {
            Router = new RoutingState();
            Router.Navigate.Execute(new MinerViewModel(Router));
        }
    }
}