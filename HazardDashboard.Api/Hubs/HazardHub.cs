using Microsoft.AspNetCore.SignalR;

namespace HazardDashboard.Api.Hubs;

public sealed class HazardHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Groups are optional here; a future map viewport could subscribe clients
        // to regional groups without changing the connection protocol.
        await base.OnConnectedAsync();
    }
}
