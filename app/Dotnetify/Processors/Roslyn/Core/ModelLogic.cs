/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 18 мая 2026 13:10:41
 * Version: 1.0.35
 */

using Dotnetify.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dotnetify.Processors.Roslyn.Core
{
    /// <summary>
    /// Cleans and wraps generated model and enum declarations into the target namespace.
    /// This is where raw NSwag DTOs become warning-free project models.
    /// </summary>
    public class ModelLogic
    {
        private DotnetifyConfig Config { get; set; }
        /// <summary>Creates model processing logic for the configured output namespace.</summary>
        public ModelLogic(DotnetifyConfig config)
        {
            Config = config;
        }

        /// <summary>Writes a model or common helper class to the generated project.</summary>
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

        /// <summary>Writes an enum declaration under the generated namespace.</summary>
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
            // The generator attribution is useful for traceability in raw NSwag output,
            // but it creates noise once Dotnetify owns the generated project structure.
            cls = cls.WithAttributeLists(
                SyntaxFactory.List(
                    cls.AttributeLists.Where(a =>
                        !a.ToString().Contains("GeneratedCode"))
                )
            );

            // JsonProperty duplicates C# property names in the scaffold and makes the
            // output harder to read. Keep behavior-oriented attributes such as Required,
            // JsonConverter and JsonExtensionData.
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

            cls = cls.ReplaceNodes(
                cls.Members.OfType<PropertyDeclarationSyntax>(),
                (prop, _) =>
                {
                    // OpenAPI optional reference properties must be nullable in the
                    // generated nullable-enabled project, otherwise clean builds emit
                    // CS8618 warnings for perfectly valid optional schema fields.
                    if (!ShouldMakePropertyNullable(prop))
                        return prop;

                    return prop.WithType(SyntaxFactory.NullableType(prop.Type));
                });

            cls = cls.ReplaceNodes(
                cls.Members.OfType<FieldDeclarationSyntax>(),
                (field, _) =>
                {
                    var type = field.Declaration.Type;

                    // NSwag uses backing fields for additionalProperties. Those fields
                    // are intentionally initialized lazily by the getter, so nullable is
                    // the correct representation of their lifecycle.
                    if (!IsNullableCandidate(type))
                        return field;

                    return field.WithDeclaration(
                        field.Declaration.WithType(SyntaxFactory.NullableType(type)));
                });

            cls = cls.ReplaceNodes(
                cls.DescendantNodes().OfType<ParameterSyntax>(),
                (parameter, _) =>
                {
                    // FileParameter constructors pass null through string parameters;
                    // reflect that explicitly to keep the generated project warning-free.
                    if (parameter.Type is null ||
                        parameter.Type.ToString() != "string" ||
                        parameter.Type is NullableTypeSyntax)
                    {
                        return parameter;
                    }

                    return parameter.WithType(
                        SyntaxFactory.NullableType(parameter.Type));
                });

            return cls;
        }

        private bool ShouldMakePropertyNullable(PropertyDeclarationSyntax prop)
        {
            if (!IsNullableCandidate(prop.Type))
                return false;

            // Strings are always reference types. Required string properties keep their
            // validation attribute, but nullable annotations avoid constructor warnings
            // in DTO-style generated models.
            if (prop.Type.ToString() == "string")
                return true;

            // Private setters are usually assigned by constructors in helper types such
            // as FileParameter. Making those nullable would weaken valid invariants.
            if (HasPrivateSetter(prop))
                return false;

            return !HasRequiredAttribute(prop) && prop.Initializer is null;
        }

        private bool HasPrivateSetter(PropertyDeclarationSyntax prop)
        {
            return prop.AccessorList?.Accessors.Any(accessor =>
                accessor.IsKind(SyntaxKind.SetAccessorDeclaration) &&
                accessor.Modifiers.Any(SyntaxKind.PrivateKeyword)) == true;
        }

        private bool HasRequiredAttribute(PropertyDeclarationSyntax prop)
        {
            return prop.AttributeLists
                .SelectMany(a => a.Attributes)
                .Any(a => a.Name.ToString().Contains("Required"));
        }

        private bool IsNullableCandidate(TypeSyntax type)
        {
            if (type is NullableTypeSyntax)
                return false;

            if (type is ArrayTypeSyntax)
                return true;

            if (type is GenericNameSyntax ||
                type is QualifiedNameSyntax ||
                type is IdentifierNameSyntax)
            {
                return !IsKnownValueType(type.ToString());
            }

            return type.ToString() == "string" ||
                   type.ToString() == "object";
        }

        private bool IsKnownValueType(string type)
        {
            return type is "bool" or "byte" or "sbyte" or "short" or "ushort"
                or "int" or "uint" or "long" or "ulong" or "float" or "double"
                or "decimal" or "char" or "Guid" or "DateTime" or "DateTimeOffset"
                or "System.Guid" or "System.DateTime" or "System.DateTimeOffset";
        }
    }
}
