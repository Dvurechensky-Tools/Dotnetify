/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 20 мая 2026 10:25:09
 * Version: 1.0.37
 */

using Dotnetify.Models;
using Dotnetify.Processors.Dotnet.Core;
using Dotnetify.Processors.PackageResolve.Core;

namespace Dotnetify.Processors.PackageResolve
{
    /// <summary>
    /// Builds the generated project and attempts to patch missing package references
    /// discovered from compiler diagnostics.
    /// </summary>
    public class PackageResolveProcessor : IDotnetifyProcessor
    {
        /// <inheritdoc />
        public string Name => "Package Resolve Processor";

        /// <summary>Locates the generated .csproj and runs the package resolve loop.</summary>
        public async Task ProcessAsync(DotnetifyContext context)
        {
            var projectDir = Path.Combine(context.OutputPath, context.ProjectName);
            var projectFile = Directory
                .GetFiles(projectDir, "*.csproj", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (projectFile is null)
                throw new FileNotFoundException("Generated .csproj not found.", projectDir);

            var resolver = new PackageResolverOrchestrator(
                new DotnetBuildRunner(),
                new BuildErrorParser(),
                new NuGetPackageResolver(new HttpClient()),
                new CsprojEditor()
            );

            await resolver.ResolveAndPatchAsync(projectDir, projectFile);
        }
    }
}
