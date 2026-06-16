/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 16 июня 2026 07:13:05
 * Version: 1.0.64
 */

using Dotnetify.Processors;

namespace Dotnetify.Models
{
    /// <summary>
    /// Defines a single Dotnetify generation run: input document, output identity,
    /// and the ordered processors that transform NSwag output into a runnable project.
    /// </summary>
    public class DotnetifyConfig
    {
        /// <summary>Path to the OpenAPI/Swagger document that will be converted.</summary>
        public string InputPath { get; set; } = "Input/swagger.json";

        /// <summary>Base output directory where the generated project folder is created.</summary>
        public string OutputPath { get; set; } = "Out";

        /// <summary>Name of the generated project folder and .csproj file.</summary>
        public string ProjectName { get; set; } = "GeneratedApi";

        /// <summary>Root C# namespace used for generated models and controller references.</summary>
        public string Namespace { get; set; } = "Generated";

        /// <summary>Reserved switch for controller generation modes.</summary>
        public bool GenerateControllers { get; set; } = true;

        /// <summary>Reserved switch for model generation modes.</summary>
        public bool GenerateModels { get; set; } = true;

        /// <summary>Ordered pipeline stages executed after NSwag produces raw C# code.</summary>
        public List<IDotnetifyProcessor> Processors { get; set; } = new();
    }
}
