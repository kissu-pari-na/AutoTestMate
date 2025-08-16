using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace AutoTestMate.Infrastructure.SK;

public sealed class SkHelperPlugin
{
    [KernelFunction, Description("Suggest common int edge cases as CSV")]
    public string IntEdgeCasesCsv(string paramNamesCsv) => "-1,0,1,2,int.MaxValue,int.MinValue";

    [KernelFunction, Description("Return safe sample args CSV for primitive parameters.")]
    public string SafeSampleArgsCsv(string paramTypesCsv)
    {
        var types = paramTypesCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return string.Join(',', types.Select(t =>
            t.Contains("int") ? "0" :
            t.Contains("long") ? "0" :
            t.Contains("double") ? "0" :
            t.Contains("float") ? "0" :
            t.Contains("decimal") ? "0" :
            t.Contains("bool") ? "false" :
            t.Contains("string") ? "\"\"" : "default"));
    }

    [KernelFunction, Description("Return string edge cases as CSV")]
    public string StringEdgeCasesCsv() => "\"\", \" \", \"Hello\", \"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"";

    [KernelFunction, Description("Build a minimal JSON array of argument objects")]
    public string BuildArgMatrixJson(string paramNamesCsv, string valuesCsv)
    {
        var names = paramNamesCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var vals  = valuesCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var row = new Dictionary<string, object?>();
        foreach (var n in names) row[n] = vals.FirstOrDefault() ?? "default";
        return JsonSerializer.Serialize(new[] { row });
    }
}