/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 26 апреля 2026 10:11:16
 * Version: 1.0.7
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
    var input = args[1];
    string projectName = "GeneratedApi";
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
        var outputDir = Path.GetFullPath(Path.Combine(
            config.OutputPath,
            $"{projectName}",
            "bin",
            "Debug",
            "net8.0"));

        var exePath = Path.Combine(outputDir, $"{projectName}.exe");
        var dllPath = Path.Combine(outputDir, $"{projectName}.dll");

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
            Console.WriteLine("Swagger: http://localhost:5000/swagger");
            return;
        }

        Console.WriteLine("Starting server...");

        Process? process = null;

        if (File.Exists(exePath))
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = outputDir,
                UseShellExecute = true
            });
        }
        else if (File.Exists(dllPath))
        {
            process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{dllPath}\"",
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
            Console.WriteLine("Swagger: http://localhost:5000/swagger");
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

