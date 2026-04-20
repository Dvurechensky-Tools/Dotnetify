/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 20 апреля 2026 05:05:56
 * Version: 1.0.
 */

using NSwag;

namespace Dotnetify.Processors
{
    public class DotnetifyContext
    {
        public OpenApiDocument Document { get; set; }

        public string GeneratedCode { get; set; } = "";

        public string OutputPath { get; set; } = "";

        public string Namespace { get; set; } = "";

        public Dictionary<string, object> Items { get; } = new();
    }
}
