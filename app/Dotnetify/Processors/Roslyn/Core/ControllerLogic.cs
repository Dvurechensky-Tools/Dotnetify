/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 30 мая 2026 16:01:40
 * Version: 1.0.47
 */

using Dotnetify.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dotnetify.Processors.Roslyn.Core
{
    /// <summary>
    /// Converts NSwag abstract controller classes into concrete ASP.NET Core controllers
    /// grouped by the first route segment.
    /// </summary>
    public class ControllerLogic
    {
        private DotnetifyConfig Config { get; set; }
        private SyntaxBodyController BodyGenerator { get; set; }

        /// <summary>
        /// Creates controller generation logic with access to model declarations for
        /// response body mock generation.
        /// </summary>
        public ControllerLogic(DotnetifyConfig config,
            Dictionary<string, ClassDeclarationSyntax> models)
        {
            Config = config;
            BodyGenerator = new SyntaxBodyController(models);
        }

        /// <summary>Detects NSwag controller classes by their ControllerBase inheritance.</summary>
        public bool IsController(ClassDeclarationSyntax cls)
        {
            return cls.BaseList?.Types
                .Any(t => t.ToString().Contains("ControllerBase")) == true;
        }

        /// <summary>
        /// Emits one or more concrete controller files from an NSwag abstract controller.
        /// </summary>
        public void Process(ClassDeclarationSyntax cls, string outputDir)
        {
            var methods = cls.Members.OfType<MethodDeclarationSyntax>();

            var grouped = new Dictionary<string, List<MethodDeclarationSyntax>>();

            foreach (var method in methods)
            {
                var routeAttr = method.AttributeLists
                    .SelectMany(a => a.Attributes)
                    .FirstOrDefault(a => a.Name.ToString().Contains("Route"));

                if (routeAttr == null)
                    continue;

                var route = routeAttr.ArgumentList?.Arguments.First().ToString().Trim('"');

                var group = ExtractGroup(route);

                if (!grouped.ContainsKey(group))
                    grouped[group] = new();

                grouped[group].Add(method);
            }

            foreach (var group in grouped)
            {
                var className = ToSafeClassName(group.Key) + "Controller";

                var writer = new StringWriter();

                writer.WriteLine("using Microsoft.AspNetCore.Mvc;");
                writer.WriteLine();
                writer.WriteLine($"namespace {Config.Namespace};");
                writer.WriteLine();
                writer.WriteLine("[ApiController]");
                writer.WriteLine($"[Route(\"{group.Key}\")]");
                writer.WriteLine($"public class {className} : ControllerBase");
                writer.WriteLine("{");

                foreach (var method in group.Value)
                {
                    var methodDecl = method
                         .WithAttributeLists(
                            SyntaxFactory.List(new[] { NormalizeAttributes(method, group.Key) })
                        )
                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                method.Modifiers.Where(m => !m.IsKind(SyntaxKind.AbstractKeyword))
                            )
                        )
                        .WithBody(BodyGenerator.GenerateBody(method))
                        .WithSemicolonToken(default);

                    writer.WriteLine(methodDecl.NormalizeWhitespace().ToFullString());
                }

                writer.WriteLine("}");

                var controllerRoot = CSharpSyntaxTree
                    .ParseText(writer.ToString())
                    .GetCompilationUnitRoot();

                var formatted = RoslynUtils.ForceSimplify(controllerRoot.NormalizeWhitespace());
                File.WriteAllText($"{outputDir}/Controllers/{className}.cs", formatted);
            }
        }

        private string ToSafeClassName(string raw)
        {
            // Route segments can contain legacy suffixes, parameters, or punctuation.
            // Normalize them into deterministic PascalCase controller class names.
            if (string.IsNullOrWhiteSpace(raw))
                return "Default";

            raw = raw.Trim();

            raw = raw.Replace(".php", " Php", StringComparison.OrdinalIgnoreCase);
            raw = raw.Replace(".asp", " Asp", StringComparison.OrdinalIgnoreCase);
            raw = raw.Replace(".aspx", " Aspx", StringComparison.OrdinalIgnoreCase);

            raw = System.Text.RegularExpressions.Regex
                .Replace(raw, @"[^a-zA-Z0-9]+", " ");

            var parts = raw
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var result = string.Concat(parts.Select(x =>
                char.ToUpperInvariant(x[0]) + x.Substring(1)));

            if (string.IsNullOrWhiteSpace(result))
                result = "Default";

            if (char.IsDigit(result[0]))
                result = "_" + result;

            return result;
        }

        private AttributeListSyntax NormalizeAttributes(MethodDeclarationSyntax method, string group)
        {
            // NSwag emits separate [Route] and [Http*] attributes on abstract methods.
            // Concrete ASP.NET controllers are cleaner with one [Http*(relativeRoute)].
            var attrs = method.AttributeLists
                .SelectMany(a => a.Attributes)
                .ToList();

            var httpAttr = attrs.FirstOrDefault(a =>
                a.Name.ToString().Contains("Http"));

            var routeAttr = attrs.FirstOrDefault(a =>
                a.Name.ToString().Contains("Route"));

            string route = routeAttr?
                .ArgumentList?
                .Arguments.First()
                .ToString()
                .Trim('"') ?? "";

            if (route.StartsWith(group + "/"))
                route = route.Substring(group.Length + 1);

            var httpName = httpAttr?.Name.ToString().Split('.').Last() ?? "HttpGet";

            var newAttr = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(
                        SyntaxFactory.IdentifierName(httpName),
                        SyntaxFactory.AttributeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.AttributeArgument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal(route)
                                    )
                                )
                            )
                        )
                    )
                )
            );

            return newAttr;
        }

        private string ExtractGroup(string? route)
        {
            // Empty/root routes and parameter-first routes cannot produce stable
            // controller names, so use the project namespace as the default group.
            if (string.IsNullOrWhiteSpace(route))
                return Config.Namespace;

            var clean = route.Trim('/');

            if (clean.StartsWith("{"))
                return Config.Namespace;

            return clean.Split('/')[0];
        }
    }
}
