/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 20 апреля 2026 05:05:56
 * Version: 1.0.
 */

using Dotnetify.Models;

using NSwag;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp.Models;

namespace Dotnetify.Processors
{
    public class DotnetifyPipeline
    {
        private readonly DotnetifyConfig _config;

        public DotnetifyPipeline(DotnetifyConfig config)
        {
            _config = config;
        }

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
