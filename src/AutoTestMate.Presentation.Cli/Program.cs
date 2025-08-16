using AutoTestMate.Application.Abstractions;
using AutoTestMate.Application.Agents;
using AutoTestMate.Infrastructure.Flow;

var webHubUrl = "http://localhost:5173/flow";
var testsDir  = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../tests/AutoTestMate.SampleUnderTest.Tests"));

IFlowPublisher publisher          = new SignalRFlowPublisher(webHubUrl);
ICodeParser parser                = null; // Initialize with a real implementation, e.g., RoslynCodeParser();
ISourceUnderTestWriter srcWriter  = null; // Initialize with a real implementation, e.g., FileSystemSourceWriter(testsDir);
ITestGenerationService gen        = null; // Initialize with a real implementation, e.g., RoslynTestGenerator();
ITestWriter writer                = null; // Initialize with a real implementation, e.g., FileSystemTestWriter(testsDir);
ITestRunner runner                = null; // Initialize with a real implementation, e.g., NUnitTestRunner(testsDir);

var agents = new List<IAgent>();

var orch = new Orchestrator(agents, publisher);

Console.WriteLine("Paste a C# method (end with a single line containing only 'END'):");
string? line; var code = "";
while (!string.Equals(line = Console.ReadLine(), "END", StringComparison.OrdinalIgnoreCase))
    code += line + Environment.NewLine;

var ws = new Workspace { CodeSnippet = code };
await orch.RunAsync(ws);

if (ws.Results is { } r)
    Console.WriteLine($"\nRESULT: {(r.Success ? "Success" : "Failed")} | Passed={r.Passed}, Failed={r.Failed}");