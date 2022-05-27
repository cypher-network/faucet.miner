using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Miner.ViewModels;
using ReactiveUI;

namespace Miner.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}