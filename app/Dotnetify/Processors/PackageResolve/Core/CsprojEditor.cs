/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 22 апреля 2026 18:58:58
 * Version: 1.0.3
 */

using System.Xml.Linq;

namespace Dotnetify.Processors.PackageResolve.Core
{
    public class CsprojEditor
    {
        public void AddPackages(string projectFile, IReadOnlyCollection<ResolvedPackage> packages)
        {
            if (packages.Count == 0)
                return;

            var doc = XDocument.Load(projectFile);
            var project = doc.Root ?? throw new InvalidOperationException("Invalid .csproj");

            var existing = project
                .Descendants("PackageReference")
                .Select(x => x.Attribute("Include")?.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

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