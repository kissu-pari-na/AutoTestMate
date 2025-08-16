using System;
using AutoTestMate.Application.Abstractions;
using AutoTestMate.Domain;

namespace AutoTestMate.Application.Agents;

public class Orchestrator
{
    private readonly IEnumerable<IAgent> _agents;
    private readonly IFlowPublisher _flow;

    public Orchestrator(IEnumerable<IAgent> agents, IFlowPublisher flow) { _agents = agents; _flow = flow; }

    public async Task RunAsync(Workspace ws, CancellationToken ct = default)
    {
        if (ws is null)
        {
            Console.WriteLine("Workspace cannot be null.");
            throw new ArgumentNullException(nameof(ws));
        }
        if (_flow is null)
        {
            Console.WriteLine("Flow publisher cannot be null.");
            throw new ArgumentNullException(nameof(_flow));
        }

        await _flow.PublishAsync(new FlowEvent(FlowStage.UserInput, "Orchestrator:Start", DateTimeOffset.UtcNow));
        int safety = 0;
        while (!ws.IsDone && safety++ < 50)
        {
            if (_agents is null || !_agents.Any())
            {
                await _flow.PublishAsync(new FlowEvent(FlowStage.UserInput, "No agents available to handle the workspace.", DateTimeOffset.UtcNow));
                ws.IsDone = true;
                break;
            }
            var next = _agents.FirstOrDefault(a => a.CanHandle(ws));
            if (next is null) { ws.IsDone = true; break; }
            await _flow.PublishAsync(new FlowEvent(FlowStage.PlanTests, $"Agent.Dispatch:{next.Name}", DateTimeOffset.UtcNow));
            await next.HandleAsync(ws, ct);
        }
        await _flow.PublishAsync(new FlowEvent(FlowStage.Summarize, "Orchestrator:Stop", DateTimeOffset.UtcNow));
    }
}
