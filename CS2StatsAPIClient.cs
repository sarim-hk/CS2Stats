using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text;
using CounterStrikeSharp.API.Modules.Entities;
using Microsoft.Extensions.Logging;

public class CS2StatsAPIClient {
    private readonly HttpClient httpClient;
    private readonly ILogger Logger;

    public CS2StatsAPIClient(string authKey, string baseURL, ILogger Logger) {
        this.Logger = Logger;
        this.httpClient = new HttpClient {
            BaseAddress = new Uri(baseURL),
            Timeout = TimeSpan.FromMinutes(1)
        };
        this.httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authKey);
    }

    public async Task UploadMatchJSONAsync(string matchJSON) {
        var jsonBytes = Encoding.UTF8.GetBytes(matchJSON);

        using var compressedStream = new MemoryStream();
        await using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true)) {
            await gzipStream.WriteAsync(jsonBytes);
        }
        compressedStream.Position = 0;

        using var content = new StreamContent(compressedStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        content.Headers.ContentEncoding.Add("gzip");

        var response = await httpClient.PostAsync("/upload_match", content);
        if (!response.IsSuccessStatusCode) {
            var errorBody = await response.Content.ReadAsStringAsync();
            Logger.LogError($"[UploadMatchJSONAsync] Upload failed. Status={(int)response.StatusCode}, Body={errorBody}");
            return;
        }
    }

    public async Task UploadPlayerJSONAsync(ulong playerID) {
        var response = await httpClient.PostAsJsonAsync("/upload_player", new { playerID });

        if (!response.IsSuccessStatusCode) {
            var errorBody = await response.Content.ReadAsStringAsync();
            Logger.LogError($"[UploadPlayerJSONAsync] Upload failed. Status={(int)response.StatusCode}, Body={errorBody}");
            return;
        }
    }

}