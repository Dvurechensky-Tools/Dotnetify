/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 21 апреля 2026 07:09:27
 * Version: 1.0.2
 */

namespace Dotnetify.Processors
{
    public interface IDotnetifyProcessor
    {
        string Name { get; }

        Task ProcessAsync(DotnetifyContext context);
    }
}
