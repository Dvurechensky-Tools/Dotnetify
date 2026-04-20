/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 20 апреля 2026 16:38:29
 * Version: 1.0.1
 */

namespace Dotnetify.Processors
{
    public interface IDotnetifyProcessor
    {
        string Name { get; }

        Task ProcessAsync(DotnetifyContext context);
    }
}
