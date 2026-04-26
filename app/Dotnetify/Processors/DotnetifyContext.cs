/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 26 апреля 2026 10:11:16
 * Version: 1.0.7
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
