/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 16 июня 2026 07:13:05
 * Version: 1.0.64
 */

using Dotnetify.Processors.Roslyn.Core;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Xunit;

namespace Dotnetify.Tests;

/// <summary>
/// Regression tests for generated mock response expressions used inside controller bodies.
/// </summary>
public class ResponseObjectGeneratorTests
{
    [Theory]
    [InlineData("float", "1.0f")]
    [InlineData("float?", "1.0f")]
    [InlineData("double", "1.0")]
    [InlineData("decimal", "1.0")]
    public void Generate_UsesCorrectNumericLiterals(string type, string expected)
    {
        // Literal spelling matters: "1.0" compiles as double and breaks float DTOs.
        var generator = new ResponseObjectGenerator(new Dictionary<string, ClassDeclarationSyntax>());

        var result = generator.Generate(type);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Generate_UsesFloatLiteralForModelFloatProperties()
    {
        // Object mock generation walks model properties, so verify the float rule
        // survives the recursive model path and not only direct primitive calls.
        const string source = """
            public partial class PrimitiveCase
            {
                public float? Ratio { get; set; }
                public double? Price { get; set; }
            }
            """;

        var model = CSharpSyntaxTree
            .ParseText(source)
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Single();

        var generator = new ResponseObjectGenerator(
            new Dictionary<string, ClassDeclarationSyntax>
            {
                ["PrimitiveCase"] = model
            });

        var result = generator.Generate("PrimitiveCase");

        Assert.Contains("Ratio = 1.0f", result);
        Assert.Contains("Price = 1.0", result);
        Assert.DoesNotContain("Ratio = 1.0,", result);
    }
}
