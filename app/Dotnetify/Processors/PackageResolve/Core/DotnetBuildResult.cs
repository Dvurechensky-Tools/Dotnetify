/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 20 апреля 2026 05:05:56
 * Version: 1.0.
 */

using System.Diagnostics;
using System.Text;

namespace Dotnetify.Processors.PackageResolve.Core
{
    public class DotnetBuildResult
    {
        public int ExitCode { get; init; }
        public string StdOut { get; init; } = string.Empty;
        public string StdErr { get; init; } = string.Empty;
    }

    public class DotnetBuildRunner
    {
        public async Task<DotnetBuildResult> BuildAsync(
            string workingDirectory,
            CancellationToken cancellationToken = default)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build --nologo",
                WorkingDirectory = workingDirectory,

                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,

                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // Стабильные англоязычные логи для парсинга
            psi.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";

            // Иногда помогает консольным тулзам
            psi.Environment["LANG"] = "en_US.UTF-8";

            using var process = new Process { StartInfo = psi };

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    stdout.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    stderr.AppendLine(e.Data);
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            return new DotnetBuildResult
            {
                ExitCode = process.ExitCode,
                StdOut = stdout.ToString(),
                StdErr = stderr.ToString()
            };
        }
    }
}