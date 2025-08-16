using AutoTestMate.Domain;
using Microsoft.AspNetCore.SignalR;

namespace AutoTestMate.Presentation.Web.Hubs;

public class FlowHub : Hub
{
    public async Task Send(FlowEvent evt) => await Clients.All.SendAsync("flowEvent", evt);
}
