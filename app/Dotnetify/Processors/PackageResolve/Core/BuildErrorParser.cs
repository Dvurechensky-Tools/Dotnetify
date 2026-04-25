/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 25 апреля 2026 08:32:03
 * Version: 1.0.6
 */

using System.Text.RegularExpressions;

namespace Dotnetify.Processors.PackageResolve.Core
{
    public enum MissingReferenceKind
    {
        TypeOrNamespace,
        NamespaceMember
    }

    public class MissingReference
    {
        public MissingReferenceKind Kind { get; init; }
        public string Value { get; init; } = string.Empty;
        public string? ParentNamespace { get; init; }
    }

    public class BuildErrorParser
    {
        // CS0246: The type or namespace name 'JsonPropertyAttribute' could not be found...
        private static readonly Regex MissingTypeRegex = new(
            @"error\s+CS0246:.*?'(?<name>[^']+)'\s+could not be found",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // CS0234: The type or namespace name 'Json' does not exist in the namespace 'Newtonsoft'
        private static readonly Regex MissingNamespaceMemberRegex = new(
            @"error\s+CS0234:.*?'(?<member>[^']+)'\s+does not exist in the namespace\s+'(?<parent>[^']+)'",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public List<MissingReference> ParseMissingReferences(string buildOutput)
        {
            var result = new List<MissingReference>();
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match match in MissingTypeRegex.Matches(buildOutput))
            {
                var value = match.Groups["name"].Value.Trim();
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var key = $"TYPE::{value}";
                if (unique.Add(key))
                {
                    result.Add(new MissingReference
                    {
                        Kind = MissingReferenceKind.TypeOrNamespace,
                        Value = value
                    });
                }
            }

            foreach (Match match in MissingNamespaceMemberRegex.Matches(buildOutput))
            {
                var member = match.Groups["member"].Value.Trim();
                var parent = match.Groups["parent"].Value.Trim();

                if (string.IsNullOrWhiteSpace(member) || string.IsNullOrWhiteSpace(parent))
                    continue;

                var full = $"{parent}.{member}";
                var key = $"NS::{full}";
                if (unique.Add(key))
                {
                    result.Add(new MissingReference
                    {
                        Kind = MissingReferenceKind.NamespaceMember,
                        Value = full,
                        ParentNamespace = parent
                    });
                }
            }

            return result;
        }
    }
}