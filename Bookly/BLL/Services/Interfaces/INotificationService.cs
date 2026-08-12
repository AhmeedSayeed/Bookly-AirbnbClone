using BLL.DTOs;
using BLL.ViewModels.Notifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Services.Interfaces
{
    public interface INotificationService
    {
        Task<Response<bool>> SendNotificationAsync(int userId, string message, string? link = null);
        Task<Response<NotificationsViewModel>> GetForUserAsync(int userId, int pageNumber, int pageSize);
        Task<Response<int>> GetUnreadCountAsync(int userId);
        Task<Response<bool>> MarkAsReadAsync(int notificationId, int userId);
        Task<Response<bool>> MarkAllAsReadAsync(int userId);
    }
}
