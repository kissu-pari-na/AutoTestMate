using AutoTestMate.Application.Abstractions;
using AutoTestMate.Domain;
using Microsoft.SemanticKernel;

namespace AutoTestMate.Infrastructure.SK;

public sealed class PublishingPromptFilter : IPromptRenderFilter
{
    private readonly IFlowPublisher _flow;
    public PublishingPromptFilter(IFlowPublisher flow) => _flow = flow;

    public async Task OnPromptRenderAsync(PromptRenderContext ctx, Func<PromptRenderContext, Task> next)
    {
        await _flow.PublishAsync(new FlowEvent(FlowStage.GenerateTests, "SK:Prompt.Render.Start", DateTimeOffset.UtcNow, ctx.Function?.Name));
        await next(ctx);
        var preview = (ctx.RenderedPrompt ?? string.Empty);
        if (preview.Length > 400) preview = preview[..400] + " â€¦";
        await _flow.PublishAsync(new FlowEvent(FlowStage.GenerateTests, "SK:Prompt.Render.Done", DateTimeOffset.UtcNow, preview));
    }
}