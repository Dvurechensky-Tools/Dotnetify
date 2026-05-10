/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 10 мая 2026 08:04:21
 * Version: 1.0.28
 */

namespace Dotnetify.Processors.Dotnet.Core
{
    /// <summary>
    /// Writes the baseline ASP.NET Core host files that make generated controllers
    /// immediately buildable and runnable.
    /// </summary>
    public class DotnetProjectGenerator
    {
        /// <summary>Creates the generated project's .csproj file.</summary>
        public void Generate(string outputDir, string projectName)
        {
            var csproj = $@"
<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>false</InvariantGlobalization>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.6.2"" />
  </ItemGroup>

</Project>
";

            File.WriteAllText(
                Path.Combine(outputDir, $"{projectName}.csproj"),
                csproj
            );
        }

        /// <summary>Creates a minimal Program.cs with controllers and Swagger UI enabled.</summary>
        public void GenerateProgram(string outputDir)
        {
            var code = @"
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger JSON
app.UseSwagger();

// Swagger UI on /swagger
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(""/swagger/v1/swagger.json"", ""Generated API v1"");
    options.RoutePrefix = ""swagger"";
    options.DocumentTitle = ""Dotnetify Docs"";
});

// Routing
app.UseRouting();

app.MapControllers();

// Redirect root -> swagger
app.MapGet(""/"", () => Results.Redirect(""/swagger""));

// Auto-open browser after startup
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var serverUrl = app.Urls.FirstOrDefault() ?? ""http://localhost:5000"";
        var url = $""{serverUrl.TrimEnd('/')}/swagger"";

        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch
    {
    }
});

app.Run();
";

            File.WriteAllText(
                Path.Combine(outputDir, "Program.cs"),
                code
            );
        }
    }
}
