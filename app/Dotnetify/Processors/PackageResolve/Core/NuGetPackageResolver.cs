/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 09 мая 2026 08:13:41
 * Version: 1.0.27
 */

using System.Text.Json;

namespace Dotnetify.Processors.PackageResolve.Core
{
    /// <summary>Package candidate selected for insertion into a generated .csproj.</summary>
    public class ResolvedPackage
    {
        public string PackageId { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
    }

    /// <summary>
    /// Searches NuGet for packages likely to provide a missing type or namespace.
    /// </summary>
    public class NuGetPackageResolver
    {
        private const string ServiceIndexUrl = "https://api.nuget.org/v3/index.json";

        private readonly HttpClient _httpClient;
        private string? _searchServiceUrl;

        /// <summary>Creates a resolver that uses the supplied HTTP client for NuGet V3 calls.</summary>
        public NuGetPackageResolver(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>Attempts to resolve a compiler missing-reference diagnostic to a package.</summary>
        public async Task<ResolvedPackage?> ResolveAsync(
            MissingReference missingReference,
            IEnumerable<string>? excludedPackageIds = null,
            CancellationToken cancellationToken = default)
        {
            var candidates = BuildQueries(missingReference);
            var excluded = excludedPackageIds is null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(excludedPackageIds, StringComparer.OrdinalIgnoreCase);

            foreach (var query in candidates)
            {
                var package = await SearchPackageAsync(query, excluded, cancellationToken);
                if (package is not null)
                    return package;
            }

            return null;
        }

        private IEnumerable<string> BuildQueries(MissingReference missingReference)
        {
            yield return missingReference.Value;

            if (missingReference.Value.EndsWith("Attribute", StringComparison.Ordinal))
            {
                yield return missingReference.Value[..^"Attribute".Length];
            }

            if (missingReference.Kind == MissingReferenceKind.NamespaceMember)
            {
                var parts = missingReference.Value.Split('.');
                if (parts.Length > 1)
                {
                    yield return string.Join(".", parts.Take(2));
                    yield return parts[0];
                }
            }
        }

        private async Task<ResolvedPackage?> SearchPackageAsync(
            string query,
            ISet<string> excludedPackageIds,
            CancellationToken cancellationToken)
        {
            var searchUrl = await GetSearchServiceUrlAsync(cancellationToken);
            var url = $"{searchUrl}?q={Uri.EscapeDataString(query)}&prerelease=false&take=10&semVerLevel=2.0.0";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return null;

            var ranked = new List<(string Id, string Version, long Downloads, int Score)>();

            foreach (var item in data.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                var version = item.TryGetProperty("version", out var verProp) ? verProp.GetString() : null;
                var downloads = item.TryGetProperty("totalDownloads", out var downProp) && downProp.TryGetInt64(out var dl)
                    ? dl
                    : 0;

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version))
                    continue;

                if (excludedPackageIds.Contains(id))
                    continue;

                var score = ScorePackage(id!, query);

                ranked.Add((id!, version!, downloads, score));
            }

            var best = ranked
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Downloads)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(best.Id))
                return null;

            return new ResolvedPackage
            {
                PackageId = best.Id,
                Version = best.Version,
                Reason = query
            };
        }

        private static int ScorePackage(string packageId, string query)
        {
            var score = 0;

            if (packageId.Equals(query, StringComparison.OrdinalIgnoreCase))
                score += 1000;

            if (packageId.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                score += 500;

            if (packageId.Contains(query, StringComparison.OrdinalIgnoreCase))
                score += 200;

            // Newtonsoft.Json <- Newtonsoft.Json.JsonProperty
            var queryParts = query.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (queryParts.Length > 1)
            {
                var prefix = string.Join(".", queryParts.Take(2));
                if (packageId.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                    score += 800;
            }

            return score;
        }

        private async Task<string> GetSearchServiceUrlAsync(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_searchServiceUrl))
                return _searchServiceUrl;

            using var response = await _httpClient.GetAsync(ServiceIndexUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var resources = document.RootElement.GetProperty("resources");

            foreach (var resource in resources.EnumerateArray())
            {
                if (!resource.TryGetProperty("@type", out var typeProp))
                    continue;

                var type = typeProp.GetString();
                if (type is null)
                    continue;

                if (!type.StartsWith("SearchQueryService", StringComparison.OrdinalIgnoreCase))
                    continue;

                var id = resource.GetProperty("@id").GetString();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    _searchServiceUrl = id;
                    return _searchServiceUrl;
                }
            }

            throw new InvalidOperationException("NuGet SearchQueryService endpoint was not found.");
        }
    }
}
