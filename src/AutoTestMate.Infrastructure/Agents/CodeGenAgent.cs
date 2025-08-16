using AutoTestMate.Application.Agents;
using AutoTestMate.Application.Abstractions;
using AutoTestMate.Domain;

namespace AutoTestMate.Infrastructure.Agents;

public sealed class CodeGenAgent : IAgent
{
    public string Name => "CodeGen";
    private readonly ITestGenerationService _gen;
    private readonly ISourceUnderTestWriter _sutWriter;
    private readonly ITestWriter _writer;
    private readonly IFlowPublisher _flow;

    public CodeGenAgent(ITestGenerationService gen, ISourceUnderTestWriter sutWriter, ITestWriter writer, IFlowPublisher flow)
    {
        _gen = gen; _sutWriter = sutWriter; _writer = writer; _flow = flow;
    }

    public bool CanHandle(Workspace ws) => ws.Parsed is not null && ws.Plan is not null && ws.Generated is null;

    public async Task HandleAsync(Workspace ws, CancellationToken ct = default)
    {
        await _flow.PublishAsync(new FlowEvent(FlowStage.GenerateTests, "Agent:CodeGen.Start", DateTimeOffset.UtcNow));
        var parsed = ws.Parsed!;
        var (ns, cls, _) = await _sutWriter.WriteAsync(parsed, ct);
        var generated = await _gen.GenerateAsync(parsed with { DeclaredNamespace = ns, DeclaredClass = cls }, ct);
        var outPath = await _writer.WriteAsync(generated, ct);
        ws.Generated = generated;
        ws.CompletedStages.Add(Name);
        await _flow.PublishAsync(new FlowEvent(FlowStage.WriteFiles, "Agent:CodeGen.Done", DateTimeOffset.UtcNow, outPath));
    }
}