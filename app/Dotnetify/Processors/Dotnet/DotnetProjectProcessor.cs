/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 21 апреля 2026 07:09:27
 * Version: 1.0.2
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
