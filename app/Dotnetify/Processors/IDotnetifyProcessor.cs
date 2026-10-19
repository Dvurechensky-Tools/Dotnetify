/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 19 октября 2026 09:58:57
 * Version: 1.0.186
 */

namespace Dotnetify.Processors
{
    /// <summary>
    /// Contract for a pipeline stage that mutates or enriches the generated project.
    /// Implementations are executed in the order provided by <see cref="Models.DotnetifyConfig"/>.
    /// </summary>
    public interface IDotnetifyProcessor
    {
        /// <summary>Human-readable stage name printed during generation.</summary>
        string Name { get; }

        /// <summary>Runs the stage against the current generation context.</summary>
        Task ProcessAsync(DotnetifyContext context);
    }
}
