/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 24 апреля 2026 07:12:04
 * Version: 1.0.5
 */

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Dotnetify.Processors.Roslyn.Core
{
    public static class RoslynUtils
    {
        public static string ForceSimplify(SyntaxNode root)
        {
            var namespaces = new HashSet<string>();

            var newRoot = root.ReplaceNodes(
                root.DescendantNodes().OfType<QualifiedNameSyntax>(),
                (node, _) =>
                {
                    var nsParts = new List<string>();

                    ExpressionSyntax current = node.Left;

                    while (current is QualifiedNameSyntax qn)
                    {
                        nsParts.Insert(0, qn.Right.Identifier.Text);
                        current = qn.Left;
                    }

                    if (current is IdentifierNameSyntax id)
                    {
                        nsParts.Insert(0, id.Identifier.Text);
                    }

                    if (nsParts.Count > 1)
                    {
                        namespaces.Add(string.Join(".", nsParts));
                    }

                    return node.Right;
                });

            var unit = (CompilationUnitSyntax)newRoot;

            var existing = unit.Usings
                .Select(u => u.Name?.ToString() ?? "")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var usings = namespaces
                .Where(ns => ns.Contains("."))
                .Where(ns => !existing.Contains(ns))
                .Where(ns => !IsBlockedNamespace(ns))   
                .OrderBy(x => x)
                .Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)));

            unit = unit.WithUsings(unit.Usings.AddRange(usings));

            unit = unit.WithUsings(
                SyntaxFactory.List(
                    unit.Usings.Where(u =>
                    {
                        var name = u.Name?.ToString() ?? "";
                        return !IsBlockedNamespace(name);
                    })
                )
            );

            return unit.NormalizeWhitespace().ToFullString();
        }

        public static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "Default";

            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static string FormatCode(SyntaxNode root)
        {
            using var workspace = new AdhocWorkspace(
                MefHostServices.Create(MefHostServices.DefaultAssemblies)
            );

            var assemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location));

            var project = workspace.AddProject("TempProject", LanguageNames.CSharp)
                .WithCompilationOptions(
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                )
                .AddMetadataReferences(assemblies);

            var document = project.AddDocument(
                "Temp.cs",
                SourceText.From(root.ToFullString())
            );

            var syntaxRoot = document.GetSyntaxRootAsync().Result!;

            var annotatedRoot = syntaxRoot.ReplaceNodes(
                syntaxRoot.DescendantNodes().OfType<QualifiedNameSyntax>(),
                (node, _) => node
                    .WithAdditionalAnnotations(Simplifier.Annotation)
                    .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation)
            );

            document = document.WithSyntaxRoot(annotatedRoot);
            document = Simplifier.ReduceAsync(document).Result;
            document = Formatter.FormatAsync(document).Result;

            var finalRoot = document.GetSyntaxRootAsync().Result!;

            var unit = (CompilationUnitSyntax)finalRoot;

            unit = unit.WithUsings(
                SyntaxFactory.List(
                    unit.Usings.Where(u =>
                    {
                        var name = u.Name?.ToString() ?? "";
                        return !IsBlockedNamespace(name);
                    })
                )
            );

            return unit.ToFullString();
        }

        private static bool IsBlockedNamespace(string ns)
        {
            if (string.IsNullOrWhiteSpace(ns))
                return false;

            return ns.Equals("Mvc", StringComparison.OrdinalIgnoreCase)
                || ns.StartsWith("Mvc.", StringComparison.OrdinalIgnoreCase);
        }
    }
}