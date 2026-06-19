using Microsoft.AspNetCore.SignalR;

namespace LeadCapture.Hubs;

public class LeadHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
