/*
 * Author: Nikolay Dvurechensky
 * Site: https://dvurechensky.pro/
 * Gmail: dvurechenskysoft@gmail.com
 * Last Updated: 24 апреля 2026 07:12:04
 * Version: 1.0.5
 */

namespace Dotnetify.Processors.Dotnet.Core
{
    public class DotnetProjectGenerator
    {
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

// Swagger UI on /docs
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(""/swagger/v1/swagger.json"", ""Generated API v1"");
    options.RoutePrefix = ""docs"";
    options.DocumentTitle = ""Dotnetify Docs"";
});

// Routing
app.UseRouting();

app.MapControllers();

// Redirect root -> docs
app.MapGet(""/"", () => Results.Redirect(""/docs""));

// Auto-open browser after startup
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var url = ""http://localhost:5000/docs"";

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