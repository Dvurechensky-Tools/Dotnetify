/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 12 июня 2026 07:14:04
 * Version: 1.0.60
 */

using System.Diagnostics;
using System.Text;

namespace Dotnetify.Processors.PackageResolve.Core
{
    /// <summary>Captures the exit code and output streams from a dotnet build invocation.</summary>
    public class DotnetBuildResult
    {
        public int ExitCode { get; init; }
        public string StdOut { get; init; } = string.Empty;
        public string StdErr { get; init; } = string.Empty;
    }

    /// <summary>
    /// Runs dotnet build in a generated project directory and returns parser-friendly output.
    /// </summary>
    public class DotnetBuildRunner
    {
        /// <summary>Executes dotnet build with English diagnostics for stable parsing.</summary>
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

            // Keep compiler diagnostics stable for BuildErrorParser across local OS languages.
            psi.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";

            // Some console tools also inspect LANG rather than DOTNET_CLI_UI_LANGUAGE.
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
