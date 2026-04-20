/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 20 апреля 2026 05:05:56
 * Version: 1.0.
 */

namespace Dotnetify.Processors
{
    public interface IDotnetifyProcessor
    {
        string Name { get; }

        Task ProcessAsync(DotnetifyContext context);
    }
}
