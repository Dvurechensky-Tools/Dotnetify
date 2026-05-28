/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 28 мая 2026 17:51:27
 * Version: 1.0.45
 */

using System.Diagnostics;

using Dotnetify.Models;
using Dotnetify.Processors;
using Dotnetify.Processors.Dotnet;
using Dotnetify.Processors.PackageResolve;
using Dotnetify.Processors.Roslyn;

if (args.Length < 2)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("dotnetify generate swagger.json [--name ProjectName] [--run]");
    return;
}

var command = args[0];

if (command == "generate")
{
    // The CLI intentionally stays lightweight for now: generation is driven by
    // positional input plus optional flags, then delegated to the processor pipeline.
    var input = args[1];
    string projectName = "GeneratedApi";
    int port = 5000;
    for (int i = 2; i < args.Length; i++)
    {
        if (args[i].Equals("--name", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
            {
                Console.WriteLine("Project name is required after --name.");
                return;
            }

            projectName = args[i + 1];
            i++;
        }
        else if (args[i].Equals("--port", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 >= args.Length ||
                args[i + 1].StartsWith("--") ||
                !int.TryParse(args[i + 1], out port) ||
                port is < 1 or > 65535)
            {
                Console.WriteLine("Invalid port");
                return;
            }

            i++;
        }
    }

    if (!IsValidProjectName(projectName))
    {
        Console.WriteLine("Project name must be a valid C# namespace and file name.");
        return;
    }

    var runAfterBuild = args.Any(x =>
        x.Equals("--run", StringComparison.OrdinalIgnoreCase));

    var config = new DotnetifyConfig
    {
        InputPath = input,
        OutputPath = "Output",
        ProjectName = projectName,
        Namespace = projectName,
        Processors = new()
        {
            new RoslynSplitProcessor(),
            new DotnetProjectProcessor(),
            new PackageResolveProcessor()
        }
    };

    var pipeline = new DotnetifyPipeline(config);

    await pipeline.RunAsync();

    Console.WriteLine("Generation complete.");

    if (runAfterBuild)
    {
        // --run starts the freshly built API if it is not already running. This keeps
        // the default generate command non-interactive while preserving a fast demo path.
        var outputDir = Path.GetFullPath(Path.Combine(
            config.OutputPath,
            $"{projectName}",
            "bin",
            "Debug",
            "net8.0"));

        var exePath = Path.Combine(outputDir, $"{projectName}.exe");
        var dllPath = Path.Combine(outputDir, $"{projectName}.dll");
        var serverUrl = $"http://localhost:{port}";
        var swaggerUrl = $"{serverUrl}/swagger";

        // Проверка уже запущенного процесса
        var alreadyRunning = Process
            .GetProcesses()
            .Any(p =>
            {
                try
                {
                    return p.ProcessName.Equals(
                        $"{projectName}",
                        StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            });

        if (alreadyRunning)
        {
            Console.WriteLine($"{projectName} is already running.");
            Console.WriteLine($"Swagger: {swaggerUrl}");
            return;
        }

        Console.WriteLine("Starting server...");

        Process? process = null;

        if (File.Exists(exePath))
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"--urls={serverUrl}",
                WorkingDirectory = outputDir,
                UseShellExecute = true
            });
        }
        else if (File.Exists(dllPath))
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{dllPath}\" --urls={serverUrl}",
                WorkingDirectory = outputDir,
                UseShellExecute = true
            });
        }
        else
        {
            Console.WriteLine("Build output not found.");
            return;
        }

        if (process != null)
        {
            Console.WriteLine($"Started PID: {process.Id}");
            Console.WriteLine($"Swagger: {swaggerUrl}");
        }
        else
        {
            Console.WriteLine("Failed to start server.");
        }
    }

    return;
}

Console.WriteLine($"Unknown command: {command}");

static bool IsValidProjectName(string projectName)
{
    // The project name is used as a folder, assembly name, and C# namespace, so validate
    // against the strictest shared contract instead of fixing it later in the pipeline.
    if (string.IsNullOrWhiteSpace(projectName))
        return false;

    if (projectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        return false;

    return projectName
        .Split('.')
        .All(IsValidIdentifier);
}

static bool IsValidIdentifier(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return false;

    if (value[0] != '_' && !char.IsLetter(value[0]))
        return false;

    return value.All(c => c == '_' || char.IsLetterOrDigit(c));
}

//using Dotnetify.Models;
//using Dotnetify.Processors;
//using Dotnetify.Processors.Dotnet;
//using Dotnetify.Processors.PackageResolve;
//using Dotnetify.Processors.Roslyn;

//var config = new DotnetifyConfig
//{
//    InputPath = "Input/swagger.json",
//    OutputPath = "Output",
//    Namespace = "ExperimentDotnetify",
//    Processors = new List<IDotnetifyProcessor>
//    {
//        new RoslynSplitProcessor(),
//        new DotnetProjectProcessor(),
//        new PackageResolveProcessor()
//    }
//};

//var pipeline = new DotnetifyPipeline(config);
//await pipeline.RunAsync();

