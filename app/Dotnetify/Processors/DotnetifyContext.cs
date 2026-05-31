/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 31 мая 2026 15:11:33
 * Version: 1.0.48
 */

using NSwag;

namespace Dotnetify.Processors
{
    /// <summary>
    /// Carries shared state between pipeline processors during a single generation run.
    /// Processors should add cross-cutting data to <see cref="Items"/> rather than
    /// depending on each other's concrete implementation details.
    /// </summary>
    public class DotnetifyContext
    {
        /// <summary>Parsed OpenAPI document loaded from the configured input file.</summary>
        public OpenApiDocument Document { get; set; }

        /// <summary>Raw C# controller/model file emitted by NSwag before Dotnetify restructuring.</summary>
        public string GeneratedCode { get; set; } = "";

        /// <summary>Base output directory for the generated project.</summary>
        public string OutputPath { get; set; } = "";

        /// <summary>Generated project folder and assembly name.</summary>
        public string ProjectName { get; set; } = "GeneratedApi";

        /// <summary>Root namespace used by generated C# files.</summary>
        public string Namespace { get; set; } = "";

        /// <summary>Extension bag for future processors that need to exchange optional metadata.</summary>
        public Dictionary<string, object> Items { get; } = new();
    }
}
