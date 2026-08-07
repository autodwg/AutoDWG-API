# AutoDWG Conversion API — Client samples

This folder contains ready-to-run client samples grouped by API version and language.

```
clients/
└── v1/
    ├── curl/         # Shell script using curl + jq
    ├── python/       # Python 3 (requests)
    ├── csharp/       # .NET console app (HttpClient)
    ├── javascript/   # Node.js (built-in fetch / form-data)
    └── php/          # PHP (cURL extension)
```

Every sample implements the same three-step asynchronous flow:

1. `POST /v1/convert` — upload a DWG/DXF/PDF file and get a `task_id`.
2. `GET  /v1/tasks/{task_id}` — poll until `status` is `Success` or `Failed`.
3. `GET  /v1/tasks/{task_id}/download` — download the converted result.

All samples share two settings you must configure before running:

| Setting | Default | Notes |
|---------|---------|-------|
| `BASE_URL` | `https://www.autodwg.com/api` | API root, no trailing slash. |
| `API_KEY`  | `YOUR_API_KEY` | Get it from the developer portal. |

Authentication is done with the `x-api-key` HTTP header on every request.

See the top-level [README](../README.md) for the full API reference and error-code table.
