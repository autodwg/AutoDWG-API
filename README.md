# AutoDWG Conversion API — Online Samples

## About

The **AutoDWG Conversion API** is a REST-based service that converts **DWG / DXF** CAD
files to **PDF, SVG and DXF**, and converts **PDF** back to **DWG / DXF**. It is
designed to be called from any programming language or platform over plain HTTPS.

Key features:

- Hosted service, nothing to install.
- Convert `DWG` / `DXF` to `PDF`, `SVG`, `DXF`, and `PDF` to `DWG` / `DXF`.
- Simple API-Key authentication (`x-api-key` header).
- Asynchronous conversion with task polling — suitable for large files.
- Monthly quota / usage metering per subscription.
- Ready-to-run sample code for **cURL, Python, C#, JavaScript (Node.js)** and **PHP**,
  plus a standalone **browser demo**.

> Get your API Key from the developer portal at `https://www.autodwg.com/api/portal/`.

<br>

## Getting started

Pick the language of your choice below. Each folder contains a runnable sample and
its own README with step-by-step instructions.

- [cURL sample](clients/v1/curl/)
- [Python sample](clients/v1/python/)
- [C# sample](clients/v1/csharp/)
- [JavaScript (Node.js) sample](clients/v1/javascript/)
- [PHP sample](clients/v1/php/)
- [Browser demo (standalone HTML)](demo/)

Every sample defaults to the production endpoint:

```
https://www.autodwg.com/api
```

Change the `BASE_URL` / `API_KEY` constants at the top of each sample before running.

<br>

## Core concepts

Conversion is **asynchronous** and always follows the same three steps:

| Step | Method & Endpoint | Purpose |
|------|-------------------|---------|
| 1. Submit | `POST /v1/convert` | Upload the DWG/DXF/PDF file, receive a `task_id`. |
| 2. Poll | `GET  /v1/tasks/{task_id}` | Poll until `status` is `Success` or `Failed`. |
| 3. Download | `GET  /v1/tasks/{task_id}/download` | Download the converted result file. |

All requests must include the header:

```
x-api-key: YOUR_API_KEY
```

<br>

### 1. Submit a conversion — `POST /v1/convert`

`multipart/form-data` with two fields:

- **file** — the input file (`DWG`, `DXF` or `PDF`).
- **output_format** — the target format. Valid values depend on the input:

| Input | `output_format` values |
|-------|------------------------|
| `DWG` / `DXF` | `pdf` (default), `svg`, `dxf` |
| `PDF` | `dwg`, `dxf` |

Returns **HTTP 202 Accepted**:

```json
{
  "task_id": "12345",
  "status": "Accepted",
  "message": "Task queued for processing"
}
```

<br>

### 2. Poll the task — `GET /v1/tasks/{task_id}`

Returns **HTTP 200**:

```json
{
  "task_id": "12345",
  "status": "Processing",
  "progress": 50,
  "result_url": null,
  "error_code": null,
  "error_message": null,
  "created_at": "2026-07-17T13:42:01",
  "completed_at": null
}
```

`status` is one of:

- **Accepted** — queued, not started yet (`progress` 0).
- **Processing** — conversion in progress (`progress` 50).
- **Success** — finished, `progress` 100, `result_url` set, ready to download.
- **Failed** — see `error_code` / `error_message`.
- **Unknown** — unexpected state.

Poll every 1–2 seconds until `status` is `Success` or `Failed`.

<br>

### 3. Download the result — `GET /v1/tasks/{task_id}/download`

Returns the converted file as a binary stream
(`application/pdf`, `image/svg+xml`, `application/dxf`, `application/dwg`, or
`application/zip` when the result is multi-file).

<br>

## Check your subscription & usage — `GET /v1/subscription`

Returns your current plan, monthly quota and usage. Requires the `x-api-key` header.

Returns **HTTP 200**:

```json
{
  "plan": "Free",
  "monthly_quota": 104857600,
  "used_this_month": 1048576,
  "remaining": 103809024,
  "max_file_size_mb": 10,
  "allowed_formats": ["DWG2PDF", "DWG2SVG"],
  "reset_date": "2026-08-01T00:00:00"
}
```

`monthly_quota`, `used_this_month` and `remaining` are byte counts.

<br>

## Error handling

Errors are returned with a non-2xx HTTP status and a standard JSON body:

```json
{
  "error": {
    "code": "FileTooLarge",
    "message": "File size (12.3MB) exceeds your plan limit (10MB).",
    "details": "Upgrade your plan to increase the file size limit."
  }
}
```

Common `code` values:

| Code | Meaning |
|------|---------|
| `Success` | Operation succeeded. |
| `InvalidApiKey` | Missing or invalid `x-api-key`. |
| `InvalidRequest` | No file provided / malformed request. |
| `UnsupportedFormat` | Input type or requested output format not supported by your plan. |
| `FileTooLarge` | File exceeds your plan's per-file size limit. |
| `QuotaExceeded` | Monthly quota exhausted. |
| `SubscriptionExpired` | Subscription found but expired. |
| `TaskNotFound` | Task id unknown, or not owned by your key. |
| `ProcessingError` | Conversion failed (corrupt input, engine error, …). |

<br>

## Sample documents

Place a test `test.dwg` (or `test.dxf` / `test.pdf`) in [`sample_documents/`](sample_documents/) and
point the samples at it. Any valid AutoCAD DWG/DXF file works, or a PDF for PDF-to-DWG/DXF.

<br>
