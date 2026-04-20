<div align="center">

# Dotnetify

### Turn any OpenAPI (`swagger.json`) file into a runnable C# (.NET) backend project in seconds.

<p align="center">
  <img src="https://shields.dvurechensky.pro/badge/.NET-8.0-blue?logo=dotnet">
  <img src="https://shields.dvurechensky.pro/badge/OpenAPI-Swagger-green">
  <img src="https://shields.dvurechensky.pro/badge/Status-Active-brightgreen">
  <img src="https://shields.dvurechensky.pro/badge/CLI-Tool-black">
</p>

</div>

---

<div align="center" style="margin: 20px 0; padding: 10px; background: #1c1917; border-radius: 10px;">
  <strong>🌐 Language: </strong>
  
  <a href="./README.ru.md" style="color: #F5F752; margin: 0 10px;">
    🇷🇺 Russian
  </a>
  | 
  <span style="color: #0891b2; margin: 0 10px;">
    ✅ 🇺🇸 English (current)
  </span>
</div>

---

## Overview

**Dotnetify** is a CLI tool that transforms an OpenAPI / Swagger specification into a fully generated **ASP.NET Core server project**.

Instead of producing raw generator output, Dotnetify builds a **structured, launch-ready backend scaffold** that can be compiled, started, and extended immediately.

It is designed for developers who need a fast backend starting point from an existing API schema.

> [!IMPORTANT]
> Dotnetify focuses on producing a **usable server project**, not just generated source files.

---

## What It Does

Input:

```text
swagger.json
```

Pipeline:

```text
OpenAPI Spec
    ↓
Code Generation
    ↓
Roslyn Processing
    ↓
Project Structuring
    ↓
Runnable .NET Backend
```

Output:

```text
GeneratedApi/
 ├── Controllers/
 ├── Models/
 ├── Enums/
 ├── Common/
 ├── Program.cs
 ├── GeneratedApi.csproj
```

---

## Features

| Category          | Description                                 |
| ----------------- | ------------------------------------------- |
| OpenAPI Import    | Reads `swagger.json` specifications         |
| Controllers       | Generates API controllers by route groups   |
| Models            | Generates DTO / entity classes              |
| Enums             | Generates enums from schema definitions     |
| Mock Responses    | Auto-generates test responses for endpoints |
| Build Ready       | Produces compilable .NET project            |
| Run Ready         | Can start generated API instantly           |
| Swagger UI        | Generated server exposes Swagger portal     |
| Roslyn Processing | Cleans and restructures generated code      |

---

## Why Dotnetify?

Most generators stop here:

```text
swagger.json → raw generated code
```

Dotnetify continues further:

```text
swagger.json → working backend project
```

That means:

- Faster frontend development
- Faster prototyping
- Easier API testing
- Better reverse engineering workflows
- Immediate backend starting point

---

## Quick Start

Go to the compiled binary:

```bash
cd app\Dotnetify\bin\Debug\net8.0
```

Generate and run:

```bash
Dotnetify.exe generate Input\swagger.json --run
```

Example output:

```text
[Dotnetify] Running: Roslyn Split Processor
[Dotnetify] Running: Dotnet Project Processor
[Dotnetify] Running: Package Resolve Processor

Generation complete.
Starting server...
Started PID: 29296

Swagger:
http://localhost:5000/swagger
```

---

## Use Cases

### Frontend Teams

Need backend before real backend exists.

### QA / API Testing

Generate local API from schema instantly.

### Legacy Systems

Recreate documented APIs into editable .NET projects.

### Startups / MVP

Launch backend prototype rapidly.

---

## Example Result

Generated controller:

```csharp
[HttpGet("inventory")]
public Task<IDictionary<string, int>> GetInventory()
{
    var response = new Dictionary<string, int>
    {
        { "test", 1 }
    };

    return Task.FromResult<IDictionary<string, int>>(response);
}
```

---

## Philosophy

Dotnetify does **not** aim to replace real backend engineering.

It aims to eliminate the empty starting phase.

Instead of spending hours scaffolding:

- controllers
- DTOs
- routes
- startup boilerplate
- fake data

You start with all of it already generated.

---

## Roadmap

Planned future improvements:

- Database scaffolding
- Repository generation
- React admin panel generation
- Authentication templates
- Docker support
- CI/CD templates
- Multiple architecture modes

---

## Current Status

> Active development

Core generation pipeline is already functional and produces runnable projects.

---

## Tech Stack

- C#
- .NET
- ASP.NET Core
- Roslyn
- NSwag
