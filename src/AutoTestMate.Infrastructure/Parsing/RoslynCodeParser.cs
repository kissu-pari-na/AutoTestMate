using AutoTestMate.Application.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoTestMate.Infrastructure.Parsing;

public sealed class RoslynCodeParser : ICodeParser
{
    public Task<ParsedCode> ParseAsync(string code, CancellationToken ct = default)
    {
        var (wrapped, wrappedNs, wrappedClass) = WrapIfNeeded(code);
        var tree = CSharpSyntaxTree.ParseText(wrapped, cancellationToken: ct);
        var root = tree.GetCompilationUnitRoot(ct);

        var typeDecl = root.DescendantNodes().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        var methodDecl = typeDecl?.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault();

        string ns  = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
                     ?? root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString()
                     ?? wrappedNs ?? "AutoTestMate.Snippets";
        string cls = typeDecl?.Identifier.Text ?? wrappedClass ?? "SnippetClass";

        if (methodDecl is null)
        {
            return Task.FromResult(new ParsedCode(code, ns, cls, "Method", "void", false, Array.Empty<Parameter>(), "UNKNOWN_SIGNATURE"));
        }

        var methodName = methodDecl.Identifier.Text;
        var returnType = methodDecl.ReturnType.ToString();
        var isStatic   = methodDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));

        var parameters = methodDecl.ParameterList.Parameters
            .Select(p => {
                var mods = string.Join(" ", p.Modifiers.Select(m => m.Text));
                var type = string.IsNullOrWhiteSpace(mods) ? p.Type?.ToString() ?? "object" : $"{mods} {p.Type}";
                return new Parameter(type, p.Identifier.Text);
            }).ToList();

        var signature = $"{(isStatic ? "static " : "")}{returnType} {methodName}({string.Join(", ", parameters.Select(pr => $"{pr.Type} {pr.Name}"))})";

        return Task.FromResult(new ParsedCode(code, ns, cls, methodName, returnType, isStatic, parameters, signature));
    }

    private static (string wrapped, string? ns, string? cls) WrapIfNeeded(string code)
    {
        var t = code.Trim();
        var looksClass = t.Contains("class ") || t.Contains("record ") || t.Contains("struct ");
        var looksNs = t.Contains("namespace ");
        if (looksClass || looksNs) return (code, null, null);
        const string ns = "AutoTestMate.Snippets";
        const string cls = "SnippetClass";
        var wrapped = $@"namespace {ns};
public class {cls}
{{
{code}
}}";
        return (wrapped, ns, cls);
    }
}