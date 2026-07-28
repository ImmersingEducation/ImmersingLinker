using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
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

    private static IEnumerable<string> ExtractValidatorExpressions(string json)
    {
        var results = new List<string>();

        using var doc = JsonDocument.Parse(json);
        CollectValidatorValues(doc.RootElement, results);

        return results;
    }

    private static void CollectValidatorValues(JsonElement element, List<string> results)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("validator")
                        && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            results.Add(value);
                    }
                    else
                    {
                        CollectValidatorValues(property.Value, results);
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectValidatorValues(item, results);
                }
                break;
        }
    }
}
