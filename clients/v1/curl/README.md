# CURL sample — AutoDWG Conversion API

A small Bash script that runs the full convert → poll → download flow using
`curl` and `jq`.

## Requirements

- `bash`
- [`curl`](https://curl.se/)
- [`jq`](https://stedolan.github.io/jq/) (for parsing JSON responses)

On Windows, run it from **Git Bash** or **WSL**.

## Configure

Edit the top of [`convert.sh`](convert.sh), or pass the values as environment
variables:

```bash
export BASE_URL="https://www.autodwg.com/api"
export API_KEY="your-real-api-key"
```

## Run

```bash
chmod +x convert.sh
./convert.sh path/to/drawing.dwg pdf
```

Arguments:

1. Input file (default `../../../../sample_documents/test.dwg`).
2. Output format — for DWG/DXF input: `pdf` (default), `svg`, `dxf`; for PDF input: `dwg`, `dxf`.

The converted file is written to `result.<format>` in the current directory.

## What it does

1. `POST /v1/convert` with the file and `output_format`, reads `task_id`.
2. Polls `GET /v1/tasks/{task_id}` every 2 seconds until `status` is
   `Success` or `Failed`.
3. On success, downloads `GET /v1/tasks/{task_id}/download` to `result.<format>`.
