/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 11 июня 2026 13:48:19
 * Version: 1.0.59
 */

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dotnetify.Processors.Roslyn.Core
{
    /// <summary>
    /// Produces deterministic mock response expressions for generated controller bodies.
    /// The output is intentionally simple but must always compile for supported schema types.
    /// </summary>
    public class ResponseObjectGenerator
    {
        private readonly Dictionary<string, ClassDeclarationSyntax> _models;

        /// <summary>
        /// Creates a response generator with access to model declarations for object initializers.
        /// </summary>
        public ResponseObjectGenerator(
            Dictionary<string, ClassDeclarationSyntax> models)
        {
            _models = models;
        }

        /// <summary>
        /// Generates a C# expression that can be assigned to or returned as the requested type.
        /// </summary>
        public string Generate(
            string type,
            int depth = 0)
        {
            type = Normalize(type);

            // Stop recursive object graphs from exploding the generated controller body.
            // The fallback still compiles and clearly signals an intentionally shallow mock.
            if (depth > 2)
                return "default!";

            if (IsNullable(type, out var nullableInner))
                return Generate(nullableInner, depth);

            if (IsString(type))
                return "\"test\"";

            if (IsInteger(type))
                return "1";

            // C# treats 1.0 as double. Float DTO fields need the suffix to keep the
            // generated project compiling without casts.
            if (IsFloat(type))
                return "1.0f";

            if (IsDecimal(type))
                return "1.0";

            if (IsBoolean(type))
                return "true";

            if (IsDateTime(type))
                return "DateTime.UtcNow";

            if (IsGuid(type))
                return "Guid.NewGuid()";

            if (IsDictionary(type, out var keyType, out var valueType))
            {
                // Dictionaries are seeded with one predictable entry so Swagger calls
                // return useful sample JSON instead of empty objects.
                return
                    $"new Dictionary<{keyType}, {valueType}> " +
                    $"{{ {{ {Generate(keyType, depth + 1)}, {Generate(valueType, depth + 1)} }} }}";
            }

            if (IsCollection(type, out var innerType))
            {
                // Lists mirror the dictionary behavior: one representative item keeps
                // generated endpoints practical for frontend and QA smoke tests.
                return
                    $"new List<{innerType}> " +
                    $"{{ {Generate(innerType, depth + 1)} }}";
            }

            if (_models.TryGetValue(GetShortName(type), out var model))
            {
                return GenerateObject(model, depth + 1);
            }

            if (IsInterfaceLike(type))
                return "default!";

            return $"new {type}()";
        }

        private string GenerateObject(
            ClassDeclarationSyntax model,
            int depth)
        {
            var typeName = model.Identifier.Text;

            var props = model.Members
                .OfType<PropertyDeclarationSyntax>()
                .ToList();

            if (props.Count == 0)
                return $"new {typeName}()";

            var lines = new List<string>();

            foreach (var prop in props)
            {
                var propName = prop.Identifier.Text;
                var propType = prop.Type.ToString();

                var value = Generate(propType, depth);

                lines.Add($"{propName} = {value}");
            }

            return
$@"new {typeName}
{{
    {string.Join("," + Environment.NewLine + "    ", lines)}
}}";
        }

        private string Normalize(string type)
        {
            return type
                .Replace("System.String", "string")
                .Replace("System.Int32", "int")
                .Replace("System.Int64", "long")
                .Replace("System.Boolean", "bool")
                .Replace("System.Double", "double")
                .Replace("System.Decimal", "decimal")
                .Replace("System.Guid", "Guid")
                .Replace("System.DateTime", "DateTime")
                .Trim();
        }

        private bool IsNullable(
            string type,
            out string inner)
        {
            inner = "";

            if (type.EndsWith("?"))
            {
                inner = type[..^1];
                return true;
            }

            if (type.StartsWith("Nullable<") &&
                type.EndsWith(">"))
            {
                inner = type.Substring(9, type.Length - 10);
                return true;
            }

            return false;
        }

        private bool IsString(string type)
        {
            return type == "string";
        }

        private bool IsInteger(string type)
        {
            return type == "int" ||
                   type == "long" ||
                   type == "short" ||
                   type == "byte";
        }

        private bool IsDecimal(string type)
        {
            return type == "double" ||
                   type == "decimal";
        }

        private bool IsFloat(string type)
        {
            return type == "float";
        }

        private bool IsBoolean(string type)
        {
            return type == "bool";
        }

        private bool IsDateTime(string type)
        {
            return type == "DateTime";
        }

        private bool IsGuid(string type)
        {
            return type == "Guid";
        }

        private bool IsCollection(
            string type,
            out string innerType)
        {
            innerType = "";

            if (!(type.StartsWith("ICollection<") ||
                  type.StartsWith("IEnumerable<") ||
                  type.StartsWith("IList<") ||
                  type.StartsWith("List<") ||
                  type.StartsWith("System.Collections.Generic.ICollection<") ||
                  type.StartsWith("System.Collections.Generic.IEnumerable<") ||
                  type.StartsWith("System.Collections.Generic.IList<") ||
                  type.StartsWith("System.Collections.Generic.List<")))
            {
                return false;
            }

            innerType = ExtractInner(type);
            return true;
        }

        private bool IsDictionary(
            string type,
            out string keyType,
            out string valueType)
        {
            keyType = "";
            valueType = "";

            if (!(type.StartsWith("IDictionary<") ||
                  type.StartsWith("Dictionary<") ||
                  type.StartsWith("System.Collections.Generic.IDictionary<") ||
                  type.StartsWith("System.Collections.Generic.Dictionary<")))
            {
                return false;
            }

            var inner = ExtractInner(type);
            var parts = SplitGenericArguments(inner);

            if (parts.Count != 2)
                return false;

            keyType = parts[0].Trim();
            valueType = parts[1].Trim();

            return true;
        }

        private string ExtractInner(string type)
        {
            var start = type.IndexOf('<');
            var end = type.LastIndexOf('>');

            return type.Substring(start + 1, end - start - 1);
        }

        private List<string> SplitGenericArguments(string input)
        {
            var result = new List<string>();
            var depth = 0;
            var start = 0;

            for (int i = 0; i < input.Length; i++)
            {
                var ch = input[i];

                if (ch == '<')
                    depth++;
                else if (ch == '>')
                    depth--;
                else if (ch == ',' && depth == 0)
                {
                    result.Add(input.Substring(start, i - start));
                    start = i + 1;
                }
            }

            result.Add(input.Substring(start));
            return result;
        }

        private bool IsInterfaceLike(string type)
        {
            if (type.StartsWith("I") &&
                type.Length > 1 &&
                char.IsUpper(type[1]))
                return true;

            if (type.StartsWith("System.Collections.Generic.I"))
                return true;

            return false;
        }

        private string GetShortName(string type)
        {
            var lastDot = type.LastIndexOf('.');

            return lastDot >= 0
                ? type[(lastDot + 1)..]
                : type;
        }
    }
}
