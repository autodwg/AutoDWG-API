<?php
/**
 * AutoDWG Conversion API - PHP sample.
 *
 * Converts a DWG/DXF file to PDF/SVG/DXF, or a PDF file to DWG/DXF, using the
 * async submit -> poll -> download flow.
 * Requires the PHP cURL extension (ext-curl).
 *
 * Usage:
 *   php convert.php path/to/drawing.dwg pdf
 *   php convert.php path/to/drawing.pdf dwg
 */

// ---- Configuration ---------------------------------------------------------
$BASE_URL = getenv('BASE_URL') ?: 'https://www.autodwg.com/api';
$API_KEY  = getenv('API_KEY')  ?: 'YOUR_API_KEY';

const POLL_INTERVAL_SECONDS = 2;
const POLL_TIMEOUT_SECONDS   = 300;

// ---- Arguments -------------------------------------------------------------
$inputFile = $argv[1] ?? __DIR__ . '/../../../../sample_documents/test.dwg';
$outputFormat = $argv[2] ?? 'pdf';

if (!is_file($inputFile)) {
    fwrite(STDERR, "Input file not found: $inputFile\n");
    exit(1);
}
$outputFile = "result.$outputFormat";

try {
    $taskId = submit($BASE_URL, $API_KEY, $inputFile, $outputFormat);
    poll($BASE_URL, $API_KEY, $taskId);
    download($BASE_URL, $API_KEY, $taskId, $outputFile);
    echo "Done.\n";
} catch (Exception $e) {
    fwrite(STDERR, 'Error: ' . $e->getMessage() . "\n");
    exit(1);
}

// ---- Step 1: submit --------------------------------------------------------
function submit($baseUrl, $apiKey, $inputFile, $outputFormat) {
    $ch = curl_init("$baseUrl/v1/convert");
    curl_setopt_array($ch, [
        CURLOPT_POST           => true,
        CURLOPT_RETURNTRANSFER => true,
        CURLOPT_HTTPHEADER     => ["x-api-key: $apiKey"],
        CURLOPT_POSTFIELDS     => [
            'file'          => new CURLFile($inputFile, 'application/octet-stream',
                                            basename($inputFile)),
            'output_format' => $outputFormat,
        ],
    ]);
    $body = curl_exec($ch);
    $code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);

    if ($code !== 202) {
        throw new Exception("Submit failed ($code): $body");
    }
    $json = json_decode($body, true);
    if (empty($json['task_id'])) {
        throw new Exception("No task_id in response: $body");
    }
    echo "Submitted. task_id={$json['task_id']}\n";
    return $json['task_id'];
}

// ---- Step 2: poll ----------------------------------------------------------
function poll($baseUrl, $apiKey, $taskId) {
    $deadline = time() + POLL_TIMEOUT_SECONDS;
    while (true) {
        $ch = curl_init("$baseUrl/v1/tasks/$taskId");
        curl_setopt_array($ch, [
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_HTTPHEADER     => ["x-api-key: $apiKey"],
        ]);
        $body = curl_exec($ch);
        $code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);

        if ($code !== 200) {
            throw new Exception("Poll failed ($code): $body");
        }
        $json = json_decode($body, true);
        echo "  status={$json['status']} progress={$json['progress']}\n";

        if ($json['status'] === 'Success') {
            return $json;
        }
        if ($json['status'] === 'Failed') {
            throw new Exception(
                "Conversion failed: {$json['error_code']} - {$json['error_message']}"
            );
        }
        if (time() > $deadline) {
            throw new Exception('Timed out waiting for conversion to finish.');
        }
        sleep(POLL_INTERVAL_SECONDS);
    }
}

// ---- Step 3: download ------------------------------------------------------
function download($baseUrl, $apiKey, $taskId, $outputFile) {
    $ch = curl_init("$baseUrl/v1/tasks/$taskId/download");
    curl_setopt_array($ch, [
        CURLOPT_RETURNTRANSFER => true,
        CURLOPT_HTTPHEADER     => ["x-api-key: $apiKey"],
    ]);
    $data = curl_exec($ch);
    $code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);

    if ($code !== 200) {
        throw new Exception("Download failed ($code): $data");
    }
    file_put_contents($outputFile, $data);
    echo "Saved: $outputFile\n";
}
