using AutoTestMate.Application.Abstractions;

namespace AutoTestMate.Application.Agents;

public interface IAgent
{
    string Name { get; }
    bool CanHandle(Workspace ws);
    Task HandleAsync(Workspace ws, CancellationToken ct = default);
}

public sealed class Workspace
{
    public string CodeSnippet { get; set; } = "";
    public ParsedCode? Parsed { get; set; }
    public TestPlan? Plan { get; set; }
    public GeneratedTests? Generated { get; set; }
    public TestRunResult? Results { get; set; }
    public List<string> Notes { get; } = new();
    public bool IsDone { get; set; }
    public HashSet<string> CompletedStages { get; } = new();
    public int RetryCount { get; set; } = 0;
}

public sealed record TestPlan(IReadOnlyList<string> Cases, string? Rationale = null);
