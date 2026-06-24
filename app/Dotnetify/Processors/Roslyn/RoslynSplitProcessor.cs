/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 24 июня 2026 10:56:23
 * Version: 1.0.72
 */

using Dotnetify.Models;
using Dotnetify.Processors.Roslyn.Core;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dotnetify.Processors.Roslyn
{
    /// <summary>
    /// Splits NSwag's single generated C# file into Dotnetify's project layout:
    /// controllers, models, enums, and common helper types.
    /// </summary>
    public class RoslynSplitProcessor : IDotnetifyProcessor
    {
        /// <inheritdoc />
        public string Name => "Roslyn Split Processor";

        private ControllerLogic _controller;
        private ModelLogic _model;

        /// <summary>
        /// Builds model lookup metadata, initializes Roslyn writers, and emits files
        /// under the generated project directory.
        /// </summary>
        public Task ProcessAsync(DotnetifyContext context)
        {
            var root = CSharpSyntaxTree
                .ParseText(context.GeneratedCode)
                .GetCompilationUnitRoot();

            var config = new DotnetifyConfig
            {
                Namespace = context.Namespace
            };

            var models = new Dictionary<string, ClassDeclarationSyntax>();

            foreach (var node in root.DescendantNodes())
            {
                if (node is ClassDeclarationSyntax cls)
                {
                    var isController =
                        cls.BaseList?.Types.Any(t =>
                            t.ToString().Contains("ControllerBase")) == true;

                    if (!isController)
                    {
                        models[cls.Identifier.Text] = cls;
                    }
                }
            }

            _controller = new ControllerLogic(config, models);
            _model = new ModelLogic(config);

            Process(root, Path.Combine(context.OutputPath, context.ProjectName));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Writes controller, model, enum, and common files from the raw compilation unit.
        /// </summary>
        public void Process(CompilationUnitSyntax root, string outputDir)
        {
            Directory.CreateDirectory(outputDir);
            Directory.CreateDirectory($"{outputDir}/Controllers");
            Directory.CreateDirectory($"{outputDir}/Models");
            Directory.CreateDirectory($"{outputDir}/Enums");
            Directory.CreateDirectory($"{outputDir}/Common");

            foreach (var node in root.DescendantNodes())
            {
                if (node is ClassDeclarationSyntax cls)
                {
                    if (_controller.IsController(cls))
                        _controller.Process(cls, outputDir);
                    else
                        _model.Process(cls, outputDir);
                }

                if (node is EnumDeclarationSyntax en)
                {
                    _model.ProcessEnum(en, outputDir);
                }
            }
        }
    }
}
