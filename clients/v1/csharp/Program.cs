using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoDwgConvertSample
{
    /// <summary>
    /// AutoDWG Conversion API - C# console sample.
    /// Converts a DWG/DXF file to PDF/SVG/DXF, or a PDF file to DWG/DXF,
    /// using the async submit -> poll -> download flow.
    ///
    /// Usage:
    ///   dotnet run -- path\to\drawing.dwg pdf
    ///   dotnet run -- path\to\drawing.pdf dwg
    /// </summary>
    internal static class Program
    {
        // ---- Configuration -------------------------------------------------
        private static readonly string BaseUrl =
            Environment.GetEnvironmentVariable("BASE_URL") ?? "https://www.autodwg.com/api";
        private static readonly string ApiKey =
            Environment.GetEnvironmentVariable("API_KEY") ?? "YOUR_API_KEY";

        private const int PollIntervalMs = 2000;
        private const int PollTimeoutMs = 300_000;

        private static readonly HttpClient Http = new HttpClient();

        private static async Task<int> Main(string[] args)
        {
            string inputFile = args.Length > 0
                ? args[0]
                : Path.Combine("..", "..", "..", "..", "sample_documents", "test.dwg");
            string outputFormat = args.Length > 1 ? args[1] : "pdf";

            if (!File.Exists(inputFile))
            {
                Console.Error.WriteLine($"Input file not found: {inputFile}");
                return 1;
            }

            string outputFile = $"result.{outputFormat}";
            Http.DefaultRequestHeaders.Add("x-api-key", ApiKey);

            try
            {
                string taskId = await SubmitAsync(inputFile, outputFormat);
                await PollAsync(taskId);
                await DownloadAsync(taskId, outputFile);
                Console.WriteLine("Done.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        // ---- Step 1: submit ------------------------------------------------
        private static async Task<string> SubmitAsync(string inputFile, string outputFormat)
        {
            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(inputFile));
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "file", Path.GetFileName(inputFile));
            form.Add(new StringContent(outputFormat), "output_format");

            var resp = await Http.PostAsync($"{BaseUrl}/v1/convert", form);
            string body = await resp.Content.ReadAsStringAsync();
            if ((int)resp.StatusCode != 202)
                throw new Exception($"Submit failed ({(int)resp.StatusCode}): {body}");

            using var doc = JsonDocument.Parse(body);
            string taskId = doc.RootElement.GetProperty("task_id").GetString();
            Console.WriteLine($"Submitted. task_id={taskId}");
            return taskId;
        }

        // ---- Step 2: poll --------------------------------------------------
        private static async Task PollAsync(string taskId)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(PollTimeoutMs);
            while (true)
            {
                var resp = await Http.GetAsync($"{BaseUrl}/v1/tasks/{taskId}");
                string body = await resp.Content.ReadAsStringAsync();
                if ((int)resp.StatusCode != 200)
                    throw new Exception($"Poll failed ({(int)resp.StatusCode}): {body}");

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                string status = root.GetProperty("status").GetString();
                int progress = root.GetProperty("progress").GetInt32();
                Console.WriteLine($"  status={status} progress={progress}");

                if (status == "Success") return;
                if (status == "Failed")
                {
                    string code = GetStringOrNull(root, "error_code");
                    string msg = GetStringOrNull(root, "error_message");
                    throw new Exception($"Conversion failed: {code} - {msg}");
                }
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Timed out waiting for conversion.");

                await Task.Delay(PollIntervalMs);
            }
        }

        // ---- Step 3: download ---------------------------------------------
        private static async Task DownloadAsync(string taskId, string outputFile)
        {
            var resp = await Http.GetAsync($"{BaseUrl}/v1/tasks/{taskId}/download");
            if ((int)resp.StatusCode != 200)
            {
                string body = await resp.Content.ReadAsStringAsync();
                throw new Exception($"Download failed ({(int)resp.StatusCode}): {body}");
            }

            await using var fs = File.Create(outputFile);
            await resp.Content.CopyToAsync(fs);
            Console.WriteLine($"Saved: {outputFile}");
        }

        private static string GetStringOrNull(JsonElement el, string name) =>
            el.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null
                ? v.GetString()
                : null;
    }
}
