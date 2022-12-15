using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Miner.ViewModels;
using ReactiveUI;

namespace Miner.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private static WindowNotificationManager _notificationManager;
    public MainWindow()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);

        _notificationManager = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.TopRight,
            MaxItems = 2
        };
    }

    public static void ShowNotification(string title, string msg, NotificationType notificationType)
    {
        _notificationManager.Show(new NotificationModel
        {
            Title = title,
            Message = msg,
            Type = notificationType
        });
    }
}