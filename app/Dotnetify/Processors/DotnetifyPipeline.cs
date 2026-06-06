/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 06 июня 2026 09:07:36
 * Version: 1.0.54
 */

using Dotnetify.Models;

using NSwag;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp.Models;

namespace Dotnetify.Processors
{
    /// <summary>
    /// Coordinates the high-level generation flow: load OpenAPI, ask NSwag for raw C#,
    /// then pass the output through Dotnetify processors.
    /// </summary>
    public class DotnetifyPipeline
    {
        private readonly DotnetifyConfig _config;

        /// <summary>Creates a pipeline configured for a single generation run.</summary>
        public DotnetifyPipeline(DotnetifyConfig config)
        {
            _config = config;
        }

        /// <summary>Executes NSwag generation and all configured post-processing stages.</summary>
        public async Task RunAsync()
        {
            var document = await OpenApiDocument.FromFileAsync(_config.InputPath);

            var generator = new CSharpControllerGenerator(
                document,
                new CSharpControllerGeneratorSettings
                {
                    ControllerStyle = CSharpControllerStyle.Abstract,
                    GenerateModelValidationAttributes = true
                });

            var code = generator.GenerateFile();

            var context = new DotnetifyContext
            {
                Document = document,
                GeneratedCode = code,
                OutputPath = _config.OutputPath,
                ProjectName = _config.ProjectName,
                Namespace = _config.Namespace
            };

            foreach (var processor in _config.Processors)
            {
                Console.WriteLine($"[Dotnetify] Running: {processor.Name}");
                await processor.ProcessAsync(context);
            }
        }
    }
}
