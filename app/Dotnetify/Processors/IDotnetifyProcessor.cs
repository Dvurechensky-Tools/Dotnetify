/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 25 мая 2026 11:33:49
 * Version: 1.0.42
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
