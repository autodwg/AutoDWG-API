#!/usr/bin/env python3
"""
AutoDWG Conversion API - Python sample.

Converts a DWG/DXF file to PDF/SVG/DXF, or a PDF file to DWG/DXF, using the
asynchronous submit -> poll -> download flow.

Requires: Python 3.7+, the `requests` package (see requirements.txt).

Usage:
    python convert.py path/to/drawing.dwg pdf
    python convert.py path/to/drawing.pdf dwg
"""

import os
import sys
import time

import requests

# ---- Configuration ---------------------------------------------------------
BASE_URL = os.environ.get("BASE_URL", "https://www.autodwg.com/api")
API_KEY = os.environ.get("API_KEY", "YOUR_API_KEY")

POLL_INTERVAL_SECONDS = 2
POLL_TIMEOUT_SECONDS = 300


def submit(input_file, output_format):
    """Step 1: upload the file and return the task id."""
    url = f"{BASE_URL}/v1/convert"
    headers = {"x-api-key": API_KEY}
    with open(input_file, "rb") as fh:
        files = {"file": (os.path.basename(input_file), fh)}
        data = {"output_format": output_format}
        resp = requests.post(url, headers=headers, files=files, data=data)

    if resp.status_code != 202:
        raise RuntimeError(f"Submit failed ({resp.status_code}): {resp.text}")

    body = resp.json()
    task_id = body.get("task_id")
    if not task_id:
        raise RuntimeError(f"No task_id in response: {body}")
    print(f"Submitted. task_id={task_id}")
    return task_id


def poll(task_id):
    """Step 2: poll until the task finishes; return the final status body."""
    url = f"{BASE_URL}/v1/tasks/{task_id}"
    headers = {"x-api-key": API_KEY}
    deadline = time.time() + POLL_TIMEOUT_SECONDS

    while True:
        resp = requests.get(url, headers=headers)
        if resp.status_code != 200:
            raise RuntimeError(f"Poll failed ({resp.status_code}): {resp.text}")

        body = resp.json()
        status = body.get("status")
        progress = body.get("progress")
        print(f"  status={status} progress={progress}")

        if status == "Success":
            return body
        if status == "Failed":
            raise RuntimeError(
                f"Conversion failed: {body.get('error_code')} - "
                f"{body.get('error_message')}"
            )
        if time.time() > deadline:
            raise TimeoutError("Timed out waiting for conversion to finish.")
        time.sleep(POLL_INTERVAL_SECONDS)


def download(task_id, output_file):
    """Step 3: download the converted result to a local file."""
    url = f"{BASE_URL}/v1/tasks/{task_id}/download"
    headers = {"x-api-key": API_KEY}
    resp = requests.get(url, headers=headers, stream=True)
    if resp.status_code != 200:
        raise RuntimeError(f"Download failed ({resp.status_code}): {resp.text}")

    with open(output_file, "wb") as fh:
        for chunk in resp.iter_content(chunk_size=8192):
            fh.write(chunk)
    print(f"Saved: {output_file}")


def main():
    input_file = sys.argv[1] if len(sys.argv) > 1 else \
        os.path.join(os.path.dirname(__file__),
                     "..", "..", "..", "..", "sample_documents", "test.dwg")
    output_format = sys.argv[2] if len(sys.argv) > 2 else "pdf"

    if not os.path.isfile(input_file):
        print(f"Input file not found: {input_file}", file=sys.stderr)
        sys.exit(1)

    output_file = f"result.{output_format}"

    task_id = submit(input_file, output_format)
    poll(task_id)
    download(task_id, output_file)
    print("Done.")


if __name__ == "__main__":
    main()
