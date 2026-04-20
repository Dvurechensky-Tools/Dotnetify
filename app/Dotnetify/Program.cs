/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 20 апреля 2026 16:38:29
 * Version: 1.0.1
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
    Console.WriteLine("dotnetify generate swagger.json [--run]");
    return;
}

var command = args[0];

if (command == "generate")
{
    var input = args[1];

    var runAfterBuild = args.Any(x =>
        x.Equals("--run", StringComparison.OrdinalIgnoreCase));

    var config = new DotnetifyConfig
    {
        InputPath = input,
        OutputPath = "Output",
        Namespace = "GeneratedApi",
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
            "GeneratedApi",
            "bin",
            "Debug",
            "net8.0"));

        var exePath = Path.Combine(outputDir, "GeneratedApi.exe");
        var dllPath = Path.Combine(outputDir, "GeneratedApi.dll");

        // Проверка уже запущенного процесса
        var alreadyRunning = Process
            .GetProcesses()
            .Any(p =>
            {
                try
                {
                    return p.ProcessName.Equals(
                        "GeneratedApi",
                        StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            });

        if (alreadyRunning)
        {
            Console.WriteLine("GeneratedApi is already running.");
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

