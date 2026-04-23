/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 23 апреля 2026 07:08:31
 * Version: 1.0.4
 */

namespace Dotnetify.Processors
{
    public interface IDotnetifyProcessor
    {
        string Name { get; }

        Task ProcessAsync(DotnetifyContext context);
    }
}
