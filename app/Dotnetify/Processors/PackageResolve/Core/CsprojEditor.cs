/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 25 мая 2026 11:33:49
 * Version: 1.0.42
 */

using System.Xml.Linq;

namespace Dotnetify.Processors.PackageResolve.Core
{
    /// <summary>
    /// Performs small, structured edits to generated .csproj files.
    /// </summary>
    public class CsprojEditor
    {
        /// <summary>
        /// Returns package ids that would create a self-reference if inserted into the project.
        /// </summary>
        public ISet<string> GetReservedPackageIds(string projectFile)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Path.GetFileNameWithoutExtension(projectFile)
            };

            var doc = XDocument.Load(projectFile);
            var project = doc.Root;

            if (project is null)
                return ids;

            foreach (var elementName in new[] { "AssemblyName", "PackageId" })
            {
                var value = project
                    .Descendants(elementName)
                    .Select(x => x.Value?.Trim())
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                if (!string.IsNullOrWhiteSpace(value))
                    ids.Add(value);
            }

            return ids;
        }

        /// <summary>Adds missing PackageReference entries while preserving existing references.</summary>
        public void AddPackages(string projectFile, IReadOnlyCollection<ResolvedPackage> packages)
        {
            if (packages.Count == 0)
                return;

            var doc = XDocument.Load(projectFile);
            var project = doc.Root ?? throw new InvalidOperationException("Invalid .csproj");
            var reserved = GetReservedPackageIds(projectFile);

            var existing = project
                .Descendants("PackageReference")
                .Select(x => x.Attribute("Include")?.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            existing.UnionWith(reserved);

            var itemGroup = project.Elements("ItemGroup")
                .FirstOrDefault(g => g.Elements("PackageReference").Any());

            if (itemGroup is null)
            {
                itemGroup = new XElement("ItemGroup");
                project.Add(itemGroup);
            }

            foreach (var package in packages)
            {
                if (existing.Contains(package.PackageId))
                    continue;

                itemGroup.Add(new XElement("PackageReference",
                    new XAttribute("Include", package.PackageId),
                    new XAttribute("Version", package.Version)));
            }

            doc.Save(projectFile);
        }
    }
}
