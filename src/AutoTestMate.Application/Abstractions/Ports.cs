using AutoTestMate.Domain;

namespace AutoTestMate.Application.Abstractions;

public interface IFlowPublisher { Task PublishAsync(FlowEvent evt, CancellationToken ct = default); }
public interface ICodeParser { Task<ParsedCode> ParseAsync(string code, CancellationToken ct = default); }
public interface ISourceUnderTestWriter { Task<(string Namespace, string ClassName, string Path)> WriteAsync(ParsedCode input, CancellationToken ct = default); }
public interface ITestGenerationService { Task<GeneratedTests> GenerateAsync(ParsedCode input, CancellationToken ct = default); }
public interface ITestWriter { Task<string> WriteAsync(GeneratedTests tests, CancellationToken ct = default); }
public interface ITestRunner { Task<TestRunResult> RunAsync(CancellationToken ct = default); }

public sealed record Parameter(string Type, string Name);
public sealed record ParsedCode(string Original, string? DeclaredNamespace, string? DeclaredClass, string MethodName, string ReturnType, bool IsStatic, IReadOnlyList<Parameter> Parameters, string Signature);
public sealed record GeneratedTests(string FileName, string SourceCode);
public sealed record TestRunResult(bool Success, int Passed, int Failed, string RawOutput);
