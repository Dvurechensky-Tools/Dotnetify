/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 20 апреля 2026 16:38:29
 * Version: 1.0.1
 */


using Dotnetify.Processors.Dotnet.Core;

namespace Dotnetify.Processors.Dotnet
{
    public class DotnetProjectProcessor : IDotnetifyProcessor
    {
        public string Name => "Dotnet Project Processor";

        public Task ProcessAsync(DotnetifyContext context)
        {
            var projectDir = Path.Combine(context.OutputPath, "GeneratedApi");

            var generator = new DotnetProjectGenerator();

            generator.Generate(projectDir, "GeneratedApi");
            generator.GenerateProgram(projectDir);

            File.WriteAllText(
                Path.Combine(projectDir, "appsettings.json"),
                "{}"
            );

            return Task.CompletedTask;
        }
    }
}
