/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 25 апреля 2026 08:32:03
 * Version: 1.0.6
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
