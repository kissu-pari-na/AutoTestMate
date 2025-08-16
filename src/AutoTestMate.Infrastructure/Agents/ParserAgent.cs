using AutoTestMate.Application.Abstractions;
using AutoTestMate.Application.Agents;
using AutoTestMate.Domain;

namespace AutoTestMate.Infrastructure.Agents;

public sealed class ParserAgent : IAgent
{
    private readonly ICodeParser _parser;
    private readonly IFlowPublisher _flow;
    public string Name => "Parser";

    public ParserAgent(ICodeParser parser, IFlowPublisher flow) { _parser = parser; _flow = flow; }

    public bool CanHandle(Workspace ws) => ws.Parsed is null && !string.IsNullOrWhiteSpace(ws.CodeSnippet);

    public async Task HandleAsync(Workspace ws, CancellationToken ct = default)
    {
        await _flow.PublishAsync(new FlowEvent(FlowStage.ParseCode, "Agent:Parser.Start", DateTimeOffset.UtcNow));
        ws.Parsed = await _parser.ParseAsync(ws.CodeSnippet, ct);
        ws.CompletedStages.Add(Name);
        await _flow.PublishAsync(new FlowEvent(FlowStage.ParseCode, "Agent:Parser.Done", DateTimeOffset.UtcNow));
    }
}