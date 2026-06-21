/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 21 июня 2026 07:11:24
 * Version: 1.0.69
 */

using Dotnetify.Processors.Roslyn.Core;
using Dotnetify.Models;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Xunit;

namespace Dotnetify.Tests;

/// <summary>
/// Regression tests for syntax simplification that turns fully-qualified NSwag output
/// into clean C# without corrupting imports or namespaces.
/// </summary>
public class RoslynUtilsTests
{
    [Fact]
    public void ForceSimplify_PreservesExistingMvcUsing()
    {
        // Regression guard: ForceSimplify must not turn Microsoft.AspNetCore.Mvc
        // into Microsoft.AspNetCore, because MVC attributes live in the full namespace.
        const string source = """
            using VkPublic;
            using Microsoft.AspNetCore.Mvc;
            using System.Threading;
            using System.Threading.Tasks;

            [ApiController]
            [Route("VkPublic")]
            public class VkPublicController : ControllerBase
            {
                [HttpGet("")]
                public Task<object> AnonymousGET()
                {
                    var response = new object();
                    return Task.FromResult<object>(response);
                }
            }
            """;

        var result = Simplify(source);

        Assert.Contains("using Microsoft.AspNetCore.Mvc;", result);
        Assert.DoesNotContain("using Microsoft.AspNetCore;", result);
        Assert.Contains("public class VkPublicController : ControllerBase", result);
    }

    [Fact]
    public void ForceSimplify_SimplifiesFullyQualifiedTaskTypeAndAddsUsing()
    {
        const string source = """
            public class SampleController
            {
                public System.Threading.Tasks.Task<object> Get()
                {
                    return System.Threading.Tasks.Task.FromResult<object>(new object());
                }
            }
            """;

        var result = Simplify(source);

        Assert.Contains("using System.Threading.Tasks;", result);
        Assert.Contains("public Task<object> Get()", result);
        Assert.Contains("return Task.FromResult<object>", result);
        Assert.DoesNotContain("System.Threading.Tasks.Task", result);
    }

    [Fact]
    public void ForceSimplify_SimplifiesFullyQualifiedMvcTypeToMvcNamespace()
    {
        const string source = """
            public class SampleController : Microsoft.AspNetCore.Mvc.ControllerBase
            {
            }
            """;

        var result = Simplify(source);

        Assert.Contains("using Microsoft.AspNetCore.Mvc;", result);
        Assert.DoesNotContain("using Microsoft.AspNetCore;", result);
        Assert.Contains("public class SampleController : ControllerBase", result);
    }

    [Fact]
    public void ForceSimplify_DoesNotShortenUsingDirectives()
    {
        const string source = """
            using System.Threading.Tasks;

            public class SampleController
            {
                public Task<object> Get()
                {
                    return Task.FromResult<object>(new object());
                }
            }
            """;

        var result = Simplify(source);

        Assert.Contains("using System.Threading.Tasks;", result);
        Assert.DoesNotContain("using System.Threading;", result);
    }

    [Fact]
    public void ForceSimplify_DoesNotShortenNamespaceDeclarations()
    {
        // Namespace declarations look like qualified names in Roslyn, but shortening
        // them would move generated types into the wrong namespace.
        const string source = """
            namespace My.Product.Api
            {
                public class Sample
                {
                }
            }
            """;

        var result = Simplify(source);

        Assert.Contains("namespace My.Product.Api", result);
        Assert.DoesNotContain("namespace Api", result);
        Assert.DoesNotContain("using My.Product;", result);
    }

    [Fact]
    public void ForceSimplify_DoesNotShortenFileScopedNamespaceDeclarations()
    {
        const string source = """
            namespace My.Product.Api;

            public class Sample
            {
            }
            """;

        var result = Simplify(source);

        Assert.Contains("namespace My.Product.Api;", result);
        Assert.DoesNotContain("namespace Api;", result);
        Assert.DoesNotContain("using My.Product;", result);
    }

    [Fact]
    public void ForceSimplify_SimplifiesQualifiedModelTypesInsideNamespace()
    {
        // Real NSwag models often combine namespace blocks with fully-qualified
        // collection and Newtonsoft attributes. Dotnetify should keep the namespace
        // intact while moving those dependencies to using directives.
        const string source = """
            namespace VkPublic
            {
                public partial class Asset
                {
                    public string? Animation_url { get; set; }
                    public System.Collections.Generic.List<Images4> Images { get; set; }
                    public Title2 Title { get; set; }
                    public Title_color Title_color { get; set; }

                    private System.Collections.Generic.IDictionary<string, object> _additionalProperties;

                    [Newtonsoft.Json.JsonExtensionData]
                    public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
                    {
                        get
                        {
                            return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>());
                        }

                        set
                        {
                            _additionalProperties = value;
                        }
                    }
                }
            }
            """;

        var result = Simplify(source);

        Assert.Contains("using Newtonsoft.Json;", result);
        Assert.Contains("using System.Collections.Generic;", result);
        Assert.Contains("namespace VkPublic", result);
        Assert.Contains("[JsonExtensionData]", result);
        Assert.Contains("public List<Images4> Images { get; set; }", result);
        Assert.Contains("private IDictionary<string, object> _additionalProperties;", result);
        Assert.Contains("public IDictionary<string, object> AdditionalProperties", result);
        Assert.Contains("new Dictionary<string, object>()", result);
        Assert.DoesNotContain("Newtonsoft.Json.JsonExtensionData", result);
        Assert.DoesNotContain("System.Collections.Generic.List", result);
        Assert.DoesNotContain("System.Collections.Generic.IDictionary", result);
        Assert.DoesNotContain("System.Collections.Generic.Dictionary", result);
    }

    [Fact]
    public void ForceSimplify_SimplifiesQualifiedTypesInsideGenericArguments()
    {
        const string source = """
            using System.Threading.Tasks;

            public class StoreController
            {
                public Task<System.Collections.Generic.IDictionary<string, int>> GetInventory()
                {
                    var response = new System.Collections.Generic.Dictionary<string, int>();
                    return Task.FromResult<System.Collections.Generic.IDictionary<string, int>>(response);
                }
            }
            """;

        var result = Simplify(source);

        Assert.Contains("using System.Collections.Generic;", result);
        Assert.Contains("public Task<IDictionary<string, int>> GetInventory()", result);
        Assert.Contains("new Dictionary<string, int>()", result);
        Assert.Contains("Task.FromResult<IDictionary<string, int>>(response)", result);
        Assert.DoesNotContain("System.Collections.Generic.IDictionary", result);
        Assert.DoesNotContain("System.Collections.Generic.Dictionary", result);
    }

    [Fact]
    public void ForceSimplify_SimplifiesGeneratedControllerReturnTypes()
    {
        const string source = """
            using CleanCheckApi;
            using Microsoft.AspNetCore.Mvc;
            using Microsoft.AspNetCore.Mvc.ModelBinding;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            [ApiController]
            [Route("store")]
            public class StoreController : ControllerBase
            {
                [HttpGet("inventory")]
                public Task<System.Collections.Generic.IDictionary<string, int>> GetInventory()
                {
                    var response = new Dictionary<string, int>
                    {
                        {
                            "test",
                            1
                        }
                    };
                    return Task.FromResult<IDictionary<string, int>>(response);
                }
            }
            """;

        var result = Simplify(source);

        Assert.Contains("using Microsoft.AspNetCore.Mvc;", result);
        Assert.Contains("using System.Collections.Generic;", result);
        Assert.Contains("public Task<IDictionary<string, int>> GetInventory()", result);
        Assert.DoesNotContain("Task<System.Collections.Generic.IDictionary", result);
    }

    [Fact]
    public void ForceSimplify_RemovesBlockedMvcUsingWhenSourceUsesMvcAlias()
    {
        const string source = """
            public class SampleController : Mvc.ControllerBase
            {
            }
            """;

        var result = Simplify(source);

        Assert.DoesNotContain("using Mvc;", result);
        Assert.Contains("public class SampleController : ControllerBase", result);
    }

    [Fact]
    public void ForceSimplify_DoesNotRewriteArbitraryMemberAccessChains()
    {
        // Only namespace-qualified static calls should be simplified. Runtime object
        // access chains must remain untouched even if they have several segments.
        const string source = """
            public class Sample
            {
                public object Get()
                {
                    return client.Response.Metadata.Value;
                }
            }
            """;

        var result = Simplify(source);

        Assert.Contains("return client.Response.Metadata.Value;", result);
        Assert.DoesNotContain("using client.Response;", result);
    }

    private static string Simplify(string source)
    {
        var root = CSharpSyntaxTree
            .ParseText(source)
            .GetCompilationUnitRoot();

        return RoslynUtils.ForceSimplify(root);
    }
}
