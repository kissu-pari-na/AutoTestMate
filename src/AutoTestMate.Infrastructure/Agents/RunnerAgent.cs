using AutoTestMate.Application.Agents;
using AutoTestMate.Application.Abstractions;
using AutoTestMate.Domain;

namespace AutoTestMate.Infrastructure.Agents;

public sealed class RunnerAgent : IAgent
{
    public string Name => "Runner";
    private readonly ITestRunner _runner;
    private readonly IFlowPublisher _flow;

    public RunnerAgent(ITestRunner runner, IFlowPublisher flow) { _runner = runner; _flow = flow; }

    public bool CanHandle(Workspace ws) => ws.Generated is not null && ws.Results is null;

    public async Task HandleAsync(Workspace ws, CancellationToken ct = default)
    {
        await _flow.PublishAsync(new FlowEvent(FlowStage.RunTests, "Agent:Runner.Start", DateTimeOffset.UtcNow));
        ws.Results = await _runner.RunAsync(ct);
        ws.CompletedStages.Add(Name);
        await _flow.PublishAsync(new FlowEvent(FlowStage.Summarize, "Agent:Runner.Done", DateTimeOffset.UtcNow,
            $"Passed={ws.Results?.Passed}, Failed={ws.Results?.Failed}"));
        ws.IsDone = ws.Results?.Failed == 0;
    }
}