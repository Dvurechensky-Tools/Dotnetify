/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 22 апреля 2026 18:58:58
 * Version: 1.0.3
 */

namespace Dotnetify.Processors
{
    public interface IDotnetifyProcessor
    {
        string Name { get; }

        Task ProcessAsync(DotnetifyContext context);
    }
}
