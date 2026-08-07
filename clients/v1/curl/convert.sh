#!/usr/bin/env bash
#
# AutoDWG Conversion API — cURL sample
# Converts a DWG/DXF (or PDF) file, polls the task, and downloads the result.
#
# Requires: bash, curl, jq
#
# Usage:
#   ./convert.sh path/to/drawing.dwg pdf
#   ./convert.sh path/to/drawing.pdf dwg
#
set -euo pipefail

# ---- Configuration ---------------------------------------------------------
BASE_URL="${BASE_URL:-https://www.autodwg.com/api}"
API_KEY="${API_KEY:-YOUR_API_KEY}"

# ---- Arguments -------------------------------------------------------------
INPUT_FILE="${1:-../../../../sample_documents/test.dwg}"
OUTPUT_FORMAT="${2:-pdf}"

if [[ ! -f "$INPUT_FILE" ]]; then
  echo "Input file not found: $INPUT_FILE" >&2
  exit 1
fi

OUTPUT_FILE="result.${OUTPUT_FORMAT}"

# ---- Step 1: Submit the conversion ----------------------------------------
echo "Submitting '$INPUT_FILE' (output_format=$OUTPUT_FORMAT) ..."
SUBMIT_RESPONSE=$(curl -sS -X POST "$BASE_URL/v1/convert" \
  -H "x-api-key: $API_KEY" \
  -F "file=@${INPUT_FILE}" \
  -F "output_format=${OUTPUT_FORMAT}")

echo "Response: $SUBMIT_RESPONSE"
TASK_ID=$(echo "$SUBMIT_RESPONSE" | jq -r '.task_id')

if [[ -z "$TASK_ID" || "$TASK_ID" == "null" ]]; then
  echo "Failed to obtain task_id. Check your API key and file." >&2
  exit 1
fi
echo "Task id: $TASK_ID"

# ---- Step 2: Poll the task until it finishes ------------------------------
echo "Polling for completion ..."
while true; do
  STATUS_RESPONSE=$(curl -sS "$BASE_URL/v1/tasks/$TASK_ID" \
    -H "x-api-key: $API_KEY")
  STATUS=$(echo "$STATUS_RESPONSE" | jq -r '.status')
  PROGRESS=$(echo "$STATUS_RESPONSE" | jq -r '.progress')
  echo "  status=$STATUS progress=$PROGRESS"

  case "$STATUS" in
    Success) break ;;
    Failed)
      echo "Conversion failed:" >&2
      echo "$STATUS_RESPONSE" | jq '.error_code, .error_message' >&2
      exit 1
      ;;
  esac
  sleep 2
done

# ---- Step 3: Download the result ------------------------------------------
echo "Downloading result to '$OUTPUT_FILE' ..."
curl -sS "$BASE_URL/v1/tasks/$TASK_ID/download" \
  -H "x-api-key: $API_KEY" \
  -o "$OUTPUT_FILE"

echo "Done. Saved: $OUTPUT_FILE"
