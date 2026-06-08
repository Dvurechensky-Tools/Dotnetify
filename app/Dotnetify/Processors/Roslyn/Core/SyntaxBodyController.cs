/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 08 июня 2026 07:13:12
 * Version: 1.0.56
 */

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dotnetify.Processors.Roslyn.Core
{
    /// <summary>
    /// Builds concrete method bodies for generated controllers based on each NSwag
    /// method's return type.
    /// </summary>
    public class SyntaxBodyController
    {
        private readonly ResponseObjectGenerator _generator;

        /// <summary>Creates a method body generator backed by model-aware response mocks.</summary>
        public SyntaxBodyController(
            Dictionary<string, ClassDeclarationSyntax> models)
        {
            _generator = new ResponseObjectGenerator(models);
        }

        /// <summary>Generates a compilable block for an abstract controller method.</summary>
        public BlockSyntax GenerateBody(MethodDeclarationSyntax method)
        {
            var returnType = method.ReturnType.ToString().Trim();

            if (IsPlainTask(returnType))
                return ReturnStatement("return Task.CompletedTask;");

            if (TryGetTaskInnerType(returnType, out var innerType))
            {
                var response = _generator.Generate(innerType);

                return ReturnStatement($@"
var response = {response};
return Task.FromResult<{innerType}>(response);
");
            }

            if (IsActionResult(returnType))
            {
                return ReturnStatement(@"
var response = new { success = true };
return Ok(response);
");
            }

            var plain = _generator.Generate(returnType);

            return ReturnStatement($@"
var response = {plain};
return response;
");
        }

        private BlockSyntax ReturnStatement(string code)
        {
            return SyntaxFactory.Block(
                SyntaxFactory.ParseStatement(code)
            );
        }

        private bool IsPlainTask(string type)
        {
            return type == "Task" ||
                   type == "System.Threading.Tasks.Task";
        }

        private bool TryGetTaskInnerType(string type, out string innerType)
        {
            innerType = string.Empty;

            if (!type.StartsWith("Task<") &&
                !type.StartsWith("System.Threading.Tasks.Task<"))
            {
                return false;
            }

            var start = type.IndexOf('<');
            var end = type.LastIndexOf('>');

            if (start < 0 || end <= start)
                return false;

            innerType = type.Substring(start + 1, end - start - 1).Trim();
            return true;
        }

        private bool IsActionResult(string type)
        {
            return type.Contains("IActionResult") ||
                   type.Contains("ActionResult");
        }

        private string GenerateMock(string type)
        {
            type = type.Trim();

            if (IsString(type))
                return "\"test\"";

            if (IsInteger(type))
                return "1";

            // Keep this in sync with ResponseObjectGenerator: float literals need the
            // suffix or generated nullable float responses fail compilation.
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
                return
                    $"new Dictionary<{keyType}, {valueType}> " +
                    $"{{ {{ {GenerateMock(keyType)}, {GenerateMock(valueType)} }} }}";
            }

            if (IsCollection(type, out var innerType))
            {
                return
                    $"new List<{innerType}> " +
                    $"{{ {GenerateMock(innerType)} }}";
            }

            if (IsInterfaceLike(type))
                return "default!";

            return $"new {type}()";
        }

        private bool IsString(string type)
        {
            return type == "string" || type == "String";
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
            return type == "DateTime" ||
                   type == "System.DateTime";
        }

        private bool IsGuid(string type)
        {
            return type == "Guid" ||
                   type == "System.Guid";
        }

        private bool IsCollection(string type, out string innerType)
        {
            innerType = string.Empty;

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

            var start = type.IndexOf('<');
            var end = type.LastIndexOf('>');

            if (start < 0 || end <= start)
                return false;

            innerType = type.Substring(start + 1, end - start - 1).Trim();
            return true;
        }

        private bool IsDictionary(string type, out string keyType, out string valueType)
        {
            keyType = string.Empty;
            valueType = string.Empty;

            if (!(type.StartsWith("IDictionary<") ||
                  type.StartsWith("Dictionary<") ||
                  type.StartsWith("System.Collections.Generic.IDictionary<") ||
                  type.StartsWith("System.Collections.Generic.Dictionary<")))
            {
                return false;
            }

            var start = type.IndexOf('<');
            var end = type.LastIndexOf('>');

            if (start < 0 || end <= start)
                return false;

            var args = type.Substring(start + 1, end - start - 1);
            var parts = SplitGenericArguments(args);

            if (parts.Count != 2)
                return false;

            keyType = parts[0].Trim();
            valueType = parts[1].Trim();
            return true;
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
            if (type.StartsWith("I") && type.Length > 1 && char.IsUpper(type[1]))
                return true;

            if (type.StartsWith("System.Collections.Generic.I"))
                return true;

            return false;
        }
    }
}
