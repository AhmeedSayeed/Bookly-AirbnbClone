using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BLL.Hubs
{
    // The Authorize attribute ensures the Context.User is populated via the JWT cookie
    [Authorize]
    public class NotificationHub : Hub
    {
        // No manual connection dictionaries here. 
        // SignalR maps the ClaimTypes.NameIdentifier to the connection automatically.
    }
}