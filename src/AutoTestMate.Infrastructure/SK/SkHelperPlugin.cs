using System.ComponentModel;
using System.Text.Json;
using AutoTestMate.Application.Abstractions;
using AutoTestMate.Domain;
using Microsoft.SemanticKernel;

namespace AutoTestMate.Infrastructure.SK;

public sealed class SkHelperPlugin
{
    private readonly IFlowPublisher? _flow; // optional: works even if null

    public SkHelperPlugin(IFlowPublisher? flow = null)
    {
        _flow = flow;
    }

    private async Task PublishAsync(string msg, object? data = null)
    {
        if (_flow is null) return; // keep plugin usable without a publisher
        string? payload = null;
        try
        {
            if (data is string s) payload = Trunc(s, 1000);
            else if (data is not null) payload = Trunc(JsonSerializer.Serialize(data), 1000);
        }
        catch { /* best-effort */ }

        // We log under GenerateTests stage so it aligns with your dashboard grouping.
        await _flow.PublishAsync(
            new FlowEvent(FlowStage.GenerateTests, msg, DateTimeOffset.UtcNow, payload)
        );
    }

    private static string Trunc(string s, int max)
        => s.Length <= max ? s : s[..max] + " …";

    // ------------------ Exposed Tool/Plugin functions ------------------

    [KernelFunction, Description("Suggest common int edge cases as CSV")]
    public async Task<string> IntEdgeCasesCsv(string paramNamesCsv)
    {
        await PublishAsync("Tool:Helper.IntEdgeCasesCsv.Start", new { paramNamesCsv });
        var csv = "-1,0,1,2,int.MaxValue,int.MinValue";
        await PublishAsync("Tool:Helper.IntEdgeCasesCsv.Done", new { result = csv });
        return csv;
    }

    [KernelFunction, Description("Return safe sample args CSV for primitive parameters.")]
    public async Task<string> SafeSampleArgsCsv(string paramTypesCsv)
    {
        await PublishAsync("Tool:Helper.SafeSampleArgsCsv.Start", new { paramTypesCsv });

        var types = paramTypesCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var values = types.Select(t =>
            t.Contains("int", StringComparison.OrdinalIgnoreCase) ? "0" :
            t.Contains("long", StringComparison.OrdinalIgnoreCase) ? "0" :
            t.Contains("double", StringComparison.OrdinalIgnoreCase) ? "0" :
            t.Contains("float", StringComparison.OrdinalIgnoreCase) ? "0" :
            t.Contains("decimal", StringComparison.OrdinalIgnoreCase) ? "0" :
            t.Contains("bool", StringComparison.OrdinalIgnoreCase) ? "false" :
            t.Contains("string", StringComparison.OrdinalIgnoreCase) ? "\"\"" : "default"
        );
        var csv = string.Join(',', values);

        await PublishAsync("Tool:Helper.SafeSampleArgsCsv.Done", new { result = csv });
        return csv;
    }

    [KernelFunction, Description("Return string edge cases as CSV")]
    public async Task<string> StringEdgeCasesCsv()
    {
        await PublishAsync("Tool:Helper.StringEdgeCasesCsv.Start");
        var csv = "\"\", \" \", \"hello\", \"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"";
        await PublishAsync("Tool:Helper.StringEdgeCasesCsv.Done", new { result = csv });
        return csv;
    }

    [KernelFunction, Description("Build a minimal JSON array of argument objects from param names and a CSV of values")]
    public async Task<string> BuildArgMatrixJson(string paramNamesCsv, string valuesCsv)
    {
        await PublishAsync("Tool:Helper.BuildArgMatrixJson.Start", new { paramNamesCsv, valuesCsv });

        var names = paramNamesCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var vals  = valuesCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var row = new Dictionary<string, object?>();
        foreach (var n in names) row[n] = vals.FirstOrDefault() ?? "default";
        var json = JsonSerializer.Serialize(new[] { row });

        await PublishAsync("Tool:Helper.BuildArgMatrixJson.Done", new { result = json });
        return json;
    }
}
