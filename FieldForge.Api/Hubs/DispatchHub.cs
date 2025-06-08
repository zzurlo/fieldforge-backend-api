using Microsoft.AspNetCore.SignalR;

namespace FieldForge.Api.Hubs
{
    public class DispatchHub : Hub
    {
        public async Task SendAssignmentUpdate(Guid serviceOrderId, List<string> technicianUserIds)
        {
            await Clients.Users(technicianUserIds)
                .SendAsync("ReceiveAssignmentUpdate", serviceOrderId);
        }
    }
}