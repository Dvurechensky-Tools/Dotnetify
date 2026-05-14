/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 14 мая 2026 10:56:15
 * Version: 1.0.32
 */


using Dotnetify.Processors.Dotnet.Core;

namespace Dotnetify.Processors.Dotnet
{
    /// <summary>
    /// Creates the physical .NET web project files around the code emitted by the
    /// Roslyn processors.
    /// </summary>
    public class DotnetProjectProcessor : IDotnetifyProcessor
    {
        /// <inheritdoc />
        public string Name => "Dotnet Project Processor";

        /// <summary>Writes the .csproj, Program.cs, and minimal appsettings.json.</summary>
        public Task ProcessAsync(DotnetifyContext context)
        {
            var projectDir = Path.Combine(context.OutputPath, context.ProjectName);

            var generator = new DotnetProjectGenerator();

            generator.Generate(projectDir, context.ProjectName);
            generator.GenerateProgram(projectDir);

            File.WriteAllText(
                Path.Combine(projectDir, "appsettings.json"),
                "{}"
            );

            return Task.CompletedTask;
        }
    }
}
