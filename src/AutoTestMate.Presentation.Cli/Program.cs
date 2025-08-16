using AutoTestMate.Application.Abstractions;
using AutoTestMate.Application.Agents;
using AutoTestMate.Infrastructure.Agents;
using AutoTestMate.Infrastructure.Flow;
using AutoTestMate.Infrastructure.Generation;
using AutoTestMate.Infrastructure.Parsing;
using AutoTestMate.Infrastructure.Runner;
using AutoTestMate.Infrastructure.Writing;

var webHubUrl = "http://localhost:5173/flow";
var testsDir  = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../tests/AutoTestMate.SampleUnderTest.Tests"));

IFlowPublisher publisher          = new SignalRFlowPublisher(webHubUrl);
ICodeParser parser                = new RoslynCodeParser();
ISourceUnderTestWriter srcWriter  = new FileSystemSourceUnderTestWriter(Path.Combine(testsDir, "Generated"));
ITestGenerationService gen        = new SKTestGenerationService(publisher);
ITestWriter writer                = new FileSystemTestWriter(Path.Combine(testsDir, "Generated"));
ITestRunner runner                = new DotNetTestRunner(testsDir, publisher);

var agents = new List<IAgent>
{
    new ParserAgent(parser, publisher),
    new TestDesignerAgent(publisher),
    new CodeGenAgent(gen, srcWriter, writer, publisher),
    //new RunnerAgent(runner, publisher),
    //new CriticAgent(publisher, maxRetries: 2)
};

var orch = new Orchestrator(agents, publisher);

Console.WriteLine("Paste a C# method (end with a single line containing only 'END'):");
string? line; var code = "";
while (!string.Equals(line = Console.ReadLine(), "END", StringComparison.OrdinalIgnoreCase))
    code += line + Environment.NewLine;

var ws = new Workspace { CodeSnippet = code };
await orch.RunAsync(ws);

if (ws.Results is { } r)
    Console.WriteLine($"\nRESULT: {(r.Success ? "Success" : "Failed")} | Passed={r.Passed}, Failed={r.Failed}");