# PHP sample — AutoDWG Conversion API

Converts a DWG/DXF file using the PHP cURL extension. No Composer packages
are required.

## Requirements

- PHP 7.2+ with the `curl` extension enabled (`ext-curl`).

## Configure

Edit the `$BASE_URL` / `$API_KEY` values at the top of [`convert.php`](convert.php),
or set environment variables:

```bash
export BASE_URL="https://www.autodwg.com/api"
export API_KEY="your-real-api-key"
```

## Run

```bash
php convert.php path/to/drawing.dwg pdf
```

Arguments:

1. Input file (default `../../../../sample_documents/test.dwg`).
2. Output format — for DWG/DXF input: `pdf` (default), `svg`, `dxf`; for PDF input: `dwg`, `dxf`.

The result is saved as `result.<format>` in the current directory.

## What it does

- `submit()` — `POST /v1/convert` (multipart via `CURLFile`) → `task_id`.
- `poll()` — `GET /v1/tasks/{task_id}` every 2s until `Success`/`Failed`.
- `download()` — `GET /v1/tasks/{task_id}/download` → `result.<format>`.
