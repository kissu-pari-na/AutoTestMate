using System.Text.Json;
using AutoTestMate.Application.Abstractions;
using AutoTestMate.Domain;
using Microsoft.SemanticKernel;

namespace AutoTestMate.Infrastructure.SK;

public sealed class PublishingFunctionFilter : IFunctionInvocationFilter
{
    private readonly IFlowPublisher _flow;
    public PublishingFunctionFilter(IFlowPublisher flow) => _flow = flow;

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext ctx, Func<FunctionInvocationContext, Task> next)
    {
        await _flow.PublishAsync(new FlowEvent(FlowStage.GenerateTests, $"Func:Call {ctx.Function.PluginName}.{ctx.Function.Name}", DateTimeOffset.UtcNow));
        try
        {
            var args = ctx.Arguments?.ToDictionary(x => x.Key, x => x.Value);
            var argsStr = args is null ? "(no args)" : JsonSerializer.Serialize(args);
            if (argsStr.Length > 800) argsStr = argsStr[..800] + " â€¦";
            await _flow.PublishAsync(new FlowEvent(FlowStage.GenerateTests, "Func:Args", DateTimeOffset.UtcNow, argsStr));
        }
        catch {}
        await next(ctx);
        try
        {
            var resultStr = ctx.Result is null ? "null" : ctx.Result.ToString()!;
            if (resultStr.Length > 800) resultStr = resultStr[..800] + " â€¦";
            await _flow.PublishAsync(new FlowEvent(FlowStage.GenerateTests, "Func:Result", DateTimeOffset.UtcNow, resultStr));
        }
        catch {}
    }
}