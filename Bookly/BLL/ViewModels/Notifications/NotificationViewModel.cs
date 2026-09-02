using System.Text.Json;

namespace BLL.ViewModels.Notifications;

public class NotificationViewModel
{
    public int Id { get; set; }

    public string? MessageKey { get; set; }

    public string? MessageArgsJson { get; set; }

    public string? LegacyMessage { get; set; }

    public string? Link { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public string[] Args =>
        string.IsNullOrWhiteSpace(MessageArgsJson)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(MessageArgsJson) ?? Array.Empty<string>();
   
}