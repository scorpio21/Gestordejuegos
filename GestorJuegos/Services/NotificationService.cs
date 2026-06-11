using System;
using System.Collections.Generic;
using System.Timers;

namespace GestorJuegos.Services;

public class NotificationService
{
    public event EventHandler<NotificationEventArgs>? NotificationReceived;

    public void Show(string message, string title = "Notificación", NotificationType type = NotificationType.Info)
    {
        NotificationReceived?.Invoke(this, new NotificationEventArgs(message, title, type));
    }

    public void Success(string message, string title = "Éxito") => Show(message, title, NotificationType.Success);
    public void Error(string message, string title = "Error") => Show(message, title, NotificationType.Error);
    public void Warning(string message, string title = "Advertencia") => Show(message, title, NotificationType.Warning);
    public void Info(string message, string title = "Información") => Show(message, title, NotificationType.Info);
}

public class NotificationEventArgs : EventArgs
{
    public string Message { get; }
    public string Title { get; }
    public NotificationType Type { get; }

    public NotificationEventArgs(string message, string title, NotificationType type)
    {
        Message = message;
        Title = title;
        Type = type;
    }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}
