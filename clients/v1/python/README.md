# Python sample — AutoDWG Conversion API

Converts a DWG/DXF file using the `requests` library.

## Requirements

- Python 3.7+
- `requests` (install via `requirements.txt`)

```bash
pip install -r requirements.txt
```

## Configure

Edit the constants at the top of [`convert.py`](convert.py), or set environment
variables:

```bash
export BASE_URL="https://www.autodwg.com/api"
export API_KEY="your-real-api-key"
```

## Run

```bash
python convert.py path/to/drawing.dwg pdf
```

Arguments:

1. Input file (default `../../../../sample_documents/test.dwg`).
2. Output format — for DWG/DXF input: `pdf` (default), `svg`, `dxf`; for PDF input: `dwg`, `dxf`.

The result is saved as `result.<format>` in the current directory.

## What it does

- `submit()` — `POST /v1/convert` (multipart file + `output_format`) → `task_id`.
- `poll()` — `GET /v1/tasks/{task_id}` every 2s until `Success`/`Failed`.
- `download()` — `GET /v1/tasks/{task_id}/download` → `result.<format>`.
