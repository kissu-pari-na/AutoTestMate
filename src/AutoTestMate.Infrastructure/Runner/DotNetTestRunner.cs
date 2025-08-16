using System.Diagnostics;
using System.Text.RegularExpressions;
using AutoTestMate.Application.Abstractions;
using AutoTestMate.Domain;

namespace AutoTestMate.Infrastructure.Runner;

public sealed class DotNetTestRunner : ITestRunner
{
    private readonly string _testsProjectDir;
    private readonly IFlowPublisher _flow;
    public DotNetTestRunner(string testsProjectDir, IFlowPublisher flow) { _testsProjectDir = testsProjectDir; _flow = flow; }

    public async Task<TestRunResult> RunAsync(CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo("dotnet", "test --nologo -v minimal")
        {
            WorkingDirectory = _testsProjectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding  = System.Text.Encoding.UTF8
        };
        var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        int passed = 0, failed = 0;
        var rxStatus = new Regex("^(Passed|Failed|Skipped)\\s+(.+?)(?:\\s+\\[.+\\])?$", RegexOptions.Compiled);

        p.OutputDataReceived += async (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data)) return;
            var line = e.Data.Trim();
            var m = rxStatus.Match(line);
            if (m.Success)
            {
                var status = m.Groups[1].Value;
                var testName = m.Groups[2].Value;
                if (status == "Passed") { passed++; await _flow.PublishAsync(new FlowEvent(FlowStage.RunTests, "Tool:dotnet.test.Pass", DateTimeOffset.UtcNow, testName)); }
                else if (status == "Failed") { failed++; await _flow.PublishAsync(new FlowEvent(FlowStage.RunTests, "Tool:dotnet.test.Fail", DateTimeOffset.UtcNow, testName)); }
                else { await _flow.PublishAsync(new FlowEvent(FlowStage.RunTests, "Tool:dotnet.test.Skipped", DateTimeOffset.UtcNow, testName)); }
            }
            else
            {
                await _flow.PublishAsync(new FlowEvent(FlowStage.RunTests, "Tool:dotnet.test.Log", DateTimeOffset.UtcNow, line));
            }
        };
        p.ErrorDataReceived += async (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) await _flow.PublishAsync(new FlowEvent(FlowStage.RunTests, "Tool:dotnet.test.Err", DateTimeOffset.UtcNow, e.Data.Trim())); };

        p.Start(); p.BeginOutputReadLine(); p.BeginErrorReadLine();
        await p.WaitForExitAsync(ct);

        var raw = $"ExitCode={p.ExitCode}";
        var success = failed == 0 && p.ExitCode == 0;
        return new TestRunResult(success, passed, failed, raw);
    }
}