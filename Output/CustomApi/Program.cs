
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
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Generated API v1");
    options.RoutePrefix = "docs";
    options.DocumentTitle = "Dotnetify Docs";
});

// Routing
app.UseRouting();

app.MapControllers();

// Redirect root -> docs
app.MapGet("/", () => Results.Redirect("/docs"));

// Auto-open browser after startup
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var url = "http://localhost:5000/docs";

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
