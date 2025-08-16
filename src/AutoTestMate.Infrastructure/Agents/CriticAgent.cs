using AutoTestMate.Application.Agents;
using AutoTestMate.Application.Abstractions;
using AutoTestMate.Domain;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AutoTestMate.Infrastructure.Agents;

public sealed class CriticAgent : IAgent
{
    public string Name => "Critic";
    private readonly IFlowPublisher _flow;
    private readonly Kernel _kernel;
    private readonly int _maxRetries;

    public CriticAgent(IFlowPublisher flow, int maxRetries = 2)
    {
        _flow = flow;
        _maxRetries = maxRetries;
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY") ?? throw new InvalidOperationException("Set GOOGLE_AI_API_KEY");
        var b = Kernel.CreateBuilder();
        b.AddGoogleAIGeminiChatCompletion(modelId: "gemini-1.5-flash", apiKey: apiKey);
        _kernel = b.Build();
    }

    public bool CanHandle(Workspace ws) => ws.Results is { Failed: > 0 } && ws.RetryCount < _maxRetries;

    public async Task HandleAsync(Workspace ws, CancellationToken ct = default)
    {
        await _flow.PublishAsync(new FlowEvent(FlowStage.PlanTests, "Agent:Critic.Start", DateTimeOffset.UtcNow,
            $"Failed={ws.Results?.Failed}, Retry={ws.RetryCount + 1}/{_maxRetries}"));

        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var p = ws.Parsed!;
        var existing = ws.Plan?.Cases ?? Array.Empty<string>();

        var sys = "You are a test critic. Propose 2-4 NEW lines starting with 'CASE:' to likely expose bugs.";
        var user = $"METHOD: {p.ReturnType} {p.MethodName}({string.Join(", ", p.Parameters.Select(x => x.Type + " " + x.Name))})\nEXISTING CASES:\n{string.Join("\n", existing)}";

        var history = new ChatHistory(); history.AddSystemMessage(sys); history.AddUserMessage(user);
        var resp = await chat.GetChatMessageContentAsync(history, kernel: _kernel, cancellationToken: ct);
        var newLines = (resp.Content ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.StartsWith("CASE:", StringComparison.OrdinalIgnoreCase))
            .Except(existing, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var merged = existing.Concat(newLines).ToList();
        ws.Plan = new TestPlan(merged, $"Augmented by Critic (retry {ws.RetryCount + 1})");
        ws.Generated = null; ws.Results = null; ws.RetryCount++;
        await _flow.PublishAsync(new FlowEvent(FlowStage.PlanTests, "Agent:Critic.Done", DateTimeOffset.UtcNow, string.Join("\n", newLines)));
    }
}