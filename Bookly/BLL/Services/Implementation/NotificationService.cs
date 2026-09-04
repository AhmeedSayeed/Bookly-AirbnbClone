using BLL.DTOs;
using BLL.Hubs;
using BLL.Services.Interfaces;
using BLL.ViewModels.Common;
using BLL.ViewModels.Notifications;
using DAL.Models.Interactions;
using DAL.Repository.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BLL.Services.Implementation
{
    public class NotificationService : INotificationService
    {
        private readonly IRepository<Notification> _notificationRepo;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            IRepository<Notification> notificationRepo,
            IHubContext<NotificationHub> hubContext)
        {
            _notificationRepo = notificationRepo;
            _hubContext = hubContext;
        }

        public async Task<Response<bool>> SendNotificationAsync(
            int userId,
            string messageKey,
            string[]? args = null,
            string? link = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                MessageKey = messageKey,
                MessageArgsJson = args == null ? null : JsonSerializer.Serialize(args),
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepo.AddAsync(notification);
            await _notificationRepo.SaveAsync();

            var viewModel = new NotificationViewModel
            {
                Id = notification.Id,
                MessageKey = notification.MessageKey,
                MessageArgsJson = notification.MessageArgsJson,
                LegacyMessage = notification.Message,
                Link = notification.Link,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };

            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("ReceiveNotification", viewModel);

            return Response<bool>.Success(true);
        }

        public async Task<Response<NotificationsViewModel>> GetForUserAsync(
            int userId,
            int pageNumber,
            int pageSize)
        {
            var pagedResult = await _notificationRepo.GetAllPaginatedAsync(
                selector: n => new NotificationViewModel
                {
                    Id = n.Id,
                    MessageKey = n.MessageKey,
                    MessageArgsJson = n.MessageArgsJson,
                    LegacyMessage = n.Message,
                    Link = n.Link,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                },
                pageNumber: pageNumber,
                pageSize: pageSize,
                filter: n => n.UserId == userId,
                orderBy: q => q.OrderByDescending(n => n.CreatedAt)
            );

            var unreadCount = await _notificationRepo.Count(
                n => n.UserId == userId && !n.IsRead);

            var vm = new NotificationsViewModel
            {
                Notifications = pagedResult.Items.ToList(),
                UnreadCount = unreadCount,
                PageInfo = new PageInfoViewModel
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = pagedResult.TotalCount
                }
            };

            return Response<NotificationsViewModel>.Success(vm);
        }

        public async Task<Response<int>> GetUnreadCountAsync(int userId)
        {
            var count = await _notificationRepo.Count(
                n => n.UserId == userId && !n.IsRead);

            return Response<int>.Success(count);
        }

        public async Task<Response<bool>> MarkAsReadAsync(
            int notificationId,
            int userId)
        {
            var notification = await _notificationRepo.GetAsync(
                selector: n => n,
                filter: n => n.Id == notificationId && n.UserId == userId
            );

            if (notification == null)
                return Response<bool>.FailWithKey(
                    ResponseStatus.NotFound,
                    "NotificationNotFound");

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _notificationRepo.Update(notification);
                await _notificationRepo.SaveAsync();
            }

            return Response<bool>.Success(true);
        }

        public async Task<Response<bool>> MarkAllAsReadAsync(int userId)
        {
            var unread = await _notificationRepo.GetAllAsync(
                selector: n => n,
                filter: n => n.UserId == userId && !n.IsRead
            );

            if (!unread.Any())
                return Response<bool>.Success(true);

            foreach (var notification in unread)
            {
                notification.IsRead = true;
                _notificationRepo.Update(notification);
            }

            await _notificationRepo.SaveAsync();

            return Response<bool>.Success(true);
        }
    }
}