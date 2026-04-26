/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 26 апреля 2026 10:11:16
 * Version: 1.0.7
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
    /// <summary>
    /// Shared Roslyn helpers for simplifying generated syntax while preserving valid
    /// C# structure such as using directives and namespace declarations.
    /// </summary>
    public static class RoslynUtils
    {
        /// <summary>
        /// Moves safe fully-qualified type references into using directives and returns
        /// normalized source text.
        /// </summary>
        public static string ForceSimplify(SyntaxNode root)
        {
            var namespaces = new HashSet<string>();

            // Static member access (for example System.Threading.Tasks.Task.FromResult)
            // is simplified only when the namespace is already known. This prevents
            // accidental rewrites of ordinary chains such as client.Response.Metadata.
            var originalUsings = root is CompilationUnitSyntax rootUnit
                ? rootUnit.Usings
                    .Select(u => u.Name?.ToString() ?? "")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var newRoot = root.ReplaceNodes(
                root.DescendantNodes().OfType<QualifiedNameSyntax>(),
                (node, _) =>
                {
                    // Using directives and namespace declarations are structural C# syntax,
                    // not type references. Shortening them corrupts valid imports such as
                    // Microsoft.AspNetCore.Mvc into unusable partial namespaces.
                    if (ShouldKeepQualifiedName(node))
                        return node;

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

            newRoot = newRoot.ReplaceNodes(
                newRoot.DescendantNodes().OfType<MemberAccessExpressionSyntax>(),
                (node, _) =>
                {
                    if (!TryGetMemberAccessParts(node, out var parts) || parts.Count < 4)
                        return node;

                    var namespaceName = string.Join(".", parts.Take(parts.Count - 2));
                    var typeName = parts[^2];

                    if (!namespaces.Contains(namespaceName)
                        && !originalUsings.Contains(namespaceName))
                    {
                        return node;
                    }

                    if (!string.IsNullOrWhiteSpace(namespaceName))
                        namespaces.Add(namespaceName);

                    return node.WithExpression(SyntaxFactory.IdentifierName(typeName));
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

            // NSwag sometimes emits Mvc.* aliases that are not valid standalone imports
            // in the generated server project. Remove those after adding real namespaces.
            unit = unit.WithUsings(
                SyntaxFactory.List(
                    unit.Usings.Where(u =>
                    {
                        var name = u.Name?.ToString() ?? "";
                        return !IsBlockedNamespace(name);
                    })
                )
            );

            unit = SimplifyNamesCoveredByUsings(unit);

            return unit.NormalizeWhitespace().ToFullString();
        }

        private static CompilationUnitSyntax SimplifyNamesCoveredByUsings(CompilationUnitSyntax unit)
        {
            // A final cleanup pass catches nested generic arguments that were left
            // fully-qualified after the first rewrite but are now covered by imports.
            var importedNamespaces = unit.Usings
                .Select(u => u.Name?.ToString() ?? "")
                .Where(ns => !string.IsNullOrWhiteSpace(ns))
                .OrderByDescending(ns => ns.Length)
                .ToArray();

            if (importedNamespaces.Length == 0)
                return unit;

            return unit.ReplaceNodes(
                unit.DescendantNodes().OfType<QualifiedNameSyntax>(),
                (node, _) =>
                {
                    if (ShouldKeepQualifiedName(node))
                        return node;

                    var name = node.ToString();
                    var importedNamespace = importedNamespaces
                        .FirstOrDefault(ns =>
                            name.StartsWith(ns + ".", StringComparison.Ordinal));

                    if (importedNamespace is null)
                        return node;

                    var shortName = name.Substring(importedNamespace.Length + 1);

                    return SyntaxFactory
                        .ParseName(shortName)
                        .WithTriviaFrom(node);
                });
        }

        private static bool ShouldKeepQualifiedName(QualifiedNameSyntax node)
        {
            // Parent-qualified nodes are handled by their outermost QualifiedNameSyntax.
            // Rewriting inner segments first would add incomplete imports like
            // System.Collections instead of System.Collections.Generic.
            return node.Ancestors().Any(a => a is UsingDirectiveSyntax)
                || node.Parent is NamespaceDeclarationSyntax namespaceDeclaration
                    && namespaceDeclaration.Name == node
                || node.Parent is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclaration
                    && fileScopedNamespaceDeclaration.Name == node
                || node.Parent is QualifiedNameSyntax;
        }

        private static bool TryGetMemberAccessParts(
            MemberAccessExpressionSyntax node,
            out List<string> parts)
        {
            parts = new List<string>();
            AddMemberAccessParts(node, parts);

            return parts.All(part => !string.IsNullOrWhiteSpace(part));
        }

        private static void AddMemberAccessParts(ExpressionSyntax expression, List<string> parts)
        {
            switch (expression)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    AddMemberAccessParts(memberAccess.Expression, parts);
                    parts.Add(memberAccess.Name.Identifier.Text);
                    break;

                case IdentifierNameSyntax identifier:
                    parts.Add(identifier.Identifier.Text);
                    break;
            }
        }

        /// <summary>Capitalizes a route segment fallback into a safe PascalCase fragment.</summary>
        public static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "Default";

            return char.ToUpper(s[0]) + s.Substring(1);
        }

        /// <summary>
        /// Uses Roslyn's simplifier/formatter services for callers that need semantic
        /// formatting instead of Dotnetify's deterministic text cleanup.
        /// </summary>
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
