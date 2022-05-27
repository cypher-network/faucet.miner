using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Miner.ViewModels;

namespace Miner.Views;

public partial class MinerView : ReactiveUserControl<MinerViewModel>
{
    public MinerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}