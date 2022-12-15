using System;
using Avalonia.Controls.Notifications;

namespace Miner.ViewModels;

public class NotificationModel: INotification
{

    public string Title { get; set; }
    public string Message { get; set; }

    public NotificationType Type { get; set; }

    public TimeSpan Expiration { get; set; }
    public Action OnClick { get; }
    public Action OnClose { get; }
    
}