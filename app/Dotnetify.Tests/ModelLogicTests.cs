/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 19 мая 2026 10:37:00
 * Version: 1.0.36
 */

using Dotnetify.Models;
using Dotnetify.Processors.Roslyn.Core;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Xunit;

namespace Dotnetify.Tests;

/// <summary>
/// Regression tests for model cleanup rules that keep generated DTOs readable and
/// nullable-warning free.
/// </summary>
public class ModelLogicTests
{
    [Fact]
    public void Process_MakesOptionalReferencePropertiesNullable()
    {
        // Optional OpenAPI reference properties should not produce CS8618 warnings
        // in the nullable-enabled generated project.
        const string source = """
            public partial class ComplexCase
            {
                [System.ComponentModel.DataAnnotations.Required]
                public PrimitiveCase Primitive { get; set; } = new PrimitiveCase();

                public System.Collections.Generic.List<System.Collections.Generic.List<int>> Matrix { get; set; }
                public System.Collections.Generic.IDictionary<string, ChildCase> Lookup { get; set; }
                public ChildCase Payload { get; set; }
                public string Name { get; set; }
            }
            """;

        var generated = ProcessModel(source, "ComplexCase");

        Assert.Contains("public List<List<int>>? Matrix { get; set; }", generated);
        Assert.Contains("public IDictionary<string, ChildCase>? Lookup { get; set; }", generated);
        Assert.Contains("public ChildCase? Payload { get; set; }", generated);
        Assert.Contains("public string? Name { get; set; }", generated);
        Assert.Contains("public PrimitiveCase Primitive { get; set; } = new PrimitiveCase();", generated);
    }

    [Fact]
    public void Process_MakesAdditionalPropertiesBackingFieldNullable()
    {
        // additionalProperties backing fields are lazily initialized by the generated
        // getter, so their field lifecycle is nullable by design.
        const string source = """
            public partial class StatusEnvelope
            {
                private System.Collections.Generic.IDictionary<string, object> _additionalProperties;

                [Newtonsoft.Json.JsonExtensionData]
                public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
                {
                    get
                    {
                        return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>());
                    }

                    set
                    {
                        _additionalProperties = value;
                    }
                }
            }
            """;

        var generated = ProcessModel(source, "StatusEnvelope");

        Assert.Contains("private IDictionary<string, object>? _additionalProperties;", generated);
        Assert.Contains("[JsonExtensionData]", generated);
        Assert.Contains("public IDictionary<string, object>? AdditionalProperties", generated);
    }

    [Fact]
    public void Process_MakesFileParameterStringMembersNullable()
    {
        // NSwag file helpers pass null through constructor overloads. The generated
        // helper must express that contract explicitly to keep builds clean.
        const string source = """
            public partial class FileParameter
            {
                public FileParameter(System.IO.Stream data) : this(data, null, null)
                {
                }

                public FileParameter(System.IO.Stream data, string fileName) : this(data, fileName, null)
                {
                }

                public FileParameter(System.IO.Stream data, string fileName, string contentType)
                {
                    Data = data;
                    FileName = fileName;
                    ContentType = contentType;
                }

                public System.IO.Stream Data { get; private set; }
                public string FileName { get; private set; }
                public string ContentType { get; private set; }
            }
            """;

        var generated = ProcessModel(source, "FileParameter", common: true);

        Assert.Contains("public FileParameter(Stream data, string? fileName)", generated);
        Assert.Contains("public FileParameter(Stream data, string? fileName, string? contentType)", generated);
        Assert.Contains("public string? FileName { get; private set; }", generated);
        Assert.Contains("public string? ContentType { get; private set; }", generated);
        Assert.Contains("public Stream Data { get; private set; }", generated);
    }

    private static string ProcessModel(string source, string className, bool common = false)
    {
        // Use a real temporary output folder so tests exercise the same file-writing
        // path used by the generator instead of only testing in-memory syntax changes.
        var outputDir = Path.Combine(Path.GetTempPath(), "Dotnetify.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(outputDir, "Models"));
        Directory.CreateDirectory(Path.Combine(outputDir, "Common"));

        try
        {
            var model = CSharpSyntaxTree
                .ParseText(source)
                .GetCompilationUnitRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Single();

            var logic = new ModelLogic(new DotnetifyConfig { Namespace = "Generated" });
            logic.Process(model, outputDir);

            var folder = common ? "Common" : "Models";
            return File.ReadAllText(Path.Combine(outputDir, folder, $"{className}.cs"));
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }
}
