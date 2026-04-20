/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 20 апреля 2026 16:38:29
 * Version: 1.0.1
 */

using Dotnetify.Processors;

namespace Dotnetify.Models
{
    public class DotnetifyConfig
    {
        public string InputPath { get; set; } = "Input/swagger.json";
        public string OutputPath { get; set; } = "Out";

        public string Namespace { get; set; } = "Generated";

        public bool GenerateControllers { get; set; } = true;
        public bool GenerateModels { get; set; } = true;

        public List<IDotnetifyProcessor> Processors { get; set; } = new();
    }
}
