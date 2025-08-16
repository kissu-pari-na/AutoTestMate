using AutoTestMate.Application.Agents;
using AutoTestMate.Application.Abstractions;
using AutoTestMate.Domain;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using AutoTestMate.Infrastructure.SK;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTestMate.Infrastructure.Agents;

public sealed class TestDesignerAgent : IAgent
{
    public string Name => "Designer";
    private readonly Kernel _kernel;
    private readonly IFlowPublisher _flow;
    private readonly KernelPlugin _helperPlugin;

    public TestDesignerAgent(IFlowPublisher flow)
    {
        _flow = flow;
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY") ?? throw new InvalidOperationException("Set GOOGLE_AI_API_KEY");
        var b = Kernel.CreateBuilder();
        b.AddGoogleAIGeminiChatCompletion(modelId: "gemini-1.5-flash", apiKey: apiKey);

        // publish prompt & function invocation (so the dashboard sees calls)
        b.Services.AddSingleton<IPromptRenderFilter>(sp => new PublishingPromptFilter(_flow));
        b.Services.AddSingleton<IFunctionInvocationFilter>(sp => new PublishingFunctionFilter(_flow));

        _kernel = b.Build();
        _helperPlugin = _kernel.Plugins.AddFromObject(new SkHelperPlugin(), "Helper");

        // (Optional) Prove tools are registered
    var tools = _kernel.Plugins.SelectMany(p => p).Select(f => $"{f.PluginName}.{f.Name}");
    _ = _flow.PublishAsync(new FlowEvent(
        FlowStage.PlanTests, "Agent:Tools.Registered", DateTimeOffset.UtcNow,
        string.Join(", ", tools)));
    }

    public bool CanHandle(Workspace ws) => ws.Parsed is not null && ws.Plan is null;

    public async Task HandleAsync(Workspace ws, CancellationToken ct = default)
    {
        await _flow.PublishAsync(new FlowEvent(FlowStage.PlanTests, "Agent:Designer.Start", DateTimeOffset.UtcNow));

        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var settings = new GeminiPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Required(
               _helperPlugin.ToList()
            ), Temperature = 0.2 };
        var p = ws.Parsed!;
        var paramTypesCsv = string.Join(',', p.Parameters.Select(x => x.Type));
        var history = new ChatHistory();
        history.AddSystemMessage("""
You are a senior test designer. 
Return lines starting with 'CASE:' (no code). 
MANDATE: Before returning cases, you MUST call at least one Helper.* function 
to decide representative inputs (e.g., SafeSampleArgsCsv, IntEdgeCasesCsv, StringEdgeCasesCsv).
""");
        history.AddUserMessage($"Method: {p.ReturnType} {p.MethodName}({string.Join(", ", p.Parameters.Select(x => x.Type + " " + x.Name))})");
        // This line primes the model with a concrete call target:
        history.AddUserMessage($"ParamTypesCsv: {paramTypesCsv}");
        // Optional: suggest an obvious function to call:
        history.AddUserMessage("""Must call Helper.SafeSampleArgsCsv(ParamTypesCsv) first.""");

        await _flow.PublishAsync(new FlowEvent(FlowStage.PlanTests, "SK:LLM.Request", DateTimeOffset.UtcNow));
        var resp = await chat.GetChatMessageContentAsync(history, executionSettings: settings, kernel: _kernel, cancellationToken: ct);
        await _flow.PublishAsync(new FlowEvent(FlowStage.GenerateTests, "SK:LLM.Response.Final", DateTimeOffset.UtcNow));
        var lines = (resp.Content ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                         .Where(l => l.StartsWith("CASE:", StringComparison.OrdinalIgnoreCase)).ToList();

        ws.Plan = new TestPlan(lines, "Designed via LLM + Helper tools");
        ws.CompletedStages.Add(Name);
        await _flow.PublishAsync(new FlowEvent(FlowStage.PlanTests, "Agent:Designer.Done", DateTimeOffset.UtcNow, string.Join("\n", lines)));
    }
}