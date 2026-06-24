/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 24 июня 2026 10:56:23
 * Version: 1.0.72
 */

using System.Net;
using System.Text;

using Dotnetify.Processors.PackageResolve.Core;

using Xunit;

namespace Dotnetify.Tests;

/// <summary>
/// Regression tests for NuGet package resolution safeguards that must not create
/// self-referencing PackageReference entries.
/// </summary>
public class PackageResolveRegressionTests
{
    [Fact]
    public void AddPackages_DoesNotAddPackageMatchingProjectIdentity()
    {
        var projectDir = Path.Combine(Path.GetTempPath(), "Dotnetify.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDir);
        var projectFile = Path.Combine(projectDir, "Rust.csproj");

        try
        {
            File.WriteAllText(projectFile, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
                  </ItemGroup>
                </Project>
                """);

            var editor = new CsprojEditor();

            editor.AddPackages(projectFile, new[]
            {
                new ResolvedPackage { PackageId = "Rust", Version = "0.11.20140519", Reason = "Rust" },
                new ResolvedPackage { PackageId = "RustLang", Version = "1.2.3", Reason = "Rust" }
            });

            var contents = File.ReadAllText(projectFile);

            Assert.DoesNotContain("""<PackageReference Include="Rust" Version="0.11.20140519" />""", contents);
            Assert.Contains("""<PackageReference Include="RustLang" Version="1.2.3" />""", contents);
        }
        finally
        {
            if (Directory.Exists(projectDir))
                Directory.Delete(projectDir, recursive: true);
        }
    }

    [Fact]
    public async Task ResolveAsync_SkipsPackageMatchingExcludedProjectName()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler
        {
            ["https://api.nuget.org/v3/index.json"] = """
                {
                  "resources": [
                    {
                      "@id": "https://unit.test/search",
                      "@type": "SearchQueryService"
                    }
                  ]
                }
                """,
            ["https://unit.test/search?q=Rust&prerelease=false&take=10&semVerLevel=2.0.0"] = """
                {
                  "data": [
                    { "id": "Rust", "version": "0.11.20140519", "totalDownloads": 1000 },
                    { "id": "RustLang", "version": "1.2.3", "totalDownloads": 100 }
                  ]
                }
                """
        });

        var resolver = new NuGetPackageResolver(httpClient);

        var result = await resolver.ResolveAsync(
            new MissingReference
            {
                Kind = MissingReferenceKind.TypeOrNamespace,
                Value = "Rust"
            },
            excludedPackageIds: new[] { "Rust" });

        Assert.NotNull(result);
        Assert.Equal("RustLang", result!.PackageId);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _responses = new(StringComparer.Ordinal);

        public string this[string url]
        {
            set => _responses[url] = value;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.ToString() ?? string.Empty;

            if (!_responses.TryGetValue(url, out var responseBody))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    RequestMessage = request
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
                RequestMessage = request
            });
        }
    }
}
