/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 24 апреля 2026 07:12:04
 * Version: 1.0.5
 */

using Dotnetify.Models;
using Dotnetify.Processors.Dotnet.Core;
using Dotnetify.Processors.PackageResolve.Core;

namespace Dotnetify.Processors.PackageResolve
{
    public class PackageResolveProcessor : IDotnetifyProcessor
    {
        public string Name => "Package Resolve Processor";

        public async Task ProcessAsync(DotnetifyContext context)
        {
            var projectDir = Path.Combine(context.OutputPath, "GeneratedApi");
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