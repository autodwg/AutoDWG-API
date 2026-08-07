#!/usr/bin/env node
/**
 * AutoDWG Conversion API - Node.js sample.
 *
 * Converts a DWG/DXF file to PDF/SVG/DXF, or a PDF file to DWG/DXF, using the
 * async submit -> poll -> download flow.
 * Uses the built-in global `fetch`, `FormData` and `Blob` (Node.js 18+).
 *
 * Usage:
 *   node convert.js path/to/drawing.dwg pdf
 *   node convert.js path/to/drawing.pdf dwg
 */

'use strict';

const fs = require('fs');
const path = require('path');

// ---- Configuration ---------------------------------------------------------
const BASE_URL = process.env.BASE_URL || 'https://www.autodwg.com/api';
const API_KEY = process.env.API_KEY || 'YOUR_API_KEY';

const POLL_INTERVAL_MS = 2000;
const POLL_TIMEOUT_MS = 300000;

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

// ---- Step 1: submit --------------------------------------------------------
async function submit(inputFile, outputFormat) {
  const buffer = fs.readFileSync(inputFile);
  const form = new FormData();
  form.append('file', new Blob([buffer]), path.basename(inputFile));
  form.append('output_format', outputFormat);

  const resp = await fetch(`${BASE_URL}/v1/convert`, {
    method: 'POST',
    headers: { 'x-api-key': API_KEY },
    body: form,
  });

  const text = await resp.text();
  if (resp.status !== 202) {
    throw new Error(`Submit failed (${resp.status}): ${text}`);
  }
  const body = JSON.parse(text);
  if (!body.task_id) throw new Error(`No task_id in response: ${text}`);
  console.log(`Submitted. task_id=${body.task_id}`);
  return body.task_id;
}

// ---- Step 2: poll ----------------------------------------------------------
async function poll(taskId) {
  const deadline = Date.now() + POLL_TIMEOUT_MS;
  for (;;) {
    const resp = await fetch(`${BASE_URL}/v1/tasks/${taskId}`, {
      headers: { 'x-api-key': API_KEY },
    });
    const text = await resp.text();
    if (resp.status !== 200) {
      throw new Error(`Poll failed (${resp.status}): ${text}`);
    }
    const body = JSON.parse(text);
    console.log(`  status=${body.status} progress=${body.progress}`);

    if (body.status === 'Success') return body;
    if (body.status === 'Failed') {
      throw new Error(
        `Conversion failed: ${body.error_code} - ${body.error_message}`
      );
    }
    if (Date.now() > deadline) {
      throw new Error('Timed out waiting for conversion to finish.');
    }
    await sleep(POLL_INTERVAL_MS);
  }
}

// ---- Step 3: download ------------------------------------------------------
async function download(taskId, outputFile) {
  const resp = await fetch(`${BASE_URL}/v1/tasks/${taskId}/download`, {
    headers: { 'x-api-key': API_KEY },
  });
  if (resp.status !== 200) {
    const text = await resp.text();
    throw new Error(`Download failed (${resp.status}): ${text}`);
  }
  const arrayBuffer = await resp.arrayBuffer();
  fs.writeFileSync(outputFile, Buffer.from(arrayBuffer));
  console.log(`Saved: ${outputFile}`);
}

async function main() {
  const inputFile =
    process.argv[2] ||
    path.join(__dirname, '..', '..', '..', '..', 'sample_documents', 'test.dwg');
  const outputFormat = process.argv[3] || 'pdf';

  if (!fs.existsSync(inputFile)) {
    console.error(`Input file not found: ${inputFile}`);
    process.exit(1);
  }

  const outputFile = `result.${outputFormat}`;
  const taskId = await submit(inputFile, outputFormat);
  await poll(taskId);
  await download(taskId, outputFile);
  console.log('Done.');
}

main().catch((err) => {
  console.error('Error:', err.message);
  process.exit(1);
});
