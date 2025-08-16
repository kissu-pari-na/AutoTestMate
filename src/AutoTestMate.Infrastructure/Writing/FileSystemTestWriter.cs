using AutoTestMate.Application.Abstractions;

namespace AutoTestMate.Infrastructure.Writing;

public sealed class FileSystemTestWriter : ITestWriter
{
    private readonly string _testsProjectDir;
    public FileSystemTestWriter(string testsProjectDir) => _testsProjectDir = testsProjectDir;
    public async Task<string> WriteAsync(GeneratedTests tests, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_testsProjectDir);
        var path = Path.Combine(_testsProjectDir, tests.FileName);
        await File.WriteAllTextAsync(path, tests.SourceCode, ct);
        return path;
    }
}