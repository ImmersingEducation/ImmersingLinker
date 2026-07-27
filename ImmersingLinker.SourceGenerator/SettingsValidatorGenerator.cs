using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace ImmersingLinker.SourceGenerator;

[Generator]
public sealed class SettingsValidatorGenerator : IIncrementalGenerator
{
    private const string DiagnosticId = "ILNK001";
    private const string Category = "SettingsValidator";

    private static readonly DiagnosticDescriptor ValidatorSyntaxError = new(
        DiagnosticId,
        "Validator expression has syntax error",
        "Validator expression '{0}' in '{1}' has syntax error(s): {2}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var jsonFiles = context.AdditionalTextsProvider
            .Where(static file => IsSettingsJsonFile(file.Path));

        context.RegisterSourceOutput(jsonFiles, GenerateReport);
    }

    private static bool IsSettingsJsonFile(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.EndsWith(".json")
            && (normalized.Contains("/Assets/Settings/") || normalized.Contains("Assets/Settings/"));
    }

    private static void GenerateReport(SourceProductionContext context, AdditionalText file)
    {
        var text = file.GetText(context.CancellationToken);
        if (text is null)
            return;

        var jsonContent = text.ToString();
        var validators = ExtractValidatorExpressions(jsonContent);

        foreach (var expr in validators)
        {
            var tree = CSharpSyntaxTree.ParseText(
                expr,
                options: new CSharpParseOptions(kind: SourceCodeKind.Script),
                cancellationToken: context.CancellationToken);

            var errors = new List<string>();
            foreach (var diagnostic in tree.GetDiagnostics())
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                    errors.Add(diagnostic.GetMessage());
            }

            if (errors.Count > 0)
            {
                var errorSummary = string.Join("; ", errors);
                var descriptor = ValidatorSyntaxError;
                context.ReportDiagnostic(
                    Diagnostic.Create(descriptor, Location.None, expr, file.Path, errorSummary));
            }
        }
    }

#pragma warning disable RS1024 // Compare symbols correctly
    private static IEnumerable<string> ExtractValidatorExpressions(string json)
    {
        var results = new List<string>();
        int searchStart = 0;

        while (true)
        {
            var keyIndex = json.IndexOf("\"validator\"", searchStart, System.StringComparison.OrdinalIgnoreCase);
            if (keyIndex < 0)
                break;

            var colonIndex = json.IndexOf(':', keyIndex + 11);
            if (colonIndex < 0)
                break;

            var quoteStart = json.IndexOf('"', colonIndex + 1);
            if (quoteStart < 0)
                break;

            var valueStart = quoteStart + 1;
            var sb = new StringBuilder();
            int i = valueStart;

            while (i < json.Length)
            {
                if (json[i] == '\\')
                {
                    sb.Append(json[i]);
                    i++;
                    if (i < json.Length)
                    {
                        sb.Append(json[i]);
                        i++;
                    }
                }
                else if (json[i] == '"')
                {
                    break;
                }
                else
                {
                    sb.Append(json[i]);
                    i++;
                }
            }

            var value = sb.ToString();
            if (!string.IsNullOrWhiteSpace(value))
                results.Add(value);

            searchStart = i + 1;
        }

        return results;
    }
}
