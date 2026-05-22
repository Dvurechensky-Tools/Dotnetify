/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 22 мая 2026 12:01:46
 * Version: 1.0.39
 */

using System.Text;

namespace Dotnetify.Processors.PackageResolve.Core
{
    /// <summary>
    /// Repeatedly builds the generated project, resolves missing references to NuGet
    /// packages, and patches the .csproj until the build succeeds or the loop gives up.
    /// </summary>
    public class PackageResolverOrchestrator
    {
        private readonly DotnetBuildRunner _buildRunner;
        private readonly BuildErrorParser _errorParser;
        private readonly NuGetPackageResolver _nugetResolver;
        private readonly CsprojEditor _csprojEditor;

        /// <summary>Creates an orchestrator from independently testable package-resolve services.</summary>
        public PackageResolverOrchestrator(
            DotnetBuildRunner buildRunner,
            BuildErrorParser errorParser,
            NuGetPackageResolver nugetResolver,
            CsprojEditor csprojEditor)
        {
            _buildRunner = buildRunner;
            _errorParser = errorParser;
            _nugetResolver = nugetResolver;
            _csprojEditor = csprojEditor;
        }

        private void LogUnresolved(
            string projectDir,
            int iteration,
            List<MissingReference> missingRefs)
        {
            var path = Path.Combine(projectDir, "package-resolve.log");

            var sb = new StringBuilder();

            sb.AppendLine($"=== Iteration {iteration} ===");
            sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            foreach (var item in missingRefs)
                sb.AppendLine($"{item.Kind}: {item.Value}");

            sb.AppendLine();

            File.AppendAllText(path, sb.ToString());
        }

        /// <summary>
        /// Runs the build/parse/resolve/patch loop for the generated project.
        /// </summary>
        public async Task ResolveAndPatchAsync(
            string projectDir,
            string projectFile,
            int maxIterations = 5,
            CancellationToken cancellationToken = default)
        {
            var alreadyAdded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var reservedPackageIds = _csprojEditor.GetReservedPackageIds(projectFile);

            for (int i = 0; i < maxIterations; i++)
            {
                var build = await _buildRunner.BuildAsync(projectDir, cancellationToken);

                if (build.ExitCode == 0)
                    return;

                var missingRefs = _errorParser.ParseMissingReferences(build.StdOut + Environment.NewLine + build.StdErr);

                if (missingRefs.Count == 0)
                {
                    throw new InvalidOperationException(
                        build.StdOut + Environment.NewLine + build.StdErr);
                }

                var resolvedPackages = new List<ResolvedPackage>();

                foreach (var missing in missingRefs)
                {
                    var result = await _nugetResolver.ResolveAsync(
                        missing,
                        reservedPackageIds,
                        cancellationToken);
                    if (result is null)
                        continue;

                    if (alreadyAdded.Add(result.PackageId))
                    {
                        resolvedPackages.Add(result);
                        reservedPackageIds.Add(result.PackageId);
                    }
                }

                if (resolvedPackages.Count == 0)
                {
                    LogUnresolved(projectDir, i + 1, missingRefs);
                    continue;
                }

                _csprojEditor.AddPackages(projectFile, resolvedPackages);
            }

            throw new InvalidOperationException("Package resolve loop exceeded max iterations.");
        }
    }
}
