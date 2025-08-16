using AutoTestMate.Application.Abstractions;
using AutoTestMate.Domain;
using Microsoft.AspNetCore.SignalR.Client;

namespace AutoTestMate.Infrastructure.Flow;

public class SignalRFlowPublisher : IFlowPublisher
{
    private readonly HubConnection _hub;
    public SignalRFlowPublisher(string hubUrl)
    {
        _hub = new HubConnectionBuilder().WithUrl(hubUrl).WithAutomaticReconnect().Build();
    }
    public async Task EnsureConnectedAsync(CancellationToken ct = default)
    {
        if (_hub.State != HubConnectionState.Connected) await _hub.StartAsync(ct);
    }
    public async Task PublishAsync(FlowEvent evt, CancellationToken ct = default)
    {
        await EnsureConnectedAsync(ct);
        await _hub.SendAsync("Send", evt, ct);
    }
    public async ValueTask DisposeAsync() => await _hub.DisposeAsync();
}
