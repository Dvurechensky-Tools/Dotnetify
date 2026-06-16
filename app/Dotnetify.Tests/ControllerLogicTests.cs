/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 16 июня 2026 07:13:05
 * Version: 1.0.64
 */

using Dotnetify.Models;
using Dotnetify.Processors.Roslyn.Core;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Xunit;

namespace Dotnetify.Tests;

/// <summary>
/// Regression tests for the full controller generation path, including file output
/// and final Roslyn simplification.
/// </summary>
public class ControllerLogicTests
{
    [Fact]
    public void Process_SimplifiesQualifiedReturnTypesInGeneratedController()
    {
        // This covers the full controller writer path, not just RoslynUtils in
        // isolation. It catches regressions where method signatures are reintroduced
        // with fully-qualified generic return types.
        const string source = """
            using Microsoft.AspNetCore.Mvc;
            using System.Threading.Tasks;

            public abstract class StoreControllerBase : ControllerBase
            {
                [Microsoft.AspNetCore.Mvc.HttpGet]
                [Microsoft.AspNetCore.Mvc.Route("store/inventory")]
                public abstract Task<System.Collections.Generic.IDictionary<string, int>> GetInventory();
            }
            """;

        var outputDir = Path.Combine(Path.GetTempPath(), "Dotnetify.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(outputDir, "Controllers"));

        try
        {
            var controller = CSharpSyntaxTree
                .ParseText(source)
                .GetCompilationUnitRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Single();

            var logic = new ControllerLogic(
                new DotnetifyConfig { Namespace = "CleanCheckApi" },
                new Dictionary<string, ClassDeclarationSyntax>());

            logic.Process(controller, outputDir);

            var generated = File.ReadAllText(Path.Combine(outputDir, "Controllers", "StoreController.cs"));

            Assert.Contains("using System.Collections.Generic;", generated);
            Assert.Contains("public Task<IDictionary<string, int>> GetInventory()", generated);
            Assert.Contains("namespace CleanCheckApi;", generated);
            Assert.DoesNotContain("System.Collections.Generic.IDictionary", generated);
            Assert.DoesNotContain("using CleanCheckApi;", generated);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }
}
