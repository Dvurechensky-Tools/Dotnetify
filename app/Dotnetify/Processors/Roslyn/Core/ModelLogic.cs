/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 26 апреля 2026 10:11:16
 * Version: 1.0.7
 */

using Dotnetify.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dotnetify.Processors.Roslyn.Core
{
    public class ModelLogic
    {
        private DotnetifyConfig Config { get; set; }
        public ModelLogic(DotnetifyConfig config)
        {
            Config = config;
        }

        public void Process(ClassDeclarationSyntax cls, string outputDir)
        {
            var name = cls.Identifier.Text;

            string folder = "Models";

            if (name.Contains("FileParameter"))
                folder = "Common";

            File.WriteAllText(
                $"{outputDir}/{folder}/{name}.cs",
                WrapModel(cls)
            );
        }

        public void ProcessEnum(EnumDeclarationSyntax en, string outputDir)
        {
            var name = en.Identifier.Text;

            File.WriteAllText($"{outputDir}/Enums/{name}.cs", WrapEnum(en));
        }

        private string WrapEnum(EnumDeclarationSyntax en)
        {
            var unit = SyntaxFactory.CompilationUnit()
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.NamespaceDeclaration(
                            SyntaxFactory.ParseName(Config.Namespace)
                        ).WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(en)
                        )
                    )
                );

            return RoslynUtils.ForceSimplify(unit.NormalizeWhitespace());
        }

        private string WrapModel(ClassDeclarationSyntax cls)
        {
            var cleaned = CleanClass(cls);

            var unit = SyntaxFactory.CompilationUnit()
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.NamespaceDeclaration(
                            SyntaxFactory.ParseName(Config.Namespace)
                        ).WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(cleaned)
                        )
                    )
                );

            return RoslynUtils.ForceSimplify(unit.NormalizeWhitespace());
        }

        private ClassDeclarationSyntax CleanClass(ClassDeclarationSyntax cls)
        {
            cls = cls.WithAttributeLists(
                SyntaxFactory.List(
                    cls.AttributeLists.Where(a =>
                        !a.ToString().Contains("GeneratedCode"))
                )
            );

            cls = cls.ReplaceNodes(
                cls.DescendantNodes().OfType<AttributeListSyntax>(),
                (oldNode, _) =>
                {
                    var filtered = oldNode.Attributes
                        .Where(a => !a.Name.ToString().Contains("JsonProperty"))
                        .ToList();

                    return filtered.Count == 0
                        ? null
                        : SyntaxFactory.AttributeList(
                            SyntaxFactory.SeparatedList(filtered));
                });

            var props = cls.Members.OfType<PropertyDeclarationSyntax>();

            foreach (var prop in props)
            {
                if (prop.Type.ToString() == "string")
                {
                    var nullable = SyntaxFactory.NullableType(prop.Type);
                    cls = cls.ReplaceNode(prop, prop.WithType(nullable));
                }
            }

            return cls;
        }
    }
}
