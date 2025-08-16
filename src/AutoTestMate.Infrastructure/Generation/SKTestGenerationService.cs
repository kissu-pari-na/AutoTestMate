using System.Text;
using AutoTestMate.Application.Abstractions;
using AutoTestMate.Infrastructure.SK;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;


namespace AutoTestMate.Infrastructure.Generation;

public sealed class SKTestGenerationService : ITestGenerationService
{
    private readonly Kernel _kernel;
    private readonly IFlowPublisher _flow;

    public SKTestGenerationService(IFlowPublisher flow)
    {
        _flow = flow;
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY") ?? throw new InvalidOperationException("Set GOOGLE_AI_API_KEY");
        Console.WriteLine($"Using Google API key: {apiKey.Substring(0,7)}..."); // Log the first few characters for debugging
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("GOOGLE_AI_API_KEY environment variable is not set.");
        var b = Kernel.CreateBuilder();
        b.AddGoogleAIGeminiChatCompletion(modelId: "gemini-1.5-flash", apiKey: apiKey);
        b.Services.AddSingleton<IPromptRenderFilter>(sp => new PublishingPromptFilter(_flow));
        b.Services.AddSingleton<IFunctionInvocationFilter>(sp => new PublishingFunctionFilter(_flow));
        _kernel = b.Build();
        _kernel.Plugins.AddFromObject(new SkHelperPlugin(), "Helper");
    }

    public async Task<GeneratedTests> GenerateAsync(ParsedCode input, CancellationToken ct = default)
    {
        var chat = _kernel.GetRequiredService<IChatCompletionService>();

        var sys = """
You are an expert C# test engineer. Produce a COMPLETE compilable xUnit test file.
Rules:
- Namespace: AutoTestMate.SampleUnderTest.Tests
- Include: using Xunit;
- Test class name: <MethodName>Tests
- Use [Theory]/[InlineData] for primitives where sensible; else [Fact].
- Include at least 3 tests (include error paths if obvious).
- Use Assert.Throws when exceptions are expected.
- MANDATE: Before you choose test inputs, you MUST call at least one Helper.* function 
  (e.g., Helper.SafeSampleArgsCsv or Helper.IntEdgeCasesCsv). Use the results to decide InlineData/inputs.
Return only one C# file inside ```csharp fences.
""";
        var paramTypes = string.Join(',', input.Parameters.Select(p => p.Type));
        var paramDecls = string.Join(", ", input.Parameters.Select(p => $"{p.Type} {p.Name}"));
        var user = $"""
SOURCE-UNDER-TEST SIGNATURE:
{input.Signature}

PARSED:
- Namespace: {input.DeclaredNamespace ?? "AutoTestMate.Snippets"}
- Class: {input.DeclaredClass ?? "SnippetClass"}
- Method: {input.MethodName}
- ReturnType: {input.ReturnType}
- IsStatic: {input.IsStatic}
- Parameters: {paramDecls}

You may call: Helper.SafeSampleArgsCsv("{paramTypes}")
OUTPUT: one C# file fenced as ```csharp â€¦ ```.
""";
        var history = new ChatHistory();
        history.AddSystemMessage(sys);
        history.AddUserMessage(user);

        await _flow.PublishAsync(new AutoTestMate.Domain.FlowEvent(AutoTestMate.Domain.FlowStage.GenerateTests, "SK:LLM.Request", DateTimeOffset.UtcNow));
        var settings = new GeminiPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };

        var sb = new StringBuilder();
        await foreach (var update in chat.GetStreamingChatMessageContentsAsync(history, executionSettings: settings, kernel: _kernel, cancellationToken: ct))
        {
            foreach (var t in update.Items.OfType<StreamingTextContent>())
            {
                if (!string.IsNullOrEmpty(t.Text))
                {
                    sb.Append(t.Text);
                    await _flow.PublishAsync(new AutoTestMate.Domain.FlowEvent(AutoTestMate.Domain.FlowStage.GenerateTests, "SK:Stream.Text", DateTimeOffset.UtcNow, t.Text));
                }
            }
            foreach (var f in update.Items.OfType<StreamingFunctionCallUpdateContent>())
            {
                var frag = f.Arguments ?? "";
                if (frag.Length > 200) frag = frag[..200] + " â€¦";
                await _flow.PublishAsync(new AutoTestMate.Domain.FlowEvent(AutoTestMate.Domain.FlowStage.GenerateTests, $"Func:Call {f.Name}", DateTimeOffset.UtcNow, frag));
            }
        }
        await _flow.PublishAsync(new AutoTestMate.Domain.FlowEvent(AutoTestMate.Domain.FlowStage.GenerateTests, "SK:LLM.Response.Final", DateTimeOffset.UtcNow));

        var content = sb.ToString();
        int start = content.IndexOf("```csharp");
        int end   = content.LastIndexOf("```");
        string testSource = (start >= 0 && end > start)
            ? content.Substring(start + "```csharp".Length, end - (start + "```csharp".Length)).Trim()
            : content;

        if (!testSource.Contains("using Xunit"))
            testSource = "using Xunit;\n" + testSource;
        if (!testSource.Contains("namespace AutoTestMate.SampleUnderTest.Tests"))
            testSource = "namespace AutoTestMate.SampleUnderTest.Tests;\n\n" + testSource;

        var fileName = $"{input.MethodName}Tests.cs";
        return new GeneratedTests(fileName, testSource);
    }
}