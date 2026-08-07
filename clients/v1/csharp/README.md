# C# sample — AutoDWG Conversion API

A .NET console application that converts a DWG/DXF file using `HttpClient`.
No external NuGet packages are required (`System.Text.Json` is built in).

## Requirements

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) or newer.

## Configure

Edit the `BaseUrl` / `ApiKey` constants at the top of [`Program.cs`](Program.cs),
or set environment variables:

```powershell
$env:BASE_URL = "https://www.autodwg.com/api"
$env:API_KEY  = "your-real-api-key"
```

## Run

```bash
dotnet run -- path\to\drawing.dwg pdf
```

Arguments (after `--`):

1. Input file (default `..\..\..\..\sample_documents\test.dwg`).
2. Output format — for DWG/DXF input: `pdf` (default), `svg`, `dxf`; for PDF input: `dwg`, `dxf`.

The result is saved as `result.<format>` in the working directory.

## What it does

- `SubmitAsync` — `POST /v1/convert` (multipart) → `task_id`.
- `PollAsync` — `GET /v1/tasks/{task_id}` every 2s until `Success`/`Failed`.
- `DownloadAsync` — `GET /v1/tasks/{task_id}/download` → `result.<format>`.
