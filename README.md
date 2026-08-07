# Sample documents

Put a test drawing here and point the samples at it.

- Default file name expected by the samples: **`test.dwg`**
- Any valid AutoCAD `DWG` or `DXF` file works.
- A `PDF` file also works when converting **PDF → DWG/DXF** (`output_format` `dwg` or `dxf`).

For example:

```
sample_documents/
└── test.dwg
```

Then run any sample without arguments, or pass an explicit path:

```bash
python ../clients/v1/python/convert.py sample_documents/test.dwg pdf
```
