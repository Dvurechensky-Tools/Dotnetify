/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 21 апреля 2026 07:09:27
 * Version: 1.0.2
 */

using Dotnetify.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dotnetify.Processors.Roslyn.Core
{
    public class ControllerLogic
    {
        private DotnetifyConfig Config { get; set; }
        private SyntaxBodyController BodyGenerator { get; set; }

        public ControllerLogic(DotnetifyConfig config,
            Dictionary<string, ClassDeclarationSyntax> models)
        {
            Config = config;
            BodyGenerator = new SyntaxBodyController(models);
        }

        public bool IsController(ClassDeclarationSyntax cls)
        {
            return cls.BaseList?.Types
                .Any(t => t.ToString().Contains("ControllerBase")) == true;
        }

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
                var className = RoslynUtils.Capitalize(group.Key) + "Controller";

                var writer = new StringWriter();

                writer.WriteLine($"using {Config.Namespace};");
                writer.WriteLine("using Microsoft.AspNetCore.Mvc;");
                writer.WriteLine();
                writer.WriteLine($"[ApiController]");
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

        private AttributeListSyntax NormalizeAttributes(MethodDeclarationSyntax method, string group)
        {
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
            if (string.IsNullOrWhiteSpace(route))
                return "default";

            var clean = route.Trim('/');

            if (clean.StartsWith("{"))
                return "default";

            return clean.Split('/')[0];
        }
    }
}
